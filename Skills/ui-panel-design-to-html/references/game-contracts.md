# 游戏契约、重导入与统一验收

新增游戏、维护多面板一致性或准备重新导入时读取本说明。游戏根仍为 `Workspaces/{GameName}/`，面板仍在 `Panels/{PanelName}/`，`project.assets.json` 继续管理素材身份。

## 初始化与只读检查

从 Skill 根运行，或在仓库使用 `Tools/game-workflow.py` 同名入口：

```text
python scripts/game_workflow.py init --root <游戏根>
python scripts/init_directory.py --root <游戏根>/Panels/<PanelName>
python scripts/game_workflow.py check --root <游戏根>
python scripts/game_workflow.py status --root <游戏根> --unity-project <Unity工程>
python scripts/game_workflow.py snapshot --root <游戏根> --output <基线.json>
python scripts/game_workflow.py impact --root <游戏根> --baseline <基线.json>
```

初始化保留已有文件，只创建未批准草稿。`check` 汇总字段问题；`status` 按面板区分 passed、failed、not-checked、stale。`snapshot` 记录当前输入指纹，`impact` 输出受影响面板和重验范围，不修改设计批准或自动重建。

## 唯一配置来源

- `contracts/style.json`：画布、语义颜色、字体层级、间距、圆角和预览字体依赖；后续生成器读取此文件。
- `contracts/screens.json`：页面入口、操作绑定、状态、fixture 与测试编号；只登记实际需求。
- `contracts/fixtures.json`：可选的固定验收数据与可观察预期；HTML/Unity 可共享数据，不将其当作游戏服务。
- `contracts/native-assembly.json`：可选的原生组件装配。`draft` 不应用，`active` 启用配置；这两个状态不是设计批准。
- `project.assets.json`：已确认素材、组件归属、三态、九宫格、导入配置与源哈希。

字段形状见 [style Schema](schemas/style.schema.json)、[screens Schema](schemas/screens.schema.json)、[native assembly Schema](schemas/native-assembly.schema.json)。Schema 用于编辑和基础结构提示；路径、批准版本、素材身份和两端实际行为仍由现有语义校验验证。v1 允许附加说明字段，不改变既有字段含义；不兼容变更须增加版本并提供迁移。

## 无参考设计与 XR 风格包

本节用于首次整体设计或需要重新确定风格的任务。仅初始化时不读取范例、不生成控件。用户参考和已批准体系优先；初始化器内置的深色 token 只是草稿占位。四套方向是本 Skill 对官方原则的适配，不是官方主题、SDK 或平台认证。默认以黑灰/中性灰或淡蓝科技配色打底，不主动使用紫色、薰衣草色或紫蓝渐变；操作强调用淡蓝/蓝色，必要的语义状态保留识别色。平台风格优先借鉴结构和交互层级，不强制使用品牌色。

### 选择与视觉参考

先查看[四方向同尺度总览](../assets/style-presets/Overview.png)，再按平台只读取候选目录的 `tokens.json` 和图片，不一次加载全部资源。游戏级一致性优先，不能每个面板随机换风格。

| 方向与目录 | 设计意图和结构配方 | 可查看范例 |
| --- | --- | --- |
| `meta-friendly` Meta 友好交互 | 明亮中性表面、蓝色主操作；轻量导航、分组设置行、图标操作卡与中性实心次操作，强调行动权重 | [设置](../assets/style-presets/meta-friendly/SettingsPanel.png)、[监控](../assets/style-presets/meta-friendly/MonitoringPanel.png)、[组件一览](../assets/style-presets/meta-friendly/Components.png) |
| `pico-spatial` PICO 空间工作区 | 黑灰工作区、淡蓝科技色强调；侧导航、边缘工具区、主内容与上下文信息并列，采用分组表面和紧凑工具按钮 | [设置](../assets/style-presets/pico-spatial/SettingsPanel.png)、[监控](../assets/style-presets/pico-spatial/MonitoringPanel.png)、[组件一览](../assets/style-presets/pico-spatial/Components.png) |
| `microsoft-mrdl` 微软空间工具 | 石墨蓝表面、青绿强调；模块化底板、独立参数模块、清楚的层级和状态，主要操作与工具区分开 | [设置](../assets/style-presets/microsoft-mrdl/SettingsPanel.png)、[监控](../assets/style-presets/microsoft-mrdl/MonitoringPanel.png)、[组件一览](../assets/style-presets/microsoft-mrdl/Components.png) |
| `spatial-soft` Apple 空间柔和 | 温和浅灰、克制的蓝色强调；横向分类、连续设置表面、留白与嵌入式信息，减少卡片套卡片 | [设置](../assets/style-presets/spatial-soft/SettingsPanel.png)、[监控](../assets/style-presets/spatial-soft/MonitoringPanel.png)、[组件一览](../assets/style-presets/spatial-soft/Components.png) |

