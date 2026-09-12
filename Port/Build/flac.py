"""Retarget the original FLAC dependency and compare all game FLAC assets in Unity Mono."""
from collections import Counter
from pathlib import Path
import argparse
import os
import shutil
import tempfile
import zipfile

import baseline as b
import foundation as foundation
import foundation_api as api
import mono_probe as mono

DEPENDENCY = b.PORT / "Dependencies/Flac"
PROJECTS = b.PORT / "Compatibility/Flac"
EXPECTED_CHECKS = {"full-pcm-decode", "known-integer-pcm", "format-length-and-duration", "seek-behavior-recorded",
                   "chunk-reads-and-buffer-bounds", "stream-disposal", "invalid-inputs-rejected", "sync-prescan-callback", "repeat-construction"}
CORPUS = ("corpus.bin", "repeat.bin", "counts.txt")


def fingerprint(path: Path) -> str:
    return b.digest(path.read_bytes() if path.suffix in (".zip", ".flac", ".pcm", ".bin", ".gz") else path.read_text(encoding="utf-8-sig").encode())


def source_fingerprints() -> dict:
    files = [Path(__file__), b.PORT / "Build/generate_flac_fixtures.py", b.PORT / "Build/foundation.py",
             b.PORT / "Build/foundation_api.py", b.PORT / "Build/mono_probe.py", b.ROOT / "ProjectSettings/ProjectVersion.txt"]
    for folder in (PROJECTS, DEPENDENCY, b.PORT / "Tests/FlacFixtures", b.PORT / "Tests/FlacUnity"):
        files.extend(p for p in folder.rglob("*") if p.is_file() and not ({"bin", "obj"} & set(p.parts)))
    return {p.relative_to(b.ROOT).as_posix(): fingerprint(p) for p in sorted(files)}


def extract_source(destination: Path) -> dict:
    lock = b.read_json(DEPENDENCY / "source-lock.json")
    source = DEPENDENCY / "source.zip"
    if b.digest(source.read_bytes()) != lock["archiveSha256"]:
        raise ValueError("FLAC source archive changed")
    with zipfile.ZipFile(source) as archive:
        files = [e.filename for e in archive.infolist() if not e.is_dir()]
        if len(files) != len(lock["files"]) or set(files) != set(lock["files"]):
            raise ValueError("FLAC source inventory changed")
        for name in files:
            path = (destination / name).resolve()
            if not path.is_relative_to(destination.resolve()) or b.digest(archive.read(name)) != lock["files"][name]:
                raise ValueError("FLAC source bytes/path changed: " + name)
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_bytes(archive.read(name))
    return lock


def prepare_fixtures(workspace: Path) -> dict:
    lock = b.read_json(b.PORT / "upstream-lock.json")
    b.check_upstream(b.SHA); b.check_pinned_files(lock)
    assets = {entry["path"]: entry["sha256"] for entry in b.read_json(b.PORT / "UpstreamFiles.json")["files"]
              if entry["path"].startswith("Survivalcraft/Content/Assets/") and entry["path"].lower().endswith(".flac")}
    if len(assets) != 334:
        raise ValueError("Original FLAC fixture selection changed")
    source = workspace / "game-assets.zip"
    b.run(["git", "-C", b.UPSTREAM, "archive", "--format=zip", f"--output={source}", b.SHA, "Survivalcraft/Content/Assets"])
    manifest = {}
    fixtures = workspace / "fixtures"
    with zipfile.ZipFile(source) as archive:
        for path, sha in assets.items():
            data = archive.read(path)
            if b.digest(data) != sha:
                raise ValueError("Original audio asset changed: " + path)
            relative = "upstream/" + path.removeprefix("Survivalcraft/Content/Assets/")
            target = fixtures / relative
            target.parent.mkdir(parents=True, exist_ok=True); target.write_bytes(data)
            manifest[relative] = {"source": path, "sha256": sha}
    generated = b.read_json(b.PORT / "Tests/FlacFixtures/fixtures.json")
    for name, case in generated["cases"].items():
        for extension, key in ((".flac", "flacSha256"), (".pcm", "pcmSha256")):
            source = b.PORT / "Tests/FlacFixtures" / (name + extension)
            if b.digest(source.read_bytes()) != case[key]:
                raise ValueError("Known PCM/FLAC fixture changed")
            relative = "synthetic/" + source.name
            (fixtures / "synthetic").mkdir(exist_ok=True)
            shutil.copyfile(source, fixtures / relative)
            manifest[relative] = {"source": source.relative_to(b.ROOT).as_posix(), "sha256": case[key]}
    b.write_json(workspace / "fixtures.json", manifest)
    return manifest


