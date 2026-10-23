# 星港拾荒者 · UI 示例工作区

当前阶段：六面板布局与九组元素已由用户确认；正式PNG、六套HTML与浏览器验收已完成。2026-10-24 已在当前测试工程完成六面板导入、原生装配、实际视觉对照与正式复制；字体使用 PICOSansVFESC SDF。游戏层接线与设备验收仍待进行。

## 目录规则（2026-10-24）

当前 Unity 测试工程为 `E:/html_to_unity_ugui_workflow/UnityProject`。六个面板源工作区统一位于 `Panels/{PanelName}/`，每个目录包含 HTML、输入需求、设计和验收记录。游戏根保留 `project.assets.json`、`shared-assets/`、`shared-art-source/`、`design-review/`、`design-tools/` 和 `index.html`。

新增面板使用 `init_directory.py --root Workspaces/StarportSalvage/Panels/NewPanel`；项目清单存在时自动设置 projectName、projectDirectory（从 html/ 回退三级为 `../../..`）及项目暂存目录。批量导入选择本游戏根目录。Unity 生成路径仍为 `Assets/HtmlToUGUIStage/StarportSalvage/{PanelName}/`。

## 打开演示

本机服务运行时打开[交互演示入口](http://127.0.0.1:8148/index.html)。上方工具栏用于独立页面与数据集检查，不属于游戏UI。

服务停止后，在仓库根目录运行：

```powershell
python -m http.server 8148 --bind 127.0.0.1 --directory Workspaces/StarportSalvage
```

也可运行[start-preview.ps1](start-preview.ps1)。请通过HTTP打开，保证跨页sessionStorage、确认iframe与资源位于同一来源。

“从初始状态开始”使用空携行；“载入整备示例”使用急救包3、电池2、重量7.0kg的批准图对应状态。实际游戏里的重置操作保留确认；工具栏复位仅用于演示和验收。

## 正式HTML入口

- [星港基地](Panels/StarportHomePanel/html/StarportHomePanel.html)
- [远征选择](Panels/ExpeditionSelectPanel/html/ExpeditionSelectPanel.html)
- [仓库整备](Panels/SupplyInventoryPanel/html/SupplyInventoryPanel.html)
- [游戏设置](Panels/GameSettingsPanel/html/GameSettingsPanel.html)
- [操作确认](Panels/ActionConfirmPanel/html/ActionConfirmPanel.html)
- [远征结算](Panels/ExpeditionResultPanel/html/ExpeditionResultPanel.html)

六套准备包共309个原生源/辅助节点，四个Item模板：MissionEntry、InventoryEntry、LoadoutEntry、RewardEntry。确认弹窗复用出发/清空/重置三种模式。每个包都包含本地资源、CSS/JS、elements/sprites/package清单与审批验收证据。

## 验证证据

- [浏览器实际截图总览](design-review/browser/overview.png)
- [14项浏览器验证](design-review/browser/checks.json)
- [六包ready校验结果](design-review/readiness.json)

重新复验前，在仓库根执行 `npm ci`、`npx playwright install chromium`；执行 `npm run verify:starport`，测试工具自行启动和关闭预览服务。新证据默认写入仓库 `Audit/Runs/latest/starport-browser/`，原批准记录保留。支持 `STARPORT_BASE_URL`、`STARPORT_EVIDENCE_DIR` 和 `HTML_UI_BROWSER_CHANNEL` 环境变量，详见 [工具说明](../../Tools/README.md#浏览器复验)。

覆盖真实跨页操作：任务切换、搜索分类交集、补给数量、库存与携行分离、确认取消、3秒执行、结果暂存与领取一次、设置应用/取消、零库存、超重拒绝、空/单/30条列表、长文本、加载/错误/重试和Esc。另检查1280×720及1920×1200等比显示。

成功闭环结果：信用点2600、能源80、矿石28、合金板7、急救包库存3；剩余携行为急救包2、电池2，重量6.0kg。

## 原始批准设计

[九组元素总览](design-review/Img_ElementGroups_Overview_v01.png) · [独立原图与审核说明](design-review/element-groups-review-v01.md) · [素材尺寸与复用计划](design-review/sprite-export-plan-v01.json)

[六面板总览](design-review/Img_StarportSalvage_Overview_v01.png) · [预览索引与文件哈希](design-review/preview-index.json) · [审核检查记录](design-review/review-v01.md)

| 面板 | 原尺寸预览 | 要求 |
| --- | --- | --- |
| 星港基地 | [StarportHomePanel](Panels/StarportHomePanel/design/layout/Img_StarportHomePanel_Preview_v01.png) | 资源、入口、远征状态 |
| 远征选择 | [ExpeditionSelectPanel](Panels/ExpeditionSelectPanel/design/layout/Img_ExpeditionSelectPanel_Preview_v01.png) | 任务列表、详情、出发 |
| 仓库整备 | [SupplyInventoryPanel](Panels/SupplyInventoryPanel/design/layout/Img_SupplyInventoryPanel_Preview_v01.png) | 搜索、物品、数量、携行 |
| 游戏设置 | [GameSettingsPanel](Panels/GameSettingsPanel/design/layout/Img_GameSettingsPanel_Preview_v01.png) | 输入、开关、滑块、画质 |
| 操作确认 | [ActionConfirmPanel](Panels/ActionConfirmPanel/design/layout/Img_ActionConfirmPanel_Preview_v01.png) | 可复用确认弹窗 |
| 远征结算 | [ExpeditionResultPanel](Panels/ExpeditionResultPanel/design/layout/Img_ExpeditionResultPanel_Preview_v01.png) | 消耗、奖励、领取 |

每个面板为独立full准备包。HTML已经实现；预览JS是同一共享演示逻辑的包内副本，不会转换为Unity业务逻辑。

原批准预览为Pillow绘制的静态设计稿；正式素材由独立SVG源生成，没有从整张面板裁片。整套shared-assets含22张POT透明中性PNG，面板包只保留实际引用及动态切换所需子集；同形色差共用素材。SVG原稿和作者工具留在Unity外。

审核图使用纯灰色完整画布，标注尺寸；总览统一缩小为 50%，不裁切弹窗画布。主面板主体1680×920，确认弹窗主体720×520。

当前样本是已整备的出发前状态，不是游戏初始空携行状态。缺省资源与结算口径见各面板 brief 及游戏需求文档。

审核标注位于浏览器预览层，不进入运行时面板节点。动态图片通过透明Group_IconCatalog原生Image注册显式Sprite引用，供转换器收集依赖。原生确认弹窗包含全屏输入遮罩。

已保存真实布局批准、元素批准及HTML验收；designApproved=true、artifactReady=true、unityAccepted=false、deviceAccepted=false。暂存导入成功单独登记，不替代完整Unity验收。

## 项目素材兼容修复（2026-10-23）

项目根目录已补齐 [project.assets.json](project.assets.json)。24个运行素材身份按实际组件、部件及状态区分，六包使用 `asset:ID`；相同类型/部件在游戏级共享。旧浏览器图片继续保留，原审核记录与HTML原样归档于 [迁移记录](design-review/project-migration-v01/)。审批原文沿用真实v01记录，没有新增用户批准。

开关三态整图使用原批准图中已展示的轨道、旋钮位置与配色；滑块明确登记Track、Fill与Handle。六张默认浏览器截图与原验收图逐像素一致，14项浏览器检查及六包ready校验重新通过。

实际生成位置：`Assets/HtmlToUGUIStage/StarportSalvage/`，包含6个主Prefab、4个Item、24张游戏共享PNG。[修复验证报告](../../Audit/Reports/PROJECT_MANIFEST_FIX_20261023.md)记录实际工程、测试和批次结果。原稿导出工具属于历史记录，不要重新执行以覆盖迁移后的工作区。

## Unity接入边界

各package启用requireSkillApproval=true，暂存根为Assets/HtmlToUGUIStage/StarportSalvage/{PanelName}，默认禁止覆盖Prefab。后续通过既有转换器查看只读计划，再暂存导入、装配原生组件、核对截图与引用；正式复制后检查最终资源。

六页实际 Unity 截图已与 HTML 和批准预览对照；修正开关重复染色、圆角比例、滑块几何与遮挡、数学减号缺字。游戏层另行实现数据、导航、列表实例和事件、设置服务及一次性结算。[制作流程](../../docs/game-example/workflow.md) 与 [转换规范及对照图](../../docs/game-example/conversion-rules.md) 记录本次参数、验证和重新导入边界。[unity-delivery.json](unity-delivery.json) 记录正式与暂存产物指纹。

design-tools中的render_previews.py、render_element_sheets.py、prepare_review.py是历史设计阶段工具，勿在批准后重新运行以覆盖冻结记录。正式修改须更新受影响的审批/清单与验收证据。

需求来源：[游戏需求](../../docs/game-example/game-requirements.md)、[UI需求](../../docs/game-example/ui-requirements.md)。

批量导入：Tools → HTML to UGUI → 导入整个项目...，选择本目录，项目暂存目录名使用StarportSalvage，勾选后查看计划再生成。已有旧平铺暂存内容不会自动移动。
