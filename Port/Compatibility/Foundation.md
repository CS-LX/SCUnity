# 阶段 1：基础数学、序列化和完整 EntitySystem

本模块把固定上游的 **139 个源文件** 编译为 Unity Mono 可加载的 `net48` 程序集：`Engine.dll` 为选定基础子集，`EntitySystem.dll` 包含全部 20 个上游 C# 文件。它们只部署到独立验收工程，尚未接入主工程，也不能替换完整游戏发行包。

阶段 1 的首个模块及 Jint/HarmonyX 结果仍见 [Mono 能力验证](README.md)。本模块进一步运行真实 `Engine.Serialization.TypeCache` 和 `GameEntitySystem`，没有用测试替身代替这些实现。完整 `Engine`、`Survivalcraft`、`Game.ModLoader`、`.scmod` 联合加载和图形宿主仍待接通。

## 实际验收

Unity 6000.3.12f1、Windows x64、Mono、.NET Framework、非 Development、禁用托管裁剪，独立 Player 构建为 0 警告/0 错误，22 项运行断言通过。另在 .NET 10 进程中使用阶段 0 **未修改的原版 Engine/EntitySystem DLL** 执行相同测试代码作为对照；不会启动原版游戏 EXE。

| 检查 | 结果与边界 |
|---|---|
| 数学运算 | 固定整数生成器产生 4,096 组输入，矩阵算术/Lerp/MultiplyRestricted、Vector2/3/4 变换、ref 重载、Vector2/3 法向量变换、带偏移的数组变换、Vector4 取整、System.Numerics 隐式转换，共 4,308,992 字节与原版完全一致 |
| 浮点边界 | 负零、正负次正规数、半整数、正负无穷和 NaN 载荷的 Floor/Ceiling/Round，672 字节与原版完全一致；未放宽误差阈值 |
| 二进制序列化 | 7-bit 整数边界、空/Unicode/重复字符串、Vector3、Matrix、数组，122 字节完全一致；关闭对象信息的回环正确、字符串引用复用和读取长度正确 |
| XML 与转换器 | ValuesDictionary 嵌套值、Vector3 和 Unicode 字符串保存/还原；210 字节 XML 完全一致；实际反射扫描发现序列化器和转换器 |
| TypeCache | 动态 DLL 初始未加载，从字节加载后原版 AssemblyLoad 处理器重新扫描；短类型名、缓存身份和必需类型缺失异常通过 |
| 实体生命周期 | 动态 DLL 中的组件继承真实 Component，经 Project.CreateEntity 反射创建并 Load；ID 分配、重复 Add、FindEntity 命中/未命中、Save、Remove/Dispose 和事件顺序通过 |
| Unity BCL | 实际 Player 执行默认接口方法，以及 Dictionary.EnsureCapacity/TryAdd/Remove(out)/TrimExcess |
| 公开 API | Engine 子集 131 个类型、完整 EntitySystem 22 个类型，4,339 条规范化元数据记录，无未登记差异；字段布局、参数、泛型和特性仍受检查 |
| 可重建 | Engine、EntitySystem、Runner、动态 Payload 四个 DLL 在独立新目录重新生成/锁定恢复/构建，字节完全一致；实际 Player 部署文件逐个核对哈希 |

数学检查覆盖表中列出的运算，并非所有数学方法、所有输入或整个世界模拟的浮点确定性证明。没有进行性能基准；四通道标量实现的性能需在后续真实帧负载中测量。这里的二进制/XML 样本也不等于完整 `.scworld` 兼容验收。

## 源码补丁

[foundation.json](Profiles/foundation.json) 登记 127 个原样文件、12 个补丁文件；23 个平台文件排除、1,265 个仍待适配，`complete` 明确为 false。全部 88 个 `Engine.Serialization` 源文件入选。生成器逐一检查原始字节、精确补丁上下文/次数和输出 SHA-256。上游子模块保持干净。

- `Float4` 为内部兼容类型，替代 Matrix/Vector 中不可用的 Intrinsics 子集，保留四通道布局与加乘顺序。没有新增外部 API。
- 实际对照发现 Unity Mono 的 Vector2/3 标量坐标变换舍入结果与原版不同。因此 Transform/TransformNormal 及 ref 重载采用同文件中原有数组变换的通道运算顺序，明确存储每步 float 结果；通过逐字节比较后纳入补丁。其他数学公式没有借迁移修改。
- 不可用的 ThrowIfNull 改为相同参数名的 ArgumentNullException 检查。
- EntitySystem 唯一补丁为 `FirstOrDefault(predicate, null)` 改为 `FirstOrDefault(predicate)`；Entity 的默认值仍是 null，命中和未命中均实际验证。
- Log 的窗口标题副作用写入内部 `CoreDiagnostics.TitleSuffix`。独立无图形工程没有窗口宿主；阶段 2 接入宿主时需要替换这一诊断出口。

