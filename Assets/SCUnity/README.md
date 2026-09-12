# 主项目核心循环

核心源码在 `External/SurvivalcraftApi` 的三个本地 UPM 包中，Unity Project 窗口的 Packages 可直接浏览和编辑。更新与编译配置见 `Docs/UnitySourceIntegration.md`。

入口：`Scenes/Survivalcraft.unity`。打开后按 Play，会持续运行原版加载界面和主菜单；普通运行不会启动自动测试或自动退出。

- `Runtime/SurvivalcraftGame.cs`：初始化原版 `Game.Program.EntryPoint`，每个 Unity Update 驱动一次原循环，并处理关闭与清理。
- `Runtime/PortRenderer.cs`、`PortRenderFeature.cs`：原版绘制命令接入 Unity URP；已接通六组原版着色器，涵盖初始世界的地形、天空、手臂与船模型。
- `Runtime/PortAudio.cs`：Unity DSP 消费原版 PCM，支持静态音效、循环、音量/音调与流式音乐。
- `Runtime/PortInput.cs`：Unity Input System 键鼠、文本事件与手柄状态到原版输入边界。
- `Settings`、`Resources/Shaders`：场景使用的 URP 管线、Render Feature 和原版 HLSL 生成的 ShaderLab。
- `Plugins/Generated`：由 `Port/Build/source.py --install` 恢复的第三方 DLL（不含三个核心程序集）；Auto Reference 关闭，通过 asmdef 显式引用。
- `../StreamingAssets/Survivalcraft`：原版 Content.zip 与 init.js。

本阶段可验收“加载 → 主菜单 → 鼠标进入设置 → Escape 返回”，声音的 12 项检查、初始世界/保存重载及 GPU 的 17 项检查已通过；完整世界视觉对照、模组着色器、IME 候选窗、原生文件对话框、窗口模式和重启仍待补齐。
不要将能进入菜单理解为完整游戏或完整模组兼容已经验收。

存档目录为 `Application.persistentDataPath/Survivalcraft`，启动时会打印实际路径，不指向旧游戏目录。
自动测试仅在命令行显式传入 `-scunity-smoke-output <独立目录>` 时启用；附加 `-scunity-audio-test` 验证 Unity Listener 的真实输出，`-scunity-world-test` 验证初始世界及保存重载。验收证据见 `Port/Tests/Baselines/desktop-audio` 与 `desktop-world`。
停止 Play 会关闭原游戏宿主并释放渲染、输入与音频资源；请保持 Unity 默认的进入 Play 时重载 Domain 设置。

中文社区搜索的旧列表/缩略图释放顺序与失效结果缓存已修正。附加 `-scunity-community-test` 可在自动测试中验证重复搜索、强制刷新、退出重进、共享纹理和真实 GPU 缩略图；服务器响应使用固定样本，不访问真实社区服务。详见 `Port/Tests/Baselines/desktop-community-search`。

项目 Windows 设置为 Mono、.NET Framework、Gamma，避免原版颜色发生二次线性化。
场景运行期间选择专用 URP 管线，停止后恢复此前管线；原 SampleScene 与模板设置资源保留。
旧可执行文件更新检查、MOTD 网络轮询、注册表文件关联和旧进程重启暂时由入口禁用，待 Unity 平台接入实现后恢复。

源码接入的额外 14 项运行检查随自动验收执行，覆盖构造器、结构体语义、文本绘制、像素指针、嵌入资源与 JSON 生成器。证据见 `Port/Tests/Baselines/unity-source`。
