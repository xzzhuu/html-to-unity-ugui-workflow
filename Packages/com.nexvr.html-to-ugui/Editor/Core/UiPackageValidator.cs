#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using UnityEngine;

namespace HtmlToUGUI.Editor
{
    [Serializable]
    public sealed class UiImportReport
    {
        public string schemaVersion = "html-ugui/1";
        public string panelName;
        public float width, height;
        public int nodes, bindings, templates;
        public string visualStatus = "pending-browser-and-unity-review";
        public List<string> errors = new List<string>();
        public List<string> warnings = new List<string>();
    }

    public static class UiPackageValidator
    {
        private static readonly string[] Components = { "Container", "Text", "Image", "RawImage", "Button", "InputField", "ScrollView", "Toggle", "Slider" };
        public static UiImportReport Validate(XDocument doc, string dir, string prefix,
            Func<XElement, Dictionary<string, string>> resolve)
        {
            var report = new UiImportReport();
            var roots = doc.Descendants().Where(e => (string)e.Attribute("data-root") == "true").ToArray();
            if (roots.Length != 1) { report.errors.Add("Exactly one data-root=true is required."); return report; }
            var root = roots[0];
            var style = resolve(root);
            foreach (var axis in new[] { "width", "height" })
                if (!style.ContainsKey(axis) || !Regex.IsMatch(style[axis], @"^\d+(\.\d+)?(px)?$") || Number(style[axis]) <= 0)
                    report.errors.Add("Root requires positive pixel " + axis + ".");
            var scopes = new List<XElement> { root };
            var templates = root.Descendants().Where(e => e.Name.LocalName == "template").ToArray();
            if (doc.Descendants().Count(e => e.Name.LocalName == "template") != templates.Length)
                report.errors.Add("Templates must be descendants of the root Panel.");
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var itemNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var template in templates)
            {
                var name = (string)template.Attribute("data-prefab") ?? "";
                if (!Regex.IsMatch(name, @"^[A-Za-z0-9_\-]+$") || !names.Add(name)) report.errors.Add("Invalid/duplicate template name: " + name);
                if (template.Elements().Count() != 1) report.errors.Add("Template requires exactly one child: " + name);
                if (template.Ancestors().Any(e => e.Name.LocalName == "template")) report.errors.Add("Nested templates are unsupported: " + name);
                var itemRoot = template.Elements().FirstOrDefault();
                if (itemRoot != null)
                {
                    var itemName = (string)itemRoot.Attribute("data-ui-name") ?? "Item_" + HtmlToUGUI.UiObjectNaming.Pascal(name);
                    if (!itemName.StartsWith("Item_", StringComparison.Ordinal)) report.errors.Add("Template root must use Item_* naming: " + name);
                    if (!itemNames.Add(itemName)) report.errors.Add("Duplicate Item prefab filename: " + itemName);
                }
                scopes.Add(template);
            }
            foreach (var scope in scopes)
            {
                var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var element in scope.DescendantsAndSelf().Where(e => e == scope ||
                    e.Ancestors().FirstOrDefault(a => a.Name.LocalName == "template") == (scope.Name.LocalName == "template" ? scope : null)))
                {
                    if (element.Name.LocalName == "template") continue;
                    var binding = ((string)element.Attribute("data-binding") ?? "").Trim();
                    if (binding.Length > 0 && !keys.Add(binding)) report.errors.Add("Duplicate binding in scope: " + binding);
                    if (binding.Length > 0) report.bindings++;
                }
            }
            var assetRoles = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var spriteBorders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var e in root.DescendantsAndSelf().Where(e => e.Name.LocalName != "template"))
            {
                report.nodes++;
                var uiName = (string)e.Attribute("data-ui-name");
                if (uiName != null)
                {
                    if (!Regex.IsMatch(uiName, @"^(Group|Txt|Img|RawImg|Btn|Input|Scroll|Toggle|Slider|Item)_[A-Z][A-Za-z0-9]*(?:_[0-9]{2,})?$"))
                        report.errors.Add("Invalid data-ui-name: " + uiName);
                    if (e.Parent != null && e.Parent.Elements().Count(x => string.Equals((string)x.Attribute("data-ui-name"), uiName, StringComparison.OrdinalIgnoreCase)) > 1)
                        report.errors.Add("Duplicate sibling data-ui-name: " + uiName);
                }

                var type = (string)e.Attribute("data-ugui") ?? "Container";
                type = Components.FirstOrDefault(c => c.Equals(type, StringComparison.OrdinalIgnoreCase)) ?? type;
                var label = (string)e.Attribute("id") ?? (string)e.Attribute("data-binding") ?? e.Name.LocalName;
                if (!Components.Contains(type, StringComparer.OrdinalIgnoreCase)) report.errors.Add(label + ": unsupported component " + type);
                if ((type == "InputField" || type == "Button" || type == "Toggle" || type == "Slider") && e.Attribute("data-binding") == null)
                    report.warnings.Add(label + ": interactive control has no binding.");
                if (type == "Text" && e.Elements().Any()) report.errors.Add(label + ": Text must contain literal text only.");
                if ((type == "RawImage" || type == "InputField" || type == "Slider" || (string)e.Attribute("data-ignore-children") == "true") && e.Elements().Any())
                    report.errors.Add(label + ": source children would be ignored by the converter.");
                if (type == "Toggle" && e.Elements().Any(x => (string)x.Attribute("data-ui-name") == "Img_Checkmark"))
                    report.errors.Add(label + ": child name collides with generated Img_Checkmark.");
                if (type == "Button" && e.Nodes().OfType<XText>().Any(t => !string.IsNullOrWhiteSpace(t.Value)) &&
                    e.Elements().Any(x => (string)x.Attribute("data-ui-name") == "Txt_Label"))
                    report.errors.Add(label + ": child name collides with generated Txt_Label.");
                var s = resolve(e);
                foreach (var property in new[] { "left", "right", "top", "bottom", "width", "height", "font-size", "content-height" })
                    if (s.ContainsKey(property) && !Regex.IsMatch(s[property], @"^-?\d+(\.\d+)?(px)?$")) report.errors.Add(label + ": pixel value required for " + property + ".");
                if (e != root && (!s.ContainsKey("width") || !s.ContainsKey("height")) && !(s.ContainsKey("left") && s.ContainsKey("right") && s.ContainsKey("height")))
                    report.warnings.Add(label + ": missing explicit dimensions; importer may estimate text size.");
                foreach (var effect in new[] { "text-shadow", "box-shadow", "border-radius", "letter-spacing", "padding", "line-height", "border", "border-bottom" })
                    if (s.ContainsKey(effect)) report.warnings.Add(label + ": CSS " + effect + " is not reproduced; review or bake decoration to PNG.");
                if (s.Any(p => p.Value.Contains("gradient("))) report.warnings.Add(label + ": gradient requires a PNG for parity.");
                if (s.ContainsKey("display") && s["display"] != "block") report.errors.Add(label + ": unsupported display=" + s["display"]);
                if (s.ContainsKey("position") && s["position"] != "absolute" && e != root) report.errors.Add(label + ": only absolute layout is supported.");
                var border = (string)e.Attribute("data-border");
                if ((string)e.Attribute("data-nine-slice") == "true" &&
                    (border == null || !Regex.IsMatch(border, @"^\d+(\.\d+)?,\d+(\.\d+)?,\d+(\.\d+)?,\d+(\.\d+)?$")))
                    report.errors.Add(label + ": Nine-slice needs L,B,R,T nonnegative data-border.");
                foreach (var attribute in new[] { "data-sprite", "data-texture", "data-normal-sprite", "data-selected-sprite" })
                {
                    var path = (string)e.Attribute(attribute);
                    if (string.IsNullOrEmpty(path)) continue;
                    try { HtmlUiImporter.ValidateAssetPath(path); } catch (Exception ex) { report.errors.Add(ex.Message); continue; }
                    if (!path.StartsWith(prefix, StringComparison.Ordinal)) { report.errors.Add(label + ": asset must use configured prefix: " + path); continue; }
                    var file = Path.Combine(dir, "assets", path.Substring(prefix.Length));
                    if (!File.Exists(file) && !File.Exists(path)) report.errors.Add(label + ": missing asset " + file);
                    var role = attribute == "data-texture" ? "texture" : "sprite";
                    string previous;
                    if (assetRoles.TryGetValue(path, out previous) && previous != role) report.errors.Add(path + ": cannot import as both sprite and texture.");
                    assetRoles[path] = role;
                    if (role == "sprite")
                    {
                        var slice = (string)e.Attribute("data-nine-slice") == "true" ? (border ?? "") : "0,0,0,0";
                        if (spriteBorders.TryGetValue(path, out previous) && previous != slice) report.errors.Add(path + ": inconsistent Nine-slice borders; use separate assets or one border definition.");
                        spriteBorders[path] = slice;
                        if (File.Exists(file) && slice.Length > 0)
                        {
                            try
                            {
                                var bytes = File.ReadAllBytes(file);
                                if (bytes.Length < 24 || bytes[0] != 137 || bytes[1] != 80 || bytes[2] != 78 || bytes[3] != 71)
                                    report.errors.Add(path + ": invalid PNG header.");
                                else
                                {
                                    var width = BigEndian(bytes, 16); var height = BigEndian(bytes, 20);
                                    var sides = slice.Split(',').Select(Number).ToArray();
                                    if (sides.Length == 4 && (sides[0] + sides[2] >= width || sides[1] + sides[3] >= height))
                                        report.errors.Add(path + ": Nine-slice borders leave no centre pixels.");
                                }
                            }
                            catch (Exception ex) { report.errors.Add(path + ": invalid sprite/border: " + ex.Message); }
                        }
                    }
                    if (Path.GetExtension(path).ToLowerInvariant() != ".png") report.errors.Add(path + ": use PNG assets for this pipeline.");
                }
                if (type == "RawImage" && e.Attribute("data-texture") == null) report.errors.Add(label + ": RawImage requires data-texture.");
                if (e.Attribute("src") != null && e.Attribute("data-sprite") == null && type == "Image") report.errors.Add(label + ": browser src requires explicit data-sprite mapping.");
            }
            report.templates = templates.Length;
            report.warnings = report.warnings.Distinct().ToList();
            report.errors = report.errors.Distinct().ToList();
            return report;
        }
        private static float Number(string value) { return float.Parse(value.Replace("px", ""), CultureInfo.InvariantCulture); }
        private static int BigEndian(byte[] bytes, int offset) { return (bytes[offset] << 24) | (bytes[offset+1] << 16) | (bytes[offset+2] << 8) | bytes[offset+3]; }
    }
}
#endif
