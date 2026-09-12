"""Guards for reviewed source generation and real Player acceptance evidence."""
from __future__ import annotations

import copy
from pathlib import Path
import sys
import tempfile
import unittest

sys.path.insert(0, str(Path(__file__).resolve().parents[1] / "Build"))
import baseline as b
import generate_compatibility as g
import mono_probe as m


class SourceGeneration(unittest.TestCase):
    def setUp(self):
        # Archive generation remains frozen at the original SHA. The active
        # checkout is now its source-port descendant, validated by a separate guard.
        if (g.b.PORT / "source-lock.json").exists():
            import source_guard
            from unittest.mock import patch
            guard = patch.object(g.b, "check_upstream", source_guard.check_upstream)
            guard.start()
            self.addCleanup(guard.stop)

    def test_generation_is_reproducible_and_reports_incomplete_coverage(self):
        with tempfile.TemporaryDirectory(dir=b.PORT / ".artifacts", prefix="generation-test-") as directory:
            root = Path(directory)
            profile = b.PORT / "Compatibility/Profiles/mono-probe.json"
            first = g.generate(profile, root / "first")
            second = g.generate(profile, root / "second")
            self.assertEqual(first, second)
            self.assertEqual(first["counts"], {"exclude-platform": 23, "pending": 1401, "preserve": 3})
            self.assertFalse(first["complete"])
            for path in (root / "first").rglob("*"):
                if path.is_file():
                    self.assertEqual(path.read_bytes(), (root / "second" / path.relative_to(root / "first")).read_bytes())
            with self.assertRaisesRegex(ValueError, "fresh destination"):
                g.generate(profile, root / "first")

    def test_profile_rejects_unreviewed_selection_and_false_completion(self):
        original = b.read_json(b.PORT / "Compatibility/Profiles/mono-probe.json")
        cases = []
        profile = copy.deepcopy(original); profile["scope"] = "complete"; cases.append(profile)
        profile = copy.deepcopy(original); profile["sources"].append(profile["sources"][0]); cases.append(profile)
        profile = copy.deepcopy(original); profile["sources"][0]["path"] = "Engine/unknown.cs"; cases.append(profile)
        profile = copy.deepcopy(original); profile["sources"][0]["inputSha256"] = "0" * 64; cases.append(profile)
        profile = copy.deepcopy(original); profile["upstreamCommit"] = "0" * 40; cases.append(profile)
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            for i, profile in enumerate(cases):
                with self.subTest(i=i):
                    path = root / f"profile-{i}.json"
                    b.write_json(path, profile)
                    with self.assertRaises(ValueError):
                        g.generate(path, root / f"output-{i}")

    def test_exact_patch_requires_input_context_count_and_output_hash(self):
        data, output = b"before\nbefore\n", b"after\nafter\n"
        rule = {"path": "Engine/Test.cs", "action": "patch", "reason": "test fixture",
                "inputSha256": b.digest(data), "outputSha256": b.digest(output),
                "edits": [{"old": "before", "new": "after", "count": 2}]}
        self.assertEqual(g.transform(data, rule, Path(".")), output)
        with self.assertRaisesRegex(ValueError, "Input fingerprint"):
            g.transform(data + b" ", rule, Path("."))
        wrong = copy.deepcopy(rule); wrong["edits"][0]["count"] = 1
        with self.assertRaisesRegex(ValueError, "context/count"):
            g.transform(data, wrong, Path("."))
        wrong = copy.deepcopy(rule); wrong["edits"][0]["new"] = "unreviewed"
        with self.assertRaisesRegex(ValueError, "Output fingerprint"):
            g.transform(data, wrong, Path("."))

    def test_preserve_and_override_cannot_silently_change_bytes(self):
        data = b"upstream\n"
        rule = {"path": "Engine/Test.cs", "action": "preserve", "inputSha256": b.digest(data), "outputSha256": b.digest(data)}
        self.assertEqual(g.transform(data, rule, Path(".")), data)
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            (root / "Backend.cs").write_bytes(b"replacement\r\n")
            rule.update(action="replace-backend", reason="test fixture", replacement="Backend.cs", outputSha256=b.digest(b"replacement\n"))
            self.assertEqual(g.transform(data, rule, root), b"replacement\n")
            (root / "Backend.cs").write_bytes(b"edited")
            with self.assertRaisesRegex(ValueError, "Output fingerprint"):
                g.transform(data, rule, root)

    def test_paths_cannot_escape_generation_or_override_roots(self):
        for path in ("", "../x", "/x", "C:/x", "x/../y", "x//y", "x/./y", "x\\y", "x/"):
            with self.subTest(path=path), self.assertRaises(ValueError):
                g.relative_path(path)
        self.assertEqual(g.relative_path("Engine/Engine/ReadOnlyList.cs"), "Engine/Engine/ReadOnlyList.cs")


