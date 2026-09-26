# Tools

从仓库根目录运行，要求 Python 3.10+。

| 工具 | 用途 |
| --- | --- |
| `install-global-skills.py` | 安装 Skill；支持 `--destination` / `--check` |
| `verify-directory-generator.py` | 初始化、覆盖保护和审批状态回归 |
| `verify-preparation-contract.py` | 无效元素、模板、路径与素材的拒绝检查 |
| `verify-installer.py` | 安装器在临时目录的清理与用户文件保护 |
| `verify-workflow-files.py` | 四个示例、文档内链与通用包边界 |
| `verify-file-integrity.py` | JSON / Python / PNG 格式和文件哈希；需要 Pillow |
| `verify-global-install.py` | 可选：已安装全局 Skill 的目录独立性；需先安装 Skill |
| `build-unity-package.py` | 默认将仓库 UPM 源码打包到 `dist/`；支持 `--package` / `--output` |
| `build-workflow.py` | 重建 draw.io 流程图，默认使用仓库内样式，可读取个人样式覆盖 |
| `compare-layout.py` | 转发 Skill 的布局比较 CLI；使用 `--help` 查看参数 |

CI 使用前五个校验工具与打包工具，不依赖本机安装记录或历史 Unity 审核日志。Unity 运行、C# 编译和最终视觉效果仍需在 Editor 中验证。
