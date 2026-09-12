"""Source-fork provenance and public API regression guards."""
import json
from pathlib import Path
import sys
import tempfile
import unittest
from unittest.mock import patch

sys.path.insert(0, str(Path(__file__).resolve().parents[1] / "Build"))
import baseline as b
import source_api
import source_guard


class SourceApi(unittest.TestCase):
    def test_compiler_metadata_does_not_hide_a_signature_change(self):
        with tempfile.TemporaryDirectory(dir=b.PORT / ".artifacts") as folder:
            original, candidate = Path(folder) / "old.txt", Path(folder) / "new.txt"
            signature = "method [Engine]Engine.Example::Read Int32 () header=20"
            original.write_text(signature + "\nattribute Engine [mscorlib]System.Runtime.Versioning.TargetFrameworkAttribute::.ctor old\n")
            candidate.write_text(signature + "\n")
            self.assertTrue(source_api.compare(original, candidate)["passed"])
            candidate.write_text(signature.replace("Int32", "Int64") + "\n")
            with self.assertRaisesRegex(ValueError, "Source API changed"):
                source_api.compare(original, candidate)


class SourceProvenance(unittest.TestCase):
    def test_ancestor_checkout_cleanliness_and_parent_gitlink(self):
        with tempfile.TemporaryDirectory(dir=b.PORT / ".artifacts") as folder:
            root = Path(folder); repo = root / "External/SurvivalcraftApi"; repo.mkdir(parents=True)
            port = root / "Port"; port.mkdir()
            def git(*args): return b.run(["git", "-C", repo, *args], cwd=root)
            git("init")
            (repo / "file.cs").write_text("upstream\n")
            git("add", ".")
            git("-c", "user.name=Test", "-c", "user.email=test@example.invalid", "commit", "-m", "upstream")
            upstream = git("rev-parse", "HEAD")
            (repo / "file.cs").write_text("port\n")
            git("add", ".")
            git("-c", "user.name=Test", "-c", "user.email=test@example.invalid", "commit", "-m", "port")
            source = git("rev-parse", "HEAD")
            (port / "source-lock.json").write_text(json.dumps({"upstreamCommit": upstream, "sourceCommit": source}))
            b.run(["git", "init", root], cwd=root)
            b.run(["git", "-C", root, "update-index", "--add", "--cacheinfo", f"160000,{source},External/SurvivalcraftApi"], cwd=root)
            with patch.object(b, "ROOT", root), patch.object(b, "PORT", port), patch.object(b, "UPSTREAM", repo):
                source_guard.check_upstream(upstream)
                (repo / "new.cs").write_text("unreviewed")
                with self.assertRaisesRegex(RuntimeError, "Commit reviewed source"):
                    source_guard.check_upstream(upstream)
                (repo / "new.cs").unlink()
                with self.assertRaisesRegex(RuntimeError, "locked upstream"):
                    source_guard.check_upstream("0" * 40)
                b.run(["git", "-C", root, "update-index", "--cacheinfo", f"160000,{upstream},External/SurvivalcraftApi"], cwd=root)
                with self.assertRaisesRegex(RuntimeError, "gitlink"):
                    source_guard.check_upstream(upstream)
