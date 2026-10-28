# Tools

从仓库根目录运行，要求 Python 3.10+。

| 工具 | 用途 |
| --- | --- |
| `game-workflow.py` | 游戏初始化、契约集中诊断、分范围验收状态、输入基线与只读影响计划；参数见 `--help` |
| `install-global-skills.py` | 安装 Skill；支持 `--destination` / `--check` |
| `verify-directory-generator.py` | 初始化、覆盖保护和审批状态回归 |
| `verify-preparation-contract.py` | 无效元素、模板、路径与素材的拒绝检查 |
| `verify-installer.py` | 安装器在临时目录的清理与用户文件保护 |
| `verify-workflow-files.py` | 星港六面板、新增游戏的路径与就绪记录、维护文档（含 Tools、Workspaces、审核报告及工程流程文档）内链与通用包边界 |
| `verify-starport-panels.py` | 星港六页、四个 Item、正式/暂存目录、共享图、PICOSans 字体/材质、允许的通用组件、报告及产物指纹、旧 Example 残留；支持 `--unity-project` |
| `verify-file-integrity.py` | JSON / Python / PNG 格式和文件哈希；需要 Pillow |
| `verify-global-install.py` | 可选：已安装全局 Skill 的目录独立性；需先安装 Skill |
| `build-unity-package.py` | 默认将仓库 UPM 源码打包到 `dist/`；支持 `--package` / `--output` |
| `build-workflow.py` | 重建 draw.io 流程图，默认使用仓库内样式，可读取个人样式覆盖 |
| `compare-layout.py` | 转发 Skill 的布局比较 CLI；使用 `--help` 查看参数 |
| `verify-unity-release.py` | 在已配置默认字体、已关闭的 Unity 工程中运行实际 Editor 回归；校验所用包与仓库一致，证据保存于工程外 |

CI 使用 Python 校验、打包工具和星港浏览器交互测试，不依赖本机安装记录或历史 Unity 审核日志。Unity 运行、C# 编译和最终视觉效果仍需在 Editor 中验证。

新增手动 `Unity release gate` 工作流使用已配置的 self-hosted Windows/Unity2022 runner；环境需提供 UNITY_EDITOR_PATH 和 UNITY_VALIDATION_PROJECT。普通 Python CI 不替代该 Editor 验证。交互编辑器可直接调用 UiReleaseValidation.Run(工程外证据目录)，无需关闭工程或写临时 Editor 脚本。

游戏级素材回归已纳入 UiReleaseValidation：检查全游戏同步、组件隔离、Toggle 三图、共享身份分化、重复引用迁移和失败恢复。清单与操作规范见 [游戏素材契约](../docs/project-assets.md)。

游戏级视觉与页面契约、原生装配配置和统一验收见 [契约说明](../Skills/ui-panel-design-to-html/references/game-contracts.md)。例如 `python -B -X utf8 Tools/game-workflow.py status --root Workspaces/StarportSalvage --unity-project UnityProject`。新增原生装配回归同样包含在 UiReleaseValidation 中。

## 浏览器复验

Skill 风格参考的独立检查运行 `npm run verify:skill`，包含交互对比度单测和内置范例颜色对审计；无需浏览器或 Unity。它只检查参考图声明的颜色对，不替代视觉审核、完整无障碍检查或 XR 设备验收。已安装 Skill 可直接运行 `node scripts/test_interaction_contrast.cjs` 及 `node scripts/interaction_contrast.cjs assets/style-presets/examples.json`。

仓库根目录运行 `npm ci`、`npx playwright install chromium` 后，运行 `npm run verify:browser`，检查星港六面板的 16 项流程（含结构化 fixture 契约）。已有 Chrome 或 Edge 时，可设置 `HTML_UI_BROWSER_CHANNEL=chrome` 或 `msedge` 使用现有浏览器。

测试工具自行启动可用端口的本机服务并在结束时关闭；`npm run verify:starport` 是同一入口。已有服务时可用 `STARPORT_BASE_URL` 指定其他端口；`STARPORT_EVIDENCE_DIR` 可指定输出目录，默认写入忽略目录 `Audit/Runs/latest/starport-browser/`，不会覆盖已批准的设计审核证据。`sharp` 仅供历史素材导出工具使用，不要为复验重新导出已批准素材。

`verify-workflow-files.py` 同时检查新增游戏和新增面板的 `Workspaces/{GameName}/Panels/{PanelName}/` 路径与项目配置；星港原有六面板要求 ready，新增草稿只进行结构检查。

`capture-starport-reference.cjs` 从运行的星港 HTTP 预览导出截图、节点矩形、文字、颜色、模板和禁用透明度，供 Unity 使用相同数据做对照。设置 `STARPORT_BASE_URL` 与 `STARPORT_EVIDENCE_DIR` 后运行；它不修改源包或批准图。实际转换规则见 [视觉验证与转换规范](../docs/game-example/conversion-rules.md)。

使用 `node Tools/verify-browser.cjs --capture-reference` 可自动启动并关闭本机服务来导出同一份参考数据。浏览器复验另核对 contracts/fixtures.json 中声明的实际状态预期。

`node Tools/verify-browser.cjs --capture-matrix` 按 screens 契约导出全部 20 个状态，供 Unity `UiReferenceAudit.RunMatrix` 执行三种分辨率的 60 例检查。矩阵证据以 Skill 的 `record_unity_acceptance.py` 统一发布，逐项验证源输入、当前产物、字体依赖和截图尺寸，失败恢复此前记录。

星港 Editor 业务测试入口为菜单“HTML转UGUI → 星港示例 → 运行编辑器业务验收”，输出到 Audit/Runs/latest/starport-editor-flow。完成后保存同版本适配器源码、查看截图，再用 Skill 的 `editor_acceptance.py` 记录；状态门禁增加 `--require-editor`。范围及命令见 [统一验收说明](../Skills/ui-panel-design-to-html/references/game-contracts.md)。
