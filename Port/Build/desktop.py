"""Rebuild and install the Windows Mono core loop into the real Unity project.

This is an incremental integration, not full renderer/audio/API acceptance.
--validate builds a disposable copy of the main project's Assets/Packages/settings
and exercises the real entry point with Unity Input System events.
"""
from __future__ import annotations

import argparse
import os
from pathlib import Path
import re
import shutil
import tempfile
import uuid
import zipfile

import baseline as b
import flac
import foundation
import generate_compatibility as generator
import images
import shaders
import mono_probe as mono

PROFILE = b.PORT / "Compatibility/Desktop"
PROJECTS = ("Engine.Windows", "EntitySystem.Windows", "Survivalcraft.Windows")
NATIVE = {"libEGL.dll", "libGLESv2.dll", "openal32.dll", "openxr_loader.dll", "SDL2.dll", "glfw3.dll"}


def prepare(workspace: Path) -> Path:
    generated = workspace / "generated"
    generator.generate(PROFILE / "profile.json", generated)
    source = workspace / "src"
    # The generator already checks the full archive inventory and every hash.
    with zipfile.ZipFile(generated / "upstream.zip") as archive:
        archive.extractall(source)
    shutil.copytree(generated / "src", source, dirs_exist_ok=True)
    manifest = b.read_json(PROFILE / "support-manifest.json")
    for group in ("additions", "projects"):
        for relative, record in manifest[group].items():
            target = source / generator.relative_path(relative)
            data = (PROFILE / generator.relative_path(record["source"])).read_bytes().replace(b"\r\n", b"\n")
            if b.digest(data) != record["sha256"]:
                raise ValueError("Desktop source changed without manifest update: " + relative)
            if record.get("upstreamSha256") and b.digest(target.read_bytes()) != record["upstreamSha256"]:
                raise ValueError("Project input changed: " + relative)
            target.parent.mkdir(parents=True, exist_ok=True)
            target.write_bytes(data)
    return source


def build(workspace: Path, unity: Path, refresh_locks: bool = False) -> Path:
    print("Rebuilding pinned FLAC and ImageSharp sources...", flush=True)
    deps = workspace / "deps"; deps.mkdir()
    for name, module, dll in (("flac", flac, "NAudio.Flac.dll"), ("images", images, "SixLabors.ImageSharp.dll")):
        directory = workspace / name; directory.mkdir()
        projects = module.build(directory, unity)
        shutil.copyfile(projects / "Runner/bin/Release/net48" / dll, deps / dll)
    source = prepare(workspace)
    for project in PROJECTS:
        lock = PROFILE / "Locks" / project / "packages.lock.json"
        if lock.exists(): shutil.copyfile(lock, source / project / "packages.lock.json")
        elif not refresh_locks: raise ValueError("Missing committed dependency lock: " + project)
    options = [f"-p:UnityFrameworkPath={foundation.framework_path(unity)}",
               f"-p:PathMap={workspace}=/scunity-desktop", "-p:RestorePackagesWithLockFile=true"]
    project = source / "Survivalcraft.Windows/Survivalcraft.Windows.csproj"
    print("Building original Engine, EntitySystem and Survivalcraft for Unity Mono...", flush=True)
    b.run(["dotnet", "restore", project, *([] if refresh_locks else ["--locked-mode"]),
           "--configfile", b.PORT / "Build/NuGet.Config", "-p:NuGetAudit=false", *options], log=workspace / "restore.log")
    b.run(["dotnet", "build", project, "--no-restore", "-c", "Release", *options], log=workspace / "build.log")
    for name in PROJECTS:
        lock = source / name / "packages.lock.json"
        committed = PROFILE / "Locks" / name / "packages.lock.json"
        if refresh_locks:
            committed.parent.mkdir(parents=True, exist_ok=True); shutil.copyfile(lock, committed)
        else: b.assert_equal(committed, lock)
    return source / "Survivalcraft.Windows/bin/Release/net48"


