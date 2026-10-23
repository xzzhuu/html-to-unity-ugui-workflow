# Unity 与 HTML 对照及转换规范

日期：2026-10-24。目标：仓库 UnityProject，Unity 2022.3.62f3c1。实际验证的是正式目录中的六张主 Prefab，而不是用 HTML 截图代替 Unity。字体按用户要求使用现有 `PICOSansVFESC SDF` 和原共享材质。

## 对照条件与结果

- 浏览器与 Unity 均为 1920×1080，纯色背景 `#89939C`，使用相同 Prepared 数据集；结算使用 Result 数据集。
- 在临时 Unity 场景创建同样的任务、库存、携行和奖励行，填充文字、图标、选中状态、禁用状态和列表长度。动态数据填充不写入通用主 Prefab。
- 审核标题和尺寸注释放在画布外部的参考层，运行时 UI 不包含它们。确认弹窗对照独立预览状态，临时取消遮罩；实际 Prefab 保留弹窗遮罩。
- 六页所有被比较的可见源节点与动态行矩形均在 1.1px 容差内；最终截图没有缺字、文字溢出或绑定超出画布的问题。该结果不代表逐像素一致：浏览器字体、TMP 字形和抗锯齿仍有差别。
- 实测开关原生点击、Off/On/Disabled 图切换；滑块 0/50/80/100 时保持 28×28 手柄，键盘 50→51 的整数步进正常。浏览器 14 项跨页流程另行通过。

| 面板 | 批准图 / HTML / Unity 三列对照 |
| --- | --- |
| 星港基地 | [实际对照图](evidence/current/StarportHomePanel.comparison.png) |
| 远征选择 | [实际对照图](evidence/current/ExpeditionSelectPanel.comparison.png) |
| 仓库整备 | [实际对照图](evidence/current/SupplyInventoryPanel.comparison.png) |
| 游戏设置 | [实际对照图](evidence/current/GameSettingsPanel.comparison.png) |
| 操作确认 | [实际对照图](evidence/current/ActionConfirmPanel.comparison.png) |
| 远征结算 | [实际对照图](evidence/current/ExpeditionResultPanel.comparison.png) |

[机器检查摘要](evidence/current/visual-summary.json) 仅覆盖已运行的检查，不能替代以上逐页图像检查。完整原尺寸截图、节点矩形和运行日志保留在本机忽略目录 `Audit/Runs/20261024-workflow-review/`。

## 已纠正的问题与复用规则

| 问题 | 本次纠正 | 后续转换规范 |
| --- | --- | --- |
| 正式图片根已经是 Sprites，共享复制又保留了游戏下的 Sprites 暂存桶 | 正式路径统一为 `Assets/Art/UI/Sprites/StarportSalvage/{组件类型}/...`；已有 24 张图通过 Unity 移动保留 GUID | 正式复制仅保留游戏名和桶内分类；移动已有资源保留 GUID，以新迁移报告关联历史复制记录，不能重写历史证据 |
| 三态开关整图又乘上浏览器轨道背景色，亮色旋钮变暗 | 转换器的完整 Surface 分支使用白色 Image tint，保留原图配色；新增真实 Editor 回归 | 完整彩色状态图的背景不作为第二次 tint；逻辑 On 和原生 Selected 分开验收 |
| 滑块 40px 整体高度被用于轨道，手柄被拉长 | 使用 10px 轨道、28px 手柄，独立 FillArea 与零高 HandleArea | CSS 伪元素不会自动变成子 Image；逐部件登记尺寸和层级，检查最小、中间、最大值 |
| 滑块填充绘制在手柄上方 | HandleArea 放在最后一个兄弟位置 | 检查遮挡顺序，不能只核对 RectTransform 尺寸 |
| 共享九宫格圆角比 HTML 大，按钮接近胶囊形 | 按已确认素材的角部像素与目标圆角设置 Image.pixelsPerUnitMultiplier | border 是切片元数据；同一素材显示圆角需独立登记 PPU multiplier。不能仅凭 CSS border-radius 或 Rect 尺寸推断 |
| PICOSans 不含数学减号 U+2212，库存减量按钮和结算出现方框 | HTML、预览逻辑和 Unity 统一改为 ASCII `-`，保持数值含义；更新技术修正及验收文件哈希 | 对实际静态与动态文字运行字形检查；缺字不能靠更换截图或无记录的备用字体掩盖 |
| 未填充动态 Item，Unity 空列表与 HTML 有数据列表无法比较 | 同一固定数据集生成临时行，复现图标、文字、颜色、选择、锁定与内容高度 | 先同步数据，再比较视觉；业务逻辑与转换结果分别记录 |
| 锁定任务的橙色状态或整体透明度未同步 | 数据填充阶段同步文字颜色、CanvasGroup.alpha 与禁用状态 | ColorTint 只影响 targetGraphic；整组 opacity 必须显式应用，防止与禁用 tint 重复相乘 |
| 弹窗独立预览与叠加模式背景不同 | 对照时使用同一模式，保留实际遮罩 | 明确比较的是独立画布、模态叠加或运行场景，避免把背景差异误判为布局问题 |

## 本例原生装配参数

设置页和库存页的 Slider 根仍保留原有位置、点击区域与绑定：

| 部件 | 相对 Slider 左上角的设置 |
| --- | --- |
| 根 Image | 透明，保留 Raycast Target |
| Img_TrackBackground | x=0，y=15，宽度=Slider 宽，高度=10；颜色 `#203745`，Sliced，PPU multiplier=4，不接收射线 |
| Group_FillArea / Img_Fill | FillArea 同轨道矩形，Fill 在内部拉伸；颜色 `#4AD7BB`，Sliced，PPU multiplier=4；由原生 Slider.fillRect 控制 |
| Group_HandleArea | x=14，y=20，宽度=Slider 宽-28，高度=0；最后一个兄弟 |
| Img_Handle | 中心 Pivot，28×28，已有圆形 Handle Sprite，颜色 `#EEF5F7`；由原生 Slider.handleRect 控制 |

星港的 Control_Background 素材角部为 20px，RoundedSurface 为 24px。针对**这些已确认的圆角素材**，multiplier 取“素材角部像素 / HTML 目标圆角”；普通输入背景目标 8px 时为 20/8=2.5，普通按钮目标 10px 时为 20/10=2。此比例不适用于任意复杂九宫格：新的异形或纹理素材必须单独确认。

上述原生装配通过现有导入接入钩子在首次保存前完成，主面板和 Item 都处理，转换报告记录最终文件指纹。完整重新导入须重做这些原生设置，再重验、复制；只重新生成默认组件不能视为同一视觉交付。

## 验收记录边界

批准原图和用户批准原文保持不变；本次仅更新实际修改的 HTML/JS 技术修正哈希，并增加本轮 Unity 图像与产物证据。没有把测试数据写成用户批准，没有声明整页像素一致、游戏业务已接通或设备已验证。

后续变更应同时保留：源文件、批准版本、导入/复制只读计划、原生装配参数、相同数据集的双端截图、字形及矩形检查、正式/暂存 GUID 与文件/meta 哈希。任一项过期后重新验收。
