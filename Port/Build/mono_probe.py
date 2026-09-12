"""Build external net48 fixtures and execute their assertions in a real Unity Mono Player."""
from __future__ import annotations

import argparse
import os
from pathlib import Path
import shutil
import subprocess
import tempfile

import baseline as b
import generate_compatibility as generator


UNITY_FRAMEWORK_ASSEMBLIES = {"System.Buffers.dll", "System.Memory.dll", "System.Numerics.Vectors.dll", "System.ValueTuple.dll"}


def source_fingerprints() -> dict:
    tracked = [b.PORT / "Build/mono_probe.py", b.PORT / "Build/generate_compatibility.py",
               b.PORT / "Compatibility/Profiles/mono-probe.json", b.ROOT / "ProjectSettings/ProjectVersion.txt"]
    for folder in (b.PORT / "Compatibility/MonoProbe", b.PORT / "Tests/UnityMono"):
        tracked.extend(p for p in folder.rglob("*") if p.is_file() and not ({"bin", "obj"} & set(p.parts)))
    return {p.relative_to(b.ROOT).as_posix(): b.digest(p.read_text(encoding="utf-8-sig").encode()) for p in sorted(tracked)}


def prepare(workspace: Path) -> tuple[Path, Path]:
    generated = workspace / "generated"
    generator.generate(b.PORT / "Compatibility/Profiles/mono-probe.json", generated)
    projects = workspace / "projects"
    shutil.copytree(b.PORT / "Compatibility/MonoProbe", projects,
                    ignore=shutil.ignore_patterns("bin", "obj"))
    return generated, projects


def build_fixtures(workspace: Path, generated: Path, projects: Path) -> Path:
    os.environ["DOTNET_CLI_UI_LANGUAGE"] = "en"
    locks = {name: (projects / name / "packages.lock.json").read_bytes()
             for name in ("Runner", "Plugin", "Contracts", "Dependency", "Upstream")}
    for name in ("Runner", "Plugin"):
        project = projects / name / f"{name}.csproj"
        b.run(["dotnet", "restore", project, "--locked-mode", "--configfile", b.PORT / "Build/NuGet.Config",
               "-p:NuGetAudit=false", f"-p:GeneratedSources={generated}"], log=workspace / f"{name}-restore.log")
        b.run(["dotnet", "build", project, "-c", "Release", "--no-restore",
               f"-p:GeneratedSources={generated}", f"-p:PathMap={workspace}=/scunity-mono-probe"],
              log=workspace / f"{name}-build.log")
    if any((projects / name / "packages.lock.json").read_bytes() != data for name, data in locks.items()):
        raise ValueError("Dependency lock changed during locked restore/build")
    return projects / "Runner/bin/Release/net48"


def prepare_unity(workspace: Path, projects: Path, runner: Path) -> tuple[Path, dict]:
    unity = workspace / "unity-project"
    shutil.copytree(b.PORT / "Tests/UnityMono/Runtime", unity / "Assets/Runtime")
    shutil.copytree(b.PORT / "Tests/UnityMono/Editor", unity / "Assets/Editor")
    settings = unity / "ProjectSettings"
    settings.mkdir()
    shutil.copyfile(b.ROOT / "ProjectSettings/ProjectVersion.txt", settings / "ProjectVersion.txt")
    b.write_json(unity / "Packages/manifest.json", {"dependencies": {"com.unity.modules.jsonserialize": "1.0.0"}})
    plugins = unity / "Assets/Plugins/Probe"
    plugins.mkdir(parents=True)
    payload = unity / "Assets/StreamingAssets/Probe"
    payload.mkdir(parents=True)
    bundle = {}
    for source in sorted(runner.glob("*.dll")):
        # Unity owns these framework assemblies/type forwards. Do not import the
        # NuGet copies alongside Unity's BCL. Unsafe is a separate runtime dependency.
        if source.name in UNITY_FRAMEWORK_ASSEMBLIES:
            continue
        shutil.copyfile(source, plugins / source.name)
        bundle[f"plugins/{source.name}"] = b.digest(source.read_bytes())
    for name in ("Plugin", "Dependency"):
        source = projects / name / f"bin/Release/net48/SCUnity.Probe.{name}.dll"
        shutil.copyfile(source, payload / (source.name + ".bytes"))
        bundle[f"payload/{source.name}.bytes"] = b.digest(source.read_bytes())
    b.write_json(workspace / "bundle-manifest.json", bundle)
    return unity, bundle


