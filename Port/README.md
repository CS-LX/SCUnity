# Survivalcraft Unity 迁移基线

本目录已建立阶段 0 的**静态、构建、运行、存档及模组参考基线**；阶段 1 已完成源码生成/Mono 能力验证、数学/序列化/完整 EntitySystem，以及 FLAC 和 ImageSharp 3.1.12 解码依赖适配。完整 Engine、Survivalcraft 和游戏宿主尚未实现。阶段 0 见 [运行基线说明](Tests/RuntimeProbe/README.md)，阶段 1 见 [Mono 能力验证](Compatibility/README.md)、[基础模块](Compatibility/Foundation.md) 、[FLAC 原版对照验收](Compatibility/Flac.md) 和 [图像验收](Compatibility/Images.md)。

## 运行

要求 Windows x64、Git、Python 3.12 或更高版本、.NET SDK **10.0.100**。SDK 在 `Port/global.json` 中锁定；阶段 0 基线工具没有第三方 Python 或 NuGet 依赖。上游构建的首次恢复需要连接 nuget.org，后续可使用本机 NuGet 缓存。

在仓库根目录执行：

```powershell
python Port/Build/baseline.py verify
python Port/Tests/test_baseline.py
```

只检查固定提交、清单和工具指纹，不重建程序集：

```powershell
python Port/Build/baseline.py verify --static-only
```

`--static-only` 不代表构建、API 或游戏运行验证通过。首次创建基线使用 `capture`；已有 `upstream-lock.json` 时，该命令拒绝覆盖。上游升级或快照格式调整需要在专门变更中审查旧/新差异，并更新基线，普通 `verify` 永远不接受漂移。

## 文件用途

| 文件 | 内容 |
|---|---|
| `upstream-lock.json` | 固定上游 SHA、构建目标、SDK、清单、依赖和生成工具的 SHA-256 |
| `UpstreamFiles.json` | 当前上游 2,417 个文件的路径、长度与 SHA-256 |
| `SourceManifest.json` | 全部 1,427 个 C# 文件的指纹、迁移分类、责任模块和理由 |
| `Build/source-policy.json` | 明确的后端替换/机械补丁例外，以及共享模块和平台排除规则 |
| `ContentManifest.json` | `Content.zip` 的 829 个文件及 `init.js` 的指纹 |
| `Dependencies/*.packages.lock.json` | 三个 Windows 工程的直接及间接依赖版本和 NuGet 内容哈希，共 54 个不同包 |
| `Compatibility/PublicApi.Shipped.txt` | 从原版三个 DLL 的 PE 元数据提取的 API 快照 |
| `Compatibility/PublicApi.Summary.json` | API 分类计数；包含 public、protected 和 protected internal 表面 |
| `Compatibility/PublicApi.Exceptions.json` | API 例外登记，目前为空；当前精确基线校验不豁免任何差异 |
| `Tests/FixtureCatalog.json` | 运行行为、存档和模组样本的状态、证据及实际覆盖边界 |
| `Tests/Baselines/` | 压缩运行 trace、结果、存档、个人样本来源指纹及运行工具锁 |

`SourceManifest` 的分类是**迁移意图**，不是编译成功或兼容完成的标记。当前 1,368 个文件计划保留，27 个替换后端，9 个应用机械补丁，23 个排除平台实现。后续发现 BCL 或平台耦合时，应显式调整分类及补丁，不能静默修改业务源文件。`preserve` 项以当前源码原样保留为目标。

## 校验流程

1. 要求子模块 HEAD、父仓库 gitlink 和锁定 SHA 一致，且子模块无已跟踪修改或未跟踪文件。
2. 从该提交 `git archive` 到 `Port/.artifacts/baseline-*/upstream`。每次运行使用全新目录，原子模块不会接收构建产物。
3. 按归档中的实际字节生成完整文件、C# 源文件和内容清单，与已固定基线对照。新文件、丢失文件、变化文件、未知源码模块或失效的分类例外均会失败。
4. 向暂存项目复制依赖锁，使用 NuGet `--locked-mode` 恢复，并执行原工程 Release 构建。NuGet 源由仓库配置指定。恢复时关闭可随日期变化的漏洞审计，仅为了基线可重复；此步骤不作依赖安全评估。
5. 对比构建产生的 `Content.zip` 的条目路径和未压缩内容。ZIP 时间戳和压缩元数据不参与内容身份，重复条目会失败。
6. 使用 `System.Reflection.Metadata` 读取三个 DLL，无需加载、执行游戏或加载原生依赖。提取程序集身份、外部可访问类型、基类、接口、泛型约束、字段、方法、参数、访问器、事件、常量、属性、显式接口实现和必要布局。
7. 对照快照，确认原子模块仍然干净。全部通过后，在本次证据目录写入 `result.json`。

参考：[NuGet 锁定恢复](https://learn.microsoft.com/zh-cn/dotnet/core/tools/dotnet-restore?tabs=netcore2x)。

## 指纹和 API 的解释

- 上游文件哈希基于 Git 归档字节，避免本机 `core.autocrlf` 改变基线。基线 JSON、文本和工具自身的指纹统一使用 UTF-8、LF；正常 CRLF 检出不会误报。
- 快照保留外部类型的程序集作用域、成员访问级别、可选参数、custom modifier、特性值和布局。常量与特性负载使用元数据类型及十六进制字节保存，避免区域设置影响。
- 对顺序/显式布局类型，也保存非公开实例字段；顺序布局另外保留实例字段的声明次序，防止仅比较 public 字段或排序后的名称漏掉 ABI 变化。
- 不将方法体、DLL 时间戳或本机绝对路径作为 API。测试确认只修改方法体不会产生 API 差异，也确认快照读取不会执行模块初始化器。
- `baseline.py` 验证**同一上游 Windows 基线可重现**。基础模块另外建立了显式 BCL 重定向政策下的 `net10.0 → net48` API 比较，覆盖 Engine 子集及完整 EntitySystem 的 153 个类型；完整三程序集和低层平台类型仍待验收。
- 原版 Windows 基线保留上游 VR 代码，便于完整记录现有 API；迁移目标仍为非 VR。分类排除不自动授权删除公开 API。

## 验证证据和当前边界

最终快照完成一次生成、一次独立全新目录的锁定恢复/重建，以及 10 项工具行为测试。最终两次原版 Release 构建均为 0 警告、0 错误，生成结果对照一致。

每次运行的 `restore.log`、`build.log`、`api-tool-build.log`、`api-snapshot.log`、`result.json` 和 DLL 保存在终端打印的 `.artifacts` 目录，不提交 Git。工具不自动清理这些目录，便于审计。

原版现已实际运行：生成模组与真实存档联合场景通过 42 项断言、605 个完整帧及 279 次采样 ECS 调用；5 个社区模组同时加载及 RealmEX 两个 Shader 变体编译通过。用户原始样本保持不变；结果与限制见 [运行基线说明](Tests/RuntimeProbe/README.md) 和 [兼容报告](../Docs/PortCompatibilityReport.md)。Unity Mono 已验证动态加载、Jint、HarmonyX，并运行真实 TypeCache/实体组件生命周期及与原版逐字节一致的选定数学/序列化测试；完整模组链路仍待接通。
