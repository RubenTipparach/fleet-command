// Screenshot a mockup page headless, one PNG per named shot, for looking at before it is shown
// (threejs-mockups skill). The page sets window.MOCKUP_SHOTS = { name: () => setView } and
// window.MOCKUP_READY = true (docs/mockups/lib/template.html). Fails on a console error or a page that
// never becomes ready. Shoots at desktop size and at a phone's landscape and portrait sizes (CLAUDE.md section 7).
// After Crab Breeze tools/mockups/shoot.mjs (69ce196).
//
//   NODE_PATH=$(npm root -g) node tools/mockups/shoot.mjs docs/mockups/<page>.html docs/screenshots/mockups [only,names]
import { createRequire } from 'node:module';
import path from 'node:path';
import fs from 'node:fs';

const { chromium } = createRequire(import.meta.url)('playwright'); // global install: run with NODE_PATH=$(npm root -g)

const [target, outDir, only] = process.argv.slice(2);
if (!target || !outDir) {
  console.error('usage: node tools/mockups/shoot.mjs <page.html|url> <out dir> [only,names]');
  process.exit(2);
}
const SIZES = [
  ['desktop', { width: 1920, height: 1080 }],
  ['phone', { width: 844, height: 390 }],
  ['portrait', { width: 390, height: 844 }],
];
const pageName = path.basename(target).replace(/\.html$/, '') || 'page';
fs.mkdirSync(outDir, { recursive: true });

const proxy = process.env.HTTPS_PROXY ? { server: process.env.HTTPS_PROXY, bypass: 'localhost,127.0.0.1' } : undefined;
const browser = await chromium.launch({ proxy, args: ['--use-gl=angle', '--use-angle=swiftshader', '--enable-unsafe-swiftshader'] });
let errors = 0;
for (const [sizeName, viewport] of SIZES) {
  const page = await browser.newPage({ viewport });
  page.on('console', (m) => { if (m.type() === 'error') { errors++; console.log('console:', m.text()); } });
  page.on('pageerror', (e) => { errors++; console.log('pageerror:', e.message); });
  await page.goto(/^https?:/.test(target) ? target : 'file://' + path.resolve(target), { timeout: 240000 });
  await page.waitForFunction(() => window.MOCKUP_READY, null, { timeout: 240000 });
  const names = await page.evaluate(() => Object.keys(window.MOCKUP_SHOTS || {}));
  for (const name of names.length ? names : ['default']) {
    if (only && !only.split(',').includes(name)) continue;
    await page.evaluate((n) => window.MOCKUP_SHOTS?.[n]?.(), name);
    await page.waitForTimeout(1500);
    const file = path.join(outDir, `${pageName}-${name}-${sizeName}.png`);
    await page.screenshot({ path: file, timeout: 180000 });
    console.log('shot', file);
  }
  await page.close();
}
await browser.close();
if (errors) {
  console.error(`${errors} console error(s)`);
  process.exit(1);
}
