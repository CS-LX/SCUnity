# Main-project core-loop acceptance

Unity 6000.3.12f1 Windows x64 Mono, built from a complete copy of the actual
`Assets`, `Packages`, and `ProjectSettings` directories. The user's open Editor
was not closed or used for CLI builds.

- Unity build: 0 errors, 0 warnings.
- Two independent Player runs: original loading → main menu → Unity Input System
  mouse click opens Settings → Escape returns → screenshot → clean host shutdown.
- Real URP draw counts: 1,509 and 1,625; both runs loaded 460 texture/buffer uploads.
- Two Editor Play/Stop cycles with Domain reload: original main menu reached in
  each cycle, no captured game/Unity errors.
- Writable settings resolve within the isolated persistent-data directory.
- Main project file fingerprints remained unchanged during validation.
- Independently regenerated Engine/EntitySystem/Survivalcraft DLLs are byte-identical.
- Existing tooling and compatibility test suite: 60 passed.

`evidence.json` includes source, installed asset, build, runtime and replay hashes.
Detailed logs and disposable builds remain local under
`Port/.artifacts/desktop-yhrpkm9z` and `Port/.artifacts/desktop-source-repeat`.
The normal scene runs continuously; smoke automation is opt-in by command line.

This accepts the menu/settings core loop only. It does not accept full game/world
rendering, Unity Audio output, complete platform input, all public APIs, or full
world/mod compatibility. Retarget MSBuild still reports 11 documented warnings;
see the Desktop compatibility README. These are separate from the clean Unity
Player build above.
