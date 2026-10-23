# Changelog

## 2026-10-24

- 维护者署名统一为 Nex Xie，更新许可证与 UPM 作者配置；历史提交通过邮箱映射统一显示名，新提交使用同一署名。
- 已批准工作区、Unity 产物与审核证据保留原始换行字节，防止跨平台 Git 检出改变验收指纹。
- 工作区校验兼容 Git 克隆不保留空素材分类目录的行为，继续严格检查实际引用与必需设计目录，避免添加占位文件改变已批准 HTML 文件集合。

- 正式复制图片根改为 `Assets/Art/UI/Sprites`，游戏共享 Sprite 去掉重复的 `Sprites/` 层；现有星港图片通过 Unity 移动保留 GUID，并以迁移记录关联原复制证据。
- 回归资源统一使用 `Assets/HtmlUiValidation/`，成功或失败均清理本次合成资源，并在为空时移除父目录；不再创建旧 `Assets/Generated/`。报告与截图继续保存于工程 `Assets` 之外。
- 修复独立草稿 HTML 缺少暂存配置、HTML/准备包项目配置不一致未被发现、无效项目初始化残留空目录，以及素材配置接受错误 JSON 类型的问题。
- 单面板窗口恢复未批准技术示例的独立导入；正式工作流继续要求游戏标识和审批。导入计划新增目标文件及 meta 哈希，拒绝计划之后发生的外部修改。
- Skill 安装器拒绝源与目标目录互相嵌套；交互式 Unity 发布验证清除旧成功记录并保存本轮失败证据。
- 浏览器工具改用标准 Node 依赖与可配置浏览器/服务地址，复验输出与已批准设计证据分离；工作区校验覆盖新增游戏及草稿面板。
- 退役 Example 三面板及相关源包、正式/暂存资源、截图和专属工具，以星港六页完整流程为唯一制作示例；新增星港交付检查，验证真实 TMP 默认 PICOSans 字体、共享材质及两处产物指纹。

- Unity 测试工程统一为仓库 `UnityProject/`。
- UPM 包名与源码目录改为 `com.nex.html-to-ugui`，同步工程依赖、安装地址、校验和打包脚本，保留 GUID。
- 星港六面板迁入 `Workspaces/StarportSalvage/Panels/`，同步初始化、设计工具、预览、项目根定位及验收元数据。
- 转换器窗口按钮、字段与菜单子项统一使用中文。
- 移除塔吊展示的源包、工作区、截图及测试依赖；文档与 GitHub 展示同步更新。


- 修复旧 StarportSalvage 工作区缺少项目素材清单导致批量导入失败：迁移已批准素材的组件/部件归属与开关三态，保留原始审核记录；共享清单错误只报告一次并显示完整路径。修复 Python 项目素材验证覆盖组件计数变量的问题。

- 游戏级已确认素材库：单面板与批量生成前统一增量同步，按实际组件类型/部件/状态/像素与导入配置去重，保留规范 GUID，安全迁移重复引用；支持 Toggle 三张整图与 Slider 部件素材。

## Unreleased — persistent stage workspace

- Add the default-enabled “保留面板名称子目录” Stage Panels option and `PlanCopy(..., keepPanelNameSubdirectory: false)` for flat main Prefabs. Both modes copy Item Prefabs into the shared formal `Item/` folder, reject duplicate destination names, and record the option in signed plans and copy evidence.
- Reuse identical formal PNGs only when content and all non-owner-GUID metadata agree; retain Sprite import/subasset settings and reject incomplete destination asset/meta pairs.
- Add structured stage-copy dry runs, per-resource overwrite approval and confirmation, consumer/atlas/bundle review findings, and before/after action evidence. Keep source assets and require manual project-specific loading/build integration.

- Add **导入整个项目...** with recursive prepared-package discovery, selectable panels, whole-selection read-only preflight, stale-plan rejection, staged overwrite protection, cancellation and per-panel results/reports.
- Group project imports under `HtmlToUGUIStage/{ProjectName}/{PanelName}`; support optional single-package `projectName`, preserve legacy layouts and preview same-named panels across projects using project-qualified paths.
- Add real Editor regression coverage for project scanning, grouped import/copy/preview, partial failures and preserved scene state.

- Default generation to `Assets/HtmlToUGUIStage/{PanelName}` and preview panels in a persistent `StageScene.unity`.
- Add the Stage Panels window with individual preview buttons, multi-panel selection and reviewed copying into formal folders.
- Copy generated Prefab/Item/PNG dependencies with reference remapping, stale-plan checks, overwrite protection and batch recovery; reuse existing project fonts/materials.
- Keep review reports in staging and flatten formal Item Prefabs into the shared formal `Item/` folder. Add Editor regression coverage for copy selection, references, rollback and scene switching.

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

## 2026-10-24 — 星港实际视觉复验

- 使用 PICOSansVFESC SDF 对正式六面板逐页捕获 Unity 图像，并与批准原图和相同数据集的 HTML 对照。
- 修复完整 Toggle 状态图被再次染色的问题，加入 Editor 回归；通过原生装配纠正滑块尺寸、遮挡顺序和九宫格圆角比例。
- 将 PICOSans 不支持的 U+2212 统一为 ASCII 减号，保留批准原文并记录技术修正。
- 记录原生装配参数、动态 Item 数据对齐、禁用透明度、字形覆盖、正式/暂存指纹及重新导入边界。
