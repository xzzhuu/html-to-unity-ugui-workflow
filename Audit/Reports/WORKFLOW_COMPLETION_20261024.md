# 星港工作流修改与编辑器验收完成

验收范围按用户确认：StarportSalvage，以 Unity 编辑器测试为准。工程为仓库 UnityProject，Unity 2022.3.62f3c1，转换包 0.4.1。设备状态为不适用，未登记设备通过。

## 完成内容

- 六面板所有声明状态完成原生验收：20 个页面/fixture 组合，在 1920×1080、1280×720、1920×1200 下共 60 例。检查实际正式 Prefab、动态 Item、原生控件、完整节点几何、字形与溢出，并查看实际画面。
- 修复背包重量条动态宽度、长名称字号/高度/稀有度位置及说明滚动内容高度。长名称使用当前 PICOSans 字体，通过 TMP 原生 20～22 字号适配，保留已有字体和材质。
- 完成独立的 Editor 业务适配器：实际正式面板与 Item 的 Button、TMP Input、Toggle、Slider 事件连接内存演示状态，覆盖导航、整备、派遣、取消、结算、设置、重试、重置与返回来源。三秒边界使用可控测试时钟；浏览器另有实际三秒等待检查。
- 业务检查包含锁定任务、库存为零、资源不足、确认时重新校验、原子超重拒绝、执行中禁止改携行、延后结算、一次性领取、急救包消耗、无急救包结算、搜索分类、无效名称、Unicode 字符边界、草稿取消及 Esc。
- 原生验收升级到 v2：强制完整状态/分辨率矩阵、当前 HTML 与游戏输入、实际依赖文件/meta、Prefab 字节、截图哈希与尺寸、重算的几何结果一致。六页记录统一验证并发布，失败回滚本次触及的文件。过期证据不再计入 fixture 覆盖率。
- Python 与 Unity 共用 23 组原生装配契约用例，覆盖草稿、批准状态、组件/部件归属、类型与浮点边界；影响计划可发现登记图片的实际像素变化。补齐 Schema、说明与操作入口。

## 本轮验证

| 验证 | 结果 |
| --- | --- |
| Python 单测 | 77 项通过，含验收发布回滚、矩阵伪覆盖/过期源/尺寸错误拒绝及 Editor 证据过期检查 |
| 初始化 / 准备契约 / 安装器 | 6 / 8 / 5 项通过 |
| 浏览器交互 | 16 项通过，含 Editor 数据与真实浏览器数据一致性 |
| Unity 通用完整回归 | 212 项通过：原回归 38、契约 36、暂存 42、项目 25、素材 32、工作流 39 |
| Unity 原生状态/分辨率矩阵 | 60 / 60 通过，全节点比较与溢出检查通过 |
| Unity Editor 业务 | 63 项业务与场景断言、154 项受保护文件指纹检查通过，16 张实际逐步截图 |
| 星港实际交付检查 | 六个源包、六个正式面板、四个 Item、24 张共享图片及字体/引用/历史报告通过 |

正式与原暂存资源各 34 个，GUID、文件与 meta 保持既有 [交付清单](../../Workspaces/StarportSalvage/unity-delivery.json) 一致。批准图、HTML 源包及原始转换/复制报告未覆盖。业务适配器只存在于测试工程的 Editor 目录；正式 Prefab 未挂载专属业务或视觉脚本，StageScene 保持原有场景与干净状态。通用包不包含星港业务逻辑。

## 证据与复验

- 当前分范围状态：[workflow-status.json](../../Workspaces/StarportSalvage/design-review/workflow-status.json)。全部原生 fixture 已覆盖，editorVerification=passed。
- 编辑器业务报告及逐步截图：[editor-flow.json](../../Workspaces/StarportSalvage/design-review/editor/editor-flow.json)。同目录保留执行版本适配器源码，验证时与工程当前源码比对。
- 各面板 `design/review/unity-acceptance.json`、`business-acceptance.json` 和 `unity/` 目录保存完整可重验记录。
- 本机详细通用回归与矩阵原始输出位于忽略目录 Audit/Runs/latest/workflow-completion-release、native-matrix-final、starport-editor-flow-final；维护证据已复制到工作区。

```text
python -B -X utf8 Tools/verify-starport-panels.py --unity-project UnityProject
python -B -X utf8 Tools/game-workflow.py status --root Workspaces/StarportSalvage --unity-project UnityProject --require-unity --require-editor
```

Unity 菜单“HTML转UGUI → 星港示例 → 运行编辑器业务验收”可重跑业务流程。执行版本源码自动归档到报告目录，实际查看截图后再用 `editor_acceptance.py` 更新记录；完整操作见 [契约与统一验收说明](../../Skills/ui-panel-design-to-html/references/game-contracts.md)。原生矩阵仍通过 `UiReferenceAudit.RunMatrix` 单独执行。

本轮完成的是编辑器中的内存演示业务与原生 UI 验收，生产游戏服务由接入应用提供。三种分辨率采用隔离 Canvas 等比适配。重导入复验的有效结构与外部 GUID 可保留，Unity 内部 fileID 与 Prefab 序列化字节不承诺确定性。全局个人 Skill 安装继续独立管理；仓库 Skill 和转换包为本轮交付版本。
