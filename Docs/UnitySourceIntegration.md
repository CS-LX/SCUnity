# Unity 核心源码接入

`Engine`、`EntitySystem`、`Survivalcraft` 由 Unity 直接编译为同名程序集。
主项目不再加载这三个预编译 DLL，修改源码后由 Unity 重编译，可直接设置断点。

## 源码与分支位置

| 内容 | 位置 |
|---|---|
| 引擎源码 | `External/SurvivalcraftApi/Engine` |
| ECS 源码 | `External/SurvivalcraftApi/EntitySystem` |
| 游戏源码 | `External/SurvivalcraftApi/Survivalcraft` |
| Unity 宿主、URP、输入、音频 | `Assets/SCUnity/Runtime` |
| 运行场景 | `Assets/SCUnity/Scenes/Survivalcraft.unity` |
| 移植提交与编译器锁 | `Port/source-lock.json` |

在 Unity Project 窗口展开 **Packages**，可看到 `Survivalcraft Engine (Source)`、
`Survivalcraft EntitySystem (Source)`、`Survivalcraft Survivalcraft (Source)`。
这些是 manifest 显式引用的本地源码包，文件实际位于上面的 External 路径。

父项目和源码子模块分别使用 GitHub 同一仓库的不同分支：父项目为 `main`，
源码移植为 `feature/unity-source-runtime`。源码分支直接继承 SCAPI1.9 的完整历史，
原始基线为 `98e5f58f779dba20503a095073451a40899549c4`，保留上游目录结构。
子模块固定的是提交，不会在 Play 或构建时自动跟随远端。
源码分支没有子模块，因此不会因 URL 相同而递归克隆主项目。

## 新检出与日常运行

```powershell
git submodule update --init --recursive
python -X utf8 Port/Build/source.py --unity "D:/Development Programs/Unity/6000.3.12f1/Editor/Unity.exe" --install --validate
```

需要 Unity 6000.3.12f1、Windows Mono 构建模块、Python 3.12+ 和 .NET SDK 10.0.100。
`source.py` 只恢复第三方依赖、重建已适配的 FLAC/ImageSharp、打包 Content.zip；
游戏核心的编译始终由 Unity 完成。第三方 DLL 和 Content.zip 不提交 Git。
日常改游戏源码无需再次运行依赖恢复；内容资源变更后重新执行 `--install` 更新资源包。

打开场景点击 Play，或使用 `Survivalcraft > Open Game Scene`。
请保持 Domain Reload。存档使用 Unity 的 persistentDataPath，不使用旧 Windows 游戏目录。
`desktop.py --install` 会拒绝向已启用源码包的主项目安装核心 DLL。

## 编译配置

- 三个包各自包含 asmdef、csc.rsp；保留程序集名、版本、unsafe 和 WINDOWS/SCUNITY 符号。
- Unity 官方声明 C# 9 支持。本版本实际自带 Roslyn 4.3.1，本项目仅在核心包响应文件启用 C# 10，
  保留无参结构体构造函数、字段初始化、常量插值字符串与 global using 的语义。
  这是一项已对固定 Editor 和 Windows Mono 验证的版本约束，不能据此推定其它版本/平台可用。
- 更高版本的集合表达式、主构造函数、field-backed 属性、条件赋值等已转换为普通源码。
  结构体保留 public 字段以及 `new T()`、`default(T)` 和复制时的原有区别。
- Engine 的 7 个文件仍以原名嵌入程序集，`GetManifestResourceStream` 的调用契约保持。
  响应文件使用 `External/SurvivalcraftApi` 相对路径，移动子模块需要同步更新这些配置。
- System.Text.Json 10.0.10 的 Roslyn 4.0 生成器由依赖恢复安装，Unity 在编译游戏包时执行它。
  它不参与 Player 运行，不替换 Unity 编译器。

## 上游更新

新克隆的子模块通常处于 detached HEAD，先在源码子模块切到移植分支：

```powershell
git -C External/SurvivalcraftApi fetch origin feature/unity-source-runtime
git -C External/SurvivalcraftApi switch feature/unity-source-runtime
git -C External/SurvivalcraftApi remote add upstream https://gitee.com/SC-SPM/SurvivalcraftApi.git
git -C External/SurvivalcraftApi fetch upstream SCAPI1.9
git -C External/SurvivalcraftApi cherry-pick -x <上游提交>
```

`remote add` 仅在新克隆且没有 upstream 时执行。完整版本同步使用 merge 指定上游分支；
精选修复使用 cherry-pick。原路径保留，冲突在源码分支上解决。不要把源码分支直接 merge 到父项目根目录。

代码修改在子模块提交后，更新 `Port/source-lock.json` 的 sourceCommit，并暂存父项目 gitlink：

```powershell
git add External/SurvivalcraftApi Port/source-lock.json
python -X utf8 Port/Build/source.py --unity "D:/Development Programs/Unity/6000.3.12f1/Editor/Unity.exe" --validate-installed
python -X utf8 -m unittest discover -s Port/Tests
```

依赖/内容变化时使用 `--install --validate`；升级依赖需单独审查包锁。
通过后先推源码分支，再提交并推父项目，确保其 gitlink 在远端可获取。
`Port/Compatibility/Desktop` 是已验证的历史转换基线，正常构建不会重放它或覆盖手工源码。
`Port/Tools/SourceMigration` 只提供迁移候选转换，不能替代冲突审查和验收。

## 验收范围

`source.py --validate` 在主项目副本中构建 Windows Player，检查 Unity CompilationPipeline
确实从源码包编译了 336/20/1062 个输入文件；Engine 的数量含空编译的平台 EGL 文件。
每次执行两次独立 Player 场景，每次包含：

- 14 项源码迁移检查：程序集、资源、构造器、结构体、文字绘制、像素指针、JSON 生成器。
- 17 项社区搜索/缩略图检查；服务器响应为固定样本，输入、PNG 解码与 GPU 绘制为真实路径。
- 17 项初始世界、模型、保存重载与 GPU 状态检查。
- 12 项实际 Listener 输出的音频检查。

另有两次 Editor Play/Stop、公开 API 元数据比对和 63 项 Python 工具检查。
API 门禁保留公开签名及布局，仅登记具体的编译器/Debug 元数据差异。
本次证据见 `Port/Tests/Baselines/unity-source`，详细日志在 `Port/.artifacts`。

完整世界视觉对照、完整模组 SDK/兼容回归、IME、文件对话框和剩余桌面功能仍属于后续计划。
本次不承诺直接加载 .NET 10 模组二进制，也没有验收 IL2CPP、VR 或其它平台。
