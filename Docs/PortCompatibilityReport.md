# Survivalcraft Unity 迁移兼容报告

更新日期：2026-09-12。目标：Windows x86-64 桌面非 VR Unity Player。

当前里程碑：**阶段 0 的静态、构建和运行参考基线已建立，可进入阶段 1 的兼容编译与 Unity Mono 可行性验证。** 下列结果均来自原版 `.NET 10` 运行时，不代表 Unity 迁移已完成。

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
- 当前只有工具、清单、依赖锁、测试和文档发生变化。没有业务补丁，也没有修改上游子模块。
- SourceManifest 中的 `patch` / `replace-backend` 仅登记后续工作的责任范围。每项实际变更仍需可重放的最小补丁或独立实现及对照证据，不能以分类理由代替业务等价验证。

## 待完成与兼容差异

| 项目 | 当前状态/影响 | 恢复或验收条件 |
|---|---|---|
| 启动与逐帧事件 trace | 已采集并验证原版 19 个帧边界调用、加载动作和采样 ECS/钩子顺序 | Unity 侧重跑同一契约；完整浮点状态和跨运行同优先级对象排序尚未承诺确定性 |
| 存档样本 | 已生成新建、游玩后和重载存档；7 个真实用户样本通过导入/升级/备份恢复，其中包含真正的 2.3 → 2.4 | 后续在 Unity 重跑；尚未逐个游玩全部真实世界，未覆盖缺失 Project.bak 时的所有恢复分支 |
| 模组样本 | 4 个可重建 `.scmod` 覆盖资源、CSV/XML、JS、动态 DLL、Reader、Block、Harmony 和错误样本；另采集 5 个实际社区模组 | 在 Unity 验证重新编译的模组；社区模组全部玩法仍需阶段 5 回归 |
| `net48` 与 BCL/依赖 | 未实现兼容工程 | 三程序集可编译并在 Unity Mono Player 中加载 |
| 动态加载、Jint、HarmonyX | 已在原版真实 `.scmod` 中执行，未在 Unity 中运行 | 在 Unity Mono Player 中完成同一发现、初始化、钩子、构造器/虚方法/泛型/游戏方法补丁检查 |
| 跨框架 API 对比 | 当前只有原版快照重现门禁；框架程序集作用域已记录 | 建立经审查的 BCL 重定向比较规则，不自动忽略低层平台类型变化 |
| 原 OpenGL/Silk 公开表面 | 快照保留低层平台类型；后端替换的 API 等价性未验证 | 针对每个外泄类型确定同 API 实现或有证据的例外；不能静默删除 |
| 内容运行时解码与渲染 | 原版实际加载主菜单和标准世界；自定义 Reader/资源覆盖通过，实际社区 GLSL 两变体编译通过 | Unity 运行时 Reader、媒体、URP 与像素输出仍待对照；编译通过不代表自定义画面等价 |
| Unity 生命周期、输入、音频、桌面服务 | 未实现 | 分阶段接入，并在 Windows Player 验证 |
| VR | 明确排除的交付范围；原版参考 DLL 仍包含上游 VR API | 保留需要的共享签名，Unity 桌面后端稳定报告 VR 未启动 |

## 重现证据

执行 [Port/README.md](../Port/README.md) 中的完整校验和测试命令即可重现本轮结果。`Port/.artifacts/baseline-*` 记录每次原版构建和快照，输出目录由命令打印；二进制和本机日志不提交。

最终快照的采集目录为 `baseline-gnnf6f79`，独立验证目录为 `baseline-bb9nj4_x`。此前的草稿快照在补齐顺序布局字段声明次序后重新生成；源码、资源和依赖指纹均未变化。这是本机证据索引，不作为跨机器固定路径或游戏兼容性承诺。

运行基线目录：联合场景 `runtime-ddskqb94/evidence`，社区场景 `runtime-vgj_5zvh/evidence`。必要结果、压缩 trace、生成存档和小型真实旧档已保存到 [Tests/Baselines](../Port/Tests/Baselines/runtime-lock.json)，不依赖本机临时日志才能审查。完整个人样本集保留在 `.artifacts/user-samples-kttmujaq`，原件与副本都已核对指纹。

测试配置显式禁用在线更新、文件关联、MOTD 和 VR 初始化；窗口隐藏、输入固定为空、声音静音，数据位于独立目录。为避免 JIT 内联漏掉观测点，仅采集进程关闭内联/预编译/分层编译。3 条损坏 DLL/错误依赖诊断为有断言的预期错误，其他错误均会失败。配置和重现命令见 [RuntimeProbe 说明](../Port/Tests/RuntimeProbe/README.md)。

仓库内样本的独立复现运行 `runtime-xgb2i5t0/evidence` 通过 38 项断言、600 帧检查、279 次 ECS 调用；无需访问 E 盘原始样本。纳入版本控制范围的全部运行证据与小型存档合计约 694 KB。
