# HTML to Unity UGUI Workflow

[简体中文](README.md) | [English](README.en.md)

[![Verify](https://github.com/xzzhuu/html-to-unity-ugui-workflow/actions/workflows/verify.yml/badge.svg)](https://github.com/xzzhuu/html-to-unity-ugui-workflow/actions/workflows/verify.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

从 UI 需求与布局审核，到可预览 XHTML，再到 Unity 原生 UGUI / TextMesh Pro 的可复用工作流。

本仓库包含一个 Codex Skill、星港六面板的完整制作示例、Python 工具，以及 `com.nex.html-to-ugui` 0.4.1 的完整 UPM 源码。设计源文件留在 Unity 工程外；转换器生成图片、Panel / Item Prefab 和通用绑定元数据，业务数据与交互由接入应用实现。

完整英文说明见 [English README](README.en.md)。

## 功能与边界

- 初始化标准 HTML UI 目录，重复调用保留现有内容。
- 记录元素用途、语义名称、父级、原生组件、绑定与动态 Item 模板。
- 校验 XHTML、素材、清单、审批记录和就绪状态。
- 导入准备好的 XHTML 和 PNG 为原生 UGUI/TMP Panel 与 Item Prefab。
- 选择项目根目录扫描并批量导入多个面板，暂存到 `Assets/HtmlToUGUIStage/{项目名}/{面板名}/`；预览按项目分组。
- 提供浏览器预览与结构验证；转换器支持有限 CSS 子集，不能保证任意网页自动复刻。

星港示例贯穿需求、面板预览确认、九组元素确认、素材导出、六套 HTML、Unity 暂存和正式复制。结构检查、浏览器交互、Unity 视觉与设备验收分别记录；浏览器 JavaScript 不会转换成 Unity 业务逻辑。

## 星港制作流程示例

从 [星港完整制作流程](docs/game-example/workflow.md) 开始，查看需求、用户批准的原图与素材、HTML、Unity 面板和复验命令。示例包含基地、远征选择、仓库整备、设置、确认弹窗和结算六页，统一使用 1920 × 1080 画布。

| 星港基地 | 仓库整备 | 游戏设置 |
| --- | --- | --- |
| ![星港基地](docs/screenshots/starport-home.png) | ![仓库整备](docs/screenshots/starport-inventory.png) | ![游戏设置](docs/screenshots/starport-settings.png) |

以上是实际 HTML 浏览器截图；Unity 实际对照与修正见 [视觉验证与转换规范](docs/game-example/conversion-rules.md)。

## 快速开始

要求：Python **3.10+**；Unity 导入要求 **2022.3**、UGUI 1.0.0、TMP 3.0.7。浏览器打开示例不要求 Unity。

```sh
git clone https://github.com/xzzhuu/html-to-unity-ugui-workflow.git
cd html-to-unity-ugui-workflow
python -m http.server 8148 --bind 127.0.0.1 --directory Workspaces/StarportSalvage
```

在浏览器打开 [星港演示入口](http://127.0.0.1:8148/index.html)。新增游戏按 [工作区目录规则](docs/workspace-layout.md) 初始化；新目录始终为未审核草稿。

### 安装 Codex Skill

可选择以下任一方式。

**方式一：npx 安装（无需手动克隆仓库）**

需要 Node.js / npm 和 Git，使用 [Skills CLI](https://github.com/vercel-labs/skills)：

```sh
npx skills add https://github.com/xzzhuu/html-to-unity-ugui-workflow/tree/main/Skills/ui-panel-design-to-html --agent codex --global --copy
```

`--agent codex` 指定 Codex，`--global` 安装到用户级 Skill 目录，`--copy` 复制完整文件以避免符号链接要求。目录和更新由 Skills CLI 管理；此方式只安装 Skill，不安装 Unity 包。Skill 中的 Python 工具仍需要 Python 3.10+。

**方式二：克隆后使用仓库安装脚本**

在本仓库根目录运行：

```sh
python -X utf8 Tools/install-global-skills.py
```

默认安装到 `~/.codex/skills`，支持 `CODEX_HOME` 和 `--destination`。安装器更新自己管理的文件并保留未知用户文件；会清理此前由它管理的旧工作流 Skill。

在新的 Codex 任务中调用：

```text
$ui-panel-design-to-html 我以“新文件目录”的身份调用，请初始化当前目录。
```

正式设计可调用：`$ui-panel-design-to-html 请根据需求和参考图制作 UI Panel，先给出布局图与完整性清单供我审核。`

### 安装 Unity 转换包

在 Package Manager 使用 **Add package from disk**，选择本仓库 `Packages/com.nex.html-to-ugui/package.json`。

或使用 **Add package from git URL**：

```text
https://github.com/xzzhuu/html-to-unity-ugui-workflow.git?path=/Packages/com.nex.html-to-ugui
```

导入 TMP Essential Resources 并选择包含所需字符的 TMP 字体，然后用 **Tools → HTML to UGUI → 导入单个面板...** 选择工程外准备好的 HTML。

默认生成到 `Assets/HtmlToUGUIStage/{PanelName}/`，在 `Assets/HtmlToUGUIStage/StageScene.unity` 预览。使用 **Tools → HTML to UGUI → 面板暂存与复制...** 逐个预览、勾选需要的面板、查看复制计划，再复制到正式目录。默认勾选“保留面板名称子目录”，主 Prefab 位于 `Assets/Art/UIPrefab/{PanelName}/{PanelName}.prefab`；取消后直接放在 `Assets/Art/UIPrefab/{PanelName}.prefab`。两种模式的 Item 均统一进入 `Assets/Art/UIPrefab/Item/`；图片进入 `Assets/Art/UI/Sprites/{PanelName}/`，报告留在暂存区。本工程字体使用 `PICOSansVFESC SDF` 及其已有共享材质。星港正式根目录指定为 `Assets/Art/UIPrefab/StarportSalvage`。目录示例与 API 见 [正式复制说明](docs/stage-promotion.md)，详细输入契约与限制见 [包说明](Packages/com.nex.html-to-ugui/README.md)。

## 测试工程与游戏工作区

打开克隆目录中的 `UnityProject/`，使用 Unity 2022.3.62f3c1。工程通过相对路径引用仓库内 UPM 包；换机器后按[工程说明](UnityProject/README.md)恢复字体和 TMP 必需资源，并调整可选的 Unity MCP 本机依赖。

[星港拾荒者工作区](Workspaces/StarportSalvage/README.md)的全部 HTML 面板源包位于 `Workspaces/StarportSalvage/Panels/{PanelName}/`。`project.assets.json`、共享素材、设计审核与工具保留在游戏根目录。批量导入选择 `Workspaces/StarportSalvage`，单面板准备包的 `projectDirectory` 为 `../../..`。项目根已有素材清单时，初始化器会自动识别 `Panels/` 并生成相同的项目路径与暂存配置。窗口按钮和菜单子项统一使用中文。

## 仓库结构

多个面板可使用 **Tools → HTML to UGUI → 导入整个项目...**：选项目目录 → 勾选面板 → 查看汇总计划 → 批量生成。项目目录名可编辑，默认使用所选源目录名称。单面板导入窗口继续可用；旧的平铺暂存内容保留。批量任务逐面板提交，遇到失败或取消时保留成功项并跳过后续项。详细说明见[包说明](Packages/com.nex.html-to-ugui/README.md#import-a-project-containing-multiple-panels)。

| 目录 | 用途 |
| --- | --- |
| [Skills/ui-panel-design-to-html](Skills/ui-panel-design-to-html/SKILL.md) | 唯一工作流 Skill、契约、脚本与可编辑流程图 |
| [Packages/com.nex.html-to-ugui](Packages/com.nex.html-to-ugui/README.md) | Unity Editor 转换器与通用 Runtime 元数据 |
| [Tools](Tools/README.md) | 安装、校验、布局比较与打包工具 |
| [Workspaces](Workspaces/README.md) | 游戏级共享素材、Panels 源包与审核证据 |
| [UnityProject](UnityProject/README.md) | Unity 2022.3 示例工程；字体资源需自行恢复 |
| [Audit](Audit/README.md) | 公开分发说明；机器运行结果不进入 Git |

## 验证与打包

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

包文件和逐文件校验报告写入 `dist/`，运行记录写入 `Audit/Runs/latest/`。GitHub Actions 在 Windows / Ubuntu 与 Python 3.10 / 3.12 上执行这些检查并保存包文件；独立浏览器任务通过 `npm ci`、`npx playwright install chromium` 和 `npm run verify:browser` 验证星港交互。CI **不包含 Unity Editor 编译或视觉验收**。恢复实际 Unity 工程资源后，运行 `python -B -X utf8 Tools/verify-starport-panels.py --unity-project UnityProject` 检查六面板、Item、图片、字体及交付指纹。

## 参与与许可

请阅读 [贡献指南](CONTRIBUTING.md)、[行为准则](CODE_OF_CONDUCT.md)、[安全策略](SECURITY.md) 和 [更新记录](CHANGELOG.md)。错误与功能建议可通过 GitHub Issues 提交。

本项目采用 [MIT 许可证](LICENSE)。第三方依赖、字体与 Unity 软件适用各自许可，见 [第三方说明](THIRD_PARTY_NOTICES.md)。

项目作者：**Nex Xie**。历史提交显示名通过 `.mailmap` 按作者邮箱统一；历史提交对象及 SHA 保留。

游戏项目的共享素材库、组件部件拆分、Toggle 三图、单面板/全项目同步去重及正式复制见 [游戏素材契约](docs/project-assets.md)。
