# 暂存面板复制到正式目录

默认采用 **复制并保留源文件**。暂存目录是持续生成与预览工作区，正式资源拥有独立身份；重新生成暂存内容不会直接更新正式资源。移动、删除源素材不属于普通复制流程。

## 目录与冲突规则

暂存根 `S` 可以是 `Assets/HtmlToUGUIStage/{PanelName}`，也可以是 `Assets/HtmlToUGUIStage/{ProjectName}/{PanelName}`。

| 来源 | 默认正式目录 |
| --- | --- |
| `S/Prefabs/{PanelName}.prefab` | `Assets/Art/UIPrefab/{PanelName}/{PanelName}.prefab` |
| `S/Prefabs/.../Item_X.prefab` | `Assets/Art/UIPrefab/Item/Item_X.prefab`（两种模式一致） |
| `S/Sprites/{relativePath}.png` | `Assets/Art/UI/Sprites/{PanelName}/{relativePath}.png` |
| `Assets/HtmlToUGUIStage/{ProjectName}/Sprites/{relativePath}.png`（项目共享库） | `Assets/Art/UI/Sprites/{ProjectName}/{relativePath}.png` |
| `Assets/HtmlToUGUIStage/{ProjectName}/Textures/{relativePath}.png`（项目共享库） | `Assets/Art/UI/Sprites/{ProjectName}/Textures/{relativePath}.png` |
| 转换、复制报告 | 暂存目录；正式目录只放运行资源 |

默认勾选“保留面板名称子目录”。取消后，主面板直接复制到 `Assets/Art/UIPrefab/{PanelName}.prefab`。此选项只控制主面板 Prefab 的目录；Item 在两种模式下均统一放入正式 Prefab 根目录下的 `Item/`，按来源是否为主面板识别。图片保持原有映射与 `Sprites/` 内部相对路径。

正式图片根默认是 `Assets/Art/UI/Sprites`。共享 Sprite 去掉暂存桶名称 `Sprites/` 后保留游戏及组件分类，例如 `StarportSalvage/Button/...`；RawImage 的 `Textures/` 分类继续保留，避免与 Sprite 混合。已有正式资源改路径时使用 Unity AssetDatabase 移动并保留 GUID，以迁移报告关联原复制证据，不改写历史报告。

例如选择 `ExpeditionSelectPanel`、`ExpeditionResultPanel`，分别引用 `Item_MissionEntry.prefab`、`Item_RewardEntry.prefab`：

```text
保留面板名称子目录（默认）          不保留面板名称子目录
Assets/Art/UIPrefab/              Assets/Art/UIPrefab/
├── ExpeditionSelectPanel/               ├── ExpeditionSelectPanel.prefab
│   └── ExpeditionSelectPanel.prefab     ├── ExpeditionResultPanel.prefab
├── ExpeditionResultPanel/                └── Item/
│   └── ExpeditionResultPanel.prefab          ├── Item_MissionEntry.prefab
└── Item/                            └── Item_RewardEntry.prefab
    ├── Item_MissionEntry.prefab
    └── Item_RewardEntry.prefab
```

不同项目的同名面板需要配置不同正式根目录，项目名不会自动加入正式路径。批内多个源映射到同一目标时拒绝执行；路径冲突按大小写不敏感检查，不自动追加数字后缀。尤其是不同面板的同名 Item 会在共享 `Item/` 中冲突，须先在源清单中明确命名再生成；开启替换也不能绕过批内冲突。

项目共享库采用表中的独立映射，保留项目及组件分类层级；多面板引用同一共享源时只复制一次。这里“项目名不会自动加入正式路径”指主面板及 Item 的正式路径，不能用共享图片的隔离规则推断同名 Prefab 已经隔离。

- 新目标：`create`，图片使用新 GUID，正式 Prefab 的资源引用指向正式副本。
- 同路径同图：只有文件 SHA256 相同且 `.meta` 除顶层 GUID 外全部一致，才为 `reuse`；保留正式文件、GUID、切片身份及导入设置，不执行写入。此比较刻意保守，不仅比较像素。
- 同路径不同图、导入设置或切片身份不同：`overwrite`，默认阻止。确认是同一用途的新版后逐项授权；其他用途应在源清单中改名后重新生成。不同路径不自动跨面板去重。
- 目标缺少资源或 `.meta` 中任一文件：阻止发布，先修复资源数据库状态，不静默重建身份。
- 覆盖保留正式资源 GUID，但不能保证 Prefab 内部组件引用、事件及业务字段保留；已接业务的面板由项目适配流程更新。

## 编辑器操作

1. 保存版本控制基线，打开 **Tools → HTML to UGUI → 面板暂存与复制...**。
2. 预览并勾选面板，核对正式 Prefab 和图片根目录。勾选“保留面板名称子目录”（默认）或取消勾选以选择平铺模式；窗口同步显示主面板和 Item 路径。修改选项会清除旧计划及覆盖授权，需重新查看计划。处理方式固定为复制、保留源、生成日志；同图复用默认开启，覆盖默认关闭。
3. 点击 **查看已选面板复制计划**。这是只读 Dry Run，不创建目标目录。查看逐项来源、目标、create/reuse/overwrite、覆盖消费者和依赖检查。
4. 必须覆盖可重建的生成资源时，开启替换选项、重新查看计划，逐项勾选允许覆盖。未全部授权时不能执行。执行前还会展示覆盖列表二次确认。
5. 点击 **复制勾选面板到正式目录**。执行重新校验计划；源或目标文件、meta、消费者或被检查图集变化会使计划过期。执行结果与完整记录显示在窗口中。
6. 在正式场景验证布局、SpriteState、动态 Item、事件及加载。提交正式资源和 `.meta`，以及团队要求保留的暂存与证据文件。

