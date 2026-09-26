# HTML to Unity UGUI Workflow

[简体中文](README.md) | [English](README.en.md)

[![Verify](https://github.com/xzzhuu/html-to-unity-ugui-workflow/actions/workflows/verify.yml/badge.svg)](https://github.com/xzzhuu/html-to-unity-ugui-workflow/actions/workflows/verify.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

A reusable workflow from UI requirements and layout review to browser-previewable XHTML and native Unity UGUI / TextMesh Pro prefabs.

This repository includes a Codex Skill, four external HTML examples, Python tools, and the complete source for the `com.nexvr.html-to-ugui` 0.2.0 UPM package. Design source files stay outside the Unity project. The importer creates image assets, Panel / Item prefabs, and generic binding metadata; the receiving application implements its own data handling and interactions.

## Features and boundaries

- Initialize a standard HTML UI workspace while preserving existing files on repeated runs.
- Describe each element's purpose, semantic name, parent, native component, binding, and dynamic Item template.
- Validate XHTML, assets, manifests, approval records, and readiness status.
- Import prepared XHTML and PNG assets as native UGUI/TMP Panel and Item prefabs.
- Support browser previews and structural validation. The importer supports a limited CSS subset and does not guarantee faithful conversion of arbitrary web pages.

TowerCrane is a `converter-example-unapproved` conversion example, and Starter is a `draft-unapproved` draft. Neither is an approved final visual deliverable. Passing structural validation does not establish visual parity or user approval. Browser JavaScript is not converted into Unity application logic.

## Example screenshots

These are actual browser captures of the repository's HTML examples using bundled sample data. They are not Unity runtime screenshots or approved final visual deliverables. The example UI text is currently in Chinese.

### TowerCrane · 1920 × 1080

A device list, six live metric cards, four historical trend charts, and alarm status.

![Tower crane dashboard HTML preview](docs/screenshots/tower-crane-dashboard.png)

| Inventory · 1280 × 720 | Settings · 720 × 1280 |
| --- | --- |
| Dynamic item list and selection | Text input, notification toggle, and volume slider |
| <img src="docs/screenshots/inventory-panel.png" alt="Inventory panel with sample item 3 selected" width="560" /> | <img src="docs/screenshots/settings-panel.png" alt="Portrait settings panel" width="240" /> |

The [complete gallery and reproduction notes](docs/screenshots/README.md) also include a device-switch state and the Starter initialization draft.

## Quick start

Requirements: **Python 3.10+**. Unity import requires **Unity 2022.3**, UGUI 1.0.0, and TMP 3.0.7. Opening the HTML examples in a browser does not require Unity.

```sh
git clone https://github.com/xzzhuu/html-to-unity-ugui-workflow.git
cd html-to-unity-ugui-workflow
python -X utf8 Skills/ui-panel-design-to-html/scripts/init_directory.py --root ./MyUI/Inventory --panel-name InventoryPanel
python -X utf8 Skills/ui-panel-design-to-html/scripts/validate_directory.py --root ./MyUI/Inventory
```

Open the generated `MyUI/Inventory/html/InventoryPanel.html` in your browser, or open the [Inventory example](Examples/Inventory/html/InventoryPanel.html) from your local clone. Initialization produces an unapproved draft.

### Install the Codex Skill

Choose either installation method below.

**Option 1: Install with npx (no manual repository clone required)**

Requires Node.js / npm and Git. Use the [Skills CLI](https://github.com/vercel-labs/skills):

```sh
npx skills add https://github.com/xzzhuu/html-to-unity-ugui-workflow/tree/main/Skills/ui-panel-design-to-html --agent codex --global --copy
```

`--agent codex` targets Codex, `--global` installs at user scope, and `--copy` copies the complete Skill without requiring symlinks. The Skills CLI manages installation paths and updates. This installs only the Skill, not the Unity package. The Skill's Python tools still require Python 3.10+.

**Option 2: Use the repository installer after cloning**

Run from the repository root:

```sh
python -X utf8 Tools/install-global-skills.py
```

The default destination is `~/.codex/skills`. The installer supports `CODEX_HOME` and `--destination`, updates files it manages, and preserves unknown user files. It also removes retired workflow Skills previously managed by this installer.

In a new Codex task, invoke the Skill to initialize a workspace:

```text
$ui-panel-design-to-html Initialize the current directory as a new HTML UI workspace.
```

For a full UI design task:

```text
$ui-panel-design-to-html Create a UI Panel from the requirements and reference images. First provide a layout image and an element completeness checklist for my review.
```

The Skill's instructions and supporting reference documents are currently primarily in Chinese. The CLI commands can be used independently of Codex. The formal design workflow requires explicit layout approval before component breakdown and final HTML implementation.

### Install the Unity importer

In Unity Package Manager, choose **Add package from disk** and select `Packages/com.nexvr.html-to-ugui/package.json` from this repository.

Alternatively, choose **Add package from git URL** and enter:

```text
https://github.com/xzzhuu/html-to-unity-ugui-workflow.git?path=/Packages/com.nexvr.html-to-ugui
```

Import TMP Essential Resources and select a TMP font containing the required glyphs. Then choose **Tools → HTML to UGUI → Import Panel...** and select a prepared HTML file outside the Unity project.

Referenced images are written to `Assets/Art/UI/Sprite/{PanelName}/`. The main prefab is written to `Assets/Art/UIPrefab/{PanelName}/`, with dynamic Item prefabs in `Items/`. The root Panel is placed under a Canvas. Save scene changes manually. See the [package guide](Packages/com.nexvr.html-to-ugui/README.md) for input requirements, outputs, and conversion limits.

### Open the sample Unity project

Open `UnityProject/` with Unity 2022.3. Its package manifest references the importer within this repository, so it does not require the original author's local package paths or MCP SDK.

Third-party fonts, TMP Essential Resources, and vendor examples are not distributed. Before using the sample scenes and prefabs:

1. Choose **Window → TextMeshPro → Import TMP Essential Resources**.
2. Prepare a TMP font covering the required characters, including Chinese text where applicable, under an appropriate license.
3. Reimport the desired HTML from `Examples/`, selecting your font in the importer to rebuild font and material references.
4. Check the Console, scene, and prefabs, then save the scene.

Existing prefab font references may be missing until reimport. The original sample project used `2022.3.62f3c1`; compatibility with other 2022.3 patch versions has not been reverified. See the [sample project notes](UnityProject/README.md) for details; those notes are currently in Chinese.

## Repository structure

| Directory | Purpose |
| --- | --- |
| [Skills/ui-panel-design-to-html](Skills/ui-panel-design-to-html/SKILL.md) | Workflow Skill, contracts, scripts, and editable workflow diagrams |
| [Examples](Examples/README.md) | External HTML examples: Settings, Inventory, Starter, and TowerCrane |
| [Packages/com.nexvr.html-to-ugui](Packages/com.nexvr.html-to-ugui/README.md) | Unity Editor importer and generic Runtime metadata |
| [Tools](Tools/README.md) | Installation, validation, layout comparison, and packaging tools |
| [UnityProject](UnityProject/README.md) | Unity 2022.3 sample project; font resources must be restored separately |
| [Audit](Audit/README.md) | Public distribution notes; machine-specific run results are excluded from Git |

Supporting documentation outside the package guide is currently primarily in Chinese.

## Validation and packaging

Run these commands from the repository root:

```sh
python -m pip install -r requirements-dev.txt
python -X utf8 Tools/verify-directory-generator.py
python -X utf8 Tools/verify-preparation-contract.py
python -X utf8 Tools/verify-installer.py
python -X utf8 Tools/verify-workflow-files.py
python -X utf8 Tools/verify-file-integrity.py
python -X utf8 Tools/build-unity-package.py
```

Package archives and file-by-file verification reports are written to `dist/`. Validation reports are written to `Audit/Runs/latest/`. Both directories are excluded from version control.

GitHub Actions runs these checks on Windows and Ubuntu with Python 3.10 and 3.12, and uploads package artifacts. **These checks do not compile the Unity project or perform visual acceptance testing.** Importer changes still require verification in Unity Editor.

## Contributing and license

Read the [contribution guide](CONTRIBUTING.md), [code of conduct](CODE_OF_CONDUCT.md), [security policy](SECURITY.md), and [changelog](CHANGELOG.md). Report bugs and propose features through GitHub Issues. For importer bugs, provide a minimal prepared XHTML example and identify whether the problem affects browser preview, Python validation, or Unity import.

For sensitive security issues, use [private vulnerability reporting](https://github.com/xzzhuu/html-to-unity-ugui-workflow/security/advisories/new) rather than publishing credentials or private project data in an Issue.

This project is distributed under the [MIT license](LICENSE). Third-party dependencies, fonts, and Unity software retain their own licenses; see [third-party notices](THIRD_PARTY_NOTICES.md).
