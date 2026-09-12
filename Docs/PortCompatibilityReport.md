# Survivalcraft Unity 迁移兼容报告

更新日期：2026-09-12。目标：Windows x86-64 桌面非 VR Unity Player。

当前里程碑：**阶段 0 与阶段 1 的基础/媒体依赖模块已验收；原版加载、主菜单和设置交互的核心循环已接入主项目。** 入口为 `Assets/SCUnity/Scenes/Survivalcraft.unity`。三个完整程序集已能在 Unity Mono 中运行该循环；原版音效/流式音乐已通过 Unity 输出验收；世界渲染、完整 API/模组/存档回归仍未完成。下方原版 `.NET 10` 基线不代表 Unity 端已经通过相同验收。详见 [桌面接入说明](../Port/Compatibility/Desktop/README.md)。

## 已建立的基线

| 项目 | 结果 | 证据 |
|---|---|---|
| 上游来源 | 固定 `SCAPI1.9` 的 `98e5f58f779dba20503a095073451a40899549c4`；原子模块保持干净 | [锁定记录](../Port/upstream-lock.json) |
| 完整文件清单 | 2,417 个上游文件，路径/长度/SHA-256 | [UpstreamFiles](../Port/UpstreamFiles.json) |
| 源文件归属 | 1,427 个 C# 文件；保留 1,368、机械补丁 9、替换后端 27、排除平台 23 | [SourceManifest](../Port/SourceManifest.json) |
| 内容 | 829 个 Content 文件，共 20,409,953 字节；另外记录 `init.js` | [ContentManifest](../Port/ContentManifest.json) |
| 依赖 | 三个 Windows 工程，共 54 个不同的直接/间接 NuGet 包，版本及内容哈希锁定 | [依赖目录说明](../Port/README.md) |
| 原版 Windows 构建 | 初次构建与独立空目录锁定重建均通过；两次均为 0 警告、0 错误 | `baseline.py capture` / `baseline.py verify` 的日志和 result.json |
| API 元数据 | 三程序集共 48,040 条元数据记录，两次提取一致；例外为空 | [API 统计](../Port/Compatibility/PublicApi.Summary.json)、[快照](../Port/Compatibility/PublicApi.Shipped.txt) |
| 工具行为测试 | 10 项通过，覆盖真实 PE/API 变化、布局、枚举、仅方法体变化、模块初始化器不执行、源码/内容/依赖漂移 | [测试入口](../Port/Tests/test_baseline.py) |
| 运行与模组/存档断言 | 联合场景 42 项断言通过，605 个完整帧逐帧顺序一致；3 帧采样包含 279 次真实 ECS Update/Draw | [运行结果](../Port/Tests/Baselines/generated/result.json)、[帧校验](../Port/Tests/Baselines/generated/validation.json) |
| 社区模组与 Shader | 5 个用户提供的模组共同到达主菜单，全部启用；RealmEX 两个自定义 Shader 变体编译成功 | [社区结果](../Port/Tests/Baselines/community/result.json) |
| 运行证据门禁 | 新增 9 项测试通过：缺失/乱序帧事件、加载动作缺失、ECS 绘制缺失、存档字段丢失、意外错误、原件/副本变化及指纹校验 | [运行测试](../Port/Tests/test_runtime_baseline.py) |

API 工具统计 Engine 413、EntitySystem 22、Survivalcraft 1,290 个外部可访问类型。Engine 比调研中的 public-only 反射统计多 3 个类型，因为本次同时纳入对派生模组可见的 protected 嵌套类型。该口径不意味着上游 API 新增了类型。

## 受保护的业务边界

- `EntitySystem/**`、`Survivalcraft/Block/**`、`Component/**`、`Subsystem/**`、`Widget/**`、`Screen/**`、`Managers/**`、`ModsManager/**` 及内容数据保持原行为。
- 主循环、ECS 更新/绘制、模组钩子、资源合并、存档格式和世界生成顺序均为受保护契约。
- 上游子模块保持不变。阶段 1 探针原样编译三个工具类；基础模块在独立生成目录应用 12 个文件的显式兼容补丁，保留数值运算顺序、异常参数名和查询默认值。具体变化及原版对照见 [基础模块](../Port/Compatibility/Foundation.md)。
- SourceManifest 中的 `patch` / `replace-backend` 仅登记后续工作的责任范围。每项实际变更仍需可重放的最小补丁或独立实现及对照证据，不能以分类理由代替业务等价验证。

## 待完成与兼容差异

