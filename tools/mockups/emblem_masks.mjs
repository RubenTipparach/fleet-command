// Render a faction's emblem to a mask image for a hull painter (the ship-detailing skill). The emblem's one copy is
// docs/mockups/lib/emblems.js (CLAUDE.md 6.1: the console crest, the small marks and the hull marking all draw from it);
// this runs it in a headless browser and draws the mark with its field in red and its device in green on a transparent
// ground, so tools/blender/painted_bake.py can paint each part in the faction's own hull colours, which are written
// beside it.
//
//   NODE_PATH=$(npm root -g) node tools/mockups/emblem_masks.mjs ssarith [more ids]
//
// Writes assets/emblems/<id>/hull_mask.png (512 x 512) and assets/emblems/<id>/hull_mask.json ({ hull colours, size }).
//
// A faction whose hull logo is its own SVG (LOGO_SVG below: the Kelvane Institute's KID, assets/ui/kelvane/emblem.svg,
// written by tools/mockups/kelvane_emblem.py; owner, 2026-11-06: "the 2d version the green wireframe and oval background
// can be used for ship hull logo") is drawn from that file instead: its red satin oval is the field, its green plot the
// device, its champagne rim left to the plate, and its hull colours are the logo's own.
import { createRequire } from 'node:module';
import path from 'node:path';
import fs from 'node:fs';
import { fileURLToPath } from 'node:url';

const { chromium } = createRequire(import.meta.url)('playwright');
const ROOT = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..', '..');
const LIB = fs.readFileSync(path.join(ROOT, 'docs', 'mockups', 'lib', 'emblems.js'), 'utf8');
const SIZE = 512;
const ids = process.argv.slice(2);
// field: the logo's satin red; device: its green plot (the darker fill, so the lighter wireframe reads over it in the
// painted map); plate: the champagne of its rim, which is the Institute's hull
const LOGO_SVG = { kelvane: { svg: path.join(ROOT, 'assets', 'ui', 'kelvane', 'emblem.svg'), name: 'Kelvane Institute',
  hull: { plate: '#d9d0bb', field: '#6c0c15', device: '#24995a', lit: false } } };
if (!ids.length) { console.error('usage: emblem_masks.mjs <faction id> ...'); process.exit(2); }
const browser = await chromium.launch();
const page = await browser.newPage();
page.on('pageerror', (e) => { console.error('pageerror:', e.message); process.exitCode = 1; });
await page.setContent('<!doctype html><html><body></body></html>');
await page.addScriptTag({ content: LIB });
for (const id of ids) {
  const L = LOGO_SVG[id];
  const out = L ? await page.evaluate(async ([svg, size, hull, name]) => {
    const img = new Image(); img.src = 'data:image/svg+xml;base64,' + btoa(unescape(encodeURIComponent(svg))); await img.decode();
    const c = document.createElement('canvas'); c.width = c.height = size; const x = c.getContext('2d');
    const k = Math.min(size / img.width, size / img.height); x.drawImage(img, (size - img.width * k) / 2, (size - img.height * k) / 2, img.width * k, img.height * k);
    const d = x.getImageData(0, 0, size, size), a = d.data;
    for (let i = 0; i < a.length; i += 4) { const r = a[i], g = a[i + 1], b = a[i + 2], op = a[i + 3] > 128;
      const dev = op && g > r + 20 && g >= b, fld = op && !dev && r > g + 25;
      a[i] = fld ? 255 : 0; a[i + 1] = dev ? 255 : 0; a[i + 2] = 0; a[i + 3] = dev || fld ? 255 : 0; }
    x.putImageData(d, 0, 0); return { png: c.toDataURL('image/png'), hull, name };
  }, [fs.readFileSync(L.svg, 'utf8'), SIZE, L.hull, L.name]) : await page.evaluate(([k, size]) => {
    const E = window.FCEmblems, d = E.BY_ID[k];
    if (!d) return null;
    const c = document.createElement('canvas'); c.width = c.height = size;
    E.draw(c.getContext('2d'), k, size, { field: '#ff0000', device: '#00ff00' });
    return { png: c.toDataURL('image/png'), hull: d.hull, name: d.name };
  }, [id, SIZE]);
  if (!out) { console.error(`no emblem ${id}`); process.exitCode = 1; continue; }
  const dir = path.join(ROOT, 'assets', 'emblems', id);
  fs.mkdirSync(dir, { recursive: true });
  fs.writeFileSync(path.join(dir, 'hull_mask.png'), Buffer.from(out.png.split(',')[1], 'base64'));
  fs.writeFileSync(path.join(dir, 'hull_mask.json'), JSON.stringify({
    _doc: `${out.name}'s emblem as a mask for hull painting: red the field, green the device, transparent the ground. Written by tools/mockups/emblem_masks.mjs from ${L ? path.relative(ROOT, L.svg) : 'docs/mockups/lib/emblems.js'}; never edited by hand.`,
    size_px: SIZE, hull: out.hull }, null, 2) + '\n');
  console.log(`wrote assets/emblems/${id}/hull_mask.png`);
}
await browser.close();
