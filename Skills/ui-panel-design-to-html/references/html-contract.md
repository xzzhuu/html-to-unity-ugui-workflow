# Ready HTML → UGUI contract v1

Each HTML document describes exactly one root Panel. Its exact English PascalCase name ends in Panel and is shared by the root object, main prefab and per-panel folders. All areas, controls and item instances are descendants of that root; only its name determines panel output paths. A child ID ending in Panel remains a child container, not another root Panel.

The editor converter reads XHTML and linked/embedded CSS. It does not run a browser or interpret the screenshot. Design-picture approval belongs to the preparation skill, not the converter.

## Package

Keep the HTML preparation workspace outside the Unity project. The importer accepts an absolute external HTML path and resolves CSS and source PNGs relative to that file. It copies referenced PNGs to the configured Unity asset destinations; it does not copy HTML, CSS, preview JavaScript, or design-review files into `Assets`.

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
     style="width:1280px;height:720px;background-color:#101b2a">
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

Native text uses the selected TMP font, not browser font lookup. Bake decoration only; keep text editable. `data-wrap="true"` enables wrapping, `data-overflow="ellipsis"` enables ellipsis. Padding, line height, letter spacing, shadows, radius, and border effects are review items; implement the intended appearance with geometry/assets rather than relying on silently ignored CSS. Default generated font material normalizes synthetic bold weight to `0.12`; package options can change it or the API can supply a material, without changing the project's source font material.

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
| Toggle | Toggle + background Image + Checkmark; data-is-on true/false |
| Slider | Slider + Fill + Handle; data-min/max/value, data-whole-numbers |

Selectable controls support `data-interactable="false"`. Initial Toggle/Slider visuals use the converter's simple generated geometry; if an approved design requires custom checkbox/track art, extend the component builder and verify it before claiming exact parity. Dropdown, horizontal scroll, UI Toolkit, arbitrary JavaScript behaviors, and full animation conversion are not implemented.

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

`src="assets/icons/save.png"` is the browser source; `data-sprite="Assets/Art/UI/Sprite/MyPanel/icons/save.png"` is the Unity destination. Both must refer to the same actual file. Prefix mapping strips `Assets/Art/UI/Sprite/MyPanel/` and appends the remainder to the HTML-side `assets/` folder. Paths must stay under Assets and must not contain traversal. External Unity-only assets are allowed by the API when present, but portable preparation packages should include their referenced source PNGs.

Sprite nodes use `data-nine-slice="true" data-border="18,18,18,18"` in **left,bottom,right,top** order. Do not reuse a file with conflicting borders or both Sprite/Texture roles. Browser stretch and Unity nine-slice may differ: simulate the same slicing in CSS or inspect at every designed size. Stateful Sprite files must be referenced explicitly so they get copied. Runtime state application still belongs to the control or Presenter.

Asset copies compare bytes, not just length/timestamps. Generated Sprite imports use single-sprite mode, no mipmaps, alpha transparency, clamp, and uncompressed textures.

The import window and prepared/package entry points also place the saved prefab under the active scene Canvas. Main prefab: `Assets/Art/UIPrefab/<name>/<name>.prefab`; item prefabs: `Assets/Art/UIPrefab/<name>/Items/`. Repeated import reuses the matching connected instance. Save the scene separately. The lower-level `Import` API builds assets only.


## Unity hierarchy names

Only the root uses PanelName. Set `data-ui-name="Btn_Save"` on non-root nodes to author Unity names separately from browser id/class and binding keys. Allowed format: role prefix (`Group|Txt|Img|RawImg|Btn|Input|Scroll|Toggle|Slider|Item`), underscore, English PascalCase purpose, optional two-or-more-digit collision suffix. Explicit names must be unique among siblings; invalid/duplicate names fail before asset mutation. Use Group for sections and Item for template roots. A grouping Image may use Group because its role is an area background.

