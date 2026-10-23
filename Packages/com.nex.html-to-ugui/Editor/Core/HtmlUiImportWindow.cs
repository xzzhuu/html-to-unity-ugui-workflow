#if UNITY_EDITOR
using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace HtmlToUGUI.Editor
{
    public sealed class HtmlUiImportWindow : EditorWindow
    {
        private string html = "", panelName = "Panel", output = "Assets/Art/UIPrefab/{PanelName}", assetPrefix = "Assets/Art/UI/Sprite/{PanelName}/";
        private TMP_FontAsset font;
        private Material fontMaterial;
        private bool allowReplacement;
        private string stagingRoot = UiStageWorkspace.DefaultStagingRoot;
        private string projectName = "", projectDirectory = "";
        private string planText = "";
        private GameObject lastPrefab;
        private string status = "请选择面板源文件；字体默认复用项目 TMP 默认字体。";
        private const string SourceDirectoryKey = "HtmlToUGUI.ExternalSourceDirectory";

        public static string ChooseExternalSource(string title, string extension)
        {
            var directory = EditorPrefs.GetString(SourceDirectoryKey, Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
            if (!Directory.Exists(directory)) directory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var selected = EditorUtility.OpenFilePanel(title, directory, extension);
            if (!string.IsNullOrEmpty(selected)) EditorPrefs.SetString(SourceDirectoryKey, Path.GetDirectoryName(selected));
            return selected;
        }
        [MenuItem("Tools/HTML to UGUI/导入单个面板...")]
        public static void Open() { GetWindow<HtmlUiImportWindow>("HTML 面板导入"); }
        private enum SourceMode { Package, PreparedHtml, ManualHtml }
        private SourceMode sourceMode;
        private string packagePath = "";
        private bool requireApproval = true;
        private bool overridePackageSettings;
        private UiImportPlan reviewedPlan;
        private Vector2 scroll;
        private void OnGUI()
        {
            EditorGUI.BeginChangeCheck();
            sourceMode = (SourceMode)EditorGUILayout.Popup("来源类型", (int)sourceMode, new[] { "面板准备包", "已准备的 HTML", "手动配置 HTML" });
            var isPackage = sourceMode == SourceMode.Package;
            if (isPackage) packagePath = EditorGUILayout.TextField("panel.package.json", packagePath);
            else html = EditorGUILayout.TextField("工程外 XHTML", html);
            if (GUILayout.Button("选择源文件..."))
            {
                var selected = ChooseExternalSource("选择工程外面板源文件", isPackage ? "json" : "html");
                if (!string.IsNullOrEmpty(selected)) { if (isPackage) packagePath = selected; else html = selected; planText = ""; }
            }
            projectName = EditorGUILayout.TextField("项目名称（备用）", projectName);
            projectDirectory = EditorGUILayout.TextField("项目源根目录（备用）", projectDirectory);
            if (sourceMode == SourceMode.ManualHtml)
            {
                panelName = EditorGUILayout.TextField("面板名称", panelName);
                output = EditorGUILayout.TextField("输出目录", output);
                assetPrefix = EditorGUILayout.TextField("Unity 素材路径前缀", assetPrefix);
                fontMaterial = EditorGUILayout.ObjectField("已有共享字体材质", fontMaterial, typeof(Material), false) as Material;
            }
            if (!isPackage) font = EditorGUILayout.ObjectField("指定 TMP 字体", font, typeof(TMP_FontAsset), false) as TMP_FontAsset;
            if (sourceMode != SourceMode.ManualHtml) overridePackageSettings = EditorGUILayout.Toggle("覆盖源配置", overridePackageSettings);
            using (new EditorGUI.DisabledScope(sourceMode != SourceMode.ManualHtml && !overridePackageSettings))
            {
            stagingRoot = EditorGUILayout.TextField("暂存根目录", stagingRoot);
            allowReplacement = EditorGUILayout.Toggle("替换已生成的预制体", allowReplacement);
            requireApproval = EditorGUILayout.Toggle("要求工作流审批", requireApproval);
            }
            if (EditorGUI.EndChangeCheck()) { reviewedPlan = null; planText = ""; }
            EditorGUILayout.HelpBox("在准备包、HTML 或备用字段中设置项目名称与源根目录。源根目录包含 project.assets.json，面板源包集中放在 Panels/{PanelName}/。每次生成先同步整个项目的已确认素材，再生成所选面板。请先查看共享素材与预制体计划。", MessageType.Info);
            if (GUILayout.Button("打开面板暂存与复制窗口")) UiStageWindow.Open();
            if (GUILayout.Button("导入整个项目（扫描多个面板）...")) UiProjectImportWindow.Open();
            if (GUILayout.Button("查看实际导入计划（只读）")) InspectPlan();
            if (!string.IsNullOrEmpty(planText))
            {
                scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.MaxHeight(220));
                EditorGUILayout.TextArea(planText);
                EditorGUILayout.EndScrollView();
            }
            if (GUILayout.Button("校验并生成暂存资源"))
            {
                try
                {
                    var request = ResolveRequest();
                    var plan = HtmlUiImporter.PlanImport(request.htmlPath, request.outputFolder, request.options);
                    planText = Newtonsoft.Json.JsonConvert.SerializeObject(plan, Newtonsoft.Json.Formatting.Indented);
                    if (plan.assets.Exists(a => a.action != "create") && reviewedPlan == null)
                        throw new InvalidOperationException("修改已有资源前，请先查看实际导入计划。");
                    var path = HtmlUiImporter.Import(request.htmlPath, request.outputFolder, request.options, reviewedPlan ?? plan);
                    reviewedPlan = null;
                    lastPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    status = "暂存资源已保存：" + path;
                    if (path.StartsWith(UiStageWorkspace.Root + "/", StringComparison.Ordinal))
                    {
                        try { UiStageWorkspace.RebuildScene(path); status += "\n暂存预览场景已更新。可在面板暂存与复制窗口预览或复制所选面板。"; }
                        catch (Exception preview) { status += "\n资源生成成功，预览更新待完成：" + preview.Message; }
                    }
                }
                catch (Exception ex) { status = ex.Message; Debug.LogException(ex); }
            }
            using (new EditorGUI.DisabledScope(lastPrefab == null))
            {
                if (GUILayout.Button("在暂存场景中预览"))
                {
                    try { UiStageWorkspace.Preview(AssetDatabase.GetAssetPath(lastPrefab)); status = "正在暂存场景中预览。"; }
                    catch (Exception ex) { status = ex.Message; }
                }
                if (GUILayout.Button("捕获独立预览并检查组件"))
                {
                    var folder = EditorUtility.OpenFolderPanel("选择检查证据目录", Application.dataPath, "");
                    if (!string.IsNullOrEmpty(folder))
                    {
                        var report = UiVisualAudit.Capture(lastPrefab, folder);
                        status = "已捕获预览，发现 " + report.issues.Count + " 项问题；视觉对照仍待完成。";
                    }
                }
            }
            EditorGUILayout.HelpBox(status, MessageType.None);
        }
        private UiImportRequest ResolveRequest()
        {
            UiImportRequest request;
            if (sourceMode == SourceMode.Package) request = HtmlUiImporter.ReadPackage(packagePath);
            else if (sourceMode == SourceMode.PreparedHtml) request = HtmlUiImporter.ReadPreparedHtml(html, font);
            else request = new UiImportRequest { htmlPath = html, outputFolder = output,
                options = new HtmlUiImporter.ImportOptions { panelName = panelName, assetPrefix = assetPrefix, font = font, fontMaterial = fontMaterial,
                    projectName = projectName, projectDirectory = string.IsNullOrWhiteSpace(projectDirectory) ? null : projectDirectory } };
            if (sourceMode == SourceMode.PreparedHtml) request.options.requireSkillApproval = true;
            if (sourceMode == SourceMode.ManualHtml || overridePackageSettings)
            {
                request.options.stagingRoot = stagingRoot;
                request.options.allowPrefabReplacement = allowReplacement;
                request.options.requireSkillApproval = requireApproval;
            }
            if (string.IsNullOrWhiteSpace(request.options.stagingRoot)) request.options.stagingRoot = UiStageWorkspace.DefaultStagingRoot;
            if (string.IsNullOrWhiteSpace(request.options.projectName)) request.options.projectName = projectName;
            if (string.IsNullOrWhiteSpace(request.options.projectDirectory) && !string.IsNullOrWhiteSpace(projectDirectory)) request.options.projectDirectory = projectDirectory;
            if (string.IsNullOrWhiteSpace(request.options.projectName) && (request.options.requireSkillApproval || !string.IsNullOrWhiteSpace(request.options.projectDirectory)))
                throw new InvalidDataException("正式工作流或已设置项目源根目录时，需要填写项目名称。未批准的独立技术示例可将两项留空。");
            return request;
        }
        private void InspectPlan()
        {
            try
            {
                var request = ResolveRequest();
                reviewedPlan = HtmlUiImporter.PlanImport(request.htmlPath, request.outputFolder, request.options);
                planText = Newtonsoft.Json.JsonConvert.SerializeObject(reviewedPlan, Newtonsoft.Json.Formatting.Indented);
                status = "只读计划已生成：请在导入前核对目标目录、覆盖操作、引用方与警告。";
            }
            catch (Exception ex) { status = ex.Message; }
        }
        public static GameObject CreatePreview(GameObject prefab)
        {
            var instance = UiPanelScenePlacement.Place(AssetDatabase.GetAssetPath(prefab));
            return instance.GetComponentInParent<Canvas>().gameObject;
        }
    }
}
#endif
