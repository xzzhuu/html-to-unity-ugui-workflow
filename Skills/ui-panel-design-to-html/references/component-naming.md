# Semantic component naming during decomposition

Read this after panel-preview approval, while preparing the required element sheets. Both panel preview and element sheets require user confirmation; naming itself needs no additional approval gate. Asset prefixes below apply to both workflows; existing hierarchy/binding names remain stable.

There is one root UI object, named in English PascalCase ending in `Panel`, such as `InventoryPanel`. Use this exact name for `panelName`, the main prefab, the root hierarchy object and that panel's asset directories. Child regions use `Group`; they are not additional Panels.

## Determine meaning before naming

For each approved element, state what it displays, controls or groups. Determine its parent, native component type and data or action source. Then choose a name that expresses that purpose, using `Group`, `Txt`, `Img`, `RawImg`, `Btn`, `Input`, `Scroll`, `Toggle`, `Slider`, or `Item`, followed by an underscore and an English PascalCase purpose. A component prefix alone does not establish meaning.

Prefer `Btn_RefreshDevices`, `Txt_DeviceCount`, `Input_DeviceSearch`, `RawImg_HeightTrend` to names derived only from color, position, CSS classes or numeric traversal order. Short names such as `Txt_Title`, `Img_Icon` and `Txt_Unit` are acceptable within a parent whose purpose makes the meaning unambiguous; still record that full meaning in the manifest. Avoid generic `Element`, `Object`, `Image1`, `Text2` in final deliverables. Repeated runtime item instances may use a sibling index; numbering must not replace business meaning.

Keep browser IDs/classes, Unity names and binding keys separate. Rename display objects without changing a stable action/data binding. Use bindings only for interactive or runtime data elements; a static decoration does not need a fabricated binding key.

## Record the decision in elements.json

Every source node and converter-generated helper node has an entry. Required fields:

- `elementId`: stable identifier of this element in the package.
- `unityName`: exact root or child object name, not an inferred suggestion.
- `purpose`: concrete meaning in this UI, including the data/action being represented.
- `parentId` and `hierarchyPath`: ownership within the one root Panel. Template entries describe their intended list Content location and record the template scope.
- `uguiComponent`: the main native UGUI/TMP component or container role.
- `bindingKey`: stable binding when needed; otherwise null.
- `template`: logical template key for repeated content, otherwise null.
- `pictureRegion`: approved-layout region used for decomposition; do not invent approved coordinates for a sample lacking approval.
- `generated`: true for helper nodes produced by the converter, otherwise false.

For compound controls, account for their helpers too: button label; input text viewport, placeholder and value; scroll content; toggle checkmark; slider fill, handle area and handle. Their names come from the converter contract, while their recorded purposes describe the specific owning control.

Example entry:

```json
{
  "elementId": "DeviceMonitorPanel/Group_DeviceHeader/Btn_RefreshDevices",
  "unityName": "Btn_RefreshDevices",
  "purpose": "Request refreshed device data and update the device list",
  "parentId": "DeviceMonitorPanel/Group_DeviceHeader",
  "hierarchyPath": "DeviceMonitorPanel/Group_DeviceHeader/Btn_RefreshDevices",
  "uguiComponent": "Button",
  "bindingKey": "Action.RefreshDevices",
  "template": null,
  "pictureRegion": [24, 104, 120, 40],
  "generated": false
}
```

## Carry the names through delivery

Write the manifest's `unityName` into each non-root node's `data-ui-name`, and the root name into `data-panel-name`. Set root folder mappings from that same Panel name. Template root names use `Item_Purpose`; template logical keys remain independent. Use the manifest names from the beginning of HTML production, rather than applying a cosmetic rename after import.

Check manifest coverage, resolved parent ownership, sibling uniqueness and exact manifest-to-HTML name agreement before delivery. During requested Unity verification, check imported hierarchy and helper names against the same manifest, plus binding and component references. Unresolved meaning, missing names, missing nodes or unexplained generic names keep that part of the package incomplete; settle the meaning from requirements or approved context, asking only when necessary.

The shared schema additionally records sourceType, htmlId, dataSource and states; elementId equals hierarchyPath. Consult [the shared manifest contract](manifests.md) for template sizing/spacing and asset/readiness fields.

## 素材与元素组图命名

新制作或导出的素材及元素组图采用 `<组件缩写>_<语义用途>_<部件或结构状态>.png`，省略不必要段落。部件用英文 PascalCase，不用颜色、位置或无意义序号代替用途。规则针对素材，不要求重命名原有层级物体或业务绑定。

| Unity 组件 | 素材前缀 | 示例 |
| --- | --- | --- |
| Button | Btn | Btn_Primary_Background.png |
| Toggle | Tgl | Tgl_Option_Checkmark.png |
| InputField / TMP_InputField | Inp | Inp_Search_Background.png |
| Dropdown / TMP_Dropdown | Dpd | Dpd_Quality_Arrow.png |
| Slider | Sld | Sld_Volume_Handle.png |
| Scrollbar | Sbr | Sbr_List_Handle.png |
| Scroll View / ScrollRect | Scr | Scr_List_Background.png |
| Image / 跨组件共用图形 | Img | Img_RoundedSurface.png |
| RawImage / Texture | RawImg | RawImg_Preview_Pattern.png |
| Text / TMP 的组图 | Txt | Txt_Title_Sheet.png |

此表是映射，不是要求制作全部类型。组图使用相同前缀，例如 `Btn_Primary_Sheet.png`；单个组可包含文字、图标、背景和实际需要的状态。记录里的 componentType 使用左列的规范类型（InputField、Dropdown、ScrollView、Text），不按 TMP 类名重复建组。

专用于按钮的背景用 Btn；多个组件共用同一图形时用 Img，并在使用映射中列出实际拥有者。形状相同、只有颜色差异的 Primary/Danger 等按钮共用一个背景素材，不因为示例语义不同各导出一份。Normal/Pressed 等状态仅在形状、纹理或透明度结构不同时拆分；纯颜色状态用 Image.color / Color Tint。不输出 Btn_Red、Btn_Blue 等仅颜色不同的副本。

保持源文件分辨率与透明细节，尺寸记录在清单中而不是强制写入文件名。已有资产原位改名时通过 Unity AssetDatabase/MCP 保留 meta/GUID；未经授权不批量重命名旧素材。引用旧命名源图时可保留为输入参考，正式新导出文件使用本规则。

所有元素素材贴图宽高使用 2 的幂次方，保留图形细节与原布局。确认后导入 Unity 的参考图沿用组件缩写，例如 Btn_Action_Sheet.png、Img_StartMenuPanel_Preview.png；保留 1920×1080 等原分辨率，不要求 POT。

正式面板预览规则：每个面板单独输出，画布使用纯色背景，不叠加场景、照片、纹理、棋盘格等干扰；在面板主体外标注画布宽×高和主体宽×高。默认画布 1920×1080；素材 POT 像素尺寸不约束 RectTransform 布局尺寸，也不要求预览图改成 POT。确认后将组图和完整面板预览原分辨率导入 Unity 独立参考目录。

VR 素材约束：各元素均使用无边缘描线的填充造型，不制作按钮/底板/内衬等细轮廓或额外 Outline 素材；通过填充色、明暗、透明度与留白表达层级及状态。装饰性细分隔线改用留白。九宫格切片边距和审核尺寸标注线保留；去描边不等于已完成设备抗锯齿验收。