以上配色与布局是原创适配选择，不声称是各平台的官方色值。PNG 均有同名可编辑 SVG；所有范例是合成内容、`draft`、未批准，不导入用户工程，不计入正式组图、素材或验收覆盖。组件一览跨类型展示是风格说明，实际项目仍逐组制作和确认。SVG 保留可编辑文字；正式运行时文字使用 TMP，不把 SVG/整张 PNG 当面板。

范例中的导航、连接状态、工作区摘要、“关闭后保持世界位置”“保存后生效”和 66% 都属于该合成示例，不是用户需求或真实默认值。仅借鉴造型与层级；没有业务依据时不复制这些控件、状态和文案，也不编造删除是否可撤销等后果。

无参考的代表页面默认比较：PICO → `pico-spatial` / `microsoft-mrdl`；Meta → `meta-friendly` / `microsoft-mrdl`；其他或未知平台 → `meta-friendly` / `spatial-soft`。明确指定单一风格时只制作该方向；用户要求别的风格时不局限于四套。候选用相同内容、尺寸、输入、状态与数据，至少能指出结构、布局或表面层级的实际差异，不用换色、增加假功能或把其中一套故意做简陋来对比。轻量美化不自动改变已有主体尺寸、布局或绑定。风格选择属于第一次面板确认，未选定前不扩展所有面板，也不制作正式组图或业务 HTML。

### 页面与组件配方

先确定页面的首要任务和主操作，再决定信息分组，最后选组件造型。给当前方案写一句具体设计意图，例如“将空间定位与显示设置分开，保存动作固定在内容底部”，不用“高级、现代、科技感”代替结构决定。

| 角色 | 推荐结构和状态表达 | 原生落地 |
| --- | --- | --- |
| 主操作 | 实心强调填充，明确动词，必要时加语义图标；同一区域避免并列多个最高强调 | Button + Image + TMP；Color Tint 表达悬停/按下/禁用 |
| 次操作 / 工具按钮 | 使用可辨识的中性填充，图标与文字按职责组合；不能把“次操作”做成融入底图的透明文字；危险操作有明确文案 | Button + Image + TMP；命中区域可大于可见形态，原生 Color Tint 保持各状态可辨 |
| 操作卡 | 图标、标题、必要说明或状态；图标位、文字排列与普通按钮不同，整卡行为必须清楚 | Button 下属 Image/TMP，复用现有绑定 |
| 设置行 | 标签与说明在一侧，Toggle/Slider/Input 在稳定对齐的另一侧；属于同组的行可共用区域底板，不每行再套卡片 | 现有控件与布局容器；Toggle 严格用 Off/On/Disabled 三态完整 Surface |
| 数据行 / 参数模块 | 标题、数值、单位有层级；数值按小数位/列对齐；用留白或交替填充分组 | TMP + Image，现有 ScrollRect/Item；不新增业务控制器 |
| 状态 / 告警 | 图标或文字与色彩共同说明；区分操作反馈和持续业务状态；空/加载/错误只覆盖实际需求 | Image/TMP 与已有业务显示逻辑；不把原生 Selected 当 Toggle On 或业务选择 |

