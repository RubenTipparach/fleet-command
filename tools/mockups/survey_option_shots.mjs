// Screenshot each Vhorr Brood console component from a console-skins page whose organic kit is one style's renders, for
// the survey's picture options (owner, 2026-11-01: "For every question in the survey please show the images specifically
// that you are asking me about ... don't show kits. Show specifically the options you are asking me to pick").
// Each component is cropped from the live console (labels and readouts drawn on top as on the page), grown by its slots'
// bleed so horns and tentacles past the slot show, at 2x.
//
//   NODE_PATH=$(npm root -g) node tools/mockups/survey_option_shots.mjs <page.html> <out dir>
//   NODE_PATH=$(npm root -g) node tools/mockups/survey_option_shots.mjs <page.html> <out dir> console
// The second form shoots the whole console instead (console.png at desktop size, phone.png at a phone's landscape
// size), with the backing the page names in window.BROOD_BACKING ({ src, rect: [x, y, w, h] in console px}) drawn
// under every panel and the battle view filling the whole console under it (the survey's W8 options: what fills the
// space between the panels).
import { createRequire } from 'module';
const { chromium } = createRequire(import.meta.url)('playwright');
const [page, out, mode] = process.argv.slice(2);
// One survey component per entry: the slots it shows, and an optional share of the first slot's width (the strip).
const COMPONENTS = {
  strip: { slots: ['strip.top'], width: 0.3 },
  ssd: { slots: ['ssd.bezel'] },
  helm: { slots: ['helm.pad', 'inset.helm-readout'] },
  weapons: { slots: ['panel.weapons'] },
  bottom: { slots: ['dial', 'inset.contacts', 'crest'] },
  nameplate: { slots: ['nameplate'] },
};
const b = await chromium.launch();
if (mode === 'console') {
  const res = {};
  const errs = [];
  for (const [name, vp, dsf] of [['console', { width: 1920, height: 1080 }, 2], ['phone', { width: 844, height: 390 }, 3]]) {
    const p = await b.newPage({ viewport: vp, deviceScaleFactor: dsf });
    p.on('pageerror', (e) => errs.push(e.message));
    await p.goto('file://' + page);
    await p.waitForFunction(() => window.MOCKUP_READY, null, { timeout: 120000 });
    await p.evaluate(() => window.MOCKUP_SHOTS.vhorr());
    await p.waitForTimeout(2500);
    const box = await p.evaluate(() => {
      const con = [...document.querySelectorAll('.console')].find((n) => n.offsetParent !== null);
      const B = window.BROOD_BACKING;
      if (B) {
        const im = document.createElement('img');
        im.className = 'brood-backing';
        im.src = B.src;
        im.style.cssText = `position:absolute;left:${B.rect[0]}px;top:${B.rect[1]}px;width:${B.rect[2]}px;height:${B.rect[3]}px;z-index:1;pointer-events:none;`;
        const view = con.querySelector(':scope > .view');
        view.style.cssText += 'left:0;top:0;right:0;bottom:0;z-index:0;';
        // (the page draws the battle at the whole console's size when it has a backing: survey_option_images.py)
        con.style.background = 'transparent';
        for (const n of con.children) if (n !== view) { if (getComputedStyle(n).position === 'static') n.style.position = 'relative'; n.style.zIndex = '2'; }
        con.insertBefore(im, con.firstChild);
      }
      document.body.style.visibility = 'hidden';
      con.style.visibility = 'visible';
      con.parentElement.scrollIntoView();
      const r = con.parentElement.getBoundingClientRect();
      return { x: r.x + scrollX, y: r.y + scrollY, width: r.width, height: r.height };
    });
    await p.waitForTimeout(800);
    await p.screenshot({ path: `${out}/${name}.png`, clip: box, fullPage: true });
    res[name] = box;
    await p.close();
  }
  console.log(JSON.stringify({ info: res, errors: errs }));
  await b.close();
  process.exit(0);
}
const p = await b.newPage({ viewport: { width: 1920, height: 1080 }, deviceScaleFactor: 2 });
const errs = [];
p.on('pageerror', (e) => errs.push(e.message));
await p.goto('file://' + page);
await p.waitForFunction(() => window.MOCKUP_READY, null, { timeout: 120000 });
await p.evaluate(() => window.MOCKUP_SHOTS.vhorr());
await p.waitForTimeout(2500);
const info = {};
for (const [name, c] of Object.entries(COMPONENTS)) {
  // only this component's slots show (and what is inside them); every other slot of the console is hidden
  await p.evaluate((ids) => {
    // nothing of the page but the console shows (its headings would sit in a crop that reaches past the console)
    document.body.style.visibility = 'hidden';
    const con = document.querySelector('.console, #console, .stage');
    con.style.visibility = 'visible';
    for (let a = con; a; a = a.parentElement) a.style.overflow = 'visible';
    const keep = [...document.querySelectorAll('[data-slot]')].filter((n) => ids.includes(n.dataset.slot) && n.offsetParent !== null);
    for (const n of document.querySelectorAll('[data-slot]')) {
      n.style.visibility = keep.some((k) => k === n || k.contains(n)) ? 'visible' : 'hidden';
    }
  }, c.slots);
  await p.waitForTimeout(300);
  const boxes = await p.evaluate((ids) => ids.map((id) => {
    const e = [...document.querySelectorAll(`[data-slot="${id}"]`)].find((n) => n.offsetParent !== null);
    if (!e) return null;
    const r = e.getBoundingClientRect();
    const scale = r.width / e.offsetWidth;
    return { x: r.x + scrollX, y: r.y + scrollY, w: r.width, h: r.height, scale };
  }), c.slots);
  if (boxes.some((x) => !x)) throw new Error(`${name}: a slot is missing`);
  const scale = boxes[0].scale, bleed = (name === 'strip' ? 46 : 30) * scale, m = 8 * scale;   // the largest slot bleed (mm) at the page's scale
  let x0 = Math.min(...boxes.map((q) => q.x)), y0 = Math.min(...boxes.map((q) => q.y));
  let x1 = Math.max(...boxes.map((q) => q.x + q.w)), y1 = Math.max(...boxes.map((q) => q.y + q.h));
  if (c.width) x1 = x0 + (x1 - x0) * c.width;
  const cx0 = Math.max(0, x0 - bleed - m), cy0 = Math.max(0, y0 - bleed - m), cx1 = x1 + bleed + m, cy1 = y1 + bleed + m;
  const clip = { x: cx0, y: cy0, width: cx1 - cx0, height: cy1 - cy0 };
  await p.screenshot({ path: `${out}/${name}.png`, clip, fullPage: true });
  info[name] = { clip, scale };
}
console.log(JSON.stringify({ info, errors: errs }));
await b.close();
