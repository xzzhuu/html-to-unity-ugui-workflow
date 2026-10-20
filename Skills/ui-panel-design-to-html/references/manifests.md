# 统一准备包契约 v1

html/elements.json：schemaVersion=1、panelName、status、templates、elements。每项元素包含 elementId（等于 hierarchyPath）、unityName、purpose、parentId、hierarchyPath、uguiComponent、sourceType、htmlId、generated、template、bindingKey、dataSource、pictureRegion、states。源节点具有唯一 HTML id、显式 data-ugui 及语义化 data-ui-name。Text 对应 TextMeshProUGUI，InputField 对应 TMP_InputField，Container 对应 RectTransform。

templates 每项包含 name、itemRootId、parentId、bindingScope、itemSize（宽高像素）、stepPx（行步长）、dataSource、states。逻辑路径指向唯一根 Panel 中未来所属列表 Content；template 隔离 Item 绑定。模板键和 Item 名称分别唯一，防止覆盖预制体。条目不是独立 Panel。

辅助物体也登记：按钮有直接文字时生成 Txt_Label；InputField 生成 Group_TextViewport 及 Txt_Placeholder/Txt_Value；ScrollView 生成 Group_Content；Toggle 生成 Img_Checkmark；Slider 生成 Img_Fill、Group_HandleArea 及 Img_Handle。它们 generated=true、sourceType/htmlId=null；purpose 说明其在所属控件中的作用。

html/sprites.json：schemaVersion=1、panelName、sprites。每项包含 source（相对 html/）、path（Unity 目标）、role（Sprite/Texture）、border（L/B/R/T）、pixelSize、hasAlpha（PNG 格式包含 alpha 通道或透明色）、purpose、usage。source 对应 assets/ 加上 path 在根 Panel Sprite 目录后的相对部分。usage=source 包含 data-sprite、data-texture、data-normal-sprite、data-selected-sprite 引用；纯浏览器预览切换所需额外 PNG 使用 preview-state。清单中后者不会自动被 Unity 复制，运行时需要时应补显式映射或交给业务数据层加载。

没有图片时 sprites=[]，没有动态项时 templates=[]。SVG 原稿保存在 inputs/art-source/，参考图保存在 inputs/references/，不能当成审批图。

## 就绪与证据

初始化为 draft-unapproved，转换器示例为 converter-example-unapproved。完整分支实际完成面板、元素组图审批及 HTML 验收后才设 ready；视觉分支完成两项审批及 Unity 替换验收后才设 ready。程序不得制造审批。

design/review/approval.json：status=approved、imageSha256、userApproval（真实审批原文），对应 layout-approved.png。

design/review/html-acceptance.json：status=passed、approvedLayoutSha256、approvedElementGroupsSha256（元素组图确认 JSON 的文件哈希）、checks（elements/naming/sprites/layout/visual/interactions 各为 passed）、filesSha256（html/ 下全部文件及 design/element-groups.json 的工作区相对路径与 SHA-256）。另写 html-acceptance.md，描述实际检查、状态及偏差处理。需求明确无交互时可记录 interactions=passed 并说明原因。

`validate_directory.py --root <目录> --require-ready` 校验结构和记录一致性，不能替代视觉判断。HTML/CSS/JS/PNG/清单改变后旧验收哈希失效；审批图变化后旧审批及验收失效。设计变更重新审核，实现修复重新验收。

## 两个分支共用的组图与素材契约

`.html-directory.json` 保留兼容文件名；新增 mode=full|visual-only，旧文件省略时视为 full。新工作区默认 width=1920、height=1080，已有配置不自动改变。HTML 专属字段与上方旧契约仅属于 full。

`design/element-groups.json`：schemaVersion=1、panelName、requiredComponents、groups、sprites。requiredComponents 只列当前项目实际需要的类型，取 Button/Toggle/InputField/Dropdown/Slider/Scrollbar/ScrollView/Image/RawImage/Text 的实际子集；不是固定全量清单。

每个 group：

- groupId：稳定唯一标识；componentType：上述规范类型；purpose：具体用途。
- usedBy：实际使用的面板/控件层级路径列表，非空。
- sheet：工作区相对 PNG 路径，位于 design/element-groups/，文件名使用组件缩写，例如 Btn_Primary_Sheet.png。
- assets：本组引用的 sprites[].source 列表；纯文字组允许空列表。共享素材可以被多组引用。

每个 sprite：source、componentType、pixelSize、originalPixelSize、hasAlpha、border、tintable、purpose。source 相对工作区，full 位于 html/assets/，visual-only 位于 assets/。文件名使用对应组件前缀，跨组件共用图形登记为 Image/Img。border 仍按 L/B/R/T。pixelSize 是实际导出尺寸；originalPixelSize 是输入源尺寸，新制图填其原生制作尺寸。若降采样，记录 resolutionChangeReason，说明为何必要；默认保留原始像素。此记录不能代替 VR 清晰度检查。tintable=true 表示中性图可通过 Image.color 改色；多色图可填 false。

