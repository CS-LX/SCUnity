"""Capture/verify the original Windows baseline. Requires Python 3.12+, Git and .NET SDK.

All upstream build writes are confined to a fresh archive under Port/.artifacts.
No third-party Python packages, game execution, or Unity Editor are required.
"""
from __future__ import annotations

import argparse
from collections import Counter
import difflib
import hashlib
import json
import os
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile
import zipfile

PORT = Path(__file__).resolve().parents[1]
ROOT = PORT.parent
UPSTREAM = ROOT / "External/SurvivalcraftApi"
PROJECTS = ("Engine.Windows", "EntitySystem.Windows", "Survivalcraft.Windows")
ASSEMBLIES = ("Engine", "EntitySystem", "Survivalcraft")
SHA = "98e5f58f779dba20503a095073451a40899549c4"
SNAPSHOTS = ("UpstreamFiles.json", "SourceManifest.json", "ContentManifest.json",
             "Compatibility/PublicApi.Shipped.txt", "Compatibility/PublicApi.Summary.json")


def run(args: list[str | Path], cwd: Path = PORT, log: Path | None = None) -> str:
    command = [str(arg) for arg in args]
    result = subprocess.run(command, cwd=cwd, stdout=subprocess.PIPE,
                            stderr=subprocess.STDOUT, encoding="utf-8", errors="replace")
    if log:
        log.write_text(result.stdout, encoding="utf-8", newline="\n")
    if result.returncode:
        raise RuntimeError(f"Command failed ({result.returncode}): {command}\n{result.stdout[-12000:]}")
    return result.stdout.strip()


def digest(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


def write_json(path: Path, value: object) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(value, ensure_ascii=False, indent=2) + "\n", encoding="utf-8", newline="\n")


def read_json(path: Path):
    return json.loads(path.read_text(encoding="utf-8-sig"))


def assert_equal(expected: Path, actual: Path) -> None:
    if not expected.is_file():
        raise RuntimeError(f"Missing baseline: {expected}")
    old = expected.read_text(encoding="utf-8-sig")
    new = actual.read_text(encoding="utf-8-sig")
    if old != new:
        diff = list(difflib.unified_diff(old.splitlines(), new.splitlines(),
                                       fromfile=str(expected), tofile=str(actual), n=2))
        raise RuntimeError("Baseline drift:\n" + "\n".join(diff[:100]))


def check_upstream(expected_sha: str) -> None:
    actual = run(["git", "-C", UPSTREAM, "rev-parse", "HEAD"])
    if actual != expected_sha:
        raise RuntimeError(f"Upstream revision mismatch: expected {expected_sha}, got {actual}")
    status = run(["git", "-C", UPSTREAM, "status", "--porcelain=v1", "--untracked-files=all"])
    if status:
        raise RuntimeError(f"Upstream must be clean (including new untracked files):\n{status}")
    # Check the parent repository index as well, so a changed gitlink cannot pass unnoticed.
    gitlink = run(["git", "-C", ROOT, "ls-files", "--stage", "External/SurvivalcraftApi"])
    if not gitlink.startswith(f"160000 {expected_sha} "):
        raise RuntimeError(f"Parent gitlink does not match the locked upstream: {gitlink}")


def classify(path: str, policy: dict) -> dict:
    # Rules are exact paths. New source files must not inherit an unreviewed backend rule.
    if path in policy["overrides"]:
        return policy["overrides"][path]
    module = path.split("/", 1)[0]
    if module in policy["sharedModules"]:
        return {"status": "preserve", "owner": module,
                "reason": "Preserve upstream source; compilation compatibility is not yet validated."}
    if module in policy["excludedModules"]:
        return {"status": "exclude-platform", "owner": "Platform",
                "reason": "Platform is outside the Windows desktop non-VR target."}
    raise RuntimeError(f"Unclassified source file: {path}")


