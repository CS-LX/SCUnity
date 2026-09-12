"""Initial-only generation of small FLAC inputs from known integer PCM (requires ffmpeg)."""
import argparse
from pathlib import Path
import subprocess
import tempfile

import baseline as b


def generate(ffmpeg: Path, destination: Path) -> None:
    if destination.exists():
        raise ValueError("Fixtures already exist; regeneration requires reviewing encoded bytes and the manifest")
    with tempfile.TemporaryDirectory(dir=b.PORT / ".artifacts", prefix="flac-fixtures-") as directory:
        stage = Path(directory)
        cases = {}
        for name, channels, rate, bits in (("mono-8000-16", 1, 8000, 16), ("stereo-44100-16", 2, 44100, 16),
                                            ("stereo-96000-24", 2, 96000, 24)):
            pcm = bytearray()
            for frame in range(5003):
                for channel in range(channels):
                    # Silence, extremes, ramps and unrelated channels exercise several FLAC predictors.
                    value = 0 if frame < 256 else (((frame * (257 + channel * 509)) % (1 << bits)) - (1 << (bits - 1)))
                    pcm.extend(value.to_bytes(bits // 8, "little", signed=True))
            raw, encoded = stage / (name + ".pcm"), stage / (name + ".flac")
            raw.write_bytes(pcm)
            b.run([ffmpeg, "-hide_banner", "-loglevel", "error", "-f", f"s{bits}le", "-ar", rate, "-ac", channels,
                   "-i", raw, "-c:a", "flac", "-compression_level", "5", "-map_metadata", "-1", encoded])
            cases[name] = {"channels": channels, "sampleRate": rate, "bitsPerSample": bits, "frames": 5003,
                           "pcmSha256": b.digest(pcm), "flacSha256": b.digest(encoded.read_bytes())}
        destination.mkdir(parents=True)
        for source in stage.iterdir():
            (destination / source.name).write_bytes(source.read_bytes())
        b.write_json(destination / "fixtures.json", {"schemaVersion": 1, "cases": cases,
            "encoderVersion": subprocess.check_output([str(ffmpeg), "-version"]).decode("utf-8").splitlines()[0],
            "encoderSha256": b.digest(ffmpeg.read_bytes()),
            "generatorSha256": b.digest(Path(__file__).read_text(encoding="utf-8-sig").encode())})


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--ffmpeg", required=True, type=Path)
    args = parser.parse_args()
    generate(args.ffmpeg.resolve(), b.PORT / "Tests/FlacFixtures")
