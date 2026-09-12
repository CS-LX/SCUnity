"""Image source replay, strict API comparison, and actual Unity evidence regression checks."""
from pathlib import Path
import gzip
import json
import sys
import tempfile
import unittest

sys.path.insert(0, str(Path(__file__).resolve().parents[1] / "Build"))
import baseline as b
import images


class ImageTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.folder = b.PORT / "Tests/ImageEvidence"
        cls.evidence = b.read_json(cls.folder / "evidence.json")
        cls.original = gzip.decompress((cls.folder / "original-api.txt.gz").read_bytes()).decode("utf-8-sig").splitlines()
        cls.candidate = gzip.decompress((cls.folder / "candidate-api.txt.gz").read_bytes()).decode("utf-8-sig").splitlines()

    def test_sources_and_recorder_unchanged(self):
        self.assertEqual(self.evidence["sources"], images.sources())
        self.assertEqual(self.evidence["recorderSha256"], b.digest((b.PORT / "Build/record_images.py").read_text(encoding="utf-8-sig").encode()))

    def test_source_replay(self):
        with tempfile.TemporaryDirectory() as folder:
            result = images.extract(Path(folder))
            self.assertEqual(result, {"sourceFiles": 1251, "patchedFiles": 31})
            lock = b.read_json(images.DEPENDENCY / "source-lock.json")
            patched = {p["path"]: p["outputSha256"] for p in lock["patches"]}
            for name, digest in lock["files"].items():
                self.assertEqual(b.digest((Path(folder) / name).read_bytes()), patched.get(name, digest), name)

    def test_complete_dependency_api(self):
        self.assertEqual(images.compare_api(self.original, self.candidate), {"types": 467, "records": 15332, "unregisteredDifferences": 0})
        for name, digest in self.evidence["apiHashes"].items():
            self.assertEqual(b.digest(gzip.decompress((self.folder / (name + ".gz")).read_bytes())), digest)

    def test_api_rejects_missing_member(self):
        changed = list(self.candidate)
        changed.pop(next(i for i, v in enumerate(changed) if v.startswith("method ")))
        with self.assertRaises(ValueError):
            images.compare_api(self.original, changed)

    def test_api_rejects_unregistered_target(self):
        changed = [v.replace("value=01001A2E4E45544672616D65776F726B2C56657273696F6E3D76342E380100540E144672616D65776F726B446973706C61794E616D6500", "value=01000000") for v in self.candidate]
        with self.assertRaises(ValueError):
            images.compare_api(self.original, changed)

    def test_runtime_is_real_mono_player(self):
        runtime = self.evidence["runtime"]
        self.assertTrue(runtime["passed"])
        self.assertFalse(runtime["error"])
        self.assertFalse(runtime["isEditor"])
        self.assertTrue(runtime["isMono"])
        self.assertEqual(runtime["platform"], "WindowsPlayer")
        self.assertEqual(runtime["pointerSize"], 8)
        self.assertEqual(runtime["unityVersion"], "6000.3.12f1")
        self.assertEqual(len(runtime["checks"]), len(images.CHECKS))
        self.assertEqual({c["name"] for c in runtime["checks"]}, images.CHECKS)
        self.assertTrue(all(c["passed"] for c in runtime["checks"]))
        self.assertEqual(self.evidence["build"], {"result": "Succeeded", "errors": 0, "warnings": 0, "backend": "Mono2x", "apiCompatibility": "NET_Unity_4_8", "development": False})

    def test_fixture_and_corpus_coverage(self):
        inputs, corpus = self.evidence["inputs"], self.evidence["corpus"]
        self.assertEqual(len(inputs), 183)
        self.assertEqual(sum(n.startswith("upstream/") for n in inputs), 171)
        self.assertEqual(len(corpus), 211)
        self.assertEqual(sum(v["bytes"] for v in corpus.values()), 76157987)
        self.assertEqual(corpus["half.bin"]["bytes"], 65536 * 6 + 100000 * 2)
        self.assertEqual(sum(n.startswith("encoded/") for n in corpus), 12)
        self.assertEqual(sum(n.startswith("encoded-async/") for n in corpus), 12)

    def test_independent_and_deployed_dlls_match(self):
        for name, digest in self.evidence["reproducibleDlls"].items():
            self.assertEqual(self.evidence["bundle"]["plugins/" + name], digest)
        self.assertNotIn("plugins/System.Memory.dll", self.evidence["bundle"])

    def test_pixel_difference_is_never_tolerated(self):
        with tempfile.TemporaryDirectory() as folder:
            left, right = Path(folder) / "left", Path(folder) / "right"
            left.mkdir(); right.mkdir()
            for i in range(211):
                (left / str(i)).write_bytes(b"same")
                (right / str(i)).write_bytes(b"same")
            self.assertEqual(len(images.compare_corpus(left, right)), 211)
            (right / "10").write_bytes(b"samf")
            with self.assertRaises(ValueError):
                images.compare_corpus(left, right)


if __name__ == "__main__":
    unittest.main()
