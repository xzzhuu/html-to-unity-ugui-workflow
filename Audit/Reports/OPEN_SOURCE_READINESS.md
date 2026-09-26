# 公开分发准备记录

## 分发内容

仓库纳入 `Packages/com.nexvr.html-to-ugui` 0.2.0 源码，保留原 C# / asmdef 的 `.meta` GUID。Skill、HTML 示例和 Unity 业务边界沿用原契约。

本机绝对包路径替换为仓库相对路径；验证工具不再读取全局 Skill、外部迁移备份或旧 Unity 层级日志。原历史记录仍保留在本机，但从 Git 排除，不视为公开仓库的验证证据。

## 公开仓库支持文件

MIT 许可证、README、贡献指南、行为准则、安全策略、更新记录、第三方说明、Issue/PR 模板、Unity/Python 忽略规则和跨平台 Python CI。

第三方字体、TMP 自带示例、Essential Resources、本机 MCP SDK 和机器运行日志不参与分发。示例工程的字体恢复步骤见 [Unity 工程说明](../../UnityProject/README.md)。

## 验证范围

发布前执行初始化回归、准备契约拒绝检查、安装器回归、四个示例与内链检查、JSON/Python/PNG 完整性检查和 UPM tarball 逐文件校验。还应从 Git 的干净导出运行相同检查，以发现空目录丢失与本机依赖。

这些检查不执行 Unity Editor，不证明 C# 编译、运行时业务接入或最终视觉一致性。TowerCrane 与 Starter 保留原有未批准状态。

## 发布前实测结果（2026-09-26）

本机运行和 Git 干净导出均通过六个验证/打包命令：6 项初始化回归、8 项无效输入拒绝、3 项安装器回归、4 个示例、43 个文档内链、19 个 JSON、15 个 Python 脚本与 46 张 PNG。UPM tarball 含 36 个文件，逐项字节校验通过。Git 导出同时验证了所需空目录的保留。

暂存内容的常见凭据格式与本机绝对路径扫描没有发现命中。此扫描仅覆盖常见模式，不是完整安全审计。GitHub Actions 的跨平台结果以仓库 Actions 页面为准。
