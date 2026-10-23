# 本次提交与验证说明 · 2026-10-24

维护者：Nex Xie。提交范围为已完成的通用转换器、工作流 Skill、星港六页源包与 Unity 交付、配套文档和验证工具。

## 行为与验收结果

- 包名统一为 `com.nex.html-to-ugui`；支持游戏级素材库、批量导入、审阅计划、受控复制及原生 Toggle 状态。
- 游戏源位于 `Workspaces/StarportSalvage/Panels/`；正式主面板、共享 Item 和图片分别按现行目录规则交付，旧 Example 源包及产物已退役。
- 本轮运行：36 项 Python 单测；初始化 6 项、准备契约 8 项、安装器 5 项；真实 Editor 173 项（38/36/42/25/32）；浏览器 14 项。
- 实际交付核对：6 个主面板、4 个 Item、24 张共享 PNG，正式/暂存各 34 个资源，PICOSans 默认字体及共享材质、GUID、引用和指纹通过。
- 当前 UPM 打包包含 66 个逐文件核对一致的包文件；不分发本机字体、TMP 必需资源、SDK、缓存和运行日志。
- 已批准源包与交付证据保留原始字节，兼容 Git 不保存空素材目录；从 Git 索引导出的干净副本单独复验。

## 作者核对范围

两份许可证、UPM 作者字段、本仓库 Git `user.name` 与本次新提交统一使用 Nex Xie。对全部待提交文件按大小写及分隔符变体扫描旧作者名；通过作者邮箱的 `.mailmap` 检查历史作者/提交者显示名。GitHub 仓库描述与主题不含旧作者名。

`.mailmap` 只归一历史显示名；已发布提交的原始 author/committer 字段、旧版本文件与 SHA 没有重写。原始历史对象和被忽略的历史归档不属于当前工作树零残留的声明范围。

## 文档路径与修改要点

以下逐项列出新增、修改或迁移后的 Markdown 文档；历史批准和原始自检文档为现有证据随示例纳入，未生成新的用户批准。

