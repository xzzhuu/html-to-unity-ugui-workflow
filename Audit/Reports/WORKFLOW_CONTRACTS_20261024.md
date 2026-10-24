# 游戏 UI 工作流契约实施与验证

后续补齐的业务流程、多状态矩阵及 0.4.1 验证结果见 [完成报告](WORKFLOW_COMPLETION_20261024.md)。本文件保留 0.4.0 当时的验证范围。

日期：2026-10-24。实际工程：仓库 UnityProject，Unity 2022.3.62f3c1，转换器 0.4.0。

## 已实施

| 方面 | 结果 |
| --- | --- |
| 统一验收 | 星港六页均补齐标准 unity-acceptance.json；绑定批准图、组图、原始转换报告、正式资源及游戏契约哈希。原生 UI、业务接线与设备状态分别报告。 |
| 视觉契约 | 游戏根 contracts/style.json 管理颜色、字体层级、圆角、画布和预览字体依赖；预览、HTML/CSS 生成与原生装配读取对应配置。原批准图和 HTML 字节保留。 |
| 原生重导入 | 新增通用 UiNativeAssembly，按 active 配置重放圆角 PPU 及独立 Slider 部件。配置进入只读计划与指纹，先于已有接入钩子运行。 |
| 页面与数据 | screens.json 关联六页的入口、操作绑定、状态、fixture 和测试；fixtures.json 的 13 种数据预期由真实浏览器核对。状态入口显示 Unity 尚未覆盖的 fixture。 |
| 影响分析 | 游戏初始化、集中诊断、输入快照、只读影响计划和分范围状态入口已加入 Skill 与 Tools；提供三类 Schema。 |
| 视觉回归 | 几何比较支持完整节点路径、动态行、可见性、重复身份和非有限坐标检查。UiReferenceAudit 在隔离预览中填充数据并检查原生控件。 |
| 参考图 | 6 张面板批准图与 9 张元素组图按原图字节导入独立 DesignReferences；不挂到运行时 Prefab。 |
| Skill 与持续集成 | 补充按需读取的契约说明，保留总入口与既有双确认；CI 加入新测试和游戏契约检查。UPM 版本更新至 0.4.0。 |

## 实际验证

- Python 单测 46 项通过；初始化、准备契约、安装器分别 6、8、5 项通过。
- 浏览器 15 项流程通过，含真实 fixture 契约核对、动态列表、完整远征闭环、设置和两种额外分辨率。
- Unity 完整回归 185 项通过：原回归 38、契约 36、暂存 42、项目 25、素材 32、工作流装配 12。
- 六页在 1920×1080、1280×720、1920×1200 的隔离等比预览中完成 18 组节点/字形/原生控件检查；所有可比较节点在 1.1px 容差内，实际图像已查看。
- 六页在独立暂存目录各重新导入两次，原生检查快照一致、GUID 保留；Unity 的 Prefab 序列化字节可以不同，不将其宣称为文件级确定性。
- 正式与原暂存各 34 个资源的 GUID、文件及 meta 指纹保持既有交付记录一致；现有字体和共享材质引用保留。
- 临时重导入目录在确认没有外部消费者后通过 Unity 资源工具清理；HtmlUiValidation 为空后移除。StageScene 无未保存修改，Console 零错误。

标准验收的数据为 Prepared / Result；其他 fixture 的 Unity 覆盖情况由状态报告明确列出。三种分辨率验证采用隔离预览的等比适配，不替代项目 UIRoot、真实游戏服务或目标设备测试。原生事件检查不执行原有业务回调。业务接线和设备实测均保留未测状态。

正式资源仍为原已交付版本；0.4.0 的重导入能力在隔离目录验证，没有用重建的 Prefab 覆盖已有正式资源或改写历史转换报告。此前 HTML 阶段 readiness 已按原字节归档，当前状态另行更新。

## 使用入口

```text
python -B -X utf8 Tools/game-workflow.py check --root Workspaces/StarportSalvage
python -B -X utf8 Tools/game-workflow.py status --root Workspaces/StarportSalvage --unity-project UnityProject
python -B -X utf8 Tools/game-workflow.py impact --root Workspaces/StarportSalvage --baseline Workspaces/StarportSalvage/design-review/workflow-baseline.json
python -B -X utf8 Tools/verify-starport-panels.py --unity-project UnityProject
```

详细字段与使用边界见 [游戏契约说明](../../Skills/ui-panel-design-to-html/references/game-contracts.md)。当前项目状态见 [workflow-status.json](../../Workspaces/StarportSalvage/design-review/workflow-status.json)，各面板实际截图和原生检查位于面板 design/review/unity/。

完整本机运行证据位于忽略目录 Audit/Runs/latest/，包括 game-workflow-release、formal-native-audit、native-replay 和浏览器检查。当前包输出为 dist/com.nex.html-to-ugui-0.4.0.tgz。本轮未创建提交、推送或发布远程 Release；已安装到用户目录的旧 Skill 不会被本轮仓库编辑自动覆盖。
