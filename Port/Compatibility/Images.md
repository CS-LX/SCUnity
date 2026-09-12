# ImageSharp 3.1.12：Unity Mono 图像依赖

原版使用的 `SixLabors.ImageSharp` 3.1.12 仅提供 net6.0 DLL。本模块从 NuGet 记录的源码提交 `4224257dccf1005973ae51de06993e4b3e502c21` 重编译为 Unity Framework 可用的 net48 DLL，保留程序集名称、3.0.0.0 程序集版本、上游签名密钥和完整公开接口。未降级到 ImageSharp 2，也没有预转换游戏或模组资源。

## 实际验收

Unity 6000.3.12f1 的 Windows x64、非 Development、Mono、Framework、禁用裁剪 Player 实际运行。所有对照都使用锁定的原始 NuGet DLL 在 .NET 10 下执行同一套测试代码。

| 项目 | 结果 |
|---|---|
| 源码 | 1,251 个 C# 文件全部编译；31 个文件应用可重放补丁，其余保持原样 |
| 公开接口 | 全部 467 个类型、15,332 条规范化元数据记录一致 |
| 游戏图片 | `Content/Assets` 中全部 171 个 WebP 文件，解码像素及 17×11 缩放结果逐字节一致 |
| 格式样本 | 12 个样本覆盖 BMP、PNG、JPEG、GIF、PBM、QOI、TIFF、TGA、WebP；包括 2 位 BMP、16 位 PNG、32 位 TIFF、有损/无损 WebP |
| 保存 | 原版与 Mono 的 12 种编码输出逐字节一致；异步文件保存与同步保存相同，异步文件读取通过 |
| 半精度像素 | 65,536 种 half 位模式和 100,000 个固定随机 float 的转换结果逐字节一致 |
| 无效输入 | 空数据、短数据和截断 PNG 头拒绝加载，异常类型与原版一致 |
| 总对照量 | 211 个输出文件、76,157,987 字节，无像素或编码容差 |
| 可重建 | 两个工程 DLL 在独立新目录重建后逐字节相同；Player 中 DLL 和输入文件哈希复核通过 |

完整像素语料、构建工程和 Player 留在本地 `.artifacts/images-*`；Git 保存 [证据与输出指纹](../Tests/ImageEvidence/evidence.json)、压缩 API 快照、12 个小样本、源码归档和锁定依赖。

## 兼容实现和边界

ImageSharp 原有的 x86/ARM 硬件分支在此构建中报告不可用，沿用库已有的软件算法。内部向量类型保持字节布局，硬件专用方法在被意外调用时明确抛出异常。它们不暴露给游戏或模组，不模拟 CPU 指令执行。当前样本没有触发这些异常；这不等于验证了 ImageSharp 每一种像素类型、处理器和畸形文件组合。

31 个源文件补丁主要替换缺失的 BCL 方法：空值检查、枚举查询、数组引用、清空/排序、MD5、半精度转换、异步 FileStream 构造和指针宽度转换。内部调色板缓存的静态接口工厂改成实例接口转发，仍调用原有工厂与构造器。图像算法与格式分支保留。新增内部属性补齐编译器注解；API 比较仅规范化明确的 BCL 作用域、编译器标记、目标框架和安全属性中的 BCL 类型名。

内存池需要的 `.NET GC.GetGCMemoryInfo` 在 Mono 中不可用：Windows 适配通过 `GlobalMemoryStatusEx` 提供物理内存总量/用量，并以总量的 90% 作为高负载参考线。这是内存池容量/回收策略的后端差异，不能宣称与 CoreCLR 的 GC 负载报告等价；不会改变图像数据格式或像素算法。长期内存与性能验收仍属后续稳定化阶段。

编码页使用锁定的 `System.Text.Encoding.CodePages` 6.0.0；Unity 自有的 Memory/Buffers/Vectors 类型不重复部署。库源码和签名密钥来自上游 Git 归档，上游许可证随源码保留。[源码锁](../Dependencies/ImageSharp/source-lock.json) 记录两个仓库提交、每个原始文件及每个补丁的输入/输出哈希，重放遇到漂移立即失败。

本模块验收的是图像依赖。`Engine.Media.Image` 包装层、GPU 上传、Unity 渲染、完整游戏和模组仍需联合验证，当前模块没有菜单或世界画面。

## 重现

```powershell
python Port/Build/images.py --unity 'D:\Development Programs\Unity\6000.3.12f1\Editor\Unity.exe' --baseline Port/.artifacts/baseline-bb9nj4_x
python Port/Tests/test_images.py
```

`--baseline` 指向已通过阶段 0 验收的原版构建目录。构建使用仓库锁定 SDK 和 NuGet 依赖；工具会创建独立目录，不写入上游子模块或正在打开的 Unity 工程。首次归档使用 `record_images.py`，已有证据时拒绝覆盖。
