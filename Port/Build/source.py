"""Bootstrap third-party dependencies/content; Unity compiles all three core assemblies.

No source generation or core compilation runs in this script. The editable local
UPM packages in External/SurvivalcraftApi are the authoritative port sources.
"""
from __future__ import annotations

import argparse
import gzip
import os
from pathlib import Path
import shutil
import tempfile
import zipfile

import baseline as b
import desktop
import flac
import foundation
import images
import mono_probe as mono
import source_api

CORE = {name + ".dll" for name in b.ASSEMBLIES}
ANALYZER = "System.Text.Json.SourceGeneration.dll"
DEPENDENCIES = b.PORT / "Dependencies/Unity"


def dependencies(workspace: Path, unity: Path, refresh_lock: bool = False) -> Path:
    project = workspace / "dependencies"
    project.mkdir()
    text = (DEPENDENCIES / "Dependencies.csproj").read_text(encoding="utf-8")
    (project / "Dependencies.csproj").write_text(text.replace(
        "../../Compatibility/Desktop/Projects/Directory.Build.props", "UnityFramework.props"), encoding="utf-8", newline="\n")
    shutil.copyfile(desktop.PROFILE / "Projects/Directory.Build.props", project / "UnityFramework.props")
    lock = DEPENDENCIES / "packages.lock.json"
    if lock.exists(): shutil.copyfile(lock, project / lock.name)
    elif not refresh_lock: raise ValueError("Missing dependency lock")
    options = [f"-p:UnityFrameworkPath={foundation.framework_path(unity)}", "-p:NuGetAudit=false"]
    b.run(["dotnet", "restore", project / "Dependencies.csproj", *([] if refresh_lock else ["--locked-mode"]),
           "--configfile", b.PORT / "Build/NuGet.Config", *options], log=workspace / "restore.log")
    b.run(["dotnet", "build", project / "Dependencies.csproj", "--no-restore", "-c", "Release", *options], log=workspace / "dependencies.log")
    if refresh_lock: shutil.copyfile(project / lock.name, lock)
    else: b.assert_equal(lock, project / lock.name)
    output = project / "bin/Release/net48"
    for name, module, dll in (("flac", flac, "NAudio.Flac.dll"), ("images", images, "SixLabors.ImageSharp.dll")):
        directory = workspace / name; directory.mkdir()
        print("Rebuilding pinned third-party " + name + "...", flush=True)
        projects = module.build(directory, unity)
        shutil.copyfile(projects / "Runner/bin/Release/net48" / dll, output / dll)
    assets = b.read_json(project / "obj/project.assets.json")
    candidates = [Path(folder) / "system.text.json/10.0.10/analyzers/dotnet/roslyn4.0/cs" / ANALYZER
                  for folder in assets["packageFolders"]]
    generator = next((p for p in candidates if p.is_file()), None)
    if generator is None: raise ValueError("Pinned STJ analyzer missing")
    shutil.copyfile(generator, output / ANALYZER)
    # Retained native NAudio dependency. This is not one of the three core DLLs.
    shutil.copyfile(b.UPSTREAM / "Survivalcraft.Windows/wrap_oal.dll", output / "wrap_oal.dll")
    if any((output / name).exists() for name in CORE): raise ValueError("Dependency build produced a core DLL")
    return output


def pack_content(directory: Path, target: Path) -> None:
    with zipfile.ZipFile(target, "w", zipfile.ZIP_DEFLATED) as archive:
        for file in sorted(directory.rglob("*")):
            if not file.is_file() or file.suffix == ".meta": continue
            data = file.read_bytes()
            # The source package declares JSON as LF. Git can consider an older
            # CRLF working copy clean; canonicalize JSON whitespace at packaging
            # so the first migration and a fresh checkout produce identical ZIPs.
            if file.suffix == ".json": data = data.replace(b"\r\n", b"\n")
            info = zipfile.ZipInfo(file.relative_to(directory).as_posix(), (2000, 1, 1, 0, 0, 0))
            info.compress_type = zipfile.ZIP_DEFLATED
            archive.writestr(info, data)


