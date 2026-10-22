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
        private string planText = "";
        private GameObject lastPrefab;
        private string status = "Choose XHTML and a TMP font containing your UI characters.";
        private const string SourceDirectoryKey = "HtmlToUGUI.ExternalSourceDirectory";

        public static string ChooseExternalSource(string title, string extension)
        {
            var directory = EditorPrefs.GetString(SourceDirectoryKey, Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
            if (!Directory.Exists(directory)) directory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var selected = EditorUtility.OpenFilePanel(title, directory, extension);
            if (!string.IsNullOrEmpty(selected)) EditorPrefs.SetString(SourceDirectoryKey, Path.GetDirectoryName(selected));
            return selected;
        }
        [MenuItem("Tools/HTML to UGUI/Import Panel...")]
        public static void Open() { GetWindow<HtmlUiImportWindow>("HTML to UGUI (TMP)"); }
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
            sourceMode = (SourceMode)EditorGUILayout.EnumPopup("Source", sourceMode);
            var isPackage = sourceMode == SourceMode.Package;
            if (isPackage) packagePath = EditorGUILayout.TextField("panel.package.json", packagePath);
            else html = EditorGUILayout.TextField("External XHTML", html);
            if (GUILayout.Button("Choose Source..."))
            {
                var selected = ChooseExternalSource("Choose external UI source", isPackage ? "json" : "html");
                if (!string.IsNullOrEmpty(selected)) { if (isPackage) packagePath = selected; else html = selected; planText = ""; }
            }
            if (sourceMode == SourceMode.ManualHtml)
            {
                panelName = EditorGUILayout.TextField("Panel name", panelName);
                output = EditorGUILayout.TextField("Output folder", output);
                assetPrefix = EditorGUILayout.TextField("Unity asset prefix", assetPrefix);
                fontMaterial = EditorGUILayout.ObjectField("Shared font material", fontMaterial, typeof(Material), false) as Material;
            }
            if (!isPackage) font = EditorGUILayout.ObjectField("TMP font override", font, typeof(TMP_FontAsset), false) as TMP_FontAsset;
            if (isPackage) overridePackageSettings = EditorGUILayout.Toggle("Override package settings", overridePackageSettings);
            using (new EditorGUI.DisabledScope(isPackage && !overridePackageSettings))
            {
            stagingRoot = EditorGUILayout.TextField("Staging root", stagingRoot);
            allowReplacement = EditorGUILayout.Toggle("Replace generated prefabs", allowReplacement);
            requireApproval = EditorGUILayout.Toggle("Require Skill approval", requireApproval);
            }
            if (EditorGUI.EndChangeCheck()) { reviewedPlan = null; planText = ""; }
            EditorGUILayout.HelpBox("Generate into Assets/HtmlToUGUIStage, preview in StageScene, then select panels in Stage Panels to copy into formal folders. Package overrides are explicit. Inspect the plan before replacing staged resources.", MessageType.Info);
            if (GUILayout.Button("打开面板暂存与复制窗口")) UiStageWindow.Open();
            if (GUILayout.Button("Inspect Effective Plan (read-only)")) InspectPlan();
            if (!string.IsNullOrEmpty(planText))
            {
                scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.MaxHeight(220));
                EditorGUILayout.TextArea(planText);
                EditorGUILayout.EndScrollView();
            }
            if (GUILayout.Button("Validate and Generate Staged Resources"))
            {
                try
                {
                    var request = ResolveRequest();
                    var plan = HtmlUiImporter.PlanImport(request.htmlPath, request.outputFolder, request.options);
                    planText = Newtonsoft.Json.JsonConvert.SerializeObject(plan, Newtonsoft.Json.Formatting.Indented);
                    if (plan.assets.Exists(a => a.action != "create") && reviewedPlan == null)
                        throw new InvalidOperationException("Inspect the effective plan before changing existing resources.");
                    var path = HtmlUiImporter.Import(request.htmlPath, request.outputFolder, request.options, reviewedPlan ?? plan);
                    reviewedPlan = null;
                    lastPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    status = "Staged resources saved: " + path;
                    if (path.StartsWith(UiStageWorkspace.Root + "/", StringComparison.Ordinal))
                    {
                        try { UiStageWorkspace.RebuildScene(request.options.panelName); status += "\nStageScene updated. Use Stage Panels to preview or copy selected panels."; }
                        catch (Exception preview) { status += "\nResources succeeded; preview update pending: " + preview.Message; }
                    }
                }
                catch (Exception ex) { status = ex.Message; Debug.LogException(ex); }
            }
            using (new EditorGUI.DisabledScope(lastPrefab == null))
            {
                if (GUILayout.Button("在 StageScene 中预览"))
                {
                    try { UiStageWorkspace.Preview(lastPrefab.name); status = "Previewing in StageScene."; }
                    catch (Exception ex) { status = ex.Message; }
                }
                if (GUILayout.Button("Capture Isolated Preview + Component Audit"))
                {
                    var folder = EditorUtility.OpenFolderPanel("Audit evidence folder", Application.dataPath, "");
                    if (!string.IsNullOrEmpty(folder))
                    {
                        var report = UiVisualAudit.Capture(lastPrefab, folder);
                        status = "Captured. Issues: " + report.issues.Count + ". Visual comparison remains pending.";
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
                options = new HtmlUiImporter.ImportOptions { panelName = panelName, assetPrefix = assetPrefix, font = font, fontMaterial = fontMaterial } };
            if (sourceMode != SourceMode.Package || overridePackageSettings)
            {
                request.options.stagingRoot = stagingRoot;
                request.options.allowPrefabReplacement = allowReplacement;
                request.options.requireSkillApproval = requireApproval;
            }
            if (string.IsNullOrWhiteSpace(request.options.stagingRoot)) request.options.stagingRoot = UiStageWorkspace.DefaultStagingRoot;
            return request;
        }
        private void InspectPlan()
        {
            try
            {
                var request = ResolveRequest();
                reviewedPlan = HtmlUiImporter.PlanImport(request.htmlPath, request.outputFolder, request.options);
                planText = Newtonsoft.Json.JsonConvert.SerializeObject(reviewedPlan, Newtonsoft.Json.Formatting.Indented);
                status = "Read-only plan: verify destinations, overwrite actions, consumers and warnings before importing.";
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
