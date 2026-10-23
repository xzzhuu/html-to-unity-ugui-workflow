# 游戏级 UGUI 素材库与面板生成契约

当前游戏源目录为 `{GameName}/Panels/{PanelName}/html/`；共享 `project.assets.json` 留在游戏根，准备包 `projectDirectory` 从 html/ 回退三级（`../../..`）。批量导入选择游戏根目录。

本契约用于 `HtmlToUGUIStage` 的项目模式。生成单面板和生成全部面板使用同一个项目清单、同步器和去重规则。未指定 `projectName` 的旧技术示例保留原面板目录布局；它们不是新游戏的素材管理入口。

## 导入顺序与目录

必须先同步当前游戏所有已确认、可交付的元素素材，再生成任何面板。未确认、尚未制作的未来素材不属于可交付全集；同步器不生成或伪造设计批准。

外部项目根目录维护 `project.assets.json`。每次执行均读取完整清单，但只复制内容发生变化的文件；单面板生成也同步未选面板所需的已确认素材。批量生成前同步一次并冻结版本，随后各面板只读取共享资源。

```text
外部项目/
  project.assets.json
  Shared/                 # 已确认的源 PNG，可按类型继续分组
  Inventory/html/panel.package.json
  Settings/html/panel.package.json

Assets/HtmlToUGUIStage/{GameName}/
  Sprites/{ComponentType}/{StyleId}/
  Textures/RawImage/{StyleId}/
  SupplyInventoryPanel/Prefabs/
  GameSettingsPanel/Prefabs/
  Reports/asset-registry.json
  Reports/project-import-*.json
```

完整面板预览、元素组图进入独立 `DesignReferences` 目录，使用 Texture/Default，不参与运行时去重。字体复用项目现有 TMP 字体与材质。动态 RenderTexture、视频等仍由项目配置；当前 HTML 转换器的文件纹理输入限 PNG，不将动态纹理烘焙成 Sprite。

## 外部清单

```json
{
  "schemaVersion": 1,
  "assets": [
    {
      "assetId": "PrimarySurface",
      "componentType": "Button",
      "part": "Surface",
      "styleId": "Primary",
      "state": "Normal",
      "source": "Shared/Button/Btn_Primary_Surface_Normal.png",
      "sourceSha256": "填写已确认源文件的64位小写SHA256",
      "approved": true,
      "approvalVersion": "填写真实批准记录的版本标识",
      "tintable": true,
      "border": [16, 16, 16, 16],
      "pivot": [0.5, 0.5],
      "pixelsPerUnit": 100,
      "sRGB": true,
      "filter": "Bilinear"
    }
  ]
}
```

示例中的哈希与批准标识是说明占位符，不可直接作为批准数据使用。SHA256 可用 PowerShell `Get-FileHash -Algorithm SHA256` 获取并转为小写。`approved=false` 的记录不会导入；引用未批准记录会导致生成失败。

- `assetId` 是稳定身份，大小写不敏感地检查唯一性。`assetId/styleId/part/state` 使用以英文字母开头的字母、数字、下划线标识。
- `componentType` 使用实际组件类型：Button、Toggle、Slider、Scrollbar、Dropdown、TMP_Dropdown、InputField、TMP_InputField、ScrollRect、Image、RawImage、Mask。类型清单描述素材，不代表转换器已经能构造全部组件。
- `source` 相对外部项目根目录，不能越界或穿过链接；必须为真实 PNG。运行时元素宽高均为 POT，单轴不超过 16384。
- `sourceSha256` 绑定批准的文件版本。源文件变更后必须取得对应批准并更新清单，不能靠修改时间选择版本。
- `border` 顺序是 L、B、R、T；必须保留中心区域。`pivot` 是 0～1 的归一化坐标。
- 固定导入配置为 Single Sprite、FullRect、Clamp、无 MipMap、无损压缩；RawImage 使用 Default Texture。`filter` 支持 Point/Bilinear。平台覆盖未登记于此契约，因此检测到覆盖时阻止同步，避免静默降质。
- `tintable=true` 要求可见像素使用中性 RGB；颜色差异写在实例的 `Image.color` 或 Color Tint 中。真实多色图不声明为可染色图。

