#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace HtmlToUGUI.Editor
{
    public static class UiWorkflowRegressionChecks
    {
        public static string Run(string evidenceDirectory)
        {
            var passed=new List<string>();
            Action<bool,string> check=(ok,label)=>{if(!ok)throw new InvalidDataException(label);passed.Add(label);};
            var token=Guid.NewGuid().ToString("N");var root=UiRegressionWorkspace.Root+"/Workflow-"+token;
            var dir=Path.Combine(Path.GetTempPath(),"Workflow-"+token);Directory.CreateDirectory(Path.Combine(dir,"contracts"));
            var name=Path.GetFileName(dir);var path=Path.Combine(dir,"contracts/native-assembly.json");
            GameObject go=null;
            try
            {
                using(var scope=new UiTemporarySceneScope())
                {
                    UiStageWorkspace.EnsureFolder(root);
                    var texture=new Texture2D(64,64,TextureFormat.RGBA32,false);
                    File.WriteAllBytes(root+"/surface.png",texture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(texture);
                    AssetDatabase.ImportAsset(root+"/surface.png");var importer=(TextureImporter)AssetImporter.GetAtPath(root+"/surface.png");
                    importer.textureType=TextureImporterType.Sprite;importer.spriteBorder=new Vector4(20,20,20,20);importer.SaveAndReimport();
                    var library=new UiProjectAssetPlan();
                    foreach(var id in new[]{"Track","Handle"})library.entries.Add(new UiProjectAssetEntry {definition=new UiProjectAssetDefinition {assetId=id,componentType="Slider",part=id,approved=true},unityPath=root+"/surface.png"});
                    File.WriteAllText(Path.Combine(dir,"contracts/style.json"),"{\"colors\":{\"card\":\"#203745\",\"teal\":\"#4AD7BB\",\"text\":\"#EEF5F7\"}}");
                    var config=JObject.FromObject(new {schemaVersion=1,projectName=name,status="active",rounding=new {fromCssRadius=true},
                        slider=new {trackHeight=10,handleSize=28,trackRadius=5,trackAssetId="Track",handleAssetId="Handle",trackColorToken="card",fillColorToken="teal",handleColorToken="text"}});
                    File.WriteAllText(path,config.ToString());var loaded=UiNativeAssembly.Load(dir,library);
                    check(loaded!=null,"Active native assembly loads existing color tokens and Slider assets");
                    go=UiTemporarySceneScope.CreateGameObject("SyntheticSlider",typeof(RectTransform),typeof(Image),typeof(Slider));
                    go.GetComponent<RectTransform>().sizeDelta=new Vector2(600,40);
                    var fill=UiTemporarySceneScope.CreateGameObject("Img_Fill",typeof(RectTransform),typeof(Image));fill.transform.SetParent(go.transform,false);
                    var area=UiTemporarySceneScope.CreateGameObject("Group_HandleArea",typeof(RectTransform));area.transform.SetParent(go.transform,false);
                    var handle=UiTemporarySceneScope.CreateGameObject("Img_Handle",typeof(RectTransform),typeof(Image));handle.transform.SetParent(area.transform,false);
                    var slider=go.GetComponent<Slider>();slider.fillRect=fill.GetComponent<RectTransform>();slider.handleRect=handle.GetComponent<RectTransform>();slider.targetGraphic=handle.GetComponent<Image>();
                    UiNativeAssembly.Apply(go,loaded,library);var count=go.GetComponentsInChildren<RectTransform>(true).Length;
                    UiNativeAssembly.Apply(go,loaded,library);
                    check(go.GetComponentsInChildren<RectTransform>(true).Length==count,"Native assembly replay is idempotent and creates no duplicate parts");
                    check(slider.handleRect.sizeDelta==new Vector2(28,28),"Replay preserves independent square handle dimensions");
                    check(go.transform.Find("Img_TrackBackground").GetComponent<RectTransform>().rect.height==10,"Replay uses configured track height");
                    check(slider.handleRect.parent.GetSiblingIndex()==go.transform.childCount-1,"Handle is drawn after track and fill");
                    var image=fill.GetComponent<Image>();image.type=Image.Type.Sliced;
                    UiNativeAssembly.ApplyRadius(image,new Dictionary<string,string>{{"border-radius","10px"}},loaded);
                    check(Mathf.Approximately(image.pixelsPerUnitMultiplier,2),"CSS radius and symmetric Sprite border determine native PPU multiplier");
                    foreach(var bad in new JToken[]{new JValue(true),new JValue(0),new JValue(-1),new JValue("10")})
                    {
                        config["slider"]["trackHeight"]=bad;File.WriteAllText(path,config.ToString());var rejected=false;
                        try{UiNativeAssembly.Load(dir,library);}catch(Exception){rejected=true;}
                        check(rejected,"Native contract rejects invalid track height: "+bad.ToString(Formatting.None));
                    }
                    config["slider"]["trackHeight"]=10;config["status"]="draft";File.WriteAllText(path,config.ToString());
                    check(UiNativeAssembly.Load(dir,library)==null,"Draft recipe does not change generated native controls");
                    config["status"]="active";config["slider"]["trackAssetId"]="Unknown";File.WriteAllText(path,config.ToString());var missing=false;
                    try{UiNativeAssembly.Load(dir,library);}catch(InvalidDataException){missing=true;}
                    check(missing,"Native contract refuses unknown or cross-component assets");
                    config["slider"]["trackAssetId"]="Track";
                    var baseline=new JObject { ["config"]=config.DeepClone(),["style"]=JObject.Parse(File.ReadAllText(Path.Combine(dir,"contracts/style.json"))),
                        ["assets"]=new JObject { ["Track"]=JObject.FromObject(library.entries[0].definition),["Handle"]=JObject.FromObject(library.entries[1].definition) } };
                    var package=UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(UiWorkflowRegressionChecks).Assembly).resolvedPath;
                    var cases=JObject.Parse(File.ReadAllText(Path.Combine(package,"Editor/Core/NativeAssemblyContractCases.json")));
                    foreach(var test in cases["cases"])
                    {
                        var data=(JObject)baseline.DeepClone();
                        foreach(var change in ((JObject)test["changes"]).Properties())
                        {
                            var parts=change.Name.Split('.');JToken target=data;
                            for(var i=0;i<parts.Length-1;i++)target=target[parts[i]];
                            target[parts[parts.Length-1]]=change.Value.DeepClone();
                        }
                        File.WriteAllText(path,data["config"].ToString());File.WriteAllText(Path.Combine(dir,"contracts/style.json"),data["style"].ToString());
                        var testLibrary=new UiProjectAssetPlan();
                        foreach(var asset in ((JObject)data["assets"]).Properties())testLibrary.entries.Add(new UiProjectAssetEntry {definition=asset.Value.ToObject<UiProjectAssetDefinition>(),unityPath=root+"/surface.png"});
                        JObject actual=null;var rejected=false;
                        try {actual=UiNativeAssembly.Load(dir,testLibrary);}catch(Exception){rejected=true;}
                        var expected=(string)test["expected"];
                        check(expected=="rejected"?rejected:!rejected && (expected=="ignored"?actual==null:actual!=null),"Shared contract case: "+(string)test["id"]);
                    }
                    var map=go.AddComponent<HtmlToUGUI.UiBindingMap>();
                    var label=UiTemporarySceneScope.CreateGameObject("FixtureName",typeof(RectTransform),typeof(TMPro.TextMeshProUGUI));label.transform.SetParent(go.transform,false);
                    var description=UiTemporarySceneScope.CreateGameObject("FixtureDescription",typeof(RectTransform),typeof(TMPro.TextMeshProUGUI));description.transform.SetParent(go.transform,false);
                    label.GetComponent<TMPro.TMP_Text>().text=new string('x',22);description.GetComponent<TMPro.TMP_Text>().text=new string('x',96);
                    map.EditorSetEntries(new List<HtmlToUGUI.UiBindingMap.Entry> {new HtmlToUGUI.UiBindingMap.Entry {key="Name",target=label.GetComponent<TMPro.TMP_Text>()},
                        new HtmlToUGUI.UiBindingMap.Entry {key="Description",target=description.GetComponent<TMPro.TMP_Text>()},new HtmlToUGUI.UiBindingMap.Entry {key="Fill",target=fill.GetComponent<Image>()}});
                    var presentation=JObject.Parse("{\"presentationData\":{\"weight\":8,\"capacity\":10},\"presentationRules\":[{\"kind\":\"text-length\",\"sourceBinding\":\"Name\",\"targetBinding\":\"Name\",\"threshold\":14,\"short\":{\"fontSize\":32,\"height\":50},\"long\":{\"fontSize\":22,\"height\":86}},{\"kind\":\"text-height\",\"sourceBinding\":\"Description\",\"targetBinding\":\"Description\",\"minimumHeight\":76,\"charactersPerLine\":16,\"lineHeight\":32},{\"kind\":\"ratio-width\",\"targetBinding\":\"Fill\",\"numerator\":\"weight\",\"denominator\":\"capacity\",\"maximumWidth\":436}]}");
                    UiReferenceAudit.ApplyPresentation(go,presentation);
                    check(label.GetComponent<TMPro.TMP_Text>().fontSize==22 && label.GetComponent<RectTransform>().rect.height==86,"Long text fixture uses declared type size and height");
                    check(description.GetComponent<RectTransform>().rect.height==192,"Long description fixture expands its native text rectangle");
                    check(Mathf.Abs(fill.GetComponent<RectTransform>().rect.width-348.8f)<.01f,"Fixture progress width uses data ratio without copying expected geometry");
                    presentation["presentationData"]["capacity"]=0;var zero=false;
                    try{UiReferenceAudit.ApplyPresentation(go,presentation);}catch(InvalidDataException){zero=true;}
                    check(zero,"Invalid presentation ratios are rejected");
                }
            }
            finally
            {
                if(go!=null)UnityEngine.Object.DestroyImmediate(go);
                if(AssetDatabase.IsValidFolder(root))AssetDatabase.DeleteAsset(root);
                UiRegressionWorkspace.RemoveEmptyRoot();
                if(Directory.Exists(dir))Directory.Delete(dir,true);
            }
            Directory.CreateDirectory(evidenceDirectory);
            var result=JsonConvert.SerializeObject(new {count=passed.Count,passed},Formatting.Indented);
            File.WriteAllText(Path.Combine(evidenceDirectory,"workflow-regression.json"),result);return result;
        }
    }
}
#endif
