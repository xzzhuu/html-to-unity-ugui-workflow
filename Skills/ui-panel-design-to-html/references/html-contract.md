# Ready HTML → UGUI contract v1

当前游戏源目录为 `{GameName}/Panels/{PanelName}/html/`；共享 `project.assets.json` 留在游戏根，准备包 `projectDirectory` 从 html/ 回退三级（`../../..`）。批量导入选择游戏根目录。

This contract applies only to mode=full, after both panel-preview and reusable-element-sheet approval. For visual-only work, skip this document, HTML/browser/converter steps and use Unity MCP directly. Deliver assets and panel prefabs; do not create or modify C# for appearance.

Each HTML document describes exactly one root Panel. Its exact English PascalCase name ends in Panel and is shared by the root object, main prefab and per-panel folders. All areas, controls and item instances are descendants of that root; only its name determines panel output paths. A child ID ending in Panel remains a child container, not another root Panel.

The editor converter reads XHTML and linked/embedded CSS. It does not run a browser or interpret the screenshot. Design-picture approval belongs to the preparation skill, not the converter.

## Package

Keep the HTML preparation workspace outside the Unity project. The importer accepts an absolute external HTML path and resolves CSS and source PNGs relative to that file. It copies referenced PNGs to the configured Unity asset destinations; it does not copy HTML, CSS or preview JavaScript into `Assets`. After confirmation, use Unity MCP to import the element sheets and complete panel preview images separately as reference-only textures; the converter does not perform this step.

Use the standard workspace: html/{PanelName}.html, its CSS/preview JS, html/assets/, html/elements.json, html/sprites.json, and design/review/ approval, browser evidence and html-acceptance.md/json. [The shared manifest contract](manifests.md) defines the schemas and ready gate.

Optional `panel.package.json` holds `schemaVersion:1`, `panelName`, relative `html`, `outputFolder`, `assetPrefix`, optional target-project `fontAsset` (null uses the TMP default font), and `boldWeight`. Explicit package fonts must exist in the selected project; examples use the target project default font. Invalid explicit HTML font paths are rejected unless a valid font is selected in the window.

The HTML itself is sufficient for ordinary conversion:

```html
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head><meta charset="utf-8"/><link rel="stylesheet" href="Panel.css"/></head>
<body>
<div id="MyPanel" data-root="true" data-ugui="Container"
     data-panel-name="MyPanel" data-asset-prefix="Assets/Art/UI/Sprite/MyPanel/"
     data-output-folder="Assets/Art/UIPrefab/MyPanel"
     style="width:1920px;height:1080px;background-color:#101b2a">
  <span id="Title" data-ui-name="Txt_Title" data-ugui="Text" style="left:40px;top:28px;width:600px;height:64px;font-size:32px;color:#ffffff">系统设置</span>
  <button id="Save" data-ui-name="Btn_Save" data-ugui="Button" data-binding="Action.Save"
      style="left:40px;top:600px;width:240px;height:64px;background-color:#007aaf;font-size:24px;color:#ffffff">保存</button>
</div>
<script src="Panel.preview.js"></script>
</body></html>
```

Use exactly one root and positive pixel root dimensions. `data-panel-name` defaults to root ID/file stem; output defaults to `Assets/Art/UIPrefab/<name>`. Asset prefix defaults to `Assets/Art/UI/Sprite/<name>/`. Optional `data-font-asset` supplies a TMP asset path; the chosen window font takes precedence, then this path, then the project's TMP default.

## XHTML and CSS

Close void elements (`img`, `input`, `meta`, `link`). **Always use explicit closing tags for scripts**: `<script src="..."></script>`; `<script .../>` is XML-valid but consumes following content in a normal HTML browser. Escape ampersands as `&amp;`. Give visible nodes stable IDs and explicit `data-ugui`, positions, dimensions, font sizes/colors, and binding keys for runtime fields/actions.

The importer supports single class/ID selectors, comma-separated lists, repeated rule source order, ID-over-class specificity, inline overrides, and basic CSS variables. It does not implement inheritance, general selectors, flex/grid, media queries, percentages, calc, nested rules, or the full CSS cascade. Root background is supported. Background on other nodes requires `Image`; a plain `Container` has no Graphic.

