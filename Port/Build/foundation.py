"""Build selected Engine sources and complete EntitySystem; compare original .NET 10 with Unity Mono."""
from __future__ import annotations

import argparse
import os
from pathlib import Path
import shutil
import tempfile

import baseline as b
import generate_compatibility as generator
import mono_probe as mono

PROFILE = b.PORT / "Compatibility/Profiles/foundation.json"
PROJECTS = b.PORT / "Compatibility/Foundation"
CORPUS = ("math.bin", "rounding-edges.bin", "archive.bin", "values.xml", "object-info.txt")
EXPECTED_CHECKS = {
    "default-interface-method", "math-corpus-completed", "matrix-column-layout", "typecache-payload-not-preloaded",
    "typecache-assembly-load-rescan", "typecache-short-names", "typecache-cache-identity", "typecache-required-missing-rejected",
    "entity-component-reflection-load", "entity-add-id-and-find", "entity-component-save", "entity-remove-and-dispose",
    "entity-event-order", "binary-integer-boundaries", "binary-string-interning", "binary-serializer-discovery",
    "binary-consumed-exactly", "values-xml-roundtrip", "human-readable-null-guard", "human-readable-invalid-input",
    "unity-dictionary-apis", "default-object-info-behavior-recorded",
}


def source_fingerprints() -> dict:
    files = [Path(__file__), b.PORT / "Build/foundation_api.py", b.PORT / "Build/generate_compatibility.py",
             b.PORT / "Build/mono_probe.py", PROFILE, b.ROOT / "ProjectSettings/ProjectVersion.txt"]
    for folder in (PROJECTS, b.PORT / "Compatibility/Support", b.PORT / "Tests/FoundationUnity"):
        files.extend(p for p in folder.rglob("*") if p.is_file() and not ({"bin", "obj"} & set(p.parts)))
    return {p.relative_to(b.ROOT).as_posix(): b.digest(p.read_text(encoding="utf-8-sig").encode()) for p in sorted(files)}


def framework_path(unity: Path) -> Path:
    path = unity.parent / "Data/UnityReferenceAssemblies/unity-4.8-api"
    if not (path / "mscorlib.dll").is_file():
        raise ValueError("Pinned Unity .NET Framework reference assemblies are required")
    return path


def prepare(workspace: Path) -> Path:
    generator.generate(PROFILE, workspace / "generated")
    shutil.copytree(PROJECTS, workspace / "projects", ignore=shutil.ignore_patterns("bin", "obj"))
    shutil.copytree(b.PORT / "Compatibility/Support", workspace / "support")
    return workspace / "projects"


def build(workspace: Path, unity: Path, locked: bool = True) -> Path:
    projects = prepare(workspace)
    args = [f"-p:GeneratedSources={workspace / 'generated'}", f"-p:CompatibilitySupport={workspace / 'support'}",
            f"-p:FrameworkPathOverride={framework_path(unity)}", f"-p:PathMap={workspace}=/scunity-foundation"]
    project = projects / "Payload/Payload.csproj"
    b.run(["dotnet", "restore", project, *(["--locked-mode"] if locked else []),
           "--configfile", b.PORT / "Build/NuGet.Config", "-p:NuGetAudit=false", *args], log=workspace / "restore.log")
    b.run(["dotnet", "build", project, "-c", "Release", "--no-restore", *args], log=workspace / "build.log")
    if locked:
        for name in ("Engine.Core", "EntitySystem", "Runner", "Payload"):
            b.assert_equal(PROJECTS / name / "packages.lock.json", projects / name / "packages.lock.json")
    return projects


def verify_baseline(path: Path) -> Path:
    result = b.read_json(path / "result.json")
    if result.get("result") != "passed" or result.get("mode") != "verify" or result.get("upstreamCommit") != b.SHA:
        raise ValueError("A passing fixed-upstream baseline verify build is required")
    originals = path / "upstream/Survivalcraft.Windows/bin/Release/net10.0-windows/win-x64"
    for assembly in result["assemblies"]:
        if b.digest((originals / assembly["name"]).read_bytes()) != assembly["sha256"]:
            raise ValueError("Original assembly changed after baseline verification")
    if {a["name"] for a in result["assemblies"]} != {"Engine.dll", "EntitySystem.dll", "Survivalcraft.dll"}:
        raise ValueError("Incomplete original baseline")
    return originals


