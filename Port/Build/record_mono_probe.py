"""Record a successfully accepted Unity Mono probe; never overwrite existing evidence."""
from __future__ import annotations

import argparse
from pathlib import Path
import shutil

import baseline as b
import mono_probe as m


def record(workspace: Path, destination: Path, repeat: Path) -> None:
    if destination.exists():
        raise ValueError("Evidence already exists; update it only in an explicitly reviewed change")
    provenance = b.read_json(workspace / "provenance.json")
    if provenance["sources"] != m.source_fingerprints():
        raise ValueError("Recorded inputs differ from current probe sources")
    result = b.read_json(workspace / "runtime-result.json")
    build = b.read_json(workspace / "player/build-summary.json")
    m.validate_result(result, provenance["unityVersion"], build)
    m.validate_negative(b.read_json(workspace / "negative-result.json"))
    files = {"runtime-result.json": "runtimeResultSha256", "negative-result.json": "negativeResultSha256",
             "player/build-summary.json": "buildSummarySha256", "generated/GenerationManifest.json": "generationManifestSha256"}
    for path, key in files.items():
        if b.digest((workspace / path).read_bytes()) != provenance[key]:
            raise ValueError(f"Evidence changed: {path}")
    player = workspace / "player/SCUnity.MonoProbe.exe"
    if b.digest(player.read_bytes()) != provenance["playerSha256"]:
        raise ValueError("Player executable changed")
    if m.verify_player_bundle(player, provenance["bundle"]) != provenance["runtimeAssemblies"]:
        raise ValueError("Player runtime assemblies changed")
    if workspace.resolve() == repeat.resolve():
        raise ValueError("Reproducibility requires an independent staging directory")
    reproducible = {}
    for name in ("Runner", "Upstream", "Contracts", "Plugin", "Dependency"):
        relative = Path("projects") / name / f"bin/Release/net48/SCUnity.Probe.{name}.dll"
        original = (workspace / relative).read_bytes()
        if original != (repeat / relative).read_bytes():
            raise ValueError(f"Non-reproducible DLL: {name}")
        reproducible[f"SCUnity.Probe.{name}.dll"] = b.digest(original)
    if (workspace / "generated/GenerationManifest.json").read_bytes() != (repeat / "generated/GenerationManifest.json").read_bytes():
        raise ValueError("Non-reproducible source generation")
    destination.mkdir(parents=True)
    for path in ("runtime-result.json", "negative-result.json", "player/build-summary.json", "provenance.json"):
        shutil.copyfile(workspace / path, destination / Path(path).name)
    generation = b.read_json(workspace / "generated/GenerationManifest.json")
    b.write_json(destination / "generation-summary.json", {
        **{key: value for key, value in generation.items() if key != "sources"},
        "selectedSources": [s for s in generation["sources"] if "outputSha256" in s]})
    b.write_json(destination / "reproducibility.json", {
        "independentDirectories": True, "sourceGenerationIdentical": True,
        "framework": "net48", "configuration": "Release", "byteIdenticalDlls": reproducible})
    b.write_json(destination / "evidence-lock.json", {
        "schemaVersion": 1, "hashBasis": "UTF-8 LF for JSON text",
        "recorderSha256": b.digest(Path(__file__).read_text(encoding="utf-8-sig").encode()),
        "files": {p.name: b.digest(p.read_text(encoding="utf-8-sig").encode()) for p in sorted(destination.iterdir())}})


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("workspace", type=Path)
    parser.add_argument("--repeat", type=Path, required=True, help="Independent build directory for byte identity checks")
    args = parser.parse_args()
    record(args.workspace.resolve(), b.PORT / "Tests/MonoEvidence", args.repeat.resolve())
    print("Recorded: Port/Tests/MonoEvidence")
