# Survivalcraft API Windows 端到 Unity 的迁移方案

## 基线

- Unity：`6000.3.12f1`，URP `17.3.0`，Input System `1.19.0`。
- 上游：`External/SurvivalcraftApi` 子模块，跟踪 `SCAPI1.9` 分支。
- 当前固定提交：`98e5f58f779dba20503a095073451a40899549c4`。
- 上游 Windows 版本已在本机使用 .NET 10 SDK 完整构建，结果为 0 警告、0 错误。

克隆本仓库后使用以下命令获取上游源码：

```powershell
git submodule update --init --recursive
```

需要主动更新到 `SCAPI1.9` 最新提交时使用：

```powershell
git submodule update --remote External/SurvivalcraftApi
```

## 迁移判断

上游采用 `Engine -> EntitySystem -> Survivalcraft` 三层结构。代码规模约为：

| 模块 | C# 文件 | 代码行 | 处理方式 |
|---|---:|---:|---|
| Engine | 327 | 37,800 | 保留数学、集合、序列化和媒体数据结构；替换平台后端 |
| EntitySystem | 20 | 2,054 | 原样复用为主 |
| Survivalcraft | 1,058 | 134,744 | 保留游戏逻辑、ECS、存档和原有 UI 逻辑 |

Windows 平台目录本身只有少量启动和 VR 代码，主要平台差异分布在共享代码的条件编译中。真正的迁移边界是自研 Engine：窗口、主循环、OpenGL ES、输入、音频、文件系统和资源加载都由它负责。

建议采用“API 兼容层 + Unity 后端”的路线。保留 `Engine`、`EntitySystem`、`Survivalcraft` 的程序集名、命名空间和公共类型，让现有业务代码继续调用 `Engine.Window`、`Engine.Graphics`、`ContentManager` 等接口；这些接口在 Unity 版本中转发到 Unity 生命周期和运行时服务。

这条路线也保留了旧模组二进制兼容的可能性。将每个业务对象重写成 MonoBehaviour 或把原有 UI 全量改成 uGUI/UI Toolkit，会扩大修改面并破坏现有模组 API，不适合作为第一阶段。

## 建议的目标结构

```text
External/SurvivalcraftApi/             上游子模块，只读基线
Port/
  Engine.UnityCompat/                  选择上游 Engine 源文件并替换后端
  EntitySystem.UnityCompat/            复用 EntitySystem
  Survivalcraft.UnityCompat/           复用游戏逻辑并替换 Program 入口
  build.ps1                            构建兼容程序集和 Content.zip
Assets/SurvivalcraftPort/
  Plugins/                             Engine.dll、EntitySystem.dll、Survivalcraft.dll
  Runtime/                             Unity 启动器及平台服务实现
  Rendering/                           URP Render Feature、材质和 Shader
  StreamingAssets/                     构建时生成的 Content.zip
```

上游源码继续留在 `External`。兼容程序集由独立 MSBuild 工程从子模块引用源文件并输出到 Unity，原因有两个：

1. 上游使用 .NET 10 和 C# preview，现有源码中约有 705 处集合表达式、13 处主构造函数、67 处 `unsafe`。外部构建可以固定编译器版本，同时把目标框架限制为 Unity 可运行的 API 集。
2. 维持 `Engine.dll`、`EntitySystem.dll`、`Survivalcraft.dll` 的程序集身份，有利于现有反射逻辑和模组引用继续工作。

## 平台映射

| 上游能力 | Unity 实现方向 | 首个里程碑范围 |
|---|---|---|
| `Program.Main`、`Window.Run`、帧事件 | 一个持久化 `SurvivalcraftBootstrap`，把 `Awake/Update/LateUpdate/OnApplicationFocus/OnApplicationQuit` 转成原有事件 | 必须 |
| `Engine.Time` | 由 Unity 帧时钟驱动，同时保留原有 `FrameIndex`、延迟任务和统计语义 | 必须 |
| `Storage` 的 `app:`、`data:`、`system:` | `StreamingAssets`、`Application.persistentDataPath` 和受控的系统路径映射 | 必须 |
| `Content.zip` 与模组资源覆盖 | 保留现有 `ContentManager` 合并规则；构建时生成 zip 并放入 StreamingAssets | 必须 |
| GLFW/Silk.NET 输入 | Unity Input System，填充原有 Keyboard/Mouse/Touch/GamePad 静态状态 | 必须 |
| OpenAL 音频 | `AudioClip`、`AudioSource` 和流式 PCM；FLAC/MP3 解码器单独替换 | 第二阶段 |
| OpenGL ES `Display`、Buffer、Texture、RenderTarget | 保留 Engine.Graphics 表层对象，内部使用 Unity Mesh/GraphicsBuffer/Texture/RenderTexture | 第二阶段 |
| GLSL `.vsh/.psh` | 将 9 组内置 Shader 转为 URP ShaderLab/HLSL，并按资源名及宏组合映射 Material | 第二阶段 |
| 原有 Widget UI | 保留布局、输入和绘制逻辑，走兼容渲染器 | 第二阶段 |
| OpenXR | 使用 Unity OpenXR，在基本画面和输入稳定后接入 | 后续 |
| Windows 文件关联、IME、剪贴板 | Unity API 加少量 Windows 原生插件 | 后续 |

