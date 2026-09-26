# UI Panel 设计到 HTML 工作流提示词

按当前需求复制适用的一段；替换方括号内容。只初始化目录时，不会进入布局设计；正式设计始终先经过布局图审核。

## 只创建新文件目录

```text
$ui-panel-design-to-html 我以“新文件目录”的身份调用。请在当前目录初始化标准 HTML UI 工作区，基于目录名推导以 Panel 结尾的根名；如有需求文档先阅读。生成可打开的 XHTML 草稿和组件清单，保持 draft-unapproved，不覆盖已有内容。只做目录初始化，完成后报告 HTML 入口和创建/保留结果。
```

## 在指定位置初始化

```text
$ui-panel-design-to-html 我以“新文件目录”的身份调用。请在 [路径] 初始化 [PanelName]，画布为 [宽]×[高]，用途是 [用途]。保留现有需求和参考文件，先生成未审核草稿，不擅自添加业务控件或审批证据。
```

## 从需求或参考图开始设计

```text
$ui-panel-design-to-html
请基于我提供的需求文档和参考图，为 [业务名称] 制作完整 UI Panel。提取画布、业务要素、数据状态和交互，按需初始化工作区，先给出布局图及元素完整性清单。停在布局审核点，等我审核内容和视觉效果；不要提前拆解正式组件、生成 Sprite 或完成 HTML。
```

## 我批准布局后继续

```text
$ui-panel-design-to-html
我批准了 [布局图文件/版本]，批准意见为：[原文]。请按此版布局确定每个组件的实际用途、Unity 名称、父级、UGUI/TMP 类型、绑定和状态；拆解动态 Item 与独立 Sprite，制作匹配批准图的 XHTML/CSS 和浏览器预览。完成结构和视觉验收并记录证据；有偏差时明确说明，不要伪造通过。
```

## 审核已有工作区

```text
$ui-panel-design-to-html
请审核 [目录] 的 UI 准备包：核对唯一根 Panel、组件语义名称和父级、辅助物体、Item 模板归属/尺寸/数据源、Sprite 源图及 Unity 路径、XHTML/CSS/JS 运行状态，以及与适用批准布局的完整性和视觉一致性。按元素和状态列出缺失/偏差与证据；没有真实审批记录时保持未审核状态。
```

## 导入已验收 HTML

```text
$ui-panel-design-to-html
请在目标 Unity 工程通过独立 com.nexvr.html-to-ugui package 选择工程外的已验收 [PanelName].html，生成唯一根 Panel、动态 Item Prefab 并导入清单引用的图片。检查命名、TMP 字形和转换警告。HTML/CSS/JS 保持工程外，不翻译或生成业务 C#；这次只构建 UI 资源。报告仍需处理的结构和视觉差异。
```

完整流程图见 [workflow.drawio](../assets/workflow.drawio)，目录、HTML 与包规则见 [使用说明](usage.md)，清单格式见 [统一清单契约](manifests.md)。
