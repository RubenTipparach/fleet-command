// Measure the joins of every emblem sketch on the Emblem Design Guide (docs/design/emblem-guide.html): each sketch must
// be the number of pieces its design says, and parts that stand apart must be at least 4 grid units clear (owner,
// 2026-11-03: "you can see gaps in the shape, this is low quality work"). The measuring is the page's own
// window.EMBLEM_CHECK (one implementation); this runs it headless and fails on a bad join.
//
//   NODE_PATH=$(npm root -g) node tools/emblems/sketch_check.mjs [docs/design/emblem-guide.html]
import { createRequire } from 'node:module';
import path from 'node:path';

const { chromium } = createRequire(import.meta.url)('playwright');
const page_ = process.argv[2] || 'docs/design/emblem-guide.html';
const browser = await chromium.launch();
const page = await browser.newPage();
await page.goto('file://' + path.resolve(page_));
await page.waitForFunction(() => window.MOCKUP_READY, null, { timeout: 60000 });
const out = await page.evaluate(() => window.EMBLEM_CHECK());
await browser.close();
let bad = 0;
for (const r of out) {
  const near = Number.isFinite(r.nearest) ? r.nearest.toFixed(1) + ' units' : 'none within 4 units';
  console.log(`${r.ok ? 'ok  ' : 'FAIL'} ${r.id}: ${r.pieces} pieces (designed ${r.expected}), nearest separate parts ${near}`);
  if (!r.ok) bad++;
}
process.exit(bad ? 1 : 0);
