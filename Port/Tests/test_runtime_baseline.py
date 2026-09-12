"""Exercise trace gates using captured reference data, without starting the game."""
import gzip
import json
from pathlib import Path
import shutil
import sys
import tempfile
import unittest

PORT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(PORT / "Build"))
import baseline
from validate_runtime import validate
from runtime_baseline import verify_samples


class RuntimeGuards(unittest.TestCase):
    def setUp(self):
        (PORT / ".artifacts").mkdir(exist_ok=True)
        self.root = Path(tempfile.mkdtemp(prefix="runtime-guard-", dir=PORT / ".artifacts"))
        reference = PORT / "Tests/Baselines/generated"
        for name in ("result.json", "before-reload.json", "after-reload.json"):
            shutil.copyfile(reference / name, self.root / name)
        self.events = [json.loads(line) for line in gzip.decompress((reference / "trace.jsonl.gz").read_bytes()).decode().splitlines()]
        self.save_trace()

    def save_trace(self):
        (self.root / "trace.jsonl").write_text("\n".join(json.dumps(e) for e in self.events) + "\n", encoding="utf-8")

    def test_recorded_reference_passes(self):
        result = validate(self.root)
        self.assertGreater(result["sampledEcsCalls"], 0)
        self.assertGreater(result["assertions"], 30)

    def test_recorded_sources_and_evidence_are_locked(self):
        directory = PORT / "Tests/Baselines"
        lock = baseline.read_json(directory / "runtime-lock.json")
        self.assertEqual(baseline.digest((PORT / "upstream-lock.json").read_text(encoding="utf-8").encode()), lock["upstreamLockSha256"])
        for path, sha in lock["sources"].items():
            self.assertEqual(baseline.digest((PORT / path).read_text(encoding="utf-8").encode()), sha, path)
        for path, sha in lock["evidence"].items():
            file = directory / path
            data = file.read_text(encoding="utf-8").encode() if file.suffix == ".json" else file.read_bytes()
            self.assertEqual(baseline.digest(data), sha, path)

    def test_missing_small_frame_method_fails(self):
        index = next(i for i,e in enumerate(self.events) if e["kind"] == "begin" and e["data"] == "Engine.Dispatcher.BeforeFrame")
        del self.events[index]
        self.save_trace()
        with self.assertRaisesRegex(RuntimeError, "sequence drift"):
            validate(self.root)

    def test_swapped_update_order_fails(self):
        a = next(i for i,e in enumerate(self.events) if e["kind"] == "begin" and e["data"] == "Engine.Input.Keyboard.BeforeFrame")
        b = next(i for i,e in enumerate(self.events) if e["kind"] == "begin" and e["data"] == "Engine.Input.Mouse.BeforeFrame")
        self.events[a], self.events[b] = self.events[b], self.events[a]
        self.save_trace()
        with self.assertRaisesRegex(RuntimeError, "sequence drift"):
            validate(self.root)

    def test_incomplete_loading_action_fails(self):
        self.events.pop(next(i for i,e in enumerate(self.events) if e["kind"] == "loading-action-end"))
        self.save_trace()
        with self.assertRaisesRegex(RuntimeError, "loading actions"):
            validate(self.root)

    def test_missing_ecs_draw_sample_fails(self):
        self.events = [e for e in self.events if e["kind"] != "ecs-call" or e["data"]["method"] != "Draw"]
        self.save_trace()
        with self.assertRaisesRegex(RuntimeError, "ECS"):
            validate(self.root)

    def test_player_state_loss_fails(self):
        path = self.root / "after-reload.json"
        state = baseline.read_json(path)
        state["players"][0]["Level"] += 1
        baseline.write_json(path, state)
        with self.assertRaisesRegex(RuntimeError, "snapshot differs"):
            validate(self.root)

    def test_unexpected_game_error_cannot_pass(self):
        path = self.root / "result.json"
        result = baseline.read_json(path)
        result["loggedErrors"].append("unexpected regression")
        baseline.write_json(path, result)
        with self.assertRaisesRegex(RuntimeError, "cleanly"):
            validate(self.root)

    def test_changed_original_or_copy_rejected(self):
        samples = self.root / "samples"
        source = self.root / "original"
        samples.mkdir(); source.mkdir()
        (source / "save.scworld").write_bytes(b"original")
        (samples / "save.scworld").write_bytes(b"original")
        baseline.write_json(samples / "manifest.json", {"sourceDirectory": str(source), "files": [
            {"path": "save.scworld", "source": "save.scworld", "sha256": baseline.digest(b"original")}]})
        verify_samples(samples)
        (source / "save.scworld").write_bytes(b"user edited")
        with self.assertRaisesRegex(RuntimeError, "sample changed"):
            verify_samples(samples)
        (source / "save.scworld").write_bytes(b"original")
        (samples / "save.scworld").write_bytes(b"copy corrupted")
        with self.assertRaisesRegex(RuntimeError, "sample changed"):
            verify_samples(samples)


if __name__ == "__main__":
    unittest.main(verbosity=2)
