/* Browser review only. This JavaScript is not converted into Unity gameplay. */
(() => {
 'use strict';
 const KEY='starport-salvage-demo-v1', root=document.querySelector('[data-root]');
 const panel=root.dataset.panelName, params=new URLSearchParams(location.search);
 const $=key=>Array.from(document.querySelectorAll(`[data-binding="${key}"]`)).find(n=>!n.closest('[data-preview-template]'));
 const action=key=>$(key.startsWith('Action.')?key:'Action.'+key);
 const text=(key,value)=>{const n=$(key);if(n)n.textContent=String(value);};
 const show=(node,value)=>{if(node)node.style.display=value?'':'none';};
 const color=(node,value)=>{if(node)node.style.backgroundColor=value;};
 const message=value=>text('Feedback.Message',value);
 const fmt=n=>Number(n).toLocaleString('en-US');
 const items=[
  {id:'medkit',name:'急救包',category:'Supply',rarity:'普通',qty:4,weight:1,icon:'Img_ItemMedkit.png',description:'成功远征时，若有携带，\n消耗1个。'},
  {id:'battery',name:'能源电池',category:'Supply',rarity:'优良',qty:2,weight:2,icon:'Img_ItemBattery.png',description:'储存应急能源。\n演示结算不消耗。'},
  {id:'ration',name:'压缩口粮',category:'Supply',rarity:'普通',qty:12,weight:.5,icon:'Img_ItemRation.png',description:'轻便的远征食品。\n演示结算不消耗。'},
  {id:'beacon',name:'应急信标',category:'Supply',rarity:'稀有',qty:0,weight:3,icon:'Img_ItemBeacon.png',description:'发出紧急求援信号。\n当前暂无库存。'},
  {id:'ore',name:'钛矿石',category:'Material',rarity:'普通',qty:20,weight:0,icon:'Img_ItemOre.png',description:'基础制造材料。'},
  {id:'alloy',name:'合金板',category:'Material',rarity:'优良',qty:5,weight:0,icon:'Img_ItemAlloy.png',description:'用于星港设施修复。'},
  {id:'circuit',name:'电路板',category:'Material',rarity:'优良',qty:3,weight:0,icon:'Img_ItemCircuit.png',description:'电子设备制造材料。'},
  {id:'chip',name:'数据芯片',category:'Material',rarity:'稀有',qty:1,weight:0,icon:'Img_ItemChip.png',description:'保存科技资料的载体。'}];
 const missions=[
  {id:'cargo',name:'废弃货运站',risk:'低风险',credits:100,energy:20,duration:10,unlocked:true,description:'回收遗留货箱与基础建材，适合新队长的首次远征。',recommended:'急救包',rewards:[['credits',300],['ore',8],['alloy',2]]},
  {id:'signal',name:'失联信号塔',risk:'中风险',credits:200,energy:35,duration:20,unlocked:true,description:'搜索失联信号源，回收电子元件与科技资料。',recommended:'急救包、能源电池',rewards:[['credits',600],['circuit',3],['chip',1]]},
  {id:'reactor',name:'旧反应堆核心区',risk:'高风险',credits:400,energy:60,duration:30,unlocked:false,description:'高辐射区域仍在封锁中。',recommended:'后续版本开放',rewards:[['credits',1200],['alloy',6],['chip',2]]}];
 function fresh(){return {version:1,captain:'林岚',credits:2400,energy:100,capacity:20,inventory:structuredClone(items),loadout:{},settings:{sound:true,volume:80,quality:'High'},selectedMission:'cargo',active:null,lastResult:null,pending:null,origin:'StarportHomePanel',view:{category:'All',search:'',selectedItem:'medkit'},fixture:'Default',dataState:'normal'};}
 function read(){try{const s=JSON.parse(sessionStorage.getItem(KEY));if(s?.version===1)return s;}catch{}return fresh();}
 let state=read(),quantity=1,settingsDraft=null,overlay=null;
 function save(){sessionStorage.setItem(KEY,JSON.stringify(state));}
 function item(id){return state.inventory.find(x=>x.id===id);}
 const getMission=id=>missions.find(x=>x.id===id)||missions[0];
 function weight(){return Object.entries(state.loadout).reduce((total,[id,n])=>total+(item(id)?.weight||0)*n,0);}
 function summary(){return Object.entries(state.loadout).filter(([,n])=>n>0).map(([id,n])=>`${item(id)?.name||id}×${n}`).join('、')||'未携带补给';}
 function locked(){return !!state.active;}
 function icon(node,file){if(node){const url=`url("assets/shared/${file}")`;node.style.maskImage=url;node.style.webkitMaskImage=url;node.style.backgroundColor=/Ore|Alloy|Chip|Credits/.test(file)?'#F2B65C':'#4AD7BB';}}
 function setSlider(node,value){if(!node)return;node.value=String(value);const min=Number(node.min),max=Number(node.max);node.style.setProperty('--progress',`${max===min?0:(value-min)/(max-min)*100}%`);}
 function go(name){location.href=`../../${name}/html/${name}.html`;}
 function bind(key,fn){const n=action(key);if(n)n.onclick=fn;}
 function rows(binding,templateKey,data,fill){
  const list=$(binding);if(!list)return;
  list.querySelectorAll('[data-preview-template]').forEach(n=>n.remove());
  const template=list.querySelector('template');
  const step={MissionEntry:174,InventoryEntry:96,LoadoutEntry:148,RewardEntry:132}[templateKey];
  const contentHeight=Math.max(list.clientHeight,data.length?step*(data.length-1)+Number(template.content.firstElementChild.style.height.replace('px','')):0);
  let extent=list.querySelector('[data-preview-extent]');
  if(!extent){extent=document.createElement('div');extent.dataset.previewExtent='true';extent.style.cssText='position:relative;width:1px;pointer-events:none';list.append(extent);}
  extent.style.height=contentHeight+'px';
  for(let i=0;i<data.length;i++){
   const n=template.content.firstElementChild.cloneNode(true);n.dataset.previewTemplate=templateKey;n.dataset.itemId=data[i].id||String(i);
   n.style.top=i*step+'px';n.style.left='0px';
   for(const child of [n,...n.querySelectorAll('[id]')])if(child.id)child.id+='__preview_'+i;
   const q=k=>n.querySelector(`[data-binding="${k}"]`);
   const tx=(k,v)=>{const el=q(k);if(el)el.textContent=String(v);};
   fill(n,data[i],i,q,tx);list.append(n);
  }
 }
 function deficit(m){const a=[];if(!m.unlocked)a.push('未解锁：后续版本开放');if(state.credits<m.credits)a.push(`还缺${m.credits-state.credits}信用点`);if(state.energy<m.energy)a.push(`还缺${m.energy-state.energy}能源`);if(weight()>state.capacity)a.push('携行超重，请调整补给');if(locked())a.push(state.active.phase==='running'?'远征执行中，请等待':'请先领取当前结算');if(state.dataState!=='normal')a.push('任务数据尚未就绪');return a;}
 function confirm(mode){
  if(mode==='start'&&deficit(getMission(state.selectedMission)).length){message(deficit(getMission(state.selectedMission)).join('、'));return;}
  state.pending={mode,origin:panel,missionId:state.selectedMission};save();
  overlay=document.createElement('iframe');overlay.title='操作确认';overlay.src='../../ActionConfirmPanel/html/ActionConfirmPanel.html?overlay=1';
  overlay.style.cssText='position:fixed;inset:0;width:100%;height:100%;border:0;z-index:1000;background:transparent;';
  document.body.append(overlay);overlay.focus();
 }
 function dismiss(){
  if(params.get('overlay')==='1'){parent.postMessage({type:'starport-confirm-close'},location.origin);return;}
  go(state.pending?.origin||'StarportHomePanel');
 }
 function commitConfirm(){
  state=read();const pending=state.pending||{mode:'start',missionId:state.selectedMission,origin:'ExpeditionSelectPanel'};
  let error='';
  if(pending.mode==='start'){
   const m=getMission(pending.missionId);error=deficit(m).join('、');
   if(!error){state.credits-=m.credits;state.energy-=m.energy;state.active={id:crypto.randomUUID(),missionId:m.id,phase:'running',endsAt:Date.now()+3000,loadout:structuredClone(state.loadout)};}
  }else if(pending.mode==='clear'){
   if(locked())error='任务期间无法修改携行';else state.loadout={};
  }else if(pending.mode==='reset'){state=fresh();}
  state.pending=null;save();
  if(params.get('overlay')==='1')parent.postMessage({type:'starport-confirm-close',error},location.origin);
  else go(pending.origin||'StarportHomePanel');
 }
 function renderHome(){
  text('Page.Subtitle',`指挥终端 / ${state.captain}队长，欢迎返回星港`);
  const a=state.active;
  text('Task.State',a?(a.phase==='running'?'执行中':'待领取'):'待命');
  text('Task.Headline',a?(a.phase==='running'?`远征执行中（演示）`:'远征完成，等待领取'):'尚未派遣远征队');
  text('Task.Description',a?(a.phase==='running'?`${getMission(a.missionId).name} · 剩余${Math.max(0,Math.ceil((a.endsAt-Date.now())/1000))}秒`:getMission(a.missionId).name):'从低风险货运站开始第一趟回收。');
  text('Loadout.Medkit','× '+(state.loadout.medkit||0));text('Loadout.Battery','× '+(state.loadout.battery||0));
  const carried=Object.entries(state.loadout).filter(([,n])=>n>0);
  for(const [index,slot] of ['Medkit','Battery'].entries()){
   const entry=carried[index],name=document.querySelector(`[data-ui-name="Txt_SummaryName${slot}"]`),picture=document.querySelector(`[data-ui-name="Img_Summary${slot}"]`);
   show(name,!!entry);show(picture,!!entry);show($('Loadout.'+slot),!!entry);
   if(entry){name.textContent=item(entry[0])?.name||entry[0];icon(picture,item(entry[0]).icon);text('Loadout.'+slot,'× '+entry[1]);}
  }
  text('Loadout.Summary',`携行${weight().toFixed(1)}kg / 上限${state.capacity.toFixed(1)}kg`);
  action('OpenExpeditions').disabled=!!a;show(action('ViewResult'),a?.phase==='pending');show($('Loadout.Summary'),a?.phase!=='pending');
 }
 function renderMissions(){
  const m=getMission(state.selectedMission),normal=state.dataState==='normal';
  text('Missions.Status',state.dataState==='loading'?'正在加载任务…':state.dataState==='error'?'读取失败，请重试':state.dataState==='empty'?'暂无可用远征':'');
  show($('Missions.Status'),!normal);show(action('RetryMissions'),state.dataState==='error');
  const list=normal?missions:[];text('Missions.Count',String(list.length).padStart(2,'0')+'项');
  rows('Missions.List','MissionEntry',list,(n,m,i,q,tx)=>{
   tx('Mission.Index',String(i+1).padStart(2,'0'));tx('Mission.Name',m.name);tx('Mission.Risk',`${m.risk} · 预计${m.duration}分钟`);
   tx('Mission.State',!m.unlocked?'未解锁 · 后续版本开放':m.id===state.selectedMission?'已选中':'已解锁');
   n.style.backgroundColor=m.id===state.selectedMission?'#203745':'#192E3C';
   q('Mission.Index').style.color=!m.unlocked?'#AAC0CA':m.id==='signal'?'#F2B65C':'#4AD7BB';
   q('Mission.State').style.color=!m.unlocked?'#AAC0CA':m.id==='signal'?'#F2B65C':'#4AD7BB';
   show(q('Mission.SelectedMarker'),m.id===state.selectedMission);
   n.onclick=()=>{state.selectedMission=m.id;save();render();};
  });
  text('Mission.Name',m.name);text('Mission.Risk',m.risk);text('Mission.Description',m.description);
  $('Mission.Risk').style.color=m.id==='cargo'?'#4AD7BB':m.id==='signal'?'#F2B65C':'#F07878';
  text('Mission.CreditsCost',m.credits);text('Mission.EnergyCost',m.energy);text('Mission.Duration',m.duration+'分钟');
  for(let i=0;i<3;i++){const [id,count]=m.rewards[i];text('Mission.RewardName'+i,id==='credits'?'信用点':item(id)?.name||id);text('Mission.RewardQuantity'+i,(id==='credits'?'+':'×')+count);icon($('Mission.RewardIcon'+i),id==='credits'?'Img_Credits.png':item(id).icon);}
  text('Mission.Recommended',m.unlocked?`推荐补给：${m.recommended}（推荐，非强制）`:'未解锁：后续版本开放');
  text('Mission.LoadoutSummary',`已携行：${summary()} / 演示耗时3秒`);
  const reasons=deficit(m);text('Mission.Deficit',reasons.length?reasons.join('、'):'资源充足，可以派遣');$('Mission.Deficit').style.color=reasons.length?'#F2B65C':'#4AD7BB';action('StartExpedition').disabled=reasons.length>0;
 }
 function available(i){return Math.max(0,i.qty-(state.loadout[i.id]||0));}
 function addSupply(){
  const i=item(state.view.selectedItem);if(locked()){message('任务期间无法修改携行');return;}
  if(!i||i.category!=='Supply')return;
  const n=Math.round(Number($('Supply.AddQuantity').value)||quantity);
  if(n<1||n>available(i)){message('数量超过当前可加入库存');return;}
  if(weight()+i.weight*n>state.capacity){message(`携行超重，还可携带${Math.max(0,state.capacity-weight()).toFixed(1)}kg`);return;}
  state.loadout[i.id]=(state.loadout[i.id]||0)+n;save();message(`已加入${i.name}×${n}`);renderInventory();renderResources();
 }
 function renderInventory(){
  const v=state.view,search=v.search.trim();
  const filtered=state.inventory.filter(i=>(v.category==='All'||v.category===i.category)&&i.name.includes(search));
  if(!filtered.some(i=>i.id===v.selectedItem))v.selectedItem=null;
  text('Inventory.Count',filtered.length+'类物品');text('Inventory.Status',filtered.length?'':search?'没有找到相关物品':'仓库暂无物品');show($('Inventory.Status'),!filtered.length);
  for(const k of ['All','Material','Supply'])color(action('Category'+k),v.category===k?'#4AD7BB':'#203745');
  for(const k of ['All','Material','Supply'])action('Category'+k).style.color=v.category===k?'#102B2C':'#EEF5F7';
  rows('Inventory.List','InventoryEntry',filtered,(n,i,_index,q,tx)=>{
   icon(q('Item.Icon'),i.icon);tx('Item.Name',i.name);tx('Item.Quantity','× '+i.qty);tx('Item.Category',`${i.category==='Supply'?'补给':'材料'} · ${i.rarity}`);
   n.style.backgroundColor=i.id===v.selectedItem?'#203745':'#192E3C';q('Item.Name').style.color=i.qty?'#EEF5F7':'#AAC0CA';
   q('Item.Quantity').style.color=i.id===v.selectedItem?'#4AD7BB':'#AAC0CA';show(q('Item.SelectedMarker'),i.id===v.selectedItem);
   n.onclick=()=>{state.view.selectedItem=i.id;quantity=1;save();renderInventory();};
  });
  const i=item(v.selectedItem),isSupply=i?.category==='Supply',max=i&&isSupply?available(i):0;
  quantity=Math.max(1,Math.min(quantity,max||1));
  text('Item.DetailName',i?.name||'选择物品');text('Item.DetailRarity',i?`${i.rarity}${isSupply?'补给':'材料'}`:'');
  const longName=(i?.name.length||0)>8;
  $('Item.DetailName').style.fontSize=longName?'22px':'32px';$('Item.DetailName').style.height=longName?'86px':'50px';
  const rarity=$('Item.DetailRarity');rarity.style.top=((longName?507:467)-Number(rarity.dataset.parentCanvasY||0))+'px';
  text('Item.Description',i?.description||'选择物品查看详情');text('Item.Stock',i?`仓库${i.qty} / 已携行${state.loadout[i.id]||0}`:'');
  $('Item.Description').style.height=Math.max(76,Math.ceil((i?.description.length||0)/16)*32)+'px';
  text('Item.Weight',i?(isSupply?`单重${i.weight.toFixed(1)}kg · 可加入${max}`:'制造材料，不能作为远征补给'):'');
  const detailIcon=$('Item.DetailIcon');show(detailIcon,!!i);if(i)icon(detailIcon,i.icon);
  text('Item.QuantityLabel',isSupply?'加入数量':'');text('Supply.QuantityValue',isSupply&&max?quantity:'');
  const slider=$('Supply.AddQuantity');slider.max=String(Math.max(1,max));setSlider(slider,quantity);slider.disabled=max<=1||locked();
  show(slider,isSupply&&max>1);show(action('DecreaseQuantity'),isSupply&&max>1);show(action('IncreaseQuantity'),isSupply&&max>1);
  action('DecreaseQuantity').disabled=locked()||quantity<=1;action('IncreaseQuantity').disabled=locked()||quantity>=max;
  text('Supply.QuantityHint',isSupply?(locked()?'任务期间无法修改携行':max===0?'无可加入数量':max===1?'仅剩1个可加入，数量固定':''):'');
  show($('Supply.QuantityHint'),isSupply&&(max<=1||locked()));show(action('AddSupply'),!!isSupply);action('AddSupply').disabled=locked()||max===0;
  rows('Loadout.List','LoadoutEntry',Object.entries(state.loadout).filter(([,n])=>n>0).map(([id,n])=>({...item(id),n})),(n,i,_idx,q,tx)=>{
   icon(q('Supply.Icon'),i.icon);tx('Supply.Name',i.name);tx('Supply.Summary',`× ${i.n} / ${(i.weight*i.n).toFixed(1)}kg`);
   q('Action.Decrease').disabled=locked();q('Action.Remove').disabled=locked();
   q('Action.Decrease').onclick=()=>{if(locked())return;state.loadout[i.id]--;if(state.loadout[i.id]<=0)delete state.loadout[i.id];save();render();};
   q('Action.Remove').onclick=()=>{if(locked())return;delete state.loadout[i.id];save();render();};
  });
  text('Loadout.Count',Object.keys(state.loadout).length+'类');show($('Loadout.Status'),!Object.keys(state.loadout).length);
  text('Loadout.Weight',`重量${weight().toFixed(1)} / ${state.capacity.toFixed(1)}kg`);
  $('Loadout.WeightFill').style.width=Math.max(0,Math.min(436,436*weight()/state.capacity))+'px';
  action('ClearLoadout').disabled=locked()||!Object.keys(state.loadout).length;save();
 }
 function renderSettings(){
  if(!settingsDraft)settingsDraft={captain:state.captain,...state.settings};
  $('Settings.CaptainName').value=settingsDraft.captain;$('Settings.SoundEnabled').checked=settingsDraft.sound;
  text('Settings.SoundState',settingsDraft.sound?'开启':'关闭');setSlider($('Settings.MasterVolume'),settingsDraft.volume);text('Settings.VolumeLabel',settingsDraft.volume+'%');
  for(const key of ['Low','Medium','High']){color(action('Quality'+key),settingsDraft.quality===key?'#4AD7BB':'#203745');action('Quality'+key).style.color=settingsDraft.quality===key?'#102B2C':'#EEF5F7';}
  text('Settings.NameCounter',Array.from(settingsDraft.captain).length+'/12');
 }
 function applySettings(){
  const name=settingsDraft.captain.trim();
  if(!name||Array.from(name).length>12||/[\r\n\x00-\x1F\x7F]/.test(name)){text('Settings.NameError',!name?'名称不能为空':'名称须为1～12个字符且不含控制字符');return;}
  state.captain=name;state.settings={sound:settingsDraft.sound,volume:settingsDraft.volume,quality:settingsDraft.quality};save();go('StarportHomePanel');
 }
 function renderConfirm(){
  const pending=state.pending||{mode:'start',missionId:state.selectedMission},m=getMission(pending.missionId);
  if(pending.mode==='start'){
   text('Confirm.Title','确认派遣远征队？');text('Confirm.Mission',m.name);text('Confirm.Risk',m.risk);
   text('Confirm.Credits',m.credits);text('Confirm.Energy',m.energy);text('Confirm.Loadout','携行：'+summary());
   text('Confirm.Weight',`总重${weight().toFixed(1)} / ${state.capacity.toFixed(1)}kg · 演示耗时3秒`);
  }else{
   const reset=pending.mode==='reset';text('Confirm.Title',reset?'重置本次演示？':'清空携行清单？');text('Confirm.Mission',reset?'恢复全部初始演示数据':'只清空计划携行');text('Confirm.Risk','请确认');
   text('Confirm.CreditsLabel',reset?'资源、仓库、设置':'仓库物品');text('Confirm.EnergyLabel',reset?'当前任务与结果':'已选携行');
   text('Confirm.Credits',reset?'全部恢复':'不会销毁');text('Confirm.Energy',reset?'全部清除':Object.keys(state.loadout).length+'类');
   text('Confirm.Loadout',reset?'此操作将清除当前任务和未领取结果。':summary());text('Confirm.Weight',reset?'恢复到2400信用点、100能源与空携行。':'确认后携行重量归零。');
   text('Confirm.Warning',reset?'点击取消将保留全部现有数据。':'仓库库存不变，之后可重新整备。');action('Confirm').textContent=reset?'重置演示':'清空携行';
  }
  if(params.get('overlay')==='1'){document.documentElement.style.background='transparent';document.body.style.background='transparent';}
  else{const blocker=document.querySelector('[data-ui-name="Btn_ModalBlocker"]');if(blocker)blocker.style.backgroundColor='transparent';}
  setTimeout(()=>action('CancelConfirm').focus(),0);
 }
 function renderResult(){
  const a=state.active,m=a?getMission(a.missionId):null;
  if(!a||a.phase!=='pending'){
   text('Result.Title',state.lastResult?'奖励已领取':'暂无结算');text('Result.Description',state.lastResult?'本次奖励已到账，不可重复领取。':'先完成一次远征，再来查看结果。');
   text('Result.Mission',state.lastResult?getMission(state.lastResult.missionId).name:'');text('Result.PaidCosts','');text('Result.Consumption','');text('Result.State','无待领取奖励');action('ClaimRewards').disabled=true;rows('Rewards.List','RewardEntry',[],()=>{});return;
  }
  text('Result.Title','远征成功');text('Result.Mission',m.name);text('Result.PaidCosts',`信用点${m.credits} / 能源${m.energy}`);text('Result.Consumption',(a.loadout.medkit||0)>0?'急救包 -1':'本次无补给消耗');
  action('ClaimRewards').disabled=false;
  rows('Rewards.List','RewardEntry',m.rewards.map(([id,n])=>({id,n})),(n,r,_index,q,tx)=>{tx('Reward.Name',r.id==='credits'?'信用点':item(r.id)?.name||r.id);tx('Reward.Quantity','+'+r.n);icon(q('Reward.Icon'),r.id==='credits'?'Img_Credits.png':item(r.id).icon);});
 }
 function claim(){
  state=read();const a=state.active;if(!a||a.phase!=='pending')return;
  action('ClaimRewards').disabled=true;
  for(const [id,n] of getMission(a.missionId).rewards){if(id==='credits')state.credits+=n;else{const i=item(id);if(i)i.qty+=n;}}
  if((a.loadout.medkit||0)>0){const med=item('medkit');if(med)med.qty=Math.max(0,med.qty-1);state.loadout.medkit=Math.max(0,(state.loadout.medkit||0)-1);if(!state.loadout.medkit)delete state.loadout.medkit;}
  state.lastResult={id:a.id,missionId:a.missionId,claimed:true};state.active=null;save();go('StarportHomePanel');
 }
 function renderResources(){text('Player.Credits',fmt(state.credits));text('Player.Energy',`${state.energy} / 100`);text('Player.LoadoutWeight',`${weight().toFixed(1)} / ${state.capacity.toFixed(1)} kg`);}
 function render(){renderResources();({StarportHomePanel:renderHome,ExpeditionSelectPanel:renderMissions,SupplyInventoryPanel:renderInventory,GameSettingsPanel:renderSettings,ActionConfirmPanel:renderConfirm,ExpeditionResultPanel:renderResult}[panel]||(()=>{}))();}
 function setup(){
  bind('OpenExpeditions',()=>go('ExpeditionSelectPanel'));bind('OpenInventory',()=>{state.origin=panel;save();go('SupplyInventoryPanel');});bind('OpenSettings',()=>go('GameSettingsPanel'));
  bind('ResetDemo',()=>confirm('reset'));bind('ViewResult',()=>go('ExpeditionResultPanel'));bind('ReturnHome',()=>go('StarportHomePanel'));
  bind('EditLoadout',()=>{state.origin=panel;save();go('SupplyInventoryPanel');});bind('Return',()=>go(state.origin||'StarportHomePanel'));bind('StartExpedition',()=>confirm('start'));
  bind('RetryMissions',()=>{state.dataState='normal';save();render();});
  for(const key of ['All','Material','Supply'])bind('Category'+key,()=>{state.view.category=key;save();renderInventory();});
  if($('Filter.Search')){$('Filter.Search').value=state.view.search;$('Filter.Search').oninput=e=>{state.view.search=e.target.value;renderInventory();};}
  bind('ClearSearch',()=>{state.view.search='';$('Filter.Search').value='';save();renderInventory();});
  if($('Supply.AddQuantity'))$('Supply.AddQuantity').oninput=e=>{quantity=Number(e.target.value);text('Supply.QuantityValue',quantity);setSlider(e.target,quantity);};
  bind('DecreaseQuantity',()=>{quantity--;renderInventory();});bind('IncreaseQuantity',()=>{quantity++;renderInventory();});bind('AddSupply',addSupply);bind('ClearLoadout',()=>confirm('clear'));
  if($('Settings.CaptainName'))$('Settings.CaptainName').oninput=e=>{settingsDraft.captain=e.target.value;text('Settings.NameCounter',Array.from(e.target.value).length+'/12');text('Settings.NameError','');};
  if($('Settings.SoundEnabled'))$('Settings.SoundEnabled').onchange=e=>{settingsDraft.sound=e.target.checked;text('Settings.SoundState',settingsDraft.sound?'开启':'关闭');};
  if($('Settings.MasterVolume'))$('Settings.MasterVolume').oninput=e=>{settingsDraft.volume=Number(e.target.value);setSlider(e.target,settingsDraft.volume);text('Settings.VolumeLabel',settingsDraft.volume+'%');};
  for(const key of ['Low','Medium','High'])bind('Quality'+key,()=>{settingsDraft.quality=key;renderSettings();});
  bind('ResetDefaults',()=>{settingsDraft={captain:'林岚',sound:true,volume:80,quality:'High'};text('Settings.NameError','');renderSettings();});
  bind('CancelSettings',()=>go('StarportHomePanel'));bind('ApplySettings',applySettings);
  bind('CancelConfirm',dismiss);bind('CloseConfirm',dismiss);bind('Confirm',()=>{action('Confirm').disabled=true;commitConfirm();});
  bind('Postpone',()=>go('StarportHomePanel'));bind('ClaimRewards',claim);
  document.addEventListener('keydown',e=>{if(e.key!=='Escape')return;e.preventDefault();if(panel==='ActionConfirmPanel'){dismiss();return;}if(overlay){overlay.remove();overlay=null;state=read();state.pending=null;save();return;}if(panel==='SupplyInventoryPanel')go(state.origin||'StarportHomePanel');else if(panel!=='StarportHomePanel')go('StarportHomePanel');});
  window.addEventListener('message',e=>{
   if(e.origin!==location.origin||e.source!==overlay?.contentWindow||e.data?.type!=='starport-confirm-close')return;
   overlay.remove();overlay=null;state=read();state.pending=null;save();render();if(e.data.error)message(e.data.error);
  });
 }
 function fixture(name){
  state=fresh();state.fixture=name;
  if(['Prepared','Result'].includes(name))state.loadout={medkit:3,battery:2};
  if(name==='Result'){state.credits=2300;state.energy=80;state.active={id:'fixture-cargo',missionId:'cargo',phase:'pending',endsAt:Date.now()-1,loadout:{medkit:3,battery:2}};}
  if(name==='OneItem')state.inventory=[structuredClone(items[0])],state.inventory[0].qty=1;
  if(name==='ManyItems')state.inventory=Array.from({length:30},(_,i)=>({...structuredClone(items[i%items.length]),id:'sample'+i,name:items[i%items.length].name+' '+(i+1)})),state.view.selectedItem='sample0';
  if(name==='EmptyInventory')state.inventory=[];
  if(name==='SearchEmpty')state.view.search='不存在的物品';
  if(name==='Insufficient')state.credits=50,state.energy=10;
  if(name==='Overweight')state.capacity=10,state.loadout={medkit:4,battery:2},state.view.selectedItem='ration';
  if(name==='LongText')state.captain='星港回收探索队第一任队长',state.inventory[0].name='星港特制超长名称急救补给物品边界验证用测试包',state.inventory[0].description='这是用于验证多行长文本说明的样本。'.repeat(8);
  if(name==='Loading')state.dataState='loading';if(name==='Error')state.dataState='error';if(name==='EmptyMissions')state.dataState='empty';
  state.pending=null;save();settingsDraft=null;quantity=1;
 }
 const annotations=document.createElement('div');annotations.dataset.previewAnnotations='true';annotations.style.cssText='position:absolute;left:0;top:0;width:1920px;height:1080px;pointer-events:none;';
 if(params.get('overlay')!=='1'){
  const names={StarportHomePanel:['01','星港基地'],ExpeditionSelectPanel:['02','远征选择'],SupplyInventoryPanel:['03','仓库整备'],GameSettingsPanel:['04','游戏设置'],ActionConfirmPanel:['05','操作确认'],ExpeditionResultPanel:['06','远征结算']};
  const [num,title]=names[panel],dims=panel==='ActionConfirmPanel'?'主体720×520 / 宽37.5% · 高48.1%':'主体1680×920 / 宽87.5% · 高85.2%';
  for(const [x,y,width,content,bold] of [[120,24,1000,'STARPORT SALVAGE / UI DESIGN REVIEW',true],[1310,24,480,`${num} / ${title} / v01`,false],[120,1025,1240,'画布1920×1080 / '+dims,false],[1440,1025,400,'HTML交互预览 · 非Unity运行',false]]){
   const label=document.createElement('span');label.className='node text';label.style.cssText=`left:${x}px;top:${y}px;width:${width}px;height:32px;font-size:20px;font-weight:${bold?700:400};color:#102B2C`;label.textContent=content;annotations.append(label);
  }
  document.body.append(annotations);
 }
 function scale(){const ratio=Math.min(innerWidth/1920,innerHeight/1080);const transform=`translate(${(innerWidth-1920*ratio)/2}px,${(innerHeight-1080*ratio)/2}px) scale(${ratio})`;for(const n of [root,annotations]){n.style.transformOrigin='0 0';n.style.transform=transform;}}
 window.addEventListener('resize',scale);scale();
 if(params.has('fixture'))fixture(params.get('fixture'));else save();
 setup();render();
 setInterval(()=>{
  if(panel==='ActionConfirmPanel'||overlay)return;
  state=read();if(!state.active)return;
  if(state.active.phase==='running'&&Date.now()>=state.active.endsAt){state.active.phase='pending';save();if(['ExpeditionSelectPanel','StarportHomePanel'].includes(panel)){go('ExpeditionResultPanel');return;}}
  if(panel==='StarportHomePanel')renderHome();
 },200);
 window.StarportDemo={state:()=>structuredClone(state),loadFixture:name=>{fixture(name);render();if($('Filter.Search'))$('Filter.Search').value=state.view.search;},panel,weight,summary};
 document.documentElement.dataset.previewReady='true';
})();