| 项目 | 当前状态/影响 | 恢复或验收条件 |
|---|---|---|
| 启动与逐帧事件 trace | 已采集并验证原版 19 个帧边界调用、加载动作和采样 ECS/钩子顺序 | Unity 侧重跑同一契约；完整浮点状态和跨运行同优先级对象排序尚未承诺确定性 |
| 存档样本 | 已生成新建、游玩后和重载存档；7 个真实用户样本通过导入/升级/备份恢复，其中包含真正的 2.3 → 2.4 | 后续在 Unity 重跑；尚未逐个游玩全部真实世界，未覆盖缺失 Project.bak 时的所有恢复分支 |
| 模组样本 | 4 个可重建 `.scmod` 覆盖资源、CSV/XML、JS、动态 DLL、Reader、Block、Harmony 和错误样本；另采集 5 个实际社区模组 | 在 Unity 验证重新编译的模组；社区模组全部玩法仍需阶段 5 回归 |
| `net48` 与 BCL/依赖 | 完整 EntitySystem 及 Engine 基础子集共 139 个源文件已运行；FLAC 依赖另有 74 个第三方源文件原样重编译并验证 | 完整 Engine/Survivalcraft 及剩余媒体/平台依赖继续适配 |
| 动态加载、Jint、HarmonyX | Mono 能力探针通过；真实 TypeCache 动态重扫与真实 Component 反射创建/加载/保存/销毁也已通过 | 接通 Game.ModLoader/.scmod 发现、初始化与完整钩子链路 |
| 跨框架 API 对比 | 已按显式 BCL 重定向政策验证 Engine 子集 131、完整 EntitySystem 22 个类型，无未登记差异 | 扩展到完整三程序集；低层平台类型变化仍需逐项登记 |
| 原 OpenGL/Silk 公开表面 | 快照保留低层平台类型；后端替换的 API 等价性未验证 | 针对每个外泄类型确定同 API 实现或有证据的例外；不能静默删除 |
| 内容运行时解码与渲染 | 原版实际加载主菜单和标准世界；自定义 Reader/资源覆盖通过，实际社区 GLSL 两变体编译通过 | Unity 运行时 Reader、媒体、URP 与像素输出仍待对照；编译通过不代表自定义画面等价 |
| Unity 生命周期、输入、音频、桌面服务 | 主项目已接入加载/主菜单/设置循环、URP Unlit、键鼠交互；原版音效与音乐经 Unity DSP 输出，完整平台服务待实现 | 两次 Player 各通过 12 项声音检查，Editor 两轮 Play/Stop 通过；继续世界、完整输入与平台回归 |
| VR | 明确排除的交付范围；原版参考 DLL 仍包含上游 VR API | 保留需要的共享签名，Unity 桌面后端稳定报告 VR 未启动 |

## 阶段 1：已完成的首个模块

| 项目 | 自验收结果 |
|---|---|
| 源码生成 | 核对全部上游归档；3 个文件原样编译、23 个平台排除、1,401 个待适配明确列出；严格输入/补丁/输出哈希，拒绝将不完整配置报告为完成 |
| 外部 `net48` 工具链 | 固定 SDK 与 NuGet 锁，Release 构建 0 警告/0 错误；5 个测试 DLL 在两个独立目录重建字节一致 |
| Unity Player | 6000.3.12f1、Windows x64、Mono、.NET Framework、非 Development、禁用托管裁剪；构建 0 警告/0 错误 |
| 运行能力 | 25 项断言通过：动态加载/依赖解析 8、Jint 4、上游工具类行为 5、Harmony 补丁/撤销 8 |
| 真实失败场景 | 缺失独立依赖的第二次 Player 运行按预期报告 FileNotFoundException 并以 1 退出 |
| 工具与证据门禁 | 12 项测试通过，覆盖生成重现、补丁/路径/覆盖完整性、实际归档指纹和 Player 成败条件 |
| 证据 | [实际结果和指纹](../Port/Tests/MonoEvidence/evidence-lock.json)、[命令与范围](../Port/Compatibility/README.md) |

这里的动态派生类型是测试契约 `ProbeExtension`，未模拟或替代 `Game.ModLoader`；Harmony 的真实上游目标是未改动的 `Game.StateMachine.Update`。泛型覆盖封闭的 `int` 实例。独立测试工程未使用图形设备，没有验证主菜单、游戏画面、完整 API 或社区模组。完整三个程序集和主工程宿主接入仍是后续模块。

## 阶段 1：已完成的基础模块

| 项目 | 自验收结果 |
|---|---|
| 编译范围 | 139 个源文件：127 原样、12 补丁；Engine 数学/序列化子集，EntitySystem 全部 20 个源文件；1,265 个其他源文件待适配明确列出 |
| 实际运行 | Windows x64 Unity 6000.3.12f1、Mono/Framework、非 Development；构建 0 警告/0 错误，22 项断言通过 |
| 原版数值对照 | 4,096 组矩阵/向量输入，共 4,308,992 字节；取整边界值另 672 字节，与未修改原版 DLL 结果逐字节一致 |
| 序列化与真实实体 | 二进制、嵌套 XML 对照一致；真实 TypeCache 和组件生命周期通过；默认接口方法、Unity Dictionary API 通过 |
| API 与重建 | 153 个类型、4,339 条规范化记录，无未登记差异；四个工程 DLL 独立重建字节一致 |
| 工具回归 | 新增 9 项证据/API/数值漂移检查通过，连同已有测试共 40 项通过 |
| 证据 | [实际 Player 结果](../Port/Tests/FoundationEvidence/runtime-result.json)、[证据锁](../Port/Tests/FoundationEvidence/evidence-lock.json)、[命令和精确范围](../Port/Compatibility/Foundation.md) |