每个素材必须归属于实际组；每个 requiredComponents 类型在 ready 前必须有组覆盖。不要通过填写虚构需求让额外组件通过校验。示意配色共用素材，不把颜色变体添加到 sprites。

`design/review/element-groups-approval.json`：status=approved、groups。每条确认包含：

- groupId、userApproval（真实用户确认原文）。
- imageSha256：该组 sheet 的文件哈希。
- definitionSha256：该组定义及所引用素材元数据的规范化哈希。
- filesSha256：该组每个素材 source 到实际文件 SHA-256 的映射。

definitionSha256 的输入为 `{ "group": <完整 group>, "sprites": <按 source 排序的本组素材记录> }`，使用 JSON ensure_ascii=False、sort_keys=True、separators=(',', ':') 编码为 UTF-8 后 SHA-256。可调用 `validate_directory.definition_digest(group, sprites_by_source)`，只计算一致性，不能生成用户确认。每组独立计算，修改一组不使无关组确认失效；任何相关素材或定义改变都需要重新确认该组。

审核组图可含候选素材；确认后应导出与确认内容相同的素材。首次补录导出元数据时须核实与已确认组图一致，不能冒用旧确认批准实质不同的素材。

## 仅视觉分支的完成证据

visual-only 不要求 html/ 或 html-acceptance。`design/review/unity-acceptance.json` 记录：

- status=passed、approvedLayoutSha256、approvedElementGroupsSha256。
- checks：structure/visual/interactions/references/resolution 的真实结果均 passed 才标记 ready。
- screenshots：工作区相对的实际 Unity 截图路径，至少一张。
- filesSha256：design/element-groups.json、所有导出素材和截图的文件哈希。

另在同目录记录可读的 unity-acceptance.md：实际 Prefab 路径、素材导入设置、绑定和引用检查、验证状态与目标设备是否测试。截图、记录与哈希只证明证据一致，校验脚本不连接 Unity，不宣称已验证设备。不能把未测 VR 设备填成已通过。

完整分支也要在 Unity 导入后记录实际检查与截图；HTML ready 与“导入转换完成”分别报告。两条路线不新增用于视觉效果的 C# 或主题资产，运行时交付只包含素材与面板预制体。

## POT 尺寸与参考图导入证据

所有新制或本次正式导出的元素素材的 pixelSize 宽、高都必须是 2 的幂次方，长方形合法。保持或提高源图采样细节，通过矢量重制或透明留边处理；有补边时增加 contentRect=[x,y,width,height] 记录有效区域，并据此配置 Sprite 和九宫格。原始组图与面板预览不强制 POT，保留原布局分辨率，不裁切或拉伸。

多面板共用准备包可在 element-groups.json 的 panelPreviews 列出各已确认的面板预览工作区相对路径，逐图沿用真实批准记录；未提供时使用 design/review/layout-approved.png。

两条分支在实际 Unity 导入完成时，unity-acceptance.json 的 referenceImages 记录 source（工作区相对）、unityPath（Assets/ 下独立参考目录）和 sha256，至少覆盖全部元素组图及 panelPreviews。视觉分支的 ready 校验包含此覆盖与源文件哈希。真实导入结果还须通过 MCP 核对目标文件哈希、Texture 尺寸及 NPOT=None/Max Size；机器检查记录一致性不能代替实际导入。HTML ready 不提前要求 Unity 已导入参考图，完整分支的 Unity 完成验收再检查。

正式面板预览规则：每个面板单独输出，画布使用纯色背景，不叠加场景、照片、纹理、棋盘格等干扰；在面板主体外标注画布宽×高和主体宽×高。默认画布 1920×1080；素材 POT 像素尺寸不约束 RectTransform 布局尺寸，也不要求预览图改成 POT。确认后将组图和完整面板预览原分辨率导入 Unity 独立参考目录。

纯色画布配色要求：画布背景必须与面板主体/元素图片主色明显区分，让边界、圆角和透明效果清楚可见；深色面板可使用中性灰底。不同面板可以使用相同的对比底色，不要求各自采用不同颜色。

VR 素材约束：各元素均使用无边缘描线的填充造型，不制作按钮/底板/内衬等细轮廓或额外 Outline 素材；通过填充色、明暗、透明度与留白表达层级及状态。装饰性细分隔线改用留白。九宫格切片边距和审核尺寸标注线保留；去描边不等于已完成设备抗锯齿验收。
