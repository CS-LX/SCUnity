"""Preserve compact, reviewable reference evidence after successful runtime validation."""
from __future__ import annotations
import argparse
import gzip
from pathlib import Path
import shutil
import baseline
from validate_runtime import validate
from runtime_baseline import verify_samples


def record(generated: Path, community: Path, samples: Path):
    target = baseline.PORT / "Tests/Baselines"
    if target.exists():
        raise RuntimeError("Runtime baseline already exists; review and archive the previous baseline before replacing it")
    validations = {"generated": validate(generated), "community": validate(community)}
    verify_samples(samples)
    target.mkdir(parents=True)
    for name, evidence in (("generated", generated), ("community", community)):
        out = target / name
        out.mkdir()
        for file in ("result.json", "validation.json", "provenance.json"):
            shutil.copyfile(evidence / file, out / file)
        (out / "trace.jsonl.gz").write_bytes(gzip.compress((evidence / "trace.jsonl").read_bytes(), mtime=0))
    for name in ("before-reload.json", "after-reload.json", "new-world.scworld", "populated-world.scworld", "reloaded-world.scworld"):
        shutil.copyfile(generated / name, target / "generated" / name)
    sample_manifest = baseline.read_json(samples / "manifest.json")
    baseline.write_json(target / "UserSamples.json", sample_manifest)
    # Keep the small authentic 2.3 fixture and upstream conversion result. Larger personal
    # worlds/mods stay in the isolated local sample set and are fingerprinted in the manifest.
    for world in sample_manifest["worlds"]:
        if world["attributes"].get("Version") == "2.3":
            source = samples / world["path"]
            shutil.copyfile(source, target / "legacy-2.3.scworld")
            shutil.copyfile(generated / ("upgraded-" + source.stem + ".scworld"), target / "legacy-upgraded-2.4.scworld")
    source_files = [*sorted((baseline.PORT / "Tests/RuntimeProbe").glob("*.cs*")),
                    *sorted((baseline.PORT / "Tests/ModFixture").glob("*.cs*")),
                    *(baseline.PORT / "Build" / p for p in ("runtime_baseline.py", "validate_runtime.py", "collect_runtime_samples.py", "record_runtime_baseline.py"))]
    baseline.write_json(target / "runtime-lock.json", {"schemaVersion": 1, "upstreamCommit": baseline.SHA,
        "upstreamLockSha256": baseline.digest((baseline.PORT / "upstream-lock.json").read_text(encoding="utf-8").encode()),
        "hashBasis": "UTF-8 LF for JSON/source text; raw bytes for gzip and scworld",
        "referenceTemplateCommit": "80a9e325ec8b68fa5d7632b36bed1fdf008f3690",
        "sources": {p.relative_to(baseline.PORT).as_posix(): baseline.digest(p.read_text(encoding="utf-8").encode()) for p in source_files},
        "evidence": {p.relative_to(target).as_posix(): baseline.digest(p.read_text(encoding="utf-8").encode() if p.suffix == ".json" else p.read_bytes())
                     for p in sorted(target.rglob("*")) if p.is_file()},
        "coverage": {k: {"assertions": v["assertions"], "frames": v["frameCount"], "ecsCalls": v["sampledEcsCalls"]} for k,v in validations.items()}})
    print(f"Recorded runtime baseline: {target}")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--generated", required=True, type=Path)
    parser.add_argument("--community", required=True, type=Path)
    parser.add_argument("--samples", required=True, type=Path)
    args = parser.parse_args()
    record(args.generated.resolve(), args.community.resolve(), args.samples.resolve())