class PlayerAcceptance(unittest.TestCase):
    def setUp(self):
        self.result = {"schema": "scunity-mono-probe-v1", "passed": True, "error": None,
                       "isEditor": False, "isMono": True, "platform": "WindowsPlayer", "pointerSize": 8,
                       "unityVersion": "6000.3.12f1", "checks": [{"name": n, "passed": True} for n in sorted(m.EXPECTED_CHECKS)]}
        self.build = {"result": "Succeeded", "errors": 0, "warnings": 0, "backend": "Mono2x", "apiCompatibility": "NET_Unity_4_8", "development": False}

    def validate(self, result=None, build=None):
        m.validate_result(result or self.result, "6000.3.12f1", build or self.build)

    def test_complete_result_is_accepted(self):
        self.validate()

    def test_recorded_player_run_matches_current_sources_and_evidence(self):
        directory = b.PORT / "Tests/MonoEvidence"
        lock = b.read_json(directory / "evidence-lock.json")
        for name, sha in lock["files"].items():
            self.assertEqual(b.digest((directory / name).read_text(encoding="utf-8-sig").encode()), sha, name)
        self.assertEqual(lock["recorderSha256"], b.digest((b.PORT / "Build/record_mono_probe.py").read_text(encoding="utf-8-sig").encode()))
        provenance = b.read_json(directory / "provenance.json")
        self.assertEqual(provenance["sources"], m.source_fingerprints())
        m.validate_result(b.read_json(directory / "runtime-result.json"), provenance["unityVersion"], b.read_json(directory / "build-summary.json"))
        m.validate_negative(b.read_json(directory / "negative-result.json"))

    def test_editor_wrong_backend_or_version_cannot_pass(self):
        for key, value in (("isEditor", True), ("isMono", False), ("platform", "WindowsEditor"),
                           ("pointerSize", 4), ("unityVersion", "6000.3.0f1"), ("passed", False), ("error", "exception")):
            with self.subTest(key=key), self.assertRaises(ValueError):
                result = copy.deepcopy(self.result); result[key] = value; self.validate(result)

    def test_missing_duplicate_and_failed_assertions_cannot_pass(self):
        cases = []
        result = copy.deepcopy(self.result); result["checks"].pop(); cases.append(result)
        result = copy.deepcopy(self.result); result["checks"][0] = result["checks"][1]; cases.append(result)
        result = copy.deepcopy(self.result); result["checks"][0]["passed"] = False; cases.append(result)
        for result in cases:
            with self.assertRaises(ValueError): self.validate(result)

    def test_wrong_build_configuration_cannot_pass(self):
        for key, value in (("result", "Failed"), ("errors", 1), ("warnings", 1), ("backend", "IL2CPP"),
                           ("apiCompatibility", "NET_Standard"), ("development", True)):
            with self.subTest(key=key), self.assertRaises(ValueError):
                build = copy.deepcopy(self.build); build[key] = value; self.validate(build=build)

    def test_negative_case_requires_the_expected_dependency_failure(self):
        result = {"passed": False, "isEditor": False, "isMono": True,
                  "error": "FileNotFoundException: SCUnity.Probe.Dependency"}
        m.validate_negative(result)
        for key, value in (("passed", True), ("isEditor", True), ("error", "other failure")):
            with self.subTest(key=key), self.assertRaises(ValueError):
                wrong = dict(result); wrong[key] = value; m.validate_negative(wrong)

    def test_player_bundle_must_match_built_inputs(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            target = root / "Probe_Data/Managed/test.dll"
            target.parent.mkdir(parents=True)
            target.write_bytes(b"expected")
            bundle = {"plugins/test.dll": b.digest(b"expected")}
            self.assertEqual(m.verify_player_bundle(root / "Probe.exe", bundle), {"test.dll": b.digest(b"expected")})
            target.write_bytes(b"changed")
            with self.assertRaisesRegex(ValueError, "bundle changed"):
                m.verify_player_bundle(root / "Probe.exe", bundle)


if __name__ == "__main__":
    unittest.main(verbosity=2)