构建使用固定 Unity Editor 自带的 `unity-4.8-api` 引用程序集，不将普通 .NET Framework 引用包等同于 Unity 的实际 BCL。显式 NuGet 依赖仅为已验证的 `System.Runtime.CompilerServices.Unsafe` 6.0.0，依赖图锁定。验证代码使用现代 Roslyn 编译，Unity 工程内仅保留桥接脚本。

## 原版行为记录

对照发现原版 BinaryOutputArchive 默认启用对象信息时，首次序列化 `int[] {1,-2,73}` 写出的对象 ID 为 0；BinaryInputArchive 将它读为 null，只消费 9 字节中的 1 字节。迁移版保持同样结果，见 [object-info.txt](../Tests/FoundationEvidence/object-info.txt)。此处没有修复原版行为；正常数组回环测试显式关闭对象信息。后续存档工作必须分别验证游戏实际使用的路径，不能把这一记录误报为默认对象信息回环成功。

## 跨框架元数据规则

[foundation_api.py](../Build/foundation_api.py) 先重新提取原版三个程序集，与阶段 0 的锁定快照精确匹配，再比较候选基础程序集。类型集合从已审查源文件配置和原版快照确定，删除候选类型不能缩小预期范围。

| 允许规范化的变化 | 约束 |
|---|---|
| BCL 程序集拆分 | 仅列举的 mscorlib/System/System.Core/System.Runtime/Collections/Numerics/XML/Threading 框架作用域；外部库作用域继续比较 |
| TargetFrameworkAttribute | 只接受精确登记的 .NET 10 与 .NET Framework 4.8 两种负载 |
| IsUnmanagedAttribute | Roslyn 在目标 BCL 缺失时生成的内部标记，保留特性和泛型约束，只规范化其作用域 |
| 转换器 Type[] 和安全声明内的 BCL 类型名 | 仅替换精确的已知 BCL 类型+程序集版本+公钥 token 字符串；目标类型、数量、构造器和其他字节仍比较 |
| Engine 程序集的 ExtensionAttribute | 完整 Engine 的扩展方法位于选定子集外，只忽略这一条精确程序集标记；成员上的特性不忽略 |

这些是基础子集的显式比较政策，不覆盖整个 Engine、Survivalcraft 或 Silk/ImageSharp 等低层外泄类型。原版 API 基线及其空例外表没有改写。

## 重现与证据

先运行 `python Port/Build/baseline.py verify` 得到原版证据目录，再在仓库根目录运行：

```powershell
python Port/Build/foundation.py --unity 'D:\Development Programs\Unity\6000.3.12f1\Editor\Unity.exe' --baseline 'Port/.artifacts/baseline-bb9nj4_x'
python Port/Tests/test_foundation.py
```

`--baseline` 按实际输出目录填写。工具核对原版 DLL 哈希及完整 API、SDK 版本、生成输入、NuGet 锁；两次启动独立 Unity 工程配置并构建，运行 Player，与原版输出比较，再进行独立重建。所有输出写入新建 `.artifacts/foundation-*`，不操作当前打开的主工程。

首次归档命令为 `python Port/Build/record_foundation.py <证据目录>`。归档器复核源文件、Player/程序集、原版对照、API 和独立重建，已有证据时拒绝覆盖。[FoundationEvidence](../Tests/FoundationEvidence/evidence-lock.json) 保存实际结果、压缩 API、指纹、覆盖统计和小型对照样本；完整 4 MB 数值语料和本机日志留在临时目录，可用同一固定输入重建。

新增 9 项回归测试覆盖实际证据/源文件指纹、完整 EntitySystem 源文件选择、API 缺失/重复/布局/特性改变、未知框架负载、单字节数据漂移及错误 Player 配置。连同已有 31 项工具测试，40 项全部通过。

下一模块继续处理完整 Engine/Survivalcraft 的平台与媒体依赖。诊断构建已确认直接退回 ImageSharp 2 会失去原版部分格式/API 能力，因此未采用该降级方案。主菜单画面仍需阶段 2 的宿主/加载链路和阶段 3 的图形后端。
