"""Run the original Windows reference in an isolated, instrumented process."""
from __future__ import annotations
import argparse
import json
import os
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile
import zipfile
import baseline
from validate_runtime import validate


class Utf8ZipInfo(zipfile.ZipInfo):
    def _encodeFilenameFlags(self):
        return self.filename.encode("utf-8"), self.flag_bits | 0x800


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--baseline", required=True, type=Path, help="Evidence directory from a successful full baseline verify")
    parser.add_argument("--scenario", choices=("menu", "world"), default="menu")
    parser.add_argument("--samples", type=Path, help="An isolated sample set produced by collect_runtime_samples.py")
    parser.add_argument("--mods", choices=("none", "generated", "community"), default="none")
    args = parser.parse_args()
    baseline.check_pinned_files(baseline.read_json(baseline.PORT / "upstream-lock.json"))
    baseline.check_upstream(baseline.SHA)
    reference = args.baseline.resolve()
    result = baseline.read_json(reference / "result.json")
    if result["result"] != "passed" or result["upstreamCommit"] != baseline.SHA:
        raise RuntimeError("Invalid reference build evidence")
    source = reference / "upstream/Survivalcraft.Windows/bin/Release/net10.0-windows/win-x64"
    for assembly in result["assemblies"]:
        if baseline.digest((source / assembly["name"]).read_bytes()) != assembly["sha256"]:
            raise RuntimeError("Reference DLL hash mismatch")
    workspace = Path(tempfile.mkdtemp(prefix="runtime-", dir=baseline.PORT / ".artifacts"))
    binary = workspace / "bin"
    shutil.copytree(source, binary)
    evidence = workspace / "evidence"
    evidence.mkdir()
    if args.samples:
        verify_samples(args.samples.resolve())
        shutil.copytree(args.samples.resolve() / "world-archives", evidence / "input")
        for directory in (args.samples.resolve() / "worlds").iterdir():
            with zipfile.ZipFile(evidence / "input" / ("world-" + directory.name + ".scworld"), "w") as archive:
                for file in sorted(directory.rglob("*")):
                    if file.is_file():
                        entry = Utf8ZipInfo(file.relative_to(directory).as_posix(), (2000, 1, 1, 0, 0, 0))
                        entry.compress_type = zipfile.ZIP_DEFLATED
                        archive.writestr(entry, file.read_bytes())
    if args.mods == "community":
        if not args.samples: raise RuntimeError("Community mods require --samples")
        shutil.copytree(args.samples.resolve() / "mods", binary / "Mods")
    elif args.mods == "generated":
        build_generated_mods(binary, workspace)
    print(f"Runtime evidence: {evidence}", flush=True)
    project = baseline.PORT / "Tests/RuntimeProbe/RuntimeProbe.csproj"
    baseline.run(["dotnet", "build", project, "-c", "Release", "--nologo", f"-p:BaselineOutput={binary}",
                  "-o", workspace / "probe", "--configfile", baseline.PORT / "Build/NuGet.Config"], log=workspace / "probe-build.log")
    shutil.copyfile(workspace / "probe/RuntimeProbe.dll", binary / "RuntimeProbe.dll")
    environment = os.environ.copy()
    environment.update(DOTNET_TieredCompilation="0", DOTNET_ReadyToRun="0", DOTNET_JitNoInline="1")
    command = ["dotnet", "exec", "--runtimeconfig", str(binary / "Survivalcraft.runtimeconfig.json"),
               "--depsfile", str(binary / "Survivalcraft.deps.json"), str(binary / "RuntimeProbe.dll"), str(evidence), args.scenario, args.mods]
    with (workspace / "runtime.log").open("w", encoding="utf-8") as log:
        process = subprocess.Popen(command, cwd=binary, env=environment, stdout=log, stderr=subprocess.STDOUT,
                                   creationflags=subprocess.CREATE_NO_WINDOW)
        try:
            code = process.wait(timeout=180)
        except subprocess.TimeoutExpired:
            process.kill()
            process.wait()
            raise RuntimeError(f"Reference runtime timed out; see {workspace / 'runtime.log'}")
    if code:
        raise RuntimeError((workspace / "runtime.log").read_text(encoding="utf-8")[-16000:])
    summary = validate(evidence)
    baseline.write_json(evidence / "validation.json", summary)
    # Keep the precise inputs and probe identity with the measured outputs.
    baseline.write_json(evidence / "provenance.json", {"upstreamCommit": baseline.SHA,
        "referenceAssemblies": result["assemblies"], "scenario": args.scenario, "mods": args.mods,
        "probeSha256": baseline.digest((binary / "RuntimeProbe.dll").read_bytes()),
        "jit": {"TieredCompilation": False, "ReadyToRun": False, "NoInline": True},
        "modPackages": [{"path": f.relative_to(binary).as_posix(), "sha256": baseline.digest(f.read_bytes())} for f in sorted((binary / "Mods").rglob("*.scmod"))],
        "sampleManifestSha256": baseline.digest((args.samples / "manifest.json").read_bytes()) if args.samples else None})
    if args.samples: verify_samples(args.samples.resolve())
    baseline.check_upstream(baseline.SHA)
    print(f"PASS: {summary['assertions']} assertions, {summary['frameCount']} ordered frames, {summary['sampledEcsCalls']} sampled ECS calls. Evidence: {evidence}")


