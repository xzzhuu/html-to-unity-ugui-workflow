#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace HtmlToUGUI.Editor
{
    public static partial class HtmlUiImporter
    {
        private static GameObject CreateToggle(string name, Transform parent, Dictionary<string, string> style, XElement e)
        {
            var go = CreateImage(name, parent, style);
            var image = go.GetComponent<Image>();
            image.raycastTarget = true;
            var toggle = go.AddComponent<Toggle>();
            if (ToggleSpriteAttributes.Any(a => e.Attribute(a) != null))
            {
                // Full state surfaces already contain track/knob colors. The
                // browser's fallback background is not a tint for this artwork.
                image.color = Color.white;
                image.type = nineSliceFor(e) ? Image.Type.Sliced : Image.Type.Simple;
                toggle.targetGraphic = image;
                toggle.transition = Selectable.Transition.None;
                toggle.isOn = (string)e.Attribute("data-is-on") == "true";
                var visuals = go.AddComponent<HtmlToUGUI.UiToggleStateGraphic>();
                visuals.surface = image;
                visuals.offSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>((string)e.Attribute("data-toggle-off-sprite"));
                visuals.onSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>((string)e.Attribute("data-toggle-on-sprite"));
                visuals.disabledSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>((string)e.Attribute("data-toggle-disabled-sprite"));
                visuals.Refresh();
                return go;
            }
            var mark = CreateImage("Img_Checkmark", go.transform, new Dictionary<string, string>());
            Stretch(mark.GetComponent<RectTransform>(), new Vector2(6, 6), new Vector2(-6, -6));
            mark.GetComponent<Image>().color = ReadColor(style, "color", new Color(0, .8f, 1));
            toggle.targetGraphic = image;
            toggle.graphic = mark.GetComponent<Image>();
            toggle.isOn = (string)e.Attribute("data-is-on") == "true";
            return go;
        }
        private static void ApplyPartSprites(GameObject go, XElement element, Dictionary<string, string> style)
        {
            foreach (var attribute in PartSpriteAttributes)
            {
                var path = (string)element.Attribute(attribute);
                if (path == null) continue;
                var childName = attribute == "data-fill-sprite" ? "Img_Fill" : attribute == "data-handle-sprite" ? "Group_HandleArea/Img_Handle" : "Img_Checkmark";
                var image = go.transform.Find(childName).GetComponent<Image>();
                image.sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
                image.color = attribute == "data-handle-sprite" ? Color.white : ReadColor(style, "color", Color.white);
                image.type = (string)element.Attribute("data-nine-slice") == "true" ? Image.Type.Sliced : Image.Type.Simple;
            }
            var visuals = go.GetComponent<HtmlToUGUI.UiToggleStateGraphic>();
            if (visuals != null) visuals.Refresh();
        }
        private static GameObject CreateSlider(string name, Transform parent, Dictionary<string, string> style, XElement e)
        {
            var go = CreateImage(name, parent, style);
            var image = go.GetComponent<Image>(); image.raycastTarget = true;
            var slider = go.AddComponent<Slider>();
            var fill = CreateImage("Img_Fill", go.transform, new Dictionary<string, string>());
            Stretch(fill.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);
            fill.GetComponent<Image>().color = ReadColor(style, "color", new Color(0, .8f, 1));
            var area = UiTemporarySceneScope.CreateGameObject("Group_HandleArea", typeof(RectTransform)); area.transform.SetParent(go.transform, false);
            Stretch(area.GetComponent<RectTransform>(), new Vector2(8, 0), new Vector2(-8, 0));
            var handle = CreateImage("Img_Handle", area.transform, new Dictionary<string, string>());
            var hr = handle.GetComponent<RectTransform>(); hr.sizeDelta = new Vector2(16, 24);
            handle.GetComponent<Image>().color = Color.white;
            slider.fillRect = fill.GetComponent<RectTransform>();
            slider.handleRect = hr;
            slider.targetGraphic = handle.GetComponent<Image>();
            slider.minValue = AttributeFloat(e, "data-min", 0);
            slider.maxValue = AttributeFloat(e, "data-max", 1);
            slider.wholeNumbers = (string)e.Attribute("data-whole-numbers") == "true";
            slider.value = AttributeFloat(e, "data-value", slider.minValue);
            return go;
        }
        private static float AttributeFloat(XElement e, string key, float fallback)
        {
            return ReadFloat(new Dictionary<string, string> { { key, (string)e.Attribute(key) ?? "" } }, key, fallback);
        }
        private static void Stretch(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = min; rect.offsetMax = max;
        }
    }
}
#endif
