"""Regression gates for actual foundation evidence, scoped API and numerical equivalence."""
import copy
import gzip
from pathlib import Path
import sys
import tempfile
import unittest

sys.path.insert(0, str(Path(__file__).resolve().parents[1] / "Build"))
import baseline as b
import foundation as f
import foundation_api as api
import record_foundation as recorder

EVIDENCE = b.PORT / "Tests/FoundationEvidence"


class Foundation(unittest.TestCase):
    def setUp(self):
        self.original = (b.PORT / "Compatibility/PublicApi.Shipped.txt").read_text(encoding="utf-8-sig").splitlines()
        self.candidate = gzip.decompress((EVIDENCE / "foundation-api.txt.gz").read_bytes()).decode("utf-8").splitlines()
        self.profile = b.read_json(f.PROFILE)
        self.provenance = b.read_json(EVIDENCE / "provenance.json")
        self.runtime = b.read_json(EVIDENCE / "runtime-result.json")
        self.build = b.read_json(EVIDENCE / "build-summary.json")

    def test_evidence_and_sources_are_current(self):
        lock = b.read_json(EVIDENCE / "evidence-lock.json")
        for name, sha in lock["files"].items():
            p = EVIDENCE / name
            self.assertEqual(b.digest(p.read_bytes() if p.suffix in (".gz", ".bin") else p.read_text(encoding="utf-8-sig").encode()), sha, name)
        self.assertEqual(lock["recorderSha256"], b.digest(Path(recorder.__file__).read_text(encoding="utf-8-sig").encode()))
        self.assertEqual(self.provenance["sources"], f.source_fingerprints())
        self.assertEqual(self.provenance["upstreamCommit"], b.SHA)
        f.validate_runtime(self.runtime, self.provenance["unityVersion"], self.build, self.provenance["checks"])
        for name in f.CORPUS:
            if name != "math.bin":
                self.assertEqual(b.digest((EVIDENCE / name).read_bytes()), self.provenance["corpus"][name]["sha256"])

    def test_complete_entitysystem_and_partial_engine_are_explicit(self):
        selected = {r["path"] for r in self.profile["sources"]}
        expected = {p.relative_to(b.UPSTREAM).as_posix() for p in (b.UPSTREAM / "EntitySystem").rglob("*.cs")}
        self.assertEqual({p for p in selected if p.startswith("EntitySystem/")}, expected)
        self.assertEqual(len(expected), 20)
        manifest = b.read_json(EVIDENCE / "generation-summary.json")
        self.assertFalse(manifest["complete"])
        self.assertEqual(manifest["counts"], {"exclude-platform": 23, "patch": 12, "pending": 1265, "preserve": 127})
        self.assertEqual(len(selected), 139)

    def test_scoped_api_matches_locked_original(self):
        self.assertEqual(api.compare(self.original, self.candidate, self.profile),
                         {"engineTypes": 131, "entitySystemTypes": 22, "normalizedRecords": 4339, "unregisteredDifferences": 0})

    def test_removed_type_cannot_disappear_from_selection(self):
        changed = [line for line in self.candidate if "[EntitySystem]GameEntitySystem.Project " not in line or not line.startswith("type ")]
        with self.assertRaisesRegex(ValueError, "type inventory"):
            api.compare(self.original, changed, self.profile)

    def test_signature_layout_duplicate_or_attribute_change_is_rejected(self):
        for prefix in ("method [EntitySystem]GameEntitySystem.Project::FindEntity ", "field [Engine]Engine.Matrix::M21 ",
                       "attribute [Engine]Engine.Serialization.Int32HumanReadableConverter "):
            line = next(line for line in self.candidate if line.startswith(prefix))
            with self.subTest(prefix=prefix):
                changed = self.candidate.copy(); changed.remove(line)
                with self.assertRaisesRegex(ValueError, "API drift"): api.compare(self.original, changed, self.profile)
                with self.assertRaisesRegex(ValueError, "API drift"): api.compare(self.original, self.candidate + [line], self.profile)

    def test_only_reviewed_framework_metadata_is_normalized(self):
        self.assertEqual(api.normalize("method X [Foreign.Library]System.Type"), "method X [Foreign.Library]System.Type")
        line = next(line for line in self.candidate if "TargetFrameworkAttribute::.ctor" in line)
        with self.assertRaisesRegex(ValueError, "Unregistered assembly target"):
            api.normalize(line.rsplit("value=", 1)[0] + "value=0000")
        line = next(line for line in self.candidate if line.startswith("attribute [Engine]Engine.Serialization.Int32HumanReadableConverter "))
        changed = self.candidate.copy(); changed.remove(line)
        changed.append(line.replace("496E743332", "496E743634"))
        with self.assertRaisesRegex(ValueError, "API drift"): api.compare(self.original, changed, self.profile)

    def test_single_byte_numeric_or_serialization_drift_is_rejected(self):
        with tempfile.TemporaryDirectory() as directory:
            left, right = Path(directory) / "left", Path(directory) / "right"
            left.mkdir(); right.mkdir()
            for name in f.CORPUS:
                (left / name).write_bytes(b"\x00\x00\x80\x3f"); (right / name).write_bytes(b"\x00\x00\x80\x3f")
            f.compare_corpus(left, right)
            for name in f.CORPUS:
                with self.subTest(name=name):
                    (right / name).write_bytes(b"\x01\x00\x80\x3f")
                    with self.assertRaisesRegex(ValueError, "corpus differs"): f.compare_corpus(left, right)
                    (right / name).write_bytes(b"\x00\x00\x80\x3f")

    def test_editor_failed_or_incomplete_player_cannot_pass(self):
        for key, value in (("isEditor", True), ("isMono", False), ("pointerSize", 4), ("unityVersion", "other"), ("error", "failure")):
            runtime = copy.deepcopy(self.runtime); runtime[key] = value
            with self.subTest(key=key), self.assertRaises(ValueError):
                f.validate_runtime(runtime, self.provenance["unityVersion"], self.build, self.provenance["checks"])
        runtime = copy.deepcopy(self.runtime); runtime["checks"].pop()
        with self.assertRaises(ValueError): f.validate_runtime(runtime, self.provenance["unityVersion"], self.build, self.provenance["checks"])
        checks = dict(self.provenance["checks"]); checks.pop("entity-event-order")
        with self.assertRaises(ValueError): f.validate_runtime(self.runtime, self.provenance["unityVersion"], self.build, checks)
        build = dict(self.build); build["development"] = True
        with self.assertRaises(ValueError): f.validate_runtime(self.runtime, self.provenance["unityVersion"], build, self.provenance["checks"])

    def test_evidence_cannot_be_overwritten(self):
        with self.assertRaisesRegex(ValueError, "already exists"):
            recorder.record(Path("unused"), EVIDENCE)


if __name__ == "__main__":
    unittest.main(verbosity=2)
