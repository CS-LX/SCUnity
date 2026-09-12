"""Archive accepted original-package / Unity FLAC evidence, without overwriting earlier evidence."""
import argparse
import gzip
from pathlib import Path
import shutil

import baseline as b
import flac as f
import mono_probe as mono


def record(workspace: Path, destination: Path) -> None:
    if destination.exists():
        raise ValueError("FLAC evidence already exists; changes require review of sources and results")
    provenance = b.read_json(workspace / "provenance.json")
    if provenance["sources"] != f.source_fingerprints() or provenance["upstreamCommit"] != b.SHA:
        raise ValueError("FLAC evidence sources changed")
    for path, sha in provenance["evidenceFiles"].items():
        if b.digest((workspace / path).read_bytes()) != sha:
            raise ValueError("FLAC evidence file changed: " + path)
    f.validate_runtime(b.read_json(workspace / "runtime-result.json"), provenance["unityVersion"],
                       b.read_json(workspace / "player/build-summary.json"), provenance["checks"])
    if f.compare_corpus(workspace / "oracle-corpus", workspace / "mono-corpus") != provenance["corpus"]:
        raise ValueError("FLAC corpus evidence changed")
    if f.compare_api((workspace / "original-api.txt").read_text(encoding="utf-8-sig").splitlines(),
                     (workspace / "candidate-api.txt").read_text(encoding="utf-8-sig").splitlines()) != provenance["api"]:
        raise ValueError("FLAC API evidence changed")
    player = workspace / "player/SCUnity.Flac.exe"
    if b.digest(player.read_bytes()) != provenance["playerSha256"] or mono.verify_player_bundle(player, provenance["bundle"]) != provenance["runtimeAssemblies"]:
        raise ValueError("FLAC Player executable or deployed DLL changed")
    for path, item in provenance["fixtures"].items():
        if b.digest((player.parent / "SCUnity.Flac_Data/StreamingAssets/Flac" / path).read_bytes()) != item["sha256"]:
            raise ValueError("FLAC Player fixture changed: " + path)
    for name, sha in provenance["reproducibleDlls"].items():
        for root in (workspace, workspace / "repeat"):
            if b.digest((root / "projects/Runner/bin/Release/net48" / name).read_bytes()) != sha:
                raise ValueError("FLAC independent rebuild changed: " + name)
    destination.mkdir(parents=True)
    for path in ("runtime-result.json", "player/build-summary.json", "provenance.json", "api-comparison.json", "oracle-corpus/counts.txt"):
        shutil.copyfile(workspace / path, destination / Path(path).name)
    for name in ("original-api", "candidate-api"):
        content = (workspace / (name + ".txt")).read_text(encoding="utf-8-sig").encode()
        (destination / (name + ".txt.gz")).write_bytes(gzip.compress(content, mtime=0))
    for name, folder in (("original", "oracle-corpus"), ("mono", "mono-corpus")):
        shutil.copyfile(workspace / folder / "durations.tsv", destination / (name + "-durations.tsv"))
    b.write_json(destination / "evidence-lock.json", {"schemaVersion": 1,
        "hashBasis": "UTF-8 LF for text; exact compressed bytes for .gz",
        "recorderSha256": b.digest(Path(__file__).read_text(encoding="utf-8-sig").encode()),
        "files": {p.name: f.fingerprint(p) for p in sorted(destination.iterdir())}})


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("workspace", type=Path)
    args = parser.parse_args()
    record(args.workspace.resolve(), b.PORT / "Tests/FlacEvidence")
    print("Recorded: Port/Tests/FlacEvidence")
