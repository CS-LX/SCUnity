"""Archive verified foundation evidence; initial-only, with source/Player/rebuild checks."""
import argparse
import gzip
from pathlib import Path
import shutil

import baseline as b
import foundation as f
import foundation_api as api
import mono_probe as mono


def record(workspace: Path, destination: Path) -> None:
    if destination.exists():
        raise ValueError("Evidence already exists; updates require reviewing the code and result differences")
    provenance = b.read_json(workspace / "provenance.json")
    if provenance["sources"] != f.source_fingerprints() or provenance["upstreamCommit"] != b.SHA:
        raise ValueError("Recorded foundation sources differ from current inputs")
    for path, sha in provenance["evidenceFiles"].items():
        if b.digest((workspace / path).read_bytes()) != sha:
            raise ValueError(f"Evidence changed: {path}")
    f.validate_runtime(b.read_json(workspace / "runtime-result.json"), provenance["unityVersion"],
                       b.read_json(workspace / "player/build-summary.json"), b.read_json(workspace / "oracle-corpus/checks.json"))
    if f.compare_corpus(workspace / "oracle-corpus", workspace / "mono-corpus") != provenance["corpus"]:
        raise ValueError("Original/Mono corpus evidence changed")
    candidate = (workspace / "foundation-api.txt").read_text(encoding="utf-8-sig")
    if api.compare((b.PORT / "Compatibility/PublicApi.Shipped.txt").read_text(encoding="utf-8-sig").splitlines(),
                   candidate.splitlines(), b.read_json(f.PROFILE)) != provenance["api"]:
        raise ValueError("Scoped API comparison changed")
    player = workspace / "player/SCUnity.Foundation.exe"
    if b.digest(player.read_bytes()) != provenance["playerSha256"] or mono.verify_player_bundle(player, provenance["bundle"]) != provenance["runtimeAssemblies"]:
        raise ValueError("Player executable or deployed runtime changed")
    reproducible = {}
    for name in ("Engine.dll", "EntitySystem.dll", "SCUnity.Foundation.Runner.dll", "SCUnity.Foundation.Payload.dll"):
        relative = Path("projects/Payload/bin/Release/net48") / name
        original = (workspace / relative).read_bytes()
        if original != (workspace / "repeat" / relative).read_bytes():
            raise ValueError(f"Independent rebuild differs: {name}")
        reproducible[name] = b.digest(original)
    generation = (workspace / "generated/GenerationManifest.json").read_bytes()
    if generation != (workspace / "repeat/generated/GenerationManifest.json").read_bytes():
        raise ValueError("Independent source generation differs")
    destination.mkdir(parents=True)
    for path in ("runtime-result.json", "player/build-summary.json", "provenance.json", "api-comparison.json"):
        shutil.copyfile(workspace / path, destination / Path(path).name)
    (destination / "foundation-api.txt.gz").write_bytes(gzip.compress(candidate.encode("utf-8"), mtime=0))
    manifest = b.read_json(workspace / "generated/GenerationManifest.json")
    b.write_json(destination / "generation-summary.json", {
        **{key: value for key, value in manifest.items() if key != "sources"},
        "selectedSources": [s for s in manifest["sources"] if "outputSha256" in s]})
    b.write_json(destination / "reproducibility.json", {"independentDirectories": True, "sourceGenerationIdentical": True,
        "framework": "net48", "configuration": "Release", "byteIdenticalDlls": reproducible})
    # Small inspectable examples; the 4 MB random corpus is reproducible from Entry.cs and hashed above.
    for name in f.CORPUS:
        if name != "math.bin":
            shutil.copyfile(workspace / "oracle-corpus" / name, destination / name)
    b.write_json(destination / "evidence-lock.json", {
        "schemaVersion": 1, "hashBasis": "UTF-8 LF for text; exact bytes for .gz and .bin",
        "recorderSha256": b.digest(Path(__file__).read_text(encoding="utf-8-sig").encode()),
        "files": {p.name: b.digest(p.read_bytes() if p.suffix in (".gz", ".bin") else p.read_text(encoding="utf-8-sig").encode())
                  for p in sorted(destination.iterdir())}})


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("workspace", type=Path)
    args = parser.parse_args()
    record(args.workspace.resolve(), b.PORT / "Tests/FoundationEvidence")
    print("Recorded: Port/Tests/FoundationEvidence")
