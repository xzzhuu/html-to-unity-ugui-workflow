# Changelog

## Unreleased — persistent stage workspace

- Default generation to `Assets/HtmlToUGUIStage/{PanelName}` and preview panels in a persistent `StageScene.unity`.
- Add the Stage Panels window with individual preview buttons, multi-panel selection and reviewed copying into formal folders.
- Copy generated Prefab/Item/PNG dependencies with reference remapping, stale-plan checks, overwrite protection and batch recovery; reuse existing project fonts/materials.
- Keep review reports in staging and flatten formal Item Prefabs beside their panel. Add Editor regression coverage for copy selection, references, rollback and scene switching.

## 0.3.0 — preparation/import contract fixes

- Preserve explicit Sprite tint; map native ColorTint and all SpriteSwap states and reject conflicting mappings.
- Add read-only resource plans, complete staging, default Prefab overwrite protection and rollback of planned resources/metas on handled failures.
- Generate resources without modifying the active scene; make placement explicit and keep isolated visual audits.
- Validate and explicitly reject unsupported padded contentRect; preserve texture size/NPOT settings and reject restrictive platform overrides.
- Add opt-in Skill approval verification, input/configuration/output fingerprints, shared font material configuration and a source-specific import window.
- Reject added/deleted HTML acceptance files; add a common Unity completion gate for both Skill branches.

## 0.2.0 — initial public source distribution

- Publish the reusable HTML design Skill, four prepared examples and generic UPM importer together.
- Include Editor conversion tools and runtime binding/template metadata; application business logic remains separate.
- Use repository-relative package paths and clone-independent Python verification.
- Add MIT licensing, contribution guidance, issue/PR templates and GitHub Actions checks.

Earlier local migration/audit records are not a public release history. Existing examples retain their unapproved/draft status.
