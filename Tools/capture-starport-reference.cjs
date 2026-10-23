// Export real browser state for an equivalent temporary Unity review instance.
const fs = require('fs'), path = require('path');
const { chromium } = require('playwright');
const base = process.env.STARPORT_BASE_URL || 'http://127.0.0.1:8148/';
const output = path.resolve(process.env.STARPORT_EVIDENCE_DIR || 'Audit/Runs/latest/starport-reference');
const panels = ['StarportHomePanel','ExpeditionSelectPanel','SupplyInventoryPanel','GameSettingsPanel','ActionConfirmPanel','ExpeditionResultPanel'];
(async()=>{
  fs.mkdirSync(output,{recursive:true});
  const browser = await chromium.launch({channel:process.env.HTML_UI_BROWSER_CHANNEL || undefined,headless:true});
  try {
    const page = await browser.newPage({viewport:{width:1920,height:1080}});
    for (const panel of panels) {
      await page.goto(new URL(`Panels/${panel}/html/${panel}.html?fixture=${panel==='ExpeditionResultPanel'?'Result':'Prepared'}`,base).href);
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
          const row=node.closest('[data-preview-template]'), rect=node.getBoundingClientRect(), style=getComputedStyle(node);
          return {path:node===root?'':hierarchy(node,row||root),row:row?rows.indexOf(row):-1,type:node.dataset.ugui,key:node.dataset.binding||null,
            visible:style.display!=='none' && style.visibility!=='hidden' && rect.width>0 && rect.height>0,text:node.dataset.ugui==='Text'?node.textContent:null,
            value:node.value??null,checked:node.checked??null,disabled:node.disabled??false,color:style.color,background:style.backgroundColor,
            mask:style.maskImage||style.webkitMaskImage,x:rect.x-origin.x,y:rect.y-origin.y,width:rect.width,height:rect.height};
        });
        const templates=rows.map(row=>{const list=row.closest('[data-ugui="ScrollView"]'),r=row.getBoundingClientRect(),p=list.getBoundingClientRect();return {name:row.dataset.previewTemplate,parent:hierarchy(list,root),x:r.x-p.x,y:r.y-p.y,width:r.width,height:r.height,contentHeight:list.scrollHeight,opacity:Number(getComputedStyle(row).opacity)};});
        return {panel:root.dataset.panelName,width:1920,height:1080,nodes,templates};
      });
      fs.writeFileSync(path.join(output,panel+'.json'),JSON.stringify(snapshot,null,2));
      await page.screenshot({path:path.join(output,panel+'.png')});
      console.log(panel+': '+snapshot.nodes.length+' visible/source nodes, '+snapshot.templates.length+' rows');
    }
  } finally {await browser.close();}
})().catch(e=>{console.error(e);process.exitCode=1;});
