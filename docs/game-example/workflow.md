# 以星港六面板完成一次 HTML → UGUI 制作

当前示例为星港拾荒者。源包只放在 [游戏工作区](../../Workspaces/StarportSalvage/README.md)，不复制另一套到 Examples。原 Example 三面板、正式和暂存资源、截图及专属工具已移除。当前测试工程为仓库 `UnityProject/`，字体使用已有 `PICOSansVFESC SDF` 及其共享材质。

## 从需求到可验收源包

1. 阅读 [游戏需求](game-requirements.md) 和 [UI 需求](ui-requirements.md)。六页分别负责基地、任务选择、库存整备、设置、确认与结算；数据及操作形成闭环。
2. 确认完整面板预览：画布 1920×1080，普通主体 1680×920，确认弹窗主体 720×520。原图、尺寸和用户确认保留在游戏的 `design-review/` 与各面板 `design/` 下。
3. 确认当前实际需要的九组元素，导出 POT 素材。在游戏根 `project.assets.json` 登记组件类型、部件、状态、源文件哈希和批准版本。同形改色复用中性素材；开关保留 Off/On/Disabled 三张整图。
4. 每个面板使用 `Panels/{PanelName}/inputs、design、html`。HTML、元素清单、Sprite 清单及准备包共同描述原生结构、绑定与 Item。准备包和 HTML 同时设置 `projectName=StarportSalvage`、`projectDirectory=../../..` 和相同暂存根。
5. 浏览器使用固定数据集对照批准图，检查空、单、多条列表、搜索、禁用、加载、失败和确认等状态。真实完成后更新 HTML 验收文件哈希；不能以结构校验代替批准。

```sh
python -m http.server 8148 --bind 127.0.0.1 --directory Workspaces/StarportSalvage
```

打开 [本机入口](http://127.0.0.1:8148/index.html)。自动复验可在仓库根运行 `npm ci`、`npx playwright install chromium`、`npm run verify:browser`，测试工具会自行启动并关闭可用端口的服务。

## 在 Unity 生成与纠正

1. 恢复 TMP Essential Resources，确保现有 `Assets/Art/Fonts/PICOSansVFESC SDF.asset` 为 TMP 默认字体。字体与其已有材质不复制、不新建。
2. 打开 **Tools → HTML to UGUI → 导入整个项目...**，选择 `Workspaces/StarportSalvage`。查看六页与全游戏素材的只读计划，再生成。
3. 共享图片同步至 `Assets/HtmlToUGUIStage/StarportSalvage/Sprites/`；六个主 Prefab、四个 Item 和转换报告按面板分目录。初次生成不能直接视为视觉验收。
4. 按 [转换规范与实际对照](conversion-rules.md) 装配圆角和滑块。当前成果通过已有 `configureRoot/configureTemplate` 导入接入钩子完成原生装配，随后生成真实报告和指纹；没有创建星港视觉脚本。通用开关重复染色问题已修复在转换器中。
5. 在隔离场景载入与浏览器相同的数据集，实例化 Item，统一画布、背景和状态，捕获 Unity 截图。检查文字、素材、层级、输入、开关、滑块和滚动区域；然后复验正式副本。

**重新导入时必须重新应用原生装配。** HTML 不包含浏览器伪元素的 UGUI 实现。普通全量导入会重建滑块默认结构；只设置字体或复制旧截图不能保留本轮装配。具体参数见转换规范。

## 正式复制与验证

在 **面板暂存与复制...** 中勾选星港六页，正式 Prefab 根设置为 `Assets/Art/UIPrefab/StarportSalvage`，Sprite 根设置为 `Assets/Art/UI/Sprites`，保留面板名称子目录。查看复制计划，再复制。

```text
Assets/Art/UIPrefab/StarportSalvage/
├── StarportHomePanel/StarportHomePanel.prefab
├── ExpeditionSelectPanel/ExpeditionSelectPanel.prefab
├── SupplyInventoryPanel/SupplyInventoryPanel.prefab
├── GameSettingsPanel/GameSettingsPanel.prefab
├── ActionConfirmPanel/ActionConfirmPanel.prefab
├── ExpeditionResultPanel/ExpeditionResultPanel.prefab
└── Item/  # MissionEntry、InventoryEntry、LoadoutEntry、RewardEntry
Assets/Art/UI/Sprites/StarportSalvage/         # 24 张共享图片，直接进入组件分类
```

主面板、Item 与图片拥有正式资源身份，引用重定向到正式副本。字体仍引用项目已有资源。转换和复制报告留在暂存区；[交付清单](../../Workspaces/StarportSalvage/unity-delivery.json) 同时记录正式和暂存资源、字体以及来源报告指纹。

```sh
python -B -X utf8 Tools/verify-workflow-files.py
python -B -X utf8 Tools/verify-starport-panels.py --unity-project UnityProject
```

当前交付包含 6 个主面板、4 个 Item、24 张共享 PNG。StageScene 保留六页的示例数据快照供切换预览；快照不实现 Unity 游戏业务。HTML 的完整交互演示、Unity 原生控件验证和正式游戏的导航、数据服务、一次性结算仍是不同层次。当前记录不包含 VR 设备实测。
