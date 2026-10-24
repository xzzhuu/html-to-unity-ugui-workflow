#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using HtmlToUGUI;
using HtmlToUGUI.Editor;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Editor-only business adapter. It never attaches a script to delivered Prefabs.
namespace StarportSalvage.Editor
{
    public static class StarportEditorFlowChecks
    {
        const string Prefabs="Assets/Art/UIPrefab/StarportSalvage/";
        static readonly string[] Panels={"StarportHomePanel","ExpeditionSelectPanel","SupplyInventoryPanel","GameSettingsPanel","ActionConfirmPanel","ExpeditionResultPanel"};
        public static string Sha(string path)
        {
            using(var hash=SHA256.Create())return BitConverter.ToString(hash.ComputeHash(File.ReadAllBytes(path))).Replace("-","").ToLowerInvariant();
        }
        internal sealed class Session
        {
            readonly JObject data;
            internal JObject state;
            internal JObject draft;
            internal string panel="StarportHomePanel",origin="StarportHomePanel",confirmOrigin,mode,selected="medkit",mission="cargo",category="All",search="",feedback="",dataState="normal";
            internal int quantity=1;
            internal double now;
            internal Session(JObject source){data=source;Reset();}
            internal void Reset(){state=(JObject)data["initial"].DeepClone();state["inventory"]=data["items"].DeepClone();state["loadout"]=new JObject();state["settings"]=JObject.Parse("{sound:true,volume:80,quality:'High'}");state["active"]=null;state["lastResult"]=null;draft=null;mode=null;panel="StarportHomePanel";selected="medkit";mission="cargo";category="All";search="";quantity=1;feedback="";dataState="normal";origin="StarportHomePanel";confirmOrigin=null;}
            internal JArray Inventory=>(JArray)state["inventory"];
            internal JObject Loadout=>(JObject)state["loadout"];
            internal JObject Item(string id)=>Inventory.OfType<JObject>().FirstOrDefault(i=>(string)i["id"]==id);
            internal JObject MissionFor(string id)=>data["missions"].OfType<JObject>().First(m=>(string)m["id"]==id);
            internal JObject Mission=>MissionFor(mission);
            internal int Count(string id)=>(int?)Loadout[id]??0;
            internal float Weight=>Loadout.Properties().Sum(p=>((float?)Item(p.Name)?["weight"]??0)*(int)p.Value);
            internal bool Locked=>state["active"] is JObject;
            internal int Available(JObject item)=>Math.Max(0,(int)item["qty"]-Count((string)item["id"]));
            internal List<JObject> Filtered=>Inventory.OfType<JObject>().Where(i=>(category=="All"||(string)i["category"]==category)&&((string)i["name"]).Contains(search.Trim())).ToList();
            internal string Summary=>string.Join("、",Loadout.Properties().Select(p=>(string)Item(p.Name)["name"]+"×"+(int)p.Value));
            internal string Deficit()
            {
                var errors=new List<string>();var m=Mission;
                if(!(bool)m["unlocked"])errors.Add("未解锁");
                if((int)state["credits"]<(int)m["credits"])errors.Add("信用点不足");
                if((int)state["energy"]<(int)m["energy"])errors.Add("能源不足");
                if(Weight>(float)state["capacity"])errors.Add("携行超重");
                if(Locked)errors.Add("请先完成当前远征与结算");
                if(dataState!="normal")errors.Add("任务数据尚未就绪");
                return string.Join("、",errors);
            }
            internal void Go(string next){feedback="";if(next=="SupplyInventoryPanel")origin=panel;panel=next;if(next=="GameSettingsPanel"){draft=(JObject)state["settings"].DeepClone();draft["captain"]=state["captain"];}}
            internal void Confirm(string action){if(action=="start"&&Deficit()!=""){feedback=Deficit();return;}mode=action;confirmOrigin=panel;panel="ActionConfirmPanel";}
            internal void Cancel(){panel=confirmOrigin??origin;mode=null;}
            internal void Commit()
            {
                if(mode=="start")
                {
                    feedback=Deficit();if(feedback==""){state["credits"]=(int)state["credits"]-(int)Mission["credits"];state["energy"]=(int)state["energy"]-(int)Mission["energy"];state["active"]=new JObject{["missionId"]=mission,["phase"]="running",["endsAt"]=now+(double)data["durationSeconds"],["loadout"]=Loadout.DeepClone()};}
                }
                else if(mode=="clear"){if(!Locked)Loadout.RemoveAll();else feedback="任务期间无法修改携行";}
                else if(mode=="reset"){Reset();return;}
                Cancel();
            }
            internal void Tick(double seconds){if(seconds<0)throw new ArgumentOutOfRangeException(nameof(seconds));now+=seconds;if(state["active"] is JObject a&&(string)a["phase"]=="running"&&now>=(double)a["endsAt"])a["phase"]="pending";}
            internal void Add()
            {
                var item=Item(selected);if(Locked||item==null||(string)item["category"]!="Supply"||quantity<1||quantity>Available(item)){feedback="无法加入补给";return;}
                if(Weight+(float)item["weight"]*quantity>(float)state["capacity"]){feedback="携行超重";return;}
                Loadout[selected]=Count(selected)+quantity;feedback="已加入补给";
            }
            internal void Remove(string id,bool all){if(Locked)return;var n=all?0:Count(id)-1;if(n<=0)Loadout.Remove(id);else Loadout[id]=n;}
            internal void Claim()
            {
                if(!(state["active"] is JObject a)||(string)a["phase"]!="pending")return;
                var m=data["missions"].OfType<JObject>().First(v=>(string)v["id"]==(string)a["missionId"]);
                foreach(var reward in m["rewards"]){var id=(string)reward[0];var n=(int)reward[1];if(id=="credits")state["credits"]=(int)state["credits"]+n;else Item(id)["qty"]=(int)Item(id)["qty"]+n;}
                if(((int?)a["loadout"]["medkit"]??0)>0){Item("medkit")["qty"]=Math.Max(0,(int)Item("medkit")["qty"]-1);RemoveAfterClaim();}
                state["lastResult"]=a.DeepClone();state["active"]=null;panel="StarportHomePanel";
            }
            void RemoveAfterClaim(){var n=Math.Max(0,Count("medkit")-1);if(n==0)Loadout.Remove("medkit");else Loadout["medkit"]=n;}
            internal void Apply()
            {
                var name=((string)draft["captain"]).Trim();var count=ScalarCount(name);
                if(count<1||count>12||name.Any(char.IsControl)){feedback="名称须为1～12个字符且不含控制字符";return;}
                state["captain"]=name;var settings=(JObject)draft.DeepClone();settings.Remove("captain");state["settings"]=settings;panel="StarportHomePanel";feedback="";
            }
            internal static int ScalarCount(string text){var n=0;for(var i=0;i<text.Length;i++,n++)if(char.IsHighSurrogate(text[i])&&i+1<text.Length&&char.IsLowSurrogate(text[i+1]))i++;return n;}
            internal void Escape(){if(panel=="ActionConfirmPanel")Cancel();else panel=panel=="SupplyInventoryPanel"?origin:"StarportHomePanel";}
        }

