# 星港 UI 转换测试工程

打开克隆目录下的 `UnityProject/`，使用 Unity 2022.3.62f3c1。当前维护者的验证位置为 `E:/html_to_unity_ugui_workflow/UnityProject`，不是其他机器的路径要求。通过 `Packages/manifest.json` 中的相对路径引用仓库 `Packages/com.nex.html-to-ugui`。Unity MCP 是可选本机开发依赖，换机器后需调整或移除其 SDK 路径。

当前示例为星港六面板，原 Example 三面板及相关正式、暂存资源已删除。完整需求、批准、素材、HTML、导入、视觉纠正与正式复制见 [制作流程](../docs/game-example/workflow.md)。

| 位置 | 内容 |
| --- | --- |
| `Assets/HtmlToUGUIStage/StarportSalvage` | 六页暂存主面板、四个 Item、24 张共享 PNG 与转换报告 |
| `Assets/HtmlToUGUIStage/StageScene.unity` | 六页可切换预览，保存固定数据集的演示快照 |
| `Assets/Art/UIPrefab/StarportSalvage` | 六个正式主 Prefab 与 `Item/` 下四个 Item |
| `Assets/Art/UI/Sprites/StarportSalvage` | 正式共享图片，面板引用已重定向 |

字体统一使用现有 `Assets/Art/Fonts/PICOSansVFESC SDF.asset` 及其共享材质，它也是当前 TMP 默认字体。字体、TMP Essential Resources 和厂商示例不随仓库公开分发。新机器需恢复 TMP 必需资源，并自行准备获许可的对应字体；不要直接提交本机字体资源。

通过 **Tools → HTML to UGUI → 导入整个项目...** 选择仓库 `Workspaces/StarportSalvage`。查看只读计划后生成，按 [转换规范](../docs/game-example/conversion-rules.md) 重新应用圆角和 Slider 原生装配，再核验截图与引用后复制到正式目录。普通全量重建不会自动保留这些项目适配。

从仓库根运行：

```sh
python -B -X utf8 Tools/verify-starport-panels.py --unity-project UnityProject
```

[交付清单](../Workspaces/StarportSalvage/unity-delivery.json) 记录正式/暂存资源、字体和来源报告指纹。菜单“HTML转UGUI → 星港示例 → 运行编辑器业务验收”使用实际正式 Prefab、原生事件与内存演示状态，覆盖六页导航、整备、设置、三秒远征和一次性结算。适配器位于 Assets/Editor/StarportSalvage；StageScene 仍保留原快照。用户指定本轮仅验收 Unity 编辑器，结果见 [完成报告](../Audit/Reports/WORKFLOW_COMPLETION_20261024.md)。
