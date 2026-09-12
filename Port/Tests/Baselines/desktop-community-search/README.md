# Chinese community search regression

The previous installed DLLs reproduce the reported `glDrawElements` / dictionary
key `0` failure after clicking Search in `CommunityContentScreen`. The fixture
uses the real screen, mouse input, tree widgets and renderer. Only the community
server callbacks are replaced, with delayed replies and generated thumbnails;
no real community service, account, search data or downloads are involved.

The underlying lifetime error was in the pinned upstream widget code:
`TreeViewWidget.Clear` released node icons but kept the old child widgets until
`Draw`. `Widget.DrawContext` had already collected the old rows for that draw, so
they still referenced the disposed icons. The result cache also retained trees
that search/force refresh had disposed, allowing later cache hits to restore them.

The desktop source profile now detaches rows before releasing their textures and
rebuilds rows during measurement before draw-item collection. Search/force
refresh evicts cached lists sharing the disposed roots. Live cache hits still
work. Shared collection/entry icon disposal is repeatable, content-owned textures
are preserved, and replacing an icon refreshes its cached subtexture. These are
explicit behavior corrections, not claims of identity with the upstream bugs.
The pinned upstream checkout is unchanged. No missing-texture fallback hides
invalid draws.

Validation: Unity 6000.3.12f1, Windows x64 Mono, full main-project snapshot.
Both Player runs pass 17 community checks, 17 world/render/index checks and 12
audio checks. The community checks include three actual search-button clicks,
returning to an earlier query, a live cache hit, forced refresh, focus changes,
screen re-entry, shared icon ownership and red thumbnail pixels on the GPU.
The existing 60 tooling/compatibility tests and generated shader checks also pass.
See `evidence.json` for build, Editor Play/Stop results, input/output hashes and
artifact locations. `community-search.png` is the verified fixture screen.

```powershell
python Port/Build/desktop.py --unity <Unity.exe> --install --validate --community-test --world-test --audio-test
```

The server transport itself is not exercised by this regression. This does not
accept every community operation, asynchronous request race, or the renderer's
complete GPU resource/cache lifecycle. Existing desktop limitations, including
the 11 retarget MSBuild warnings, remain documented in the desktop README.
