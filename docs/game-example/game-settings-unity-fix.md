# GameSettingsPanel 层级与控件修正

> 当前测试工程已于 2026-10-24 切换为 `E:/html_to_unity_ugui_workflow/UnityProject`；本文保留 2026-10-23 的历史验证范围与原始证据，不能替代新工程的本次检查。星港源包现位于 `Workspaces/StarportSalvage/Panels/{PanelName}/`。

本次修正包括六页 HTML 的功能分组，以及当前 Unity 工程中 GameSettingsPanel 的原生组件装配。没有修改转换器视觉 C#，没有新增字体、材质或视觉脚本。

## 实际资源

Unity 工程：`E:/Unity Project/Work Space/HTML2UGUI`。

Prefab：`Assets/HtmlToUGUIStage/StarportSalvage/GameSettingsPanel/Prefabs/GameSettingsPanel.prefab`。

原 `Assets/HtmlToUGUIStage/GameSettingsPanel` 已通过 AssetDatabase 整目录迁移，Prefab GUID 保持 `6295c90da75e2c24e94dce0ccf1cfae3`。16 个已有绑定及其对象引用保留，未保存或替换当前 StageScene。

## 原因及层级

转换器按 HTML DOM 建层级。旧 HTML 大量节点直接放在根下，因此 Unity 中也是平铺；此次修正生成器的 DOM，而不是按 Unity 名称猜测父子关系。

```text
GameSettingsPanel
├─ Group_PanelBody
│  ├─ Group_Header
│  ├─ Group_SettingsForm
│  │  ├─ Group_CaptainNameRow
│  │  ├─ Group_SoundRow / Toggle_Sound
│  │  ├─ Group_VolumeRow / Slider_MasterVolume
│  │  └─ Group_QualityRow
│  └─ Group_Footer
└─ Group_IconCatalog
```

六页保留原画布坐标，重算相对父节点坐标，更新 elements.json 的实际路径。动态列表模板保持自己的局部坐标；仓库长名称的稀有度文本也改为相对分组定位。六页浏览器基准截图与分组前逐像素一致，14 组交互检查通过。

## 开关原生装配

CSS 的 `::after`、`:checked` 不会生成 Unity 子物体。旧转换结果是铺满内部区域的矩形 Checkmark，缺少滑钮。

当前保持原 Toggle 组件、绑定和事件，使用既有 Tgl_Sound_Track 与 Img_Circle：

- 根 Image 为关闭轨道，颜色 `#36505F`，尺寸 90×42，Sliced，Pixels Per Unit Multiplier 为 1.25。
- `Img_OffKnob` 是 32×32 圆形，局部左上坐标 (5,5)。
- `Group_OnState` 覆盖完整 90×42；原 `Img_Checkmark` 改为青色轨道 `#4AD7BB`，右滑钮 32×32，坐标 (50,5)。
- Toggle.graphic 继续引用 Img_Checkmark，targetGraphic 指向根 Image，Toggle Transition 为 None。
- On Value Changed 增加动态 bool 持久事件，目标为 Group_OnState 的 GameObject.SetActive；保留原有监听。Group_OnState 初始 Active 必须与 Is On 一致。

这些视觉子图像关闭 Raycast Target，点击区域仍由根 Toggle 提供。

## 音量滑块原生装配

旧结果把填充撑满 40px 高度，手柄为无 Sprite 矩形。当前根 Image 透明但保留点击接收，范围 0～100，Whole Numbers，默认 80。

以下坐标是相对 Slider 左上角的 (x,y,width,height)。固定矩形使用左上锚点和 Pivot，Unity Anchored Position 的 y 取负值。

| 节点 | 矩形 / 设置 |
| --- | --- |
| Img_TrackBackground | (0,15,600,10)，Sld_Value_Bar，深色 `#203745` |
| Group_FillArea | (0,15,600,10) |
| Img_Fill | 在 FillArea 内拉伸，Sld_Value_Bar，青色 `#4AD7BB`，由 Slider.fillRect 控制水平比例 |
| Group_HandleArea | (14,20,572,0) |
| Img_Handle | Img_Circle，28×28，中心 Pivot，由 Slider.handleRect 控制位置 |

两个轨道 Image 使用 Sliced，Pixels Per Unit Multiplier 为 4；Slider.targetGraphic 指向手柄。HandleArea 高度设为 0，因为原生 Slider 会将手柄纵向锚点设为拉伸，若保留 40px 区域会把 28px 圆形撑成 68px 椭圆。

0 / 50 / 80 / 100 时手柄左边界分别为画布 x=740 / 1026 / 1197.6 / 1312，尺寸始终 28×28，填充宽度分别为 0 / 300 / 480 / 600，与浏览器一致。

## 验证和边界

- 实际 Unity 1920×1080 截图：[默认 80%](evidence/game-settings-volume-80.png)、[关闭状态](evidence/game-settings-sound-off.png)。
- [原生检查记录](evidence/game-settings-checks.json)：保存后重新加载，开关原生指针点击 on→off→on、滑块键盘 50→51，以及四个数值的真实 RectTransform 尺寸通过。检查在隔离编辑器预览中执行，仅在临时副本上把 RuntimeOnly 回调改为 EditorAndRuntime；磁盘 Prefab 仍为 RuntimeOnly。
- 所有截图没有文字溢出、缺字或超出画布问题。图中的“关闭”和百分比文本由截图准备阶段赋值；业务层的标签更新、设置应用和服务接线仍需游戏接入，不能将截图视为这些业务行为已通过。
- 现有 SourceHanSansCN-Regular SDF 的 boldStyle=0.75 导致伪粗体糊字。该 Prefab 的 12 个粗体文本改用同一字体、同一共享材质的常规字重。浏览器微软雅黑与 Unity 思源黑体、字重及部分圆角仍有差异；未声明整页像素一致。
- 本次不是六页 Unity 完整交付验收，也没有 VR 设备实测；HTML ready 与 Unity/设备验收分开记录。

## 后续重新导入

**六页功能分组已写入 HTML，后续导入会保留；开关/滑块的这次 Unity 原生装配没有写进转换器。完整重新生成 GameSettingsPanel 会覆盖装配，需要按上述步骤重新应用。**

按当前 ui-panel-design-to-html Skill 的边界，CSS 不支持的外观使用原生组件和已确认素材装配，不为视觉效果扩展转换器 C#。此次没有把浏览器伪元素伪装成转换器已支持的能力。

手动检查当前修正版时，直接打开上述 Prefab，展开 Group_SettingsForm。进入 Play 后检查 Toggle 的 Is On、轨道和滑钮，再将 Slider 拖到最小、中间和最大。不要先点击完整重新导入，否则看到的是转换器默认装配。
