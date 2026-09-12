# Main-project initial world acceptance

Unity 6000.3.12f1 Windows x64 Mono, built from a full copy of the main project's
Assets/Packages/ProjectSettings. Unity build: 0 errors, 0 warnings. Two independent
Player runs each pass 17 world/render/index checks and 12 audio checks, following
the menu/settings/Escape interaction. Draw totals: 37,580 and 39,596. Two Editor
Play/Stop cycles with Domain reload also pass. All 60 installed managed plugins
match the Player; all three game assemblies independently rebuild byte-identically.
The existing 60 tooling/compatibility tests pass.

The fixture uses the current serialization version, creative flat terrain, seed
123456, and a fixed player. It creates a stone column and the original Boat entity,
then saves, disposes, reloads and checks the player, seed, placed block and boat
drawing. Screenshots wait three real seconds after spawn for chunk fades to settle.
The screenshot visually verifies the grass, sky, hand, stone selection and boat.
Pixel assertions separately verify depth occlusion, bound depth attachments,
render-texture orientation and nonpremultiplied alpha. The original .NET 10 world
baseline used an empty serialization version; this current-world fixture is a new
scenario and is not falsely represented as identical to that earlier scenario.

Reproduce:

```powershell
python Port/Build/shaders.py --check
python Port/Build/desktop.py --unity <Unity.exe> --install --validate --audio-test --world-test
```

The reflection-only `Port/Tests/IndexBufferRegression` tool reproduces the original
IndexBuffer range-check failure without a native graphics context. Its two args
are the original Engine.dll path and output JSON; append `fixed` when inspecting
the corrected desktop DLL. In both cases invalid counts/offsets remain rejected.
Unity additionally verifies narrowing, widening, and that overflow/count errors
do not perform buffer uploads. See evidence.json for hashes and per-run results.
This is an explicitly documented upstream bug correction. Exceptions previously
swallowed by SubsystemDrawing now fail visibly through the Unity frame boundary.

This accepts the initial world path and listed GPU contracts. It does not accept
full original-versus-Unity image parity, complete terrain/transparent/skinned-model
coverage, arbitrary mod shaders, texture updates/mips/readbacks, all renderer states,
complete resource lifetime, full API/mod/platform input or legacy world compatibility.
The 11 retarget MSBuild warnings remain documented in the desktop README.
Detailed logs and builds remain in the artifact directories in evidence.json.
