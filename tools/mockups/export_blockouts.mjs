// Export a ship page's picked hulls, as OBJ, for Blender to unwrap and bake (the painted-bake and ship-detailing skills;
// OpenSpec changes ssarith-ship-concepts, kelvane-ship-concepts). The page builds every hull itself (the Ssarith Hunt's
// docs/mockups/ssarith-ship-concepts.html by default, or the page named) and hands each pick out through
// window.MOCKUP_EXPORT_OBJ(n), in ship space (+x port, +y up, +z bow), one object per piece (hull_*, glow_* or well_*, dark
// glass). Nothing here shapes a part (CLAUDE.md 6.1: the page's builders are the one copy).
//
//   NODE_PATH=$(npm root -g) node tools/mockups/export_blockouts.mjs [docs/mockups/kelvane-ship-concepts.html]
//
// Writes tools/materials/raw/painted/ships/<slug>.obj (gitignored: a build intermediate) and prints each slug and size.
import { createRequire } from 'node:module';
import path from 'node:path';
import fs from 'node:fs';
import { fileURLToPath } from 'node:url';

const { chromium } = createRequire(import.meta.url)('playwright');
const ROOT = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..', '..');
const PAGE = process.argv[2] ? path.resolve(process.argv[2]) : path.join(ROOT, 'docs', 'mockups', 'ssarith-ship-concepts.html');
const OUT = path.join(ROOT, 'tools', 'materials', 'raw', 'painted', 'ships');
fs.mkdirSync(OUT, { recursive: true });
const proxy = process.env.HTTPS_PROXY ? { server: process.env.HTTPS_PROXY, bypass: 'localhost,127.0.0.1' } : undefined;
const browser = await chromium.launch({ proxy, args: ['--use-gl=angle', '--use-angle=swiftshader', '--enable-unsafe-swiftshader'] });
const page = await browser.newPage({ viewport: { width: 800, height: 600 } });
page.on('pageerror', (e) => { console.error('pageerror:', e.message); process.exitCode = 1; });
await page.goto('file://' + PAGE, { timeout: 240000 });
await page.waitForFunction(() => window.MOCKUP_READY, null, { timeout: 240000 });
const picks = await page.evaluate(() => window.MOCKUP_PICKS());
for (const { n, slug } of picks) {
  const obj = await page.evaluate((k) => window.MOCKUP_EXPORT_OBJ(k), n);
  fs.writeFileSync(path.join(OUT, slug + '.obj'), obj);
  console.log(`exported ${n} ${slug}: ${obj.split('\n').filter((l) => l.startsWith('f ')).length} triangles`);
}
await browser.close();
