# Desktop core-loop integration

This is the first integration into the main Unity project's `Assets/SCUnity`.
It is an incremental implementation, not full desktop migration acceptance.

`profile.json` replays 1,404 selected source files against upstream
`98e5f58f779dba20503a095073451a40899549c4`: 1,336 unchanged, 66 exact patches,
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
All diagnostic workspaces and detailed logs are under `Port/.artifacts`.

The main scene keeps the original `Engine`, `EntitySystem`, and `Survivalcraft`
assembly names and calls the original entry point. Unity owns the frame loop,
window, GPU and physical input. Silk GL/AL entry points resolve to managed command
adapters; no legacy EXE, GLFW window, OpenGL context, or embedded .NET Core runtime
is started. The GL adapter accepts shader declarations to preserve Engine's
parameter bindings; actual GPU validation currently covers only the original
Unlit HLSL translated to Unity ShaderLab. Acceptance does not extend to arbitrary
GL entry points, shaders, shader compilation, or mod graphics.

The Unity retarget maps ModsManager's writable Windows roots and screenshots to
`data:`. Content.zip and init.js remain under `app:`. Editor and Player use
`Application.persistentDataPath/Survivalcraft`; validation uses isolated paths.

## Remaining work

- Audio currently decodes and tracks playback commands without Unity Audio output.
- World/model/terrain and mod shaders, texture updates/mips/readbacks, complete
  render-target/state semantics and resource lifetime need implementation and
  validation. A menu screenshot is not a world-rendering pass.
- Input menu interactions pass; complete text/IME, focus, raw scan-code queries,
  low-level gamepad facades and full desktop window/platform services remain.
- Full API metadata comparison, BCL differential tests, world save/reload and
  generated/community mod runs have not been accepted for these three full DLLs.
- Retargeting currently produces 11 MSBuild warnings: Unity's vector facade versus
  dependency assembly version, nullable flow in PriorityQueue, an obsolete override
  and placeholder native-dialog fields. Unity's API updater also reports the
  existing Harmony/MonoMod assembly-reference cycle. These are recorded limitations,
  not suppressed compatibility evidence.
- GL/AL callback prototypes originated from Silk 2.22 metadata; the actual locked
  dependencies are 2.23. The exercised menu calls work; a complete ABI audit against
  2.23 is still required.

`Support/Survivalcraft/Properties/PriorityQueue.cs` derives from the MIT-licensed
.NET runtime v10.0.0 implementation at
<https://github.com/dotnet/runtime/blob/v10.0.0/src/libraries/System.Collections/src/System/Collections/Generic/PriorityQueue.cs>.
Its original license header is preserved. Unity lacks several modern BCL types;
the extra `Lock`, `IReadOnlySet` and queue types are provisional compatibility
surfaces and require explicit scope registration in the final API policy.

Legacy executable updater/MOTD polling, registry associations and process restart
are disabled at the main Unity entry until replacements are ready. Unsupported
graphics and platform calls must fail visibly rather than count as passed tests.
