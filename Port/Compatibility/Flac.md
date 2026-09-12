# 阶段 1：FLAC 解码依赖适配

已解决一个完整 Engine 构建的依赖障碍：原版 `NAudio.Flac.Unknown.Mod` 1.0.4 只提供 `net10.0` DLL。本模块从包内登记的源码提交重建 `net48` 的 `NAudio.Flac.dll`，保留程序集名、版本 1.0.4.0 和全部公开 API，并在真实 Unity Mono Player 中对照原版解码结果。

这是 **FLAC 依赖的完成模块**。还没有接入完整 Engine 的 SoundData/StreamingSource 包装、Mixer 或 Unity Audio 输出，也没有增加可视界面。Survivalcraft 源文件迁移覆盖数没有因此增加；完整 Engine/Survivalcraft、图片解码和平台后端仍待推进。

## 源码与构建

- 来源：[XiaofengdiZhu/NAudio.Flac](https://github.com/XiaofengdiZhu/NAudio.Flac/tree/1b4bfa22d02553948590a748199c27fad77e17fb)，固定提交 `1b4bfa22d02553948590a748199c27fad77e17fb`，与原版包 nuspec 的 repository commit 一致。
- [源码归档及锁](../Dependencies/Flac/source-lock.json) 包含全部 74 个解码库 C# 文件、上游项目和说明；逐文件/归档 SHA-256 校验，所有 C# 字节原样保留。许可证为 [Unlicense](../Dependencies/Flac/LICENSE)。不退回另一个旧版 FLAC 包，也不修改解码算法。
- 该提交内的项目写着 net8.0/version 1.0.3，而实际 1.0.4 包为 net10.0。因此本模块没有仅凭提交号断言一致：同时锁定实际 1.0.4 DLL 哈希，比较完整元数据和运行行为。
- 独立构建项目使用固定 SDK、Unity 6000.3.12f1 的 Framework 引用程序集，以及与游戏相同的 NAudio.Core 2.3.0。依赖恢复使用 `--locked-mode`。仅屏蔽原第三方源码未使用私有字段的 CS0414，其他编译警告作为错误处理。
- 69 个可见类型、1,405 条元数据记录对照通过。仅规范化已列举的 BCL 程序集作用域、精确登记的 TargetFrameworkAttribute 和安全声明中的 BCL 类型名；程序集身份、成员、参数、布局及其他特性继续比较。

## 实际 Player 验收

Unity 6000.3.12f1、Windows x64、Mono、.NET Framework、非 Development、禁用托管裁剪；构建 0 警告/0 错误。原版对照进程只加载原始 NAudio.Flac/NAudio.Core DLL，不启动原版游戏 EXE。

| 内容 | 结果 |
|---|---|
| 游戏真实样本 | 固定上游全部 334 个内置 FLAC 音效，逐文件核对原始字节 |
| 已知 PCM 样本 | 另有 3 个每声道 5,003 帧的自建样本：8 kHz 单声道 16 位、44.1 kHz 双声道 16 位、96 kHz 双声道 24 位；含静音、极值和斜坡，解码必须与原始整数 PCM 一致 |
| 完整解码 | 337 个文件共 7,229,296 字节 PCM，与原版逐字节一致 |
| 流行为 | 默认构造器、格式/长度、四个跳转位置、多种分块读取、读取返回值/位置、缓冲区边界及流释放均完成对照 |
| 错误与重建 | 空输入、短输入、无效签名、截断元数据和 null 输入拒绝；同步预扫描回调、释放后重新构建解码器通过 |
| 可重建与部署 | 解码 DLL 和测试 Runner 在两个独立目录构建后逐字节一致；实际 Player 的 DLL 和全部音频样本逐一核对哈希 |
| 回归 | 9 项 Player 检查组、11 项新增工具/证据测试通过；连同既有工具测试共 51 项通过 |

所有解码都发生在进程内。测试不播放声音；这不等于 Unity Audio、混音或音效时序已经验收。

## 已登记行为边界

**TotalTime 的毫秒精度差异。** NAudio.Core 的继承属性通过 `TimeSpan.FromSeconds(double)` 计算时长；Framework 对应行为精确到最近的毫秒。[NAudio 2.3.0 源码](https://raw.githubusercontent.com/naudio/NAudio/v2.3.0/NAudio.Core/Wave/WaveStreams/WaveStream.cs)、[Microsoft 文档](https://learn.microsoft.com/en-us/dotnet/api/system.timespan.fromseconds?view=netframework-4.8)。

实际对照有 334 个样本出现取整差异，最大 4,989 ticks，即 **0.4989 ms**。规则登记于 [runtime-policy.json](../Dependencies/Flac/runtime-policy.json)：对于本批正时长，Unity ticks 必须恰好等于原版 ticks 四舍五入到毫秒的值，绝对差不得超过 5,000 ticks。原版和 Unity 的每个时长观测均保留。该规则只用于 TotalTime，PCM、采样长度、字节位置、读取结果和异常仍要求逐字节一致。

**原版已有的流行为保持原样。** 跳转后的实际位置不一定精确等于请求位置；某些越过末尾的分块读取会写入部分缓冲区但返回 0。测试同时记录请求、实际位置、返回值和完整请求缓冲区，与原版比较；没有把这些结果误报为通用流语义正确，也没有借迁移修复。

**异步预扫描尚未通过。** 初次对照中原版 Async 构造器因 Frames 尚未初始化抛出 NullReferenceException；源码先排队线程、随即读取 Frames，存在竞态。本模块未修改此分支，只验收游戏实际使用的默认同步路径和同步回调，不能宣称已修复或支持可靠的异步预扫描。其他未覆盖的第三方元数据、浮点读取接口及任意外部 FLAC 文件也不在本次完成声明中。

## 重现

先执行 `python Port/Build/baseline.py verify` 得到原版构建证据目录，然后在仓库根目录运行：

```powershell
python Port/Build/flac.py --unity 'D:\Development Programs\Unity\6000.3.12f1\Editor\Unity.exe' --baseline 'Port/.artifacts/baseline-bb9nj4_x'
python Port/Tests/test_flac.py
```

按本机实际路径替换参数。每次都创建全新的 `.artifacts/flac-*`，校验并解压已锁定第三方源码、从固定上游 Git 归档提取全部 FLAC 音效、构建原版对照与迁移程序集、构建独立 Player、对照结果并独立重建。不会改动当前打开的 Unity 工程或上游子模块。

自建 FLAC/PCM 样本已经提交，正常重现不需要 FFmpeg。`generate_flac_fixtures.py` 仅供首次生成，已有样本时拒绝覆盖；样本清单保留生成器和编码器指纹。

首次归档使用 `python Port/Build/record_flac.py <证据目录>`，已有归档时拒绝覆盖。[FlacEvidence](../Tests/FlacEvidence/evidence-lock.json) 保存实际 Player 结果、原版/候选完整 API、每个时长观测、输入/DLL/语料指纹和重建结果。完整约 13 MB 的二进制对照语料保留在本机证据目录，可以按固定输入重建。

下一步继续处理图片解码依赖及完整 Engine 的剩余平台耦合。此时仍在阶段 1；游戏主菜单需要后续宿主、完整加载链路和图形后端。