def inventory(stage: Path, output: Path, sha: str) -> None:
    policy = read_json(PORT / "Build/source-policy.json")
    files, sources, content = [], [], []
    for file in sorted(stage.rglob("*")):
        if not file.is_file():
            continue
        path = file.relative_to(stage).as_posix()
        data = file.read_bytes()
        entry = {"path": path, "size": len(data), "sha256": digest(data)}
        files.append(entry)
        if path.endswith(".cs"):
            sources.append({**entry, **classify(path, policy)})
        prefix = "Survivalcraft/Content/"
        if path.startswith(prefix):
            content.append({"path": path[len(prefix):], "size": len(data), "sha256": digest(data)})
    paths = {entry["path"] for entry in sources}
    stale = set(policy["overrides"]) - paths
    if stale:
        raise RuntimeError(f"Stale source-policy entries: {sorted(stale)}")
    header = {"schemaVersion": 1, "upstreamCommit": sha, "hashBasis": "git-archive bytes (no checkout line-ending conversion)"}
    write_json(output / "UpstreamFiles.json", {**header, "files": files})
    write_json(output / "SourceManifest.json", {**header,
        "classificationStage": "migration intent; not a net48 compilation result",
        "counts": dict(sorted(Counter(s["status"] for s in sources).items())), "files": sources})
    write_json(output / "ContentManifest.json", {**header, "archive": "Content.zip",
        "archiveHashPolicy": "sorted entry paths and uncompressed bytes; ZIP metadata is not an identity",
        "files": content, "sidecars": [e for e in files if e["path"] == "Survivalcraft/init.js"]})


def check_content_zip(stage: Path, manifest: Path) -> None:
    expected = read_json(manifest)["files"]
    actual = []
    with zipfile.ZipFile(stage / "Survivalcraft/Content.zip") as archive:
        seen = set()
        for entry in archive.infolist():
            if entry.is_dir():
                continue
            name = entry.filename.replace("\\", "/")
            if name in seen:
                raise RuntimeError(f"Duplicate Content.zip path: {name}")
            seen.add(name)
            data = archive.read(entry)
            actual.append({"path": name, "size": len(data), "sha256": digest(data)})
    if sorted(actual, key=lambda e: e["path"]) != sorted(expected, key=lambda e: e["path"]):
        raise RuntimeError("Built Content.zip differs from the upstream content manifest")


