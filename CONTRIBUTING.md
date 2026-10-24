# 贡献指南 / Contributing

欢迎提交可复现的问题、文档改进和通用转换器修复。较大的功能改动请先开 Issue 描述输入、预期输出和兼容性。

## 本地验证

使用 Python 3.10 或更新版本，在仓库根目录运行：

```sh
python -m pip install -r requirements-dev.txt
python -B -X utf8 -m unittest discover -s Skills/ui-panel-design-to-html/scripts -p "test*.py"
python -X utf8 Tools/verify-directory-generator.py
python -X utf8 Tools/verify-preparation-contract.py
python -X utf8 Tools/verify-installer.py
python -X utf8 Tools/verify-workflow-files.py
python -X utf8 Tools/verify-file-integrity.py
python -X utf8 Tools/build-unity-package.py
```

这些检查可在全新克隆中运行，不要求安装全局 Skill 或 Unity。Unity C#、Prefab 或素材导入的改动还需在 Unity 2022.3 中检查编译、Console 和导入结果；Python 检查不能替代 Unity 测试。

## 提交约定

- 一个 PR 聚焦一个问题，说明触发条件、行为变化、验证方法和限制。
- 保留已有 `.meta` GUID；不要提交 Library、Temp、凭据或本机日志。
- 修改 `Packages/com.nex.html-to-ugui/` 中的包源码；HTML 和设计材料继续放在工程外。
- 不将业务 Presenter、远程数据接入或示例专属逻辑加入通用转换包。
- 不伪造布局批准、视觉验收或 `ready` 状态。初始化草稿与正式批准状态分别记录；星港现有批准原文和原图不能被回归测试覆盖。
- 新增依赖和素材时记录来源及许可；提交的内容需允许以本仓库 MIT 许可证分发。

Generated UGUI structure and browser preview behavior are separate responsibilities. Please include the smallest prepared XHTML reproducer for importer changes.

当前测试工程、游戏 Panels 源目录和中文编辑器入口请遵守 [目录与生成规则](docs/workspace-layout.md)。修改生成路径时同步初始化器、预览入口、项目根定位、验收指纹和 CI 检查。

浏览器检查先运行 `npm ci`、`npx playwright install chromium`，再运行 `npm run verify:browser`。对已恢复字体和 TMP 资源的实际 Unity 工程，交付前运行 `python -B -X utf8 Tools/verify-starport-panels.py --unity-project UnityProject`；此检查依赖当前交付指纹，不能用新机器上任意重导入的文件冒充原始验收产物。

维护者署名统一为 Nex Xie。历史作者显示名由根目录 `.mailmap` 按邮箱归一，新提交请设置相应的 Git `user.name`；不改写已发布提交对象。

已批准的 `Workspaces/` 源包、`UnityProject/Assets/` 产物及验收证据按 `.gitattributes` 保留原始字节，避免 Git 自动换行转换破坏已登记的 SHA-256。不要为格式清理改写批准原图、原文、Prefab 或 meta；实际修改后应重新验收对应证据。