Unity 输出命名为 `<缩写>_<StyleId>_<Part>_<State>_<AssetId>.png`，如 `Btn_Primary_Surface_Normal_PrimarySurface.png`。加入 AssetId 保证稳定且避免同名覆盖；已有规范路径优先保留。组件前缀为 Btn/Tgl/Sld/Sbr/Dpd/Inp/Scr/Img/RawImg/Msk。颜色不作为样式命名依据。

单面板 `panel.package.json` 设置：

```json
{
  "schemaVersion": 1,
  "panelName": "SupplyInventoryPanel",
  "projectName": "StarportSalvage",
  "projectDirectory": "../../..",
  "html": "SupplyInventoryPanel.html",
  "assetPrefix": "Assets/Art/UI/Sprite/SupplyInventoryPanel/",
  "requireSkillApproval": true,
  "stagingRoot": "Assets/HtmlToUGUIStage/StarportSalvage/SupplyInventoryPanel"
}
```

`projectDirectory` 相对准备包目录；省略时向上寻找 `project.assets.json`。项目批量窗口所选源根目录和游戏名决定有效项目配置。项目模式的输出由游戏名和面板名决定，不允许显式旧 stagingRoot 把共享资源重新分散。

示例保留已批准源包的旧 `assetPrefix`，该字段用于源引用兼容，不代表正式复制目录。当前项目共享图片暂存在游戏级 `Sprites/`、`Textures/`；正式复制默认根为 `Assets/Art/UI/Sprites`，星港输出到 `Assets/Art/UI/Sprites/StarportSalvage/{组件类型}/...`，不重复添加 `Sprites/` 层。

HTML 可直接引用稳定 ID：

```html
<button data-ugui="Button" data-ui-name="Btn_Confirm"
        data-sprite="asset:PrimarySurface"
        data-nine-slice="true" data-border="16,16,16,16"
        style="width:240px;height:64px;background-color:#38B8FF">确认</button>
```

也支持旧 `assetPrefix` 路径：按 HTML 旁 `assets/` 的真实源文件匹配清单；一个源路径登记了多个身份时必须改用 `asset:ID` 消除歧义。浏览器仍使用正常 `src`/CSS URL，不会解释 `asset:`。

Skill 工作区中的 `sprites.json` 可将 `path` 写为 `asset:ID`，`source` 仍指向本面板供浏览器与设计审核使用的本地副本；副本哈希必须等于项目批准源。元素组图与 HTML 批准的原有门禁继续有效。

## 实际部件与状态

| 组件 | 制作的视觉部件 | 保留的原生结构与状态 |
| --- | --- | --- |
| Button | Surface、独立 Icon、必要装饰 | TMP Label；Normal/Highlighted/Pressed/Selected/Disabled。只变色时用 Color Tint |
| Toggle | 每样式 Off/On/Disabled 三张完整 Surface；动态 Icon 可独立 | Toggle.isOn 与交互状态分离；不规则外形和固定标记合入状态整图，禁止几何碎片拼接 |
| Slider | Track、Fill、Handle；必要刻度 | fillRect 与 handleRect 分开；值驱动填充/位置，不导出各百分比截图 |
| Scrollbar | Track、Handle | Handle 的位置及可视比例长度；没有 Slider 的数值填充职责 |
| Dropdown / TMP_Dropdown | Surface、Arrow、可选 Caption Image、列表底图 | Template 的 Item 实际是 Toggle，其视觉归 Toggle；列表 Scrollbar 归 Scrollbar；文字保持文本 |
| InputField / TMP_InputField | Surface、独立功能 Icon/子按钮 | 文本视口、Placeholder、输入文本；光标与选区使用组件渲染。错误/成功是业务状态 |
| ScrollRect | 可选 Surface、必要非矩形遮罩 | Viewport、Content、布局与 Item 不烘焙为图片；矩形裁剪用 RectMask2D |
| Image / 面板底图 | 自身完整图案、独立变化的子装饰 | Simple/Sliced/Tiled/Filled 按需求配置；进度用 Filled 或原生结构 |
| RawImage | Texture 引用 | 动态纹理由项目提供，不参与 Sprite 转换 |
| Text / TMP | 已有字体及材质，按需 TMP Sprite Asset | 文案、本地化、数值不烘焙为 PNG |
| Mask / RectMask2D | 非矩形 Mask 形状图；RectMask2D 无图 | 遮罩与显示背景区分 |
| 容器、布局组、CanvasGroup | 无必需素材 | 仅承担布局、交互与分组；有背景才单独登记 Image |

