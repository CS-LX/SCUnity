# 主项目核心循环

入口：`Scenes/Survivalcraft.unity`。打开后按 Play，会持续运行原版加载界面和主菜单；普通运行不会启动自动测试或自动退出。

- `Runtime/SurvivalcraftGame.cs`：初始化原版 `Game.Program.EntryPoint`，每个 Unity Update 驱动一次原循环，并处理关闭与清理。
- `Runtime/PortRenderer.cs`、`PortRenderFeature.cs`：原版绘制命令接入 Unity URP；当前已实现菜单使用的 Unlit 着色器。
- `Runtime/PortInput.cs`：Unity Input System 键鼠、文本事件与手柄状态到原版输入边界。
- `Settings`、`Resources/Shaders`：场景使用的 URP 管线、Render Feature 和原版 Unlit HLSL。
- `Plugins/Generated`：由 `Port/Build/desktop.py --install` 重建并安装的 DLL；Auto Reference 关闭，通过 asmdef 显式引用。
- `../StreamingAssets/Survivalcraft`：原版 Content.zip 与 init.js。

本阶段可验收“加载 → 主菜单 → 鼠标进入设置 → Escape 返回”，完整声音、世界渲染、IME 候选窗、原生文件对话框、窗口模式和重启仍待补齐。
不要将能进入菜单理解为完整游戏或完整模组兼容已经验收。

存档目录为 `Application.persistentDataPath/Survivalcraft`，启动时会打印实际路径，不指向旧游戏目录。
自动测试仅在命令行显式传入 `-scunity-smoke-output <独立目录>` 时启用。
停止 Play 会关闭原游戏宿主并释放渲染与输入资源；请保持 Unity 默认的进入 Play 时重载 Domain 设置。

项目 Windows 设置为 Mono、.NET Framework、Gamma，避免原版颜色发生二次线性化。
场景运行期间选择专用 URP 管线，停止后恢复此前管线；原 SampleScene 与模板设置资源保留。
旧可执行文件更新检查、MOTD 网络轮询、注册表文件关联和旧进程重启暂时由入口禁用，待 Unity 平台接入实现后恢复。
