const fs = require('fs');
const path = require('path');
const pw = require(path.join(process.env.USERPROFILE, '.nuget/packages/microsoft.playwright/1.61.0/.playwright/package'));
const url = 'file:///' + path.join(__dirname, '54-contextmenu-probe.html').split(path.sep).join('/');
const outDir = __dirname;

(async () => {
  const pages = [];
  for (const engine of ['chromium', 'firefox', 'webkit']) {
    const browser = await pw[engine].launch({ headless: false });
    const page = await browser.newPage({ viewport: { width: 900, height: 700 } });
    await page.goto(url);
    await page.evaluate(e => { document.title = e + ' probe'; document.querySelector('h1').textContent = e + ' ' + navigator.userAgent.match(/(Chrome|Firefox|Version)\/[\d.]+/)?.[0]; }, engine);
    pages.push({ engine, page });
  }
  setInterval(async () => {
    for (const { engine, page } of pages) {
      try {
        const log = await page.evaluate(() => window.__probeLog);
        fs.writeFileSync(path.join(outDir, `54-hand-${engine}.log`), log.join('\n'));
      } catch { }
    }
  }, 1000);
})().catch(e => { console.error(e); process.exit(1); });
