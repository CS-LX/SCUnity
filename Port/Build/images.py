"""Replay ImageSharp 3.1.12 for Unity Mono and compare the original package's pixels and codecs."""
from pathlib import Path
from collections import Counter
import argparse
import os
import shutil
import tempfile
import zipfile

import baseline as b
import foundation as foundation
import foundation_api as api
import mono_probe as mono

DEPENDENCY = b.PORT / "Dependencies/ImageSharp"
PROJECTS = b.PORT / "Compatibility/ImageSharp"
CHECKS = {"all-183-images-and-resize", "nine-formats-twelve-encoder-modes", "async-file-save-equals-sync",
          "async-file-load", "invalid-images-rejected", "all-half-bit-patterns-and-100000-floats"}


def sources():
    files = [Path(__file__), b.PORT / "Build/baseline.py", b.PORT / "Build/foundation_api.py", b.PORT / "Build/mono_probe.py"]
    for folder in (PROJECTS, DEPENDENCY, b.PORT / "Tests/ImageFixtures", b.PORT / "Tests/ImageUnity"):
        files += [p for p in folder.rglob("*") if p.is_file() and not ({"obj", "bin"} & set(p.parts))]
    return {p.relative_to(b.ROOT).as_posix(): b.digest(p.read_bytes() if p.suffix in (".zip", ".image") else p.read_text(encoding="utf-8-sig").encode()) for p in sorted(files)}


def extract(destination):
    lock = b.read_json(DEPENDENCY / "source-lock.json")
    source = DEPENDENCY / "source.zip"
    if b.digest(source.read_bytes()) != lock["archiveSha256"]:
        raise ValueError("ImageSharp source archive changed")
    with zipfile.ZipFile(source) as z:
        names = z.namelist()
        if len(names) != len(lock["files"]) or set(names) != set(lock["files"]):
            raise ValueError("ImageSharp source inventory changed")
        data = {}
        for name in names:
            if not (destination / name).resolve().is_relative_to(destination.resolve()) or b.digest(z.read(name)) != lock["files"][name]:
                raise ValueError("ImageSharp source path/hash changed: " + name)
            data[name] = z.read(name)
    seen = set()
    for patch in lock["patches"]:
        name = patch["path"]
        if name in seen or b.digest(data[name]) != patch["inputSha256"]:
            raise ValueError("Duplicate or changed ImageSharp patch: " + name)
        seen.add(name)
        value = data[name].decode("utf-8-sig")
        for edit in patch["edits"]:
            if value.count(edit["old"]) != edit["count"]:
                raise ValueError("ImageSharp patch context changed: " + name)
            value = value.replace(edit["old"], edit["new"])
        data[name] = value.encode()
        if b.digest(data[name]) != patch["outputSha256"]:
            raise ValueError("ImageSharp patch output changed: " + name)
    for name, value in data.items():
        path = destination / name
        path.parent.mkdir(parents=True, exist_ok=True); path.write_bytes(value)
    return {"sourceFiles": sum(n.endswith(".cs") for n in names), "patchedFiles": len(seen)}


def build(workspace, unity, locked=True):
    manifest = extract(workspace / "sources")
    projects = workspace / "projects"
    shutil.copytree(PROJECTS, projects, ignore=shutil.ignore_patterns("obj", "bin"))
    options = [f"-p:ImageSources={workspace / 'sources'}", f"-p:FrameworkPathOverride={foundation.framework_path(unity)}", f"-p:PathMap={workspace}=/scunity-images"]
    b.run(["dotnet", "restore", projects / "Runner/Runner.csproj", *(["--locked-mode"] if locked else []), "--configfile", b.PORT / "Build/NuGet.Config", "-p:NuGetAudit=false", *options], log=workspace / "restore.log")
    b.run(["dotnet", "build", projects / "Runner/Runner.csproj", "--no-restore", "-c", "Release", *options], log=workspace / "build.log")
    if locked:
        for name in ("Backend", "Runner"):
            b.assert_equal(PROJECTS / name / "packages.lock.json", projects / name / "packages.lock.json")
    b.write_json(workspace / "generation.json", manifest)
    return projects


def fixtures(workspace):
    b.check_upstream(b.SHA); b.check_pinned_files(b.read_json(b.PORT / "upstream-lock.json"))
    selected = {f["path"]: f["sha256"] for f in b.read_json(b.PORT / "UpstreamFiles.json")["files"] if f["path"].startswith("Survivalcraft/Content/Assets/") and f["path"].endswith(".webp")}
    if len(selected) != 171:
        raise ValueError("Image fixture inventory changed")
    archive = workspace / "game-images.zip"
    b.run(["git", "-C", b.UPSTREAM, "archive", "--format=zip", f"--output={archive}", b.SHA, "Survivalcraft/Content/Assets"])
    result = {}
    with zipfile.ZipFile(archive) as z:
        for name, sha in selected.items():
            data = z.read(name)
            if b.digest(data) != sha:
                raise ValueError("Image asset hash changed")
            relative = "upstream/" + name.removeprefix("Survivalcraft/Content/Assets/")
            target = workspace / "fixtures" / relative; target.parent.mkdir(parents=True, exist_ok=True); target.write_bytes(data)
            result[relative] = sha
    for name, sha in b.read_json(b.PORT / "Tests/ImageFixtures/fixtures.json")["files"].items():
        source = b.PORT / "Tests/ImageFixtures" / name
        if b.digest(source.read_bytes()) != sha:
            raise ValueError("Synthetic image changed")
        target = workspace / "fixtures/synthetic" / name; target.parent.mkdir(parents=True, exist_ok=True); shutil.copyfile(source, target)
        result["synthetic/" + name] = sha
    return result


