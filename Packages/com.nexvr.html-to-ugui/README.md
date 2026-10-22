# HTML to UGUI (TMP) 0.3.0

This Unity package imports one prepared external XHTML Panel, its referenced images, and dynamic item templates as native UGUI/TMP objects and Prefabs. It contains generic Editor conversion tools and reusable structure metadata only. It does not copy HTML/CSS/JavaScript into the Unity project, execute browser preview scripts, or generate per-Panel business C#.

## Requirements and installation

- Unity 2022.3
- `com.unity.ugui` 1.0.0
- `com.unity.textmeshpro` 3.0.7
- `com.unity.nuget.newtonsoft-json` 3.0.2 (Editor evidence JSON)

Install through Unity Package Manager using **Add package from disk** and select `package.json`, or **Add package from tarball** and select the `.tgz` release.

For Git installation, use `https://github.com/xzzhuu/html-to-unity-ugui-workflow.git?path=/Packages/com.nexvr.html-to-ugui` in **Add package from git URL**. Import TMP Essential Resources and configure a font containing the needed glyphs.

## Import a prepared Panel

Choose **Tools → HTML to UGUI → Import Panel...**, select a prepared HTML file outside the Unity project, inspect the import report, and run the import. The optional `panel.package.json` configures a prepared HTML package; it is distinct from the UPM package's `package.json`. Select a TMP font with the needed glyphs or configure the project's default font.

The XHTML root describes one Panel using `data-root="true"` and a semantic PascalCase `data-panel-name` ending in `Panel`. Its areas and controls are children of that one root. Use pixel dimensions and the documented CSS subset. Dynamic repeated content is described with one-root `<template data-prefab="...">` blocks and item-scoped bindings. Browser preview data and JavaScript are for browser review only.

## Unity outputs

- Default staging: `Assets/HtmlToUGUIStage/{PanelName}/Prefabs/` and `Sprites/`.
- Preview scene: `Assets/HtmlToUGUIStage/StageScene.unity`.
- After explicit selection and copying: `Assets/Art/UIPrefab/{PanelName}/` contains the main and Item Prefabs; images use `Assets/Art/UI/Sprite/{PanelName}/`.
- Low-level import creates resources only. The import window additionally updates the dedicated StageScene asset; it does not place objects in the user's normal scene or mark that scene dirty.

Use **Stage Panels...** to preview one staged panel at a time in StageScene. The import window updates this persistent scene asset while preserving the active scene. **Capture Isolated Preview + Component Audit** remains available for screenshots. Low-level `Import` only generates resources; `ImportOptions` defaults to the persistent staging root, and custom integration/tests can explicitly supply another staging configuration.

## Conversion and runtime boundary

The converter builds native UGUI/TMP components and copies referenced PNG files; it does not render the HTML in a browser. Its CSS support is intentionally limited. Unsupported selectors or visual effects are reported in `import-report.json`; a successful structural import does not prove visual parity. Compare the resulting Unity view against the approved layout and HTML preview.

The package may attach generic `UiBindingMap` and `UiTemplateCatalog` metadata. `data-binding` values identify UI elements; the binding map provides typed lookup, and the catalog references generated Item Prefabs. They do not fetch domain data, perform application actions, instantiate runtime rows, or implement a Panel Presenter. The receiving application owns its data, event handling, and runtime list construction.

## Package contents

`Editor/` contains the generic importer, asset validation, Prefab construction and placement, and visual-audit helpers. `Runtime/Core/` contains generic binding, template-reference, and naming helpers. The package has no TowerCrane or Inventory Presenter sources. Unity projects should keep their business logic in their own application code.

For the reusable design workflow, global Skill installation, directory rules, review gates, and prompt templates, use the separate `ui-panel-design-to-html` Codex Skill. Those design documents and source HTML remain outside this package and outside the Unity project.


## License

Distributed under the [MIT license](LICENSE.md). Unity and other dependencies retain their own licenses.

## Plan, stage, recover

`HtmlUiImporter.PlanImport(htmlPath, outputFolder, options)` is read-only. It lists create/overwrite/reuse-with-import-settings actions, target paths, existing resource consumers and warnings. `ReadPackage` / `ReadPreparedHtml` expose the effective source request; the window selects one source and shows its plan. Plans are recalculated immediately before import.

Set `ImportOptions.stagingRoot` (or the package JSON field) to e.g. `Assets/HtmlToUGUIStage/SettingsPanel`. All images, Items, the root Prefab and report go under this root. Formal asset paths are untouched. `fontMaterial` accepts an existing shared TMP material (asset path in package JSON); it is reused without cloning or writing it. When omitted, the font's existing material is reused. `configureRoot` and `configureTemplate` remain integration hooks and must modify only the generated objects.

Existing Prefabs are protected by default. Explicit `allowPrefabReplacement=true` regenerates disposable generated Prefabs; it does not patch a business Prefab or guarantee retention of internal component references, UnityEvents or serialized project bindings. Adapt integrated panels from staging using project tools. Ordinary ScrollRect conversion does not implement LoopScrollRect or project data wiring.

A handled import failure restores the planned resources and their meta files and removes newly created resource folders. This is not crash/power-loss recovery or a transaction for arbitrary callback side effects. Keep source control backups; promotion is an explicit operation in the Stage Panels window; integrated business Prefabs still need project-specific adaptation. Scene placement is outside the resource transaction.

## Supported behavior

