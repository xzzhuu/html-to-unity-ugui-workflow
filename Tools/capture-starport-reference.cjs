// Capture fixed browser fixtures for equivalent native UI checks.
const fs = require('fs'), path = require('path'), crypto = require('crypto');
const { chromium } = require('playwright');
const game = path.resolve(__dirname, '../Workspaces/StarportSalvage');
const base = process.env.STARPORT_BASE_URL || 'http://127.0.0.1:8148/';
const digest = file => crypto.createHash('sha256').update(fs.readFileSync(file)).digest('hex');
const read = file => JSON.parse(fs.readFileSync(file, 'utf8'));
function files(directory) {
  return fs.readdirSync(directory, {withFileTypes:true}).flatMap(item => {
    const file = path.join(directory,item.name);
    if(item.isSymbolicLink()) throw new Error('Capture inputs must not traverse links: '+file);
    return item.isDirectory() ? files(file) : [file];
  });
}
async function capture(page, panel, fixture) {
  await page.goto(new URL(`Panels/${panel}/html/${panel}.html?fixture=${encodeURIComponent(fixture)}`,base).href);
  await page.waitForFunction(()=>document.documentElement.dataset.previewReady==='true');
  await page.evaluate(()=>document.activeElement?.blur());
  const snapshot = await page.evaluate(()=>{
    const root=document.querySelector('[data-root]'), origin=root.getBoundingClientRect();
    function hierarchy(node,stop){
      const names=[];
      while(node && node!==stop){if(node.dataset.uiName)names.unshift(node.dataset.uiName);const parent=node.parentElement;if(parent?.dataset.ugui==='ScrollView')names.unshift('Group_Content');node=parent;}
      return names.join('/');
    }
    const rows=[...root.querySelectorAll('[data-preview-template]')];
    const nodes=[root,...root.querySelectorAll('[data-ugui]')].map(node=>{
      const row=node.closest('[data-preview-template]'),rect=node.getBoundingClientRect(),style=getComputedStyle(node);
      return {path:node===root?'':hierarchy(node,row||root),row:row?rows.indexOf(row):-1,type:node.dataset.ugui,key:node.dataset.binding||null,
        visible:style.display!=='none' && style.visibility!=='hidden' && rect.width>0 && rect.height>0,text:node.dataset.ugui==='Text'?node.textContent:null,
        value:node.value??null,checked:node.checked??null,disabled:node.disabled??false,color:style.color,background:style.backgroundColor,
        range:node.dataset.ugui==='Slider'?{min:Number(node.min),max:Number(node.max),wholeNumbers:node.step==='1'}:null,
        mask:style.maskImage||style.webkitMaskImage,x:rect.x-origin.x,y:rect.y-origin.y,width:rect.width,height:rect.height};
    });
    const templates=rows.map(row=>{const list=row.closest('[data-ugui="ScrollView"]'),r=row.getBoundingClientRect(),p=list.getBoundingClientRect();return {name:row.dataset.previewTemplate,parent:hierarchy(list,root),x:r.x-p.x,y:r.y-p.y,width:r.width,height:r.height,contentHeight:list.scrollHeight,opacity:Number(getComputedStyle(row).opacity)};});
    const state=window.StarportDemo.state();
    return {schemaVersion:2,panel:root.dataset.panelName,fixture:state.fixture,width:1920,height:1080,canvasBackground:getComputedStyle(document.body).backgroundColor,
      presentationData:{loadoutWeight:window.StarportDemo.weight(),capacity:state.capacity},nodes,templates};
  });
  if(snapshot.fixture!==fixture) throw new Error('Browser did not select fixture '+fixture);
  const panelDirectory=path.join(game,'Panels',panel);
  snapshot.sourceFilesSha256=Object.fromEntries(files(path.join(panelDirectory,'html')).map(file=>[path.relative(panelDirectory,file).replaceAll('\\','/'),digest(file)]));
  snapshot.gameContractsSha256=Object.fromEntries(files(path.join(game,'contracts')).filter(file=>file.endsWith('.json')).map(file=>[path.relative(game,file).replaceAll('\\','/'),digest(file)]));
  const registry=read(path.join(game,'project.assets.json'));
  const sources=registry.assets.filter(asset=>asset.approved).map(asset=>{
    const file=path.resolve(game,asset.source);
    if(!file.startsWith(game+path.sep)) throw new Error('Asset source leaves the game workspace');
    return file;
  });
  snapshot.gameInputsSha256={...snapshot.gameContractsSha256,...Object.fromEntries([path.join(game,'project.assets.json'),...sources].map(file=>[path.relative(game,file).replaceAll('\\','/'),digest(file)]))};
  return snapshot;
}
async function run(options={}) {
  const matrix=options.matrix===true;
  const output=path.resolve(process.env.STARPORT_EVIDENCE_DIR || (matrix?'Audit/Runs/latest/starport-reference-matrix':'Audit/Runs/latest/starport-reference'));
  const screens=read(path.join(game,'contracts/screens.json')).screens.filter(s=>s.implemented);
  const browser=await chromium.launch({channel:process.env.HTML_UI_BROWSER_CHANNEL || undefined,headless:true});
  const cases=[];
  try {
    const page=await browser.newPage({viewport:{width:1920,height:1080}});
    for(const screen of screens) for(const fixture of matrix?screen.fixtures:[screen.panelName==='ExpeditionResultPanel'?'Result':'Prepared']) {
      if(!/^[A-Z][A-Za-z0-9]*Panel$/.test(screen.panelName)||!/^[A-Za-z][A-Za-z0-9_-]*$/.test(fixture)) throw new Error('Unsafe capture identity');
      const snapshot=await capture(page,screen.panelName,fixture);
      snapshot.presentationRules=screen.fixturePresentation||[];
      const directory=matrix?path.join(output,screen.panelName,fixture):output;
      fs.mkdirSync(directory,{recursive:true});
      const reference=path.join(directory,screen.panelName+'.json'),screenshot=path.join(directory,screen.panelName+'.png');
      fs.writeFileSync(reference,JSON.stringify(snapshot,null,2));await page.screenshot({path:screenshot});
      cases.push({panel:screen.panelName,fixture,reference:path.relative(output,reference).replaceAll('\\','/'),screenshot:path.relative(output,screenshot).replaceAll('\\','/'),referenceSha256:digest(reference),screenshotSha256:digest(screenshot)});
      console.log(screen.panelName+'/'+fixture+': '+snapshot.nodes.length+' nodes, '+snapshot.templates.length+' rows');
    }
    fs.writeFileSync(path.join(output,'matrix.json'),JSON.stringify({schemaVersion:1,status:'passed',projectName:path.basename(game),browser:await browser.version(),viewports:[[1920,1080],[1280,720],[1920,1200]],cases},null,2));
  } finally {await browser.close();}
}
module.exports=run;
if(require.main===module)run({matrix:process.argv.includes('--matrix')}).catch(error=>{console.error(error);process.exitCode=1;});
