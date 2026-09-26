# 统一准备包契约 v1

html/elements.json：schemaVersion=1、panelName、status、templates、elements。每项元素包含 elementId（等于 hierarchyPath）、unityName、purpose、parentId、hierarchyPath、uguiComponent、sourceType、htmlId、generated、template、bindingKey、dataSource、pictureRegion、states。源节点具有唯一 HTML id、显式 data-ugui 及语义化 data-ui-name。Text 对应 TextMeshProUGUI，InputField 对应 TMP_InputField，Container 对应 RectTransform。

templates 每项包含 name、itemRootId、parentId、bindingScope、itemSize（宽高像素）、stepPx（行步长）、dataSource、states。逻辑路径指向唯一根 Panel 中未来所属列表 Content；template 隔离 Item 绑定。模板键和 Item 名称分别唯一，防止覆盖预制体。条目不是独立 Panel。

辅助物体也登记：按钮有直接文字时生成 Txt_Label；InputField 生成 Group_TextViewport 及 Txt_Placeholder/Txt_Value；ScrollView 生成 Group_Content；Toggle 生成 Img_Checkmark；Slider 生成 Img_Fill、Group_HandleArea 及 Img_Handle。它们 generated=true、sourceType/htmlId=null；purpose 说明其在所属控件中的作用。

html/sprites.json：schemaVersion=1、panelName、sprites。每项包含 source（相对 html/）、path（Unity 目标）、role（Sprite/Texture）、border（L/B/R/T）、pixelSize、hasAlpha（PNG 格式包含 alpha 通道或透明色）、purpose、usage。source 对应 assets/ 加上 path 在根 Panel Sprite 目录后的相对部分。usage=source 包含 data-sprite、data-texture、data-normal-sprite、data-selected-sprite 引用；纯浏览器预览切换所需额外 PNG 使用 preview-state。清单中后者不会自动被 Unity 复制，运行时需要时应补显式映射或交给业务数据层加载。

没有图片时 sprites=[]，没有动态项时 templates=[]。SVG 原稿保存在 inputs/art-source/，参考图保存在 inputs/references/，不能当成审批图。

## 就绪与证据

初始化为 draft-unapproved，转换器示例为 converter-example-unapproved。实际完成用户布局审批、HTML 完整性及视觉验收后才设 ready；程序不得制造审批。

design/review/approval.json：status=approved、imageSha256、userApproval（真实审批原文），对应 layout-approved.png。

design/review/html-acceptance.json：status=passed、approvedLayoutSha256、checks（elements/naming/sprites/layout/visual/interactions 各为 passed）、filesSha256（html/ 下全部文件的工作区相对路径与 SHA-256）。另写 html-acceptance.md，描述实际检查、状态及偏差处理。需求明确无交互时可记录 interactions=passed 并说明原因。

`validate_directory.py --root <目录> --require-ready` 校验结构和记录一致性，不能替代视觉判断。HTML/CSS/JS/PNG/清单改变后旧验收哈希失效；审批图变化后旧审批及验收失效。设计变更重新审核，实现修复重新验收。
