# SCUnity

Survivalcraft API 的 Unity Windows 桌面迁移工程。Unity 版本：6000.3.12f1。

当前已接入原版加载、主菜单和设置页面的核心循环，使用 Unity Mono、URP 和 Input System。
原版静态音效与流式音乐已接入 Unity Audio 并通过输出采样验收。初始平坦世界的地形、天空、手臂、船模型与保存重载已接通；完整世界视觉对照和模组/API 回归仍在开发中。

## 直接运行

三个核心程序集已改为 Unity 直接编译源码，源码包位于 `External/SurvivalcraftApi/{Engine,EntitySystem,Survivalcraft}`，在 Project 窗口的 Packages 中可见。第三方依赖与原版资源已安装到主项目。打开 `Assets/SCUnity/Scenes/Survivalcraft.unity`，点击 Unity Play。
也可以使用菜单 `Survivalcraft > Open Game Scene`。请给 Game 视图焦点后操作键鼠。
默认 `SampleScene` 保留，但构建入口已改为 `Survivalcraft` 场景。

新检出仓库时，先初始化 submodule，然后在仓库根目录运行：

```powershell
git submodule update --init --recursive
python Port/Build/source.py --unity "D:/Development Programs/Unity/6000.3.12f1/Editor/Unity.exe" --install --validate
```

需要 Python、仓库固定的 .NET SDK 10.0.100，以及对应 Unity Windows Mono 构建模块。
命令恢复锁定的第三方依赖、重建 FLAC/ImageSharp 并打包内容。三个核心程序集由 Unity 自带编译器在 Editor/Player 构建中生成，日常源码修改直接由 Unity 重编译。
`--validate` 使用主项目完整副本构建 Player，通过真实入口注入鼠标点击和 Escape，并验证声音输出、初始世界与保存重载，生成截图和验收结果。

第三方 DLL 和 Content.zip 是可重建产物，不提交 Git。主项目禁止安装旧版三个核心 DLL；源码分支保留 SC 上游历史和原路径，支持 merge/cherry-pick 更新。

源码位置、C# 10 配置、分支关系和更新步骤见 [源码接入说明](Docs/UnitySourceIntegration.md)。
开发位置与剩余边界见 [主项目说明](Assets/SCUnity/README.md) 和 [桌面兼容层说明](Port/Compatibility/Desktop/README.md)。
