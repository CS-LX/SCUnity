"""Validate measured reference traces; do not mistake reaching a screen for compatibility."""
from __future__ import annotations
from collections import Counter, defaultdict
import json
from pathlib import Path


def validate(evidence: Path) -> dict:
    result = json.loads((evidence / "result.json").read_text(encoding="utf-8"))
    if result["status"] != "passed" or not result["completed"] or result["loggedErrors"]:
        raise RuntimeError("Reference runtime did not complete cleanly")
    events = [json.loads(line) for line in (evidence / "trace.jsonl").read_text(encoding="utf-8").splitlines()]
    per_frame = defaultdict(list)
    modules = ["Engine.Time", "Engine.Dispatcher", "Engine.Graphics.Display", "Engine.Input.Keyboard",
               "Engine.Input.Mouse", "Engine.Input.Touch", "Engine.Input.GamePad", "Engine.Audio.Mixer"]
    expected = ["Engine.Window.BeforeFrameAll", *(m + ".BeforeFrame" for m in modules), "Game.Program.FrameHandler",
                "Engine.Window.AfterFrameAll", *(m + ".AfterFrame" for m in modules)]
    for event in events:
        if event["kind"] == "begin" and event["data"] in expected:
            per_frame[event["frame"]].append(event["data"])
    if len(per_frame) != result["frameCount"]:
        raise RuntimeError("Missing frame records")
    for frame, calls in per_frame.items():
        if calls != expected:
            raise RuntimeError(f"Core frame sequence drift in frame {frame}: {calls}")
    begins = [e["data"] for e in events if e["kind"] == "loading-action-begin"]
    ends = [e["data"] for e in events if e["kind"] == "loading-action-end"]
    if not begins or begins != ends:
        raise RuntimeError("Incomplete or reordered loading actions")
    if result["checks"] and not all(c["passed"] for c in result["checks"]):
        raise RuntimeError("A runtime assertion failed")
    ecs = [e for e in events if e["kind"] == "ecs-call"]
    if result["scenario"] == "world":
        if not ecs or not any(e["data"]["method"] == "Draw" for e in ecs):
            raise RuntimeError("World scenario did not sample actual ECS update and draw calls")
        before = json.loads((evidence / "before-reload.json").read_text())
        after = json.loads((evidence / "after-reload.json").read_text())
        before.pop("allocatedChunks")
        after.pop("allocatedChunks")
        if before != after:
            raise RuntimeError("Persistent player/world snapshot differs after reload")
    mods = next((e["data"] for e in events if e["kind"] == "mods"), [])
    return {"schemaVersion": 1, "status": "passed", "scenario": result["scenario"], "frameCount": len(per_frame),
            "coreFrameSequence": expected, "loadingActions": begins, "assertions": len(result["checks"]),
            "sampledEcsCalls": len(ecs), "sampledEcsFrames": sorted({e["frame"] for e in ecs}),
            "sampledEcsTypes": dict(sorted(Counter(e["data"]["type"] for e in ecs).items())),
            "mods": [{"name": m["name"], "disabled": m["IsDisabled"], "reason": m["reason"],
                       "blockTypeCount": len(m["blockTypes"]), "loaders": m["loaders"]} for m in mods],
            "determinism": "Ordered frame contract validated; wall-clock duration, startup frame count and full physics state are not asserted deterministic."}