def oracle(workspace: Path, originals: Path) -> Path:
    projects = workspace / "oracle-projects"
    shutil.copytree(PROJECTS, projects, ignore=shutil.ignore_patterns("bin", "obj", "packages.lock.json"))
    args = ["-p:FoundationTargetFramework=net10.0-windows", f"-p:OriginalAssemblies={originals}",
            "-p:RestorePackagesWithLockFile=false", "-p:NuGetAudit=false"]
    for name in ("Oracle", "Payload"):
        b.run(["dotnet", "build", projects / name / f"{name}.csproj", "-c", "Release", *args,
               "--configfile", b.PORT / "Build/NuGet.Config"], log=workspace / f"oracle-{name}-build.log")
    output = projects / "Oracle/bin/Release/net10.0-windows"
    # Full Engine's reflection scan needs its original managed dependency closure.
    for source in originals.glob("*.dll"):
        shutil.copyfile(source, output / source.name)
    result = workspace / "oracle-corpus"
    mono.execute(["dotnet", output / "SCUnity.Foundation.Oracle.dll", result,
                  projects / "Payload/bin/Release/net10.0-windows/SCUnity.Foundation.Payload.dll"],
                 workspace / "oracle.log", 180)
    return result


def prepare_unity(workspace: Path, projects: Path) -> tuple[Path, dict]:
    unity = workspace / "unity-project"
    shutil.copytree(b.PORT / "Tests/FoundationUnity/Runtime", unity / "Assets/Runtime")
    shutil.copytree(b.PORT / "Tests/FoundationUnity/Editor", unity / "Assets/Editor")
    (unity / "ProjectSettings").mkdir()
    shutil.copyfile(b.ROOT / "ProjectSettings/ProjectVersion.txt", unity / "ProjectSettings/ProjectVersion.txt")
    b.write_json(unity / "Packages/manifest.json", {"dependencies": {"com.unity.modules.jsonserialize": "1.0.0"}})
    plugins = unity / "Assets/Plugins/Foundation"
    plugins.mkdir(parents=True)
    payload = unity / "Assets/StreamingAssets/Probe"
    payload.mkdir(parents=True)
    bundle = {}
    for source in sorted((projects / "Runner/bin/Release/net48").glob("*.dll")):
        if source.name not in mono.UNITY_FRAMEWORK_ASSEMBLIES:
            shutil.copyfile(source, plugins / source.name)
            bundle[f"plugins/{source.name}"] = b.digest(source.read_bytes())
    source = projects / "Payload/bin/Release/net48/SCUnity.Foundation.Payload.dll"
    shutil.copyfile(source, payload / (source.name + ".bytes"))
    bundle[f"payload/{source.name}.bytes"] = b.digest(source.read_bytes())
    return unity, bundle


def compare_corpus(expected: Path, actual: Path) -> dict:
    result = {}
    for name in CORPUS:
        left, right = (expected / name).read_bytes(), (actual / name).read_bytes()
        if not left or left != right:
            first = next((i for i, (a, c) in enumerate(zip(left, right)) if a != c), min(len(left), len(right)))
            raise ValueError(f"Original/Mono corpus differs: {name}, first byte {first}, sizes {len(left)}/{len(right)}")
        result[name] = {"bytes": len(left), "sha256": b.digest(left)}
    return result


