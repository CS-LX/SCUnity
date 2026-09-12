"""Generate reviewed source subsets from the locked upstream, never from its checkout.

A probe profile is deliberately incomplete. Its manifest reports every unselected
source as pending; only a complete profile can attest to migration coverage.
"""
from __future__ import annotations

import argparse
from collections import Counter
from pathlib import Path, PurePosixPath
import tempfile
import zipfile
from xml.sax.saxutils import escape

import baseline as b


def relative_path(value: str) -> str:
    path = PurePosixPath(value)
    if (not value or "\\" in value or ":" in value or path.is_absolute()
            or any(p in (".", "..") for p in value.split("/"))
            or path.as_posix() != value):
        raise ValueError(f"Unsafe relative path: {value}")
    return value


def transform(data: bytes, rule: dict, profile_root: Path) -> bytes:
    if b.digest(data) != rule["inputSha256"]:
        raise ValueError(f"Input fingerprint mismatch: {rule['path']}")
    action = rule["action"]
    if action == "preserve":
        result = data
    elif action == "patch":
        # Exact, counted edits are replayable; neither fuzz nor whole-tree regexes.
        result = data
        if not rule.get("reason") or not rule.get("edits"):
            raise ValueError("Patches require a reason and edits")
        for edit in rule["edits"]:
            old, new = edit["old"].encode(), edit["new"].encode()
            if not old or edit["count"] < 1 or result.count(old) != edit["count"]:
                raise ValueError(f"Patch context/count mismatch: {rule['path']}")
            result = result.replace(old, new)
    elif action == "replace-backend":
        if not rule.get("reason"):
            raise ValueError("Backend overrides require a reason")
        source = (profile_root / relative_path(rule["replacement"])).resolve()
        if not source.is_relative_to(profile_root.resolve()):
            raise ValueError("Replacement escapes profile directory")
        result = source.read_bytes().replace(b"\r\n", b"\n")
    else:
        raise ValueError(f"Unknown action: {action}")
    if b.digest(result) != rule["outputSha256"]:
        raise ValueError(f"Output fingerprint mismatch: {rule['path']}")
    return result


def generate(profile_path: Path, destination: Path) -> dict:
    lock = b.read_json(b.PORT / "upstream-lock.json")
    b.check_upstream(lock["upstream"]["commit"])
    b.check_pinned_files(lock)
    profile = b.read_json(profile_path)
    if profile["schemaVersion"] != 1 or profile["scope"] not in ("probe", "complete"):
        raise ValueError("Unknown profile schema or scope")
    if profile["upstreamCommit"] != lock["upstream"]["commit"]:
        raise ValueError("Profile upstream revision mismatch")
    sources = b.read_json(b.PORT / "SourceManifest.json")["files"]
    known = {s["path"]: s for s in sources}
    rules = {}
    for rule in profile["sources"]:
        path = relative_path(rule["path"])
        if path not in known or path in rules:
            raise ValueError(f"Unknown or repeated source: {path}")
        if known[path]["status"] == "exclude-platform":
            raise ValueError(f"Platform source cannot be selected: {path}")
        if rule["inputSha256"] != known[path]["sha256"]:
            raise ValueError(f"Rule disagrees with baseline: {path}")
        rules[path] = rule
    if not rules:
        raise ValueError("A source profile must select at least one file")
    pending = [s["path"] for s in sources if s["status"] != "exclude-platform" and s["path"] not in rules]
    if profile["scope"] == "complete" and pending:
        raise ValueError(f"Complete profile has {len(pending)} unresolved files")
    if destination.exists():
        raise ValueError(f"Generation requires a fresh destination: {destination}")
    destination.mkdir(parents=True)
    archive_path = destination / "upstream.zip"
    b.run(["git", "-C", b.UPSTREAM, "archive", "--format=zip",
           f"--output={archive_path}", profile["upstreamCommit"]])
    with zipfile.ZipFile(archive_path) as archive:
        expected = {s["path"]: s["sha256"] for s in b.read_json(b.PORT / "UpstreamFiles.json")["files"]}
        entries = [e for e in archive.infolist() if not e.is_dir()]
        if len(entries) != len(expected) or {e.filename for e in entries} != set(expected):
            raise ValueError("Archive inventory differs from locked upstream")
        for entry in entries:
            if b.digest(archive.read(entry)) != expected[entry.filename]:
                raise ValueError(f"Archive bytes differ: {entry.filename}")
        records = []
        for source in sources:
            path = source["path"]
            if path in rules:
                rule = rules[path]
                output = transform(archive.read(path), rule, profile_path.parent)
                target = destination / "src" / path
                target.parent.mkdir(parents=True, exist_ok=True)
                target.write_bytes(output)
                records.append({"path": path, "status": rule["action"],
                                "inputSha256": source["sha256"], "outputSha256": b.digest(output)})
            else:
                records.append({"path": path, "status": "exclude-platform" if source["status"] == "exclude-platform" else "pending"})
    items = "\n".join(f'    <Compile Include="$(MSBuildThisFileDirectory)src/{escape(p, {chr(34): "&quot;"})}" />' for p in sorted(rules))
    (destination / "Sources.props").write_text(f"<Project>\n  <ItemGroup>\n{items}\n  </ItemGroup>\n</Project>\n", encoding="utf-8", newline="\n")
    report = {"schemaVersion": 1, "profile": profile["name"], "scope": profile["scope"],
              "upstreamCommit": profile["upstreamCommit"],
              "profileSha256": b.digest(profile_path.read_text(encoding="utf-8-sig").encode()),
              "complete": not pending, "counts": dict(sorted(Counter(r["status"] for r in records).items())),
              "sources": records}
    b.write_json(destination / "GenerationManifest.json", report)
    b.check_upstream(profile["upstreamCommit"])
    return report


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--profile", type=Path, default=b.PORT / "Compatibility/Profiles/mono-probe.json")
    args = parser.parse_args()
    artifacts = b.PORT / ".artifacts"
    artifacts.mkdir(exist_ok=True)
    workspace = Path(tempfile.mkdtemp(prefix="compatibility-", dir=artifacts))
    report = generate(args.profile.resolve(), workspace / "generated")
    print(f"Generated: {workspace / 'generated'}\nCoverage: {report['counts']}; complete={report['complete']}")


if __name__ == "__main__":
    main()