def install(output: Path, unity: Path) -> dict:
    if "com.scunity.engine" in b.read_json(b.ROOT / "Packages/manifest.json")["dependencies"]:
        raise ValueError("Source packages are active. Use Port/Build/source.py --install; core DLL installation is disabled.")
    plugins = b.ROOT / "Assets/SCUnity/Plugins/Generated"
    plugins.mkdir(parents=True, exist_ok=True)
    framework = foundation.framework_path(unity)
    supplied = mono.UNITY_FRAMEWORK_ASSEMBLIES | {p.name for p in framework.rglob("*.dll")}
    files = {}
    for dll in sorted(output.glob("*.dll")):
        if dll.name in supplied | NATIVE: continue
        target = plugins / dll.name
        shutil.copyfile(dll, target)
        files[target.relative_to(b.ROOT).as_posix()] = b.digest(target.read_bytes())
        meta = target.with_name(target.name + ".meta")
        if not meta.exists() or "PluginImporter:" not in meta.read_text():
            guid = (re.search(r"guid: (\w+)", meta.read_text()).group(1) if meta.exists()
                    else uuid.uuid5(uuid.NAMESPACE_URL, "scunity:" + target.relative_to(b.ROOT).as_posix()).hex)
            meta.write_text("fileFormatVersion: 2\nguid: " + guid + "\nPluginImporter:\n  externalObjects: {}\n"
                "  serializedVersion: 2\n  isPreloaded: 0\n  isOverridable: 1\n  isExplicitlyReferenced: 1\n"
                "  validateReferences: 1\n  platformData:\n  - first:\n      Any: \n    second:\n      enabled: 1\n      settings: {}\n")
        if "isExplicitlyReferenced: 1" not in meta.read_text():
            raise ValueError("Game plugins require Auto Reference disabled: " + str(meta))
    unexpected = {p.name for p in plugins.glob("*.dll")} - {Path(p).name for p in files}
    if unexpected: raise ValueError("Unexpected old plugin files require review: " + str(unexpected))
    content = b.ROOT / "Assets/StreamingAssets/Survivalcraft"
    content.mkdir(parents=True, exist_ok=True)
    for name in ("Content.zip", "init.js"):
        shutil.copyfile(output / name, content / name)
        files[(content / name).relative_to(b.ROOT).as_posix()] = b.digest((content / name).read_bytes())
    b.write_json(b.PORT / ".artifacts/desktop-install.json", {"upstreamCommit": b.SHA, "files": files})
    return files


def snapshot_roots() -> tuple[str, ...]:
    roots = ("Assets", "Packages", "ProjectSettings")
    if "com.scunity.engine" in b.read_json(b.ROOT / "Packages/manifest.json")["dependencies"]:
        roots += tuple("External/SurvivalcraftApi/" + name for name in b.ASSEMBLIES)
    return roots


def main_snapshot() -> dict:
    files = [p for name in ("Assets", "Packages", "ProjectSettings")
             for p in (b.ROOT / name).rglob("*") if p.is_file()]
    if len(snapshot_roots()) > 3:
        # The source guard requires a clean committed checkout. Ignore old build
        # products such as Content.zip that may remain in an existing checkout.
        tracked = b.run(["git", "-C", b.UPSTREAM, "ls-files", "-z", "--", *b.ASSEMBLIES])
        files.extend(b.UPSTREAM / name for name in tracked.split("\0") if name)
    return {p.relative_to(b.ROOT).as_posix(): b.digest(p.read_bytes()) for p in sorted(files)}