        // Every interaction goes through native UGUI/TMP events on actual delivered instances.
        internal sealed class Adapter
        {
            internal readonly GameObject root;
            internal readonly UiBindingMap map;
            readonly Session s;
            readonly JObject style=JObject.Parse(File.ReadAllText(Path.Combine(Workspace,"contracts/style.json")));
            internal readonly HashSet<string> wired=new HashSet<string>();
            readonly string current;
            internal Adapter(GameObject instance,Session session){root=instance;map=root.GetComponent<UiBindingMap>();s=session;current=root.name.Replace("(Clone)","");Wire();Render();}
            internal void Text(string key,object value){var t=map.Get<TMP_Text>(key);if(t!=null)t.text=Convert.ToString(value,System.Globalization.CultureInfo.InvariantCulture);}
            void Icon(UiBindingMap destination,string key,string file)
            {
                var image=destination.Get<Image>(key);var source=map.Get<Image>("Source."+Path.GetFileNameWithoutExtension(file));
                if(image==null)return;
                if(source!=null)image.sprite=source.sprite;
                else {var path=AssetDatabase.FindAssets(Path.GetFileNameWithoutExtension(file)+" t:Sprite",new[]{"Assets/Art/UI/Sprites/StarportSalvage"}).Select(AssetDatabase.GUIDToAssetPath).FirstOrDefault(p=>Path.GetFileName(p)==file);if(path!=null)image.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(path);}
            }
            void Visible(string key,bool value){var t=map.Get(key);if(t!=null)t.gameObject.SetActive(value);}
            void Enabled(string key,bool value){var t=map.Get<Selectable>(key);if(t!=null)t.interactable=value;}
            void Choice(string key,bool selected)
            {
                var button=map.Get<Button>(key);if(button==null)return;
                Color background,foreground;ColorUtility.TryParseHtmlString((string)style["colors"][selected?"teal":"card"],out background);ColorUtility.TryParseHtmlString((string)style["colors"][selected?"ink":"text"],out foreground);
                button.image.color=background;var label=button.GetComponentInChildren<TMP_Text>();if(label!=null)label.color=foreground;
            }
            void Button(string key,Action action){var b=map.Get<Button>(key);if(b==null)return;b.onClick.RemoveAllListeners();b.onClick.AddListener(()=>{action();if(s.panel==current)Render();});wired.Add(key);}
            void Input(string key,Action<string> action){var t=map.Get<TMP_InputField>(key);if(t==null)return;t.onValueChanged.RemoveAllListeners();t.onValueChanged.AddListener(v=>{action(v);Render();});wired.Add(key);}
            void Wire()
            {
                Button("Action.OpenExpeditions",()=>s.Go("ExpeditionSelectPanel"));Button("Action.OpenInventory",()=>s.Go("SupplyInventoryPanel"));Button("Action.OpenSettings",()=>s.Go("GameSettingsPanel"));
                Button("Action.ReturnHome",()=>s.Go("StarportHomePanel"));Button("Action.EditLoadout",()=>s.Go("SupplyInventoryPanel"));Button("Action.Return",()=>s.Go(s.origin));
                Button("Action.ViewResult",()=>s.Go("ExpeditionResultPanel"));Button("Action.Postpone",()=>s.Go("StarportHomePanel"));Button("Action.ClaimRewards",s.Claim);
                Button("Action.ResetDemo",()=>s.Confirm("reset"));Button("Action.StartExpedition",()=>s.Confirm("start"));Button("Action.ClearLoadout",()=>s.Confirm("clear"));
                Button("Action.CancelConfirm",s.Cancel);Button("Action.CloseConfirm",s.Cancel);Button("Action.Confirm",s.Commit);
                Button("Action.RetryMissions",()=>s.dataState="normal");
                foreach(var cat in new[]{"All","Material","Supply"}){var value=cat;Button("Action.Category"+value,()=>s.category=value);}
                Input("Filter.Search",v=>s.search=v);Button("Action.ClearSearch",()=>s.search="");
                Button("Action.DecreaseQuantity",()=>s.quantity--);Button("Action.IncreaseQuantity",()=>s.quantity++);Button("Action.AddSupply",s.Add);
                var slider=map.Get<Slider>("Supply.AddQuantity");if(slider!=null){slider.onValueChanged.RemoveAllListeners();slider.onValueChanged.AddListener(v=>{s.quantity=Mathf.RoundToInt(v);Render();});wired.Add("Supply.AddQuantity");}
                Input("Settings.CaptainName",v=>{s.draft["captain"]=v;s.feedback="";});
                var sound=map.Get<Toggle>("Settings.SoundEnabled");if(sound!=null){sound.onValueChanged.RemoveAllListeners();sound.onValueChanged.AddListener(v=>{s.draft["sound"]=v;Render();});wired.Add("Settings.SoundEnabled");}
                var volume=map.Get<Slider>("Settings.MasterVolume");if(volume!=null){volume.onValueChanged.RemoveAllListeners();volume.onValueChanged.AddListener(v=>{s.draft["volume"]=Mathf.RoundToInt(v);Render();});wired.Add("Settings.MasterVolume");}
                foreach(var quality in new[]{"Low","Medium","High"}){var value=quality;Button("Action.Quality"+value,()=>s.draft["quality"]=value);}
                Button("Action.ResetDefaults",()=>s.draft=JObject.Parse("{captain:'林岚',sound:true,volume:80,quality:'High'}"));Button("Action.CancelSettings",()=>s.Go("StarportHomePanel"));Button("Action.ApplySettings",s.Apply);
            }
            List<UiBindingMap> Rows(string key,string template,int count,float step)
            {
                var scroll=map.Get<ScrollRect>(key);var result=new List<UiBindingMap>();if(scroll==null)return result;
                for(var i=scroll.content.childCount-1;i>=0;i--)UnityEngine.Object.DestroyImmediate(scroll.content.GetChild(i).gameObject);
                for(var i=0;i<count;i++){var row=root.GetComponent<UiTemplateCatalog>().CreateItem(template,scroll.content);var rect=row.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);rect.anchoredPosition=new Vector2(0,-step*i);result.Add(row);}
                scroll.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,Mathf.Max(scroll.viewport.rect.height,step*count));return result;
            }
            static void RowText(UiBindingMap map,string key,object value){var t=map.Get<TMP_Text>(key);if(t!=null)t.text=Convert.ToString(value);}
            void RowButton(UiBindingMap row,string key,Action action){var b=row.Get<Button>(key);if(b==null)return;b.interactable=!s.Locked;b.onClick.RemoveAllListeners();b.onClick.AddListener(()=>{action();Render();});}
            internal void Render()
            {
                Text("Player.Credits",((int)s.state["credits"]).ToString("N0",System.Globalization.CultureInfo.InvariantCulture));Text("Player.Energy",s.state["energy"]+" / 100");Text("Player.LoadoutWeight",s.Weight.ToString("0.0")+" / "+s.state["capacity"]+" kg");Text("Feedback.Message",s.feedback);
                if(current=="StarportHomePanel")
                {
                    var phase=(string)(s.state["active"] as JObject)?["phase"];Text("Task.State",phase=="running"?"执行中":phase=="pending"?"待领取":"待命");Text("Task.Headline",phase==null?"尚未派遣远征队":phase=="running"?"远征执行中（演示）":"远征完成，等待领取");Text("Loadout.Medkit","× "+s.Count("medkit"));Text("Loadout.Battery","× "+s.Count("battery"));Text("Loadout.Summary","携行"+s.Weight.ToString("0.0")+"kg");Enabled("Action.OpenExpeditions",!s.Locked);Visible("Action.ViewResult",phase=="pending");
                }
                if(current=="ExpeditionSelectPanel")
                {
                    Text("Mission.Name",s.Mission["name"]);Text("Mission.Risk",s.Mission["risk"]);Text("Mission.Description",s.Mission["description"]);Text("Mission.Duration",s.Mission["duration"]+"分钟");Text("Mission.Recommended","推荐补给："+s.Mission["recommended"]);Text("Mission.LoadoutSummary","已携行："+s.Summary+" / 演示耗时3秒");Text("Mission.CreditsCost",s.Mission["credits"]);Text("Mission.EnergyCost",s.Mission["energy"]);Text("Mission.Deficit",s.Deficit()==""?"资源充足，可以派遣":s.Deficit());Enabled("Action.StartExpedition",s.Deficit()=="");
                    for(var i=0;i<3;i++){var reward=s.Mission["rewards"][i];var id=(string)reward[0];Text("Mission.RewardName"+i,id=="credits"?"信用点":(string)s.Item(id)["name"]);Text("Mission.RewardQuantity"+i,(id=="credits"?"+":"×")+reward[1]);Icon(map,"Mission.RewardIcon"+i,id=="credits"?"Img_Credits.png":(string)s.Item(id)["icon"]);}
                    var missions=s.dataState=="normal"?s.Mission.Parent.ToList():new List<JToken>();var rows=Rows("Missions.List","MissionEntry",missions.Count,174);Text("Missions.Count",missions.Count+"项");Visible("Missions.Status",s.dataState!="normal");Text("Missions.Status",s.dataState=="error"?"读取失败，请重试":s.dataState=="loading"?"正在加载任务…":"暂无可用远征");Visible("Action.RetryMissions",s.dataState=="error");
                    for(var i=0;i<rows.Count;i++){var m=missions[i];var id=(string)m["id"];RowText(rows[i],"Mission.Name",m["name"]);RowText(rows[i],"Mission.Index",(i+1).ToString("00"));RowText(rows[i],"Mission.Risk",m["risk"]+" · 预计"+m["duration"]+"分钟");RowText(rows[i],"Mission.State",!(bool)m["unlocked"]?"未解锁":id==s.mission?"已选中":"已解锁");rows[i].Get("Mission.SelectedMarker").gameObject.SetActive(id==s.mission);var b=rows[i].GetComponent<Button>();b.onClick.AddListener(()=>{s.mission=id;Render();});}
                }
                if(current=="SupplyInventoryPanel")
                {
                    foreach(var category in new[]{"All","Material","Supply"})Choice("Action.Category"+category,s.category==category);
                    var list=s.Filtered;if(!list.Any(i=>(string)i["id"]==s.selected))s.selected=null;var rows=Rows("Inventory.List","InventoryEntry",list.Count,96);
                    for(var i=0;i<rows.Count;i++){var item=list[i];var id=(string)item["id"];RowText(rows[i],"Item.Name",item["name"]);RowText(rows[i],"Item.Quantity","× "+item["qty"]);RowText(rows[i],"Item.Category",(string)item["category"]=="Supply"?"补给":"材料");Icon(rows[i],"Item.Icon",(string)item["icon"]);rows[i].Get("Item.SelectedMarker").gameObject.SetActive(id==s.selected);rows[i].GetComponent<Button>().onClick.AddListener(()=>{s.selected=id;s.quantity=1;Render();});}
                    Text("Inventory.Count",list.Count+"类物品");Visible("Inventory.Status",list.Count==0);Text("Inventory.Status",s.search.Trim()==""?"仓库暂无物品":"没有找到相关物品");map.Get<TMP_InputField>("Filter.Search").SetTextWithoutNotify(s.search);
                    var chosen=s.Item(s.selected);var supply=(string)chosen?["category"]=="Supply";var available=supply?s.Available(chosen):0;s.quantity=Mathf.Clamp(s.quantity,1,Math.Max(1,available));
                    Text("Item.DetailName",(string)chosen?["name"]??"选择物品");Text("Item.Description",(string)chosen?["description"]??"选择物品查看详情");Text("Item.Stock",chosen==null?"":"仓库"+chosen["qty"]+" / 已携行"+s.Count(s.selected));Text("Supply.QuantityValue",supply&&available>0?s.quantity.ToString():"");
                    Text("Item.DetailRarity",chosen==null?"":chosen["rarity"]+(supply?"补给":"材料"));Text("Item.Weight",chosen==null?"":supply?"单重"+((float)chosen["weight"]).ToString("0.0")+"kg · 可加入"+available:"制造材料，不能作为远征补给");Visible("Item.DetailIcon",chosen!=null);if(chosen!=null)Icon(map,"Item.DetailIcon",(string)chosen["icon"]);
                    var slider=map.Get<Slider>("Supply.AddQuantity");slider.minValue=1;slider.maxValue=Math.Max(1,available);slider.wholeNumbers=true;slider.SetValueWithoutNotify(s.quantity);slider.interactable=available>1&&!s.Locked;Visible("Supply.AddQuantity",supply&&available>1);
                    Visible("Action.AddSupply",supply);Enabled("Action.AddSupply",available>0&&!s.Locked);Visible("Action.DecreaseQuantity",supply&&available>1);Visible("Action.IncreaseQuantity",supply&&available>1);Enabled("Action.DecreaseQuantity",s.quantity>1&&!s.Locked);Enabled("Action.IncreaseQuantity",s.quantity<available&&!s.Locked);Enabled("Action.ClearLoadout",s.Loadout.Count>0&&!s.Locked);Text("Supply.QuantityHint",s.Locked?"任务期间无法修改携行":available==0?"无可加入数量":"仅剩1个可加入，数量固定");Visible("Supply.QuantityHint",supply&&(available<=1||s.Locked));
                    var carried=s.Loadout.Properties().ToList();var carry=Rows("Loadout.List","LoadoutEntry",carried.Count,148);for(var i=0;i<carry.Count;i++){var id=carried[i].Name;RowText(carry[i],"Supply.Name",s.Item(id)["name"]);RowText(carry[i],"Supply.Summary","× "+s.Count(id));RowButton(carry[i],"Action.Decrease",()=>s.Remove(id,false));RowButton(carry[i],"Action.Remove",()=>s.Remove(id,true));}
                    Text("Loadout.Count",carried.Count+"类");Text("Loadout.Weight","重量"+s.Weight.ToString("0.0")+" / "+s.state["capacity"]+"kg");Visible("Loadout.Status",carried.Count==0);
                    var rules=JObject.Parse(File.ReadAllText(Path.Combine(Workspace,"contracts/screens.json")))["screens"].First(p=>(string)p["panelName"]==current)["fixturePresentation"];UiReferenceAudit.ApplyPresentation(root,new JObject{["presentationRules"]=rules.DeepClone(),["presentationData"]=new JObject{["loadoutWeight"]=s.Weight,["capacity"]=s.state["capacity"]}});
                }
                if(current=="GameSettingsPanel")
                {
                    foreach(var quality in new[]{"Low","Medium","High"})Choice("Action.Quality"+quality,(string)s.draft["quality"]==quality);
                    map.Get<TMP_InputField>("Settings.CaptainName").SetTextWithoutNotify((string)s.draft["captain"]);map.Get<Toggle>("Settings.SoundEnabled").SetIsOnWithoutNotify((bool)s.draft["sound"]);map.Get<Slider>("Settings.MasterVolume").SetValueWithoutNotify((float)s.draft["volume"]);Text("Settings.NameCounter",Session.ScalarCount((string)s.draft["captain"])+"/12");Text("Settings.SoundState",(bool)s.draft["sound"]?"开启":"关闭");Text("Settings.VolumeLabel",s.draft["volume"]+"%");Text("Settings.NameError",s.feedback);
                }
                if(current=="ActionConfirmPanel"){var start=s.mode=="start";var reset=s.mode=="reset";Text("Confirm.Title",reset?"重置本次演示？":start?"确认派遣远征队？":"清空携行清单？");Text("Confirm.Mission",start?s.Mission["name"].ToString():reset?"恢复全部初始演示数据":"只清空计划携行");Text("Confirm.Risk",start?s.Mission["risk"].ToString():"请确认");Text("Confirm.Credits",start?s.Mission["credits"].ToString():reset?"全部恢复":"不会销毁");Text("Confirm.Energy",start?s.Mission["energy"].ToString():reset?"全部清除":s.Loadout.Count+"类");Text("Confirm.Loadout",reset?"此操作将清除当前任务和未领取结果。":"携行："+s.Summary);Text("Confirm.Weight",start?"总重"+s.Weight.ToString("0.0")+" / "+s.state["capacity"]+"kg · 演示耗时3秒":reset?"恢复到2400信用点、100能源与空携行。":"确认后携行重量归零。");}
                foreach(var toggleGraphic in root.GetComponentsInChildren<UiToggleStateGraphic>(true))toggleGraphic.Refresh();
                if(current=="ExpeditionResultPanel")
                {
                    var active=s.state["active"] as JObject;var pending=(string)active?["phase"]=="pending";var resultMission=active==null?s.Mission:s.MissionFor((string)active["missionId"]);Enabled("Action.ClaimRewards",pending);Text("Result.Title",pending?"远征成功":"暂无待领取奖励");Text("Result.Mission",resultMission["name"]);Text("Result.PaidCosts",pending?"信用点"+resultMission["credits"]+" / 能源"+resultMission["energy"]:"");Text("Result.Consumption",pending&&((int?)active["loadout"]["medkit"]??0)>0?"急救包 -1":"本次无补给消耗");
                    var rewards=pending?resultMission["rewards"].ToList():new List<JToken>();var rows=Rows("Rewards.List","RewardEntry",rewards.Count,132);for(var i=0;i<rows.Count;i++){var id=(string)rewards[i][0];RowText(rows[i],"Reward.Name",id=="credits"?"信用点":(string)s.Item(id)["name"]);RowText(rows[i],"Reward.Quantity","+"+rewards[i][1]);Icon(rows[i],"Reward.Icon",id=="credits"?"Img_Credits.png":(string)s.Item(id)["icon"]);}
                }
            }
            internal void Click(string key){var b=map.Get<Button>(key);if(b==null)throw new InvalidDataException("Missing Button: "+key);b.OnPointerClick(new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left});}
            internal void RowClick(string list,int index,string key=null){var row=map.Get<ScrollRect>(list).content.GetChild(index).GetComponent<UiBindingMap>();var b=key==null?row.GetComponent<Button>():row.Get<Button>(key);b.OnPointerClick(new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left});}
        }

        internal static string Workspace=>Path.GetFullPath(Path.Combine(Application.dataPath,"../../Workspaces/StarportSalvage"));
        [MenuItem("HTML转UGUI/星港示例/运行编辑器业务验收")]
        public static void MenuRun()
        {
            var result=JObject.Parse(Run(Path.GetFullPath(Path.Combine(Application.dataPath,"../../Audit/Runs/latest/starport-editor-flow"))));
            if((string)result["status"]=="passed")Debug.Log("星港编辑器业务验收通过，报告位于 Audit/Runs/latest/starport-editor-flow。");
            else Debug.LogError("星港编辑器业务验收失败："+result["error"]);
        }
        public static string Run(string outputDirectory)
        {
            outputDirectory=Path.GetFullPath(outputDirectory);
            var assets=Path.GetFullPath(Application.dataPath).TrimEnd(Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar);
            if(string.Equals(outputDirectory,assets,StringComparison.OrdinalIgnoreCase)||outputDirectory.StartsWith(assets+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Editor evidence must stay outside Assets.");
            Directory.CreateDirectory(outputDirectory);File.WriteAllText(Path.Combine(outputDirectory,"editor-flow.json"),"{\"status\":\"running\"}");
            var checks=new JArray();var screenshots=new JArray();var dataPath=Path.Combine(Workspace,"design-tools/editor-demo-data.json");var s=new Session(JObject.Parse(File.ReadAllText(dataPath)));var protectedAssets=new JObject();
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();var sceneHandle=scene.handle;var sceneDirty=scene.isDirty;var sceneRoots=scene.rootCount;
            foreach(var path in AssetDatabase.FindAssets("",new[]{"Assets/Art/UIPrefab/StarportSalvage","Assets/Art/UI/Sprites/StarportSalvage","Assets/HtmlToUGUIStage/StarportSalvage"}).Select(AssetDatabase.GUIDToAssetPath).Where(File.Exists)) {protectedAssets[path]=Sha(path);if(File.Exists(path+".meta"))protectedAssets[path+".meta"]=Sha(path+".meta");}
            Action<bool,string> check=(ok,name)=>{checks.Add(new JObject{["name"]=name,["status"]=ok?"passed":"failed"});if(!ok)throw new InvalidDataException(name);};
            Action<string,Action<Adapter>> step=(name,act)=>
            {
                var panel=s.panel;var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs+panel+"/"+panel+".prefab");if(prefab==null)throw new FileNotFoundException(panel);var dir=Path.Combine(outputDirectory,name);
                UiVisualAudit.Capture(prefab,dir,root=>{var adapter=new Adapter(root,s);act(adapter);check(adapter.map.Entries.Where(e=>e.target is Button).All(e=>adapter.wired.Contains(e.key)),name+": all declared Button bindings wired");},new Color32(137,147,156,255));
                var screenshot=Path.Combine(dir,panel+".unity.png");screenshots.Add(new JObject{["panel"]=panel,["step"]=name,["path"]=Path.GetRelativePath(outputDirectory,screenshot).Replace('\\','/'),["sha256"]=Sha(screenshot)});
            };
            var report=new JObject{["schemaVersion"]=1,["projectName"]="StarportSalvage",["target"]="UnityEditor",["scope"]="editor-demo",["status"]="running",["clock"]="deterministic test clock, three-second boundary",["unityVersion"]=Application.unityVersion,["checks"]=checks,["screenshots"]=screenshots,["panels"]=JArray.FromObject(Panels)};
            try
            {
                step("01-home",a=>{a.Click("Action.OpenExpeditions");check(s.panel=="ExpeditionSelectPanel","home navigation");});
                step("02-missions",a=>{a.RowClick("Missions.List",2);check(s.Deficit().Contains("未解锁")&&!a.map.Get<Button>("Action.StartExpedition").interactable,"locked mission disabled");a.Click("Action.StartExpedition");check(!s.Locked,"disabled start has no effect");a.RowClick("Missions.List",0);a.Click("Action.EditLoadout");check(s.origin=="ExpeditionSelectPanel","inventory remembers origin");});
                step("03-prepare",a=>{a.map.Get<Slider>("Supply.AddQuantity").value=3;a.Click("Action.AddSupply");check(s.Count("medkit")==3&&s.Weight==3,"slider and add supply");a.RowClick("Inventory.List",1);a.map.Get<Slider>("Supply.AddQuantity").value=2;a.Click("Action.AddSupply");check(s.Weight==7&&s.Count("battery")==2,"merge loadout weight");a.Click("Action.Return");check(s.panel=="ExpeditionSelectPanel","inventory return origin");});
                step("04-confirm-open",a=>{a.Click("Action.StartExpedition");check(s.panel=="ActionConfirmPanel"&&(int)s.state["credits"]==2400,"opening confirm charges nothing");});
                step("05-confirm-cancel",a=>{a.Click("Action.CancelConfirm");check(!s.Locked&&(int)s.state["credits"]==2400,"cancel preserves resources");});
                s.Confirm("start");step("06-dispatch",a=>{a.Click("Action.Confirm");check(s.Locked&&(int)s.state["credits"]==2300&&(int)s.state["energy"]==80,"dispatch charges once");a.Click("Action.Confirm");check((int)s.state["credits"]==2300,"duplicate confirm is inert");});
                s.Go("SupplyInventoryPanel");step("07-running",a=>{var before=s.Loadout.ToString();a.Click("Action.AddSupply");s.Add();a.RowClick("Loadout.List",0,"Action.Remove");check(before==s.Loadout.ToString(),"running loadout is locked");});
                s.mission="signal";s.Tick(2.999);check((string)s.state["active"]["phase"]=="running","not complete before three seconds");s.Tick(.0011);check((string)s.state["active"]["phase"]=="pending","complete at three seconds");s.Go("StarportHomePanel");
                step("08-pending",a=>{check(!a.map.Get<Button>("Action.OpenExpeditions").interactable,"pending blocks redispatch");a.Click("Action.ViewResult");check(s.panel=="ExpeditionResultPanel","pending result navigation");});
                step("09-result",a=>{check(a.map.Get<ScrollRect>("Rewards.List").content.childCount==3,"real reward Items populated");check(a.map.Get<TMP_Text>("Result.Mission").text=="废弃货运站","result uses dispatched mission snapshot after selection changes");a.Click("Action.Postpone");check(s.Locked,"postpone preserves pending result");});
                s.Go("ExpeditionResultPanel");step("10-claim",a=>{a.Click("Action.ClaimRewards");a.Click("Action.ClaimRewards");check(!s.Locked&&(int)s.state["credits"]==2600&&(int)s.state["energy"]==80,"reward exactly once");check((int)s.Item("ore")["qty"]==28&&(int)s.Item("alloy")["qty"]==7&&(int)s.Item("medkit")["qty"]==3&&s.Weight==6,"inventory rewards and one medkit consumed");});
                s.Go("SupplyInventoryPanel");step("11-filters",a=>{a.map.Get<TMP_InputField>("Filter.Search").text="不存在";check(s.Filtered.Count==0&&a.map.Get<ScrollRect>("Inventory.List").content.childCount==0,"search empty native list");a.Click("Action.ClearSearch");a.Click("Action.CategoryMaterial");check(s.Filtered.Count==4,"material filter");a.RowClick("Inventory.List",0);check(!a.map.Get<Button>("Action.AddSupply").gameObject.activeSelf,"materials cannot be carried");a.Click("Action.CategoryAll");a.RowClick("Inventory.List",3);check(!a.map.Get<Button>("Action.AddSupply").interactable,"zero-stock disabled");a.RowClick("Loadout.List",0,"Action.Decrease");check(s.Count("medkit")==1,"decrease native Item action");a.RowClick("Loadout.List",0,"Action.Remove");check(s.Count("medkit")==0,"remove native Item action");a.Click("Action.ClearLoadout");});
                step("12-clear",a=>{a.Click("Action.Confirm");check(s.Loadout.Count==0&&(int)s.Item("medkit")["qty"]==3,"clear preserves warehouse");});
                s.selected="ration";s.quantity=12;s.state["capacity"]=1;var load=s.Loadout.ToString();s.Add();check(s.Loadout.ToString()==load&&s.feedback.Contains("超重"),"overweight rejection atomic");s.state["capacity"]=20;
                s.Go("GameSettingsPanel");step("13-settings",a=>{var input=a.map.Get<TMP_InputField>("Settings.CaptainName");input.text="   ";a.Click("Action.ApplySettings");check(s.panel=="GameSettingsPanel"&&(string)s.state["captain"]=="林岚","empty captain rejected");input.text="远航者";a.map.Get<Toggle>("Settings.SoundEnabled").isOn=false;a.map.Get<Slider>("Settings.MasterVolume").value=35;a.Click("Action.QualityLow");check(a.map.Get<Button>("Action.QualityLow").image.color!=a.map.Get<Button>("Action.QualityHigh").image.color,"quality selection updates native visuals");a.Click("Action.ApplySettings");check((string)s.state["captain"]=="远航者"&&!(bool)s.state["settings"]["sound"]&&(int)s.state["settings"]["volume"]==35&&(string)s.state["settings"]["quality"]=="Low","native settings applied");});
                s.Go("GameSettingsPanel");step("14-settings-cancel",a=>{a.Click("Action.ResetDefaults");a.Click("Action.CancelSettings");check((string)s.state["captain"]=="远航者","cancel discards settings draft");});
                s.Go("GameSettingsPanel");s.draft["captain"]=new string('甲',13);s.Apply();check(s.panel=="GameSettingsPanel","overlong captain rejected");s.draft["captain"]="a\nb";s.Apply();check(s.panel=="GameSettingsPanel","control character rejected");s.draft["captain"]=string.Concat(Enumerable.Repeat("🚀",12));s.Apply();check(s.panel=="StarportHomePanel"&&Session.ScalarCount((string)s.state["captain"])==12,"unicode scalar boundary accepted");
                s.Reset();s.Confirm("start");s.state["credits"]=50;s.state["energy"]=10;s.Commit();check(!s.Locked&&(int)s.state["credits"]==50&&(int)s.state["energy"]==10,"confirmation revalidates resources atomically");
                foreach(var value in new[]{"loading","error","empty"}){s.dataState=value;check(s.Deficit()!="","data unavailable blocks start: "+value);}s.dataState="error";s.Go("ExpeditionSelectPanel");step("15-retry",a=>{a.Click("Action.RetryMissions");check(s.dataState=="normal","retry loads native mission list");});
                s.Reset();s.Confirm("start");s.Commit();s.Tick(3);s.Claim();check((int)s.Item("medkit")["qty"]==4&&(int)s.state["credits"]==2600,"no-medkit expedition has no consumption");
                s.Reset();s.mission="signal";s.Confirm("start");s.Commit();s.Tick(3);s.Claim();check((int)s.state["credits"]==2800&&(int)s.state["energy"]==65&&(int)s.Item("circuit")["qty"]==6&&(int)s.Item("chip")["qty"]==2,"second mission uses its own costs and rewards");
                s.Go("ExpeditionSelectPanel");s.Confirm("start");s.Escape();check(s.panel=="ExpeditionSelectPanel"&&s.mode==null,"escape dismisses modal");s.Go("SupplyInventoryPanel");s.Escape();check(s.panel=="ExpeditionSelectPanel","escape returns inventory origin");
                s.Go("StarportHomePanel");s.Confirm("reset");step("16-reset",a=>{a.Click("Action.Confirm");check((int)s.state["credits"]==2400&&(int)s.state["energy"]==100&&s.Loadout.Count==0&&!s.Locked&&(string)s.state["captain"]=="林岚","reset restores complete session");});
                foreach(var entry in protectedAssets.Properties())check(Sha(entry.Name)==(string)entry.Value,"asset unchanged: "+entry.Name);
                scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();check(scene.handle==sceneHandle&&scene.isDirty==sceneDirty&&scene.rootCount==sceneRoots,"active scene and dirty state preserved");
                report["status"]="passed";
            }
            catch(Exception error){report["status"]="failed";report["error"]=error.ToString();}
            report["inputFilesSha256"]=new JObject{["design-tools/editor-demo-data.json"]=Sha(dataPath),["design-tools/preview_runtime.js"]=Sha(Path.Combine(Workspace,"design-tools/preview_runtime.js")),["contracts/screens.json"]=Sha(Path.Combine(Workspace,"contracts/screens.json")),["contracts/style.json"]=Sha(Path.Combine(Workspace,"contracts/style.json"))};report["adapterPath"]="Assets/Editor/StarportSalvage/StarportEditorFlowChecks.cs";report["adapterSha256"]=Sha("Assets/Editor/StarportSalvage/StarportEditorFlowChecks.cs");report["protectedAssetsSha256"]=protectedAssets;
            File.WriteAllBytes(Path.Combine(outputDirectory,"editor-adapter.cs.txt"),File.ReadAllBytes((string)report["adapterPath"]));
            File.WriteAllText(Path.Combine(outputDirectory,"editor-flow.json"),report.ToString());return report.ToString(Newtonsoft.Json.Formatting.None);
        }
    }
}
#endif