| Input | Behavior |
| --- | --- |
| Explicit CSS background-color/background + Sprite | Base Image.color is retained; no explicit color defaults to white |
| data-normal/highlighted/pressed/selected/disabled-color | Native ColorTint multipliers (#RRGGBB / #RRGGBBAA), multiplied by base tint |
| data-normal-sprite | Selectable target Image's normal sprite |
| data-highlighted/pressed/selected/disabled-sprite | Corresponding native SpriteState; selected means navigation selection, not Toggle.isOn |
| Color and sprite state attributes together | Rejected; one native transition mode per Selectable |
| border | L/B/R/T in whole-PNG pixels; display size remains independent |
| contentRect | Optional [x,y,width,height], bottom-left integer pixels; only full-texture rectangles supported. Invalid or padded rectangles are rejected |
| Texture fidelity | Uncompressed, no mipmaps, NPOT=None, Max Size raised to fit source; restrictive known platform overrides fail for review |
| Project naming/material/list rules | Configure shared materials and project adapters; no automatic business naming migration or virtualization |
| UGUI reverse export to HTML | Unsupported |

## Skill evidence and import report

Generic API imports do not require design approval. Skill-generated package JSON enables `requireSkillApproval=true`; import checks existing full-workspace readiness, explicit panel/element approvals, definition hashes and HTML acceptance including exact file sets. It never generates approvals or runs browser checks. Changes during conversion fail and roll back the resource batch.

The report records converter version, effective configuration and hash, source file hashes, output paths/GUIDs/file and meta hashes, actions, consumers, warnings and pending visual-review status. Its own fingerprint is omitted to avoid recursion. Successful conversion is not visual approval. HTML readiness, Unity completion and target-device verification are separate stages.

Run `HtmlUiRegressionChecks.Run()` and `UiContractRegressionChecks.Run(externalEvidenceDirectory)` in the Editor for the existing and contract regression suites. The latter uses explicitly synthetic fixtures, captures an isolated PNG, and retains successful generated outputs under `Assets/HtmlUiValidation/` for inspection. It does not certify a production design or VR display quality.

## Current execution contract

- Package mode inherits its JSON configuration. Window overrides require enabling **Override package settings**; the effective plan shows the resulting values. Changes to inputs, destinations, resource actions or consumers invalidate a reviewed plan. Existing-resource operations require inspecting the plan in the window. API callers may pass `reviewedPlan` to `Import` for the same freshness check.
- Repeated plans reuse consumer results while Unity's artifact dependency version and target set remain unchanged. Execution still recomputes input fingerprints and resource actions. Reports include planning time.
- Missing workspace mode is compatible with `full` in both Python and Unity. Strict Skill approval requires the ready package's approval switch and staging root, and validates each additional preview approval.
- The importer reuses the selected font's existing material (the TMP default font when no font is specified). It never creates or saves font/material copies. Legacy `boldWeight` remains readable but does not edit shared resources. Native text styles still apply.
- Empty Item folders are not created. Conversion reports stay in the persistent stage workspace; formal directories contain only selected runtime assets.
- Recovery attempts every planned path even when one fails, preserves the original import error alongside recovery errors, and saves a JSON failure report under the OS temporary `HtmlUiImportFailures` directory. It does not provide crash recovery.
- `UiReleaseValidation.Run(externalDirectory)` executes the importer, contract and stage/copy Editor suites. `Tools/verify-unity-release.py` runs the same entry in a closed, prepared Unity project; it rejects a package that differs from this repository. The manual Unity release workflow requires a configured self-hosted Windows runner with label `Unity2022`, an existing default TMP font, and environment variables `UNITY_EDITOR_PATH` and `UNITY_VALIDATION_PROJECT`. Successful test fixtures are disposable and should be removed after inspection.


## Persistent stage workspace and selective copy

Open **Tools → HTML to UGUI → Stage Panels...** (面板暂存与复制). Imports default to `Assets/HtmlToUGUIStage/{PanelName}`; the default was also added to prepared HTML and legacy package readers. Explicit existing staging configurations remain supported.

- **Preview** opens `StageScene.unity`, showing one panel with its own CanvasScaler reference resolution. Further selections switch in place; pending scene edits remain unsaved without a prompt. Rebuilding a loaded, unsaved StageScene is rejected until its changes are saved/discarded. Generating resources never closes the user's scene.
- Check one or more panels, inspect their copy plan, and use **复制勾选面板到正式目录**. Target roots are configurable. Existing formal resources are protected unless replacement is explicitly enabled.
- Copying includes the selected panels' generated Item/PNG dependencies and remaps serialized references. Item Prefabs share the panel folder. Fonts and existing project materials are reused; no fonts are duplicated.
- Plans bind staged and target file/meta fingerprints. A stale plan fails before copying. The resource batch rolls back on handled failures, and formal GUIDs are retained on explicit replacement. This is regeneration of generated resources, not a patch preserving business events or internal component references.
- Staged Prefabs, Scene and reports remain for review. Copy reports under `HtmlToUGUIStage/CopyReports/` contain final artifact fingerprints and source-report hashes for acceptance records. Do not remove the permanent workspace while cleaning synthetic tests.
- `UiStageWorkspace.PlanCopy` / `CopySelected` expose the same selection/copy behavior for Editor integrations. Cross-panel staged dependencies and non-Prefab/PNG assets inside staging are rejected for explicit review rather than silently copying unselected panels or fonts.
