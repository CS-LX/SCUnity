"""Guards for unchanged FLAC sources, exact PCM/API comparison and a narrow duration policy."""
import copy
import gzip
from pathlib import Path
import sys
import tempfile
import unittest

sys.path.insert(0, str(Path(__file__).resolve().parents[1] / "Build"))
import baseline as b
import flac as f
import record_flac as recorder

EVIDENCE = b.PORT / "Tests/FlacEvidence"


class Flac(unittest.TestCase):
    def setUp(self):
        self.provenance = b.read_json(EVIDENCE / "provenance.json")
        self.original = gzip.decompress((EVIDENCE / "original-api.txt.gz").read_bytes()).decode().splitlines()
        self.candidate = gzip.decompress((EVIDENCE / "candidate-api.txt.gz").read_bytes()).decode().splitlines()

    def test_actual_player_evidence_and_sources_are_current(self):
        lock = b.read_json(EVIDENCE / "evidence-lock.json")
        for name, sha in lock["files"].items():
            self.assertEqual(f.fingerprint(EVIDENCE / name), sha, name)
        self.assertEqual(lock["recorderSha256"], f.fingerprint(Path(recorder.__file__)))
        self.assertEqual(self.provenance["sources"], f.source_fingerprints())
        f.validate_runtime(b.read_json(EVIDENCE / "runtime-result.json"), self.provenance["unityVersion"],
                           b.read_json(EVIDENCE / "build-summary.json"), self.provenance["checks"])

    def test_dependency_has_74_unchanged_source_files(self):
        with tempfile.TemporaryDirectory() as directory:
            lock = f.extract_source(Path(directory))
            files = list(Path(directory).rglob("*.cs"))
            self.assertEqual(len(files), 74)
            for p in files:
                self.assertEqual(b.digest(p.read_bytes()), lock["files"][p.relative_to(directory).as_posix()])
        self.assertEqual(lock["originalPackage"]["assemblySha256"], self.provenance["originalAssemblies"]["NAudio.Flac.dll"])

    def test_every_original_flac_asset_is_included(self):
        expected = {s["path"]: s["sha256"] for s in b.read_json(b.PORT / "UpstreamFiles.json")["files"]
                    if s["path"].startswith("Survivalcraft/Content/Assets/") and s["path"].endswith(".flac")}
        recorded = {r["source"]: r["sha256"] for r in self.provenance["fixtures"].values() if r["source"].startswith("Survivalcraft/")}
        self.assertEqual(recorded, expected)
        self.assertEqual(len(recorded), 334)
        self.assertTrue((EVIDENCE / "counts.txt").read_text().startswith("files=337\nsynthetic=3\n"))

    def test_full_dependency_api_is_preserved(self):
        self.assertEqual(f.compare_api(self.original, self.candidate), {"types": 69, "records": 1405, "unregisteredDifferences": 0})

    def test_known_pcm_fixture_bytes_and_generator_are_pinned(self):
        directory = b.PORT / "Tests/FlacFixtures"
        manifest = b.read_json(directory / "fixtures.json")
        self.assertEqual(manifest["generatorSha256"], f.fingerprint(b.PORT / "Build/generate_flac_fixtures.py"))
        self.assertEqual(len(manifest["cases"]), 3)
        for name, case in manifest["cases"].items():
            pcm = (directory / (name + ".pcm")).read_bytes()
            self.assertEqual(len(pcm), case["frames"] * case["channels"] * case["bitsPerSample"] // 8)
            self.assertEqual(b.digest(pcm), case["pcmSha256"])
            self.assertEqual(b.digest((directory / (name + ".flac")).read_bytes()), case["flacSha256"])

    def test_removed_or_duplicated_api_and_unknown_framework_fail(self):
        for prefix in ("type [NAudio.Flac]NAudio.Flac.FlacReader ", "method [NAudio.Flac]NAudio.Flac.FlacReader::Read "):
            line = next(s for s in self.candidate if s.startswith(prefix))
            missing = self.candidate.copy(); missing.remove(line)
            for changed in (missing, self.candidate + [line]):
                with self.assertRaisesRegex(ValueError, "API drift"): f.compare_api(self.original, changed)
        line = next(s for s in self.candidate if "TargetFrameworkAttribute::.ctor" in s)
        with self.assertRaisesRegex(ValueError, "Unregistered FLAC target"):
            f.normalize(line.rsplit("value=", 1)[0] + "value=0000")

    def test_duration_policy_matches_actual_observations(self):
        self.assertEqual(f.compare_durations(EVIDENCE / "original-durations.tsv", EVIDENCE / "mono-durations.tsv"),
                         self.provenance["corpus"]["durationComparison"])

    def test_duration_policy_does_not_hide_larger_or_nonrounded_errors(self):
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "changed.tsv"
            original = (EVIDENCE / "mono-durations.tsv").read_text(encoding="utf-8-sig").splitlines()
            name, ticks = original[0].split("\t")
            for adjustment in (1, 10000):
                path.write_text("\n".join([name + "\t" + str(int(ticks) + adjustment), *original[1:]]) + "\n", encoding="utf-8")
                with self.assertRaisesRegex(ValueError, "duration difference"):
                    f.compare_durations(EVIDENCE / "original-durations.tsv", path)
            path.write_text("\n".join(original[:-1]), encoding="utf-8")
            with self.assertRaisesRegex(ValueError, "incomplete"):
                f.compare_durations(EVIDENCE / "original-durations.tsv", path)

    def test_pcm_byte_changes_are_never_tolerated(self):
        with tempfile.TemporaryDirectory() as directory:
            left, right = Path(directory) / "a", Path(directory) / "b"
            left.mkdir(); right.mkdir()
            (left / "corpus.bin").write_bytes(b"\x00\x00\x00\x00")
            (right / "corpus.bin").write_bytes(b"\x00\x01\x00\x00")
            with self.assertRaisesRegex(ValueError, "corpus differs"):
                f.compare_corpus(left, right)

    def test_wrong_player_or_missing_checks_cannot_pass(self):
        runtime = b.read_json(EVIDENCE / "runtime-result.json")
        build = b.read_json(EVIDENCE / "build-summary.json")
        for key, value in (("isEditor", True), ("isMono", False), ("pointerSize", 4), ("passed", False), ("error", "failure")):
            changed = copy.deepcopy(runtime); changed[key] = value
            with self.assertRaises(ValueError): f.validate_runtime(changed, self.provenance["unityVersion"], build, self.provenance["checks"])
        changed = copy.deepcopy(runtime); changed["checks"][0] = changed["checks"][1]
        with self.assertRaises(ValueError): f.validate_runtime(changed, self.provenance["unityVersion"], build, self.provenance["checks"])
        changed = dict(build); changed["backend"] = "IL2CPP"
        with self.assertRaises(ValueError): f.validate_runtime(runtime, self.provenance["unityVersion"], changed, self.provenance["checks"])

    def test_evidence_cannot_be_overwritten(self):
        with self.assertRaisesRegex(ValueError, "already exists"):
            recorder.record(Path("unused"), EVIDENCE)


if __name__ == "__main__":
    unittest.main(verbosity=2)
