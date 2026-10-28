# 统一准备包契约 v1

当前游戏源目录为 `{GameName}/Panels/{PanelName}/html/`；共享 `project.assets.json` 留在游戏根，准备包 `projectDirectory` 从 html/ 回退三级（`../../..`）。批量导入选择游戏根目录。

html/elements.json：schemaVersion=1、panelName、status、templates、elements。每项元素包含 elementId（等于 hierarchyPath）、unityName、purpose、parentId、hierarchyPath、uguiComponent、sourceType、htmlId、generated、template、bindingKey、dataSource、pictureRegion、states。源节点具有唯一 HTML id、显式 data-ugui 及语义化 data-ui-name。Text 对应 TextMeshProUGUI，InputField 对应 TMP_InputField，Container 对应 RectTransform。

templates 每项包含 name、itemRootId、parentId、bindingScope、itemSize（宽高像素）、stepPx（行步长）、dataSource、states。逻辑路径指向唯一根 Panel 中未来所属列表 Content；template 隔离 Item 绑定。模板键和 Item 名称分别唯一，防止覆盖预制体。条目不是独立 Panel。

辅助物体也登记：按钮有直接文字时生成 Txt_Label；InputField 生成 Group_TextViewport 及 Txt_Placeholder/Txt_Value；ScrollView 生成 Group_Content；旧技术 Toggle 生成 Img_Checkmark，三张完整状态图的 Toggle 不生成 Checkmark；Slider 生成 Img_Fill、Group_HandleArea 及 Img_Handle。它们 generated=true、sourceType/htmlId=null；purpose 说明其在所属控件中的作用。

html/sprites.json：schemaVersion=1、panelName、sprites。每项包含 source（相对 html/）、path（Unity 目标）、role（Sprite/Texture）、border（L/B/R/T）、pixelSize、hasAlpha（PNG 格式包含 alpha 通道或透明色）、purpose、usage。source 对应 assets/ 加上 path 在根 Panel Sprite 目录后的相对部分。usage=source 包含 data-sprite、data-texture、data-normal-sprite、data-highlighted-sprite、data-pressed-sprite、data-selected-sprite、data-disabled-sprite 引用；纯浏览器预览切换所需额外 PNG 使用 preview-state。清单中后者不会自动被 Unity 复制，运行时需要时应补显式映射或交给业务数据层加载。

没有图片时 sprites=[]，没有动态项时 templates=[]。SVG 原稿保存在 inputs/art-source/，参考图保存在 inputs/references/，不能当成审批图。

## 就绪与证据

初始化为 draft-unapproved，转换器示例为 converter-example-unapproved。完整分支实际完成面板、元素组图审批及 HTML 验收后才设 ready；视觉分支完成两项审批及 Unity 替换验收后才设 ready。程序不得制造审批。

design/review/approval.json：status=approved、imageSha256、userApproval（真实审批原文），对应 layout-approved.png。

design/review/html-acceptance.json：status=passed、approvedLayoutSha256、approvedElementGroupsSha256（元素组图确认 JSON 的文件哈希）、checks（elements/naming/sprites/layout/visual/interactions 各为 passed）、filesSha256（html/ 下全部文件及 design/element-groups.json 的工作区相对路径与 SHA-256）。另写 html-acceptance.md，描述实际检查、状态及偏差处理。需求明确无交互时可记录 interactions=passed 并说明原因。

`validate_directory.py --root <目录> --require-ready` 校验结构和记录一致性，不能替代视觉判断。HTML/CSS/JS/PNG/清单改变后旧验收哈希失效；审批图变化后旧审批及验收失效。设计变更重新审核，实现修复重新验收。

## 两个分支共用的组图与素材契约

`.html-directory.json` 保留兼容文件名；新增 mode=full|visual-only，旧文件省略时视为 full。新工作区默认 width=1920、height=1080，已有配置不自动改变。HTML 专属字段与上方旧契约仅属于 full。

