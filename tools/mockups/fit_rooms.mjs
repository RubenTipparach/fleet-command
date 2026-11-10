// A measurement instrument for a conversion's room plan (OpenSpec change vos-ship-concepts, D16): a Vos ship is a freighter's
// drive block and taken parts, so its rooms are fitted into hulls that already exist, the opposite of a warship designed
// inside out. This trims each room of data/ships/vos/<plan>_plan.json until it lies inside the ship's parts on mockup 37,
// grown by the plan's clearance and structure, using the page's own inside test (window.MOCKUP_INSIDE, the test
// window.MOCKUP_ROOMS checks with): greedily, the room's outer edge in 0.5 m steps or either end in 1 m steps, a room and its
// starboard twin kept equal. It writes the plan back and prints what moved; the plan file stays the source, and a room it
// cannot fit is reported for the plan to be redrawn by hand. Nothing here decides what a room is.
//
//   NODE_PATH=$(npm root -g) node tools/mockups/fit_rooms.mjs 8:saucer_runner 3:boarder   (board number : plan name)
import { createRequire } from 'node:module';
import path from 'node:path';
import fs from 'node:fs';
import { fileURLToPath } from 'node:url';

const { chromium } = createRequire(import.meta.url)('playwright');
const ROOT = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..', '..');
const PAGE = path.join(ROOT, 'docs', 'mockups', 'vos-ship-concepts.html');
const browser = await chromium.launch({ args: ['--use-gl=angle', '--use-angle=swiftshader', '--enable-unsafe-swiftshader'] });
const page = await browser.newPage();
await page.goto('file://' + PAGE, { timeout: 240000 });
await page.waitForFunction(() => window.MOCKUP_READY, null, { timeout: 240000 });
for (const [n, name] of process.argv.slice(2).map((s) => s.split(':'))) {
  const file = path.join(ROOT, 'data', 'ships', 'vos', `${name}_plan.json`), P = JSON.parse(fs.readFileSync(file, 'utf8'));
  const F = P.deck_floor_m, H = P.headroom_m, C = P.clearance_m + P.structure_m;
  // the same samples as the page's check: corners, edge middles and face centres of the grown box, the middles a hair off
  // the centre plane (a mirrored part's seam)
  const samples = (r) => { const y0 = Math.min(...r.decks.map((d) => F[d])), y1 = Math.max(...r.decks.map((d) => F[d])) + H, [x0, x1] = r.x_m, [z0, z1] = r.z_m, out = [];
    const xm = (x0 + x1) / 2 + 0.137, ym = (y0 + y1) / 2 + 0.071, zm = (z0 + z1) / 2 + 0.113;
    for (const x of [x0 - C, xm, x1 + C]) for (const y of [y0 - C, ym, y1 + C]) for (const z of [z0 - C, zm, z1 + C]) if (!(x === xm && y === ym && z === zm)) out.push([x, y, z]);
    return out; };
  const outside = async (r) => (await page.evaluate(([k, s]) => window.MOCKUP_INSIDE(k, s), [+n, samples(r)])).filter((v) => !v).length;
  const log = [];
  for (const r of P.rooms) {
    if (r.id.endsWith('_s') && P.rooms.some((q) => q.id === r.id.slice(0, -2) + '_p')) continue;   // a twin follows its port room
    let f = await outside(r); if (!f) continue;
    const was = JSON.stringify([r.x_m, r.z_m]);
    while (f) {
      const port = r.x_m[0] >= 0, star = r.x_m[1] <= 0, opts = [];
      if (r.x_m[1] - r.x_m[0] > 2.6 && r.kind !== 'corridor') opts.push(port ? { x_m: [r.x_m[0], r.x_m[1] - 0.5] } : star ? { x_m: [r.x_m[0] + 0.5, r.x_m[1]] } : { x_m: [r.x_m[0] + 0.5, r.x_m[1] - 0.5] });
      if (r.z_m[1] - r.z_m[0] > 3) opts.push({ z_m: [r.z_m[0] + 1, r.z_m[1]] }, { z_m: [r.z_m[0], r.z_m[1] - 1] });
      if (!opts.length) break;
      let best = null;
      for (const o of opts) { const g = await outside({ ...r, ...o }); if (!best || g < best.g) best = { o, g }; }
      Object.assign(r, best.o); f = best.g;
    }
    const twin = P.rooms.find((q) => q.id === r.id.slice(0, -2) + '_s');
    if (r.id.endsWith('_p') && twin) { twin.x_m = [-r.x_m[1], -r.x_m[0]]; twin.z_m = [...r.z_m]; }
    log.push(`${r.id}: ${was} -> ${JSON.stringify([r.x_m, r.z_m])}${f ? `, still ${f} points outside: redraw it` : ''}`);
  }
  for (const r of P.rooms) { r.x_m = r.x_m.map((v) => +v.toFixed(2)); r.z_m = r.z_m.map((v) => +v.toFixed(2)); }
  fs.writeFileSync(file, JSON.stringify(P, null, 1));
  console.log(`${n} ${name}: ${log.length ? '\n  ' + log.join('\n  ') : 'every room fits'}`);
}
await browser.close();
