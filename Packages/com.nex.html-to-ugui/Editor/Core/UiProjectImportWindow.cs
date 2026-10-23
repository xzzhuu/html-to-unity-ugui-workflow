#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

namespace HtmlToUGUI.Editor
{
    public sealed class UiProjectImportWindow : EditorWindow
    {
        private const string DirectoryKey = "HtmlToUGUI.ProjectSourceDirectory";
        private string directory = "", projectName = "", status = "选择项目根目录，扫描并勾选面板准备包。";
        private bool replaceGeneratedPrefabs;
        private UiProjectScan scan;
        private UiProjectImportPlan plan;
        private UiProjectImportResult result;
        private readonly HashSet<string> selected = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private Vector2 scroll;

        [MenuItem("Tools/HTML to UGUI/导入整个项目...")]
        public static void Open() { GetWindow<UiProjectImportWindow>("项目批量导入"); }

        private void Invalidate() { plan = null; result = null; }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("HTML to UGUI · 项目批量导入", EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling))
            {
                EditorGUI.BeginChangeCheck();
                var nextDirectory = EditorGUILayout.TextField("项目源目录", directory);
                if (EditorGUI.EndChangeCheck()) { directory = nextDirectory; scan = null; selected.Clear(); Invalidate(); }
                if (GUILayout.Button("选择项目根目录..."))
                {
                    var chosen = EditorUtility.OpenFolderPanel("选择包含多个面板准备包的项目目录", EditorPrefs.GetString(DirectoryKey, ""), "");
                    if (!string.IsNullOrEmpty(chosen))
                    {
                        directory = chosen; projectName = Path.GetFileName(chosen.TrimEnd('/', '\\'));
                        EditorPrefs.SetString(DirectoryKey, chosen); Scan();
                    }
                }
                EditorGUI.BeginChangeCheck();
                projectName = EditorGUILayout.TextField("项目暂存目录名", projectName);
                replaceGeneratedPrefabs = EditorGUILayout.Toggle("替换已有暂存Prefab", replaceGeneratedPrefabs);
                if (EditorGUI.EndChangeCheck()) Invalidate();
                EditorGUILayout.LabelField("暂存位置", UiStageWorkspace.Root + "/" + projectName + "/{PanelName}/");
                if (GUILayout.Button("扫描 / 刷新面板准备包")) Scan();
                EditorGUILayout.HelpBox("选择包含 project.assets.json 的项目源根目录，面板源包放在 Panels/{PanelName}/ 下。递归扫描 panel.package.json，保留各包的字体与审批设置。执行前校验整个选择，之后逐面板提交；某项失败或取消时停止，已成功项保留，后续项跳过。", MessageType.Info);
                scroll = EditorGUILayout.BeginScrollView(scroll);
                if (scan != null)
                {
                    foreach (var warning in scan.warnings) EditorGUILayout.HelpBox(warning, MessageType.Warning);
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button("全选有效面板")) { selected.Clear(); foreach (var source in scan.sources.Where(s => s.error == null)) selected.Add(source.packagePath); Invalidate(); }
                        if (GUILayout.Button("全不选")) { selected.Clear(); Invalidate(); }
                    }
                    if (scan.sources.Count == 0) EditorGUILayout.HelpBox("没有找到 panel.package.json。请选择包含面板工作区的目录。", MessageType.None);
                    foreach (var source in scan.sources)
                    {
                        using (new EditorGUI.DisabledScope(source.error != null))
                        {
                            var enabled = selected.Contains(source.packagePath);
                            var next = EditorGUILayout.ToggleLeft(source.panelName ?? Path.GetFileName(Path.GetDirectoryName(source.packagePath)), enabled);
                            if (next != enabled) { if (next) selected.Add(source.packagePath); else selected.Remove(source.packagePath); Invalidate(); }
                        }
                        EditorGUILayout.LabelField(source.packagePath, EditorStyles.wordWrappedMiniLabel);
                        if (source.error != null) EditorGUILayout.HelpBox(source.error, MessageType.Error);
                    }
                }
                if (plan != null)
                {
                    foreach (var error in plan.errors) EditorGUILayout.HelpBox(error, MessageType.Error);
                    foreach (var panel in plan.panels)
                    {
                        EditorGUILayout.LabelField(panel.panelName ?? panel.packagePath, EditorStyles.boldLabel);
                        if (panel.error != null) EditorGUILayout.HelpBox(panel.error, MessageType.Error);
                        else
                        {
                            EditorGUILayout.LabelField(panel.import.outputFolder, EditorStyles.wordWrappedMiniLabel);
                            EditorGUILayout.LabelField(string.Join(" / ", panel.import.assets.GroupBy(a => a.action).Select(g => g.Key + ": " + g.Count())));
                            foreach (var warning in panel.import.warnings.Distinct()) EditorGUILayout.HelpBox(warning, MessageType.Warning);
                        }
                    }
                    EditorGUILayout.TextArea(JsonConvert.SerializeObject(plan, Formatting.Indented));
                }
                if (result != null)
                {
                    foreach (var entry in result.panels)
                        EditorGUILayout.HelpBox(entry.panelName + " · " + entry.status + (entry.error == null ? "" : "\n" + entry.error), entry.status == "failed" ? MessageType.Error : MessageType.Info);
                }
                EditorGUILayout.EndScrollView();
                using (new EditorGUI.DisabledScope(scan == null || selected.Count == 0))
                    if (GUILayout.Button("查看已选面板导入计划（只读）"))
                    {
                        try { result = null; plan = UiProjectImport.Plan(directory, projectName, selected, replaceGeneratedPrefabs); status = plan.valid ? "计划已生成，请核对目标路径、资源操作与警告。" : "请先解决计划中的错误。"; }
                        catch (Exception error) { plan = null; status = error.Message; }
                    }
                using (new EditorGUI.DisabledScope(plan == null || !plan.valid))
                    if (GUILayout.Button("批量生成勾选面板到项目暂存区"))
                    {
                        try
                        {
                            result = UiProjectImport.Import(plan, (i, count) => EditorUtility.DisplayCancelableProgressBar("项目批量导入", plan.panels[i].panelName + " (" + (i + 1) + "/" + count + ")", (float)i / count));
                            status = "批次：" + result.status + "；成功 " + result.panels.Count(p => p.status == "imported") + "，失败 " + result.panels.Count(p => p.status == "failed") + "，跳过 " + result.panels.Count(p => p.status == "skipped") + "\n记录：" + result.reportPath;
                            if (result.previewError != null) status += "\n暂存预览待更新：" + result.previewError;
                            if (result.reportError != null) status += "\n批次记录写入失败：" + result.reportError;
                        }
                        catch (Exception error) { status = error.Message; }
                        finally { EditorUtility.ClearProgressBar(); plan = null; }
                    }
                if (GUILayout.Button("打开面板暂存与复制窗口")) UiStageWindow.Open();
            }
            EditorGUILayout.HelpBox(status, MessageType.None);
        }

        private void Scan()
        {
            Invalidate(); selected.Clear();
            try
            {
                scan = UiProjectImport.Scan(directory);
                if (string.IsNullOrWhiteSpace(projectName)) projectName = Path.GetFileName(directory.TrimEnd('/', '\\'));
                foreach (var source in scan.sources.Where(s => s.error == null)) selected.Add(source.packagePath);
                status = "找到 " + scan.sources.Count + " 个准备包，已勾选 " + selected.Count + " 个有效来源。";
            }
            catch (Exception error) { scan = null; status = error.Message; }
        }
    }
}
#endif