Button 子 Image 的素材仍归 Button，不能把背景改登记为 Image 来跨类型复用。控件内部子 Image 要设置 `data-asset-part="Icon"` 等实际部件。独立装饰 Image 才属于 Image。根 Container 的背景归 Image。

转换器当前明确支持 Container/Text/Image/RawImage/Button/InputField（生成 TMP_InputField）/ScrollView/Toggle/Slider；Dropdown、Scrollbar 等暂不提供 HTML 构造器，校验会报错。不得把未知类型静默降级成容器。

Slider 的 `data-sprite` 对应 Track；`data-fill-sprite`、`data-handle-sprite` 分别对应 Fill、Handle。旧技术 Toggle 可用 `data-checkmark-sprite`；新项目 Toggle 使用下述三张 Surface。部件必须与清单匹配。当前同一源节点的素材使用相同九宫格声明；部件需要不同切片配置时应通过项目既有原生 Prefab 适配，不可静默修改共享导入设置。

## Toggle 三图映射

```html
<input data-ugui="Toggle" data-ui-name="Toggle_Music" data-binding="Music"
       data-toggle-off-sprite="asset:MusicOff"
       data-toggle-on-sprite="asset:MusicOn"
       data-toggle-disabled-sprite="asset:MusicDisabled"
       data-is-on="false" style="width:256px;height:64px" />
```

清单三项均为 `componentType=Toggle, part=Surface`，同一 `styleId`，state 分别为 Off/On/Disabled。三张图必须相同画布、Border、Pivot、PPU，固定图案在同一位置。即使三张图像素完全相同，也保留三份状态资源；只合并不同实例的对应状态。

生成 Toggle + Image + 通用 `UiToggleStateGraphic`，不生成额外 Checkmark。优先级为 `!IsInteractable()` → Disabled，否则 isOn → On，否则 Off。禁用不改变 isOn；恢复可交互时恢复原开关视觉。初始化和普通 onValueChanged 立即刷新；SetIsOnWithoutNotify、interactable、父 CanvasGroup 的变化在下一帧刷新，也可显式调用 Refresh。

Selectable.Selected 是导航焦点，不能用 selectedSprite 代替 On。三图模式禁止混用普通背景、Checkmark 和 Selectable SpriteSwap；纯颜色交互反馈可用 Color Tint。原生转换只作用 targetGraphic，不会自动为所有子图标/文字换色。

这里的 Disabled 不显示原 On/Off 的区别。如果需要 DisabledOn/DisabledOff、Mixed，或独立绘制每种悬停/按下组合，三图契约不足，当前生成器会要求项目另行适配，不能把它们伪装成已支持。

## 复用、版本变化与去重

自动候选键为：游戏 + **实际组件类型** + **部件** + **状态语义** + 导入配置签名 + 像素尺寸 + 解码后 RGBA 哈希。文件 SHA256 绑定版本，像素哈希允许同像素不同 PNG 编码去重；不使用感知相似度直接合并。

