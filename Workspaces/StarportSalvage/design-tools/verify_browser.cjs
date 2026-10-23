/* Real Chromium interaction and screenshots for the local six-panel demo. */
const fs=require('fs'),path=require('path'),assert=require('assert/strict');
const {chromium}=require('playwright');
const root=path.resolve(__dirname,'..'),evidence=process.env.STARPORT_EVIDENCE_DIR?path.resolve(process.env.STARPORT_EVIDENCE_DIR):path.resolve(root,'../../Audit/Runs/latest/starport-browser');fs.mkdirSync(evidence,{recursive:true});
const panels=['StarportHomePanel','ExpeditionSelectPanel','SupplyInventoryPanel','GameSettingsPanel','ActionConfirmPanel','ExpeditionResultPanel'];
const base=(process.env.STARPORT_BASE_URL || 'http://127.0.0.1:8148/').replace(/\/?$/, '/');
const url=(p,f)=>base+'Panels/'+p+'/html/'+p+'.html'+(f?'?fixture='+f:'');
const checks=[],errors=[];
let browser,page;
const b=k=>page.locator(`[data-binding="${k}"]:not([data-preview-template] *)`).first();
const act=k=>b('Action.'+k);
const state=()=>page.evaluate(()=>window.StarportDemo.state());
async function visit(p,fixture){await page.goto(url(p,fixture));await page.waitForFunction(()=>document.documentElement.dataset.previewReady==='true');}
async function ready(p){await page.waitForURL('**/'+p+'.html');await page.waitForFunction(()=>document.documentElement.dataset.previewReady==='true');}
async function fixture(name){await page.evaluate(n=>window.StarportDemo.loadFixture(n),name);}
async function shot(name){await page.screenshot({path:path.join(evidence,name+'.png'),fullPage:false});}
async function test(name,fn){try{await fn();checks.push({name,status:'passed'});console.log('PASS',name);}catch(e){checks.push({name,status:'failed',error:e.message});console.error('FAIL',name,e.message);throw e;}}
async function modal(){const el=await page.waitForSelector('iframe');const frame=await el.contentFrame();await frame.waitForURL('**/*overlay=1');await frame.waitForFunction(()=>document.documentElement.dataset.previewReady==='true');return frame;}
async function clickItem(id){await page.locator(`[data-preview-template="InventoryEntry"][data-item-id="${id}"]`).click();}
async function slider(node,value){await node.evaluate((el,value)=>{el.value=value;el.dispatchEvent(new Event('input',{bubbles:true}));},String(value));}
async function main(){
 browser=await chromium.launch({channel:process.env.HTML_UI_BROWSER_CHANNEL || undefined,headless:true});
 const context=await browser.newContext({viewport:{width:1920,height:1080},deviceScaleFactor:1});
 page=await context.newPage();page.on('pageerror',e=>errors.push(e.message));
 page.on('response',r=>{if(r.status()>=400)errors.push(r.status()+' '+r.url());});
 await test('Six independent XHTML pages render without errors',async()=>{
  for(const p of panels){await visit(p,p==='ExpeditionResultPanel'?'Result':'Prepared');await shot(p);
   assert.equal(await page.locator('[data-root]').count(),1);assert.equal(await page.locator('[data-root]').evaluate(n=>n.offsetWidth),1920);
   for(const img of await page.locator('.icon').evaluateAll(ns=>ns.map(n=>getComputedStyle(n).maskImage)))assert.ok(img.includes('.png'));
  }
 });
 await test('Default data starts with empty loadout',async()=>{await visit(panels[0],'Default');const s=await state();assert.equal(s.credits,2400);assert.deepEqual(s.loadout,{});assert.equal(await b('Player.LoadoutWeight').innerText(),'0.0 / 20.0 kg');});
 await test('Inventory selection, quantity, merge and stock accounting',async()=>{
  await act('OpenInventory').click();await ready('SupplyInventoryPanel');
  await slider(b('Supply.AddQuantity'),3);await act('AddSupply').click();let s=await state();assert.equal(s.loadout.medkit,3);assert.equal(s.inventory.find(i=>i.id==='medkit').qty,4);
  await clickItem('battery');await slider(b('Supply.AddQuantity'),2);await act('AddSupply').click();s=await state();assert.equal(s.loadout.battery,2);assert.equal(await page.evaluate(()=>StarportDemo.weight()),7);
  await shot('InventoryPreparedFromInteraction');
 });
 await test('Search intersects category, clears detail and preserves zero inventory',async()=>{
  await act('CategoryMaterial').click();assert.equal(await page.locator('[data-preview-template="InventoryEntry"]').count(),4);
  await b('Filter.Search').fill('不存在的物品');assert.equal(await page.locator('[data-preview-template="InventoryEntry"]').count(),0);assert.match(await b('Inventory.Status').innerText(),/没有找到/);await shot('SearchEmpty');
  await act('ClearSearch').click();await act('CategorySupply').click();await clickItem('beacon');assert.equal(await act('AddSupply').isDisabled(),true);
  await act('CategoryAll').click();await act('Return').click();await ready('StarportHomePanel');assert.equal((await state()).loadout.medkit,3);
 });
 await test('Locked mission and confirmation cancel never charge',async()=>{
  await act('OpenExpeditions').click();await ready('ExpeditionSelectPanel');
  await page.locator('[data-preview-template="MissionEntry"][data-item-id="signal"]').click();assert.equal(await b('Mission.Name').innerText(),'失联信号塔');assert.equal(await b('Mission.CreditsCost').innerText(),'200');assert.equal(await b('Mission.RewardName1').innerText(),'电路板');
  await page.locator('[data-preview-template="MissionEntry"][data-item-id="reactor"]').click();assert.equal(await act('StartExpedition').isDisabled(),true);assert.match(await b('Mission.Deficit').innerText(),/未解锁/);await shot('LockedMission');
  await page.locator('[data-preview-template="MissionEntry"][data-item-id="cargo"]').click();await act('StartExpedition').click();let f=await modal();assert.match(await f.locator('[data-binding="Confirm.Loadout"]').innerText(),/急救包×3/);await shot('OverlayConfirm');
  await f.locator('[data-binding="Action.CancelConfirm"]').click();await page.waitForFunction(()=>!document.querySelector('iframe'));assert.equal((await state()).credits,2400);
 });
 await test('Three-second expedition, same-session result, one-time claim',async()=>{
  await act('StartExpedition').click();const f=await modal();await f.locator('[data-binding="Action.Confirm"]').click();await page.waitForFunction(()=>!document.querySelector('iframe'));
  let s=await state();assert.equal(s.credits,2300);assert.equal(s.energy,80);assert.equal(s.active.phase,'running');
  await act('ReturnHome').click();await ready('StarportHomePanel');assert.equal(await act('OpenExpeditions').isDisabled(),true);
  await ready('ExpeditionResultPanel');assert.equal(await page.locator('[data-preview-template="RewardEntry"]').count(),3);await shot('RealExpeditionResult');
  await act('Postpone').click();await ready('StarportHomePanel');assert.equal((await state()).credits,2300);await act('ViewResult').click();await ready('ExpeditionResultPanel');
  await act('ClaimRewards').dblclick({noWaitAfter:true}).catch(()=>{});await ready('StarportHomePanel');s=await state();assert.equal(s.credits,2600);assert.equal(s.energy,80);assert.equal(s.inventory.find(i=>i.id==='ore').qty,28);assert.equal(s.inventory.find(i=>i.id==='alloy').qty,7);assert.equal(s.inventory.find(i=>i.id==='medkit').qty,3);assert.deepEqual(s.loadout,{medkit:2,battery:2});assert.equal(await page.evaluate(()=>StarportDemo.weight()),6);
  await visit('ExpeditionResultPanel');assert.equal(await act('ClaimRewards').isDisabled(),true);assert.equal((await state()).credits,2600);await shot('ClaimedResult');
 });
 await test('Settings reject invalid names, cancel drafts, apply valid values',async()=>{
  await visit('GameSettingsPanel','Default');await b('Settings.CaptainName').fill('   ');await act('ApplySettings').click();assert.match(await b('Settings.NameError').innerText(),/不能为空/);
  await b('Settings.CaptainName').fill('一二三四五六七八九十一二三');await act('ApplySettings').click();assert.match(await b('Settings.NameError').innerText(),/1～12/);await shot('InvalidSettings');
  await b('Settings.CaptainName').fill('测试队长');await act('CancelSettings').click();await ready('StarportHomePanel');assert.equal((await state()).captain,'林岚');
  await act('OpenSettings').click();await ready('GameSettingsPanel');await b('Settings.CaptainName').fill('一二三四五六七八九十一二');await b('Settings.SoundEnabled').uncheck();await slider(b('Settings.MasterVolume'),35);await act('QualityLow').click();await act('ApplySettings').click();await ready('StarportHomePanel');assert.equal(Array.from((await state()).captain).length,12);assert.deepEqual((await state()).settings,{sound:false,volume:35,quality:'Low'});
  await act('OpenSettings').click();await ready('GameSettingsPanel');assert.equal(await b('Settings.MasterVolume').inputValue(),'35');await act('ResetDefaults').click();await act('CancelSettings').click();await ready('StarportHomePanel');assert.equal((await state()).settings.volume,35);
 });
 await test('Empty, single, thirty items, scroll and long text',async()=>{
  await visit('SupplyInventoryPanel','EmptyInventory');assert.match(await b('Inventory.Status').innerText(),/仓库暂无/);assert.equal(await act('AddSupply').isVisible(),false);await shot('EmptyInventory');
  await fixture('OneItem');assert.equal(await page.locator('[data-preview-template="InventoryEntry"]').count(),1);assert.equal(await b('Supply.AddQuantity').isVisible(),false);await shot('OneItem');
  await fixture('ManyItems');assert.equal(await page.locator('[data-preview-template="InventoryEntry"]').count(),30);assert.ok(await b('Inventory.List').evaluate(n=>n.scrollHeight>n.clientHeight));await b('Inventory.List').evaluate(n=>n.scrollTop=n.scrollHeight);await shot('ManyItems');
  await fixture('LongText');await b('Inventory.List').evaluate(n=>n.scrollTop=0);assert.equal(await b('Item.DetailName').evaluate(n=>n.scrollHeight<=n.clientHeight),true);assert.ok(await b('Item.DescriptionScroll').evaluate(n=>n.scrollHeight>n.clientHeight));await shot('LongText');
 });
 await test('Overweight rejects atomically; zero stock, remove and clear',async()=>{
  await fixture('Overweight');await slider(b('Supply.AddQuantity'),5);await act('AddSupply').click();assert.equal((await state()).loadout.ration,undefined);assert.equal(await page.evaluate(()=>StarportDemo.weight()),8);assert.match(await b('Feedback.Message').innerText(),/超重/);await shot('OverweightRejected');
  const row=page.locator('[data-preview-template="LoadoutEntry"][data-item-id="medkit"]');await row.locator('[data-binding="Action.Decrease"]').click();assert.equal((await state()).loadout.medkit,3);
  await page.locator('[data-preview-template="LoadoutEntry"][data-item-id="battery"] [data-binding="Action.Remove"]').click();assert.equal((await state()).loadout.battery,undefined);
  await act('ClearLoadout').click();const f=await modal();assert.match(await f.locator('[data-binding="Confirm.Title"]').innerText(),/清空/);await f.locator('[data-binding="Action.Confirm"]').click();await page.waitForFunction(()=>!document.querySelector('iframe'));assert.deepEqual((await state()).loadout,{});
 });
 await test('Deficits, loading, error/retry and empty missions',async()=>{
  await visit('ExpeditionSelectPanel','Insufficient');assert.match(await b('Mission.Deficit').innerText(),/50信用点/);assert.match(await b('Mission.Deficit').innerText(),/10能源/);assert.equal(await act('StartExpedition').isDisabled(),true);await shot('Insufficient');
  await fixture('Loading');assert.match(await b('Missions.Status').innerText(),/加载/);assert.equal(await act('StartExpedition').isDisabled(),true);await shot('Loading');
  await fixture('Error');assert.equal(await act('RetryMissions').isVisible(),true);await shot('Error');await act('RetryMissions').click();assert.equal(await page.locator('[data-preview-template="MissionEntry"]').count(),3);
  await fixture('EmptyMissions');assert.match(await b('Missions.Status').innerText(),/暂无/);await shot('EmptyMissions');
 });
 await test('Task snapshot locks loadout; no-medkit result; reset/cancel and Esc',async()=>{
  await visit('SupplyInventoryPanel','Result');assert.equal(await act('AddSupply').isDisabled(),true);assert.equal(await act('ClearLoadout').isDisabled(),true);
  await page.evaluate(()=>{const s=StarportDemo.state();s.active.loadout={};sessionStorage.setItem('starport-salvage-demo-v1',JSON.stringify(s));});await visit('ExpeditionResultPanel');assert.match(await b('Result.Consumption').innerText(),/无补给消耗/);await shot('NoConsumables');
  await visit('StarportHomePanel');await act('ResetDemo').click();let f=await modal();await f.locator('body').press('Escape');await page.waitForFunction(()=>!document.querySelector('iframe'));assert.equal((await state()).credits,2300);
  await act('ResetDemo').click();f=await modal();await f.locator('[data-binding="Action.Confirm"]').click();await page.waitForFunction(()=>!document.querySelector('iframe'));assert.equal((await state()).credits,2400);assert.deepEqual((await state()).loadout,{});assert.equal((await state()).active,null);
 });
 await test('Uniform scaling at 1280x720 and 1920x1200',async()=>{
  for(const [width,height] of [[1280,720],[1920,1200]]){await page.setViewportSize({width,height});await visit('SupplyInventoryPanel','Prepared');const r=await page.locator('[data-root]').boundingBox();assert.ok(Math.abs(r.width/r.height-16/9)<.001);assert.ok(r.x>=0&&r.y>=0&&r.x+r.width<=width+1&&r.y+r.height<=height+1);await shot('Scaled'+width+'x'+height);}
  await page.setViewportSize({width:1920,height:1080});
 });
 await test('Demo launcher switches panels and fixtures outside the game UI',async()=>{
  await page.goto(base+'index.html');await page.locator('#prepared').click();let frame=await (await page.waitForSelector('#game')).contentFrame();await frame.waitForFunction(()=>window.StarportDemo?.panel==='SupplyInventoryPanel');assert.equal(await frame.evaluate(()=>StarportDemo.weight()),7);
  await page.locator('#fixture').selectOption('Overweight');await frame.waitForFunction(()=>window.StarportDemo?.state().fixture==='Overweight');assert.equal(await frame.evaluate(()=>StarportDemo.state().capacity),10);
  await page.locator('#initial').click();await frame.waitForFunction(()=>window.StarportDemo?.panel==='StarportHomePanel');assert.equal(await frame.evaluate(()=>StarportDemo.state().credits),2400);await shot('Launcher');
 });
 await test('Browser console and local resources clean',async()=>assert.deepEqual(errors,[]));
 fs.writeFileSync(path.join(evidence,'checks.json'),JSON.stringify({status:'passed',browser:await browser.version(),checks,errors},null,2));
 await browser.close();
}
async function run(){try{await main();}catch(e){fs.writeFileSync(path.join(evidence,'checks.json'),JSON.stringify({status:'failed',checks,errors,error:e.stack},null,2));if(page)await shot('Failure').catch(()=>{});if(browser)await browser.close();throw e;}}
module.exports=run;
if(require.main===module)run().catch(e=>{console.error(e.message);process.exitCode=1;});
