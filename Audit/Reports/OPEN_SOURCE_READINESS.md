# 公开分发说明

更新日期：2026-10-24。UPM 包为 `com.nex.html-to-ugui` 0.4.1，源码在 `Packages/com.nex.html-to-ugui`。

仓库包含通用转换器、唯一工作流 Skill、星港六页完整制作示例、校验工具、文档和 Unity 测试工程。旧 Example 三面板及相关源包和 Unity 资源已移除。当前制作与目录约定见 [星港流程](../../docs/game-example/workflow.md)。

字体按用户要求使用本机已有 PICOSansVFESC SDF 和其共享材质；字体、TMP 必需资源、厂商示例、本机 SDK、Unity 缓存及运行日志不参与分发。新机器需按 [工程说明](../../UnityProject/README.md) 恢复合法资源。

MIT、贡献指南、安全策略、Issue/PR 模板和 CI 保留。Python CI 校验全部工作流单测、游戏契约、初始化、准备契约、安装器、六个 ready 源包、文档、完整性和打包；浏览器 CI 验证星港 16 项流程。Unity Editor 编译、原生状态、复制、恢复与视觉检查属于独立验证。当前示例另有 20 状态 × 3 分辨率的原生矩阵和独立 Editor 内存演示业务验收，详见[完成报告](WORKFLOW_COMPLETION_20261024.md)；这些记录不证明已接入生产服务或已通过设备实测。

正式与暂存交付清单位于游戏根 `unity-delivery.json`。当前实际视觉证据见 [转换规范](../../docs/game-example/conversion-rules.md)，本轮结果见 [工作流检查报告](WORKFLOW_REVIEW_20261024.md)。原始批准与历史报告保持其日期和适用范围。