def execute(command: list[str | Path], log: Path, timeout: int, expected_exit: int = 0) -> None:
    # Never use a shell; the original project and existing Editor are untouched.
    with log.open("w", encoding="utf-8") as stream:
        result = subprocess.run([str(c) for c in command], cwd=b.PORT, stdout=stream,
                                stderr=subprocess.STDOUT, timeout=timeout,
                                creationflags=subprocess.CREATE_NO_WINDOW if os.name == "nt" else 0)
    if result.returncode != expected_exit:
        raise RuntimeError(f"Process failed ({result.returncode}); see {log}")


EXPECTED_CHECKS = {
    "payload-not-preloaded", "assembly-load-bytes", "assembly-load-event", "assembly-enumeration",
    "derived-type-discovery", "dynamic-initialization", "assembly-resolve-bytes", "dynamic-type-lookup",
    "jint-evaluate", "jint-host-callback", "jint-clr-interop", "jint-repeated-callback",
    "upstream-readonly-enumeration", "upstream-readonly-mutation-rejected", "upstream-collection-select",
    "upstream-state-machine-order", "upstream-state-machine-history",
    "harmony-constructor", "harmony-virtual", "harmony-closed-generic", "harmony-upstream-game-method",
    "harmony-unpatch-constructor", "harmony-unpatch-virtual", "harmony-unpatch-generic", "harmony-unpatch-game-method"
}


def validate_result(result: dict, version: str, build: dict) -> None:
    if (result.get("schema") != "scunity-mono-probe-v1" or result.get("passed") is not True
            or result.get("error") or result.get("isEditor") is not False
            or result.get("isMono") is not True or result.get("platform") != "WindowsPlayer"
            or result.get("pointerSize") != 8 or result.get("unityVersion") != version):
        raise ValueError("Runtime result is not a passing pinned Windows x64 Unity Mono Player run")
    checks = result["checks"]
    if (len(checks) != len(EXPECTED_CHECKS) or {c["name"] for c in checks} != EXPECTED_CHECKS
            or any(c["passed"] is not True for c in checks)):
        raise ValueError("Runtime assertions are missing, duplicated, unexpected, or failing")
    if (build.get("result") != "Succeeded" or build.get("errors") != 0
            or build.get("warnings") != 0
            or build.get("backend") != "Mono2x" or build.get("apiCompatibility") != "NET_Unity_4_8"
            or build.get("development") is not False):
        raise ValueError("Player build configuration/result is not acceptable")


def verify_player_bundle(player: Path, bundle: dict) -> dict:
    data = player.parent / (player.stem + "_Data")
    for path, expected in bundle.items():
        category, name = path.split("/")
        target = data / ("Managed" if category == "plugins" else "StreamingAssets/Probe") / name
        if not target.is_file() or b.digest(target.read_bytes()) != expected:
            raise ValueError(f"Player bundle changed or missing: {path}")
    return {p.name: b.digest(p.read_bytes()) for p in sorted((data / "Managed").glob("*.dll"))}