def install(output: Path, unity: Path) -> dict:
    plugins = b.ROOT / "Assets/SCUnity/Plugins/Generated"
    supplied = mono.UNITY_FRAMEWORK_ASSEMBLIES | {p.name for p in foundation.framework_path(unity).rglob("*.dll")}
    files = {}
    for dll in sorted(output.glob("*.dll")):
        if dll.name in supplied | desktop.NATIVE | {"Dependencies.dll"}: continue
        if dll.name in CORE: raise ValueError("Core must compile inside Unity: " + dll.name)
        meta = plugins / (dll.name + ".meta")
        if not meta.exists(): raise ValueError("Review importer for new dependency: " + dll.name)
        target = plugins / dll.name
        shutil.copyfile(dll, target)
        files[target.relative_to(b.ROOT).as_posix()] = b.digest(target.read_bytes())
    # Explicit migration cleanup: only these three known generated core files.
    for name in CORE:
        (plugins / name).unlink(missing_ok=True)
    unexpected = {p.name for p in plugins.glob("*.dll")} - {Path(p).name for p in files}
    if unexpected: raise ValueError("Review unexpected plugins: " + str(sorted(unexpected)))
    content = b.ROOT / "Assets/StreamingAssets/Survivalcraft"
    content.mkdir(parents=True, exist_ok=True)
    pack_content(b.UPSTREAM / "Survivalcraft/Content", content / "Content.zip")
    shutil.copyfile(b.UPSTREAM / "Survivalcraft/init.js", content / "init.js")
    for name in ("Content.zip", "init.js"):
        file = content / name; files[file.relative_to(b.ROOT).as_posix()] = b.digest(file.read_bytes())
    b.write_json(b.PORT / ".artifacts/source-install.json", {"files": files, "coreCompiler": "Unity Editor"})
    return files


def audit_api(workspace: Path) -> dict:
    original = workspace / "desktop-api.txt"
    original.write_bytes(gzip.decompress((b.PORT / "Tests/Baselines/unity-source/desktop-api.txt.gz").read_bytes()))
    target = workspace / "unity-api.txt"
    tool = b.PORT / "Build/ApiSnapshot"
    b.run(["dotnet", "build", tool / "ApiSnapshot.csproj", "-c", "Release"], log=workspace / "api-tool.log")
    assemblies = workspace / "main-project/Library/ScriptAssemblies"
    paths = [assemblies / (name + ".dll") for name in b.ASSEMBLIES]
    b.run(["dotnet", tool / "bin/Release/net10.0/ApiSnapshot.dll", target, *paths], log=workspace / "api-snapshot.log")
    result = source_api.compare(original, target)
    result["unityAssemblies"] = {p.name: b.digest(p.read_bytes()) for p in paths}
    result["sourceCompilation"] = (workspace / "player/source-compilation.txt").read_text(encoding="utf-8-sig")
    b.write_json(workspace / "source-api.json", result)
    return result


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--unity", type=Path, required=True)
    parser.add_argument("--install", action="store_true")
    parser.add_argument("--validate", action="store_true")
    parser.add_argument("--validate-installed", action="store_true")
    parser.add_argument("--refresh-package-lock", action="store_true")
    args = parser.parse_args()
    os.environ["DOTNET_CLI_UI_LANGUAGE"] = "en"
    workspace = Path(tempfile.mkdtemp(prefix="source-", dir=b.PORT / ".artifacts"))
    print("Evidence: " + str(workspace), flush=True)
    if not args.validate_installed:
        output = dependencies(workspace, args.unity, args.refresh_package_lock)
        if args.install: install(output, args.unity)
    if args.validate or args.validate_installed:
        if not (args.install or args.validate_installed): raise ValueError("Validation requires installed dependencies")
        desktop.validate(workspace, args.unity, audio_test=True, world_test=True, community_test=True)
        audit_api(workspace)
    print("Completed. " + str(workspace), flush=True)


if __name__ == "__main__":
    main()
