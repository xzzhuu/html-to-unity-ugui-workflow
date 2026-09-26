# 贡献指南 / Contributing

欢迎提交可复现的问题、文档改进和通用转换器修复。较大的功能改动请先开 Issue 描述输入、预期输出和兼容性。

## 本地验证

使用 Python 3.10 或更新版本，在仓库根目录运行：

```sh
python -m pip install -r requirements-dev.txt
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
- 修改 `Packages/com.nexvr.html-to-ugui/` 中的包源码；HTML 和设计材料继续放在工程外。
- 不将业务 Presenter、远程数据接入或示例专属逻辑加入通用转换包。
- 不伪造布局批准、视觉验收或 `ready` 状态。示例的草稿状态属于契约。
- 新增依赖和素材时记录来源及许可；提交的内容需允许以本仓库 MIT 许可证分发。

Generated UGUI structure and browser preview behavior are separate responsibilities. Please include the smallest prepared XHTML reproducer for importer changes.
