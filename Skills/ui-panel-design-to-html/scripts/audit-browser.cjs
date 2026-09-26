// npm install playwright, or use the bundled runtime's NODE_PATH.
const fs = require('fs');
const path = require('path');
const { pathToFileURL } = require('url');
const { chromium } = require('playwright');
(async () => {
  const [input, output, name = path.basename(input || '', '.html')] = process.argv.slice(2);
  if (!input || !output) throw new Error('Usage: node audit-browser.cjs panel.html output-folder [PanelName]');
  fs.mkdirSync(output, { recursive: true });
  const browser = await chromium.launch({ channel: 'chrome', headless: true });
  try {
    const page = await browser.newPage({ viewport: { width: 1920, height: 1280 }, deviceScaleFactor: 1 });
    const issues = [];
    page.on('pageerror', e => issues.push(`JavaScript: ${e.message}`));
    page.on('requestfailed', r => issues.push(`Request failed: ${r.url()}`));
    await page.goto(pathToFileURL(path.resolve(input)).href);
    await page.evaluate(async () => { await document.fonts.ready; await Promise.all(Array.from(document.images).map(img => img.decode().catch(() => {}))); });
    if (await page.locator('[data-root="true"]').count() !== 1) throw Error('Exactly one root required');
    const size = await page.locator('[data-root="true"]').boundingBox();
    await page.setViewportSize({ width: Math.ceil(size.width), height: Math.ceil(size.height) });
    const result = await page.evaluate(() => {
      const root = document.querySelector('[data-root="true"]');
      const base = root.getBoundingClientRect();
      const bindings = Array.from(root.querySelectorAll('[data-binding]')).map(e => {
        const r = e.getBoundingClientRect();
        return { key: e.dataset.binding, templateScope: e.closest('[data-preview-template]')?.dataset.previewTemplate || null, component: e.dataset.ugui, text: e.textContent.trim(), x: r.x-base.x, y: r.y-base.y, width: r.width, height: r.height };
      });
      // Scope is explicit: one dynamic row must still remain an item binding.
      const main = bindings.filter(b => !b.templateScope);
      return { width: base.width, height: base.height, bindings: main, dynamicBindings: bindings.filter(b => b.templateScope), images: Array.from(root.querySelectorAll('img')).filter(e=>!e.complete || !e.naturalWidth).map(e=>e.src) };
    });
    result.issues = [...issues, ...result.images.map(x => `Missing image: ${x}`)];
    await page.screenshot({ path: path.join(output, `${name}.browser.png`) });
    fs.writeFileSync(path.join(output, `${name}.browser.json`), JSON.stringify(result, null, 2));
    console.log(JSON.stringify({ name, width: result.width, height: result.height, bindings: result.bindings.length, issues: result.issues }));
    if (result.issues.length) process.exitCode = 1;
  } finally { await browser.close(); }
})().catch(e => { console.error(e.message); process.exitCode = 1; });
