"""Copy user-specified saves/mods into a fresh local fixture set; never edit originals."""
from __future__ import annotations
import argparse
from pathlib import Path
import shutil
import tempfile
import xml.etree.ElementTree as ET
import zipfile
import baseline


def collect(source: Path) -> Path:
    source = source.resolve(strict=True)
    artifacts = baseline.PORT / ".artifacts"
    artifacts.mkdir(exist_ok=True)
    output = Path(tempfile.mkdtemp(prefix="user-samples-", dir=artifacts))
    for name in ("mods", "worlds", "world-archives"):
        (output / name).mkdir()
    records = []
    def copy(file: Path, destination: Path):
        if file.is_symlink() or not file.resolve().is_relative_to(source):
            raise RuntimeError(f"Sample escapes the specified source: {file}")
        data = file.read_bytes()
        destination.parent.mkdir(parents=True, exist_ok=True)
        destination.write_bytes(data)
        sha = baseline.digest(data)
        if baseline.digest(file.read_bytes()) != sha:
            raise RuntimeError(f"Sample changed during capture: {file}")
        records.append({"source": file.relative_to(source).as_posix(), "path": destination.relative_to(output).as_posix(),
                        "size": len(data), "sha256": sha})
    for file in sorted(source.rglob("*.scmod")):
        relative = file.relative_to(source).as_posix()
        copy(file, output / "mods" / (baseline.digest(relative.encode())[:8] + "-" + file.name))
    for file in sorted(source.rglob("*.scworld")):
        relative = file.relative_to(source).as_posix()
        copy(file, output / "world-archives" / (baseline.digest(relative.encode())[:8] + ".scworld"))
    for project in sorted(source.rglob("Project.xml")):
        relative = project.parent.relative_to(source).as_posix()
        target = output / "worlds" / baseline.digest(relative.encode())[:8]
        for file in sorted(project.parent.rglob("*")):
            if file.is_file(): copy(file, target / file.relative_to(project.parent))
    worlds, mods = [], []
    for file in (output / "world-archives").glob("*.scworld"):
        with zipfile.ZipFile(file) as archive:
            root = ET.fromstring(archive.read("Project.xml"))
            worlds.append({"path": file.relative_to(output).as_posix(), "attributes": root.attrib})
    for file in (output / "worlds").glob("*/Project.xml"):
        worlds.append({"path": file.relative_to(output).as_posix(), "attributes": ET.parse(file).getroot().attrib})
    for file in (output / "mods").glob("*.scmod"):
        with zipfile.ZipFile(file) as archive:
            names = archive.namelist()
            mods.append({"path": file.relative_to(output).as_posix(), "entryCount": len(names),
                         "dlls": [n for n in names if n.lower().endswith(".dll")],
                         "shaders": [n for n in names if n.lower().endswith((".vsh", ".psh"))]})
    manifest = {"schemaVersion": 1, "sourceDirectory": str(source), "status": "captured-not-yet-runtime-validated",
                "files": records, "worlds": worlds, "mods": mods}
    baseline.write_json(output / "manifest.json", manifest)
    print(f"Captured {len(mods)} mods and {len(worlds)} world samples: {output}", flush=True)
    return output


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("source", type=Path)
    collect(parser.parse_args().source)