“专业工作台”组合导航、内容、上下文工具与保存区；“工业监控”组合少量关键指标、设备表格、告警详情与时间信息。它们是页面模式，不是额外固定配色包。根据任务选模式；确认窗只保留说明和必要操作，世界空间操作卡保持项目本地尺寸，不机械套侧栏、指标卡或固定标题栏。

无描边不等于只画一种圆角矩形：通过整体轮廓、填充层级、图标与标签关系、内边距、表面组合区分职责。不要为了“丰富”强制增加渐变、装饰、嵌套卡片或图标数量。相同组件类型内同形颜色变化共用素材；确有不同结构才新增相应组件前缀素材。

### 交互组件可辨识规范

可操作性必须在默认、未悬停状态就清楚，不能靠用户先把射线移上去才发现。独立按钮、工具图标按钮、可点卡/行、导航项、输入/下拉、Toggle、Slider、Scrollbar 都按本节检查；静态底板、只读说明与不可点数据行不因此变成按钮。仅保留透明命中 Graphic 不能代替可见的交互形态。

**检查基线：**用于识别控件或状态的必要图形与其实际相邻色至少 **3:1**；普通控件文字（包括必要的输入占位提示）至少 **4.5:1**。按角色选择识别方式：独立动作按钮、输入/拖动控件默认采用有辨识度的形态；导航由分组、图标与标签、间距和当前项共同表达，不要求每项有独立强填充。确有行内链接需求时说明持续可见的链接标识和核对结果，不用这一例外把“恢复默认”等独立动作改成透明文字。

