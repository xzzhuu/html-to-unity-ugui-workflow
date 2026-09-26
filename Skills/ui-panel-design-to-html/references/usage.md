# UI Panel 设计到 HTML 工作流使用说明

## 安装与调用

在本仓库运行 `python -X utf8 Tools/install-global-skills.py`，将单一工作流安装到全局 Codex Skill 目录。默认 Windows 位置为 `~/.codex/skills/ui-panel-design-to-html`（或 `$CODEX_HOME/skills/ui-panel-design-to-html`）；手动安装时复制完整文件夹，不要只复制 `SKILL.md`。新任务会重新发现全局 Skill，不需要在各个项目中重复安装。

只初始化工作区：`$ui-panel-design-to-html 我以“新文件目录”的身份调用，请在当前目录初始化。`

正式面板设计：`$ui-panel-design-to-html 请根据需求文档和参考图制作 UI Panel，先给出布局图和完整性清单供我审核。`

命令行初始化示例：

```powershell
python -X utf8 Skills/ui-panel-design-to-html/scripts/init_directory.py --root ./MyUI/Inventory --panel-name InventoryPanel
python -X utf8 Skills/ui-panel-design-to-html/scripts/validate_directory.py --root ./MyUI/Inventory
```

初始化脚本只依赖 Python 标准库，不需要 Unity、npm 或 Draw.io。重复调用保留已有内容；配置冲突时停止，不自动覆盖。

## 固定工作区结构

```text
目标目录/
  .html-directory.json
  inputs/
    brief.md
    references/
  design/
    layout/
    review/
  html/
    {PanelName}.html
    {PanelName}.css
    {PanelName}.preview.js
    elements.json
    sprites.json
    panel.package.json
    assets/icons/
    assets/sprites/
    assets/textures/
```

目录用小写英文；根名使用英文 PascalCase 且以 `Panel` 结尾。初始化生成可打开的 XHTML 草稿并标记 `draft-unapproved`。Skill 手册、流程图和提示词集中存放于 Skill，不复制进工作区；只在项目有独立交接说明时再按需建立 `docs/`。

## HTML 与审核规则

初始化不杜撰业务控件、Sprite 或用户审批。正式面板流程先制作布局图并等待用户确认元素完整性和视觉效果。获批后再确定每个 UI 物体的实际用途、语义化 Unity 名称、父级、UGUI/TMP 类型、数据绑定和状态，准备动态 Item 与独立 Sprite，并制作匹配布局图的 HTML。

HTML 使用相对 CSS/JS/素材路径及 XML 可解析标记；根节点通过 `data-root="true"` 和 `data-panel-name` 标识。非根节点用 `data-ui-name` 对应 `elements.json`。使用像素定位和转换器支持的 CSS 子集；不支持效果、动态逻辑或需要重建的图表须在验收中说明。浏览器预览 JS 不会转为 Unity 业务 C#。

元素、模板、Sprite 字段和就绪证据以 [统一清单契约](manifests.md) 为准。结构校验通过不代表用户批准或视觉验收通过；只有审批记录、清单完整性和 HTML 验收证据齐备后才能标记 `ready`。

## 独立 Unity package 对接

使用 `com.nexvr.html-to-ugui` UPM package。源码位于本仓库 `Packages/com.nexvr.html-to-ugui/`，也可通过 Package Manager 从 `.tgz` 安装。包支持 Unity 2022.3，声明依赖 UGUI 1.0.0 与 TMP 3.0.7。安装、输入、输出和实现边界统一见包自己的 `README.md`。

从 Unity 的 `Tools → HTML to UGUI → Import Panel...` 选择工作区外的 `html/{PanelName}.html`。图片导入至 `Assets/Art/UI/Sprite/{PanelName}/`，主预制体至 `Assets/Art/UIPrefab/{PanelName}/{PanelName}.prefab`，动态 Item Prefab 位于 `Items/`。根实例放在场景 Canvas 下。HTML/CSS/JS、布局图和审核记录不复制进 Unity；业务数据和交互由目标项目实现。

完成后运行 `scripts/validate_directory.py --root <目标目录> --require-ready`。此校验核实清单、模板、素材和审批/验收证据；浏览器运行、互动及视觉一致性仍需按实际输出检查。
