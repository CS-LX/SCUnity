"""Archive an independently validated ImageSharp run without retaining large pixel corpora."""
from pathlib import Path
import argparse
import gzip

import baseline as b
import images


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("workspace", type=Path)
    args = parser.parse_args(); workspace = args.workspace.resolve()
    destination = b.PORT / "Tests/ImageEvidence"
    if destination.exists():
        raise ValueError("Image evidence already exists; review a replacement explicitly")
    result = b.read_json(workspace / "provenance.json")
    if result["sources"] != images.sources():
        raise ValueError("Image evidence sources changed")
    if images.compare_corpus(workspace / "oracle-corpus", workspace / "mono-corpus") != result["corpus"]:
        raise ValueError("Image evidence corpus changed")
    if images.compare_api((workspace / "original-api.txt").read_text(encoding="utf-8-sig").splitlines(),
                          (workspace / "candidate-api.txt").read_text(encoding="utf-8-sig").splitlines()) != result["api"]:
        raise ValueError("Image evidence API changed")
    for name, digest in result["reproducibleDlls"].items():
        for folder in (workspace, workspace / "repeat"):
            if b.digest((folder / "projects/Runner/bin/Release/net48" / name).read_bytes()) != digest:
                raise ValueError("Image evidence DLL changed")
    if b.read_json(workspace / "runtime-result.json") != result["runtime"] or b.read_json(workspace / "player/build-summary.json") != result["build"]:
        raise ValueError("Image runtime/build evidence changed")
    destination.mkdir()
    api_hashes = {}
    for name in ("original-api.txt", "candidate-api.txt"):
        data = (workspace / name).read_bytes()
        (destination / (name + ".gz")).write_bytes(gzip.compress(data, mtime=0))
        api_hashes[name] = b.digest(data)
    result["apiHashes"] = api_hashes
    result["recorderSha256"] = b.digest(Path(__file__).read_text(encoding="utf-8-sig").encode())
    b.write_json(destination / "evidence.json", result)
    print("Recorded " + str(destination))


if __name__ == "__main__":
    main()
