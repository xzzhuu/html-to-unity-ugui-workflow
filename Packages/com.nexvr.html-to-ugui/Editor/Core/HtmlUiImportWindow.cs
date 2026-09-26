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
        private void OnGUI()
        {
            html = EditorGUILayout.TextField("External HTML file", html);
            if (GUILayout.Button("One-click Import panel.package.json"))
            {
                var package = ChooseExternalSource("Choose external UI package", "json");
                if (!string.IsNullOrEmpty(package))
                {
                    try
                    {
                        var path = HtmlUiImporter.ImportPackage(package);
                        lastPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

                        status = path + "\nPrefab saved and instance placed under Canvas. Save the scene to keep scene changes.";
                    }
                    catch (Exception ex) { status = ex.Message; Debug.LogException(ex); }
                }
            }
            if (GUILayout.Button("Choose External HTML..."))
            {
                var selected = ChooseExternalSource("Choose external HTML source", "html");
                if (!string.IsNullOrEmpty(selected)) html = selected;
            }
            if (GUILayout.Button("Import Prepared HTML"))
            {
                try
                {
                    var path = HtmlUiImporter.ImportPreparedHtml(html, font);
                    lastPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

                    status = path + "\nPrefab saved and instance placed under Canvas. Save the scene to keep scene changes.";
                }
                catch (Exception ex) { status = ex.Message; Debug.LogException(ex); }
            }
            panelName = EditorGUILayout.TextField("Panel name", panelName);
            output = EditorGUILayout.TextField("Output folder", output);
            assetPrefix = EditorGUILayout.TextField("Unity asset prefix", assetPrefix);
            font = EditorGUILayout.ObjectField("TMP font", font, typeof(TMP_FontAsset), false) as TMP_FontAsset;
            EditorGUILayout.HelpBox("Keep HTML, CSS and preview scripts outside the Unity project. Import reads the selected source folder and copies only referenced image assets into Assets.", MessageType.Info);
            EditorGUILayout.HelpBox("Fixed pixel design canvas. Import creates native TMP/UGUI components and template prefabs. CSS effects outside the supported subset are reported for review.", MessageType.Info);
            if (GUILayout.Button("Validate and Import"))
            {
                try
                {
                    if (font == null) throw new InvalidDataException("Select a TMP font; do not silently use a Latin font for Chinese UI.");
                    UiPanelScenePlacement.ValidateContext();
                    var path = HtmlUiImporter.Import(html, output, new HtmlUiImporter.ImportOptions { panelName = panelName, assetPrefix = assetPrefix, font = font });
                    lastPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

                    UiPanelScenePlacement.Place(path);
                    status = path + "\nPrefab saved and instance placed under Canvas. Save the scene to keep scene changes.";
                }
                catch (Exception ex) { status = ex.Message; Debug.LogException(ex); }
            }
            using (new EditorGUI.DisabledScope(lastPrefab == null))
            {
                if (GUILayout.Button("Select / Place Panel Under Canvas")) CreatePreview(lastPrefab);
                if (GUILayout.Button("Capture Unity PNG + Component Audit"))
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
        public static GameObject CreatePreview(GameObject prefab)
        {
            var instance = UiPanelScenePlacement.Place(AssetDatabase.GetAssetPath(prefab));
            return instance.GetComponentInParent<Canvas>().gameObject;
        }
    }
}
#endif
