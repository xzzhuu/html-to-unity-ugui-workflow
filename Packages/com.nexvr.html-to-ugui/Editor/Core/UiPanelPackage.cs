#if UNITY_EDITOR
using System;
using System.IO;
using TMPro;
using UnityEditor;

namespace HtmlToUGUI.Editor
{
    [Serializable]
    public sealed class UiPanelPackage
    {
        public int schemaVersion = 1;
        public string panelName;
        public string html;
        public string outputFolder;
        public string assetPrefix;
        public string fontAsset;
        public float boldWeight = .12f;
        public string fontMaterial;
        public string stagingRoot;
        public bool allowPrefabReplacement;
        public bool requireSkillApproval;
    }
    public sealed class UiImportRequest
    {
        public string htmlPath, outputFolder;
        public HtmlUiImporter.ImportOptions options;
    }
    public static partial class HtmlUiImporter
    {
        public static UiImportRequest ReadPreparedHtml(string htmlPath, TMP_FontAsset font = null)
        {
            var document = System.Xml.Linq.XDocument.Load(htmlPath);
            var root = System.Linq.Enumerable.Single(document.Descendants(), e => (string)e.Attribute("data-root") == "true");
            var name = (string)root.Attribute("data-panel-name") ?? (string)root.Attribute("id") ?? Path.GetFileNameWithoutExtension(htmlPath);
            var fontPath = (string)root.Attribute("data-font-asset");
            if (font == null && !string.IsNullOrEmpty(fontPath))
            {
                font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fontPath);
                if (font == null) throw new InvalidDataException("The explicit HTML data-font-asset does not exist: " + fontPath);
            }
            if (font == null) font = TMP_Settings.defaultFontAsset;
            if (font == null) throw new InvalidDataException("Set the project's TMP default font or select a font in the import window.");
            return new UiImportRequest { htmlPath = htmlPath,
                outputFolder = (string)root.Attribute("data-output-folder") ?? "Assets/Art/UIPrefab/" + name,
                options = new ImportOptions { panelName = name, assetPrefix = (string)root.Attribute("data-asset-prefix") ?? "Assets/Art/UI/Sprite/" + name + "/", font = font,
                    stagingRoot = (string)root.Attribute("data-staging-root") ?? UiStageWorkspace.DefaultStagingRoot } };
        }
        public static UiImportRequest ReadPackage(string packagePath)
        {
            var package = UnityEngine.JsonUtility.FromJson<UiPanelPackage>(File.ReadAllText(packagePath));
            if (package == null || package.schemaVersion != 1) throw new InvalidDataException("Unsupported package schemaVersion.");
            if (string.IsNullOrWhiteSpace(package.html) || Path.IsPathRooted(package.html) || package.html.Replace('\\', '/').Split('/').ContainsDotDot())
                throw new InvalidDataException("Package html must be relative and inside the package directory.");
            var font = string.IsNullOrWhiteSpace(package.fontAsset) ? TMP_Settings.defaultFontAsset : AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(package.fontAsset);
            if (font == null) throw new InvalidDataException("Set a TMP default font, or provide an existing package fontAsset in the target Unity project.");
            var htmlPath = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(packagePath)), package.html);
            var material = string.IsNullOrWhiteSpace(package.fontMaterial) ? null : AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(package.fontMaterial);
            if (!string.IsNullOrWhiteSpace(package.fontMaterial) && material == null) throw new InvalidDataException("Shared fontMaterial does not exist: " + package.fontMaterial);
            return new UiImportRequest { htmlPath = htmlPath, outputFolder = package.outputFolder,
                options = new ImportOptions { panelName = package.panelName, assetPrefix = package.assetPrefix, font = font, fontMaterial = material,
                    boldWeight = package.boldWeight, stagingRoot = string.IsNullOrWhiteSpace(package.stagingRoot) ? UiStageWorkspace.DefaultStagingRoot : package.stagingRoot, allowPrefabReplacement = package.allowPrefabReplacement,
                    requireSkillApproval = package.requireSkillApproval } };
        }
        public static string ImportPreparedHtml(string htmlPath, TMP_FontAsset font = null)
        {
            var request = ReadPreparedHtml(htmlPath, font);
            return Import(request.htmlPath, request.outputFolder, request.options);
        }
        public static string ImportPackage(string packagePath)
        {
            var request = ReadPackage(packagePath);
            return Import(request.htmlPath, request.outputFolder, request.options);
        }
        private static bool ContainsDotDot(this string[] segments) { return Array.IndexOf(segments, "..") >= 0; }
    }
}
#endif