def validate_negative(result: dict) -> None:
    if (result.get("passed") is not False or result.get("isEditor") is not False
            or result.get("isMono") is not True or "SCUnity.Probe.Dependency" not in result.get("error", "")
            or "FileNotFoundException" not in result.get("error", "")):
        raise ValueError("Missing dependency was not rejected with the expected error in Player")


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--unity", type=Path, default=os.environ.get("UNITY_EDITOR"), help="Pinned Unity Editor executable, or UNITY_EDITOR environment variable")
    args = parser.parse_args()
    if not args.unity or not args.unity.is_file():
        parser.error("Provide the installed Unity Editor executable with --unity or UNITY_EDITOR")
    artifacts = b.PORT / ".artifacts"
    artifacts.mkdir(exist_ok=True)
    workspace = Path(tempfile.mkdtemp(prefix="mono-probe-", dir=artifacts))
    print(f"Evidence directory: {workspace}", flush=True)
    sources = source_fingerprints()
    generated, projects = prepare(workspace)
    runner = build_fixtures(workspace, generated, projects)
    unity, bundle = prepare_unity(workspace, projects, runner)
    player = workspace / "player/SCUnity.MonoProbe.exe"
    player.parent.mkdir()
    print("net48 fixtures built; importing and building isolated Unity Player...", flush=True)
    execute([args.unity, "-batchmode", "-nographics", "-projectPath", unity, "-buildTarget", "Win64",
             "-executeMethod", "SCUnity.Validation.BuildMonoProbe.Configure",
             "-logFile", workspace / "unity-configure.log"], workspace / "unity-configure-process.log", 600)
    execute([args.unity, "-batchmode", "-nographics", "-projectPath", unity, "-buildTarget", "Win64",
             "-executeMethod", "SCUnity.Validation.BuildMonoProbe.Build", "-portProbePlayer", player,
             "-logFile", workspace / "unity-build.log"], workspace / "unity-process.log", 1200)
    print("Unity Player built; executing runtime assertions...", flush=True)
    result_path = workspace / "runtime-result.json"
    execute([player, "-batchmode", "-nographics", "-portProbeResult", result_path,
             "-logFile", workspace / "player.log"], workspace / "player-process.log", 180)
    result = b.read_json(result_path)
    build = b.read_json(player.parent / "build-summary.json")
    version = (b.ROOT / "ProjectSettings/ProjectVersion.txt").read_text().splitlines()[0].split(": ")[1]
    validate_result(result, version, build)
    runtime_assemblies = verify_player_bundle(player, bundle)
    print("Positive assertions passed; checking missing dependency failure...", flush=True)
    negative_payload = workspace / "negative-payload"
    negative_payload.mkdir()
    shutil.copyfile(projects / "Plugin/bin/Release/net48/SCUnity.Probe.Plugin.dll", negative_payload / "SCUnity.Probe.Plugin.dll.bytes")
    negative_path = workspace / "negative-result.json"
    execute([player, "-batchmode", "-nographics", "-portProbePayload", negative_payload,
             "-portProbeResult", negative_path, "-logFile", workspace / "negative-player.log"],
            workspace / "negative-process.log", 180, expected_exit=1)
    validate_negative(b.read_json(negative_path))
    if source_fingerprints() != sources:
        raise ValueError("Probe sources changed during validation; run again against stable inputs")
    b.write_json(workspace / "provenance.json", {
        "upstreamCommit": b.SHA, "dotnetSdk": b.run(["dotnet", "--version"]),
        "unityVersion": version, "sources": sources,
        "bundle": bundle, "playerSha256": b.digest(player.read_bytes()),
        "unityProvidedFrameworkAssemblies": sorted(UNITY_FRAMEWORK_ASSEMBLIES),
        "runtimeAssemblies": runtime_assemblies,
        "runtimeResultSha256": b.digest(result_path.read_bytes()),
        "negativeResultSha256": b.digest(negative_path.read_bytes()),
        "buildSummarySha256": b.digest((player.parent / "build-summary.json").read_bytes()),
        "generationManifestSha256": b.digest((generated / "GenerationManifest.json").read_bytes()),
        "scope": "capability probe; 3 unchanged upstream sources; not the three full game assemblies or ModLoader"})
    b.check_upstream(b.SHA)
    print(f"PASS: {len(result['checks'])} assertions in Windows x64 Unity {version} Mono Player.", flush=True)


if __name__ == "__main__":
    main()
