"""Check registered GL/AL native callback prototypes against locked Silk 2.23 metadata.

AL uses calli signatures in generated vtable methods; ILSpy 10.1.0.8386 exposes
those for inspection without executing native code. This checks prototypes,
not implementation completeness or arbitrary low-level API compatibility.
"""
from pathlib import Path
import argparse
import re
import tempfile

import baseline as b

SUPPORT = b.PORT / "Compatibility/Desktop/Support/Engine/Properties"


def prototypes(path, prefix):
    text = path.read_text(encoding="utf-8-sig")
    matches = re.findall(r"delegate ([\w.]+) D_(" + prefix + r"\w+)\(([^)]*)\);", text)
    return {name: [ret, *([arg.rsplit(" ", 1)[0] for arg in args.split(", ")] if args else [])]
            for ret, name, args in matches}


def audit(workspace, output):
    gl = prototypes(SUPPORT / "UnityCallbacks.cs", "gl")
    al = prototypes(SUPPORT / "UnityAudioCallbacks.cs", "al")
    (workspace / "entry-points.txt").write_text("\n".join(sorted(gl)) + "\n")
    project = b.PORT / "Build/AbiAudit/AbiAudit.csproj"
    b.run(["dotnet", "restore", project, "--locked-mode", "--configfile", b.PORT / "Build/NuGet.Config"], log=workspace / "restore.log")
    b.run(["dotnet", "run", "--project", project, "--no-restore", "-c", "Release", "--",
           workspace / "entry-points.txt", workspace / "gl.json", output / "Silk.NET.OpenAL.dll",
           output / "Silk.NET.OpenGLES.dll"], log=workspace / "gl.log")
    expected_gl = b.read_json(workspace / "gl.json")
    if expected_gl["assemblyVersion"] != "2.23.0.0" or expected_gl["alAssemblyVersion"] != "2.23.0.0" or expected_gl["signatures"] != gl or len(gl) != 122:
        raise ValueError("GL native callback ABI differs from Silk 2.23.")
    text = b.run(["ilspycmd", "-t", "Silk.NET.OpenAL.AL", output / "Silk.NET.OpenAL.dll"], log=workspace / "al-decompiled.log")
    entries = dict(re.findall(r'"(al\w+)"\s*=>\s*(_\w+)', text))
    slots = {}
    for match in re.finditer(r"delegate\* unmanaged\[Cdecl\]<([^>]+)>\)\((?:\(\(NativeApiContainer\)this\)\.|base\.)CurrentVTable as _B\)\.(_\w+)", text):
        slots[match[2]] = [part.strip() for part in match[1].split(",")]
    def pointer(value):
        return "IntPtr" if "*" in value or value == "nint" else value
    expected_al = {name: [pointer(slots[slot][-1]), *map(pointer, slots[slot][:-1])]
                   for name, slot in entries.items() if slot in slots}
    if expected_al != al or len(al) != 68:
        raise ValueError("AL native callback ABI differs from the installed Silk assembly.")
    result = {"silkVersion": "2.23.0", "GL": {"callbacks": 122, "differences": 0},
              "AL": {"callbacks": 68, "differences": 0},
              "inputs": {name: b.digest((output / name).read_bytes()) for name in ("Silk.NET.OpenGLES.dll", "Silk.NET.OpenAL.dll")},
              "callbackSources": {name: b.digest((SUPPORT / name).read_bytes().replace(b"\r\n", b"\n"))
                                  for name in ("UnityCallbacks.cs", "UnityAudioCallbacks.cs")},
              "scope": "Registered callback prototypes only; unsupported native commands still fail."}
    b.write_json(workspace / "result.json", result)
    return result


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--assemblies", type=Path, default=b.ROOT / "Assets/SCUnity/Plugins/Generated")
    args = parser.parse_args()
    workspace = Path(tempfile.mkdtemp(prefix="abi-audit-", dir=b.PORT / ".artifacts"))
    print(audit(workspace, args.assemblies.resolve()))
    print("Evidence: " + str(workspace))
