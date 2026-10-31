// Measure, on the real page, that every skin's strip readouts sit inside their strip (the analog panel bible's
// children-inside-parents rule, chapter 7: owner, 2026-10-31, "make sure the UI fits well inside parent elements").
// For each skin it switches the console-skins page to that skin and prints, for every bar in the top and bottom
// strips, its distance in reference px (the console is drawn scaled) to the strip's content box (top, bottom) and to
// the strip's own edges; it exits 1 if a bar leaves its content box or comes within 2 px of the strip's edge.
//
//   NODE_PATH=$(npm root -g) node tools/mockups/strip_fit.mjs docs/mockups/console-skins.html
import { createRequire } from 'node:module';
import path from 'node:path';
const { chromium } = createRequire(import.meta.url)('playwright');
const browser = await chromium.launch();
let over = 0;
const page = await browser.newPage({ viewport: { width: 1920, height: 1080 } });
await page.goto('file://' + path.resolve(process.argv[2]), { timeout: 120000 });
await page.waitForFunction(() => window.MOCKUP_READY, null, { timeout: 240000 });
const ids = await page.evaluate(() => [...document.querySelectorAll('#switch button')].map((b) => b.dataset.id));
for (const id of ids) {
  const r = await page.evaluate((id) => {
    document.querySelector(`#switch button[data-id="${id}"]`).click();
    const out = [];
    for (const s of document.querySelectorAll('.k-strip')) {
      const sr = s.getBoundingClientRect(), cs = getComputedStyle(s), k = sr.height / parseFloat(cs.height);
      const top = sr.top + parseFloat(cs.paddingTop) * k, bot = sr.bottom - parseFloat(cs.paddingBottom) * k;
      for (const b of s.querySelectorAll('.barbox')) { const br = b.getBoundingClientRect(); out.push([s.classList.contains('top') ? 'top' : 'bottom', +((br.top - top) / k).toFixed(1), +((bot - br.bottom) / k).toFixed(1), +((br.top - sr.top) / k).toFixed(1), +((sr.bottom - br.bottom) / k).toFixed(1)]); }
    }
    return out;
  }, id);
  const bad = r.filter(([, a, b, c, d]) => a < 0 || b < 0 || c < 2 || d < 2);
  over += bad.length;
  console.log(id.padEnd(9), bad.length ? 'OVER ' + JSON.stringify(bad) : 'inside', JSON.stringify(r[0]), JSON.stringify(r[r.length - 1]));
}
await browser.close();
process.exit(over ? 1 : 0);
