# 游戏级素材、组件部件与同步规则

本规则优先于本 Skill 中旧的“跨组件共用 Img”示例。新游戏使用 `projectName` 与外部项目根目录的 `project.assets.json`；单面板和全部面板生成前，都先同步当前游戏全部已确认、可交付素材到 `Assets/HtmlToUGUIStage/{GameName}/Sprites/`。RawImage 使用 `Textures/RawImage`。未制作、未确认的未来素材不必提前提供。

## 组件归属与部件

游戏源目录采用 `{GameName}/Panels/{PanelName}/html/`；`project.assets.json`、共享素材及项目级审核/工具保留在 `{GameName}/`。准备包的 `projectDirectory` 和 HTML `data-project-directory` 从 html/ 相对定位，标准值为 `../../..`；项目批量导入选择游戏根目录。初始化器在已有清单的 Panels/ 下自动配置这三项，不覆盖已有工作区。Unity 暂存路径不增加 Panels 层。

复用只允许同一实际组件类型、对应部件与兼容状态之间。Button/Toggle 的子 Image 仍归 Button/Toggle；Dropdown Item 的实际 Toggle 归 Toggle，Scrollbar 归 Scrollbar。独立装饰才归 Image；不得用 Image 类型绕过隔离。旧设计组的 InputField/Dropdown/ScrollView 在游戏素材清单中应对应实际 TMP_InputField/TMP_Dropdown/ScrollRect。

| 类型 | 素材部件 | 原生行为、状态与非图片部分 |
| --- | --- | --- |
| Button | Surface、独立 Icon/装饰 | TMP Label；Normal/Highlighted/Pressed/Selected/Disabled；纯颜色反馈用 Color Tint |
| Toggle | Off/On/Disabled 三张完整 Surface、独立动态 Icon | isOn 与交互状态分离；不规则外形及固定标记合入每张整图，不拼几何碎片、不叠加 Checkmark |
| Slider | Track、Fill、Handle，必要刻度 | fillRect 控制填充，handleRect 控制位置；不导出各百分比截图 |
| Scrollbar | Track、Handle | Handle 位置与长度对应滚动位置/可视比例，无数值填充 |
| Dropdown / TMP_Dropdown | 收起 Surface、Arrow、可选 Caption Image、列表背景 | Item 为 Toggle；其背景、选中标记和 Icon 按 Toggle 制作；滚动条独立；文字保持 TMP |
| InputField / TMP_InputField | Surface、独立功能图标/子按钮 | 文本视口、Placeholder、输入值；光标与选区原生渲染；错误/成功是业务状态 |
| ScrollRect | 可选背景、非矩形 Mask | Viewport、Content、Item 保持节点；不烘焙整个滚动内容 |
| Image / Panel 背景 | 自身图案、独立变化装饰 | 按需 Simple/Sliced/Tiled/Filled；填充进度不导出多张截图 |
| RawImage | Texture 引用 | 动态视频、RenderTexture 由项目提供，不能烘焙替代 |
| Text / TMP | 已有字体及材质，必要 TMP Sprite Asset | 普通文案、数值、本地化不做 PNG |
| Mask / RectMask2D | 仅非矩形 Mask 需要形状图 | RectMask2D 不要素材 |
| 布局/CanvasGroup/容器 | 无必需图片 | 有背景时单独归 Image |

表格是实际需求参考，不要求每项目制作全部类型。当前 HTML 转换器没有 Dropdown/Scrollbar 构造器，会在生成前报错；有对应素材规范不等于可自动转换。

## Toggle 三图

三态默认定义为 Off（可交互且未勾选）、On（可交互且已勾选）、Disabled（有效不可交互，不区分开关值）。禁用不改变业务 isOn，恢复交互后恢复原状态。若禁用时还要显示 On/Off，或需要 Mixed，则超过三图契约，必须报告并由项目适配，不能假称原生支持。

三张图同画布、Border、Pivot、PPU，固定图案同位置。即使像素完全相同也保留三个状态资源；多个同样式 Toggle 共用这一组三图。Selected 是导航焦点，不是 isOn，不能用 selectedSprite 代替 On。

HTML：`data-toggle-off-sprite="asset:MusicOff"`、`data-toggle-on-sprite="asset:MusicOn"`、`data-toggle-disabled-sprite="asset:MusicDisabled"`。配套清单三项为 `componentType=Toggle, part=Surface`，同 styleId，state 分别 Off/On/Disabled。生成器复用已有通用 UiToggleStateGraphic；不生成额外 Checkmark，也不编写面板专属脚本。普通 value change 立即刷新；SetIsOnWithoutNotify、interactable、父 CanvasGroup 的变化在下一帧刷新。