面板主体按 [尺寸档位与画布占比](../SKILL.md#面板尺寸档位与画布占比)组织。工作区 width/height 表示完整画布，主体档位和宽高在需求/布局/预览元数据中分别记录；B/C 主体不覆盖工作区画布尺寸。D 记录当前文案的实际尺寸与自适应说明，W 记录本地布局尺寸及预览比例。

`design/element-groups.json`：schemaVersion=1、panelName、requiredComponents、groups、sprites。requiredComponents 只列当前项目实际需要的类型，取 Button/Toggle/InputField/Dropdown/Slider/Scrollbar/ScrollView/Image/RawImage/Text 的实际子集；不是固定全量清单。

每个 group：

- groupId：稳定唯一标识；componentType：上述规范类型；purpose：具体用途。
- usedBy：实际使用的面板/控件层级路径列表，非空。
- sheet：工作区相对 PNG 路径，位于 design/element-groups/，文件名使用组件缩写，例如 Btn_Primary_Sheet.png。
- assets：本组引用的 sprites[].source 列表；纯文字组允许空列表。共享素材可以被多组引用。

每个 sprite：source、componentType、pixelSize、originalPixelSize、hasAlpha、border、tintable、purpose。source 相对工作区，full 位于 html/assets/，visual-only 位于 assets/。文件名使用对应组件前缀，同一实际组件类型内复用；禁止跨组件共用图形或将控件子图形改登记为 Image/Img。border 仍按 L/B/R/T。pixelSize 是实际导出尺寸；originalPixelSize 是输入源尺寸，新制图填其原生制作尺寸。若降采样，记录 resolutionChangeReason，说明为何必要；默认保留原始像素。此记录不能代替 VR 清晰度检查。tintable=true 表示中性图可通过 Image.color 改色；多色图可填 false。

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

所有新制或本次正式导出的元素素材的 pixelSize 宽、高都必须是 2 的幂次方，长方形合法。保持或提高源图采样细节，HTML 路线优先通过矢量重制处理；contentRect=[x,y,width,height] 使用左下角为原点、整数像素单位；x/y 非负，宽高为正且整个矩形在 pixelSize 内。0.3.0 仅支持省略此字段或 [0,0,整图宽,整图高]，透明补边的非整图有效区在 HTML 结构校验和转换器均明确拒绝；visual-only 路线允许边界合法的有效区，但必须通过 Unity 人工适配与实际验收。先重制为无需补边的 POT 素材，或走经人工适配的直接 Unity 路线；不可把未支持的补边登记为可导入。border 相对于整张 PNG 的 L/B/R/T，RectTransform 独立控制显示尺寸。原始组图与面板预览不强制 POT，保留原布局分辨率，不裁切或拉伸。

多面板共用准备包可在 element-groups.json 的 panelPreviews 列出各已确认的面板预览工作区相对路径，逐图沿用真实批准记录；未提供时使用 design/review/layout-approved.png。

两条分支在实际 Unity 导入完成时，unity-acceptance.json 的 referenceImages 记录 source（工作区相对）、unityPath（Assets/ 下独立参考目录）和 sha256，至少覆盖全部元素组图及 panelPreviews。视觉分支的 ready 校验包含此覆盖与源文件哈希。真实导入结果还须通过 MCP 核对目标文件哈希、Texture 尺寸及 NPOT=None/Max Size；机器检查记录一致性不能代替实际导入。HTML ready 不提前要求 Unity 已导入参考图，完整分支的 Unity 完成验收再检查。

正式面板预览规则：每个面板单独输出，画布使用纯色背景，不叠加场景、照片、纹理、棋盘格等干扰；在面板主体外标注画布宽×高和主体宽×高。默认画布 1920×1080；素材 POT 像素尺寸不约束 RectTransform 布局尺寸，也不要求预览图改成 POT。确认后将组图和完整面板预览原分辨率导入 Unity 独立参考目录。

纯色画布配色要求：画布背景必须与面板主体/元素图片主色明显区分，让边界、圆角和透明效果清楚可见；深色面板可使用中性灰底。不同面板可以使用相同的对比底色，不要求各自采用不同颜色。

VR 素材约束：各元素均使用无边缘描线的填充造型，不制作按钮/底板/内衬等细轮廓或额外 Outline 素材；通过填充色、明暗、透明度与留白表达层级及状态。装饰性细分隔线改用留白。九宫格切片边距和审核尺寸标注线保留；去描边不等于已完成设备抗锯齿验收。

## 0.3.0 导入与完成契约

- 初始化生成的 panel.package.json 默认 requireSkillApproval=true、stagingRoot=Assets/HtmlToUGUIStage/{PanelName}、allowPrefabReplacement=false；fontMaterial 可指定项目已有共享材质。旧工作区不自动改写；正式游戏导入前补 projectName、项目素材清单及配置，并重新验收。省略 stagingRoot 的旧通用准备包和 Prepared HTML 入口也默认生成到 HtmlToUGUIStage；显式指定的已有暂存路径继续沿用。
- stagingRoot 同时重定向 Sprite/Texture、Item、主 Prefab 和导入报告，不能仅改 outputFolder；字体直接共享。先查看只读 PlanImport，再导入。默认导入窗口更新 HtmlToUGUIStage/StageScene.unity，面板暂存与复制 窗口负责切换预览和勾选复制。复制计划包含依赖和目标指纹；执行前复核，批次失败时恢复目标资源。业务 Prefab 通过项目适配受控更新，不能以 GUID 保留推断内部组件或 UnityEvent 保留。
- HTML 验收比较 html/ 文件集合与逐文件哈希；新增、修改、删除均使原验收失效。候选素材可在组图审核前生成；确认后冻结同一版本，不重复生成再冒用旧哈希。
- designApproved（两项审批）、artifactReady（HTML ready 或视觉素材就绪）、unityAccepted（Unity 完成证据）、deviceAccepted（目标设备实测）分别报告，不把 HTML ready 当作设备或 Unity 通过。
- 两条路线都使用上述 unity-acceptance.json 与 referenceImages 契约。full 分支导入后运行 `python scripts/validate_directory.py --root <工作区> --require-unity --unity-project <Unity工程>`；此参数验证完成记录、截图、素材哈希及实际参考图字节，不替代 Unity 实际显示/交互/设备验证。
- 转换报告包含转换器版本、配置哈希、输入文件哈希、产物路径/GUID/文件及 meta 哈希、计划动作及消费者、警告与待视觉审核状态。报告不包含自身指纹，避免递归哈希。Unity 审批校验不生成确认，也不运行浏览器。

full 的 unity-acceptance.json 另包含 importReport（复制到工作区的实际转换报告路径），filesSha256 覆盖此报告。`--require-unity` 核对报告记录的输入仍为当前版本；指定 `--unity-project` 时还逐个核对实际产物、meta 与 GUID。Unity 验收记录和转换报告是下游证据，不纳入转换报告的上游设计输入哈希，避免自引用。

## 执行门禁与最终接入证据

- full 的 ready 检查强制 requireSkillApproval=true 和安全的 stagingRoot。旧工作区省略 mode 时，Python 与 Unity 均解释为 full；缺少新增门禁配置的旧包需显式补录并重新验收。
- approval.json 的 previews 数组为每张额外 panelPreviews 登记 source、imageSha256、userApproval；layout-approved.png 继续使用原字段。额外预览未批准或哈希过期时拒绝就绪及严格导入。
- 两条分支的 unity-acceptance.json 均须包含非空 finalArtifacts，每项为 path（Assets/ 相对路径）、guid（32 位小写十六进制）、sha256、metaSha256，至少一项为最终业务 Prefab；同时登记交付 Item 与本次交付素材。full 另填 sourceImportReportSha256，等于 importReport 文件哈希。转换报告保存暂存产物历史指纹；工程核对针对 finalArtifacts。暂存区与 StageScene 是持久预览工作区，不作为测试残留自动删除；只有用户明确要求清理具体资源并核对依赖、复制记录和证据保留后，才处理授权范围。最终资源变化须重新验收。
- 校验结果统一输出 designApproved、artifactReady、unityAccepted、deviceAccepted。ready 的旧分支语义继续兼容；缺少 Unity 或设备验证时对应状态不通过。
- 可选 device-acceptance.json 包含 status=passed|failed|not-tested；实际测试时另填 device、testedAt、reviewer、unityAcceptanceSha256、screenshots 及对应 filesSha256。设备结果绑定当前 Unity 验收和实测截图，未测保持 not-tested，不推导为通过。
- 默认使用 TMP 当前默认字体及已有材质，不生成字体材质副本；boldWeight 保留为旧包兼容字段，不修改共享材质，文字粗体使用原生 TMP FontStyles。

## 游戏级素材清单

新游戏在外部项目根目录维护 `project.assets.json`，准备包设置 `projectName`，可用相对 `projectDirectory` 或向上查找确定项目根目录。所有已确认素材在任何 Unity Prefab 生成/导入前统一同步到 `Assets/HtmlToUGUIStage/{GameName}/Sprites`（RawImage 为 Textures）；设计预览阶段只读核对，不提前写入 Unity。单面板与全部面板走同一同步器，全集检查、增量写入、同类型/部件/状态去重。详情见 [项目素材契约](project-assets.md)。

HTML 与 sprites.json 的 path 可以使用 `asset:AssetId`，sprites.json 的 source 保留本面板浏览器副本，哈希必须与项目批准源一致。设计批准、元素组图批准和 HTML 验收仍按原清单版本执行；不能用新建项目注册表替代用户批准。