def build(workspace: Path, unity: Path, locked: bool = True) -> Path:
    extract_source(workspace / "source")
    projects = workspace / "projects"
    shutil.copytree(PROJECTS, projects, ignore=shutil.ignore_patterns("bin", "obj"))
    args = [f"-p:FlacSources={workspace / 'source'}", f"-p:FrameworkPathOverride={foundation.framework_path(unity)}",
            f"-p:PathMap={workspace}=/scunity-flac"]
    runner = projects / "Runner/Runner.csproj"
    b.run(["dotnet", "restore", runner, *(["--locked-mode"] if locked else []), "--configfile", b.PORT / "Build/NuGet.Config",
           "-p:NuGetAudit=false", *args], log=workspace / "restore.log")
    b.run(["dotnet", "build", runner, "-c", "Release", "--no-restore", *args], log=workspace / "build.log")
    if locked:
        for name in ("Decoder", "Runner"):
            b.assert_equal(PROJECTS / name / "packages.lock.json", projects / name / "packages.lock.json")
    return projects


def oracle(workspace: Path, original: Path) -> Path:
    lock = b.read_json(DEPENDENCY / "source-lock.json")
    if b.digest((original / "NAudio.Flac.dll").read_bytes()) != lock["originalPackage"]["assemblySha256"]:
        raise ValueError("Original FLAC package DLL differs from pinned 1.0.4")
    projects = workspace / "oracle-projects"
    shutil.copytree(PROJECTS, projects, ignore=shutil.ignore_patterns("bin", "obj", "packages.lock.json"))
    b.run(["dotnet", "build", projects / "Oracle/Oracle.csproj", "-c", "Release", "-p:FlacTargetFramework=net10.0-windows",
           f"-p:OriginalAssemblies={original}", "-p:RestorePackagesWithLockFile=false", "-p:NuGetAudit=false",
           "--configfile", b.PORT / "Build/NuGet.Config"], log=workspace / "oracle-build.log")
    output = projects / "Oracle/bin/Release/net10.0-windows"
    for name in ("NAudio.Flac.dll", "NAudio.Core.dll"):
        shutil.copyfile(original / name, output / name)
    corpus = workspace / "oracle-corpus"
    mono.execute(["dotnet", output / "SCUnity.Flac.Oracle.dll", workspace / "fixtures", corpus], workspace / "oracle.log", 300)
    return corpus


def normalize(line: str) -> str:
    line = api.normalize(line)
    if line.startswith("attribute NAudio.Flac [BCL]System.Runtime.Versioning.TargetFrameworkAttribute::"):
        prefix, value = line.rsplit(" value=", 1)
        if value not in api.TFM_VALUES:
            raise ValueError("Unregistered FLAC target framework")
        return prefix + " value=<reviewed-net10-to-net48>"
    if line.startswith("security NAudio.Flac action=RequestMinimum permissions="):
        prefix, value = line.rsplit(" permissions=", 1)
        data = bytes.fromhex(value)
        name = "System.Security.Permissions.SecurityPermissionAttribute"
        for scope in (api.CORE10, api.CORE48):
            data = data.replace(api.serialized_string(name + ", " + scope), api.serialized_string(name + ", [BCL]"))
        return prefix + " permissions=" + data.hex().upper()
    return line


def compare_api(original: list[str], candidate: list[str]) -> dict:
    left = Counter(normalize(s) for s in original if not s.startswith("#"))
    right = Counter(normalize(s) for s in candidate if not s.startswith("#"))
    if not left or left != right:
        raise ValueError("FLAC API drift:\n" + "\n".join(["- " + s for s in left-right] + ["+ " + s for s in right-left])[:10000])
    return {"types": sum(s.startswith("type ") for s in original), "records": left.total(), "unregisteredDifferences": 0}


def verify_api(workspace: Path, projects: Path, original: Path) -> dict:
    tool = b.PORT / "Build/ApiSnapshot"
    b.run(["dotnet", "build", tool / "ApiSnapshot.csproj", "-c", "Release", "--configfile", b.PORT / "Build/NuGet.Config"], log=workspace / "api-tool.log")
    for name, dll in (("original-api", original / "NAudio.Flac.dll"), ("candidate-api", projects / "Runner/bin/Release/net48/NAudio.Flac.dll")):
        b.run(["dotnet", tool / "bin/Release/net10.0/ApiSnapshot.dll", workspace / (name + ".txt"), dll], log=workspace / (name + ".log"))
    result = compare_api((workspace / "original-api.txt").read_text(encoding="utf-8-sig").splitlines(),
                         (workspace / "candidate-api.txt").read_text(encoding="utf-8-sig").splitlines())
    b.write_json(workspace / "api-comparison.json", result)
    return result