三图模式不能混用普通背景、Checkmark 或 Selectable SpriteSwap；可用 Color Tint 提供纯颜色交互反馈。Selectable 仅处理 targetGraphic，子图标和文字不会自动同步所有状态。

## 清单与命名

`project.assets.json` 使用 `schemaVersion=1, assets=[]`。每项字段：

- assetId、componentType、part、styleId、state：稳定语义身份。ID 以英文字母开头，仅字母、数字、下划线。
- source：相对项目根目录的 PNG，禁止越界或链接。
- approved、approvalVersion、sourceSha256：真实批准及源文件小写 SHA256；approved=false 不导入，哈希变化需新批准。
- tintable：同类型同部件纯颜色变化共用已批准中性 RGB 图。多色图不强制中性化。
- border：L/B/R/T，默认全零；pivot 默认 [0.5,0.5]；pixelsPerUnit 默认 100；sRGB 默认 true；filter 默认 Bilinear，可用 Point。

Unity 命名 `<组件缩写>_<StyleId>_<Part>_<State>_<AssetId>.png`；目录 `Sprites/{实际类型}/{StyleId}`。缩写 Btn/Tgl/Sld/Sbr/Dpd/Inp/Scr/Img/RawImg/Msk。不能用颜色、位置或面板名替代可复用样式身份；已有规范路径和 GUID 保留。

准备包设置 `projectName`；可用相对 projectDirectory 指向清单根目录，省略则向上查找。HTML 图片属性及 sprites.json 的 path 可用 `asset:ID`；浏览器使用普通 src/CSS URL。sprites.json 的 source 保留面板本地浏览器副本，其哈希须与项目批准源一致。批准清单继续记录本面板实际素材、元素组图和预览，不伪造用户批准。

Slider 的 data-sprite 为 Track，data-fill-sprite/data-handle-sprite 对应 Fill/Handle。控件内部独立 Image 设 data-asset-part，填写 Icon 等实际部件。同一源节点的图片当前使用同一九宫格声明；部件切片不同需项目既有 Prefab 适配，不静默修改共享 importer。

元素 PNG 均为 POT，保留源像素细节；不通过强拉伸恢复清晰度。项目导入固定 Single、FullRect、Clamp、无 MipMap、无损压缩。未登记的平台覆盖会阻止同步。面板预览与元素组图保持原布局分辨率，独立导入 DesignReferences，不加入运行素材去重池。

## 同步、规范选择与清理

每次生成前读取完整游戏清单，检查源批准哈希及所有被引用资产。候选必须同游戏、实际类型、部件、状态语义，尺寸、Border/Pivot/PPU、色彩空间、过滤及导入配置一致，并且解码 RGBA 像素一致，才自动合并。视觉相似度只能提示复核。材质、轮廓、Alpha、纹理、阴影与九宫格适用性还需设计审核；不是看到形状近似就合并。

既有注册的规范素材优先保留；否则固定按 assetId 字典序确定并登记，不依赖扫描顺序。源内容变化属于版本更新；组件/部件/状态变更要新 ID；同 ID 不同来源冲突不得最后写入覆盖。

共享身份分化创建新图，保留旧图并报告需要重新生成的消费者。已知唯一新目标的重复图先迁移本游戏受管 Prefab 的序列化引用，再扫描 Assets；无依赖才删。正式资源、场景或其他外部引用仍存在时保留重复图并报告。归属不明确时保留旧图，相关面板重新生成后再清理。退休路径继续记录，不清理任意未使用美术。

单面板：全集只读计划 → 项目增量同步与去重 → 冻结素材快照 → 只生成所选面板及 Item → 检查状态/引用 → 更新 StageScene 与报告。全部面板：全集预检 → 同步一次 → 冻结快照 → 顺序生成全部所选面板 → 汇总成功、失败、跳过。

源、清单、目标或 meta 变化使旧计划失效；执行中快照变化阻止当前面板提交。素材同步和逐面板导入各自恢复受管写入，不声称整个批次原子回滚。面板失败可能保留已提交的共享素材和先前面板，需要查看消费者、警告和结果。

正式复制按项目共享依赖只复制一次并重定向多个面板，检查没有暂存依赖。业务资源适配、真实 Unity 截图与交互检查、VR 设备验证继续执行并分别登记。