3:1 参考 [W3C 非文本对比度](https://www.w3.org/WAI/WCAG22/Understanding/non-text-contrast.html)，不用颜色作为唯一线索参考 [W3C 色彩使用](https://www.w3.org/WAI/WCAG22/Understanding/use-of-color.html)。W3C 并不要求每个控件都画出命中边界；本 Skill 为独立动作按钮采用可辨填充，但不将其泛化为所有导航、容器或背景都必须高对比。它是设计检查基线，不是 XR 专用标准或整体验证通过声明。

| 组件 | 具体检查 |
| --- | --- |
| 主/次按钮、工具按钮、可点卡/行 | 默认可见填充与所在表面有足够明度差，标签/图标可读；主次通过语义色、尺寸、位置和分组表达，不靠让次操作消失。图标按钮保留可辨底座；可点行与静态行明确区分 |
| 导航/Tab/分段选择 | 导航区域底色只与底图轻微区分，也可完全沿用底图；未选中项默认不铺按钮色块。保证文字和语义图标可读，当前项通过字重、柔和底色或已有状态标识表达；不把原功能图标替换成勾选图标。3:1 核对必要图标/状态标识，4.5:1 核对文字，不强求导航底板对底图达到 3:1。选择与聚焦反馈使用既有状态能力，不新增状态脚本 |
| Input/Dropdown | 输入或触发区域与外部背景分开；当前值、占位提示、下拉箭头及必要编辑/展开反馈可辨，不只让 Label 漂在底板上 |
| Toggle/复选与单选 | 启用的 Off 不是 Disabled；Off/On 轨道对外部背景、滑块/状态标识对轨道分别核对。位置、形态或标记与颜色共同表达选择，沿用既定三态 Surface 契约 |
| Slider/Scrollbar | 未填充轨道不能融入父背景；填充/滑块与轨道、可拖拽部分与周围表面分别核对。连续填充与突出滑块可为同色，但滑块位置和拖动形态必须可见；不可用模糊阴影代替轨道或滑块 |

正常、悬停、聚焦、按下和选中各状态都不能丢失必要对比；不硬性要求不同时显示的状态彼此也达到 3:1。聚焦反馈要持续可见，不能只依赖瞬间动画；错误/选择不只靠红绿或蓝灰色相。真正不可用的 Disabled 可降低权重并明确不可操作，但不将未悬停、Off、未选中或只读内容自动按 Disabled 豁免。

VR 继续禁止边缘描线：按组件角色使用填充、稳定留白、可辨标记和原生状态素材，不用细边框、光晕或发光轮廓补救。不要为了达到对比值把导航或所有装饰卡片变成高对比块；先明确需要辨识的是文字、图标、状态标识还是操作区域，再对该部分核对。

语义 token：独立操作使用 `secondary/onSecondary`、`controlHover/controlFocus/controlPressed`；拖动控件使用 `controlTrack/controlFill`；导航区域使用轻量的 `navBackground/navSelectedSurface`，不用次按钮色覆盖全部导航项。各预设提供浅/深背景适用值，使用时针对实际所在表面复核，不能认为“用了 token 就一定通过”。文字与子图标采用能覆盖各状态的前景；原生 Selectable 的 Color Tint 不会自动改变所有子节点颜色。Sprite tint、Image.color、Selectable 颜色相乘，以及 CanvasGroup/贴图 Alpha 都要计入最终合成色，不为视觉新增 C#。

数值核对记录组件、状态、识别部位、实际前景/相邻背景、最低值与结果。半透明先在实际背景上合成；背景未知时保持待验证。阈值判断使用未四舍五入的比值，显示值仅供阅读。重看灰度或缩小预览，检查默认可操作性、主次、形态和空间距离；数字达标不能替代图像评审或头显验证。

范例渲染器为实际组件记录 `interactionContrasts` 并拒绝未达基线的图；可独立执行 `node scripts/interaction_contrast.cjs assets/style-presets/examples.json`，结果只表示参考色对检查，不产生用户批准或 Unity/设备通过。组件一览增加输入、下拉和滚动条的**造型示范**，不表示 HTML 转换器新增了这些构造器；正式任务继续使用既有组件与分支能力。

### token 与项目风格记录

每个目录的 `tokens.json` 是可校验的 style v1 **示范数据**，`projectName=XRStyleExample`。只将所选方向需要的 `colors`、`typography`、`spacing`、`geometry` 及设计说明适配到项目现有 `contracts/style.json`；保留实际 projectName、画布、已有额外 token、字体、平台、面板与用户设置，不整文件覆盖。旧示例兼容 token 如 `teal`/`amber` 仍可保留，新增设计优先用 `primary`/`warning` 等语义角色。已有项目只修改本次授权的字段。

v1 增加以下**可选说明**，无新必填字段或自动迁移：

- `designDirection`：presetId、presetVersion、sourceUrls、signature、componentRecipes、iconFamily、interactionPolicy。记录参考来源、交互外观约定与本项目实际适配；自定义风格同样可用，预设只是起点。
- `xrContext`：targetPlatform、displayType、inputModes、panelSizeMeters、viewingDistanceMeters、verification。尺寸和距离未知时为 null，输入未知时为空数组，verification 保持 `not-checked`；不要从范例推断项目已经支持手势或已通过设备验收。

这些说明不安装 SDK、不改变 Canvas、不启用交互，也不是设计批准；现有 snapshot/impact 会把 style 的变化纳入重验。选择 `pico-spatial` 不代表切换 PICO Spatial 模式，当前使用 PICO XR 的项目继续保持其 SDK/输入配置。单面板旧工作区没有游戏契约时，先在已有 `inputs/brief.md` 记录方向与 XR 条件，不为使用预设强制迁移工作区。

### XR 可读性与交互检查

1. 核对真实设备、显示类型、Canvas 模式/世界缩放及现有输入，分别评估手柄射线、手部射线和直接触碰；不凭平台名称假设存在眼动或捏合能力。
2. 区分可见图形与点击区域。小图标可用现有原生透明 Graphic 扩大区域，核对相邻区域不重叠、父节点裁切、射线遮挡和事件绑定。手部交互通常需要更宽松的目标与间距；具体阈值按所用平台/SDK及设备实测确定。Meta 文档中的 dp 不是 Unity 布局像素，不能直接把 48/60dp 写成通用 VR 像素阈值。
3. 记录面板世界宽高、预期观看距离、文字实际字形高度。高度 h、距离 d 使用同一单位，可估算视角 `2*atan(h/(2*d))*180/pi`；这只是几何信息，不能代替中文字体、设备分辨率及观看条件的可读性测试。布局字号、贴图像素和 TMP 字形高度不是同一个量。未知项保持待验证。
4. 主要操作放在舒适可达/可见区域；避免必须长期抬头、过度转头或连续悬臂的布局。需要手边菜单、跟随、拖拽或曲面时先核对项目已有能力，不从范例自动新增行为。
5. PICO/Meta 视频透视和 HoloLens 光学透视分开评估。后者的加色显示不能照搬不透明屏幕的黑色/透明度逻辑；白字也须核对现实背景。正式确认图仍使用有对比的纯色背景，必要的额外明暗验证单独保存。
6. 覆盖实际需要的正常、悬停/聚焦、按下、禁用及业务状态。无描边时仍须用填充、亮度、标签或图标提供清楚反馈；不把去掉焦点描边误解为去掉焦点反馈。

每个方案说明效果对应的组件/素材路径：填充与圆角 → Image/九宫格；图标 → 中性 Sprite；轻微静态明暗 → 已批准素材；文字 → TMP；状态 → 原生 Selectable/既有业务组件。实时模糊、折射、手指接近光效、立体按压、视线跟随和合成层必须标明额外依赖；当前项目没有时给可实现替代，不在基础预览中假装已经具备。不要为匹配平台范例新建 C#、材质或 Shader。

### 图标来源与制作

优先复用项目已批准图标。没有可用图标时，默认以 **Fluent System Icons 的 Filled** 建立统一图标族；需要其他气质时整组选择 Phosphor Fill 或 Heroicons Solid。它们是适用于多种风格的资源，不代表使用这些图标就成为官方主题。实际使用前检查所选版本及文件许可；以下为 2026-10-28 核对结果。

用户明确要求某个面板或探索方案使用其他图标族时，仅在该范围遵从并记录例外，不因此替换全项目已批准图标；同一方案内部仍保持一致。

| 仓库 | 已核对许可 | 适用与选取规则 |
| --- | --- | --- |
| [microsoft/fluentui-system-icons](https://github.com/microsoft/fluentui-system-icons) | [MIT](https://github.com/microsoft/fluentui-system-icons/blob/main/LICENSE) | 默认；优先同一网格尺寸的 Filled，覆盖工具、设备、空间操作与常见业务 |
| [phosphor-icons/core](https://github.com/phosphor-icons/core) | [MIT](https://github.com/phosphor-icons/core/blob/main/LICENSE) | 使用 `assets/fill`；轮廓较圆润，同项目保持同一 weight，不混入 Thin/Light |
| [tailwindlabs/heroicons](https://github.com/tailwindlabs/heroicons) | [MIT](https://github.com/tailwindlabs/heroicons/blob/master/LICENSE) | 使用 `optimized/24/solid` 等同一尺寸系列；适合简洁操作图标，不混用 Outline |

上述 MIT 条款允许使用、修改与商业分发，分发时须保留版权和许可。只取实际需要的 SVG，不安装前端运行库或全量下载图标集。不存在合适图标时，以所选图标族的网格、视觉重量和转角重制业务符号，并标明改动；不要把任意线性图标直接加粗、扩线后声称已符合 VR 要求，也不用 emoji、字母或随机几何图形代替功能图标。平台品牌标识不作为默认功能图标。

流程：确定语义 → 在同一族中选择 Filled/Solid → 核对小尺寸识别与负空间 → 在矢量层统一光学大小、居中和必要的简化 → 仅对单色图标规范化 fill（保留透明孔洞，不整体填实）→ 按实际清晰度需要从 SVG 导出 64/128/256 等 POT PNG → 按所属组件命名并登记组图。Button 的子图标用 `Btn_..._Icon`，不能用 Img 前缀绕过组件归属。导出尺寸不是通用固定值，不按显示尺寸降低源细节；同形颜色变化使用 Image.color。SVG 源留在工程外，Unity 使用已确认 PNG 和原生 Image。

在已有素材清单的附加 `iconSource` 中记录 repository、commit/tag、sourcePath、sourceSha256、license、licensePath、modifications 与导出映射；未修改也写明。使用或重制开源图标均保留对应版本 LICENSE，随素材交付到项目已有的第三方许可位置；没有位置时使用 `Assets/Art/UI/ThirdPartyNotices/{Library}/LICENSE.txt`，不要把署名塞进界面。Skill 内的少量默认 SVG、完整 MIT 及来源指纹见 [图标清单](../assets/icon-library/sources.json)，它们不属于项目批准素材，不能直接充当批准记录。没有网络时可用这些已核对的本地图标，不编造未取得的来源。

### 视觉自评、重制与来源

提交前实际查看图像，逐项写具体问题或通过依据：首要任务/主操作是否可见；正文与数值是否有层级；是否机械重复卡片；图标是否统一且语义明确；中文有无裁切；必要状态是否能辨认；XR 条件是否已知；能否使用既有组件装配。问题先修订再提交，不用自动评分或结构校验替代美术判断。

范例重制仅用于维护 Skill 资源：`node scripts/render_style_examples.cjs --sharp-module <已安装sharp目录或入口>`，可加 `--output <外部目录>`。脚本离线读取 token/本地图标，核对交互对比后重建 12 张 SVG/PNG 与总览、几何与对比数据，不写项目或批准。SVG 生成使用 Node 标准库；PNG 重制需要现有 sharp，缺少时不自动安装，已附 PNG/SVG 可直接查看。正常设计和初始化不依赖该渲染器。

来源（2026-10-28 核对；具体风格 token 属于本 Skill 的适配）：[Meta 组件](https://developers.meta.com/vr/design/components/)、[Meta 交互目标](https://developers.meta.com/vr/design/styles_inputs_hit_targets/)、[PICO Spatial UI](https://github.com/Pico-Developer/PICO-Spatial-SDK-Sample-SpatialUI)、[微软 MRDL](https://learn.microsoft.com/en-us/windows/mixed-reality/mrtk-unity/mrtk3-uxcomponents/packages/uxcomponents/mixed-reality-design-language)、[微软空间排版](https://learn.microsoft.com/en-us/windows/mixed-reality/design/typography)、[Apple 空间 UI](https://developer.apple.com/videos/play/wwdc2023/10076/)。设计方法参考 [Anthropic frontend-design](https://github.com/anthropics/skills/blob/main/skills/frontend-design/SKILL.md) 的业务主题、自评与克制，以及 [UI UX Pro Max](https://github.com/nextlevelbuilder/ui-ux-pro-max-skill) 的设计系统与项目一致性；不引入其 Web 技术栈、审批或字体安装要求。

## 原生装配

转换器 0.4.1 的项目模式自动读取游戏根 `contracts/native-assembly.json`，将契约加入输入指纹与只读计划。当前提供两项通用能力：

1. `rounding.fromCssRadius=true`：对具有相等四边 border 的 Sliced Image，根据源 CSS 的单一正数圆角设置 PPU multiplier；复杂或不对称九宫格仍需显式适配。
2. `slider`：按配置建立独立 Track、FillArea、HandleArea，使用属于 Slider 的项目素材，以及 style 中的颜色 token。字段为 trackHeight、handleSize、trackRadius、trackAssetId、handleAssetId、trackColorToken、fillColorToken、handleColorToken。

装配只操作新生成的原生对象，先于 configureRoot/configureTemplate 执行。不新增游戏视觉 MonoBehaviour，不修改现有字体/材质。配置改动会使已查看的计划过期。

重导入应验证原生结构、状态和实际图像可重复，以及外部 GUID 保留。Unity 重新序列化可能改变 Prefab 内部 fileID 或文件字节；业务 Prefab 的组件引用与事件保护仍需受控接入，不以 GUID 相同推断内部引用安全。

## 标准验收与范围

`unity-acceptance.json` 继续使用原有批准哈希、截图、referenceImages、importReport、sourceImportReportSha256 和 finalArtifacts 契约。可增加：

- `scope: native-ui`：验收对象是原生 UI 交付。
- `gameContractsSha256`：相对游戏根的契约文件哈希。
- `businessIntegration`、`device`：分别记录实际范围，未测保持 not-checked。
- `testedResolutions`、`testedFixtures`：只登记真正验证过的分辨率和数据。

`unityVerification` 是新增的明确状态；旧 unityAccepted 布尔值继续兼容，不能单独用来区分未测与失败。`--require-unity` 仍严格验证全部必需完成证据。设备验收仍由原有 device-acceptance 契约单独约束。

`UiReferenceAudit.Run(prefabPath, browserReferenceJson, outputDirectory)` 在隔离预览场景填充 fixture，比较完整源节点/动态 Item，并检查原生控件；保留实际 Prefab，不写回 fixture。数据填充不会修改静态源节点的布局。原生 Button/Toggle/Slider 检查不执行已有业务事件；业务状态机、游戏服务和设备效果继续单独验收。

可在 Run 的后两个参数传入目标宽高，检查隔离预览中的等比适配与留边；这不替代项目 UIRoot/设备的运行测试。使用 `scripts/record_unity_acceptance.py --help` 查看标准记录入口。实际查看截图后才使用 `--reviewed-native-ui`，工具会重新验证对照 JSON、截图、参考图、来源报告和正式产物指纹；此标志不产生用户设计批准。

浏览器与 TMP 的字体栅格化允许合理差异。矩形比较、字形检查和原生操作检查不能替代图像评审。新增节点路径、row 与可见性比较时，重复身份和非有限坐标必须失败；双方均隐藏的节点不比较无意义的屏幕几何。

## 完整矩阵与证据重放

0.4.1 的新记录使用 [native acceptance v2 Schema](schemas/native-acceptance.schema.json)，保留旧版已批准记录的兼容读取。`matrixCases` 必须覆盖 screens 中每个已实现页面的全部 fixture 与全部分辨率；逐例关联浏览器参考、实际截图、原生审计与重算的几何结果。记录包含 HTML、全部契约、项目清单、实际源图片、正式产物及外部字体依赖的文件/meta 指纹。截图尺寸和当前源输入也必须一致，过期数据不计入 fixture 覆盖率。

星港的完整运行顺序：

```text
node Tools/verify-browser.cjs --capture-matrix
```

在已连接的 Unity Editor 调用 `UiReferenceAudit.RunMatrix(browserMatrixJson, outputDirectory, formalPrefabRoot)`，等待输出 matrix.json 为 passed。实际查看各状态及分辨率图像后运行：

```text
python scripts/record_unity_acceptance.py --root <游戏根> --unity-project <工程> --browser-reference <浏览器矩阵目录> --evidence <Unity矩阵目录> --reviewed-native-ui
python scripts/game_workflow.py status --root <游戏根> --unity-project <工程> --require-unity
```

记录器先验证全部案例和六页待写内容，全部写入后再验证；任何一步失败会恢复此次发布触及的所有文件。`fixturePresentation` 仅声明业务文字长度、说明高度与比率宽度等通用原生适配规则，不从浏览器矩形反向复制静态布局。长名称可声明 TMP 原生自动字号的最小/最大值；实际字体仍需测试。

## 编辑器业务验收

原生控件审计隔离既有回调。业务流程由接入项目另行测试，放在其 Editor 代码内，使用真实交付 Prefab 和原生控件事件；不要把游戏逻辑放入通用转换包。可使用 `editor_acceptance.py` 记录实际测试报告和逐页截图，它独立绑定当前原生验收、测试适配器源码、业务输入与受保护资源。源码或任何依赖改变即需重验。

```text
python scripts/editor_acceptance.py --root <游戏根> --unity-project <工程> --evidence <业务测试目录> --reviewed-editor-ui
python scripts/game_workflow.py status --root <游戏根> --unity-project <工程> --require-unity --require-editor
```

报告旁需保存测试时的适配器原字节为 `editor-adapter.cs.txt`。报告说明实际时钟、状态存储和服务范围；Editor 内存演示通过不代表已连接生产服务。设备测试继续由 device-acceptance 约束；只有用户明确指定仅编辑器范围时，游戏级 design-review/verification-scope.json 才能记录 target=UnityEditor、deviceRequired=false、requestedBy=user，状态报告据此将未测设备记为 not-applicable，设备通过布尔值仍为 false。
