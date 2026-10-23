# 工作区、测试工程与生成规则

更新日期：2026-10-24。

## Unity 测试工程与包

本机实际目标工程为 `E:/html_to_unity_ugui_workflow/UnityProject`，使用 Unity 2022.3.62f3c1。所有本次工程验证均针对该目录。

UPM 包名称和源码目录统一为 `com.nex.html-to-ugui`、`Packages/com.nex.html-to-ugui/`。工程清单使用 `file:../../Packages/com.nex.html-to-ugui`；Git 安装地址为 `https://github.com/xzzhuu/html-to-unity-ugui-workflow.git?path=/Packages/com.nex.html-to-ugui`。升级已有工程时移除旧包依赖，再指向新目录；沿用 C#、asmdef 和 meta GUID，保持资源引用。

本机 Unity MCP 是可选开发工具，清单中的本地 SDK 路径需要按机器调整，不属于转换器运行依赖。

## 游戏源目录

```text
Workspaces/StarportSalvage/
├── project.assets.json
├── Panels/
│   ├── StarportHomePanel/
│   ├── ExpeditionSelectPanel/
│   ├── SupplyInventoryPanel/
│   ├── GameSettingsPanel/
│   ├── ActionConfirmPanel/
│   └── ExpeditionResultPanel/
├── shared-assets/
├── shared-art-source/
├── design-review/
├── design-tools/
└── index.html
```

每个面板目录保留 `inputs/`、`design/`、`html/` 和 `.html-directory.json`。共享素材、项目清单、项目级证据和工具留在游戏根目录，不能随某个面板移动。

新增游戏使用相同的 `Panels/{PanelName}/` 结构。初始化前在游戏根维护 `project.assets.json`，再运行：

```sh
python -B -X utf8 Skills/ui-panel-design-to-html/scripts/init_directory.py --root Workspaces/StarportSalvage/Panels/NewPanel
```

初始化器识别父目录 `Panels` 及游戏根清单，自动设置项目名称和暂存路径。准备包的 `projectDirectory` 与 HTML 的 `data-project-directory` 均相对 `html/`，值为 `../../..`。初始化只生成未审核草稿；重复执行保留已有文件。现有星港导出工具同步使用 `Panels/`，其中历史设计工具不能重新执行覆盖已批准内容。

批量导入选择 `Workspaces/StarportSalvage`；该根目录包含 `project.assets.json`，生成前同步全游戏素材。浏览器总入口为根目录 `index.html`，页面间相对跳转仍在 `Panels/` 的同级面板之间进行。

Unity 暂存路径为 `Assets/HtmlToUGUIStage/StarportSalvage/{PanelName}/`，共享素材位于游戏级 `Sprites/`、`Textures/`。源目录增加 `Panels` 不会在 Unity 暂存区增加同名层级。

正式共享 Sprite 位于 `Assets/Art/UI/Sprites/StarportSalvage/{组件类型}/...`；复制时去掉暂存桶 `Sprites/`，不生成 `StarportSalvage/Sprites/` 重复层。现有星港资源以 Unity 移动保留 GUID，交付清单同时记录当前路径与迁移报告，原复制报告作为历史证据保留。

## 中文编辑器入口

`Tools → HTML to UGUI` 下提供“导入单个面板...”“导入整个项目...”和“面板暂存与复制...”。所有窗口按钮、来源选择、字段和操作提示使用中文；文件名、资源路径及 API 名称保持技术标识。

先查看导入计划，再生成暂存资源。正式交付依照 [复制规则](stage-promotion.md)；星港六面板作为唯一完整流程示例，正式根为 `Assets/Art/UIPrefab/StarportSalvage`，字体按用户要求使用现有 `PICOSansVFESC SDF`。制作与对照见 [星港流程](game-example/workflow.md)。旧 Example 三面板已退役。

## 展示与证据

当前仓库以星港六面板工作区作为完整流程示例。旧 Inventory、Settings、Starter 三组技术示例已删除。塔吊展示的源包、设计工作区、截图和 Unity 专用素材也已移除；塔吊移除前的本机备份位于仓库外 `E:/html_to_unity_retired_20261024/`（源文件与 Unity 素材分别归档），不参与打包或 GitHub 展示。

Editor 回归测试的合成资源统一暂存在 `Assets/HtmlUiValidation/{本次运行标识}/`，成功或失败都清理本次资源，父目录为空时一并删除。旧测试路径 `Assets/Generated/` 不再使用。测试期间目录可能短暂出现；报告与对照截图保留在 `Assets` 之外的 `Audit/Runs/`，不依赖这些临时资源。

历史审核报告保留发生时的验证范围和原始证据，不将旧工程结果写成本次验证。星港迁移保留原批准图像、素材和批准原文，仅更新定位信息及对应验收指纹；Unity 视觉、业务接线与设备验收状态分别保留。

验证命令见 [Tools](../Tools/README.md)。交付前必须运行：

```sh
python -B -X utf8 Tools/verify-workflow-files.py
python -B -X utf8 Tools/verify-starport-panels.py --unity-project E:/html_to_unity_ugui_workflow/UnityProject
```
