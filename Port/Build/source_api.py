"""Compare the existing Desktop API to Unity's source-compiled, unstripped API."""
import argparse
from pathlib import Path
import re
import baseline as b


def normalize(line: str) -> str | None:
    if line.startswith("#"): return None
    if re.match(r"attribute (Engine|EntitySystem|Survivalcraft) \[mscorlib\]System\.(Diagnostics.DebuggableAttribute|Runtime.Versioning.TargetFrameworkAttribute)::", line):
        return None  # Editor/Release debug flags and MSBuild-only TFM annotation.
    if line.startswith("attribute ") and " [mscorlib]System.Diagnostics.DebuggerStepThroughAttribute::" in line:
        return None  # Roslyn emits this on the same async methods in Debug builds.
    if line.startswith("attribute [Survivalcraft]Game.Animation.Drivers.LookAtDriver::get_") and any(
            "::get_" + name + " " in line for name in ("TargetBoneName", "MaxAngleX", "MaxAngleY")) and "CompilerGeneratedAttribute::" in line:
        return None  # The field-backed auto getters are now explicit getters.
    if "[Engine]Engine.Graphics.GLWrapper::DebugMessageDelegate " in line:
        return None  # Existing upstream #if DEBUG diagnostic callback.
    if line.startswith("type ") and ("ClassSemanticsMask, Abstract" in line
            or line.startswith("type [Survivalcraft]System.Collections.Generic.PriorityQueue`2 ")):
        line = line.replace(", BeforeFieldInit", "")  # Interface compiler flag; queue's existing DEBUG static ctor.
    if line.startswith("override [Survivalcraft]System.Collections.Generic.PriorityQueue`2+UnorderedItemsCollection "):
        line = line.replace("IEnumerable<(TElementElement,TPriorityPriority)>.GetEnumerator", "IEnumerable<System.ValueTuple<TElement,TPriority>>.GetEnumerator")
    return line


def compare(original: Path, candidate: Path) -> dict:
    old_lines = original.read_text(encoding="utf-8").splitlines()
    new_lines = candidate.read_text(encoding="utf-8").splitlines()
    old = {n for line in old_lines if (n := normalize(line)) is not None}
    new = {n for line in new_lines if (n := normalize(line)) is not None}
    if old != new:
        raise ValueError("Source API changed outside the reviewed compiler differences:\n" + "\n".join(
            ["- " + line for line in sorted(old - new)][:30] + ["+ " + line for line in sorted(new - old)][:30]))
    return {"passed": True, "comparedRecords": len(old), "originalRecords": len(old_lines), "candidateRecords": len(new_lines),
            "originalSha256": b.digest(original.read_bytes()), "candidateSha256": b.digest(candidate.read_bytes()),
            "policy": "source_api.py: explicit Roslyn, Debug and MSBuild metadata differences only; public signatures and layouts retained"}


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("original", type=Path)
    parser.add_argument("candidate", type=Path)
    parser.add_argument("output", type=Path)
    args = parser.parse_args()
    result = compare(args.original, args.candidate)
    b.write_json(args.output, result)
    print(result)
