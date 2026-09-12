# Unity source branch

This branch descends from SCAPI1.9 commit `98e5f58f779dba20503a095073451a40899549c4`.
The upstream history and original source paths are retained. Its three local UPM
packages (`Engine`, `EntitySystem`, `Survivalcraft`) are the editable runtime
sources for the SCUnity main project. The other platform projects are historical
upstream files, not the Unity build entry point.

Open the SCUnity project with Unity **6000.3.12f1**, Windows x64, Mono, .NET Framework.
Unity compiles the three named assemblies from these packages. No precompiled
Engine/EntitySystem/Survivalcraft DLL belongs in Assets. Third-party packages are
restored by `Port/Build/source.py` in the parent project.

The bundled Roslyn is 4.3.1; package-local `csc.rsp` enables C# 10. Unity's published
language support is C# 9, so this port intentionally requires the tested Editor
version and Windows Mono target. C# 10 preserves public parameterless struct
constructors, initializers, constant strings and global imports. Later language
features have been lowered to ordinary source. Do not replace the Editor compiler
or enable `preview` to accept new upstream changes without validation.

Engine resources remain actual manifest resources, with their original names.
Response-file resource paths expect the package checkout at
`External/SurvivalcraftApi`. The System.Text.Json 10.0.10 Roslyn 4.0 analyzer runs
inside Unity and generates the original release-information serializer.

The proven Desktop compatibility profile was applied once before source lowering,
including the managed window/render/audio adapters and community thumbnail
lifetime fix. The profile under the parent's `Port/Compatibility/Desktop` is now
a historical comparison baseline. Normal builds never regenerate these sources.
Edit this checkout directly and commit changes on this branch.

## Updating from upstream

Run in this submodule checkout, preserving the original paths:

```powershell
git switch feature/unity-source-runtime
git remote add upstream https://gitee.com/SC-SPM/SurvivalcraftApi.git # once per clone
git fetch upstream SCAPI1.9
git cherry-pick -x <reviewed-upstream-commit>
```

For a full update, merge the intended upstream branch instead of cherry-picking
every commit. Resolve overlapping language/runtime adaptations here, then test
the parent Unity project. Do not merge this unrelated source tree into the root
of the Unity project: the parent tracks a pinned submodule commit.

After acceptance, push this source branch to `origin`, update the parent's
`Port/source-lock.json` and gitlink, and commit/push the parent. The source fork
and parent currently use separate branches in the same GitHub repository; this
source branch has no nested submodules and does not recurse into the Unity project.

Existing .NET 10 mod binaries and non-Windows/IL2CPP/VR builds are not validated by
this source migration. See the parent project's acceptance evidence and roadmap.
