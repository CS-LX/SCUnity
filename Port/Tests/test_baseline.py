"""Behavioral tests for the baseline guards and metadata snapshot; no game execution."""
from __future__ import annotations

import importlib.util
import os
from pathlib import Path
import shutil
import subprocess
import tempfile
import unittest
from unittest.mock import patch
import zipfile

PORT = Path(__file__).resolve().parents[1]
spec = importlib.util.spec_from_file_location("baseline", PORT / "Build/baseline.py")
baseline = importlib.util.module_from_spec(spec)
spec.loader.exec_module(baseline)


class BaselineGuards(unittest.TestCase):
    def setUp(self):
        self.workspace = Path(tempfile.mkdtemp(prefix="guard-test-", dir=PORT / ".artifacts"))

    def test_inventory_detects_addition_removal_and_byte_changes(self):
        stage = self.workspace / "upstream"
        stage.mkdir()
        policy = {"sharedModules": ["Engine"], "excludedModules": [], "overrides": {}}
        file = stage / "Engine/A.cs"
        file.parent.mkdir()
        file.write_bytes(b"public class A {}\n")
        with patch.object(baseline, "read_json", return_value=policy):
            baseline.inventory(stage, self.workspace / "original", "test")
            for name, change in [
                ("changed", lambda: file.write_bytes(b"public class Changed {}\n")),
                ("added", lambda: (stage / "Engine/New.cs").write_bytes(b"public class New {}\n")),
                ("removed", lambda: file.unlink()),
            ]:
                change()
                baseline.inventory(stage, self.workspace / name, "test")
                with self.assertRaisesRegex(RuntimeError, "Baseline drift"):
                    baseline.assert_equal(self.workspace / "original/SourceManifest.json", self.workspace / name / "SourceManifest.json")

    def test_unclassified_and_stale_source_rules_fail(self):
        stage = self.workspace / "upstream"
        stage.mkdir()
        (stage / "Unexpected.cs").write_bytes(b"class Unexpected {}")
        with self.assertRaisesRegex(RuntimeError, "Unclassified"):
            baseline.inventory(stage, self.workspace / "candidate", "test")
        policy = {"sharedModules": [], "excludedModules": [], "overrides": {"Missing.cs": {}}}
        (stage / "Unexpected.cs").unlink()
        with patch.object(baseline, "read_json", return_value=policy):
            with self.assertRaisesRegex(RuntimeError, "Stale source-policy"):
                baseline.inventory(stage, self.workspace / "candidate", "test")

    def test_zip_compares_content_not_timestamps_and_rejects_corruption(self):
        stage = self.workspace / "upstream"
        (stage / "Survivalcraft").mkdir(parents=True)
        manifest = self.workspace / "manifest.json"
        data = b"texture payload"
        baseline.write_json(manifest, {"files": [{"path": "Assets/Test.bin", "size": len(data), "sha256": baseline.digest(data)}]})
        archive = stage / "Survivalcraft/Content.zip"
        for year in (2000, 2020):
            with zipfile.ZipFile(archive, "w") as z:
                z.writestr(zipfile.ZipInfo("Assets/Test.bin", (year, 1, 1, 0, 0, 0)), data)
            baseline.check_content_zip(stage, manifest)
        with zipfile.ZipFile(archive, "w") as z:
            z.writestr("Assets/Test.bin", b"wrong bytes")
        with self.assertRaisesRegex(RuntimeError, "differs"):
            baseline.check_content_zip(stage, manifest)

    def test_lock_fingerprint_accepts_crlf_and_rejects_tampering(self):
        path = self.workspace / "dependency.lock.json"
        path.write_bytes(b'{\r\n  "version": 1\r\n}\r\n')
        lock = {"fingerprints": {path.name: baseline.digest(b'{\n  "version": 1\n}\n')}}
        with patch.object(baseline, "PORT", self.workspace):
            baseline.check_pinned_files(lock)
            path.write_bytes(b'{"version":2}')
            with self.assertRaisesRegex(RuntimeError, "changed"):
                baseline.check_pinned_files(lock)

    def test_upstream_rejects_wrong_commit_dirty_files_and_gitlink(self):
        upstream = self.workspace / "External/SurvivalcraftApi"
        upstream.mkdir(parents=True)
        command = baseline.run
        command(["git", "init", upstream])
        (upstream / "file.cs").write_bytes(b"original\n")
        command(["git", "-C", upstream, "add", "."])
        command(["git", "-C", upstream, "-c", "user.name=Baseline Test", "-c", "user.email=test@example.invalid", "commit", "-m", "fixture"])
        sha = command(["git", "-C", upstream, "rev-parse", "HEAD"])
        command(["git", "init", self.workspace])
        command(["git", "-C", self.workspace, "update-index", "--add", "--cacheinfo", f"160000,{sha},External/SurvivalcraftApi"])
        with patch.object(baseline, "ROOT", self.workspace), patch.object(baseline, "UPSTREAM", upstream):
            baseline.check_upstream(sha)
            with self.assertRaisesRegex(RuntimeError, "revision mismatch"):
                baseline.check_upstream("0" * 40)
            (upstream / "file.cs").write_bytes(b"changed\n")
            with self.assertRaisesRegex(RuntimeError, "must be clean"):
                baseline.check_upstream(sha)
            (upstream / "file.cs").write_bytes(b"original\n")
            (upstream / "new.cs").write_bytes(b"new source")
            with self.assertRaisesRegex(RuntimeError, "must be clean"):
                baseline.check_upstream(sha)
            (upstream / "new.cs").unlink()
            command(["git", "-C", self.workspace, "update-index", "--cacheinfo", f"160000,{'1' * 40},External/SurvivalcraftApi"])
            with self.assertRaisesRegex(RuntimeError, "gitlink"):
                baseline.check_upstream(sha)


