#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace HtmlToUGUI.Editor
{
    public sealed class UiStageWindow : EditorWindow
    {
        [SerializeField] private string prefabRoot = "Assets/Art/UIPrefab";
        [SerializeField] private string spriteRoot = "Assets/Art/UI/Sprites";
        [SerializeField] private bool keepPanelNameSubdirectory = true;
        [SerializeField] private bool replaceExisting;
        [SerializeField] private bool reuseIdenticalImages = true;
        private readonly HashSet<string> approvedOverwrites = new HashSet<string>();
        private readonly HashSet<string> selected = new HashSet<string>();
        private List<UiStagedPanel> panels = new List<UiStagedPanel>();
        private UiStageCopyPlan plan;
        private Vector2 scroll;
        private string status = "勾选需要复制的面板；预览与复制独立操作。";
        private string planText = "";

        [MenuItem("Tools/HTML to UGUI/面板暂存与复制...")]
        public static void Open() { GetWindow<UiStageWindow>("面板暂存与复制"); }

        private void OnEnable()
        {
            if (spriteRoot == "Assets/Art/UI/Sprite") spriteRoot = "Assets/Art/UI/Sprites";
            minSize = new Vector2(480, 420);
            RefreshPanels();
            EditorApplication.projectChanged += RefreshPanels;
        }
        private void OnDisable() { EditorApplication.projectChanged -= RefreshPanels; }
        private void RefreshPanels()
        {
            panels = UiStageWorkspace.FindPanels();
            selected.RemoveWhere(p => !panels.Any(panel => panel.prefabPath == p));
            plan = null; approvedOverwrites.Clear();
            Repaint();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("HtmlToUGUIStage · 暂存、预览、选择性复制", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(UiStageWorkspace.ScenePath, EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.HelpBox("生成资源保留在暂存目录。预览时每次显示一个面板，并沿用各自设计分辨率。勾选后查看复制计划，再复制到正式目录。", MessageType.Info);
            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("刷新列表")) RefreshPanels();
                    if (GUILayout.Button("重建暂存预览场景")) Run(() => { UiStageWorkspace.RebuildScene(); status = "暂存预览场景已更新。"; });
                }
                EditorGUI.BeginChangeCheck();
                prefabRoot = EditorGUILayout.TextField("正式 Prefab 目录", prefabRoot);
                spriteRoot = EditorGUILayout.TextField("正式图片目录", spriteRoot);
                keepPanelNameSubdirectory = EditorGUILayout.Toggle("保留面板名称子目录", keepPanelNameSubdirectory);
                EditorGUILayout.LabelField("面板路径", prefabRoot + (keepPanelNameSubdirectory ? "/{PanelName}/{PanelName}.prefab" : "/{PanelName}.prefab"), EditorStyles.wordWrappedMiniLabel);
                EditorGUILayout.LabelField("Item 路径", prefabRoot + "/Item/{ItemName}.prefab", EditorStyles.wordWrappedMiniLabel);
                EditorGUILayout.LabelField("处理方式", "复制并保留源文件；强制生成处理日志");
                reuseIdenticalImages = EditorGUILayout.Toggle("复用同图及相同导入设置", reuseIdenticalImages);
                replaceExisting = EditorGUILayout.Toggle("替换已有生成资源", replaceExisting);
                if (EditorGUI.EndChangeCheck()) { plan = null; planText = ""; approvedOverwrites.Clear(); }
                EditorGUILayout.HelpBox("图集、AssetBundle 和资源配置仅检查，不自动修改。复制后须完成项目适配与运行验收；不删除暂存素材。", MessageType.Info);
                if (replaceExisting) EditorGUILayout.HelpBox("替换会重建已选生成资源。接入业务的 Prefab 请通过项目适配流程更新；GUID 保留不代表内部组件引用或事件保留。", MessageType.Warning);
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("全选")) { foreach (var panel in panels) selected.Add(panel.prefabPath); plan = null; planText = ""; }
                    if (GUILayout.Button("全不选")) { selected.Clear(); plan = null; planText = ""; }
                }
                scroll = EditorGUILayout.BeginScrollView(scroll);
                if (panels.Count == 0) EditorGUILayout.HelpBox("暂无暂存面板。先使用 Import Panel 生成到 HtmlToUGUIStage。", MessageType.None);
                foreach (var panel in panels)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        var enabled = selected.Contains(panel.prefabPath);
                        var next = EditorGUILayout.ToggleLeft(panel.displayName, enabled);
                        if (next != enabled)
                        {
                            if (next) selected.Add(panel.prefabPath); else selected.Remove(panel.prefabPath);
                            plan = null; planText = "";
                        }
                        if (GUILayout.Button("预览", GUILayout.Width(56))) Run(() => UiStageWorkspace.Preview(panel.prefabPath));
                        if (GUILayout.Button("定位", GUILayout.Width(56))) EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<GameObject>(panel.prefabPath));
                    }
                    EditorGUILayout.LabelField(panel.prefabPath, EditorStyles.wordWrappedMiniLabel);
                }
                if (plan != null)
                {
                    EditorGUILayout.LabelField("操作前差异（Dry Run）", EditorStyles.boldLabel);
                    foreach (var asset in plan.assets)
                    {
                        EditorGUILayout.LabelField(asset.action + "  " + asset.destination, EditorStyles.wordWrappedLabel);
                        EditorGUILayout.LabelField("来源：" + asset.source, EditorStyles.wordWrappedMiniLabel);
                        if (asset.action == "overwrite" && replaceExisting)
                        {
                            var approved = approvedOverwrites.Contains(asset.destination);
                            var next = EditorGUILayout.ToggleLeft("允许覆盖此资源（已接业务的 Prefab 请走项目适配）", approved);
                            if (next != approved)
                            {
                                if (next) approvedOverwrites.Add(asset.destination); else approvedOverwrites.Remove(asset.destination);
                                UiStageWorkspace.SetCopyOverwriteApproval(plan, approvedOverwrites);
                            }
                        }
                        foreach (var consumer in asset.consumers) EditorGUILayout.LabelField("使用者：" + consumer, EditorStyles.wordWrappedMiniLabel);
                    }
                    foreach (var warning in plan.dependencyChecks) EditorGUILayout.HelpBox(warning, MessageType.Warning);
                }
                if (!string.IsNullOrEmpty(planText)) EditorGUILayout.TextArea(planText);
                EditorGUILayout.EndScrollView();
                using (new EditorGUI.DisabledScope(selected.Count == 0))
                {
                    if (GUILayout.Button("查看已选面板复制计划")) Run(() =>
                    {
                        approvedOverwrites.Clear();
                        plan = UiStageWorkspace.PlanCopy(selected, prefabRoot, spriteRoot, replaceExisting, reuseIdenticalImages, approvedOverwrites, keepPanelNameSubdirectory);
                        planText = "";
                        status = "只读计划：新增 " + plan.assets.Count(a => a.action == "create") + "，复用 " + plan.assets.Count(a => a.action == "reuse") +
                            "，覆盖 " + plan.assets.Count(a => a.action == "overwrite") + "。覆盖须逐项勾选授权。";
                    });
                }
                using (new EditorGUI.DisabledScope(plan == null || plan.assets.Any(a => a.action == "overwrite" &&
                    (!replaceExisting || !approvedOverwrites.Contains(a.destination)))))
                {
                    if (GUILayout.Button("复制勾选面板到正式目录")) Run(() =>
                    {
                        var overwrites = plan.assets.Where(a => a.action == "overwrite").ToArray();
                        if (overwrites.Length > 0 && !EditorUtility.DisplayDialog("确认覆盖正式生成资源",
                            "将覆盖以下资源。保留 GUID 不保证业务绑定和事件保留，请确认已保存版本控制基线。\n\n" +
                            string.Join("\n", overwrites.Select(a => a.destination)), "确认覆盖", "取消")) return;
                        var summary = "新增 " + plan.assets.Count(a => a.action == "create") + "，复用 " + plan.assets.Count(a => a.action == "reuse") +
                            "，覆盖 " + overwrites.Length;
                        var report = UiStageWorkspace.CopySelected(plan);
                        status = "复制完成：" + summary + "；暂存内容已保留。依赖重定向检查通过；图集/加载配置及运行验收待项目确认。记录：" + report;
                        planText = System.IO.File.ReadAllText(report);
                        plan = null;
                    });
                }
            }
            EditorGUILayout.HelpBox(status, MessageType.None);
        }

        private void Run(Action action)
        {
            try { action(); }
            catch (Exception error) { status = error.Message; plan = null; Debug.LogException(error); }
        }
    }
}
#endif
