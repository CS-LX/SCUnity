"""Scoped metadata gate; explicitly normalize reviewed .NET 10 / Unity BCL retargeting only."""
from collections import Counter
from pathlib import Path
import re

import baseline as b

BCL_SCOPES = {"mscorlib", "System", "System.Core", "System.Runtime", "System.Runtime.InteropServices",
              "System.Collections", "System.Collections.Concurrent", "System.Numerics", "System.Numerics.Vectors",
              "System.Xml.Linq", "System.Xml.XDocument", "System.Threading"}
PRIMITIVE_TYPES = "Boolean Byte SByte Char DateTime Decimal Double Enum Guid Int16 Int32 Int64 Single String UInt16 UInt32 UInt64 Version".split()
DRAWING_TYPES = "Color Point PointF Rectangle RectangleF Size SizeF".split()
CORE10 = "System.Runtime, Version=10.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a"
CORE48 = "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"
DRAW10 = "System.Drawing.Primitives, Version=10.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a"
DRAW48 = "System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a"
TFM_VALUES = {
    "0100192E4E4554436F72654170702C56657273696F6E3D7631302E300100540E144672616D65776F726B446973706C61794E616D65092E4E45542031302E30",
    "01001A2E4E45544672616D65776F726B2C56657273696F6E3D76342E380100540E144672616D65776F726B446973706C61794E616D6500",
}


def serialized_string(value: str) -> bytes:
    data = value.encode("utf-8")
    size = len(data)
    if size < 128:
        return bytes([size]) + data
    if size < 16384:
        return bytes([0x80 | (size >> 8), size & 255]) + data
    raise ValueError("Unexpected serialized type-name length")


def normalize(line: str) -> str | None:
    line = re.sub(r"\[([^\]]+)\]", lambda m: "[BCL]" if m[1] in BCL_SCOPES else m[0], line)
    # Compiler embeds this internal marker when the target BCL lacks it.
    line = line.replace("[Engine]System.Runtime.CompilerServices.IsUnmanagedAttribute::.ctor", "[BCL]System.Runtime.CompilerServices.IsUnmanagedAttribute::.ctor")
    # Full Engine contains extension methods outside the selected source subset.
    if line == "attribute Engine [BCL]System.Runtime.CompilerServices.ExtensionAttribute::.ctor Void () header=20 genericArity=0 requiredParameters=0 value=01000000":
        return None
    if re.match(r"attribute (Engine|EntitySystem) \[BCL\]System.Runtime.Versioning.TargetFrameworkAttribute::", line):
        prefix, blob = line.rsplit(" value=", 1)
        if blob not in TFM_VALUES:
            raise ValueError("Unregistered assembly target framework metadata")
        return prefix + " value=<reviewed-net10-to-net48>"
    # Preserve constructor, target types, array length, named arguments and all other bytes.
    # Only exact assembly-qualified BCL type strings may change in these known blob kinds.
    marker = None
    if line.startswith("attribute ") and " [Engine]Engine.Serialization.HumanReadableConverterAttribute::.ctor " in line:
        marker = " value="
    elif re.match(r"security (Engine|EntitySystem) action=RequestMinimum permissions=", line):
        marker = " permissions="
    if marker:
        prefix, encoded = line.rsplit(marker, 1)
        data = bytes.fromhex(encoded)
        families = [("System." + t, (CORE10, CORE48)) for t in PRIMITIVE_TYPES]
        families += [("System.Drawing." + t, (DRAW10, DRAW48)) for t in DRAWING_TYPES]
        families += [("System.Security.Permissions.SecurityPermissionAttribute", (CORE10, CORE48))]
        for name, assemblies in families:
            for assembly in assemblies:
                data = data.replace(serialized_string(name + ", " + assembly), serialized_string(name + ", [BCL]"))
        line = prefix + marker + data.hex().upper()
    return line


def owner(line: str) -> str:
    return line.split(" ", 2)[1].split("::", 1)[0]


def expected_types(original: list[str], profile: dict) -> set[str]:
    # Derive selection from the reviewed SOURCE profile, never from candidate output.
    core = set()
    for rule in profile["sources"]:
        path = Path(rule["path"])
        if path.parent.as_posix() in ("Engine/Engine", "Engine/Engine.Graphics"):
            namespace = "Engine.Graphics." if path.parent.name == "Engine.Graphics" else "Engine."
            core.add("[Engine]" + namespace + path.stem)
    return {owner(line) for line in original if line.startswith("type ") and (
        owner(line).startswith(("[EntitySystem]", "[Engine]Engine.Serialization."))
        or owner(line).split("+", 1)[0].split("`", 1)[0] in core)}


def compare(original: list[str], candidate: list[str], profile: dict) -> dict:
    types = expected_types(original, profile)
    actual = {owner(line) for line in candidate if line.startswith("type ")}
    if actual != types:
        raise ValueError(f"Public type inventory changed: missing={sorted(types-actual)}, extra={sorted(actual-types)}")
    def selected(line: str) -> bool:
        return not line.startswith("#") and (owner(line) in types or owner(line) in ("Engine", "EntitySystem"))
    left = Counter(n for line in original if selected(line) for n in [normalize(line)] if n is not None)
    right = Counter(n for line in candidate if not line.startswith("#") for n in [normalize(line)] if n is not None)
    if left != right:
        raise ValueError("Unregistered public API drift:\n" + "\n".join(["- " + x for x in (left-right)] + ["+ " + x for x in (right-left)])[:16000])
    return {"engineTypes": sum(t.startswith("[Engine]") for t in types), "entitySystemTypes": sum(t.startswith("[EntitySystem]") for t in types),
            "normalizedRecords": left.total(), "unregisteredDifferences": 0}


def verify(workspace: Path, projects: Path, originals: Path) -> dict:
    tool = b.PORT / "Build/ApiSnapshot"
    b.run(["dotnet", "build", tool / "ApiSnapshot.csproj", "-c", "Release", "--configfile", b.PORT / "Build/NuGet.Config"], log=workspace / "api-tool.log")
    exe = tool / "bin/Release/net10.0/ApiSnapshot.dll"
    # Recheck original full metadata against the locked baseline before using it as oracle.
    full = workspace / "original-api.txt"
    b.run(["dotnet", exe, full, *(originals / f"{n}.dll" for n in ("Engine", "EntitySystem", "Survivalcraft"))], log=workspace / "original-api.log")
    b.assert_equal(b.PORT / "Compatibility/PublicApi.Shipped.txt", full)
    candidate = workspace / "foundation-api.txt"
    output = projects / "Runner/bin/Release/net48"
    b.run(["dotnet", exe, candidate, output / "Engine.dll", output / "EntitySystem.dll"], log=workspace / "foundation-api.log")
    result = compare(full.read_text(encoding="utf-8-sig").splitlines(), candidate.read_text(encoding="utf-8-sig").splitlines(),
                     b.read_json(b.PORT / "Compatibility/Profiles/foundation.json"))
    b.write_json(workspace / "api-comparison.json", result)
    return result
