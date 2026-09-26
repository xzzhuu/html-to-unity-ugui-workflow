# HTML to UGUI (TMP) 0.2.0

This Unity package imports one prepared external XHTML Panel, its referenced images, and dynamic item templates as native UGUI/TMP objects and Prefabs. It contains generic Editor conversion tools and reusable structure metadata only. It does not copy HTML/CSS/JavaScript into the Unity project, execute browser preview scripts, or generate per-Panel business C#.

## Requirements and installation

- Unity 2022.3
- `com.unity.ugui` 1.0.0
- `com.unity.textmeshpro` 3.0.7

Install through Unity Package Manager using **Add package from disk** and select `package.json`, or **Add package from tarball** and select the `.tgz` release.

For Git installation, use `https://github.com/xzzhuu/html-to-unity-ugui-workflow.git?path=/Packages/com.nexvr.html-to-ugui` in **Add package from git URL**. Import TMP Essential Resources and configure a font containing the needed glyphs.

## Import a prepared Panel

Choose **Tools → HTML to UGUI → Import Panel...**, select a prepared HTML file outside the Unity project, inspect the import report, and run the import. The optional `panel.package.json` configures a prepared HTML package; it is distinct from the UPM package's `package.json`. Select a TMP font with the needed glyphs or configure the project's default font.

The XHTML root describes one Panel using `data-root="true"` and a semantic PascalCase `data-panel-name` ending in `Panel`. Its areas and controls are children of that one root. Use pixel dimensions and the documented CSS subset. Dynamic repeated content is described with one-root `<template data-prefab="...">` blocks and item-scoped bindings. Browser preview data and JavaScript are for browser review only.

## Unity outputs

- Referenced images: `Assets/Art/UI/Sprite/{PanelName}/`, preserving source subfolders.
- Main Prefab: `Assets/Art/UIPrefab/{PanelName}/{PanelName}.prefab`.
- Dynamic Item Prefabs: `Assets/Art/UIPrefab/{PanelName}/Items/`.
- The imported Panel instance is placed under the active scene Canvas. Scene changes are marked dirty and are not auto-saved.

When the scene already has a Canvas, existing CanvasScaler settings are retained. Reimport reuses the matching connected Panel instance. Save scene changes separately.

## Conversion and runtime boundary

The converter builds native UGUI/TMP components and copies referenced PNG files; it does not render the HTML in a browser. Its CSS support is intentionally limited. Unsupported selectors or visual effects are reported in `import-report.json`; a successful structural import does not prove visual parity. Compare the resulting Unity view against the approved layout and HTML preview.

The package may attach generic `UiBindingMap` and `UiTemplateCatalog` metadata. `data-binding` values identify UI elements; the binding map provides typed lookup, and the catalog references generated Item Prefabs. They do not fetch domain data, perform application actions, instantiate runtime rows, or implement a Panel Presenter. The receiving application owns its data, event handling, and runtime list construction.

## Package contents

`Editor/` contains the generic importer, asset validation, Prefab construction and placement, and visual-audit helpers. `Runtime/Core/` contains generic binding, template-reference, and naming helpers. The package has no TowerCrane or Inventory Presenter sources. Unity projects should keep their business logic in their own application code.

For the reusable design workflow, global Skill installation, directory rules, review gates, and prompt templates, use the separate `ui-panel-design-to-html` Codex Skill. Those design documents and source HTML remain outside this package and outside the Unity project.


## License

Distributed under the [MIT license](LICENSE.md). Unity and other dependencies retain their own licenses.