Positions/dimensions are pixels. Missing width with both left/right is resolved within the parent; missing height with top/bottom behaves similarly. Fixed canvas preview uses uniform CanvasScaler scaling and letterboxing across aspect ratios, not responsive reflow. Validate landscape and portrait as separate intended designs when reflow is required.

Native text uses the selected TMP font, not browser font lookup. Bake decoration only; keep text editable. `data-wrap="true"` enables wrapping, `data-overflow="ellipsis"` enables ellipsis. Padding, line height, letter spacing, shadows, radius, and border effects are review items; implement the intended appearance with geometry/assets rather than relying on silently ignored CSS. By default the converter reuses the current TMP default font and its existing material, including an embedded material subasset. It creates no font or material copies. Legacy boldWeight is read for compatibility but does not change shared materials; font weight maps to native TMP FontStyles.

## Components

| data-ugui | Native component / contract |
| --- | --- |
| Container | RectTransform; root may have background Image |
| Text | TextMeshProUGUI; literal text, no nested rich HTML |
| Image | Image; optional data-sprite; decorative raycast disabled |
| RawImage | RawImage; data-texture required |
| Button | Image + Button; direct text produces TMP Label; child Text/Image allowed |
| InputField | TMP_InputField, RectMask2D viewport, editable TMP Text + Placeholder |
| ScrollView | ScrollRect + RectMask2D + transparent raycast Image + Content; vertical scroll |
| Toggle | Toggle + 三态整图 Image + UiToggleStateGraphic；data-is-on true/false；旧技术入口保留 Checkmark |
| Slider | Slider + Fill + Handle; data-min/max/value, data-whole-numbers |

Selectable controls support `data-interactable="false"`. Project Toggle uses data-toggle-off-sprite/data-toggle-on-sprite/data-toggle-disabled-sprite with three full surfaces and no generated Checkmark. Slider supports data-sprite for Track plus data-fill-sprite/data-handle-sprite. These are existing converter capabilities; do not write per-panel appearance scripts. All sprites must use the same border declaration on that source node; differing part borders require existing project Prefab adaptation. Dropdown, horizontal scroll, UI Toolkit, arbitrary JavaScript behaviors, and full animation conversion are not implemented.

`data-binding` must be unique ignoring case within the main panel or each template. `UiBindingMap.Get<T>(key)` retrieves a typed native component. Templates may reuse the same keys as the panel or other templates; they each have a separate binding map.

```html
<div id="Items" data-ugui="ScrollView" data-binding="Items.List"
     style="left:40px;top:120px;width:640px;height:480px;content-height:640px">
  <template data-prefab="InventoryRow">
    <button id="ItemRoot" data-ugui="Button" data-binding="Item.Select"
            style="left:0;top:0;width:640px;height:64px">
      <span id="ItemName" data-ugui="Text" data-binding="Item.Name"
            style="left:24px;top:12px;width:480px;height:40px;font-size:24px">物品名称</span>
    </button>
  </template>
</div>
```

One template root, safe unique `data-prefab` name, no nested templates. Browser preview clones template rows; Unity creates `Items/InventoryRow.prefab` and a root `UiTemplateCatalog`. Caller creates rows under ScrollRect.content, fills their scoped binding maps, positions/reuses rows, and updates content height. The core does not invent data or attach a domain-specific list controller.

## Assets

`src="assets/icons/Img_Save.png"` is the browser source; `data-sprite="Assets/Art/UI/Sprite/MyPanel/icons/Img_Save.png"` is the Unity destination. Both must refer to the same actual file. Prefix mapping strips `Assets/Art/UI/Sprite/MyPanel/` and appends the remainder to the HTML-side `assets/` folder. Paths must stay under Assets and must not contain traversal. External Unity-only assets are allowed by the API when present, but portable preparation packages should include their referenced source PNGs.

Sprite nodes use `data-nine-slice="true" data-border="18,18,18,18"` in **left,bottom,right,top** order. Do not reuse a file with conflicting borders or both Sprite/Texture roles. Browser stretch and Unity nine-slice may differ: simulate the same slicing in CSS or inspect at every designed size. Stateful Sprite files must be referenced explicitly so they get copied. Runtime state application still belongs to the control or Presenter.

