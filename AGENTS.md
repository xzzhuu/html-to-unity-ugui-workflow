<!-- CODEGRAPH_START -->
## CodeGraph

In repositories indexed by CodeGraph (a `.codegraph/` directory exists at the repo root), reach for it BEFORE grep/find or reading files when you need to understand or locate code:

- Use the `codegraph_explore` MCP tool when available, or `codegraph explore "<symbol names or question>"` in the shell.
- If there is no `.codegraph/` directory, skip CodeGraph entirely; indexing is the user's decision.
<!-- CODEGRAPH_END -->

## 当前工程与游戏工作区

- Unity 测试工程为 `E:/html_to_unity_ugui_workflow/UnityProject`。
- 转换包统一为 `com.nex.html-to-ugui`，源码位于 `Packages/com.nex.html-to-ugui/`。
- 星港及新增游戏的 HTML 面板工作区放在 `Workspaces/{GameName}/Panels/{PanelName}/`；游戏根保留 `project.assets.json`、共享素材、审核证据和工具。批量导入选择游戏根；面板 `html/panel.package.json` 的 `projectDirectory` 为 `../../..`。
- 编辑器窗口按钮、字段和菜单子项使用中文。目录规则见 `docs/workspace-layout.md`。

## 星港流程示例与交付约束

- 示例统一使用 `Workspaces/StarportSalvage/` 的六面板制作流程，入口说明为 `docs/game-example/workflow.md`。旧 Example 三面板及源包、正式和暂存资源已按用户要求删除，不再生成或恢复。
- 暂存面板：`Assets/HtmlToUGUIStage/StarportSalvage/{PanelName}/`；共享图片统一位于游戏级 `Sprites/`、`Textures/`。
- 正式主面板：`Assets/Art/UIPrefab/StarportSalvage/{PanelName}/{PanelName}.prefab`；独立 Item：`Assets/Art/UIPrefab/StarportSalvage/Item/`。转换与复制报告留在暂存区。
- 正式共享 Sprite：`Assets/Art/UI/Sprites/StarportSalvage/{组件类型}/...`；去掉暂存桶 `Sprites/`，不得在游戏目录下再套同名层。已有资源移动保留 GUID，以迁移报告关联历史复制证据。
- 复用当前默认 TMP 字体及现有材质；保留原生 UGUI/TMP 与实际需要的通用绑定、Item 目录及 Toggle 状态组件，不新增星港专属视觉脚本。
- 交付前对实际工程运行 `python -B -X utf8 Tools/verify-starport-panels.py --unity-project <工程路径>`，同时核对六个源包、暂存与正式资源、Item、共享图片、字体、引用、指纹及旧 Example 残留。