def oracle(workspace, original):
    if b.digest((original / "SixLabors.ImageSharp.dll").read_bytes()) != b.read_json(DEPENDENCY / "source-lock.json")["package"]["assemblySha256"]:
        raise ValueError("Original ImageSharp DLL changed")
    projects = workspace / "oracle-projects"
    shutil.copytree(PROJECTS, projects, ignore=shutil.ignore_patterns("bin", "obj", "packages.lock.json"))
    b.run(["dotnet", "build", projects / "Oracle/Oracle.csproj", "-c", "Release", "-p:ImageTargetFramework=net10.0",
           f"-p:OriginalAssemblies={original}", "-p:RestorePackagesWithLockFile=false", "--configfile", b.PORT / "Build/NuGet.Config"], log=workspace / "oracle-build.log")
    mono.execute(["dotnet", projects / "Oracle/bin/Release/net10.0/SCUnity.Image.Oracle.dll", workspace / "fixtures", workspace / "oracle-corpus"], workspace / "oracle.log", 300)


def compare_corpus(left, right):
    names = {p.relative_to(left).as_posix() for p in left.rglob("*") if p.is_file()}
    actual = {p.relative_to(right).as_posix() for p in right.rglob("*") if p.is_file()}
    if names != actual or len(names) != 211:
        raise ValueError(f"Incomplete image corpus: {len(names)}/{len(actual)}")
    result = {}
    for name in sorted(names):
        a, c = (left / name).read_bytes(), (right / name).read_bytes()
        if not a or a != c:
            raise ValueError("Image corpus differs: " + name)
        result[name] = {"bytes": len(a), "sha256": b.digest(a)}
    return result


def normalize(line):
    line = api.normalize(line)
    for name in ("IsExternalInit", "IsUnmanagedAttribute", "CompilerFeatureRequiredAttribute"):
        line = line.replace("[SixLabors.ImageSharp]System.Runtime.CompilerServices." + name, "[BCL]System.Runtime.CompilerServices." + name)
    for name in ("UnscopedRefAttribute", "MemberNotNullAttribute", "MemberNotNullWhenAttribute"):
        line = line.replace("[SixLabors.ImageSharp]System.Diagnostics.CodeAnalysis." + name, "[BCL]System.Diagnostics.CodeAnalysis." + name)
    if line.startswith("attribute SixLabors.ImageSharp [BCL]System.Runtime.Versioning.TargetFrameworkAttribute::"):
        prefix, blob = line.rsplit(" value=", 1)
        allowed = api.TFM_VALUES | {"0100182E4E4554436F72654170702C56657273696F6E3D76362E300100540E144672616D65776F726B446973706C61794E616D65082E4E455420362E30"}
        if blob not in allowed:
            raise ValueError("Unregistered image target framework")
        return prefix + " value=<reviewed-net6-to-net48>"
    if line.startswith("security SixLabors.ImageSharp action=RequestMinimum permissions="):
        prefix, blob = line.rsplit(" permissions=", 1)
        data = bytes.fromhex(blob)
        for scope in (api.CORE48, api.CORE10.replace("10.0.0.0", "6.0.0.0")):
            name = "System.Security.Permissions.SecurityPermissionAttribute"
            data = data.replace(api.serialized_string(name + ", " + scope), api.serialized_string(name + ", [BCL]"))
        return prefix + " permissions=" + data.hex().upper()
    return line


def compare_api(left, right):
    a = Counter(normalize(l) for l in left if not l.startswith("#"))
    c = Counter(normalize(l) for l in right if not l.startswith("#"))
    if not a or a != c:
        raise ValueError("Image API differs:\n" + "\n".join(["- " + l for l in a-c] + ["+ " + l for l in c-a])[:15000])
    return {"types": sum(l.startswith("type ") for l in left), "records": a.total(), "unregisteredDifferences": 0}


