# 可重建的模组测试样本

由 `Build/runtime_baseline.py --mods generated` 编译并打包四个真实 `.scmod`。样本仅用于兼容测试，不进入 Unity 交付内容。

`resources`、`code`、`late` 形成刻意冲突的优先级、依赖和资源覆盖关系；`broken` 包含无效 DLL，验证原版对该失败的实际处理。`.scmod` 使用 ZIP、固定时间戳与 UTF-8 文件名标记。

代码参照官方模板的 ModLoader 初始化和 OnLoadingFinished 注册模式，独立编写断言；未修改参考模板或上游源码。参考：[SurvivalcraftTemplateModForAPI](https://gitee.com/SC-SPM/SurvivalcraftTemplateModForAPI)，固定参考提交 `80a9e325ec8b68fa5d7632b36bed1fdf008f3690`。

当前样本目标是原版 `.NET 10`，直接引用已经验证的参考 DLL，避免下载另一版本游戏 SDK。迁移阶段 1 仍需建立 `net48` 多目标 SDK 并在 Unity Mono Player 重跑这些测试。
