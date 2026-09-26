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
    }
    public static partial class HtmlUiImporter
    {
        public static string ImportPreparedHtml(string htmlPath, TMP_FontAsset font = null)
        {
            UiPanelScenePlacement.ValidateContext();
            var document = System.Xml.Linq.XDocument.Load(htmlPath);
            var root = System.Linq.Enumerable.Single(document.Descendants(), e => (string)e.Attribute("data-root") == "true");
            var name = (string)root.Attribute("data-panel-name") ?? (string)root.Attribute("id") ?? Path.GetFileNameWithoutExtension(htmlPath);
            var prefix = (string)root.Attribute("data-asset-prefix") ?? "Assets/Art/UI/Sprite/" + name + "/";
            var output = (string)root.Attribute("data-output-folder") ?? "Assets/Art/UIPrefab/" + name;
            var fontPath = (string)root.Attribute("data-font-asset");
            if (font == null && !string.IsNullOrEmpty(fontPath))
            {
                font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fontPath);
                if (font == null) throw new InvalidDataException("The explicit HTML data-font-asset does not exist: " + fontPath);
            }
            if (font == null) font = TMP_Settings.defaultFontAsset;
            if (font == null) throw new InvalidDataException("Set the project's TMP default font or select a font in the import window.");
            var path = Import(htmlPath, output, new ImportOptions { panelName = name, assetPrefix = prefix, font = font });
            UiPanelScenePlacement.Place(path);
            return path;
        }

        public static string ImportPackage(string packagePath)
        {
            UiPanelScenePlacement.ValidateContext();
            var package = UnityEngine.JsonUtility.FromJson<UiPanelPackage>(File.ReadAllText(packagePath));
            if (package == null || package.schemaVersion != 1) throw new InvalidDataException("Unsupported package schemaVersion.");
            if (string.IsNullOrWhiteSpace(package.html) || Path.IsPathRooted(package.html) || package.html.Replace('\\', '/').Split('/').ContainsDotDot())
                throw new InvalidDataException("Package html must be relative and inside the package directory.");
            var font = string.IsNullOrWhiteSpace(package.fontAsset) ? TMP_Settings.defaultFontAsset : AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(package.fontAsset);
            if (font == null) throw new InvalidDataException("Set a TMP default font, or provide an existing package fontAsset in the target Unity project.");
            var path = Import(Path.Combine(Path.GetDirectoryName(Path.GetFullPath(packagePath)), package.html), package.outputFolder,
                new ImportOptions { panelName = package.panelName, assetPrefix = package.assetPrefix, font = font, boldWeight = package.boldWeight });
            UiPanelScenePlacement.Place(path);
            return path;
        }
        private static bool ContainsDotDot(this string[] segments) { return Array.IndexOf(segments, "..") >= 0; }
    }
}
#endif