| 文件路径 | 修改要点 |
| --- | --- |
| `AGENTS.md` | 记录当前工程、包名、中文入口、游戏目录和交付检查约定。 |
| `Audit/README.md` | 整理公开审计入口及本机运行记录边界。 |
| `Audit/Reports/OPEN_SOURCE_READINESS.md` | 同步现行包、星港示例、许可、CI 与实际验收边界。 |
| `Audit/Reports/PROJECT_MANIFEST_FIX_20261023.md` | 保留项目素材清单修复经过及其当时验证范围。 |
| `Audit/Reports/WORKFLOW_REVIEW_20261024.md` | 记录流程修复、真实 Editor/浏览器检查与最终正式图片目录。 |
| `Audit/Reports/WORKSPACE_MIGRATION_20261024.md` | 记录包名、工作区、资源路径迁移及后续规则。 |
| `CHANGELOG.md` | 记录批量导入、共享素材、复制保护、目录迁移、视觉修复、发布兼容修复和作者更新。 |
| `CONTRIBUTING.md` | 明确新源码位置、审批与指纹边界、Python/浏览器/Unity 检查及作者映射规则。 |
| `Packages/com.nex.html-to-ugui/LICENSE.md` | 包目录迁移，维护者版权署名统一为 Nex Xie。 |
| `Packages/com.nex.html-to-ugui/README.md` | 说明项目扫描、共享素材契约、只读计划、审批、暂存预览、正式复制和恢复边界。 |
| `README.en.md` | 同步包名迁移、星港六页入口、正式/暂存路径、跨机器使用、验证命令和作者署名。 |
| `README.md` | 同步包名迁移、星港六页入口、正式/暂存路径、跨机器使用、验证命令和作者署名。 |
| `Skills/ui-panel-design-to-html/SKILL.md` | 更新项目素材确认、导入准备、命名与实际交付约束。 |
| `Skills/ui-panel-design-to-html/references/component-naming.md` | 同步组件、部件、状态及素材身份命名。 |
| `Skills/ui-panel-design-to-html/references/html-contract.md` | 同步项目根、准备包配置和原生控件转换要求。 |
| `Skills/ui-panel-design-to-html/references/manifests.md` | 更新共享素材、审批、HTML/Unity 就绪证据与最终资源契约。 |
| `Skills/ui-panel-design-to-html/references/project-assets.md` | 新增游戏共享素材库、确认与复用规则。 |
| `Skills/ui-panel-design-to-html/references/usage.md` | 同步两分支、项目导入及克隆时空目录校验行为。 |
| `THIRD_PARTY_NOTICES.md` | 说明当前示例素材和依赖来源、许可，以及字体/TMP 资源不分发的边界。 |
| `Tools/README.md` | 说明六页交付、浏览器自启服务、可配置输出与 Unity 发布回归入口。 |
| `UnityProject/Assets/docs/项目总体流程.md` | 说明从批准、素材库、HTML、Unity 装配到正式交付的当前步骤。 |
| `UnityProject/Assets/docs/项目整理执行报告.md` | 保留历史整理记录并标明适用日期，链接现行星港流程。 |
| `UnityProject/README.md` | 说明仓库相对包依赖、正式/暂存产物、合法字体恢复、重导入和交付核对。 |
| `Workspaces/README.md` | 明确游戏根、Panels 子目录、素材清单和批量导入规则。 |
| `Workspaces/StarportSalvage/Panels/ActionConfirmPanel/design/review/html-acceptance.md` | 随示例纳入既有 HTML 验收说明，保留原批准与原始证据。 |
| `Workspaces/StarportSalvage/Panels/ActionConfirmPanel/inputs/brief.md` | 随完整示例纳入该面板的现有需求，未新造设计批准。 |
| `Workspaces/StarportSalvage/Panels/ExpeditionResultPanel/design/review/html-acceptance.md` | 随示例纳入既有 HTML 验收说明，保留原批准与原始证据。 |
| `Workspaces/StarportSalvage/Panels/ExpeditionResultPanel/inputs/brief.md` | 随完整示例纳入该面板的现有需求，未新造设计批准。 |
| `Workspaces/StarportSalvage/Panels/ExpeditionSelectPanel/design/review/html-acceptance.md` | 随示例纳入既有 HTML 验收说明，保留原批准与原始证据。 |
| `Workspaces/StarportSalvage/Panels/ExpeditionSelectPanel/inputs/brief.md` | 随完整示例纳入该面板的现有需求，未新造设计批准。 |
| `Workspaces/StarportSalvage/Panels/GameSettingsPanel/design/review/html-acceptance.md` | 随示例纳入既有 HTML 验收说明，保留原批准与原始证据。 |
| `Workspaces/StarportSalvage/Panels/GameSettingsPanel/inputs/brief.md` | 随完整示例纳入该面板的现有需求，未新造设计批准。 |
| `Workspaces/StarportSalvage/Panels/StarportHomePanel/design/review/html-acceptance.md` | 随示例纳入既有 HTML 验收说明，保留原批准与原始证据。 |
| `Workspaces/StarportSalvage/Panels/StarportHomePanel/inputs/brief.md` | 随完整示例纳入该面板的现有需求，未新造设计批准。 |
| `Workspaces/StarportSalvage/Panels/SupplyInventoryPanel/design/review/html-acceptance.md` | 随示例纳入既有 HTML 验收说明，保留原批准与原始证据。 |
| `Workspaces/StarportSalvage/Panels/SupplyInventoryPanel/inputs/brief.md` | 随完整示例纳入该面板的现有需求，未新造设计批准。 |
| `Workspaces/StarportSalvage/README.md` | 提供六页演示入口、批准记录、HTML/Unity 状态与复验方法。 |
| `Workspaces/StarportSalvage/design-review/element-groups-review-v01.md` | 纳入原始设计/元素自检记录，保留其日期和审批状态。 |
| `Workspaces/StarportSalvage/design-review/review-v01.md` | 纳入原始设计/元素自检记录，保留其日期和审批状态。 |
| `docs/game-example/conversion-rules.md` | 明确同数据集视觉核对、原生圆角/Slider 参数及重新导入边界。 |
| `docs/game-example/game-requirements.md` | 纳入示例游戏范围、玩法和数据要求。 |
| `docs/game-example/game-settings-unity-fix.md` | 保留设置页原生开关与滑块修复说明和证据。 |
| `docs/game-example/ui-requirements.md` | 纳入六页需求、布局、控件与状态规范。 |
| `docs/game-example/workflow.md` | 串联需求、双确认、素材导出、HTML、Unity 视觉适配和正式交付。 |
| `docs/project-assets.md` | 说明共享素材身份、组件隔离、Toggle/Slider 部件、源引用和正式路径区别。 |
| `docs/screenshots/README.md` | 替换退役截图说明，说明星港当前浏览器截图来源。 |
| `docs/stage-promotion.md` | 说明复制目录选项、共享 Item、PNG 复用、逐资源覆盖审批和证据。 |
| `docs/submission-20261024.md` | 列出本次提交范围、作者处理边界、逐项文档与实际验证结果。 |
| `docs/workspace-layout.md` | 统一当前 Unity 工程、包名、游戏 Panels、共享图片和正式/暂存目录。 |

退役文档：`Examples/Inventory/inputs/brief.md`、`Examples/README.md`、`Examples/Settings/inputs/brief.md`、`Examples/Starter/inputs/brief.md`、`Examples/TowerCrane/README.md`、`Examples/TowerCrane/inputs/brief.md`

## 验证边界

新机器需自行恢复合法 TMP/字体资源并调整可选 Unity MCP SDK 依赖；普通 CI 不替代 Editor、真实视觉和设备验收。当前示例数据/页面预览不代表完整游戏业务已接通，未声明 VR 设备已验证。
