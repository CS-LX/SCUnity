# 原版 Windows 运行基线

这是阶段 0 的诊断宿主，使用已经构建并验证的原版三个程序集。它直接调用原 `Game.Program.EntryPoint`，并以 Harmony 记录加载动作、帧生命周期、模组钩子、RenderTarget 切换和采样帧内的实际 ECS Update/Draw 调用。它不属于 Unity Player，也不证明 Unity Mono 兼容性。

## 重现

首先执行 `python Port/Build/baseline.py verify`，将输出中的证据目录传给 `--baseline`。例如本轮目录为 `Port/.artifacts/baseline-bb9nj4_x`。

```powershell
# 无需用户资料：原版主菜单
python Port/Build/runtime_baseline.py --baseline Port/.artifacts/baseline-bb9nj4_x

# 无需用户资料：生成四个 .scmod，进入固定种子世界、保存及重载
python Port/Build/runtime_baseline.py --baseline Port/.artifacts/baseline-bb9nj4_x --scenario world --mods generated

# 可在新检出环境中使用仓库内保存的真实旧版及生成存档
python Port/Build/collect_runtime_samples.py Port/Tests/Baselines
# 将上一命令打印的 user-samples-* 目录作为 --samples
python Port/Build/runtime_baseline.py --baseline Port/.artifacts/baseline-bb9nj4_x --scenario world --mods generated --samples Port/.artifacts/user-samples-替换为实际目录

# 原始用户样本，仅在用户提供的位置仍可用时执行
python Port/Build/collect_runtime_samples.py "E:\CS Projects\survivalcraft-api\Survivalcraft.Windows"
# 本轮完整用户样本集：user-samples-kttmujaq
python Port/Build/runtime_baseline.py --baseline Port/.artifacts/baseline-bb9nj4_x --scenario world --mods generated --samples Port/.artifacts/user-samples-kttmujaq
python Port/Build/runtime_baseline.py --baseline Port/.artifacts/baseline-bb9nj4_x --mods community --samples Port/.artifacts/user-samples-kttmujaq

# 只验证保存的证据与校验器，不启动游戏、不需要显卡
python Port/Tests/test_runtime_baseline.py
```

运行宿主要求 Windows 桌面图形设备及原版支持的图形驱动。每次复制参考产物到新的 `.artifacts/runtime-*`，创建隐藏窗口，结束后正常关闭。超时会终止本次测试子进程；失败记录保留，不更新基线。

## 测试配置及差异

- 窗口为 960×540 且隐藏；输入未注入，测试玩家使用 `WidgetInputDevice.None`。声音与音乐音量为 0，视距 32，地形更新使用单线程。
- `app:` 在独立测试可执行目录内；`app:/doc`、Mods、世界和日志都留在该目录。`data:` 通过诊断前缀重定向到 evidence/data。
- 测试禁用在线更新、MOTD 更新、文件关联初始化和 VR 初始化。调用 `EntryPoint`，未覆盖 OS 参数解析、单实例锁及 IME 启动流程。这些桌面能力在后续阶段验证。
- 世界固定种子 `123456`、创造模式、平坦大陆、静态环境、关闭天气及季节变化；使用固定玩家名和皮肤。原始时间推进与物理计算保持原行为。
- 采集进程设置 `DOTNET_TieredCompilation=0`、`DOTNET_ReadyToRun=0`、`DOTNET_JitNoInline=1`。小方法会被 JIT 内联而绕过观测点，因此完整事件采集需要关闭内联；此配置不是性能基线。[.NET JIT 配置源码](https://github.com/dotnet/runtime/blob/main/src/coreclr/jit/jitconfigvalues.h)
- 19 个帧边界调用必须逐帧完整且顺序一致；每个加载动作必须有配对的开始/结束事件。世界开始游玩后的 3 帧记录 ECS 类型、组件 Entity.Id、UpdateOrder 和 DrawOrder。没有对全部世界状态、浮点值或跨进程同优先级对象排序作确定性承诺。

## 已验证

最终生成模组和全部用户存档联合运行：**42 项断言、605 个完整帧、3 个 ECS 采样帧、279 次实际 Update/Draw 调用**。社区模组运行：**3 项断言、22 个完整帧**。

- 真正的 `.scmod` ZIP 包从嵌套 Mods 目录被发现；代码 DLL 经原 `Assembly.Load(byte[])` 加载，发现 ModLoader、Block 和自定义 Reader。
- 验证 API 类型发现、OnLoadingFinished、资源覆盖、CSV 方块注册、语言合并、XML 数据库覆盖、JavaScript 初始化和逐帧回调。
- Harmony 运行检查包括构造器、虚方法、闭合泛型方法及一个真实游戏方法。游戏方法通过反射调用来观察方法入口补丁，避免调用方已有内联影响测量；测试完成后撤销该游戏方法补丁。
- 检查依赖版本范围、LoadOrder、LoadAfter、同优先级稳定性、循环时的排序回退、同名实例保留、缺失/版本错误依赖的禁用。
- 故意损坏的 DLL、缺失依赖及错误版本产生 3 条**明确预期**的错误。测试核对这些错误和后续有效模组成功，其他错误均导致失败。这不是对所有模组失败模式的隔离承诺。
- 新世界生成后实际进入游戏，放置方块，保存、释放、重载。玩家字段、种子、实体数等持久化摘要相等，方块值在地形重新加载后恢复。区块缓存数不参与持久化摘要比较。
- 7 个真实用户样本执行导入、原版升级和 Project.bak 恢复对照；其中一个真实序列化版本为 2.3，已由原版转换到 2.4。尚未逐一游玩这些真实世界，也未验证缺失备份时的完整恢复分支。
- 用户的 RealmEX、RecipaediaEX、SCIE、Logistics、EBoyTerminal 一起进入主菜单，均保持启用；RealmEX 的自定义 Shader 两种变体在原 OpenGL 后端编译成功。尚未对自定义画面或模组全部玩法做回归。

## 保存的证据

`Tests/Baselines` 保存结果、逐帧校验摘要、压缩 trace、源文件和证据指纹、生成存档，以及小型真实 2.3 存档及原版升级结果。9 项工具测试基于这些真实采集数据注入缺失事件、顺序交换、加载中断、持久化字段丢失等错误，确保门禁能拒绝漂移。

全部个人世界和模组的本机副本保存在 `Port/.artifacts/user-samples-kttmujaq`；原始目录未改动，每次使用前后核对原件与副本指纹。较大文件未复制到版本控制范围，来源、大小和哈希记录于 `Tests/Baselines/UserSamples.json`。新增样本应重新采集并审查，不能默默替换锁定文件。

最终完整运行目录为 `runtime-ddskqb94/evidence`，社区目录为 `runtime-vgj_5zvh/evidence`。各自的 `provenance.json` 记录运行程序集、模组和输入来源指纹。

另一次仅使用仓库 `Tests/Baselines` 内存档重新采集的运行，通过 38 项断言、600 帧顺序检查和 279 次 ECS 调用，证据为 `runtime-xgb2i5t0/evidence`。该运行不访问用户的 E 盘原始目录，验证了仓库内样本的独立复现路径。
