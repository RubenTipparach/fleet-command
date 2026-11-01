// Screenshot each Vhorr Brood console component from a console-skins page whose organic kit is one style's renders, for
// the survey's picture options (owner, 2026-11-01: "For every question in the survey please show the images specifically
// that you are asking me about ... don't show kits. Show specifically the options you are asking me to pick").
// Each component is cropped from the live console (labels and readouts drawn on top as on the page), grown by its slots'
// bleed so horns and tentacles past the slot show, at 2x.
//
//   NODE_PATH=$(npm root -g) node tools/mockups/survey_option_shots.mjs <page.html> <out dir>
import { createRequire } from 'module';
const { chromium } = createRequire(import.meta.url)('playwright');
const [page, out] = process.argv.slice(2);
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
