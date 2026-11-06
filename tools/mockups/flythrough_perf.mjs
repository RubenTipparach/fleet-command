import { createRequire } from 'node:module';
// Usage: NODE_PATH=$(npm root -g) node tools/mockups/flythrough_perf.mjs $PWD/docs/mockups/space-flythrough.html (a measurement
// instrument for mockup 22: frames a second at each quality, then each quality's milliseconds a frame uncapped, MOCKUP_BENCH).
const { chromium } = createRequire(import.meta.url)('playwright'); // global install: run with NODE_PATH=$(npm root -g)
const file = process.argv[2];
const browser = await chromium.launch({ args: ['--use-angle=metal', '--enable-gpu', '--ignore-gpu-blocklist'] });
const page = await browser.newPage({ viewport: { width: 1920, height: 1080 } });
await page.goto('file://' + file);
await page.waitForFunction(() => window.MOCKUP_READY, null, { timeout: 120000 });
console.log('gpu', await page.evaluate(() => { const g = document.createElement('canvas').getContext('webgl2'); const e = g.getExtension('WEBGL_debug_renderer_info'); return e ? g.getParameter(e.UNMASKED_RENDERER_WEBGL) : '?'; }));
for (const q of ['Low', 'High', 'Best']) {
  await page.evaluate((q) => [...document.querySelectorAll('.keys button')].find((b) => b.textContent === q).click(), q);
  const fps = await page.evaluate(() => new Promise((res) => { let n = 0; const t0 = performance.now(); const f = () => { n++; if (performance.now() - t0 < 4000) requestAnimationFrame(f); else res(n / ((performance.now() - t0) / 1000)); }; requestAnimationFrame(f); }));
  console.log(q, fps.toFixed(1), 'fps');
}
console.log('bake', (await page.evaluate(() => window.MOCKUP_BAKE_MS)).toFixed(0), 'ms');
for (const q of ['Low', 'High', 'Best']) {
  await page.evaluate((q) => [...document.querySelectorAll('.keys button')].find((b) => b.textContent === q).click(), q);
  console.log(q, (await page.evaluate(() => window.MOCKUP_BENCH(60))).toFixed(2), 'ms a frame uncapped');
}
await browser.close();