def check_pinned_files(lock: dict) -> None:
    for path, sha in lock["fingerprints"].items():
        full = PORT / path
        # All fingerprint files are authored JSON/text, normalized for Git CRLF checkout.
        if not full.is_file() or digest(full.read_text(encoding="utf-8-sig").replace("\r\n", "\n").encode()) != sha:
            raise RuntimeError(f"Locked baseline/tool file changed: Port/{path}")


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("mode", choices=("capture", "verify"))
    parser.add_argument("--static-only", action="store_true", help="Verify inventories only; does not attest to build/API reproducibility")
    args = parser.parse_args()
    lock_path = PORT / "upstream-lock.json"
    if args.mode == "capture" and (lock_path.exists() or args.static_only):
        raise RuntimeError("Capture is initial-only and requires a full build. Existing baselines must be updated through a reviewed upstream migration.")
    lock = read_json(lock_path) if args.mode == "verify" else None
    sha = lock["upstream"]["commit"] if lock else SHA
    check_upstream(sha)
    if lock:
        check_pinned_files(lock)
    artifacts = PORT / ".artifacts"
    artifacts.mkdir(exist_ok=True)
    workspace = Path(tempfile.mkdtemp(prefix="baseline-", dir=artifacts))
    print(f"Evidence directory: {workspace}", flush=True)
    stage, candidate = workspace / "upstream", workspace / "candidate"
    stage.mkdir()
    candidate.mkdir()
    archive_path = workspace / "upstream.zip"
    run(["git", "-C", UPSTREAM, "archive", "--format=zip", f"--output={archive_path}", sha])
    with zipfile.ZipFile(archive_path) as archive:
        for entry in archive.infolist():
            if not (stage / entry.filename).resolve().is_relative_to(stage.resolve()):
                raise RuntimeError(f"Unsafe archive entry: {entry.filename}")
        archive.extractall(stage)
    inventory(stage, candidate, sha)
    if lock:
        for path in SNAPSHOTS[:3]:
            assert_equal(PORT / path, candidate / path)
    print("Source and content inventories validated.", flush=True)
    if args.static_only:
        print("PASS: static baseline only; build and API checks were not run.")
        return

    sdk = run(["dotnet", "--version"])
    expected_sdk = read_json(PORT / "global.json")["sdk"]["version"]
    if sdk != expected_sdk:
        raise RuntimeError(f"SDK mismatch: expected {expected_sdk}, got {sdk}")
    for project in PROJECTS:
        if lock:
            shutil.copyfile(PORT / f"Dependencies/{project}.packages.lock.json", stage / project / "packages.lock.json")
    project_file = stage / "Survivalcraft.Windows/Survivalcraft.Windows.csproj"
    print("Restoring upstream Windows dependencies (locked on verify)...", flush=True)
    run(["dotnet", "restore", project_file, "--use-lock-file", *(["--locked-mode"] if lock else []),
         "--configfile", PORT / "Build/NuGet.Config", "-p:NuGetAudit=false"], log=workspace / "restore.log")
    for project in PROJECTS:
        dest = candidate / f"Dependencies/{project}.packages.lock.json"
        write_json(dest, read_json(stage / project / "packages.lock.json"))
        if lock:
            assert_equal(PORT / f"Dependencies/{project}.packages.lock.json", dest)
    print("Building unmodified upstream Windows sources (Release)...", flush=True)
    run(["dotnet", "build", project_file, "--no-restore", "-c", "Release", "--nologo"], log=workspace / "build.log")
    check_content_zip(stage, candidate / "ContentManifest.json")
    print("Extracting API metadata without loading/executing game code...", flush=True)
    tool = PORT / "Build/ApiSnapshot/ApiSnapshot.csproj"
    run(["dotnet", "build", tool, "-c", "Release", "--nologo", "--configfile", PORT / "Build/NuGet.Config"], log=workspace / "api-tool-build.log")
    output_dir = stage / "Survivalcraft.Windows/bin/Release/net10.0-windows/win-x64"
    dlls = [output_dir / f"{name}.dll" for name in ASSEMBLIES]
    if not all(dll.is_file() for dll in dlls):
        raise RuntimeError(f"Missing upstream build output in {output_dir}")
    api_out = candidate / "Compatibility/PublicApi.Shipped.txt"
    api_out.parent.mkdir(exist_ok=True)
    run(["dotnet", PORT / "Build/ApiSnapshot/bin/Release/net10.0/ApiSnapshot.dll", api_out, *dlls], log=workspace / "api-snapshot.log")
    check_upstream(sha)
    paths = [*SNAPSHOTS, *(f"Dependencies/{p}.packages.lock.json" for p in PROJECTS)]
    if lock:
        for path in paths:
            assert_equal(PORT / path, candidate / path)
    else:
        for path in paths:
            dest = PORT / path
            dest.parent.mkdir(parents=True, exist_ok=True)
            shutil.copyfile(candidate / path, dest)
        fingerprint_paths = [*paths, "global.json", "Build/NuGet.Config", "Build/source-policy.json",
                             "Build/baseline.py", "Build/ApiSnapshot/ApiSnapshot.csproj", "Build/ApiSnapshot/Program.cs",
                             "Compatibility/PublicApi.Exceptions.json"]
        lock = {"schemaVersion": 1, "scope": "Windows x64 desktop non-VR migration; original Windows reference build includes upstream VR",
                "upstream": {"path": "External/SurvivalcraftApi", "url": "https://gitee.com/SC-SPM/SurvivalcraftApi.git",
                             "branch": "SCAPI1.9", "commit": sha},
                "toolchain": {"dotnetSdk": sdk, "configuration": "Release", "targetFramework": "net10.0-windows", "runtimeIdentifier": "win-x64"},
                "fingerprints": {path: digest((PORT / path).read_text(encoding="utf-8-sig").replace("\r\n", "\n").encode()) for path in sorted(fingerprint_paths)}}
        write_json(lock_path, lock)
    write_json(workspace / "result.json", {"result": "passed", "mode": args.mode, "upstreamCommit": sha,
               "checks": ["clean fixed upstream", "all tracked files", "source classification", "content bytes",
                          "NuGet lock files", "Release build", "Content.zip entries", "API metadata snapshot"],
               "assemblies": [{"name": p.name, "sha256": digest(p.read_bytes())} for p in dlls]})
    print(f"PASS: {args.mode}; all inventories, dependencies, build, Content.zip and API checks passed.\nEvidence: {workspace}")


if __name__ == "__main__":
    try:
        main()
    except (RuntimeError, OSError, ValueError, KeyError, zipfile.BadZipFile) as exc:
        print(f"FAIL: {exc}", file=sys.stderr)
        sys.exit(1)
