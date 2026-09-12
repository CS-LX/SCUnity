# Desktop core-loop integration

This is the first integration into the main Unity project's `Assets/SCUnity`.
It is an incremental implementation, not full desktop migration acceptance.

`profile.json` replays 1,404 selected source files against upstream
`98e5f58f779dba20503a095073451a40899549c4`: 1,333 unchanged, 69 exact patches,
and two backend replacements. The 23 platform exclusions remain excluded from
compilation. `scope: complete` records source inventory coverage only; it does
not attest to complete runtime behavior or API equivalence.

`Support` contains 13 additional sources: Unity command/input boundaries and
Mono BCL compatibility helpers. `Projects` contains six project overrides.
Every input/output and support file is hash checked. NuGet restores use committed
locks. FLAC and ImageSharp are rebuilt using their previously validated source
modules. No files in the upstream submodule are edited.

Build and install with `python Port/Build/desktop.py --unity <Unity.exe> --install`.
Add `--validate` to build a full copy of the real main project and run the menu /
settings / Escape interaction twice, each with independent data and evidence.
The Player must have a visible normal window for URP execution; it closes after
capturing its result. `--validate-installed` verifies already installed files.
Add `--audio-test` to include actual listener PCM, static/streaming controls and
original music resource checks in both Player runs. The pure managed PCM mixer
advances source cursors only when Unity DSP consumes frames; decoding and stream
refills remain in the original Engine classes.
Add `--world-test` for original world creation, player/boat models, terrain edit,
save/dispose/reload, and pixel assertions for depth, target orientation and alpha.
`python Port/Build/shaders.py --check` verifies the six generated original HLSL
pairs (nine upstream asset pairs share six unique source identities).
All diagnostic workspaces and detailed logs are under `Port/.artifacts`.
Add `--community-test` to reproduce the Chinese community search/refresh path
with deterministic server replies and real PNG decoding, input and GPU drawing.
The 17 checks cover repeated searches, live result-cache reuse, force refresh,
screen re-entry, shared icon ownership and visible thumbnail pixels.

The main scene keeps the original `Engine`, `EntitySystem`, and `Survivalcraft`
assembly names and calls the original entry point. Unity owns the frame loop,
window, GPU and physical input. Silk GL/AL entry points resolve to managed command
adapters; no legacy EXE, GLFW window, OpenGL context, or embedded .NET Core runtime
is started. The GL adapter accepts shader declarations to preserve Engine's
parameter bindings. Six original HLSL pairs are built as Unity ShaderLab shaders,
including Unlit, Lit, terrain, Model and Highlight. The initial flat-world test
covers opaque terrain, sky, hand, boat and selection outline. It does not yet
establish baseline image equivalence for complete worlds or arbitrary mod shaders.

The Unity retarget maps ModsManager's writable Windows roots and screenshots to
`data:`. Content.zip and init.js remain under `app:`. Editor and Player use
`Application.persistentDataPath/Survivalcraft`; validation uses isolated paths.

## Remaining work

- Static PCM and original streaming music now reach Unity Audio. The 12 output/
  state assertions pass twice, plus two Editor Play/Stop cycles. This is not
  bit-exact OpenAL mixing or all low-level AL behavior; HRTF, Doppler, effects and
  arbitrary buffer formats are outside the implemented Engine command subset.
  See [audio acceptance](../../Tests/Baselines/desktop-audio/README.md).
- Initial world rendering and save/reload now pass; full world coverage, skinned
  models, mod shaders, texture updates/mips/readbacks, complete render-target/state
  semantics and resource lifetime still need implementation and validation.
- Input menu interactions pass; complete text/IME, focus, raw scan-code queries,
  low-level gamepad facades and full desktop window/platform services remain.
- Full API metadata comparison, broader BCL differential tests and
  generated/community mod runs have not been accepted for these three full DLLs.
- Retargeting currently produces 11 MSBuild warnings: Unity's vector facade versus
  dependency assembly version, nullable flow in PriorityQueue, an obsolete override
  and placeholder native-dialog fields. Unity's API updater also reports the
  existing Harmony/MonoMod assembly-reference cycle. These are recorded limitations,
  not suppressed compatibility evidence.
- All 122 registered GL and 68 registered AL callback prototypes have been
  compared with the installed Silk 2.23 assemblies, with zero differences.
  `python Port/Build/abi_audit.py` repeats this metadata-only check (requires
  ilspycmd 10.1.0.8386). It does not establish implementation completeness.

`Support/Survivalcraft/Properties/PriorityQueue.cs` derives from the MIT-licensed
.NET runtime v10.0.0 implementation at
<https://github.com/dotnet/runtime/blob/v10.0.0/src/libraries/System.Collections/src/System/Collections/Generic/PriorityQueue.cs>.
Its original license header is preserved. Unity lacks several modern BCL types;
the extra `Lock`, `IReadOnlySet` and queue types are provisional compatibility
surfaces and require explicit scope registration in the final API policy.

Legacy executable updater/MOTD polling, registry associations and process restart
are disabled at the main Unity entry until replacements are ready. Unsupported
graphics and platform calls must fail visibly rather than count as passed tests.

## Explicit behavior correction

The pinned upstream `IndexBuffer.VerifyParametersSetData` checks source byte width
before `SetData` converts int/uint indices into 16 bit indices. A valid six-index
upload into six slots therefore throws and the original `SubsystemDrawing`
swallows it, making the boat invisible. The desktop patch checks converted upload
width. This is a confirmed upstream bug correction, not claimed behavior identity.
A reflection-only probe reproduces the failure in the original .NET 10 DLL and
verifies the corrected behavior without creating a GL context. Unity tests also
exercise real narrowed/widened uploads and reject index overflow or out-of-range
counts before any upload. `SubsystemDrawing` now forwards caught draw failures to
the Unity host, which fails the frame visibly.

Original source data and exact edits remain hash-checked; no upstream checkout
was edited. See [world evidence](../../Tests/Baselines/desktop-world/README.md).

The community search crash is another explicit upstream behavior correction.
`TreeViewWidget` previously freed icons in `Clear` but removed their old rows only
inside `Draw`, after `Widget.DrawContext` had already collected those rows. Search
then rendered a disposed texture with handle zero. Rows now detach before icons
are released, and node rows rebuild during measurement before draw collection.
Search/force refresh also evict cache entries referencing trees being disposed;
cache hits continue to reuse live trees. Node disposal preserves content-owned
textures, tolerates collection/entry thumbnail sharing, and clears old subtextures.
See [search regression evidence](../../Tests/Baselines/desktop-community-search/README.md).
