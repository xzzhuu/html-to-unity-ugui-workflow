#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace HtmlToUGUI.Editor
{
    // Optional, game-owned data. Only modifies newly generated native components.
    public static class UiNativeAssembly
    {
        public static JObject Load(string directory, UiProjectAssetPlan library)
        {
            if (string.IsNullOrEmpty(directory)) return null;
            directory=Path.GetFullPath(directory);
            var path = Path.Combine(directory, "contracts/native-assembly.json");
            if (!File.Exists(path)) return null;
            ContractFiles(directory);
            var data = JObject.Parse(File.ReadAllText(path));
            if (data["schemaVersion"]?.Type != JTokenType.Integer || (int?)data["schemaVersion"] != 1 || (string)data["projectName"] != Path.GetFileName(directory.TrimEnd('/', '\\')))
                throw new InvalidDataException("Native assembly identity/version mismatch.");
            if ((string)data["status"] != "active" && (string)data["status"] != "draft") throw new InvalidDataException("Native assembly status must be draft or active.");
            if (data["rounding"]?["fromCssRadius"]?.Type != JTokenType.Boolean)
                throw new InvalidDataException("rounding.fromCssRadius must be boolean.");
            if((string)data["status"]=="draft")return null;
            var slider = data["slider"] as JObject;
            if (slider != null)
            {
                foreach (var key in new[] { "trackHeight", "handleSize", "trackRadius" })
                    if ((slider[key]?.Type != JTokenType.Integer && slider[key]?.Type != JTokenType.Float) || (float)slider[key] <= 0 || float.IsInfinity((float)slider[key]) || float.IsNaN((float)slider[key]))
                        throw new InvalidDataException("slider." + key + " must be a positive number.");
                foreach (var key in new[] { "trackAssetId", "handleAssetId" }) ResolveSprite(slider, key, library, false);
                var style = JObject.Parse(File.ReadAllText(Path.Combine(directory, "contracts/style.json")));
                foreach (var key in new[] { "track", "fill", "handle" })
                {
                    if(slider[key+"ColorToken"]?.Type!=JTokenType.String)throw new InvalidDataException("Color tokens must be strings.");
                    var token = (string)slider[key + "ColorToken"];
                    var color = (string)style["colors"]?[token ?? ""];
                    Color parsed;
                    if (color == null || !Regex.IsMatch(color,@"^#[0-9a-fA-F]{6}([0-9a-fA-F]{2})?$") || !ColorUtility.TryParseHtmlString(color, out parsed)) throw new InvalidDataException("Unknown native color token: " + token);
                    slider[key + "Color"] = color;
                }
            }
            else if (data["slider"] != null && data["slider"].Type != JTokenType.Null)
                throw new InvalidDataException("slider must be an object or null.");
            return (string)data["status"] == "draft" ? null : data;
        }

        internal static string[] ContractFiles(string directory)
        {
            var root=Path.GetFullPath(directory).TrimEnd(Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar);
            var files=new List<string>();
            foreach(var name in new[]{"style","screens","native-assembly","fixtures"})
            {
                var path=Path.Combine(root,"contracts/"+name+".json");if(!File.Exists(path))continue;
                for(var current=path;current!=null;current=Path.GetDirectoryName(current))
                {
                    if((File.GetAttributes(current)&FileAttributes.ReparsePoint)!=0)throw new InvalidDataException("Game contracts cannot traverse directory links: "+path);
                    if(current==root)break;
                }
                files.Add(Path.GetFullPath(path));
            }
            return files.ToArray();
        }

        private static Sprite ResolveSprite(JObject slider, string key, UiProjectAssetPlan library, bool load)
        {
            if(slider[key]?.Type!=JTokenType.String)throw new InvalidDataException("Slider asset IDs must be strings.");
            var id = (string)slider[key];
            var part=key=="handleAssetId"?"Handle":"Track";
            var entry = library == null ? null : library.entries.SingleOrDefault(e => string.Equals(e.definition.assetId,id,StringComparison.OrdinalIgnoreCase));
            if (entry == null || entry.definition.componentType != "Slider" || entry.definition.part!=part || !entry.definition.approved) throw new InvalidDataException("Unknown or incompatible Slider "+part+" asset: " + id);
            if (!load) return null;
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(entry.unityPath);
            if (sprite == null) throw new InvalidDataException("Native assembly Sprite missing: " + entry.unityPath);
            return sprite;
        }

        public static void ApplyRadius(Image image, Dictionary<string, string> style, JObject data)
        {
            if ((bool?)data?["rounding"]?["fromCssRadius"] != true || image.sprite == null || image.type != Image.Type.Sliced) return;
            string value;
            if (!style.TryGetValue("border-radius", out value)) return;
            float radius;
            if (!float.TryParse(value.Replace("px", "").Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out radius) || radius <= 0) return;
            if(float.IsInfinity(radius)||float.IsNaN(radius))throw new InvalidDataException("Native radius must be finite");
            var b = image.sprite.border;
            // Complex/asymmetric artwork requires explicit design adaptation.
            if (b.x > 0 && Mathf.Approximately(b.x,b.y) && Mathf.Approximately(b.x,b.z) && Mathf.Approximately(b.x,b.w))
                image.pixelsPerUnitMultiplier = b.x / radius;
        }

        public static void Apply(GameObject root, JObject data, UiProjectAssetPlan library)
        {
            var config = data?["slider"] as JObject;
            if (config == null) return;
            foreach (var slider in root.GetComponentsInChildren<Slider>(true))
            {
                var rect = slider.GetComponent<RectTransform>();
                var w = rect.rect.width; var h = rect.rect.height;
                var trackHeight = (float)config["trackHeight"]; var handleSize = (float)config["handleSize"];
                if (w <= handleSize || h < trackHeight) throw new InvalidDataException("Slider is too small for native assembly: " + slider.name);
                slider.GetComponent<Image>().color = Color.clear;
                var fill = slider.fillRect.GetComponent<Image>();
                var track = Child(slider.transform,"Img_TrackBackground").GetComponent<Image>();
                var area = Child(slider.transform,"Group_FillArea",false).GetComponent<RectTransform>();
                SetRect(track.rectTransform,0,(h-trackHeight)/2,w,trackHeight);
                SetRect(area,0,(h-trackHeight)/2,w,trackHeight);
                fill.transform.SetParent(area,false);
                fill.rectTransform.anchorMin = Vector2.zero; fill.rectTransform.anchorMax = Vector2.one;
                fill.rectTransform.offsetMin = fill.rectTransform.offsetMax = Vector2.zero;
                var sprite = ResolveSprite(config,"trackAssetId",library,true);
                foreach (var image in new[] {track,fill})
                {
                    image.sprite=sprite;image.type=Image.Type.Sliced;image.raycastTarget=false;
                    image.pixelsPerUnitMultiplier=sprite.border.x/(float)config["trackRadius"];
                }
                track.color=ReadColor(config,"trackColor");fill.color=ReadColor(config,"fillColor");
                var handleArea=slider.handleRect.parent as RectTransform;
                SetRect(handleArea,handleSize/2,h/2,w-handleSize,0);handleArea.SetAsLastSibling();
                var handle=slider.handleRect;handle.anchorMin=handle.anchorMax=new Vector2(0,.5f);
                handle.pivot=new Vector2(.5f,.5f);handle.sizeDelta=new Vector2(handleSize,handleSize);handle.anchoredPosition=Vector2.zero;
                var handleImage=handle.GetComponent<Image>();handleImage.sprite=ResolveSprite(config,"handleAssetId",library,true);
                handleImage.type=Image.Type.Simple;handleImage.color=ReadColor(config,"handleColor");
                slider.fillRect=null;slider.handleRect=null;slider.fillRect=fill.rectTransform;slider.handleRect=handle;
                slider.SetValueWithoutNotify(slider.value);
            }
        }
        private static Color ReadColor(JObject data,string key) { Color color;ColorUtility.TryParseHtmlString((string)data[key],out color);return color; }
        private static GameObject Child(Transform parent,string name,bool image=true)
        {
            var existing=parent.Find(name);if(existing!=null)return existing.gameObject;
            var go=UiTemporarySceneScope.CreateGameObject(name,image?new[]{typeof(RectTransform),typeof(CanvasRenderer),typeof(Image)}:new[]{typeof(RectTransform)});
            go.transform.SetParent(parent,false);return go;
        }
        private static void SetRect(RectTransform r,float x,float y,float w,float h)
        {
            r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);
        }
    }
}
#endif
