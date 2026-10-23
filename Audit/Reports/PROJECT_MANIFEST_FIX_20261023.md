# StarportSalvage 项目批量导入修复

> 当前测试工程已于 2026-10-24 切换为 `E:/html_to_unity_ugui_workflow/UnityProject`；本文保留 2026-10-23 的历史验证范围与原始证据，不能替代新工程的本次检查。星港源包现位于 `Workspaces/StarportSalvage/Panels/{PanelName}/`。

截图中的根因是六个包已经设置 projectName，但源根目录未提供 project.assets.json；项目规划失败后继续逐包规划，重复显示同一错误。

## 修复

- 缺失清单时报告完整源路径、配置要求及迁移说明；共享清单失败立即停止规划，避免每个面板重复报错。保持只读规划及批准校验。
- 补齐 StarportSalvage/project.assets.json：24 个运行时素材身份，按实际组件类型、部件及状态隔离。六包改用 asset:ID；相同类型、部件的素材跨面板共用。保留浏览器原素材。
- Toggle 整图由既有批准图中展示的 90×42 轨道/旋钮形状、位置、颜色合成，导出为 1024×512 POT PNG；Off、On、Disabled 保持独立。Slider 登记 Track、Fill、Handle。
- 原审核记录和 HTML 已原样归档至 Workspaces/StarportSalvage/design-review/project-migration-v01；迁移记录明确沿用既有批准，未新增用户确认。更新元数据哈希前校验原批准/文件指纹；重新验证 HTML 后更新验收指纹。
- 修复 validate_directory.py 项目素材检查中 expected 变量被状态语义覆盖，导致最终组件计数 len(None) 的错误，并增加针对性回归。

## 实际验证

目标工程：E:/Unity Project/Work Space/HTML2UGUI，Unity 2022.3.62f3c1，直接引用本仓库 UPM 包。

- UiProjectImportRegressionChecks：25 项通过，含新增缺失/损坏共享清单只报告一次的回归。
- UiProjectAssetRegressionChecks：30 项通过。
- Python test_workflow.py：30 项通过。
- 六包 require-ready 校验通过；14 项真实 Chromium 浏览器交互检查通过。六张默认截图对比原验收图，RGB 逐像素一致。
- 实际六面板全部 imported，批次 completed，零错误；生成 6 个主 Prefab、4 个 Item，24 张共享 PNG。暂存位置 Assets/HtmlToUGUIStage/StarportSalvage。
- Example 的实际目标工程目录、字体、原生组件及交付指纹校验通过。

原始证据位于同级 ProjectManifestFix-20261023/：project-import-regression.json、project-assets-regression.json、starport-plan.json、starport-import.json、starport-structure.json。

本次验证覆盖导入与结构、Toggle 状态切换；不将其登记为完整 Unity 视觉/游戏交互或 VR 设备验收。正式复制和业务接线维持后续阶段。
