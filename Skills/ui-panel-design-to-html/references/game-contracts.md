# 游戏契约、重导入与统一验收

新增游戏、维护多面板一致性或准备重新导入时读取本说明。游戏根仍为 `Workspaces/{GameName}/`，面板仍在 `Panels/{PanelName}/`，`project.assets.json` 继续管理素材身份。

## 初始化与只读检查

从 Skill 根运行，或在仓库使用 `Tools/game-workflow.py` 同名入口：

```text
python scripts/game_workflow.py init --root <游戏根>
python scripts/init_directory.py --root <游戏根>/Panels/<PanelName>
python scripts/game_workflow.py check --root <游戏根>
python scripts/game_workflow.py status --root <游戏根> --unity-project <Unity工程>
python scripts/game_workflow.py snapshot --root <游戏根> --output <基线.json>
python scripts/game_workflow.py impact --root <游戏根> --baseline <基线.json>
```

初始化保留已有文件，只创建未批准草稿。`check` 汇总字段问题；`status` 按面板区分 passed、failed、not-checked、stale。`snapshot` 记录当前输入指纹，`impact` 输出受影响面板和重验范围，不修改设计批准或自动重建。

## 唯一配置来源

- `contracts/style.json`：画布、语义颜色、字体层级、间距、圆角和预览字体依赖；后续生成器读取此文件。
- `contracts/screens.json`：页面入口、操作绑定、状态、fixture 与测试编号；只登记实际需求。
- `contracts/fixtures.json`：可选的固定验收数据与可观察预期；HTML/Unity 可共享数据，不将其当作游戏服务。
- `contracts/native-assembly.json`：可选的原生组件装配。`draft` 不应用，`active` 启用配置；这两个状态不是设计批准。
- `project.assets.json`：已确认素材、组件归属、三态、九宫格、导入配置与源哈希。

字段形状见 [style Schema](schemas/style.schema.json)、[screens Schema](schemas/screens.schema.json)、[native assembly Schema](schemas/native-assembly.schema.json)。Schema 用于编辑和基础结构提示；路径、批准版本、素材身份和两端实际行为仍由现有语义校验验证。v1 允许附加说明字段，不改变既有字段含义；不兼容变更须增加版本并提供迁移。

## 原生装配

转换器 0.4.1 的项目模式自动读取游戏根 `contracts/native-assembly.json`，将契约加入输入指纹与只读计划。当前提供两项通用能力：

1. `rounding.fromCssRadius=true`：对具有相等四边 border 的 Sliced Image，根据源 CSS 的单一正数圆角设置 PPU multiplier；复杂或不对称九宫格仍需显式适配。
2. `slider`：按配置建立独立 Track、FillArea、HandleArea，使用属于 Slider 的项目素材，以及 style 中的颜色 token。字段为 trackHeight、handleSize、trackRadius、trackAssetId、handleAssetId、trackColorToken、fillColorToken、handleColorToken。

装配只操作新生成的原生对象，先于 configureRoot/configureTemplate 执行。不新增游戏视觉 MonoBehaviour，不修改现有字体/材质。配置改动会使已查看的计划过期。

重导入应验证原生结构、状态和实际图像可重复，以及外部 GUID 保留。Unity 重新序列化可能改变 Prefab 内部 fileID 或文件字节；业务 Prefab 的组件引用与事件保护仍需受控接入，不以 GUID 相同推断内部引用安全。

## 标准验收与范围

`unity-acceptance.json` 继续使用原有批准哈希、截图、referenceImages、importReport、sourceImportReportSha256 和 finalArtifacts 契约。可增加：

- `scope: native-ui`：验收对象是原生 UI 交付。
- `gameContractsSha256`：相对游戏根的契约文件哈希。
- `businessIntegration`、`device`：分别记录实际范围，未测保持 not-checked。
- `testedResolutions`、`testedFixtures`：只登记真正验证过的分辨率和数据。