def validate(workspace: Path, unity: Path, audio_test: bool = False, world_test: bool = False, community_test: bool = False) -> dict:
    shaders.generate(check=True)
    before = main_snapshot()
    project = workspace / "main-project"
    for name in ("Assets", "Packages", "ProjectSettings"):
        shutil.copytree(b.ROOT / name, project / name)
    for relative in before:
        if relative.startswith("External/SurvivalcraftApi/"):
            target = project / relative; target.parent.mkdir(parents=True, exist_ok=True)
            shutil.copyfile(b.ROOT / relative, target)
    player = workspace / "player/SCUnity.exe"; player.parent.mkdir()
    print("Building a complete main-project snapshot; the open Editor stays in place...", flush=True)
    mono.execute([unity, "-batchmode", "-nographics", "-projectPath", project, "-buildTarget", "Win64",
                  "-executeMethod", "SCUnity.Editor.SurvivalcraftProject.BuildCoreLoop", "-scunity-player", player,
                  "-logFile", workspace / "unity-build.log"], workspace / "unity-process.log", 1200)
    build_result = b.read_json(player.parent / "build.json")
    if build_result != {"result": "Succeeded", "errors": 0, "warnings": 0}:
        raise ValueError("Main project build was not clean: " + str(build_result))
    runs = []
    for index in range(2):
        output = workspace / f"smoke-{index + 1}"; output.mkdir()
        # A normally visible Player is required to execute URP on Windows.
        # Each invocation has an isolated data/result directory and closes itself.
        mono.execute([player, "-scunity-smoke-output", output, *(["-scunity-audio-test"] if audio_test else []), *(["-scunity-world-test"] if world_test else []),
                      *(["-scunity-community-test"] if community_test else []),
                      "-logFile", output / "player.log"], output / "process.log", 300 if world_test else 120)
        result = b.read_json(output / "result.json")
        if (not all(result.get(key) is True for key in ("passed", "enteredSettingsWithMouse", "returnedWithEscape", "isMono"))
                or result.get("isEditor") is not False or result.get("error") or result.get("pointerSize") != 8
                or result.get("platform") != "WindowsPlayer" or result.get("executedDraws", 0) < 1
                or result.get("entryPoint") != "SCUnity.Runtime.SurvivalcraftGame"):
            raise ValueError("Main-project Player assertions failed: " + str(result))
        if audio_test and (len(result.get("audioChecks", [])) != 12 or result.get("audioBlocks", 0) < 1):
            raise ValueError("Unity DSP audio assertions are incomplete: " + str(result))
        if world_test and (len(result.get("worldChecks", [])) != 17 or result.get("screen") != "Game.GameScreen"):
            raise ValueError("World/render/index assertions are incomplete: " + str(result))
        if community_test and len(result.get("communityChecks", [])) != 17:
            raise ValueError("Community search/thumbnail assertions are incomplete: " + str(result))
        if len(snapshot_roots()) > 3 and len(result.get("sourceChecks", [])) != 14:
            raise ValueError("Source compilation assertions are incomplete: " + str(result))
        runs.append(result)
    editor_output = workspace / "editor-validation"; editor_output.mkdir()
    print("Verifying two Editor Play/Stop cycles with Domain reload...", flush=True)
    mono.execute([unity, "-batchmode", "-nographics", "-projectPath", project, "-buildTarget", "Win64",
                  "-executeMethod", "SCUnity.Editor.CoreLoopEditorTest.Run",
                  "-scunity-validation-output", editor_output, "-scunity-data-path", editor_output / "data",
                  "-logFile", editor_output / "editor.log"], editor_output / "process.log", 240)
    editor_result = b.read_json(editor_output / "editor-result.json")
    if not editor_result.get("passed") or editor_result.get("error") or editor_result.get("playStopCycles") != 2:
        raise ValueError("Editor Play/Stop failed: " + str(editor_result))
    after = main_snapshot()
    if before != after: raise ValueError("Main project changed while its snapshot was being verified; rerun validation.")
    evidence = {"schemaVersion": 1, "upstreamCommit": b.SHA, "mainProject": before,
                "build": build_result, "runs": runs, "editor": editor_result,
                "files": {p.relative_to(workspace).as_posix(): b.digest(p.read_bytes()) for p in workspace.glob("smoke-*/*") if p.is_file()},
                "scope": "Original loading/main-menu/settings loop through main Unity scene; "
                         + ("static/streaming PCM and original music verified at Unity listener; " if audio_test else "audio not asserted in this run; ")
                         + ("initial world/model/save-reload and selected GPU states verified; " if world_test else "world not asserted in this run; ")
                         + "full world/mod/API/platform compatibility remains incomplete."}
    b.write_json(workspace / "main-project-evidence.json", evidence)
    return evidence


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--unity", type=Path, required=True)
    parser.add_argument("--install", action="store_true")
    parser.add_argument("--validate", action="store_true")
    parser.add_argument("--validate-installed", action="store_true", help="Validate current main-project files without rebuilding dependencies")
    parser.add_argument("--refresh-package-locks", action="store_true")
    parser.add_argument("--audio-test", action="store_true", help="Also validate static/streamed PCM through the Unity listener")
    parser.add_argument("--world-test", action="store_true", help="Also validate world creation, rendering, models, saving/reloading and GPU states")
    parser.add_argument("--community-test", action="store_true", help="Also validate community searches, thumbnail lifetime and result-cache refresh with fixture server replies")
    args = parser.parse_args(); os.environ["DOTNET_CLI_UI_LANGUAGE"] = "en"
    unity = args.unity.resolve()
    workspace = Path(tempfile.mkdtemp(prefix="desktop-", dir=b.PORT / ".artifacts"))
    print("Evidence: " + str(workspace), flush=True)
    if not args.validate_installed:
        output = build(workspace, unity, args.refresh_package_locks)
        b.write_json(workspace / "assemblies.json", {p.name: b.digest(p.read_bytes()) for p in output.glob("*.dll")})
        if args.install: install(output, unity)
    if args.validate or args.validate_installed:
        if not (args.install or args.validate_installed): raise ValueError("--validate requires --install or --validate-installed")
        validate(workspace, unity, args.audio_test, args.world_test, args.community_test)
    b.check_upstream(b.SHA)
    print("Completed. " + str(workspace), flush=True)


if __name__ == "__main__":
    main()
