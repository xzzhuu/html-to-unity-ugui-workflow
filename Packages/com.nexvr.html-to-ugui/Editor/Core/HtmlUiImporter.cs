#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using TMPro;
using HtmlToUGUI;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HtmlToUGUI.Editor
{
    /// <summary>
    /// 受约束 HTML/CSS -> Unity UGUI Prefab 导入器。
    ///
    /// 固定像素设计画布，尺寸从根元素读取；不实现浏览器完整 CSS。
    /// 支持：absolute left/top/right/bottom/width/height、颜色、字号、对齐、
    /// Image/Text/Button/RawImage/InputField/ScrollView 以及 data-binding。
    ///
    /// HTML 必须是 XML 可解析的 XHTML 风格。data-binding 只生成通用结构化映射；浏览器逻辑不会转换为 Unity 业务 Presenter。
    /// </summary>
    public static partial class HtmlUiImporter
    {
        private static readonly Dictionary<string, Dictionary<string, string>> CssRules =
            new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

        private static readonly Dictionary<string, string> CssVariables =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        private static readonly List<UiBindingMap.Entry> Bindings =
            new List<UiBindingMap.Entry>();


        private static ImportOptions options;
        private static readonly List<string> CssWarnings = new List<string>();
        public sealed class ImportOptions
        {
            public string panelName = "Panel";
            public string assetPrefix = "Assets/Art/UI/Sprite/{PanelName}/";
            public TMP_FontAsset font;
            public Material fontMaterial;
            public float boldWeight = 0.12f;
            public Action<GameObject, UiBindingMap, Dictionary<string, string>> configureRoot;
            public Action<string, GameObject, UiBindingMap> configureTemplate;
        }

        public static string Import(string htmlPath, string outputFolder, ImportOptions settings = null)
        {
            options = settings ?? new ImportOptions();
            outputFolder = (outputFolder ?? "Assets/Art/UIPrefab/{PanelName}").Replace("{PanelName}", options.panelName);
            options.assetPrefix = (options.assetPrefix ?? "Assets/Art/UI/Sprite/{PanelName}/").Replace("{PanelName}", options.panelName);
            ValidateAssetPath(outputFolder + "/dummy.prefab");
            if (!Regex.IsMatch(options.panelName, @"^(?:Panel|[A-Z][A-Za-z0-9]*Panel)$"))
                throw new InvalidDataException("panelName must use PascalCase and end with Panel (for example InventoryPanel).");
            if (!options.assetPrefix.EndsWith("/")) options.assetPrefix += "/";
            ValidateAssetPath(options.assetPrefix + "dummy.png");
            Bindings.Clear(); CssRules.Clear(); CssVariables.Clear(); OrderedRules.Clear(); CssWarnings.Clear();
            var document = XDocument.Load(htmlPath, LoadOptions.PreserveWhitespace);
            var htmlDir = Path.GetDirectoryName(Path.GetFullPath(htmlPath));
            LoadLinkedCss(document, htmlDir);
            var report = UiPackageValidator.Validate(document, htmlDir, options.assetPrefix, ResolveStyle);
            report.warnings.AddRange(CssWarnings);
            if (document.Descendants().Any(e => new[] { "Text", "Button", "InputField" }.Contains((string)e.Attribute("data-ugui"), StringComparer.OrdinalIgnoreCase)))
            {
                if (options.font == null) options.font = TMP_Settings.defaultFontAsset;
                if (options.font == null) report.errors.Add("A TMP font is required for text-bearing controls.");
            }
            if (report.errors.Count > 0) throw new InvalidDataException(string.Join("\n", report.errors.ToArray()));
            if (report.warnings.Count > 0) Debug.LogWarning("[HtmlUI] " + report.warnings.Count + " review items; see import-report.json.");
            var sourceRoot = document.Descendants().Single(x => (string)x.Attribute("data-root") == "true");
            var declaredName = (string)sourceRoot.Attribute("data-panel-name");
            if (!string.IsNullOrEmpty(declaredName) && declaredName != options.panelName)
                throw new InvalidDataException("HTML data-panel-name differs from the requested panelName.");
            var rootStyle = ResolveStyle(sourceRoot);
            var resolution = new Vector2(ReadFloat(rootStyle, "width", 0), ReadFloat(rootStyle, "height", 0));
            EnsureFolder(outputFolder);
            if (options.font != null && options.fontMaterial == null)
            {
                var materialPath = outputFolder + "/" + options.panelName + ".Font.mat";
                var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                if (material == null)
                {
                    material = new Material(options.font.material);
                    AssetDatabase.CreateAsset(material, materialPath);
                }
                else material.CopyPropertiesFromMaterial(options.font.material);
                if (material.HasProperty("_WeightBold")) material.SetFloat("_WeightBold", options.boldWeight);
                options.fontMaterial = material;
                EditorUtility.SetDirty(material);
            }
            PrepareReferencedAssets(document, htmlDir);
            var itemPrefabPaths = BuildTemplatePrefabs(document, outputFolder);
            Bindings.Clear();
            var rootGo = new GameObject(options.panelName, typeof(RectTransform));
            try
            {
                var rect = rootGo.GetComponent<RectTransform>();
                rect.sizeDelta = resolution;
                var map = rootGo.AddComponent<UiBindingMap>();
                ApplyRootBackground(rootGo, rootStyle);
                TryApplySprite(rootGo, sourceRoot, rootStyle);
                foreach (var child in sourceRoot.Elements()) BuildElement(child, rootGo.transform, resolution);
                map.EditorSetEntries(new List<UiBindingMap.Entry>(Bindings));
                if (itemPrefabPaths.Count > 0)
                {
                    rootGo.AddComponent<UiTemplateCatalog>().EditorSetTemplates(itemPrefabPaths.Select(p =>
                        new UiTemplateCatalog.Entry { name = p.Key, prefab = AssetDatabase.LoadAssetAtPath<GameObject>(p.Value) }).ToList());
                }
                if (options.configureRoot != null) options.configureRoot(rootGo, map, itemPrefabPaths);
                var path = outputFolder.TrimEnd('/') + "/" + options.panelName + ".prefab";
                if (PrefabUtility.SaveAsPrefabAsset(rootGo, path) == null) throw new IOException("Prefab save failed: " + path);
                report.panelName = options.panelName;
                report.width = resolution.x; report.height = resolution.y;
                File.WriteAllText(outputFolder + "/" + options.panelName + ".import-report.json", JsonUtility.ToJson(report, true));
                AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
                UiPanelScenePlacement.CleanupTransientMeshesForPrefab(path);
                return path;
            }
            finally { UnityEngine.Object.DestroyImmediate(rootGo); }
        }

        public static UiImportReport ValidatePackage(string htmlPath, string assetPrefix = null)
        {
            CssRules.Clear(); CssVariables.Clear(); OrderedRules.Clear(); CssWarnings.Clear();
            var document = XDocument.Load(htmlPath, LoadOptions.PreserveWhitespace);
            var htmlDir = Path.GetDirectoryName(Path.GetFullPath(htmlPath));
            LoadLinkedCss(document, htmlDir);
            if (string.IsNullOrEmpty(assetPrefix))
            {
                var root = document.Descendants().FirstOrDefault(e => (string)e.Attribute("data-root") == "true");
                var name = root == null ? "Panel" : (string)root.Attribute("data-panel-name") ?? (string)root.Attribute("id") ?? Path.GetFileNameWithoutExtension(htmlPath);
                assetPrefix = root == null ? null : (string)root.Attribute("data-asset-prefix");
                if (string.IsNullOrEmpty(assetPrefix)) assetPrefix = "Assets/Art/UI/Sprite/" + name + "/";
            }
            var report = UiPackageValidator.Validate(document, htmlDir, assetPrefix.TrimEnd('/') + "/", ResolveStyle);
            report.warnings.AddRange(CssWarnings);
            return report;
        }

        internal static void ValidateAssetPath(string path)
        {
            if (!path.StartsWith("Assets/", StringComparison.Ordinal) || path.Contains("\\") ||
                path.Split('/').Any(p => p == ".." || p == "." || string.IsNullOrEmpty(p)) || path.Contains(":"))
                throw new InvalidDataException("Unsafe Unity asset path: " + path);
        }

        private static Transform BuildElement(XElement element, Transform parent, Vector2 parentSize)
        {
            if (element.Name.LocalName.Equals("template", StringComparison.OrdinalIgnoreCase))
                return null;

            var uguiType = ((string)element.Attribute("data-ugui") ?? "Container").Trim();
            var style = ResolveStyle(element);
            var name = BuildObjectName(element, uguiType, parent);

            GameObject go;
            Transform childParent;
            Component bindingTarget = null;

            switch (uguiType.ToLowerInvariant())
            {
                case "text":
                    go = CreateText(name, parent, style, element.Value);
                    childParent = go.transform;
                    bindingTarget = go.GetComponent<TMP_Text>();
                    break;

                case "image":
                    go = CreateImage(name, parent, style);
                    childParent = go.transform;
                    bindingTarget = go.GetComponent<Image>();
                    break;

                case "rawimage":
                    go = CreateRawImage(name, parent, style);
                    childParent = go.transform;
                    bindingTarget = go.GetComponent<RawImage>();
                    break;

                case "button":
                    go = CreateButton(name, parent, style, DirectText(element), element);
                    childParent = go.transform;
                    bindingTarget = go.GetComponent<Button>();
                    break;

                case "inputfield":
                    var placeholderText = ((string)element.Attribute("placeholder") ?? element.Value ?? string.Empty).Trim();
                    go = CreateInputField(name, parent, style, placeholderText);
                    go.GetComponent<TMP_InputField>().text = (string)element.Attribute("data-value") ?? (string)element.Attribute("value") ?? string.Empty;
                    childParent = go.transform;
                    bindingTarget = go.GetComponent<TMP_InputField>();
                    break;

                case "scrollview":
                    go = CreateScrollView(name, parent, style, out childParent);
                    bindingTarget = go.GetComponent<ScrollRect>();
                    break;

                case "toggle":
                    go = CreateToggle(name, parent, style, element);
                    childParent = go.transform;
                    bindingTarget = go.GetComponent<Toggle>();
                    break;

                case "slider":
                    go = CreateSlider(name, parent, style, element);
                    childParent = go.transform;
                    bindingTarget = go.GetComponent<Slider>();
                    break;

                default:
                    go = new GameObject(name, typeof(RectTransform));
                    go.transform.SetParent(parent, false);
                    ApplyRect(go.GetComponent<RectTransform>(), style, parentSize, element);
                    childParent = go.transform;
                    bindingTarget = go.transform;
                    break;
            }

            // Non-container creators apply Rect using their parent size here.
            if (uguiType.Equals("Text", StringComparison.OrdinalIgnoreCase) ||
                uguiType.Equals("Image", StringComparison.OrdinalIgnoreCase) ||
                uguiType.Equals("RawImage", StringComparison.OrdinalIgnoreCase) ||
                uguiType.Equals("Button", StringComparison.OrdinalIgnoreCase) ||
                uguiType.Equals("InputField", StringComparison.OrdinalIgnoreCase) ||
                uguiType.Equals("ScrollView", StringComparison.OrdinalIgnoreCase) ||
                uguiType.Equals("Toggle", StringComparison.OrdinalIgnoreCase) ||
                uguiType.Equals("Slider", StringComparison.OrdinalIgnoreCase))
            {
                ApplyRect(go.GetComponent<RectTransform>(), style, parentSize, element);

                if (uguiType.Equals("ScrollView", StringComparison.OrdinalIgnoreCase))
                {
                    var viewportRect = go.GetComponent<RectTransform>();
                    var contentRect = childParent as RectTransform;
                    if (contentRect != null)
                    {
                        var requestedContentHeight = ReadFloat(style, "content-height", viewportRect.sizeDelta.y);
                        contentRect.sizeDelta = new Vector2(0, Mathf.Max(viewportRect.sizeDelta.y, requestedContentHeight));
                    }
                }
            }

            TryApplySprite(go, element, style);
            TryApplyRawTexture(go, element);
            TryAddOutline(go, style);
            RegisterBinding(element, bindingTarget);
            var selectable = go.GetComponent<Selectable>();
            if (selectable != null) selectable.interactable = (string)element.Attribute("data-interactable") != "false";
            var tmp = go.GetComponent<TMP_Text>();
            if (tmp != null)
            {
                tmp.enableWordWrapping = (string)element.Attribute("data-wrap") == "true";
                tmp.overflowMode = (string)element.Attribute("data-overflow") == "ellipsis" ? TextOverflowModes.Ellipsis : TextOverflowModes.Overflow;
            }

            var ignoreChildren = string.Equals((string)element.Attribute("data-ignore-children"), "true", StringComparison.OrdinalIgnoreCase);
            var noRecursiveChildren = uguiType.Equals("Text", StringComparison.OrdinalIgnoreCase) ||
                                      uguiType.Equals("RawImage", StringComparison.OrdinalIgnoreCase) ||
                                      uguiType.Equals("InputField", StringComparison.OrdinalIgnoreCase) ||
                                      uguiType.Equals("Slider", StringComparison.OrdinalIgnoreCase);

            if (!ignoreChildren && !noRecursiveChildren)
            {
                var rect = go.GetComponent<RectTransform>();
                var childRect = childParent as RectTransform;
                var size = childRect != null ? childRect.rect.size : (rect != null ? rect.rect.size : parentSize);
                foreach (var child in element.Elements())
                    BuildElement(child, childParent, size);
            }

            return go.transform;
        }

        private static Dictionary<string, string> BuildTemplatePrefabs(XDocument document, string outputFolder)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var itemFolder = outputFolder.TrimEnd('/') + "/Items";
            EnsureFolder(itemFolder);

            foreach (var template in document.Descendants().Where(x => x.Name.LocalName.Equals("template", StringComparison.OrdinalIgnoreCase)))
            {
                var prefabName = ((string)template.Attribute("data-prefab") ?? string.Empty).Trim();
                var source = template.Elements().FirstOrDefault();
                if (string.IsNullOrWhiteSpace(prefabName) || source == null)
                    continue;

                Bindings.Clear();
                var host = new GameObject(prefabName + "_BuildHost", typeof(RectTransform));
                GameObject itemGo = null;
                try
                {
                var hostRect = host.GetComponent<RectTransform>();
                var templateParent = template.Parent;
                var templateStyle = ResolveStyle(templateParent);
                hostRect.sizeDelta = new Vector2(ReadFloat(templateStyle, "width", 1000), ReadFloat(templateStyle, "height", 1000));
                var built = BuildElement(source, host.transform, hostRect.sizeDelta);
                if (built == null)
                {
    
                    continue;
                }

                itemGo = built.gameObject;
                itemGo.name = (string)source.Attribute("data-ui-name") ?? "Item_" + UiObjectNaming.Pascal(prefabName);
                itemGo.transform.SetParent(null, false);


                var map = itemGo.GetComponent<UiBindingMap>();
                if (map == null) map = itemGo.AddComponent<UiBindingMap>();
                map.EditorSetEntries(new List<UiBindingMap.Entry>(Bindings));
                EditorUtility.SetDirty(map);

                if (options.configureTemplate != null) options.configureTemplate(prefabName, itemGo, map);

                var prefabPath = itemFolder + "/" + itemGo.name + ".prefab";
                if (PrefabUtility.SaveAsPrefabAsset(itemGo, prefabPath) == null) throw new IOException("Item prefab save failed: " + prefabPath);

                result[prefabName] = prefabPath;
                }
                finally
                {
                    if (itemGo != null) UnityEngine.Object.DestroyImmediate(itemGo);
                    if (host != null) UnityEngine.Object.DestroyImmediate(host);
                }
            }

            Bindings.Clear();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return result;
        }

        private static GameObject CreateImage(string name, Transform parent, Dictionary<string, string> style)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = ReadBackgroundColor(style, new Color(0.03f, 0.11f, 0.2f, 1f));
            image.raycastTarget = false;
            return go;
        }

        private static GameObject CreateRawImage(string name, Transform parent, Dictionary<string, string> style)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            go.transform.SetParent(parent, false);
            var raw = go.GetComponent<RawImage>();
            raw.color = Color.white;
            raw.raycastTarget = false;
            return go;
        }

        private static GameObject CreateText(string name, Transform parent, Dictionary<string, string> style, string value)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<TextMeshProUGUI>();
            text.text = NormalizeText(value);
            if (options.font != null) text.font = options.font;
            if (options.fontMaterial != null) text.fontSharedMaterial = options.fontMaterial;
            text.richText = false;
            text.fontSize = ReadFloat(style, "font-size", 18f);
            text.fontStyle = ReadFontStyle(style);
            text.color = ReadColor(style, "color", Color.white);
            text.alignment = ReadTextAlignment(style);
            text.raycastTarget = false;
            text.enableWordWrapping = false;
            text.overflowMode = TextOverflowModes.Overflow;
            return go;
        }

        private static GameObject CreateButton(string name, Transform parent, Dictionary<string, string> style, string directText, XElement element)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = ReadBackgroundColor(style, new Color(0.04f, 0.21f, 0.4f, 1f));
            var button = go.GetComponent<Button>();
            button.targetGraphic = image;

            if (!string.IsNullOrWhiteSpace(directText))
            {
                var labelGo = new GameObject("Txt_Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                labelGo.transform.SetParent(go.transform, false);
                var rect = labelGo.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                var label = labelGo.GetComponent<TextMeshProUGUI>();
                label.text = NormalizeText(directText);
                if (options.font != null) label.font = options.font;
                if (options.fontMaterial != null) label.fontSharedMaterial = options.fontMaterial;
                label.richText = false;
                label.fontSize = ReadFloat(style, "font-size", 18f);
                label.fontStyle = ReadFontStyle(style);
                label.color = ReadColor(style, "color", Color.white);
                label.alignment = style.ContainsKey("text-align") ? ReadTextAlignment(style) : TextAlignmentOptions.Center;
                label.enableWordWrapping = (string)element.Attribute("data-wrap") == "true";
                label.overflowMode = (string)element.Attribute("data-overflow") == "ellipsis" ? TextOverflowModes.Ellipsis : TextOverflowModes.Overflow;
                label.raycastTarget = false;
            }
            return go;
        }

        private static GameObject CreateInputField(string name, Transform parent, Dictionary<string, string> style, string placeholderText)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(TMP_InputField));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = ReadBackgroundColor(style, new Color(0.02f, 0.09f, 0.16f, 1f));

            var textArea = new GameObject("Group_TextViewport", typeof(RectTransform), typeof(RectMask2D));
            textArea.transform.SetParent(go.transform, false);
            var areaRect = textArea.GetComponent<RectTransform>();
            areaRect.anchorMin = Vector2.zero;
            areaRect.anchorMax = Vector2.one;
            areaRect.offsetMin = new Vector2(12, 5);
            areaRect.offsetMax = new Vector2(-12, -5);

            var placeholder = CreateText("Txt_Placeholder", textArea.transform, style, placeholderText);
            var placeholderRect = placeholder.GetComponent<RectTransform>();
            placeholderRect.anchorMin = Vector2.zero;
            placeholderRect.anchorMax = Vector2.one;
            placeholderRect.offsetMin = Vector2.zero;
            placeholderRect.offsetMax = Vector2.zero;
            placeholder.GetComponent<TMP_Text>().color = new Color(0.55f, 0.67f, 0.77f, 1f);

            var textGo = CreateText("Txt_Value", textArea.transform, style, string.Empty);
            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            var input = go.GetComponent<TMP_InputField>();
            input.textViewport = areaRect;
            input.textComponent = textGo.GetComponent<TMP_Text>();
            input.placeholder = placeholder.GetComponent<TMP_Text>();
            return go;
        }

        private static GameObject CreateScrollView(string name, Transform parent, Dictionary<string, string> style, out Transform contentTransform)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(RectMask2D), typeof(ScrollRect));
            go.transform.SetParent(parent, false);

            var content = new GameObject("Group_Content", typeof(RectTransform));
            content.transform.SetParent(go.transform, false);
            var contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = Vector2.zero;

            var hit = go.AddComponent<Image>();
            hit.color = Color.clear;
            hit.raycastTarget = true;
            var scroll = go.GetComponent<ScrollRect>();
            scroll.viewport = go.GetComponent<RectTransform>();
            scroll.content = contentRect;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 30f;

            contentTransform = content.transform;
            return go;
        }

        private static void ApplyRect(RectTransform rect, Dictionary<string, string> style, Vector2 parentSize, XElement element)
        {
            var defaultFont = ReadFloat(style, "font-size", 18f);
            var value = NormalizeText(element.Value);

            var width = ReadFloat(style, "width", -1f);
            var height = ReadFloat(style, "height", -1f);
            if (width < 0 && style.ContainsKey("left") && style.ContainsKey("right")) width = parentSize.x - ReadFloat(style, "left", 0) - ReadFloat(style, "right", 0);
            if (height < 0 && style.ContainsKey("top") && style.ContainsKey("bottom")) height = parentSize.y - ReadFloat(style, "top", 0) - ReadFloat(style, "bottom", 0);
            if (width < 0) width = Mathf.Max(30f, value.Length * defaultFont * 0.72f + 8f);
            if (height < 0) height = Mathf.Max(28f, defaultFont * 1.55f);

            var left = ReadFloat(style, "left", float.NaN);
            var top = ReadFloat(style, "top", float.NaN);
            var right = ReadFloat(style, "right", float.NaN);
            var bottom = ReadFloat(style, "bottom", float.NaN);

            if (float.IsNaN(left) && !float.IsNaN(right))
                left = parentSize.x - right - width;
            if (float.IsNaN(top) && !float.IsNaN(bottom))
                top = parentSize.y - bottom - height;
            if (float.IsNaN(left)) left = 0;
            if (float.IsNaN(top)) top = 0;

            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = new Vector2(left, -top);
        }

        private static void ApplyRootBackground(GameObject root, Dictionary<string, string> style)
        {
            var image = root.AddComponent<Image>();
            image.color = ReadBackgroundColor(style, new Color(0.01f, 0.04f, 0.08f, 1f));
            image.raycastTarget = false;
        }

        private static void RegisterBinding(XElement element, Component target)
        {
            var key = ((string)element.Attribute("data-binding") ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(key) || target == null)
                return;

            if (Bindings.Any(x => string.Equals(x.key, key, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidDataException("Duplicate binding: " + key);
            Bindings.Add(new UiBindingMap.Entry { key = key, target = target });
        }

        private static void TryApplySprite(GameObject go, XElement element, Dictionary<string, string> style)
        {
            var spritePath = ((string)element.Attribute("data-sprite") ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(spritePath))
                return;
            ValidateAssetPath(spritePath);

            var image = go.GetComponent<Image>();
            if (image == null)
                return;

            var useNineSlice = string.Equals(
                (string)element.Attribute("data-nine-slice"),
                "true",
                StringComparison.OrdinalIgnoreCase);

            var border = ParseBorder(element, useNineSlice ? new Vector4(18, 18, 18, 18) : Vector4.zero);
            EnsureSpriteImporter(spritePath, useNineSlice, border);

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            if (sprite == null)
            {
                throw new FileNotFoundException("Sprite not found: " + spritePath);
            }

            image.sprite = sprite;
            image.color = Color.white;
            image.type = useNineSlice ? Image.Type.Sliced : Image.Type.Simple;
        }

        private static void EnsureSpriteImporter(string spritePath, bool useNineSlice, Vector4 border)
        {
            var importer = AssetImporter.GetAtPath(spritePath) as TextureImporter;
            if (importer == null)
                return;

            var changed = false;
            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                changed = true;
            }

            if (importer.spriteBorder != (useNineSlice ? border : Vector4.zero))
            {
                importer.spriteBorder = useNineSlice ? border : Vector4.zero;
                changed = true;
            }

            if (importer.mipmapEnabled)
            {
                importer.mipmapEnabled = false;
                changed = true;
            }
            if (!importer.alphaIsTransparency)
            {
                importer.alphaIsTransparency = true;
                changed = true;
            }
            if (importer.wrapMode != TextureWrapMode.Clamp)
            {
                importer.wrapMode = TextureWrapMode.Clamp;
                changed = true;
            }
            if (importer.textureCompression != TextureImporterCompression.Uncompressed)
            {
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                changed = true;
            }

            if (changed)
                importer.SaveAndReimport();
        }

        private static void ValidateReferencedAsset(string htmlDir, string targetAssetPath, List<string> missing)
        {
            targetAssetPath = (targetAssetPath ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(targetAssetPath))
                return;

            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(targetAssetPath) != null)
                return;

            var sourcePath = ResolveHtmlAssetSource(htmlDir, targetAssetPath);
            if (string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath))
                missing.Add(targetAssetPath + " -> " + (sourcePath ?? "<无法映射>"));
        }

        private static string ResolveHtmlAssetSource(string htmlDir, string targetAssetPath)
        {
            var targetPrefix = options.assetPrefix;
            ValidateAssetPath(targetAssetPath);
            if (!targetAssetPath.StartsWith(targetPrefix, StringComparison.OrdinalIgnoreCase))
                return null;

            var relative = targetAssetPath.Substring(targetPrefix.Length).Replace('/', Path.DirectorySeparatorChar);
            return Path.Combine(htmlDir, "assets", relative);
        }

        private static void PrepareReferencedAssets(XDocument document, string htmlDir)
        {
            foreach (var element in document.Descendants())
            {
                var spritePath = ((string)element.Attribute("data-sprite") ?? string.Empty).Trim();
                if (!string.IsNullOrWhiteSpace(spritePath))
                {
                    EnsureAssetCopiedFromHtml(htmlDir, spritePath);
                    var nineSlice = string.Equals((string)element.Attribute("data-nine-slice"), "true", StringComparison.OrdinalIgnoreCase);
                    var border = ParseBorder(element, nineSlice ? new Vector4(18, 18, 18, 18) : Vector4.zero);
                    EnsureSpriteImporter(spritePath, nineSlice, border);
                }

                var texturePath = ((string)element.Attribute("data-texture") ?? string.Empty).Trim();
                if (!string.IsNullOrWhiteSpace(texturePath))
                {
                    EnsureAssetCopiedFromHtml(htmlDir, texturePath);
                    EnsureRawTextureImporter(texturePath);
                }

                var normalSprite = ((string)element.Attribute("data-normal-sprite") ?? string.Empty).Trim();
                if (!string.IsNullOrWhiteSpace(normalSprite))
                {
                    EnsureAssetCopiedFromHtml(htmlDir, normalSprite);
                    EnsureSpriteImporter(normalSprite, nineSliceFor(element), ParseBorder(element, Vector4.zero));
                }

                var selectedSprite = ((string)element.Attribute("data-selected-sprite") ?? string.Empty).Trim();
                if (!string.IsNullOrWhiteSpace(selectedSprite))
                {
                    EnsureAssetCopiedFromHtml(htmlDir, selectedSprite);
                    EnsureSpriteImporter(selectedSprite, nineSliceFor(element), ParseBorder(element, Vector4.zero));
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static bool nineSliceFor(XElement element) { return (string)element.Attribute("data-nine-slice") == "true"; }

        private static void EnsureAssetCopiedFromHtml(string htmlDir, string targetAssetPath)
        {
            var targetPrefix = options.assetPrefix;
            ValidateAssetPath(targetAssetPath);
            if (!targetAssetPath.StartsWith(targetPrefix, StringComparison.OrdinalIgnoreCase))
                return;

            var sourcePath = ResolveHtmlAssetSource(htmlDir, targetAssetPath);
            if (!File.Exists(sourcePath))
            {
                if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(targetAssetPath) != null) return;
                throw new FileNotFoundException("HTML asset source not found: " + sourcePath);
            }

            var projectRoot = Directory.GetParent(Application.dataPath).FullName;
            var absoluteTarget = Path.Combine(projectRoot, targetAssetPath.Replace('/', Path.DirectorySeparatorChar));
            var targetDir = Path.GetDirectoryName(absoluteTarget);
            if (!string.IsNullOrWhiteSpace(targetDir))
                Directory.CreateDirectory(targetDir);

            var shouldCopy = !File.Exists(absoluteTarget) || !File.ReadAllBytes(sourcePath).SequenceEqual(File.ReadAllBytes(absoluteTarget));
            if (!shouldCopy)
                return;

            File.Copy(sourcePath, absoluteTarget, true);
            AssetDatabase.ImportAsset(targetAssetPath, ImportAssetOptions.ForceUpdate);
        }

        private static void EnsureRawTextureImporter(string assetPath)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
                return;

            var changed = false;
            if (importer.textureType != TextureImporterType.Default)
            {
                importer.textureType = TextureImporterType.Default;
                changed = true;
            }
            if (importer.mipmapEnabled)
            {
                importer.mipmapEnabled = false;
                changed = true;
            }
            if (importer.wrapMode != TextureWrapMode.Clamp)
            {
                importer.wrapMode = TextureWrapMode.Clamp;
                changed = true;
            }
            if (importer.textureCompression != TextureImporterCompression.Uncompressed)
            {
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                changed = true;
            }

            if (changed)
                importer.SaveAndReimport();
        }

        private static Vector4 ParseBorder(XElement element, Vector4 fallback)
        {
            var value = ((string)element.Attribute("data-border") ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(value))
                return fallback;

            var parts = value.Split(',');
            if (parts.Length != 4)
                return fallback;

            float left, bottom, right, top;
            if (!float.TryParse(parts[0].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out left) ||
                !float.TryParse(parts[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out bottom) ||
                !float.TryParse(parts[2].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out right) ||
                !float.TryParse(parts[3].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out top))
                return fallback;

            return new Vector4(left, bottom, right, top);
        }

        private static Sprite LoadSprite(string assetPath)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        }

        private static void TryApplyRawTexture(GameObject go, XElement element)
        {
            var texturePath = ((string)element.Attribute("data-texture") ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(texturePath))
                return;

            var rawImage = go.GetComponent<RawImage>();
            if (rawImage == null)
                return;

            EnsureRawTextureImporter(texturePath);
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            if (texture == null)
            {
                Debug.LogWarning($"[HtmlUiImporter] Texture not found: {texturePath}");
                return;
            }

            rawImage.texture = texture;
            rawImage.color = Color.white;
        }

        private static void TryAddOutline(GameObject go, Dictionary<string, string> style)
        {
            if (!style.TryGetValue("border-color", out var colorText))
                return;

            var graphic = go.GetComponent<Graphic>();
            if (graphic == null)
                return;

            var outline = go.AddComponent<Outline>();
            outline.effectColor = ParseColor(ResolveVariables(colorText), new Color(0, 0.8f, 1f, 0.75f));
            outline.effectDistance = new Vector2(1, -1);
            outline.useGraphicAlpha = true;
        }

        private sealed class CssRule
        {
            public string selector;
            public Dictionary<string, string> declarations;
        }
        private static readonly List<CssRule> OrderedRules = new List<CssRule>();
        internal static Dictionary<string, string> ResolveStyle(XElement element)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var classes = ((string)element.Attribute("class") ?? "").Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
            var id = (string)element.Attribute("id");
            foreach (var prefix in new[] { ".", "#" })
                foreach (var rule in OrderedRules)
                    if (rule.selector.StartsWith(prefix) &&
                        (prefix == "." ? classes.Contains(rule.selector.Substring(1)) : id == rule.selector.Substring(1)))
                        foreach (var pair in rule.declarations) result[pair.Key] = ResolveVariables(pair.Value);
            foreach (var pair in ParseDeclarations((string)element.Attribute("style") ?? "")) result[pair.Key] = ResolveVariables(pair.Value);
            return result;
        }

        private static void LoadLinkedCss(XDocument document, string htmlDir)
        {
            foreach (var link in document.Descendants().Where(x => x.Name.LocalName.Equals("link", StringComparison.OrdinalIgnoreCase)))
            {
                var rel = ((string)link.Attribute("rel") ?? string.Empty).Trim();
                var href = ((string)link.Attribute("href") ?? string.Empty).Trim();
                if (!rel.Equals("stylesheet", StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(href))
                    continue;

                var cssPath = Path.Combine(htmlDir, href.Replace('/', Path.DirectorySeparatorChar));
                if (!File.Exists(cssPath)) throw new FileNotFoundException("CSS missing: " + cssPath);
                ParseCss(File.ReadAllText(cssPath));
            }
            foreach (var sheet in document.Descendants().Where(x => x.Name.LocalName == "style")) ParseCss(sheet.Value);
        }

        private static void ParseCss(string css)
        {
            css = Regex.Replace(css, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);
            if (Regex.IsMatch(css, @"@(media|supports|import|keyframes)\b", RegexOptions.IgnoreCase))
                throw new InvalidDataException("CSS media/supports/import/keyframes are unsupported. Use a separate browser-only stylesheet if needed.");
            foreach (Match match in Regex.Matches(css, @"(?<selector>[^{}]+)\{(?<body>[^{}]*)\}"))
            {
                var selectorText = match.Groups["selector"].Value.Trim();
                var declarations = ParseDeclarations(match.Groups["body"].Value);

                if (selectorText == ":root")
                {
                    foreach (var pair in declarations.Where(x => x.Key.StartsWith("--")))
                        CssVariables[pair.Key] = pair.Value;
                    continue;
                }

                foreach (var selector in selectorText.Split(','))
                {
                    var s = selector.Trim();
                    // Only simple .class and #id selectors are imported.
                    if (!Regex.IsMatch(s, @"^[.#][A-Za-z0-9_\-]+$"))
                    {
                        if (s != "html" && s != "body" && s != "*" && s != "template") CssWarnings.Add("CSS selector ignored by Unity: " + s);
                        continue;
                    }

                    OrderedRules.Add(new CssRule { selector = s, declarations = declarations });
                }
            }
        }

        private static Dictionary<string, string> ParseDeclarations(string body)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var declaration in body.Split(';'))
            {
                var idx = declaration.IndexOf(':');
                if (idx <= 0) continue;
                var key = declaration.Substring(0, idx).Trim();
                var value = declaration.Substring(idx + 1).Trim();
                if (!string.IsNullOrWhiteSpace(key))
                    result[key] = value;
            }
            return result;
        }

        private static string ResolveVariables(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return value;
            return Regex.Replace(value, @"var\((--[^)]+)\)", m =>
                CssVariables.TryGetValue(m.Groups[1].Value, out var resolved) ? resolved : m.Value);
        }

        private static string BuildObjectName(XElement element, string uguiType, Transform parent)
        {
            var explicitName = ((string)element.Attribute("data-ui-name") ?? "").Trim();
            if (explicitName.Length > 0) return explicitName;
            var semantic = (string)element.Attribute("data-binding") ?? (string)element.Attribute("id");
            if (string.IsNullOrWhiteSpace(semantic))
                semantic = ((string)element.Attribute("class") ?? "").Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            if (!string.IsNullOrEmpty(semantic) && semantic.EndsWith("Panel", StringComparison.Ordinal))
                semantic = semantic.Substring(0, semantic.Length - 5);
            var name = UiObjectNaming.Prefix(uguiType) + "_" + UiObjectNaming.Pascal(semantic ?? "Element");
            return UiObjectNaming.UniqueUnder(parent, name);
        }

        private static string DirectText(XElement element)
        {
            return NormalizeText(string.Concat(element.Nodes().OfType<XText>().Select(x => x.Value)));
        }

        private static string NormalizeText(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            return Regex.Replace(value, @"\s+", " ").Trim();
        }

        private static float ReadFloat(Dictionary<string, string> style, string key, float fallback)
        {
            if (!style.TryGetValue(key, out var value) || string.IsNullOrWhiteSpace(value))
                return fallback;
            value = ResolveVariables(value).Trim().Replace("px", string.Empty).Replace("°", string.Empty);
            return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result) ? result : fallback;
        }

        private static Color ReadColor(Dictionary<string, string> style, string key, Color fallback)
        {
            return style.TryGetValue(key, out var value) ? ParseColor(ResolveVariables(value), fallback) : fallback;
        }

        private static Color ReadBackgroundColor(Dictionary<string, string> style, Color fallback)
        {
            if (style.TryGetValue("background-color", out var color))
                return ParseColor(ResolveVariables(color), fallback);

            if (style.TryGetValue("background", out var background))
            {
                background = ResolveVariables(background);
                var hex = Regex.Match(background, @"#[0-9a-fA-F]{6,8}");
                if (hex.Success) return ParseColor(hex.Value, fallback);
                var rgba = Regex.Match(background, @"rgba?\([^)]*\)");
                if (rgba.Success) return ParseColor(rgba.Value, fallback);
            }
            return fallback;
        }

        private static Color ParseColor(string value, Color fallback)
        {
            if (string.IsNullOrWhiteSpace(value)) return fallback;
            value = value.Trim();
            if (ColorUtility.TryParseHtmlString(value, out var html)) return html;

            var rgba = Regex.Match(value, @"rgba?\(\s*(\d+)\s*,\s*(\d+)\s*,\s*(\d+)(?:\s*,\s*([0-9.]+))?\s*\)");
            if (rgba.Success)
            {
                var r = int.Parse(rgba.Groups[1].Value) / 255f;
                var g = int.Parse(rgba.Groups[2].Value) / 255f;
                var b = int.Parse(rgba.Groups[3].Value) / 255f;
                var a = rgba.Groups[4].Success
                    ? float.Parse(rgba.Groups[4].Value, CultureInfo.InvariantCulture)
                    : 1f;
                return new Color(r, g, b, a);
            }
            return fallback;
        }

        private static FontStyles ReadFontStyle(Dictionary<string, string> style)
        {
            if (style.TryGetValue("font-weight", out var weight) &&
                (weight.Equals("bold", StringComparison.OrdinalIgnoreCase) || weight == "700" || weight == "800" || weight == "900"))
                return FontStyles.Bold;
            return FontStyles.Normal;
        }

        private static TextAlignmentOptions ReadTextAlignment(Dictionary<string, string> style)
        {
            if (!style.TryGetValue("text-align", out var value)) return TextAlignmentOptions.Left;
            if (value.Equals("center", StringComparison.OrdinalIgnoreCase)) return TextAlignmentOptions.Center;
            if (value.Equals("right", StringComparison.OrdinalIgnoreCase)) return TextAlignmentOptions.Right;
            return TextAlignmentOptions.Left;
        }

        private static void EnsureFolder(string path)
        {
            var parts = path.Split('/');
            var current = parts[0];
            for (var i = 1; i < parts.Length; i++)
            {
                var next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
#endif