`unityVerification` 是新增的明确状态；旧 unityAccepted 布尔值继续兼容，不能单独用来区分未测与失败。`--require-unity` 仍严格验证全部必需完成证据。设备验收仍由原有 device-acceptance 契约单独约束。

`UiReferenceAudit.Run(prefabPath, browserReferenceJson, outputDirectory)` 在隔离预览场景填充 fixture，比较完整源节点/动态 Item，并检查原生控件；保留实际 Prefab，不写回 fixture。数据填充不会修改静态源节点的布局。原生 Button/Toggle/Slider 检查不执行已有业务事件；业务状态机、游戏服务和设备效果继续单独验收。

可在 Run 的后两个参数传入目标宽高，检查隔离预览中的等比适配与留边；这不替代项目 UIRoot/设备的运行测试。使用 `scripts/record_unity_acceptance.py --help` 查看标准记录入口。实际查看截图后才使用 `--reviewed-native-ui`，工具会重新验证对照 JSON、截图、参考图、来源报告和正式产物指纹；此标志不产生用户设计批准。

浏览器与 TMP 的字体栅格化允许合理差异。矩形比较、字形检查和原生操作检查不能替代图像评审。新增节点路径、row 与可见性比较时，重复身份和非有限坐标必须失败；双方均隐藏的节点不比较无意义的屏幕几何。

## 完整矩阵与证据重放

0.4.1 的新记录使用 [native acceptance v2 Schema](schemas/native-acceptance.schema.json)，保留旧版已批准记录的兼容读取。`matrixCases` 必须覆盖 screens 中每个已实现页面的全部 fixture 与全部分辨率；逐例关联浏览器参考、实际截图、原生审计与重算的几何结果。记录包含 HTML、全部契约、项目清单、实际源图片、正式产物及外部字体依赖的文件/meta 指纹。截图尺寸和当前源输入也必须一致，过期数据不计入 fixture 覆盖率。

星港的完整运行顺序：

```text
node Tools/verify-browser.cjs --capture-matrix
```

在已连接的 Unity Editor 调用 `UiReferenceAudit.RunMatrix(browserMatrixJson, outputDirectory, formalPrefabRoot)`，等待输出 matrix.json 为 passed。实际查看各状态及分辨率图像后运行：

```text
python scripts/record_unity_acceptance.py --root <游戏根> --unity-project <工程> --browser-reference <浏览器矩阵目录> --evidence <Unity矩阵目录> --reviewed-native-ui
python scripts/game_workflow.py status --root <游戏根> --unity-project <工程> --require-unity
```

记录器先验证全部案例和六页待写内容，全部写入后再验证；任何一步失败会恢复此次发布触及的所有文件。`fixturePresentation` 仅声明业务文字长度、说明高度与比率宽度等通用原生适配规则，不从浏览器矩形反向复制静态布局。长名称可声明 TMP 原生自动字号的最小/最大值；实际字体仍需测试。

## 编辑器业务验收

原生控件审计隔离既有回调。业务流程由接入项目另行测试，放在其 Editor 代码内，使用真实交付 Prefab 和原生控件事件；不要把游戏逻辑放入通用转换包。可使用 `editor_acceptance.py` 记录实际测试报告和逐页截图，它独立绑定当前原生验收、测试适配器源码、业务输入与受保护资源。源码或任何依赖改变即需重验。

```text
python scripts/editor_acceptance.py --root <游戏根> --unity-project <工程> --evidence <业务测试目录> --reviewed-editor-ui
python scripts/game_workflow.py status --root <游戏根> --unity-project <工程> --require-unity --require-editor
```

报告旁需保存测试时的适配器原字节为 `editor-adapter.cs.txt`。报告说明实际时钟、状态存储和服务范围；Editor 内存演示通过不代表已连接生产服务。设备测试继续由 device-acceptance 约束；只有用户明确指定仅编辑器范围时，游戏级 design-review/verification-scope.json 才能记录 target=UnityEditor、deviceRequired=false、requestedBy=user，状态报告据此将未测设备记为 not-applicable，设备通过布尔值仍为 false。
