// Measures the seam between a selected weapon box and its selection ring on the battle console (owner, 2026-11-01:
// "Remeasure precisely"). From each selected box's centre, rays walk out through the edges and corners; any pixel between
// the last green and the first Sky pixel darker than both is a seam. Prints the worst dip per mount; 0 is clean.
// Unselected mounts (point defence) have no ring and read high: the ray crosses the background to the hull line.
// Run: NODE_PATH=$(npm root -g) node tools/mockups/ring_seam.mjs
import { createRequire } from 'node:module'; import path from 'node:path';
const { chromium } = createRequire(import.meta.url)('playwright');
const proxy = process.env.HTTPS_PROXY ? { server: process.env.HTTPS_PROXY, bypass: 'localhost,127.0.0.1' } : undefined;
const b = await chromium.launch({ proxy, args: ['--use-gl=angle', '--use-angle=swiftshader', '--enable-unsafe-swiftshader'] });
for (const dpr of [1, 2]) for (const shot of ['battle', 'ship_expanded']) {
  const p = await b.newPage({ viewport: { width: 1920, height: 1080 }, deviceScaleFactor: dpr });
  await p.goto('file://' + path.resolve('docs/mockups/battle-console.html')); await p.waitForFunction(() => window.MOCKUP_READY);
  await p.evaluate((s) => window.MOCKUP_SHOTS[s](), shot); await p.waitForTimeout(400);
  const r = await p.evaluate((dpr) => {
    const hs = window.MOCKUP_HITS().filter((q) => /^ssd\.m\.\d+$|^own\.m\.\d+$|\.m\.\d+$/.test(q.id) && !/^tgt/.test(q.id));
    const cv = [...document.querySelectorAll('canvas')].find((c) => c.width >= innerWidth * dpr - 1 && c.getContext('2d'));
    const g = cv.getContext('2d'); const out = [];
    const lum = (d) => 0.2126 * d[0] + 0.7152 * d[1] + 0.0722 * d[2];
    for (const h of hs) {
      const cx = (h.x + h.w / 2) * dpr, cy = (h.y + h.h / 2) * dpr, R = h.w / 2 * dpr + 6 * dpr;
      let worst = 0, sel = false, where = '';
      for (const [dx, dy, nm] of [[1, 0, 'E'], [-1, 0, 'W'], [0, 1, 'S'], [0, -1, 'N'], [0.7071, 0.7071, 'SE'], [-0.7071, -0.7071, 'NW'], [0.7071, -0.7071, 'NE'], [-0.7071, 0.7071, 'SW']]) {
        const px = []; for (let t = 0; t < R; t += 0.5) px.push(g.getImageData(Math.round(cx + dx * t), Math.round(cy + dy * t), 1, 1).data);
        // last green pixel then first sky pixel along the ray
        let lg = -1, fs = -1;
        for (let i = 0; i < px.length; i++) { const d = px[i]; if (d[1] > 150 && d[0] < 120 && d[2] < 140) lg = i; }
        for (let i = lg + 1; i < px.length; i++) { const d = px[i]; if (d[2] > 200 && d[0] < 120) { fs = i; break; } }
        if (lg < 0 || fs < 0) continue; sel = true;
        const floor = Math.min(lum(px[lg]), lum(px[fs]));
        for (let i = lg + 1; i < fs; i++) { const dip = floor - lum(px[i]); if (dip > worst) { worst = dip; where = nm; } }
      }
      if (sel) out.push(h.id + ':' + Math.round(worst) + (worst > 8 ? '(' + where + ')' : ''));
    }
    return out;
  }, dpr);
  console.log('dpr', dpr, shot, r.join(' ')); await p.close();
}
await b.close();