Without metadata, names derive from binding, then ID, then first class, using component prefix and PascalCase; only same-parent collisions receive _02 etc. Automatic helper nodes use the same convention. Templates retain their logical catalog key; their prefab filenames and object roots use Item_Purpose. Migrate existing assets with AssetDatabase to preserve GUID references. Runtime instances receive numbered names without (Clone). Names are for hierarchy clarity; logic continues to use stable binding keys.

Semantic names are decided during component decomposition and recorded with purposes and parent ownership in elements.json before final HTML. Follow [component-naming.md](component-naming.md); converter fallback naming is only compatibility for older input, not the Skill naming workflow.

State art can use data-normal-sprite/data-selected-sprite; they use the same border as the source graphic and must appear in sprites.json. Dynamic browser clones retain data-preview-template so audit scope remains correct for zero/one/many items. These attributes do not implement Unity business behavior.

## Acceptance

Record each check as pass, fail, or pending with concrete evidence. Do not invent completion percentages or visual scores without a measured basis.

### User-approved layout

Review the picture against requirement IDs: visible fields, controls, charts, labels/units, repeated-data representation, and specified empty/loading/error states. Check full-canvas balance, hierarchy, legibility, spacing, contrast, state differentiation, and clipping. Record explicit user approval and the exact approved-picture SHA-256. A machine checklist cannot replace this review.

### HTML against the approved picture

Every visible approved element and generated helper must have an `elements.json` entry with a concrete purpose, exact Unity name, parent, and native component. The one root name ends in Panel; every source node's `data-ui-name` matches its manifest entry, and generated helpers follow the converter contract. Missing semantic names, ambiguous generic naming, duplicate siblings, or mismatched ownership fail component decomposition. Every approved source element must also have a counterpart in rendered HTML. Trace each binding and item-template requirement. Check missing/extra elements separately from real-vs-placeholder data changes. Verify actual PNG files, alpha edges, dimensions, repeated backgrounds, state variants, border slicing, icon shapes, font appearance, and chart presentation.

At the approved resolution, capture a browser PNG after fonts, images, scripts, and preview data settle. Exercise specified actions, selection, search, input, enabled/disabled and normal/alarm states; test empty/one/many data and long names where relevant. Inspect the whole canvas and detailed crops beside the approved picture. A usable HTML page with the wrong typography, white-background charts in an approved dark chart style, or missing dynamic rows fails visual acceptance.

`compare_visual.py` generates comparison images and raw pixel metrics. Treat them as diagnostics: antialiasing or data changes may create differences when layout matches. Document each material deviation and its resolution. If a required element or visual region remains unmatched, keep readiness incomplete. Do not let an unrequested percentage or SSIM threshold override visible discrepancies.

### Unity import verification

This check is separate from HTML acceptance. Verify zero compiler errors, correct root dimensions, native component types, TMP glyph coverage/overflow, input viewport/mask, raycast behavior, ScrollRect content extent, template Prefabs, typed/scoped bindings, and repeat-import behavior. Exercise events and representative business-independent presenter wiring. Inspect visual evidence at design size and at any required aspect ratios; fixed Canvas should fit without stretching.

`UiVisualAudit.Capture(prefab, output, populate)` uses a disposable preview scene. Populate templates with the same preview data before comparison. `compare-layout.py` compares exported main binding rectangles within one design pixel; it does not cover all unbound decoration or prove image parity. Review screenshots separately. `HtmlUiRegressionChecks.Run()` checks importer behavior and invalid-input rejection; it is not a visual acceptance test.

Record which dimensions, states, and devices were actually exercised. Leave hardware pointer input, build-platform behavior, unsupported controls, or unreviewed visual states pending when they were not tested.

Persist `html-acceptance.json` with `approvedLayoutSha256`, checks, and `filesSha256` for every file under `html/`, plus the human-readable report. Run the shared readiness validator after setting `ready` only when actual review has passed. An unchanged hash proves evidence consistency, not aesthetic quality.
