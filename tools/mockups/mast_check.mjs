// Checks the Ship Design Bible's T13 on a baked ship page: on every ship no two antennas share a length (owner, 2026-11-11:
// "Antenna need to also be different lengths. No two antenna shall have same length"). It measures each antenna as built by
// docs/mockups/lib/baked-ship.js masts() (footing to tip, head and spike included) and fails when two on one ship are less
// than 12% apart. A page that loads its maps as files must be served over HTTP (WebGL refuses a file:// page's images):
//
//   (cd docs/mockups && python3 -m http.server 8765 --bind 127.0.0.1) &
//   NODE_PATH=$(npm root -g) node tools/mockups/mast_check.mjs http://127.0.0.1:8765/vos-ship-concepts.html
import { createRequire } from 'node:module';
const { chromium } = createRequire(import.meta.url)('playwright');
const browser = await chromium.launch({ args: ['--use-gl=angle', '--use-angle=swiftshader', '--enable-unsafe-swiftshader'] });
const page = await browser.newPage();
await page.goto(process.argv[2], { timeout: 240000 });
await page.waitForFunction(() => window.MOCKUP_READY, null, { timeout: 240000 });
const ships = await page.evaluate(() => { const out = [];
  window.MOCKUP_SCENE.traverse((o) => { if (!o.isGroup || !o.children.length || !o.children.every((c) => c.userData.mast)) return;
    out.push(o.children.filter((f) => f.userData.mast === 'antenna').map((f) => { f.updateMatrixWorld(true); const inv = new THREE.Matrix4().copy(f.matrixWorld).invert(), bx = new THREE.Box3();
      f.traverse((m) => { if (m.isMesh) { m.geometry.computeBoundingBox(); bx.union(m.geometry.boundingBox.clone().applyMatrix4(new THREE.Matrix4().multiplyMatrices(inv, m.matrixWorld))); } });
      return bx.max.z - bx.min.z; })); });
  return out; });
let bad = 0;
ships.forEach((L, i) => { const s = [...L].sort((a, b) => a - b), gaps = s.slice(1).map((v, k) => v / s[k] - 1), min = gaps.length ? Math.min(...gaps) : 1;
  console.log(`ship ${i}: ${s.map((v) => v.toFixed(2)).join(' ')} m, closest ${(min * 100).toFixed(1)}%`); if (min < 0.12) bad++; });
console.log(bad ? `mast check: FAIL (${bad} ships with two antennas under 12% apart)` : 'mast check: PASS');
await browser.close();
process.exitCode = bad ? 1 : 0;