def validate_runtime(runtime: dict, version: str, build_result: dict, expected_checks: dict) -> None:
    if (set(expected_checks) != EXPECTED_CHECKS or any(v is not True for v in expected_checks.values())
            or runtime.get("schema") != "scunity-foundation-v1" or runtime.get("passed") is not True or runtime.get("error")
            or runtime.get("isEditor") is not False or runtime.get("isMono") is not True
            or runtime.get("pointerSize") != 8 or runtime.get("platform") != "WindowsPlayer" or runtime.get("unityVersion") != version
            or len(runtime.get("checks", [])) != len(expected_checks)
            or any(c.get("passed") is not True for c in runtime["checks"])
            or {c["name"]: c["passed"] for c in runtime["checks"]} != expected_checks):
        raise ValueError("Unity Mono runtime configuration/assertions differ from passing original oracle")
    if build_result != {"result": "Succeeded", "errors": 0, "warnings": 0, "backend": "Mono2x",
                        "apiCompatibility": "NET_Unity_4_8", "development": False}:
        raise ValueError("Unexpected Player build configuration/result")


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--unity", required=True, type=Path)
    parser.add_argument("--baseline", required=True, type=Path)
    args = parser.parse_args()
    args.unity = args.unity.resolve()
    args.baseline = args.baseline.resolve()
    os.environ["DOTNET_CLI_UI_LANGUAGE"] = "en"
    workspace = Path(tempfile.mkdtemp(prefix="foundation-", dir=b.PORT / ".artifacts"))
    print(f"Evidence directory: {workspace}", flush=True)
    sources = source_fingerprints()
    if b.run(["dotnet", "--version"]) != b.read_json(b.PORT / "global.json")["sdk"]["version"]:
        raise ValueError("Pinned .NET SDK is required")
    originals = verify_baseline(args.baseline)
    projects = build(workspace, args.unity)
    print("net48 foundation built; validating original-assembly oracle...", flush=True)
    expected = oracle(workspace, originals)
    import foundation_api
    api = foundation_api.verify(workspace, projects, originals)
    project, bundle = prepare_unity(workspace, projects)
    player = workspace / "player/SCUnity.Foundation.exe"
    player.parent.mkdir()
    print("Building isolated Windows x64 Unity Mono Player...", flush=True)
    mono.execute([args.unity, "-batchmode", "-nographics", "-projectPath", project, "-buildTarget", "Win64",
                  "-executeMethod", "SCUnity.Validation.BuildFoundation.Configure", "-logFile", workspace / "unity-configure.log"],
                 workspace / "unity-configure-process.log", 600)
    mono.execute([args.unity, "-batchmode", "-nographics", "-projectPath", project, "-buildTarget", "Win64",
                  "-executeMethod", "SCUnity.Validation.BuildFoundation.Build", "-portProbePlayer", player,
                  "-logFile", workspace / "unity-build.log"], workspace / "unity-build-process.log", 1200)
    actual = workspace / "mono-corpus"
    mono.execute([player, "-batchmode", "-nographics", "-foundationCorpus", actual,
                  "-portProbeResult", workspace / "runtime-result.json", "-logFile", workspace / "player.log"],
                 workspace / "player-process.log", 180)
    runtime = b.read_json(workspace / "runtime-result.json")
    build_result = b.read_json(player.parent / "build-summary.json")
    version = (b.ROOT / "ProjectSettings/ProjectVersion.txt").read_text().splitlines()[0].split(": ")[1]
    expected_checks = b.read_json(expected / "checks.json")
    validate_runtime(runtime, version, build_result, expected_checks)
    corpus = compare_corpus(expected, actual)
    managed = mono.verify_player_bundle(player, bundle)
    print("Runtime and original-assembly comparison passed; checking independent rebuild...", flush=True)
    repeat = workspace / "repeat"
    repeat.mkdir()
    repeated = build(repeat, args.unity)
    if (workspace / "generated/GenerationManifest.json").read_bytes() != (repeat / "generated/GenerationManifest.json").read_bytes():
        raise ValueError("Independent source generation differs")
    for name in ("Engine.dll", "EntitySystem.dll", "SCUnity.Foundation.Runner.dll", "SCUnity.Foundation.Payload.dll"):
        if (projects / "Payload/bin/Release/net48" / name).read_bytes() != (repeated / "Payload/bin/Release/net48" / name).read_bytes():
            raise ValueError(f"Independent rebuild differs: {name}")
    if source_fingerprints() != sources:
        raise ValueError("Foundation sources changed during validation")
    refs = framework_path(args.unity)
    b.write_json(workspace / "provenance.json", {
        "schemaVersion": 1, "upstreamCommit": b.SHA, "sources": sources, "dotnetSdk": b.run(["dotnet", "--version"]),
        "unityVersion": version, "unityEditorSha256": b.digest(args.unity.read_bytes()),
        "unityReferenceAssemblies": {p.relative_to(refs).as_posix(): b.digest(p.read_bytes()) for p in sorted(refs.rglob("*.dll"))},
        "originalAssemblies": {n: b.digest((originals / n).read_bytes()) for n in ("Engine.dll", "EntitySystem.dll")},
        "bundle": bundle, "runtimeAssemblies": managed, "corpus": corpus, "api": api,
        "playerSha256": b.digest(player.read_bytes()),
        "evidenceFiles": {p: b.digest((workspace / p).read_bytes()) for p in ("runtime-result.json", "player/build-summary.json",
            "oracle-corpus/checks.json", "generated/GenerationManifest.json", "api-comparison.json", "foundation-api.txt")},
        "independentRebuild": "four project assemblies byte-identical", "checks": expected_checks,
        "scope": "selected Engine core and complete EntitySystem; not full Engine, Survivalcraft, ModLoader or graphics"})
    b.check_upstream(b.SHA)
    print(f"PASS: {len(expected_checks)} assertions; five corpora byte-identical; scoped API preserved; independent rebuild identical.", flush=True)


if __name__ == "__main__":
    main()
