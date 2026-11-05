// Write a Concord patch's parts (the picked one by default, or the key given) from the Emblem Design Guide
// (docs/design/emblem-guide.html, window.EMBLEM_PARTS) to assets/emblems/concord/<key>.parts.json: every outline as
// polygons in grid units (1 unit = 1 mm on the 3D badge, y down), so the Blender render (tools/blender/emblem_concord.py)
// and the three.js badge (docs/mockups/concord-emblem-3d.html) build from the very shapes the sketch draws.
//
//   NODE_PATH=$(npm root -g) node tools/emblems/export_parts.mjs [B-E23 | krai | ssarith]
import { createRequire } from 'node:module';
import fs from 'node:fs';
import path from 'node:path';

const { chromium } = createRequire(import.meta.url)('playwright');
const key = process.argv[2] || null;
const browser = await chromium.launch();
const page = await browser.newPage();
await page.goto('file://' + path.resolve('docs/design/emblem-guide.html'));
await page.waitForFunction(() => window.MOCKUP_READY, null, { timeout: 60000 });
if (key === 'krai') {
  // The Krai's picked emblem, KC+ (the fang torc): ring, field, thin crescent, torc body, fangs and the forge star.
  const parts = await page.evaluate(() => window.EMBLEM_PARTS_KRAI());
  await browser.close();
  const out = path.join('assets', 'emblems', 'krai', 'kc.parts.json');
  fs.mkdirSync(path.dirname(out), { recursive: true });
  fs.writeFileSync(out, JSON.stringify(parts) + '\n');
  console.log(`wrote ${out}: ring 2 rings, thin ${parts.thin.length}, body ${parts.body.length}, fangs ${parts.fangs.length}, star ${parts.star.length}`);
  process.exit(0);
}
if (key === 'ssarith') {
  // The Ssarith's Bow Rites, SD: the pennon frame and field, and each bow's limb, string, shaft, vanes and head.
  const parts = await page.evaluate(() => window.EMBLEM_PARTS_SSARITH());
  await browser.close();
  const out = path.join('assets', 'emblems', 'ssarith', 'sd.parts.json');
  fs.mkdirSync(path.dirname(out), { recursive: true });
  fs.writeFileSync(out, JSON.stringify(parts) + '\n');
  console.log(`wrote ${out}: frame 2 rings, ${parts.bows.length} bows`);
  process.exit(0);
}
const parts = await page.evaluate((k) => window.EMBLEM_PARTS(k), key);
await browser.close();
const out = path.join('assets', 'emblems', 'concord', `${parts.key.toLowerCase()}.parts.json`);
fs.mkdirSync(path.dirname(out), { recursive: true });
fs.writeFileSync(out, JSON.stringify(parts) + '\n');
const count = (a) => a.reduce((n, p) => n + p.length, 0);
console.log(`wrote ${out}: frame ${parts.frame.length} rings (${count(parts.frame)} points), field ${count(parts.field)}, legs ${parts.legs.length}, lens ${parts.lens.length}, star ${parts.star.length}`);