两个 Vector2/3 标量变换在初次 Mono 对照中存在舍入差异，已用登记补丁固定为原版 float 运算顺序并重新通过逐字节验证。此结果不代表全部数学函数或世界模拟已实现跨运行时确定性，也没有代替真实存档和完整模组验收。

原版默认对象信息序列化首次 `int[]` 的读回结果为 null，迁移版保持相同行为，已单独记录；正常数组回环测试关闭对象信息。这一上游行为未在迁移中修复。直接退回 ImageSharp 2 会失去原版部分格式/API 能力，诊断尝试没有纳入正式兼容产物；媒体后端仍需继续处理。

## 阶段 1 新增：FLAC 解码依赖

原版 NAudio.Flac.Unknown.Mod 1.0.4 为 net10.0。本次从包记录的固定源码提交重建同名同版本 net48 DLL，74 个 C# 文件没有修改；完整依赖 API 的 69 个类型、1,405 条规范化记录对照通过。

Windows x64 Unity 6000.3.12f1 Mono Player 的 9 项检查组通过，构建 0 警告/0 错误。全部 334 个内置 FLAC 音效及 3 个已知 PCM 样本共 7,229,296 字节 PCM 与原版完全一致；跳转、读取结果、缓冲区和错误对照一致。两个工程 DLL 独立重建字节相同。新增 11 项工具/证据回归测试通过，总计 51 项通过。

继承的 TotalTime 属性存在 BCL 毫秒取整差异：334 个样本不同，最大 0.4989 ms；只对该属性应用精确的取整规则，PCM/字节位置不豁免。原版 Async 预扫描在诊断中出现竞态异常，未修改也未标记通过；游戏使用的默认同步路径已验证。详细范围、原版已有流行为和重现命令见 [FLAC 说明](../Port/Compatibility/Flac.md)，实际证据见 [FlacEvidence](../Port/Tests/FlacEvidence/evidence-lock.json)。本模块没有接入 Unity Audio、游戏包装层或可视输出。

## 重现证据

执行 [Port/README.md](../Port/README.md) 中的完整校验和测试命令即可重现本轮结果。`Port/.artifacts/baseline-*` 记录每次原版构建和快照，输出目录由命令打印；二进制和本机日志不提交。

最终快照的采集目录为 `baseline-gnnf6f79`，独立验证目录为 `baseline-bb9nj4_x`。此前的草稿快照在补齐顺序布局字段声明次序后重新生成；源码、资源和依赖指纹均未变化。这是本机证据索引，不作为跨机器固定路径或游戏兼容性承诺。

运行基线目录：联合场景 `runtime-ddskqb94/evidence`，社区场景 `runtime-vgj_5zvh/evidence`。必要结果、压缩 trace、生成存档和小型真实旧档已保存到 [Tests/Baselines](../Port/Tests/Baselines/runtime-lock.json)，不依赖本机临时日志才能审查。完整个人样本集保留在 `.artifacts/user-samples-kttmujaq`，原件与副本都已核对指纹。

测试配置显式禁用在线更新、文件关联、MOTD 和 VR 初始化；窗口隐藏、输入固定为空、声音静音，数据位于独立目录。为避免 JIT 内联漏掉观测点，仅采集进程关闭内联/预编译/分层编译。3 条损坏 DLL/错误依赖诊断为有断言的预期错误，其他错误均会失败。配置和重现命令见 [RuntimeProbe 说明](../Port/Tests/RuntimeProbe/README.md)。

仓库内样本的独立复现运行 `runtime-xgb2i5t0/evidence` 通过 38 项断言、600 帧检查、279 次 ECS 调用；无需访问 E 盘原始样本。纳入版本控制范围的全部运行证据与小型存档合计约 694 KB。

## 阶段 1 新增：ImageSharp 图像依赖

固定原始包源码重新编译为 net48，1,251 个 C# 文件中 31 个应用 BCL/后端兼容补丁，全部 467 个公开类型、15,332 条规范化元数据记录保持。程序集名称、版本和签名身份保持，没有回退到 ImageSharp 2。

实际 Windows x64 Unity Mono Player 通过 6 组检查：全部 171 个内置 WebP 和 12 个跨格式样本的像素、缩放、编码、异步文件读写及半精度转换，共 211 个文件、76,157,987 字节对照一致。构建 0 警告/0 错误，两个 DLL 独立重建字节相同。新增 9 项回归门禁。

内存池改用 Windows 物理内存报告，不能等同于 CoreCLR 的 GC 负载估计；这一差异及软件解码路径、未覆盖的格式边界见 [图像说明](../Port/Compatibility/Images.md)。[实际证据](../Port/Tests/ImageEvidence/evidence.json) 保留运行配置、接口快照和全部输出指纹。本模块尚未接入 Engine 包装层、纹理上传或游戏画面。
