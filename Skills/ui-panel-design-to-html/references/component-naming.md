# Semantic component naming during decomposition

Read this after the user approves the layout, before producing final HTML. The approval checkpoint concerns layout completeness and visuals; do not add another naming approval gate unless the user asks for one.

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
