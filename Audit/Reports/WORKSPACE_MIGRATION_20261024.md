# 测试工程、Panels 与包名迁移

日期：2026-10-24。当前测试工程：`E:/html_to_unity_ugui_workflow/UnityProject`。

## 已完成

- 星港 6 个完整源工作区迁入 `Workspaces/StarportSalvage/Panels/{PanelName}/`，同步总入口、跨页测试、作者工具、审核索引和源需求链接。共享素材及 `project.assets.json` 保持游戏根目录。
- 准备包和直接 HTML 同时配置 `projectName=StarportSalvage`、`projectDirectory=../../..` 与 `Assets/HtmlToUGUIStage/StarportSalvage/{PanelName}`。新增项目面板由初始化器识别 Panels 及项目清单并自动设置这些字段。
- UPM 包及目录改为 `com.nex.html-to-ugui`；工程依赖、锁文件、安装链接、验证和打包工具同步。28 个已有受 Git 管理的 meta GUID 全部保持；本地新增脚本的 meta 直接随目录迁移。
- 3 个编辑器窗口的按钮、字段、提示及菜单子项统一使用中文。直接 HTML 默认保留源文件中的项目和暂存配置，仅在“覆盖源配置”启用后应用窗口覆盖值；默认要求工作流审批。
- 塔吊 HTML、设计工作区、展示截图和 Unity 专用素材从项目移除。68 个剩余素材文件无外部序列化引用，连同目录 meta 共 69 个文件归档后删除；旧包名的本地发布包也归档到工程外。
- 中英文 README、安装说明、Example/Workspaces 文档、Skill 契约、工程说明、贡献指南和更新记录同步；全局已安装 Skill 与仓库版本一致。历史验证结果明确保留发生时的工程与验证范围。

移除内容的本机归档位于 `E:/html_to_unity_retired_20261024/`，不参与 GitHub 展示或发布。现行规则见 [目录与生成规则](../../docs/workspace-layout.md)。

## 验证

| 检查 | 结果 |
| --- | --- |
| Python 工作流回归 | 31 项通过，含 Panels 项目根与重复初始化保护 |
| 目录生成、准备契约、安装器 | 分别 6、8、3 项通过；PNG 元数据拒绝测试使用独立合成素材 |
| 源包及文档 | 3 个 Example 与 6 个星港包通过；HTML/准备包路径一致，所有维护文档本地链接有效 |
| 浏览器交互 | 星港 14 项、Inventory/Settings 9 项通过；6 张默认截图与原验收图 RGB 逐像素一致 |
| Example 实际工程交付 | 对当前 UnityProject 运行专用校验：正式/暂存目录、4 个产物指纹、默认字体、原生组件与独立 Item 全通过 |
| C# 编译 | 使用当前 UnityProject 实际编译器、程序集引用及响应文件编译最终包源码，零错误；输出保存在工程外 Audit/Runs 的忽略目录 |
| UPM 打包 | 新包名打包并逐文件字节核对通过 |
| Skill 安装 | 全局副本检查通过，无差异或陈旧受管文件 |

本次运行证据位于忽略目录 `Audit/Runs/latest/`，包括 `layout-browser/`、`directory-migration-verification.json` 与 `csharp/verification.json`。Example 的已有批准与交付指纹未改写。星港仅迁移定位元数据，设计图片、素材和用户批准原文保持，验收指纹记录元数据变化。

## 验证范围

Unity 已解析新包，Editor 日志记录了一次编译和域重载；最终追加的窗口配置修复另用当前工程编译参数完成编译检查。未在本次运行 Editor 发布回归、重新导入星港面板、完整 Unity 视觉或设备验收。Computer Use 捕获窗口连续两次超时（`FrameArrived timed out`、`window capture timed out`），因此不把源码与编译检查表述成实际窗口截图验收。

本报告和 GitHub 文档源均已在工作区更新；本次没有创建 Git 提交或推送远端。

本报告记录较早的目录迁移阶段。后续按用户要求删除旧 Example 三面板并改用星港完整示例，当前交付与验证见 [工作流检查报告](WORKFLOW_REVIEW_20261024.md)。
