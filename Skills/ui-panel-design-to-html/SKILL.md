---
name: ui-panel-design-to-html
description: 当用户以“新文件目录”调用时，初始化标准化 HTML UI 工作区并生成可运行草稿；也用于从 UI 需求或参考图完成布局审核、组件与素材拆解、HTML 验收及 UGUI/TMP 导入准备。不用于普通网站开发。
---

# UI Panel 设计到 HTML 工作流

一个 Skill 覆盖两个连续阶段：新目录初始化，以及正式 UI Panel 设计到 Unity 导入准备。初始化可以单独完成；正式设计必须经过用户审核布局图。HTML、CSS、预览 JavaScript、清单和审核证据留在 Unity 工程外，独立 `com.nexvr.html-to-ugui` 包负责构建 UGUI/TMP。

## 选择入口

- 用户以“新文件目录”身份调用，或只要求准备 HTML UI 工作区：运行目录初始化，交付可打开的草稿后停止。不要擅自设计业务控件或把草稿称为审核通过。
- 用户提供 UI 需求/参考图并要求完成面板：需要时先初始化工作区，然后继续完整设计流程。若用户只给了需求或参考图，先交付布局图并停在审核关卡。
- 用户给出现有工作区或已批准布局：沿用现有内容，只从当前阶段继续。检查审批是否适用于当前版本；不得覆盖已有内容或虚构审批。

## 初始化标准工作区

默认目标为当前工作目录；尊重用户给出的路径、Panel 名称、标题、分辨率和需求。新目录从目录名推导英文 PascalCase 且以 `Panel` 结尾的名称；无法推导时使用 `MainPanel`。已有 `.html-directory.json` 时沿用并验证其中配置。

从本 Skill 目录运行 `scripts/init_directory.py --root <目标目录>`，按需传入 `--panel-name`、`--title`、`--width`、`--height`；随后运行 `scripts/validate_directory.py --root <目标目录>`。Python 脚本只使用标准库，路径从已加载 Skill 目录解析，不依赖当前项目工作目录。冲突时停止并说明；重复调用保留所有已有文件，不自动覆盖。

初始化器建立固定 `inputs/`、`design/`、`html/` 结构，生成可在浏览器打开的 XHTML/CSS/预览 JS、元素清单和 Unity 导入配置。初始状态为 `draft-unapproved`。不生成业务控件、真实 Sprite、审批证据或项目文档副本。若用户只要求建目录，返回 HTML 入口及创建/保留结果后结束。

## 正式 UI 流程

### 1. 需求或参考图 → 布局图

读取需求、参考图、已批准设计和现有工作区。提取可见字段、控件、重复数据、交互、必要状态和画布尺寸；区分真实数据、示例数据与未知信息。没有影响内容正确性的缺失信息时，记录合理假设继续。

制作预期尺寸的完整 Panel 布局图和需求到可见元素清单，确保字段、标签、单位、动态代表行和状态清楚可读。说明未决问题。把图交给用户审核元素完整性和视觉效果。

**布局图获得用户明确批准前，停止组件拆解、Sprite 制作和最终 HTML。** 修改要求先更新布局图并再次审核。用户明确指定从某张已批准图继续时，视为满足此关卡。批准后将图保存至 `design/review/layout-approved.png`，在 `approval.json` 记录用户批准文字、图片 SHA-256 及限定条件。

### 2. 已批准布局 → 组件、名称、Item 与 Sprite

阅读 [组件命名规范](references/component-naming.md)、[HTML 转换与验收契约](references/html-contract.md) 和 [统一清单契约](references/manifests.md)。整张 UI 只有一个英文 PascalCase 根 Panel，名称以 `Panel` 结尾；所有区域和控件都是它的子物体，不为区域另建 Panel。

逐个元素及转换器辅助节点确定实际用途、父级、精确 Unity 名称、UGUI/TMP 类型、绑定键、数据来源、状态和所属模板。正式命名必须表达用途，类型前缀或全局数字不能替代语义。写入 `elements.json`，并把非根源节点的名称写入 `data-ui-name`，根名称写入 `data-panel-name`。HTML id/class、Unity 物体名和绑定键分别维护。

重复数据用单根 `<template data-prefab="...">` 描述；记录 Item 尺寸、间距、滚动 Content、数据来源与状态。主 Panel 不写死动态行数。Importer 只生成独立 Item Prefab 和通用模板引用；应用层运行时数据、实例化及交互由目标项目实现。

生成每个独立 PNG 图片素材，检查尺寸、透明边缘和缩放边框；在 `sprites.json` 记录来源、用途、像素尺寸、Sprite/Texture 类型、Unity 目标路径和九宫格边界。标签、数值等可编辑内容保留为文本。不能用全屏截图代替可编辑组件，也不能把带文字的截图裁片当独立 Sprite。

### 3. 实现 HTML 并按批准图验收

生成 `html/{PanelName}.html`、CSS、预览脚本、相对路径素材及清单。使用转换器支持的 XHTML/CSS 子集；浏览器预览 JavaScript 只服务于浏览器，不转译成 Unity C#。不支持的效果或控件要明确记录差距，不能静默改成无功能容器。

在批准尺寸下运行浏览器预览，检查字体、图片、脚本、交互和代表性数据状态。核对每个批准元素与 `elements.json` 的对应关系、名称、父级和素材；结合整图与重点裁剪图比较布局、外观和状态。使用 `scripts/compare_visual.py` 生成辅助比较证据，像素指标不能代替视觉判断。按 [验收标准](references/html-contract.md#acceptance) 记录结果；有实质差距则保持未就绪并报告。只有真实审批、完整清单和 HTML 视觉验收全部成立，才可标记 `ready`。

### 4. 可选 Unity 导入

用户要求转换时，使用独立 `com.nexvr.html-to-ugui` UPM 包，选择 Unity 工程外的已验收 HTML。包的统一安装、输入与输出说明维护在其 `README.md`；需要操作 Unity 编辑器时使用 Unity MCP Skill。输出位置为 `Assets/Art/UI/Sprite/{PanelName}/` 和 `Assets/Art/UIPrefab/{PanelName}/{PanelName}.prefab`，Item 位于 `Items/`；根 Panel 实例放在场景 Canvas 下。HTML/CSS/JS 和审核材料不复制进 Unity，Importer 不生成每个 Panel 的业务 C#。

检查 TMP 字形、导入警告、原生组件、Prefab 和场景实例。场景修改标脏但不自动保存。编译或绑定位置正确不能证明视觉一致；按需要比较浏览器与 Unity 截图，并分别报告结构、交互和视觉状态。

## 支持资料

- [使用说明](references/usage.md)：全局安装、调用方式、固定目录与 HTML 生成规则。
- [统一清单契约](references/manifests.md)：元素、模板、Sprite 和 `ready` 证据格式。
- [组件命名规范](references/component-naming.md) 与 [HTML 转换/验收契约](references/html-contract.md)：只在正式组件拆解和 HTML 验收阶段读取。
- [可复用提示词](references/prompt-templates.md)：新目录、需求布局、审批后制作、工作区审核和 Unity 导入。
- [完整流程图](assets/workflow.drawio)：初始化及正式设计/Unity 导入两页；PNG 为预览。

共享说明和流程图只在本 Skill 中维护。只在示例确有专属交接材料时，才在该示例下放置 `docs/`；不得把通用 Skill 手册复制到各示例。
