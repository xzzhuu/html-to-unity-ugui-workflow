#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

namespace HtmlToUGUI.Editor
{
    public sealed class UiStageWindow : EditorWindow
    {
        [SerializeField] private string prefabRoot = "Assets/Art/UIPrefab";
        [SerializeField] private string spriteRoot = "Assets/Art/UI/Sprite";
        [SerializeField] private bool replaceExisting;
        private readonly HashSet<string> selected = new HashSet<string>();
        private List<UiStagedPanel> panels = new List<UiStagedPanel>();
        private UiStageCopyPlan plan;
        private Vector2 scroll;
        private string status = "勾选需要复制的面板；预览与复制独立操作。";
        private string planText = "";

        [MenuItem("Tools/HTML to UGUI/Stage Panels...")]
        public static void Open() { GetWindow<UiStageWindow>("面板暂存与复制"); }

        private void OnEnable()
        {
            minSize = new Vector2(480, 420);
            RefreshPanels();
            EditorApplication.projectChanged += RefreshPanels;
        }
        private void OnDisable() { EditorApplication.projectChanged -= RefreshPanels; }
        private void RefreshPanels()
        {
            panels = UiStageWorkspace.FindPanels();
            selected.RemoveWhere(p => !panels.Any(panel => panel.prefabPath == p));
            plan = null; planText = "";
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
                    if (GUILayout.Button("重建 StageScene")) Run(() => { UiStageWorkspace.RebuildScene(); status = "StageScene 已更新。"; });
                }
                EditorGUI.BeginChangeCheck();
                prefabRoot = EditorGUILayout.TextField("正式 Prefab 目录", prefabRoot);
                spriteRoot = EditorGUILayout.TextField("正式图片目录", spriteRoot);
                replaceExisting = EditorGUILayout.Toggle("替换已有生成资源", replaceExisting);
                if (EditorGUI.EndChangeCheck()) { plan = null; planText = ""; }
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
                        var next = EditorGUILayout.ToggleLeft(panel.panelName, enabled);
                        if (next != enabled)
                        {
                            if (next) selected.Add(panel.prefabPath); else selected.Remove(panel.prefabPath);
                            plan = null; planText = "";
                        }
                        if (GUILayout.Button("预览", GUILayout.Width(56))) Run(() => UiStageWorkspace.Preview(panel.panelName));
                        if (GUILayout.Button("定位", GUILayout.Width(56))) EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<GameObject>(panel.prefabPath));
                    }
                    EditorGUILayout.LabelField(panel.prefabPath, EditorStyles.wordWrappedMiniLabel);
                }
                if (!string.IsNullOrEmpty(planText)) EditorGUILayout.TextArea(planText);
                EditorGUILayout.EndScrollView();
                using (new EditorGUI.DisabledScope(selected.Count == 0))
                {
                    if (GUILayout.Button("查看已选面板复制计划")) Run(() =>
                    {
                        plan = UiStageWorkspace.PlanCopy(selected, prefabRoot, spriteRoot, replaceExisting);
                        planText = JsonConvert.SerializeObject(plan, Formatting.Indented);
                        status = "已选择 " + plan.panels.Count + " 个面板，共 " + plan.assets.Count + " 个运行资源。";
                    });
                }
                using (new EditorGUI.DisabledScope(plan == null || (!replaceExisting && plan.assets.Any(a => a.action == "overwrite"))))
                {
                    if (GUILayout.Button("复制勾选面板到正式目录")) Run(() =>
                    {
                        var report = UiStageWorkspace.CopySelected(plan);
                        status = "复制完成；暂存内容已保留。记录：" + report;
                        plan = null; planText = "";
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
