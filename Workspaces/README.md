# 游戏 UI 工作区

每个游戏使用 `{GameName}/Panels/{PanelName}/` 存放独立面板源包，每个面板包含 `inputs/`、`design/`、`html/` 和 `.html-directory.json`。游戏根保存 `project.assets.json`、共享素材、项目级审核、工具与预览总入口。

当前工作区：[星港拾荒者](StarportSalvage/README.md)，包含 6 个面板。新增面板前先维护游戏根素材清单，再用初始化器创建 `Panels/{PanelName}/`；准备包与 HTML 相对项目根均为 `../../..`。批量导入时选择游戏根。

Unity 测试工程为仓库 `UnityProject/`，包名为 `com.nex.html-to-ugui`。完整规则见 [目录与生成规则](../docs/workspace-layout.md)。
