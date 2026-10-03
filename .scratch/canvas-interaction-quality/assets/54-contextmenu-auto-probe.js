const path = require('path');
const pw = require(path.join(process.env.USERPROFILE, '.nuget/packages/microsoft.playwright/1.61.0/.playwright/package'));
const url = 'file:///' + path.join(__dirname, '54-contextmenu-probe.html').split(path.sep).join('/');
const headless = process.argv[2] !== 'headed';

async function section(page, title, fn) {
  await page.evaluate(() => { window.__probeLog.length = 1; });
  await fn();
  await page.waitForTimeout(150);
  const log = await page.evaluate(() => window.__probeLog.slice(1));
  console.log(`  -- ${title}`);
  for (const line of log) console.log('    ' + line);
}

async function setBox(page, id, on) {
  await page.evaluate(([id, on]) => { document.getElementById(id).checked = on; }, [id, on]);
}

(async () => {
  for (const engine of ['chromium', 'firefox', 'webkit']) {
    const browser = await pw[engine].launch({ headless });
    const page = await browser.newPage();
    await page.goto(url);
    console.log(`== ${engine} ${browser.version()} headless=${headless}`);
    const a = await page.locator('#a').boundingBox();
    const b = await page.locator('#b').boundingBox();
    const y = a.y + a.height / 2;
    const pressX = a.x + a.width - 1.5;

    await section(page, 'right press in A, move 3px into B, release', async () => {
      await page.mouse.move(pressX, y);
      await page.mouse.down({ button: 'right' });
      await page.mouse.move(pressX + 3, y);
      await page.mouse.up({ button: 'right' });
    });

    await section(page, 'right press and release in A, no movement', async () => {
      await page.mouse.move(a.x + 20, y);
      await page.mouse.down({ button: 'right' });
      await page.mouse.up({ button: 'right' });
    });

    for (const prevent of [false, true]) {
      await setBox(page, 'preventShiftF10', prevent);
      await page.focus('#stop');
      await section(page, `Shift+F10 on tab stop, preventDefault ${prevent ? 'on' : 'off'}`, async () => {
        await page.keyboard.press('Shift+F10');
      });
    }
    await setBox(page, 'preventShiftF10', false);

    for (const prevent of [false, true]) {
      await setBox(page, 'preventMenuKey', prevent);
      await page.focus('#stop');
      await section(page, `ContextMenu key on tab stop (down, pause, up), preventDefault ${prevent ? 'on' : 'off'}`, async () => {
        await page.keyboard.down('ContextMenu');
        await page.waitForTimeout(100);
        await page.evaluate(() => window.__probeLog.push('         --- pause between keydown and keyup ---'));
        await page.keyboard.up('ContextMenu');
      });
    }
    await browser.close();
  }
})().catch(e => { console.error(e); process.exit(1); });
