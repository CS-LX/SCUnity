# 阶段 1：源码生成与 Unity Mono 运行能力验证

本模块建立了从固定上游生成源码、外部 Roslyn 编译 `net48`、构建独立 Unity Windows Player、自动验收并保存证据的链路。**它是阶段 1 的首个完成模块，完整的三个兼容程序集仍待实现。**

## 已验证的范围

Unity `6000.3.12f1` 的 Windows x64 **非 Development Mono Player**，使用 `.NET Framework` API 档位、禁用托管裁剪，在 `-batchmode -nographics` 下实际运行：

| 检查 | 断言数 | 覆盖 |
|---|---:|---|
| 动态程序集 | 8 | `Assembly.Load(byte[])`、加载事件、程序集枚举、派生类型发现/实例化、动态类型查询、经 `AssemblyResolve` 从字节加载独立依赖 |
| Jint `4.15.3` | 4 | JavaScript 求值、调用 C# 委托、CLR 命名空间互操作、重复回调 |
| 原样上游源码 | 5 | `ReadOnlyList` 枚举/拒绝修改、`CollectionUtils.SelectNth`、`StateMachine` 转移顺序和历史 |
| HarmonyX `2.16.1` | 8 | 构造器、虚方法、封闭的 `int` 泛型方法、原版 `Game.StateMachine.Update` 的补丁执行及撤销 |

另外启动同一个 Player，故意不给动态插件提供依赖 DLL，要求得到明确的 `FileNotFoundException` 和退出码 1。通过构建、返回空断言列表、Editor 中运行、使用错误后端、缺少检查或任何失败断言均不能通过验收。

该工程只有一个 Unity Player 进程，没有运行原版 Survivalcraft EXE，也没有 GLFW/OpenGL 宿主。插件和依赖仅作为 `.dll.bytes` 放在 `StreamingAssets`，不作为 Unity 预加载插件导入；运行时确认二者尚未加载后才执行动态加载。

## 重现

在仓库根目录运行，要求 Python 3.12+、固定的 .NET SDK 10.0.100、Unity 6000.3.12f1 及 Windows Mono Build Support：

```powershell
python Port/Build/generate_compatibility.py
python Port/Build/mono_probe.py --unity 'D:\Development Programs\Unity\6000.3.12f1\Editor\Unity.exe'
python Port/Tests/test_compatibility.py
```

`--unity` 按本机安装路径填写，也可设置 `UNITY_EDITOR`。生成器和构建都写入全新的 `Port/.artifacts/` 子目录；不会写入上游子模块或更改当前打开的 Unity 工程。

工具通过 Unity `PlayerSettings` API 设置独立测试工程的 Mono/Framework 配置，保存并退出后重新打开构建，避免在一次构建过程中切换 API 导致导入状态过期。构建失败、超时或预期之外的进程退出码都会失败，终端输出证据目录。

首次已审查的依赖图保存在五个项目各自的 `packages.lock.json`；普通构建必须有这些锁，执行 `--locked-mode`，并确认恢复/构建没有改写锁。NuGet 依赖仍使用与上游相同的 Jint/HarmonyX 版本。`PathMap` 消除暂存目录差异，五个测试 DLL 已在两个独立目录重建并逐字节比较一致。

## 源码和依赖边界

[mono-probe.json](Profiles/mono-probe.json) 明确选中三个原样文件。生成器先核对固定上游提交、干净子模块、阶段 0 指纹和整个 Git 归档，再输出源码、`Sources.props` 和覆盖全部 1,427 个 C# 文件的 `GenerationManifest.json`。本配置明确记录 **3 个 preserve、23 个 exclude-platform、1,401 个 pending**；不会将子集编译成功标记为全项目兼容。

生成器支持已登记输入/输出 SHA-256 的逐段、严格匹配次数的机械补丁，以及独立后端替换文件；补丁必须填写原因。上下文、次数、输入或输出任一变化即拒绝生成。当前探针没有应用业务补丁。`complete` 配置只要仍有未处理源码就失败；未知、重复、平台排除路径和越界路径同样失败。

上游子集编译为 `SCUnity.Probe.Upstream.dll`，不冒充完整的 `Engine.dll` 或 `Survivalcraft.dll`。其余 `SCUnity.Probe.*` 程序集是测试夹具。`ProbeExtension` 是用于验证 CLR 派生类型加载的测试契约，**没有替代或模拟 `Game.ModLoader`**。

Unity 提供 `System.Buffers`、`System.Memory`、`System.Numerics.Vectors`、`System.ValueTuple` 对应的 BCL 类型或转发程序集，因此不把 NuGet 的同名文件重复导入为插件。`System.Runtime.CompilerServices.Unsafe` 仍随插件部署。工具对进入 Player 的自有和第三方 DLL、动态载荷逐个核对哈希，并记录 Player 实际 `Managed` 目录的全部程序集指纹。

Unity 配置依据：[PlayerSettings.SetApiCompatibilityLevel](https://docs.unity3d.com/cn/6000.0/ScriptReference/PlayerSettings.SetApiCompatibilityLevel.html)、[Unity .NET 配置文件](https://docs.unity3d.com/cn/6000.0/Manual/dotnet-profile-support.html)。依赖框架声明：[Jint 4.15.3](https://www.nuget.org/packages/Jint/4.15.3)、[HarmonyX 2.16.1](https://www.nuget.org/packages/HarmonyX/2.16.1)。是否能运行以本仓库实际 Player 结果为准。

## 验收证据和下一模块

[MonoEvidence](../Tests/MonoEvidence/evidence-lock.json) 保存实际 Player 正/负结果、构建概要、源码和 DLL 指纹、生成覆盖统计及独立重建对照结果。源码和证据变动会使测试失败。首次归档命令是 `python Port/Build/record_mono_probe.py <证据目录> --repeat <另一轮独立构建目录>`，已有归档时拒绝覆盖；证据更新需要审查代码与结果差异。

完整 Editor 日志、Player、临时项目及 DLL 保存在终端打印的 `.artifacts/mono-probe-*` 中，不提交二进制构建产物。此模块没有图像验收：图形设备为 Null，尚不涉及主菜单、世界画面或 URP 输出。

接下来的阶段 1 模块仍需完成 `Engine`/`EntitySystem`/`Survivalcraft` 三个完整 `net48` 程序集、现代 BCL/媒体依赖适配、平台后端替换、跨框架 API 比较，以及真正的 `TypeCache`、`ModLoader` 和 `.scmod` 在 Player 中的联合验证。当前结果证明这些运行时基础能力可行，不能代替完整游戏或社区模组验收。