New runtime PNG widths and heights must each be powers of two; preserve source detail via vector re-authoring without stretching bitmap art. Converter 0.3.0 rejects padded contentRect; re-author a full-texture POT asset or adapt padding manually outside this conversion route. Reference sheets/panel previews retain their original layout resolution and are exempt. Keep original source pixel dimensions and alpha detail; use RectTransform for displayed size rather than downsampling. Check Max Size, compression and platform overrides for unintended quality loss. Asset names use component abbreviation prefixes from [component-naming.md](component-naming.md#素材与元素组图命名); color-only variants share neutral art tinted with Image.color / native Color Tint. Asset copies compare bytes, not just length/timestamps. Generated Sprite imports use single-sprite mode, no mipmaps, alpha transparency, clamp, and uncompressed textures.

The import window and prepared/package entry points also place the saved prefab under the active scene Canvas. Converter default main prefab: `Assets/Art/UIPrefab/<name>/<name>.prefab`; item prefabs: `Assets/Art/UIPrefab/<name>/Items/`. Repeated import reuses the matching connected instance. Save the scene separately. The lower-level `Import` API builds assets only.


## Unity hierarchy names

Only the root uses PanelName. Set `data-ui-name="Btn_Save"` on non-root nodes to author Unity names separately from browser id/class and binding keys. Allowed format: role prefix (`Group|Txt|Img|RawImg|Btn|Input|Scroll|Toggle|Slider|Item`), underscore, English PascalCase purpose, optional two-or-more-digit collision suffix. Explicit names must be unique among siblings; invalid/duplicate names fail before asset mutation. Use Group for sections and Item for template roots. A grouping Image may use Group because its role is an area background.

Without metadata, names derive from binding, then ID, then first class, using component prefix and PascalCase; only same-parent collisions receive _02 etc. Automatic helper nodes use the same convention. Templates retain their logical catalog key; their prefab filenames and object roots use Item_Purpose. Migrate existing assets with AssetDatabase to preserve GUID references. Runtime instances receive numbered names without (Clone). Names are for hierarchy clarity; logic continues to use stable binding keys.

Semantic names are decided during component decomposition and recorded with purposes and parent ownership in elements.json before final HTML. Follow [component-naming.md](component-naming.md); converter fallback naming is only compatibility for older input, not the Skill naming workflow.

State art uses data-normal-sprite and data-highlighted/pressed/selected/disabled-sprite. Normal assigns the target Image sprite; the others map to the corresponding native Selectable SpriteState. Selected means navigation selection, not Toggle.isOn or application business selection. All use the same border and must appear in sprites.json. Color Tint instead uses data-normal/highlighted/pressed/selected/disabled-color (#RRGGBB or #RRGGBBAA). These are native color multipliers applied to the base Image.color, not absolute replacement colors. SpriteSwap and ColorTint attributes cannot be mixed. Base tint comes from explicit CSS background-color/background; a Sprite without an explicit color defaults to white. Dynamic browser clones retain data-preview-template so audit scope remains correct for zero/one/many items. These attributes do not implement Unity business behavior.

## Acceptance

Record each check as pass, fail, or pending with concrete evidence. Do not invent completion percentages or visual scores without a measured basis.

### User-approved layout

Review the picture against requirement IDs: visible fields, controls, charts, labels/units, repeated-data representation, and specified empty/loading/error states. Check full-canvas balance, hierarchy, legibility, spacing, contrast, state differentiation, and clipping. Record explicit user approval and the exact approved-picture SHA-256. A machine checklist cannot replace this review.

### Element-sheet approval

Before final export or HTML implementation, obtain explicit approval for independently editable sheets of only the actually required components. Record group definitions, source resolution and approved sheet/material hashes as specified in [manifests.md](manifests.md). Color demonstrations do not require separate colored PNGs.

### HTML against the approved picture

Every visible approved element and generated helper must have an `elements.json` entry with a concrete purpose, exact Unity name, parent, and native component. The one root name ends in Panel; every source node's `data-ui-name` matches its manifest entry, and generated helpers follow the converter contract. Missing semantic names, ambiguous generic naming, duplicate siblings, or mismatched ownership fail component decomposition. Every approved source element must also have a counterpart in rendered HTML. Trace each binding and item-template requirement. Check missing/extra elements separately from real-vs-placeholder data changes. Verify actual PNG files, alpha edges, dimensions, repeated backgrounds, state variants, border slicing, icon shapes, font appearance, and chart presentation.

At the approved resolution, capture a browser PNG after fonts, images, scripts, and preview data settle. Exercise specified actions, selection, search, input, enabled/disabled and normal/alarm states; test empty/one/many data and long names where relevant. Inspect the whole canvas and detailed crops beside the approved picture. A usable HTML page with the wrong typography, white-background charts in an approved dark chart style, or missing dynamic rows fails visual acceptance.

`compare_visual.py` generates comparison images and raw pixel metrics. Treat them as diagnostics: antialiasing or data changes may create differences when layout matches. Document each material deviation and its resolution. If a required element or visual region remains unmatched, keep readiness incomplete. Do not let an unrequested percentage or SSIM threshold override visible discrepancies.

### Unity import verification

This check is separate from HTML acceptance. Verify zero compiler errors, correct root dimensions, native component types, TMP glyph coverage/overflow, input viewport/mask, raycast behavior, ScrollRect content extent, template Prefabs, typed/scoped bindings, and repeat-import behavior. Exercise events and representative business-independent presenter wiring. Inspect visual evidence at design size and at any required aspect ratios; fixed Canvas should fit without stretching.

`UiVisualAudit.Capture(prefab, output, populate)` uses a disposable preview scene. Populate templates with the same preview data before comparison. `compare-layout.py` compares exported main binding rectangles within one design pixel; it does not cover all unbound decoration or prove image parity. Review screenshots separately. `HtmlUiRegressionChecks.Run()` checks importer behavior and invalid-input rejection; it is not a visual acceptance test.

`Capture` also accepts an optional fourth background color. Use the approved solid canvas color on both sides and identify whether a modal is shown independently or over another screen. Review annotations belong outside runtime UI. Match the data, selected/locked states, icon tints and entire-group opacity before comparing screenshots. Native ColorTint only affects targetGraphic; it does not reproduce CSS opacity on every child.

Complete Toggle state surfaces already contain their track and knob colors: keep their Image tint white rather than multiplying the browser fallback background again. Check native pointer On/Off and Disabled surfaces. For Sliders, explicitly assemble Track, FillArea and HandleArea from the approved parts; a zero-height centered HandleArea prevents vertical stretch, and the handle must render above the fill. Test the actual handle rectangle at minimum, middle and maximum values and keyboard increments.

Nine-slice border metadata does not specify the final visible corner radius. Record the Image pixelsPerUnitMultiplier required by the approved source and target appearance, including Item prefabs. Never infer the corner of arbitrary textured art from CSS alone. Native assembly made through configureRoot/configureTemplate must be reapplied before saving a regenerated delivery and rechecked after formal copying.

Check glyphs in both static text and representative dynamic data with the actual requested TMP font. Record any semantically equivalent character normalization or explicit existing fallback policy; do not silently substitute a font. Keep approval originals while updating the actual modified implementation hashes and the new validation evidence.

Record which dimensions, states, and devices were actually exercised. Leave hardware pointer input, build-platform behavior, unsupported controls, or unreviewed visual states pending when they were not tested.

Persist `html-acceptance.json` with `approvedLayoutSha256`, `approvedElementGroupsSha256`, checks, and `filesSha256` for every file under `html/` plus `design/element-groups.json`, plus the human-readable report. Run the shared readiness validator after setting `ready` only when actual review has passed. An unchanged hash proves evidence consistency, not aesthetic quality.

Follow the target project's UIKit/UIRoot and formal prefab directory conventions. Preserve original GUIDs, bindings and events when replacing an existing panel; do not create an independent ordinary Canvas around the project UI lifecycle. Record actual Unity and VR-device checks separately.

Formal promotion defaults to `Assets/Art/UI/Sprites`. Project shared Sprite output is `{formalSpriteRoot}/{GameName}/{relativePathWithinStagingSprites}`: strip the staging `Sprites/` bucket so the game directory does not contain a repeated `Sprites/` level. RawImage keeps its `Textures/` category. The prepared HTML asset prefix is a source mapping alias; the reviewed copy plan determines the formal destination. Move existing assets through Unity to preserve GUIDs, record current paths and a migration report, and retain original copy reports as historical evidence.

Game imports use projectName and the approved project.assets.json inventory. References may use asset:AssetId; resolve them through the game registry before generation. Use the [project asset contract](project-assets.md) for type ownership, parts, states and synchronization.
