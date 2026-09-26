const fs = require('fs');
const path = require('path');
const { pathToFileURL } = require('url');
const { chromium } = require('playwright');
(async () => {
  const root = path.resolve(__dirname, '..');
  const browser = await chromium.launch({channel:'chrome',headless:true});
  try {
    const page = await browser.newPage({viewport:{width:1280,height:720}});
    const errors=[];
    page.on('pageerror', e=>errors.push(e.message));
    page.on('requestfailed', r=>errors.push(r.url()));
    await page.goto(pathToFileURL(path.join(root,'Examples/Starter/html/StarterPanel.html')).href);
    const result=await page.evaluate(()=>({ready:document.documentElement.dataset.previewReady==='true',roots:document.querySelectorAll('[data-root="true"]').length,title:document.querySelector('#Title').textContent,rootSize:[document.querySelector('[data-root]').offsetWidth,document.querySelector('[data-root]').offsetHeight]}));
    if(errors.length || !result.ready || result.roots!==1 || result.rootSize.join('x')!=='1280x720') throw Error(JSON.stringify({errors,result}));
    await page.screenshot({path:path.join(root,'Audit/Evidence/StarterPanel.browser.png')});
    fs.writeFileSync(path.join(root,'Audit/Evidence/starter-browser.json'),JSON.stringify({errors,...result},null,2));
    console.log(JSON.stringify({errors,...result}));
  } finally {await browser.close();}
})().catch(e=>{console.error(e);process.exitCode=1;});
