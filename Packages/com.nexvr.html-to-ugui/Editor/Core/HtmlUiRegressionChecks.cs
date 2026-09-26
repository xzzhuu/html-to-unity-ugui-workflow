#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace HtmlToUGUI.Editor
{
    public static class HtmlUiRegressionChecks
    {
        public static string Run()
        {
            var passed = new List<string>();
            var id = Guid.NewGuid().ToString("N");
            var dir = Path.Combine(Path.GetTempPath(), "HtmlUiRegression-" + id);
            var output = "Assets/Generated/HtmlUiRegression-" + id;
            Directory.CreateDirectory(dir);
            var html = Path.Combine(dir, "panel.html");
            Func<string, string> wrap = content => "<html><head><style>.a{width:20px;height:30px;left:10px;top:5px}.b{width:40px}.a{height:35px}#target{width:60px}</style></head><body><div data-root='true' style='width:320px;height:240px'>" + content + "</div></body></html>";
            Action<bool, string> check = (condition, name) => { if (!condition) throw new Exception("Failed: " + name); passed.Add(name); };
            var settings = new HtmlUiImporter.ImportOptions { panelName = "RegressionPanel", font = TMP_Settings.defaultFontAsset };
            try
            {
                File.WriteAllText(html, wrap("<div id='target' class='b a' data-binding='Target'/><input data-ugui='Toggle' data-is-on='true' data-binding='Switch' style='left:100px;top:0;width:24px;height:24px'/><input data-ugui='Slider' data-binding='Volume' data-min='0' data-max='100' data-value='65' style='left:100px;top:40px;width:180px;height:24px'/><div data-ugui='ScrollView' data-binding='List' style='left:0;top:100px;width:320px;height:100px;content-height:400px'><template data-prefab='Row'><span data-ugui='Text' data-binding='Target' style='left:0;top:0;width:120px;height:40px'>Row</span></template></div>"));
                var path = HtmlUiImporter.Import(html, output, settings);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var map = prefab.GetComponent<UiBindingMap>();
                check(map.Get<RectTransform>("Target") != null, "Typed main binding lookup");
                check(map.Get<RectTransform>("Target").name == "Group_Target", "Semantic names from binding keys");
                var target = map.Get<RectTransform>("Target");
                check(target.sizeDelta == new Vector2(60,35), "CSS source order and id specificity");
                check(map.Get<Toggle>("Switch").isOn, "Native Toggle state");
                check(Mathf.Approximately(map.Get<Slider>("Volume").value,65), "Native Slider range and value");
                check(map.Get<ScrollRect>("List").GetComponent<Image>().raycastTarget, "ScrollView receives pointer events");
                check(map.Get<ScrollRect>("List").content.sizeDelta.y == 400, "Scrollable content extent");
                check(prefab.GetComponent<UiTemplateCatalog>() != null && AssetDatabase.LoadAssetAtPath<GameObject>(output + "/Items/Item_Row.prefab") != null, "Generic template catalog and scoped bindings");
                var instance = UnityEngine.Object.Instantiate(prefab);
                try
                {
                    var item = instance.GetComponent<UiTemplateCatalog>().CreateItem("Row", instance.transform);
                    check(item.name.EndsWith("_01") && !item.name.Contains("(Clone)"), "Dynamic item has stable numbered name");
                    check(item.Get<TMP_Text>("Target").text == "Row", "Template instantiation and typed lookup");
                    check(instance.GetComponent<UiBindingMap>().Get<RectTransform>("Target") != item.Get<RectTransform>("Target"), "Parent and item bindings remain isolated");
                }
                finally { UnityEngine.Object.DestroyImmediate(instance); }
                settings.fontMaterial = null;
                HtmlUiImporter.Import(html, output, settings);
                check(AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponents<UiBindingMap>().Length == 1, "Repeat import has one binding map");
                File.WriteAllText(html, wrap("<div id='target' class='a' data-binding='Target' style='width:90px'/>") );
                settings.fontMaterial = null;
                var inlinePath = HtmlUiImporter.Import(html,output,settings);
                check(AssetDatabase.LoadAssetAtPath<GameObject>(inlinePath).GetComponent<UiBindingMap>().Get<RectTransform>("Target").sizeDelta.x == 90, "Inline style overrides id rule");
                File.WriteAllText(html,wrap("<button data-ugui='Button' data-binding='Save' data-wrap='true' data-overflow='ellipsis' style='left:0;top:0;width:200px;height:40px;font-weight:700;text-align:left'>Save changes</button><input data-ugui='InputField' data-binding='Name' value='Initial name' style='left:0;top:60px;width:200px;height:40px'/><input data-ugui='InputField' data-binding='Override' value='HTML value' data-value='Configured value' style='left:0;top:120px;width:200px;height:40px'/>") );
                settings.fontMaterial = null;
                var controlsPath = HtmlUiImporter.Import(html,output,settings);
                var controls = AssetDatabase.LoadAssetAtPath<GameObject>(controlsPath).GetComponent<UiBindingMap>();
                check(controls.Get<Button>("Save") != null && controls.Get("Target") == null, "Reimport invalidates cached bindings after hierarchy changes");
                var buttonLabel = controls.Get<Button>("Save").GetComponentInChildren<TMP_Text>();
                check((buttonLabel.fontStyle & FontStyles.Bold) != 0 && buttonLabel.alignment == TextAlignmentOptions.Left, "Button label preserves font style and alignment");
                check(buttonLabel.enableWordWrapping && buttonLabel.overflowMode == TextOverflowModes.Ellipsis, "Button label preserves wrap and overflow settings");
                check(controls.Get<TMP_InputField>("Name").text == "Initial name", "InputField preserves HTML initial value");
                check(controls.Get<TMP_InputField>("Override").text == "Configured value", "InputField explicit data-value takes precedence");
                var invalid = new Dictionary<string,string>
                {
                    {"Unsafe UI name", wrap("<div data-ui-name='Bad/Name'/>")},
                    {"Duplicate sibling UI names", wrap("<div data-ui-name='Group_Test'/><div data-ui-name='Group_Test'/>")},
                    {"Duplicate binding", wrap("<div data-binding='K'/><div data-binding='k'/>")},
                    {"Unsupported component", wrap("<div data-ugui='Dropdown'/>")},
                    {"Unsafe asset traversal", wrap("<img data-ugui='Image' data-sprite='Assets/HtmlUI/../../bad.png'/>")},
                    {"Missing asset", wrap("<img data-ugui='Image' data-sprite='Assets/HtmlUI/missing.png'/>")},
                    {"Invalid pixel units", wrap("<div style='width:50%;height:30px'/>")},
                    {"Template name traversal", wrap("<template data-prefab='../Escape'><div/></template>")},
                    {"Multiple template roots", wrap("<template data-prefab='Row'><div/><div/></template>")},
                    {"Duplicate root", wrap("<div data-root='true' style='width:10px;height:10px'/>")},
                    {"Item prefab filename collision", wrap("<template data-prefab='A'><div data-ui-name='Item_Shared'/></template><template data-prefab='B'><div data-ui-name='Item_Shared'/></template>")},
                    {"Template outside Panel", "<html><div data-root='true' style='width:320px;height:240px'/><template data-prefab='Outside'><div/></template></html>"},
                    {"Ignored component children", wrap("<input data-ugui='InputField'><span data-ugui='Text'>Lost</span></input>")},
                    {"Generated helper name collision", wrap("<button data-ugui='Button'>Save<span data-ugui='Text' data-ui-name='Txt_Label'>Duplicate</span></button>")},
                    {"Mismatched explicit root name", "<html><div data-root='true' data-panel-name='OtherPanel' style='width:320px;height:240px'/></html>"},
                    {"Unsupported layout", wrap("<div style='display:flex;width:100px;height:50px'/>")},
                    {"Missing root size", "<html><div data-root='true'/></html>"}
                };
                foreach (var pair in invalid)
                {
                    File.WriteAllText(html, pair.Value);
                    var rejected = false;
                    try { HtmlUiImporter.Import(html, output + "/Rejected", settings); }
                    catch (InvalidDataException) { rejected = true; }
                    check(rejected && !AssetDatabase.IsValidFolder(output + "/Rejected"), pair.Key + " rejected before output mutation");
                }
                File.WriteAllText(html,"<html><head><link rel='stylesheet' href='missing.css'/></head><div data-root='true' style='width:320px;height:240px'/></html>");
                var cssRejected = false;
                try { HtmlUiImporter.Import(html,output + "/Rejected",settings); } catch (FileNotFoundException) { cssRejected = true; }
                check(cssRejected && !AssetDatabase.IsValidFolder(output + "/Rejected"), "Missing linked CSS rejected before output mutation");
                var invalidRootName = false;
                try { HtmlUiImporter.Import(html, output + "/BadName", new HtmlUiImporter.ImportOptions { panelName = "Dashboard", font = TMP_Settings.defaultFontAsset }); }
                catch (InvalidDataException) { invalidRootName = true; }
                check(invalidRootName && !AssetDatabase.IsValidFolder(output + "/BadName"), "Root name without Panel suffix rejected before output mutation");
                File.WriteAllText(html,"<html><div data-root='true' data-panel-name='RegressionPanel' data-font-asset='Assets/MissingFont.asset' style='width:320px;height:240px'/></html>");
                var invalidFont = false;
                try { HtmlUiImporter.ImportPreparedHtml(html); } catch (InvalidDataException) { invalidFont = true; }
                check(invalidFont, "Explicit missing HTML font rejected without fallback");
                return JsonUtility.ToJson(new Result { count=passed.Count, passed=passed }, true);
            }
            finally
            {
                // Both paths are generated locally for this run and stay inside their declared roots.
                if (AssetDatabase.IsValidFolder(output)) AssetDatabase.DeleteAsset(output);
                Directory.Delete(dir, true);
            }
        }
        [Serializable] private sealed class Result { public int count; public List<string> passed; }
    }
}
#endif