def compare_corpus(expected: Path, actual: Path) -> dict:
    result = {}
    for name in CORPUS:
        left, right = (expected / name).read_bytes(), (actual / name).read_bytes()
        if not left or left != right:
            offset = next((i for i, (a, c) in enumerate(zip(left, right)) if a != c), min(len(left), len(right)))
            raise ValueError(f"FLAC corpus differs: {name}, byte {offset}, lengths {len(left)}/{len(right)}")
        result[name] = {"bytes": len(left), "sha256": b.digest(left)}
    if not (actual / "counts.txt").read_text().startswith("files=337\nsynthetic=3\n"):
        raise ValueError("FLAC corpus must include all 334 original assets and three known-PCM samples")
    result["durationComparison"] = compare_durations(expected / "durations.tsv", actual / "durations.tsv")
    return result


def compare_durations(original: Path, candidate: Path) -> dict:
    def read(path: Path) -> dict:
        rows = [line.split("\t") for line in path.read_text(encoding="utf-8-sig").splitlines()]
        values = {name: int(ticks) for name, ticks in rows}
        if len(rows) != 337 or len(values) != 337 or any(v <= 0 for v in values.values()):
            raise ValueError("Duration observations are incomplete or duplicated")
        return values
    left, right = read(original), read(candidate)
    if left.keys() != right.keys():
        raise ValueError("Duration fixture identities differ")
    differences = []
    for name, ticks in left.items():
        delta = right[name] - ticks
        if right[name] != ((ticks + 5000) // 10000) * 10000 or abs(delta) > 5000:
            raise ValueError("Unregistered duration difference: " + name)
        differences.append(abs(delta))
    return {"policy": "flac-inherited-totaltime-millisecond-rounding", "samples": len(left),
            "differentSamples": sum(d != 0 for d in differences), "maxAbsoluteTicks": max(differences),
            "originalSha256": b.digest(original.read_bytes()), "monoSha256": b.digest(candidate.read_bytes())}


def validate_runtime(runtime: dict, version: str, build: dict, expected: dict) -> None:
    if (set(expected) != EXPECTED_CHECKS or any(v is not True for v in expected.values())
            or runtime.get("schema") != "scunity-flac-v1" or runtime.get("passed") is not True or runtime.get("error")
            or runtime.get("isEditor") is not False or runtime.get("isMono") is not True or runtime.get("pointerSize") != 8
            or runtime.get("platform") != "WindowsPlayer" or runtime.get("unityVersion") != version
            or len(runtime.get("checks", [])) != len(expected) or any(c.get("passed") is not True for c in runtime["checks"])
            or {c["name"]: c["passed"] for c in runtime["checks"]} != expected):
        raise ValueError("Invalid FLAC Player configuration or assertions")
    if build != {"result": "Succeeded", "errors": 0, "warnings": 0, "backend": "Mono2x", "apiCompatibility": "NET_Unity_4_8", "development": False}:
        raise ValueError("Invalid FLAC Player build")


def prepare_unity(workspace: Path, projects: Path) -> tuple[Path, dict]:
    project = workspace / "unity-project"
    shutil.copytree(b.PORT / "Tests/FlacUnity", project / "Assets")
    (project / "ProjectSettings").mkdir()
    shutil.copyfile(b.ROOT / "ProjectSettings/ProjectVersion.txt", project / "ProjectSettings/ProjectVersion.txt")
    b.write_json(project / "Packages/manifest.json", {"dependencies": {"com.unity.modules.jsonserialize": "1.0.0"}})
    plugins = project / "Assets/Plugins/Flac"; plugins.mkdir(parents=True)
    bundle = {}
    for source in sorted((projects / "Runner/bin/Release/net48").glob("*.dll")):
        if source.name in mono.UNITY_FRAMEWORK_ASSEMBLIES:
            continue
        shutil.copyfile(source, plugins / source.name)
        bundle[f"plugins/{source.name}"] = b.digest(source.read_bytes())
    shutil.copytree(workspace / "fixtures", project / "Assets/StreamingAssets/Flac")
    return project, bundle


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--unity", type=Path, required=True)
    parser.add_argument("--baseline", type=Path, required=True)
    args = parser.parse_args(); unity = args.unity.resolve()
    os.environ["DOTNET_CLI_UI_LANGUAGE"] = "en"
    workspace = Path(tempfile.mkdtemp(prefix="flac-", dir=b.PORT / ".artifacts"))
    print(f"Evidence directory: {workspace}", flush=True)
    sources = source_fingerprints()
    if b.run(["dotnet", "--version"]) != b.read_json(b.PORT / "global.json")["sdk"]["version"]:
        raise ValueError("Pinned SDK is required")
    original = foundation.verify_baseline(args.baseline.resolve())
    fixtures = prepare_fixtures(workspace)
    projects = build(workspace, unity)
    print("Unchanged FLAC sources built for net48; running original package against 337 samples...", flush=True)
    expected = oracle(workspace, original)
    comparison = verify_api(workspace, projects, original)
    project, bundle = prepare_unity(workspace, projects)
    player = workspace / "player/SCUnity.Flac.exe"; player.parent.mkdir()
    print("Original package passed; building isolated Unity Mono Player...", flush=True)
    for method, extra, timeout in (("Configure", [], 600), ("Build", ["-portProbePlayer", player], 1200)):
        mono.execute([unity, "-batchmode", "-nographics", "-projectPath", project, "-buildTarget", "Win64",
                      "-executeMethod", "SCUnity.Validation.BuildFlac." + method, *extra, "-logFile", workspace / ("unity-" + method.lower() + ".log")],
                     workspace / (method + "-process.log"), timeout)
    actual = workspace / "mono-corpus"
    mono.execute([player, "-batchmode", "-nographics", "-flacOutput", actual, "-portProbeResult", workspace / "runtime-result.json",
                  "-logFile", workspace / "player.log"], workspace / "player-process.log", 300)
    version = (b.ROOT / "ProjectSettings/ProjectVersion.txt").read_text().splitlines()[0].split(": ")[1]
    checks = b.read_json(expected / "checks.json")
    validate_runtime(b.read_json(workspace / "runtime-result.json"), version, b.read_json(player.parent / "build-summary.json"), checks)
    corpus = compare_corpus(expected, actual)
    managed = mono.verify_player_bundle(player, bundle)
    for relative, record in fixtures.items():
        if b.digest((player.parent / "SCUnity.Flac_Data/StreamingAssets/Flac" / relative).read_bytes()) != record["sha256"]:
            raise ValueError("Player fixture changed: " + relative)
    print("Unity PCM and behavior comparison passed; checking independent rebuild...", flush=True)
    repeat = workspace / "repeat"; repeat.mkdir()
    repeated = build(repeat, unity)
    reproducible = {}
    for name in ("NAudio.Flac.dll", "SCUnity.Flac.Runner.dll"):
        data = (projects / "Runner/bin/Release/net48" / name).read_bytes()
        if data != (repeated / "Runner/bin/Release/net48" / name).read_bytes():
            raise ValueError("Independent rebuild differs: " + name)
        reproducible[name] = b.digest(data)
    if source_fingerprints() != sources:
        raise ValueError("Sources changed during FLAC validation")
    b.write_json(workspace / "provenance.json", {
        "schemaVersion": 1, "upstreamCommit": b.SHA, "sources": sources, "unityVersion": version,
        "dotnetSdk": b.run(["dotnet", "--version"]), "unityEditorSha256": b.digest(unity.read_bytes()),
        "unityReferenceAssemblies": {p.relative_to(foundation.framework_path(unity)).as_posix(): b.digest(p.read_bytes())
                                     for p in sorted(foundation.framework_path(unity).rglob("*.dll"))},
        "originalAssemblies": {name: b.digest((original / name).read_bytes()) for name in ("NAudio.Flac.dll", "NAudio.Core.dll")},
        "bundle": bundle, "runtimeAssemblies": managed, "fixtures": fixtures, "corpus": corpus,
        "checks": checks, "api": comparison, "reproducibleDlls": reproducible,
        "playerSha256": b.digest(player.read_bytes()),
        "evidenceFiles": {p: b.digest((workspace / p).read_bytes()) for p in ("runtime-result.json", "player/build-summary.json",
            "original-api.txt", "candidate-api.txt", "api-comparison.json", "fixtures.json", "oracle-corpus/checks.json")},
        "scope": "NAudio.Flac 1.0.4 dependency retarget; no Engine audio wrappers, mixer, Unity Audio output or full game validation"})
    b.check_upstream(b.SHA)
    print("PASS: 337 FLAC files; PCM/seek/read/error corpus byte-identical; full dependency API preserved; independent rebuild identical.", flush=True)


if __name__ == "__main__":
    main()