渲染层需要由 URP 的 Render Feature/Render Pass 统一提交命令。Unity 持有图形设备和上下文，原有 Silk.NET/OpenGL 调用不能直接嵌入 Unity 渲染循环。游戏层有 288 个文件引用 `Engine.Graphics`，其中约 182 个文件直接参与绘制，因此保持 Engine.Graphics 的公开表面并更换底层实现，修改量最可控。

原项目支持运行时编译 GLSL。Unity Player 无法运行时创建 ShaderLab Shader，所以首版只映射内置 Shader；使用自定义 `.vsh/.psh` 的模组需要后续增加离线转换流程或明确标记为不兼容。

## 框架与依赖差距

Windows 上游目标是 `net10.0-windows`，Unity 工程目前也未开启 unsafe。兼容程序集需要独立启用 unsafe，并针对 Unity 的托管 API 配置做持续编译检查。

首次 `netstandard2.1` 兼容性探测已经在依赖还原阶段发现 `NAudio.Flac.Unknown.Mod 1.0.4` 仅支持 `net10.0`。以下依赖需要逐个分类：

- 直接保留或选择兼容版本：Jint、HarmonyX、NVorbis、ImageSharp、SharpGLTF、NCalc。
- 由 Unity 后端取代：Silk.NET.Windowing、Silk.NET.OpenGLES、Silk.NET.OpenAL、Silk.NET.Input、NativeFileDialogCore、TextCopy、ImeSharp。
- 需要替代实现：NAudio FLAC/MP3 链路和部分 .NET 10 BCL API。

Windows 首版建议固定使用 Mono 脚本后端。`.scmod` 中的托管 DLL 通过 `Assembly.Load` 动态加载，HarmonyX 也依赖运行时方法修补；IL2CPP 构建不能承诺这两项能力。资源型模组和 JavaScript 模组可以先恢复，托管 DLL 模组需要用真实模组样本单独验证。

## 实施阶段

### 1. 可编译的无图形核心

- 建立三个兼容程序集和自动构建脚本。
- 排除 Silk.NET 后端并提供最小接口桩。
- 让 Engine 的数学、序列化、内容数据结构以及完整 EntitySystem 在 Unity 目标 API 下编译。
- 验收：加载 `Database.xml`，创建一个 Project，完成一次存档数据读写回环。

### 2. 启动与内容纵切

- 接入 Unity 生命周期、时间、日志、存储和 Content.zip。
- 运行原有 `Program.Initialize` 和 `LoadingScreen`，暂时使用无图形后端记录绘制命令。
- 验收：加载全部内置资源与方块数据库，进入 MainMenuScreen，控制台无持续异常。

### 3. 基础渲染与输入

- 实现纹理、顶点/索引缓冲、渲染目标、状态对象和绘制命令队列。
- 先移植 Unlit、AlphaTested、Opaque 三条 Shader 路径，再补 Lit、Model、Transparent、Sky 等路径。
- 接入键盘、鼠标和触控状态。
- 验收：主菜单可操作；创建世界后能正确显示一个区块、天空和玩家 HUD。

### 4. 音频、媒体和完整游戏循环

- 接入音效、音乐流和媒体解码。
- 修正坐标系、矩阵约定、深度范围、裁剪方向和透明排序差异。
- 验收：标准新世界可以连续游玩、保存、退出并重新载入。

### 5. 模组与 Windows 集成

- 依次验证资源模组、JavaScript 模组、托管 DLL 模组和 Harmony 补丁。
- 恢复剪贴板、文件选择、IME、文件拖放与文件关联。
- 使用 Unity OpenXR 重新实现 VR 接口。
- 验收：建立兼容性样本集，并为每类模组记录支持状态。

## 第一轮实现建议

先完成阶段 1，并做一个“加载数据库后打印方块数量”的 Unity 场景。这个纵切能同时验证源代码构建方式、程序集边界、XML/JSON 依赖、Content.zip 路径和基础生命周期，是进入渲染层之前成本最低的可行性门槛。

上游仓库根目录未发现 LICENSE、COPYING 或 NOTICE 文件。子模块引用本身只记录外部仓库和提交；开始复制、修改或分发源码与游戏资源前，需要确认相应授权和发布边界。
