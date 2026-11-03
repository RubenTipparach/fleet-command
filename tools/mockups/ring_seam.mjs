// Measures the seam between a selected weapon box and its selection ring on the battle console (owner, 2026-11-01:
// "Still gaps here on the weapon box", "Remeasure precisely"). For each selected mount (from the simulation):
// - the box's fill is the most common colour inside the box (so the glyph or the cooldown figures do not count),
//   which works for bright (ready) and dark (cooling) fills alike;
// - eight rays (sides and corners) walk out from the centre; on each, the stretch from the last fill-coloured pixel to
//   the first ring (Sky) pixel is the edge;
// - a pixel in that stretch darker than both the fill and the ring is a seam. The worst dip per mount is printed; 0 is
//   clean. The check runs at 1x and 2x (the page caps its pixel ratio at 2).
// Run: NODE_PATH=$(npm root -g) node tools/mockups/ring_seam.mjs [page]   (default docs/mockups/battle-console.html)
import { createRequire } from 'node:module'; import path from 'node:path';
const { chromium } = createRequire(import.meta.url)('playwright');
const page = process.argv[2] || 'docs/mockups/battle-console.html';
const proxy = process.env.HTTPS_PROXY ? { server: process.env.HTTPS_PROXY, bypass: 'localhost,127.0.0.1' } : undefined;
const b = await chromium.launch({ proxy, args: ['--use-gl=angle', '--use-angle=swiftshader', '--enable-unsafe-swiftshader'] });
let fail = 0;
for (const dpr of [1, 2]) for (const shot of ['battle', 'ship_expanded']) {
  const p = await b.newPage({ viewport: { width: 1920, height: 1080 }, deviceScaleFactor: dpr });
  await p.goto('file://' + path.resolve(page)); await p.waitForFunction(() => window.MOCKUP_READY);
  await p.evaluate((s) => window.MOCKUP_SHOTS[s](), shot); await p.waitForTimeout(400);
  const r = await p.evaluate((dpr) => {
    const cv = [...document.querySelectorAll('canvas')].find((c) => c.width >= innerWidth * dpr - 1 && c.getContext('2d'));
    const g = cv.getContext('2d'), lum = (d) => 0.2126 * d[0] + 0.7152 * d[1] + 0.0722 * d[2];
    const at = (x, y) => g.getImageData(Math.round(x), Math.round(y), 1, 1).data;
    const near = (a, c) => Math.abs(a[0] - c[0]) + Math.abs(a[1] - c[1]) + Math.abs(a[2] - c[2]) < 24;
    // the ring: clearly blue, including the pixels where it blends with the fill at a 1x corner
    const isSky = (d) => d[2] > 150 && d[2] > d[0] + 60;
    const out = [];
    for (const h of window.MOCKUP_HITS().filter((q) => /^ssd\.m\.\d+$/.test(q.id))) {
      if (!window.MOCKUP_SIM.W.player.mounts[+h.id.split('.').pop()].sel) continue;
      // the hit rectangle is the box plus 5 px each side
      const cx = (h.x + h.w / 2) * dpr, cy = (h.y + h.h / 2) * dpr, half = (h.w / 2 - 5) * dpr;
      const counts = new Map();
      for (let y = -half + 2; y <= half - 2; y++) for (let x = -half + 2; x <= half - 2; x++) { const d = at(cx + x, cy + y), k = d[0] + ',' + d[1] + ',' + d[2]; counts.set(k, (counts.get(k) || 0) + 1); }
      const fill = [...counts.entries()].sort((a, c) => c[1] - a[1])[0][0].split(',').map(Number);
      let worst = 0, where = '';
      for (const [dx, dy, nm] of [[1, 0, 'E'], [-1, 0, 'W'], [0, 1, 'S'], [0, -1, 'N'], [0.7071, 0.7071, 'SE'], [-0.7071, -0.7071, 'NW'], [0.7071, -0.7071, 'NE'], [-0.7071, 0.7071, 'SW']]) {
        const px = []; for (let t = 0; t < half * 1.6 + 6 * dpr; t += 0.5) px.push(at(cx + dx * t, cy + dy * t));
        const fs = px.findIndex(isSky); if (fs < 0) continue;
        let lf = -1; for (let i = 0; i < fs; i++) if (near(px[i], fill)) lf = i;
        if (lf < 0) continue;
        const floor = Math.min(lum(fill), lum(px[fs]));
        for (let i = lf + 1; i < fs; i++) { const dip = floor - lum(px[i]); if (dip > worst) { worst = dip; where = nm; } }
      }
      out.push([h.id, Math.round(worst), where]);
    }
    return out;
  }, dpr);
  const bad = r.filter(([, w]) => w > 3); fail += bad.length;
  console.log(`dpr ${dpr} ${shot}: ${r.length} selected mounts, worst dip ${Math.max(0, ...r.map((q) => q[1]))}` + (bad.length ? '  SEAM ' + bad.map((q) => q.join(':')).join(' ') : ''));
  await p.close();
}
await b.close();
process.exit(fail ? 1 : 0);
