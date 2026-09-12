# Unity source compilation acceptance

Accepted with Unity 6000.3.12f1, Windows x64 Mono and the main project's existing
settings (including managed stripping). Source commit is recorded in `evidence.json`.

- Strict Player build: **0 errors, 0 warnings**.
- Unity CompilationPipeline: Engine **336**, EntitySystem **20**, Survivalcraft
  **1062** source inputs. No precompiled core DLL in Assets.
- Two independent Players: **14 source + 17 community + 17 world/GPU/save-reload
  + 12 audio** checks each, with **44,036 / 45,123** executed draws.
- Two Editor Play/Stop cycles with Domain Reload passed.
- **62** Python guard/metadata/profile tests passed.
- **48,215** API records match the Desktop reference after the explicitly reviewed
  compiler/Debug metadata normalization in `Port/Build/source_api.py`.

`desktop-api.txt.gz` preserves the pre-migration Desktop metadata oracle; its DLL
fingerprints are in `evidence.json`. `project-files.json.gz` records the complete
tested project/source snapshot. `world.png` is the final saved/reloaded world and
`community-search.png` is the real fixture thumbnail rendered by Unity. Community
server replies are fixtures; networking/account actions were not sent to the service.

Detailed logs, linked Player binaries, API snapshots and isolated data remain in
`Port/.artifacts/source-ru4or8yn`. Source-related documentation was finalized after
the runtime snapshot; no runtime or project-setting change followed acceptance.
Third-party dependencies were rebuilt with the independent locked dependency
project before this run. Core compilation used only Unity's bundled compiler.

The scope is source migration plus the established core-loop baseline, not full
world rendering equivalence, all mod compatibility, or other platform support.