def verify_samples(samples: Path):
    manifest = baseline.read_json(samples / "manifest.json")
    source = Path(manifest["sourceDirectory"])
    for entry in manifest["files"]:
        for root, relative in ((samples, entry["path"]), (source, entry["source"])):
            file = (root / relative).resolve()
            if not file.is_relative_to(root.resolve()) or baseline.digest(file.read_bytes()) != entry["sha256"]:
                raise RuntimeError(f"User sample changed: {file}")


def build_generated_mods(binary: Path, workspace: Path):
    project = baseline.PORT / "Tests/ModFixture/ModFixture.csproj"
    baseline.run(["dotnet", "build", project, "-c", "Release", "--nologo", f"-p:BaselineOutput={binary}",
                  "-o", workspace / "mod-build", "--configfile", baseline.PORT / "Build/NuGet.Config"], log=workspace / "mod-build.log")
    definitions = [
        ("resources", 100, {}, {"Assets/PortProbe/winner.txt": b"first"}),
        ("code", -100, {"scunity.probe.resources": "[1.0.0,2.0.0)"}, {
            "PortProbeMod.dll": (workspace / "mod-build/PortProbeMod.dll").read_bytes(),
            "Assets/PortProbe/winner.txt": b"code", "Assets/PortProbe/payload.probe": b"payload",
            "Assets/PortProbeBlocks.csv": b"Class Name;DefaultDisplayName\nPortProbeBlock;Port Baseline Block\n",
            "Assets/PortProbeDatabase.xdb": b'<Mod><Parameter Name="Color" Guid="480fe1d2-2474-4fa9-a984-9a999d61b1c9" new-Value="0,0,0" Type="Color" /></Mod>',
            "probe.js": b"globalThis.portProbeScript = 17; globalThis.portProbeFrames = 0; frameHandlers.push(function(){ globalThis.portProbeFrames++; });"}),
        ("late", -200, {}, {"Assets/PortProbe/winner.txt": b"late",
            "Assets/Lang/en-US.json": b'{"PortProbe":{"Value":"late"}}',
            "Assets/Lang/zh-CN.json": b'{"PortProbe":{"Value":"late"}}'}),
        ("broken", 200, {}, {"BrokenForBaseline.dll": b"deliberately invalid managed assembly"})]
    for name, order, dependencies, files in definitions:
        info = {"Name": "Port Probe " + name, "Version": "1.0.0", "ApiVersion": "1.9", "PackageName": "scunity.probe." + name,
                "LoadOrder": order, "Dependencies": dependencies, "NonPersistentMod": True}
        files["modinfo.json"] = json.dumps(info).encode()
        path = binary / "Mods/nested" / (name + ".scmod")
        path.parent.mkdir(parents=True, exist_ok=True)
        with zipfile.ZipFile(path, "w", zipfile.ZIP_DEFLATED) as archive:
            for key, data in sorted(files.items()):
                entry = Utf8ZipInfo(key, (2000, 1, 1, 0, 0, 0))
                entry.compress_type = zipfile.ZIP_DEFLATED
                # Upstream requires the UTF-8 filename flag even for ASCII names.
                entry.flag_bits |= 0x800
                archive.writestr(entry, data)


if __name__ == "__main__":
    try:
        main()
    except Exception as error:
        print(f"FAIL: {error}", file=sys.stderr)
        sys.exit(1)