日志强制保存在 `Assets/HtmlToUGUIStage/CopyReports/`，包含 `keepPanelNameSubdirectory`、源与目标映射、操作、覆盖前指纹、消费者、待检查项、最终路径/GUID/文件和 meta 哈希及源转换报告哈希。`status=copied` 只表示资源复制与暂存依赖检查完成，不代表业务接入或视觉验收完成。

可捕获的执行失败会回滚本批写入文件及 meta；复用的图片不进入写入事务。不提供断电/崩溃恢复，不代替 Git。当前不自动修改图集、AssetBundle、资源注册表或场景，也没有清空源素材选项。

## 依赖验收清单

- 自动重定向：组件序列化对象引用，包括主 Prefab、Item、Sprite、SpriteState 和模板目录；正式 Prefab 递归依赖不得指向暂存区。暂存中非 PNG/Prefab 资源或跨暂存面板依赖会被拒绝；项目共享 Sprite/Texture 库是明确支持的共享依赖例外。
- 覆盖影响：计划列出常见 Unity 序列化资源（Prefab、场景、asset、材质、动画、Controller、图集）的依赖消费者；字符串路径和自定义文件不在消费者扫描范围内，须手动搜索。
- Sprite：核对类型、切片、local fileID、九宫格、Pivot、PPU、尺寸、平台压缩及覆盖设置。新图复制源导入设置；复用不改设置。
- SpriteAtlas：计划列出收集暂存文件夹/资源或正式目标的 `.spriteatlas`。检查 Packables、Include in Build、平台设置、Variant 和运行加载，再执行 Pack Preview。更改覆盖范围可能引入重复打包；工具不会自动替换暂存 Packable，以免破坏暂存预览。
- 材质与字体：共享项目资源继续复用；检查纹理、Shader、TMP 材质及动画中的间接依赖。不能通过重定向 Prefab 推断任意外部资产已更新。
- AssetBundle：计划列出源图片和目标图片的显式标记。覆盖使用源 meta，因此可能替换目标标记；必须核对正式归属、目录继承及打包脚本。
- Resources 与配置：手动搜索加载路径、JSON、ScriptableObject、面板工厂和注册表。普通对象引用扫描无法更新路径字符串。
- 运行入口：正式场景、嵌套 Prefab、Variant、业务字段、UnityEvent、动态 Item 实例化和异步资源加载均需验证。禁止将暂存场景或暂存资源误收进正式构建。

## 手动处理与移动例外

少量一次性整理、共享素材归并、已接业务面板的精细更新适合手动处理。先记录映射，在 Unity 内操作，按上述清单重绑和检查，审查 Git 差异后提交；不能只复制 Prefab 或在文件管理器中复制带相同 GUID 的 meta。

移动仅适用于明确停止暂存再生成的一次性交接：先查全部消费者及字符串路径，使用 Unity Project 窗口或 `AssetDatabase.MoveAsset` 保留 meta/GUID，同步路径配置并验收。移动会使暂存预览依赖正式资源；再生成可能创建新身份，因此不能作为当前持续暂存工作流的默认方案。磁盘节省需与额外回滚、二次生成和 Git 路径合并成本一起评估。

## Editor API

`PlanCopy(selected, prefabRoot, spriteRoot, replaceExisting=false, reuseIdenticalImages=true, approvedOverwrites=null, keepPanelNameSubdirectory=true)` 生成只读计划。新增可选参数置于末尾，已有调用默认保留面板名称目录。编辑器窗口通过序列化布尔选项保存选择，计划和日志保存相同字段。该字段参与计划签名，执行时用相同模式重新校验；修改计划字段后不能直接执行旧签名。

```csharp
// 保留面板名称子目录（默认）。
var grouped = UiStageWorkspace.PlanCopy(selected);
// 不保留面板名称子目录；Item 仍进入 Assets/Art/UIPrefab/Item/。
var flat = UiStageWorkspace.PlanCopy(selected, keepPanelNameSubdirectory: false);
// 查看 flat.assets 的源/目标映射后执行。
UiStageWorkspace.CopySelected(flat);
```

`approvedOverwrites` 非空时只允许列出的目标被覆盖；传空列表拒绝全部覆盖。为兼容已有调用，`replaceExisting=true` 且 `approvedOverwrites=null` 表示调用者授权本计划所有覆盖；窗口始终传逐项列表。

`CopySelected(reviewedPlan)` 重算计划并校验签名后执行。图集与资源配置自动同步需要项目专属适配器，并把相关配置纳入预览、指纹、备份和回滚后才能接入；当前版本只提供检查与人工接入规范。

星港完整示例使用 `Assets/Art/UIPrefab/StarportSalvage` 作为正式 Prefab 根目录，详见 [制作流程](game-example/workflow.md)。
