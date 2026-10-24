#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HtmlToUGUI.Editor
{
    // A reference provides fixture data, never static layout coordinates.
    public static class UiReferenceAudit
    {
        public static string Run(string prefabPath, string referencePath, string outputDirectory, int outputWidth = 0, int outputHeight = 0)
        {
            var source=JObject.Parse(File.ReadAllText(referencePath));
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if(prefab==null)throw new FileNotFoundException("Audit prefab missing",prefabPath);
            if((string)source["panel"]!=prefab.name || !(source["nodes"] is JArray) || !source["nodes"].Any())throw new InvalidDataException("Reference panel identity/nodes mismatch");
            var result=new JObject { ["prefab"]=prefabPath,["referenceSha256"]=HtmlUiImporter.Sha256(referencePath),["nodes"]=new JArray() };
            result["schemaVersion"]=2;result["fixture"]=source["fixture"];
            result["prefabSha256"]=HtmlUiImporter.Sha256(prefabPath);
            result["prefabMetaSha256"]=HtmlUiImporter.Sha256(prefabPath+".meta");
            result["converterVersion"]=UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(UiReferenceAudit).Assembly).version;
            result["dependencies"]=JArray.FromObject(AssetDatabase.GetDependencies(prefabPath,true));
            var controls=new List<string>();
            var snapshot=UiVisualAudit.Capture(prefab,outputDirectory,root=>
            {
                var rows=Populate(root,source);
                Canvas.ForceUpdateCanvases();
                controls=CheckControls(root);
                Canvas.ForceUpdateCanvases();
                var nodes=(JArray)result["nodes"];
                foreach(var node in source["nodes"])
                {
                    var target=Find(root,rows,node);
                    if(target==null)throw new InvalidDataException("Reference node missing: "+node["row"]+":"+node["path"]);
                    var rect=target as RectTransform;var corners=new Vector3[4];rect.GetWorldCorners(corners);
                    var origin=root.GetComponent<RectTransform>();var min=origin.InverseTransformPoint(corners[0]);var max=origin.InverseTransformPoint(corners[2]);
                    nodes.Add(new JObject { ["path"]=(string)node["path"],["row"]=(int)node["row"],
                        ["visible"]=target.gameObject.activeInHierarchy,["x"]=min.x+origin.rect.width/2,["y"]=origin.rect.height/2-max.y,
                        ["width"]=max.x-min.x,["height"]=max.y-min.y });
                }
            },source["canvasBackground"]==null?new Color32(137,147,156,255):CssColor((string)source["canvasBackground"]),outputWidth,outputHeight);
            result["width"]=snapshot.width;result["height"]=snapshot.height;result["issues"]=JArray.FromObject(snapshot.issues);
            result["viewportWidth"]=snapshot.viewportWidth;result["viewportHeight"]=snapshot.viewportHeight;
            result["nativeControls"]=JArray.FromObject(controls);result["status"]=snapshot.issues.Count==0?"passed":"failed";
            result["screenshotSha256"]=HtmlUiImporter.Sha256(Path.Combine(outputDirectory,prefab.name+".unity.png"));
            result["artifacts"]=new JArray(AssetDatabase.GetDependencies(prefabPath,true).Where(p=>p.StartsWith("Assets/",StringComparison.Ordinal)&&File.Exists(p)&&File.Exists(p+".meta"))
                .Select(p=>new JObject { ["path"]=p,["guid"]=AssetDatabase.AssetPathToGUID(p),["sha256"]=HtmlUiImporter.Sha256(p),["metaSha256"]=HtmlUiImporter.Sha256(p+".meta") }));
            if((string)result["prefabSha256"]!=HtmlUiImporter.Sha256(prefabPath))throw new InvalidDataException("Prefab changed during native audit");
            File.WriteAllText(Path.Combine(outputDirectory,prefab.name+".reference-audit.json"),result.ToString(Formatting.Indented));
            return result.ToString(Formatting.None);
        }

        public static string RunMatrix(string browserMatrixPath,string outputDirectory,string prefabRoot)
        {
            var matrix=JObject.Parse(File.ReadAllText(browserMatrixPath));
            if((string)matrix["status"]!="passed" || !(matrix["cases"] is JArray) || !matrix["cases"].Any())throw new InvalidDataException("Missing completed browser matrix");
            if(!prefabRoot.StartsWith("Assets/",StringComparison.Ordinal) || prefabRoot.Split('/').Any(p=>p==".." || p=="."))throw new InvalidDataException("Invalid formal prefab root");
            var result=new JObject { ["schemaVersion"]=1,["status"]="running",["projectName"]=matrix["projectName"],["browserMatrixSha256"]=HtmlUiImporter.Sha256(browserMatrixPath),["cases"]=new JArray() };
            Directory.CreateDirectory(outputDirectory);var output=Path.Combine(outputDirectory,"matrix.json");
            File.WriteAllText(output,result.ToString(Formatting.Indented));
            foreach(var item in matrix["cases"])
            {
                var panel=(string)item["panel"];var fixture=(string)item["fixture"];
                if(!Regex.IsMatch(panel??"",@"^[A-Z][A-Za-z0-9]*Panel$") || !Regex.IsMatch(fixture??"",@"^[A-Za-z][A-Za-z0-9_-]*$"))throw new InvalidDataException("Unsafe matrix identity");
                var relative=(string)item["reference"];
                if(string.IsNullOrEmpty(relative)||Path.IsPathRooted(relative)||relative.Replace('\\','/').Split('/').Any(p=>p==".."||p=="."))throw new InvalidDataException("Unsafe browser reference path");
                var reference=Path.Combine(Path.GetDirectoryName(Path.GetFullPath(browserMatrixPath)),relative);
                if(HtmlUiImporter.Sha256(reference)!=(string)item["referenceSha256"])throw new InvalidDataException("Browser matrix reference changed");
                foreach(var size in matrix["viewports"])
                {
                    var width=(int)size[0];var height=(int)size[1];var viewport=width+"x"+height;
                    var directory=Path.Combine(outputDirectory,panel,fixture,viewport);
                    var record=new JObject { ["panel"]=panel,["fixture"]=fixture,["viewport"]=viewport,["reference"]=relative,
                        ["audit"]=panel+"/"+fixture+"/"+viewport+"/"+panel+".reference-audit.json",["screenshot"]=panel+"/"+fixture+"/"+viewport+"/"+panel+".unity.png" };
                    try
                    {
                        var audit=JObject.Parse(Run(prefabRoot.TrimEnd('/')+"/"+panel+"/"+panel+".prefab",reference,directory,width,height));
                        record["status"]=audit["status"];
                    }
                    catch(Exception error){record["status"]="failed";record["error"]=error.ToString();}
                    ((JArray)result["cases"]).Add(record);
                }
                File.WriteAllText(output,result.ToString(Formatting.Indented));
            }
            result["status"]=result["cases"].All(c=>(string)c["status"]=="passed")?"passed":"failed";
            File.WriteAllText(output,result.ToString(Formatting.Indented));return result.ToString(Formatting.None);
        }

        private static List<Transform> Populate(GameObject root,JObject source)
        {
            var rows=new List<Transform>();var catalog=root.GetComponent<HtmlToUGUI.UiTemplateCatalog>();
            foreach(var row in source["templates"] ?? new JArray())
            {
                var list=root.transform.Find((string)row["parent"]).GetComponent<ScrollRect>();
                var map=catalog.CreateItem((string)row["name"],list.content);var rect=map.GetComponent<RectTransform>();
                rect.anchorMin=rect.anchorMax=new Vector2(0,1);rect.pivot=new Vector2(0,1);rect.anchoredPosition=new Vector2((float)row["x"],-(float)row["y"]);
                list.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,(float)row["contentHeight"]);
                var group=map.GetComponent<CanvasGroup>();
                if(group==null)group=map.gameObject.AddComponent<CanvasGroup>();
                group.alpha=(float?)row["opacity"]??1;
                rows.Add(map.transform);
            }
            foreach(var node in source["nodes"])
            {
                var target=Find(root,rows,node);if(target==null)continue;
                target.gameObject.SetActive((bool)node["visible"]);
                var bound=node["key"]?.Type==JTokenType.String || (int)node["row"]>=0;
                var text=target.GetComponent<TMP_Text>();
                if(text!=null && bound) { text.text=(string)node["text"]??"";text.color=CssColor((string)node["color"]); }
                var input=target.GetComponent<TMP_InputField>();if(input!=null)input.SetTextWithoutNotify((string)node["value"]??"");
                var slider=target.GetComponent<Slider>();if(slider!=null)
                {
                    if(node["range"] is JObject) {slider.minValue=(float)node["range"]["min"];slider.maxValue=(float)node["range"]["max"];slider.wholeNumbers=(bool)node["range"]["wholeNumbers"];}
                    slider.SetValueWithoutNotify(float.Parse((string)node["value"],System.Globalization.CultureInfo.InvariantCulture));
                }
                var toggle=target.GetComponent<Toggle>();if(toggle!=null)toggle.SetIsOnWithoutNotify((bool?)node["checked"]??false);
                var selectable=target.GetComponent<Selectable>();if(selectable!=null)selectable.interactable=!(bool)node["disabled"];
                var image=target.GetComponent<Image>();
                if(image!=null && bound && toggle==null && slider==null && input==null)
                {
                    var mask=(string)node["mask"];
                    if(mask!=null && mask.Contains(".png"))
                    {
                        var file=Regex.Match(mask,@"([^/""()]+)\.png").Groups[1].Value;
                        var sourceImage=root.GetComponent<HtmlToUGUI.UiBindingMap>().Get<Image>("Source."+file);
                        if(sourceImage!=null)image.sprite=sourceImage.sprite;
                    }
                    image.color=CssColor((string)node["background"]);
                }
                var visuals=target.GetComponent<HtmlToUGUI.UiToggleStateGraphic>();if(visuals!=null)visuals.Refresh();
            }
            ApplyPresentation(root,source);
            return rows;
        }
        public static void ApplyPresentation(GameObject root,JObject source)
        {
            var map=root.GetComponent<HtmlToUGUI.UiBindingMap>();
            foreach(var rule in source["presentationRules"]??new JArray())
            {
                var binding=(string)rule["targetBinding"];var component=map.Get(binding);
                if(component==null)throw new InvalidDataException("Presentation target binding missing: "+binding);
                var rect=component.transform as RectTransform;
                var kind=(string)rule["kind"];
                if(kind=="text-length")
                {
                    if(rule["threshold"]?.Type!=JTokenType.Integer || (long)rule["threshold"]<0 || (long)rule["threshold"]>int.MaxValue)throw new InvalidDataException("Invalid text-length threshold");
                    var text=map.Get<TMP_Text>((string)rule["sourceBinding"]);
                    if(text==null)throw new InvalidDataException("Presentation source text missing");
                    var settings=rule[text.text.Length>(int)rule["threshold"]?"long":"short"];
                    if(!(settings is JObject))throw new InvalidDataException("Presentation settings must be an object");
                    if(settings["fontSize"]!=null)
                    {
                        var target=component as TMP_Text;if(target==null)throw new InvalidDataException("Font size needs a text binding");
                        target.fontSize=PresentationNumber(settings["fontSize"]);
                        target.enableAutoSizing=settings["fontSizeMin"]!=null;
                        if(target.enableAutoSizing)
                        {
                            target.fontSizeMax=target.fontSize;target.fontSizeMin=PresentationNumber(settings["fontSizeMin"]);
                            if(target.fontSizeMin>target.fontSizeMax)throw new InvalidDataException("Minimum text size exceeds its maximum");
                        }
                    }
                    if(settings["height"]!=null)rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,PresentationNumber(settings["height"]));
                    if(settings["canvasY"]!=null)
                    {
                        var origin=root.GetComponent<RectTransform>();var corners=new Vector3[4];rect.GetWorldCorners(corners);
                        var current=origin.rect.height/2-origin.InverseTransformPoint(corners[1]).y;
                        var delta=rect.parent.InverseTransformVector(origin.TransformVector(new Vector3(0,current-PresentationNumber(settings["canvasY"],false),0)));
                        rect.anchoredPosition+=new Vector2(delta.x,delta.y);
                    }
                }
                else if(kind=="text-height")
                {
                    var text=map.Get<TMP_Text>((string)rule["sourceBinding"]);
                    if(text==null)throw new InvalidDataException("Presentation source text missing");
                    var height=Mathf.Max(PresentationNumber(rule["minimumHeight"]),Mathf.Ceil(text.text.Length/PresentationNumber(rule["charactersPerLine"]))*PresentationNumber(rule["lineHeight"]));
                    rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,height);
                    var scroll=rect.GetComponentInParent<ScrollRect>();
                    if(scroll!=null)scroll.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,Mathf.Max(scroll.viewport.rect.height,height));
                }
                else if(kind=="ratio-width")
                {
                    var numerator=PresentationNumber(source["presentationData"][(string)rule["numerator"]],false);
                    var denominator=PresentationNumber(source["presentationData"][(string)rule["denominator"]]);
                    rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,Mathf.Clamp01(numerator/denominator)*PresentationNumber(rule["maximumWidth"]));
                }
                else throw new InvalidDataException("Unknown presentation rule: "+kind);
            }
        }
        private static float PresentationNumber(JToken value,bool positive=true)
        {
            if(value==null || (value.Type!=JTokenType.Integer && value.Type!=JTokenType.Float))throw new InvalidDataException("Presentation values must be numbers");
            var number=(float)value;
            if(float.IsInfinity(number)||float.IsNaN(number)||(positive?number<=0:number<0))throw new InvalidDataException("Presentation values must be finite and nonnegative");
            return number;
        }
        private static Transform Find(GameObject root,List<Transform> rows,JToken node)
        {
            var row=(int)node["row"];var parent=row<0?root.transform:rows[row];var path=(string)node["path"];
            return string.IsNullOrEmpty(path)?parent:parent.Find(path);
        }
        private static Color CssColor(string value)
        {
            Color parsed;if(value!=null && value.StartsWith("#") && ColorUtility.TryParseHtmlString(value,out parsed))return parsed;
            var numbers=Regex.Matches(value??"",@"[\d.]+").Cast<Match>().Select(m=>float.Parse(m.Value,System.Globalization.CultureInfo.InvariantCulture)).ToArray();
            return numbers.Length<3?Color.white:new Color(numbers[0]/255,numbers[1]/255,numbers[2]/255,numbers.Length>3?numbers[3]:1);
        }
        private static List<string> CheckControls(GameObject root)
        {
            var results=new List<string>();
            var eventGo=UiTemporarySceneScope.CreateGameObject("NativeAuditEvents",typeof(EventSystem));
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(eventGo,root.scene);
            try
            {
                var events=eventGo.GetComponent<EventSystem>();
                foreach(var button in root.GetComponentsInChildren<Button>(true))
                {
                    if(!button.gameObject.activeInHierarchy)continue;
                    var old=button.onClick;var enabled=button.interactable;var calls=0;
                    button.onClick=new Button.ButtonClickedEvent();button.onClick.AddListener(()=>calls++);
                    try
                    {
                        button.interactable=true;button.OnPointerClick(new PointerEventData(events){button=PointerEventData.InputButton.Left});
                        button.interactable=false;button.OnPointerClick(new PointerEventData(events){button=PointerEventData.InputButton.Left});
                        if(calls!=1)throw new InvalidDataException("Button native click/disabled failed: "+button.name);
                        results.Add(button.name+": pointer click + disabled");
                    }
                    finally {button.onClick=old;button.interactable=enabled;}
                }
                foreach(var toggle in root.GetComponentsInChildren<Toggle>(true))
                {
                    if(!toggle.gameObject.activeInHierarchy)continue;
                    var enabled=toggle.interactable;var value=toggle.isOn;var old=toggle.onValueChanged;toggle.onValueChanged=new Toggle.ToggleEvent();
                    try
                    {
                        toggle.interactable=true;toggle.OnPointerClick(new PointerEventData(events){button=PointerEventData.InputButton.Left});
                        if(toggle.isOn==value)throw new InvalidDataException("Toggle pointer failed");
                        toggle.interactable=false;var current=toggle.isOn;toggle.OnPointerClick(new PointerEventData(events){button=PointerEventData.InputButton.Left});
                        if(toggle.isOn!=current)throw new InvalidDataException("Disabled Toggle changed");
                        results.Add(toggle.name+": pointer + disabled");
                    }
                    finally {toggle.SetIsOnWithoutNotify(value);toggle.interactable=enabled;toggle.onValueChanged=old;var graphic=toggle.GetComponent<HtmlToUGUI.UiToggleStateGraphic>();if(graphic!=null)graphic.Refresh();}
                }
                foreach(var slider in root.GetComponentsInChildren<Slider>(true))
                {
                    if(!slider.gameObject.activeInHierarchy)continue;
                    var value=slider.value;var enabled=slider.interactable;var old=slider.onValueChanged;slider.onValueChanged=new Slider.SliderEvent();
                    try
                    {
                        slider.interactable=true;
                        if(slider.handleRect==null || slider.fillRect==null)throw new InvalidDataException("Slider parts missing: "+slider.name);
                        var handleSize=slider.handleRect.sizeDelta;
                        foreach(var amount in new[]{slider.minValue,(slider.minValue+slider.maxValue)/2,slider.maxValue})
                        {slider.SetValueWithoutNotify(amount);if(slider.handleRect==null || slider.fillRect==null || slider.handleRect.sizeDelta!=handleSize)throw new InvalidDataException("Slider parts/handle dimensions changed");}
                        slider.SetValueWithoutNotify(slider.minValue);slider.OnMove(new AxisEventData(events){moveDir=MoveDirection.Right});
                        if(slider.maxValue>slider.minValue && slider.value<=slider.minValue)throw new InvalidDataException("Slider keyboard failed");
                        results.Add(slider.name+": min/mid/max + keyboard");
                    }
                    finally {slider.SetValueWithoutNotify(value);slider.interactable=enabled;slider.onValueChanged=old;}
                }
                foreach(var input in root.GetComponentsInChildren<TMP_InputField>(true))
                {
                    if(!input.gameObject.activeInHierarchy)continue;
                    var text=input.text;input.SetTextWithoutNotify("1");
                    if(input.text!="1")throw new InvalidDataException("Input text assignment failed");input.SetTextWithoutNotify(text);
                    results.Add(input.name+": editable text (business validation excluded)");
                }
                foreach(var scroll in root.GetComponentsInChildren<ScrollRect>(true))
                {
                    if(scroll.content==null || scroll.viewport==null)throw new InvalidDataException("ScrollRect references missing");
                    var position=scroll.content.anchoredPosition;scroll.verticalNormalizedPosition=0;scroll.verticalNormalizedPosition=1;scroll.content.anchoredPosition=position;
                    results.Add(scroll.name+": content/viewport + normalized scroll");
                }
            }
            finally {UnityEngine.Object.DestroyImmediate(eventGo);}
            return results;
        }
    }
}
#endif