class MetadataSnapshot(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.workspace = Path(tempfile.mkdtemp(prefix="api-test-", dir=PORT / ".artifacts"))
        cls.fixture = cls.workspace / "fixture"
        shutil.copytree(PORT / "Tests/ApiFixture", cls.fixture)
        baseline.run(["dotnet", "build", PORT / "Build/ApiSnapshot/ApiSnapshot.csproj", "-c", "Release", "--nologo"])
        cls.snapshots = {}
        cls.marker = cls.workspace / "must-not-exist.txt"
        for variant in ("BASELINE", "MUTATION", "PRIVATE_CHANGE"):
            baseline.run(["dotnet", "build", cls.fixture / "ApiFixture.csproj", "-c", "Release", "--nologo", f"-p:DefineConstants={variant}"])
            target = cls.workspace / f"{variant}.txt"
            with patch.dict(os.environ, {"SC_API_SNAPSHOT_PROBE": str(cls.marker)}):
                baseline.run(["dotnet", PORT / "Build/ApiSnapshot/bin/Release/net10.0/ApiSnapshot.dll", target,
                              cls.fixture / "bin/Release/net10.0/ApiFixture.dll"])
            cls.snapshots[variant] = target.read_text(encoding="utf-8")

    def test_metadata_read_does_not_execute_module_initializer(self):
        self.assertFalse(self.marker.exists())

    def test_external_surface_and_abi_details_are_present(self):
        text = self.snapshots["BASELINE"]
        for value in ("::ForDerived", "+DerivedVisible", "::ReadOnly", "::Changed", "::Item",
                      "parameter[1]", "generic[0]", "ReferenceTypeConstraint", "DefaultConstructorConstraint",
                      "pack=2 size=16", "offset=4", "layoutIndex=0", "constant=Int64:2A00000000000000", "fnptr", "Int32&",
                      "DecimalConstantAttribute", "ParamArrayAttribute", "override "):
            self.assertIn(value, text)
        for value in ("HiddenNested", "+Hidden ", "::HiddenMethod", "::AssemblyOnly"):
            self.assertNotIn(value, text)

    def test_changes_to_api_and_layout_are_detected(self):
        old = self.snapshots["BASELINE"].splitlines()
        new = self.snapshots["MUTATION"].splitlines()
        delta = "\n".join(sorted(set(old) ^ set(new)))
        for value in ("::Value", "::ReadOnly", "::Transform", "::Create", "::Answer", "::storage", "Sequential::first", "Sequential::second"):
            self.assertIn(value, delta)

    def test_method_body_only_change_does_not_change_api(self):
        self.assertEqual(self.snapshots["BASELINE"], self.snapshots["PRIVATE_CHANGE"])

    def test_nuget_locked_restore_rejects_dependency_change(self):
        project = self.fixture / "ApiFixture.csproj"
        baseline.run(["dotnet", "restore", project, "--use-lock-file", "--configfile", PORT / "Build/NuGet.Config"])
        original = project.read_text(encoding="utf-8")
        project.write_text(original.replace("</Project>", '<ItemGroup><PackageReference Include="NVorbis" Version="0.10.5" /></ItemGroup></Project>'), encoding="utf-8")
        result = subprocess.run(["dotnet", "restore", str(project), "--locked-mode", "--configfile", str(PORT / "Build/NuGet.Config")],
                                cwd=PORT, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, encoding="utf-8", errors="replace")
        self.assertNotEqual(result.returncode, 0)
        self.assertIn("NU1004", result.stdout)


if __name__ == "__main__":
    (PORT / ".artifacts").mkdir(exist_ok=True)
    unittest.main(verbosity=2)