还需在设计时核对外形、透明结构、孔洞、圆角、阴影、纹理、光照、有效区域对齐、九宫格拉伸能力及所用材质。不同尺寸、Pivot、PPU、切片、过滤、色彩空间或导入配置不合并。显示尺寸可以不同，但必须确认等比/九宫格缩放满足设计。颜色变体在制作时共用已确认中性素材，不自动重绘已批准的多色图。

规范素材选择：已有注册的规范资产优先，保留路径和 GUID；否则按 assetId 字典序确定并登记。未来执行读取登记，不因扫描顺序改选。相同 assetId 的新批准内容属于版本更新；多个不同内容用同一 assetId、同名异内容、组件/部件/状态身份变化均报错，后者必须分配新 ID。

共享样式分化时创建独立资源，不覆盖仍被其他样式使用的旧共享图。报告明确列出需要重新生成的身份和旧消费者。能够确定唯一新目标的旧重复资源，先通过 SerializedObject 迁移本游戏受管 Prefab 引用，再检查整个 Assets 的依赖；无引用才删除。场景、正式资源、其他游戏等外部消费者继续引用时保留旧图并报告。身份分化导致旧引用归属不明确时保留旧图，重新生成相关面板后再清理。退休路径持续记入注册表，不因执行一次就丢失。

这与“清理所有未使用资源”不同：清单仍需要但当前未使用的资源保留；不扫描删除任意项目美术资产，不移动旧正式资源或批量改 GUID 文本。

## 两种入口的执行流程

| 阶段 | 单面板 | 全部面板 |
| --- | --- | --- |
| 只读计划 | 全游戏已批准清单 + 所选面板依赖 | 同一全集 + 全部所选面板依赖 |
| 预检 | 检查源哈希、批准、类型、部件、三态和目标指纹 | 增加重复面板名、输出冲突检查 |
| 素材同步 | 全项目增量导入，统一去重及引用迁移 | 开始前统一执行一次 |
| 冻结 | 固定源、素材文件与 meta、注册表指纹 | 所有面板共用冻结快照 |
| 生成 | 只生成所选面板与其 Item | 固定顺序逐面板生成 |
| 验证与报告 | 保存依赖、GUID、文件及 meta 哈希，再更新预览 | 记录 imported/failed/skipped；失败停止后续面板 |

计划查看后源文件、注册表、目标文件或 meta 变化，计划过期，执行前重新预检；生成中快照变化导致当前面板失败。编辑器同步器拒绝同游戏重入，导入器拒绝嵌套面板导入。

素材同步自身使用事务，恢复其写入文件、meta 和受管引用；面板生成继续使用逐面板事务。二者不是整个项目的原子回滚：面板失败时已提交的素材同步和早先成功面板保留。报告需结合 assetWarnings、注册表消费者及各面板状态判断影响；不承诺编辑器崩溃或断电恢复。

面板暂存与复制 正式复制允许同游戏共享 Sprites/Textures 依赖，同一源资源在一批中只复制一次，多面板重定向到同一正式图片。跨游戏或跨面板 Prefab 依赖仍受限制；复制后检查正式 Prefab 不再依赖暂存目录。正式目录已存在的图片复用/覆盖策略见 [正式复制](stage-promotion.md)。

## 验收

- 单面板同步整个游戏素材，但不生成未选面板。
- 重复同步无额外 PNG、无 GUID/导入设置漂移。
- 同类型对应部件可跨面板复用；不同类型即使像素相同也隔离。
- Toggle 三张整图及状态行为有效，没有多余 Checkmark。
- 共享样式分化不破坏其他面板；合并时先迁移引用、无依赖再删除。
- 源或目标变化使旧计划失效，失败恢复受管文件和 meta。
- 单面板与全部面板使用相同规范资源；正式复制后没有暂存依赖。
- Unity 视觉与交互验收、目标 VR 设备实测分别登记，不以结构测试替代设备验收。
