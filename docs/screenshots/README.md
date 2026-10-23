# 星港示例截图

截图于 2026-10-24 从真实 Chromium 浏览器捕获，画布均为 1920×1080，使用 Prepared 演示数据；没有生成或修饰界面内容。

| 星港基地 | 仓库整备 | 游戏设置 |
| --- | --- | --- |
| ![星港基地](starport-home.png) | ![仓库整备](starport-inventory.png) | ![游戏设置](starport-settings.png) |

源包位于 [星港工作区](../../Workspaces/StarportSalvage/README.md)。从仓库根运行 `npm ci`、`npx playwright install chromium`、`npm run verify:browser`，可复验六页交互。已有浏览器可设置 `HTML_UI_BROWSER_CHANNEL=chrome` 或 `msedge`。

以上为 HTML 演示截图，Unity 正式面板的实际截图、修正和边界见 [视觉对照与转换规范](../game-example/conversion-rules.md)。完整制作步骤见 [星港流程示例](../game-example/workflow.md)。