def verify_api(workspace, projects, original):
    exe = b.PORT / "Build/ApiSnapshot/bin/Release/net10.0/ApiSnapshot.dll"
    for label, dll in (("original", original / "SixLabors.ImageSharp.dll"), ("candidate", projects / "Runner/bin/Release/net48/SixLabors.ImageSharp.dll")):
        b.run(["dotnet", exe, workspace / (label + "-api.txt"), dll], log=workspace / (label + "-api.log"))
    return compare_api((workspace / "original-api.txt").read_text(encoding="utf-8-sig").splitlines(), (workspace / "candidate-api.txt").read_text(encoding="utf-8-sig").splitlines())


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--unity", type=Path, required=True)
    parser.add_argument("--baseline", type=Path, required=True)
    args = parser.parse_args(); unity = args.unity.resolve(); os.environ["DOTNET_CLI_UI_LANGUAGE"] = "en"
    workspace = Path(tempfile.mkdtemp(prefix="images-", dir=b.PORT / ".artifacts")); print("Evidence directory: " + str(workspace), flush=True)
    fingerprints = sources(); original = foundation.verify_baseline(args.baseline.resolve())
    inputs = fixtures(workspace); projects = build(workspace, unity); oracle(workspace, original)
    api_result = verify_api(workspace, projects, original)
    print("Original package passed; building Unity Mono Player...", flush=True)
    project = workspace / "unity-project"
    shutil.copytree(b.PORT / "Tests/ImageUnity", project / "Assets")
    (project / "ProjectSettings").mkdir(); shutil.copyfile(b.ROOT / "ProjectSettings/ProjectVersion.txt", project / "ProjectSettings/ProjectVersion.txt")
    b.write_json(project / "Packages/manifest.json", {"dependencies": {"com.unity.modules.jsonserialize": "1.0.0"}})
    plugins = project / "Assets/Plugins/Images"; plugins.mkdir(parents=True); bundle = {}
    for dll in (projects / "Runner/bin/Release/net48").glob("*.dll"):
        if dll.name not in mono.UNITY_FRAMEWORK_ASSEMBLIES:
            shutil.copyfile(dll, plugins / dll.name); bundle["plugins/" + dll.name] = b.digest(dll.read_bytes())
    shutil.copytree(workspace / "fixtures", project / "Assets/StreamingAssets/Images")
    player = workspace / "player/SCUnity.Images.exe"; player.parent.mkdir()
    for method in ("Configure", "Build"):
        mono.execute([unity, "-batchmode", "-nographics", "-projectPath", project, "-buildTarget", "Win64", "-executeMethod", "SCUnity.Validation.BuildImage." + method,
                      "-portProbePlayer", player, "-logFile", workspace / ("unity-" + method + ".log")], workspace / (method + "-process.log"), 1200)
    mono.execute([player, "-batchmode", "-nographics", "-imageOutput", workspace / "mono-corpus", "-portProbeResult", workspace / "runtime-result.json", "-logFile", workspace / "player.log"], workspace / "player-process.log", 300)
    runtime = b.read_json(workspace / "runtime-result.json")
    version = (b.ROOT / "ProjectSettings/ProjectVersion.txt").read_text().splitlines()[0].split(": ")[1]
    if runtime.get("passed") is not True or runtime.get("error") or runtime.get("isEditor") is not False or runtime.get("isMono") is not True or runtime.get("pointerSize") != 8 or runtime.get("platform") != "WindowsPlayer" or runtime.get("unityVersion") != version or {v["name"] for v in runtime["checks"]} != CHECKS or any(v["passed"] is not True for v in runtime["checks"]):
        raise ValueError("Image Player assertions/configuration failed")
    build_summary = b.read_json(player.parent / "build-summary.json")
    if build_summary != {"result": "Succeeded", "errors": 0, "warnings": 0, "backend": "Mono2x", "apiCompatibility": "NET_Unity_4_8", "development": False}:
        raise ValueError("Image Player build configuration differs")
    corpus = compare_corpus(workspace / "oracle-corpus", workspace / "mono-corpus")
    managed = mono.verify_player_bundle(player, bundle)
    for name, sha in inputs.items():
        if b.digest((player.parent / "SCUnity.Images_Data/StreamingAssets/Images" / name).read_bytes()) != sha:
            raise ValueError("Player image input changed")
    print("Player pixels/codecs passed; verifying independent rebuild...", flush=True)
    repeat = workspace / "repeat"; repeat.mkdir(); rebuilt = build(repeat, unity); reproducible = {}
    for name in ("SixLabors.ImageSharp.dll", "SCUnity.Image.Runner.dll"):
        data = (projects / "Runner/bin/Release/net48" / name).read_bytes()
        if data != (rebuilt / "Runner/bin/Release/net48" / name).read_bytes():
            raise ValueError("Image DLL rebuild differs: " + name)
        reproducible[name] = b.digest(data)
    if sources() != fingerprints:
        raise ValueError("Sources changed during image validation")
    b.write_json(workspace / "provenance.json", {"schemaVersion": 1, "sources": fingerprints, "upstreamCommit": b.SHA, "inputs": inputs, "api": api_result,
        "corpus": corpus, "bundle": bundle, "managed": managed, "reproducibleDlls": reproducible, "unityVersion": version,
        "unityEditorSha256": b.digest(unity.read_bytes()), "dotnetSdk": b.run(["dotnet", "--version"]), "runtime": runtime, "build": build_summary,
        "scope": "Full ImageSharp source retarget; no Engine image wrappers, Unity texture upload or full-game validation"})
    b.check_upstream(b.SHA)
    print("PASS: 183 images; pixel, resize, encoder, async and half conversion corpora identical; dependency API preserved.", flush=True)


if __name__ == "__main__":
    main()
