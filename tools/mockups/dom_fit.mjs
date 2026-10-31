// The children-inside-parents rule on any mockup page's DOM (owner, 2026-10-31: "remember the overlap rule? can you
// fix this too...and every other ui systems too"), for flat CSS panels where geometry is the truth. The kit-skinned
// console, whose frames are images, is checked on its pixels by tools/mockups/console_fit.mjs instead.
//
// For every shot the page offers (window.MOCKUP_SHOTS), at desktop, phone landscape and portrait sizes, it flags:
//   spill    a child that leaves its framed parent's inner box (inside the border) by more than 0.5 px; a framed
//            parent is one with a border or a background, and clipping parents (overflow other than visible) are
//            scroll areas, so their children are exempt
//   overlap  two in-flow siblings whose boxes intersect by more than 0.5 px each way
//   cut      text clipped without an ellipsis (CLAUDE.md section 7 allows trimming with an ellipsis only)
// Absolutely positioned, fixed and sticky elements are overlays: they are skipped as children and as in-flow siblings,
// and checked against each other instead (an image or canvas under them is art they are drawn on, by design).
//
//   NODE_PATH=$(npm root -g) node tools/mockups/dom_fit.mjs docs/mockups/<page>.html [shot,names]
import { createRequire } from 'node:module';
import path from 'node:path';
const { chromium } = createRequire(import.meta.url)('playwright');
const [target, only] = process.argv.slice(2);
const SIZES = [['desktop', { width: 1920, height: 1080 }], ['phone', { width: 844, height: 390 }], ['portrait', { width: 390, height: 844 }]];
const browser = await chromium.launch({ args: ['--use-gl=angle', '--use-angle=swiftshader', '--enable-unsafe-swiftshader'] });
let total = 0;
for (const [sizeName, viewport] of SIZES) {
  const page = await browser.newPage({ viewport });
  await page.goto('file://' + path.resolve(target), { timeout: 240000 });
  // A page without MOCKUP_READY (a guide, the survey, the tracker) is checked once, as it loads.
  const ready = await page.waitForFunction(() => window.MOCKUP_READY, null, { timeout: 15000 }).then(() => true, () => false);
  if (!ready) await page.waitForTimeout(3000);
  const names = ready ? await page.evaluate(() => Object.keys(window.MOCKUP_SHOTS || {})) : ['page'];
  for (const name of names) {
    if (only && !only.split(',').includes(name)) continue;
    if (ready) await page.evaluate((n) => window.MOCKUP_SHOTS[n](), name);
    await page.waitForTimeout(700);
    const faults = await page.evaluate(() => {
      const out = [];
      const vis = (n) => { const s = getComputedStyle(n); if (s.display === 'none' || s.visibility === 'hidden' || +s.opacity === 0) return false; const r = n.getBoundingClientRect(); return r.width > 0 && r.height > 0; };
      // A closed <details> draws only its summary.
      const shown = (n) => { for (let p = n; p && p !== document.body; p = p.parentElement) { if (!vis(p)) return false; const d = p.parentElement; if (d && d.tagName === 'DETAILS' && !d.open && p.tagName !== 'SUMMARY') return false; } return true; };
      const label = (n) => (n.id ? '#' + n.id : n.tagName.toLowerCase() + (n.className && typeof n.className === 'string' ? '.' + n.className.trim().split(/\s+/).slice(0, 2).join('.') : '')) + (n.children.length === 0 && n.textContent.trim() ? ` "${n.textContent.trim().slice(0, 24)}"` : '');
      const overlay = (s) => s.position === 'absolute' || s.position === 'fixed' || s.position === 'sticky';
      const framed = (s) => parseFloat(s.borderTopWidth) + parseFloat(s.borderLeftWidth) > 0 || (s.backgroundColor !== 'rgba(0, 0, 0, 0)' && s.backgroundColor !== 'transparent') || s.backgroundImage !== 'none';
      for (const p of document.body.querySelectorAll('*')) {
        if (!shown(p) || ['CANVAS', 'svg', 'SCRIPT', 'STYLE', 'IMG', 'BUTTON', 'INPUT', 'SELECT', 'OPTION', 'path', 'g'].includes(p.tagName)) continue;
        const ps = getComputedStyle(p);
        const kids = [...p.children].filter((c) => shown(c) && !overlay(getComputedStyle(c)) && !['SCRIPT', 'STYLE', 'BR'].includes(c.tagName));
        if (framed(ps) && ps.overflow === 'visible' && p !== document.documentElement && p !== document.body) {
          const r = p.getBoundingClientRect();
          const inner = { l: r.left + parseFloat(ps.borderLeftWidth), t: r.top + parseFloat(ps.borderTopWidth), r: r.right - parseFloat(ps.borderRightWidth), b: r.bottom - parseFloat(ps.borderBottomWidth) };
          for (const c of kids) {
            const q = c.getBoundingClientRect(), d = Math.max(inner.l - q.left, inner.t - q.top, q.right - inner.r, q.bottom - inner.b);
            if (d > 0.5) out.push(`spill ${label(c)} out of ${label(p)} by ${d.toFixed(1)} px`);
          }
        }
        for (let i = 0; i < kids.length; i++) for (let j = i + 1; j < kids.length; j++) {
          const a = kids[i].getBoundingClientRect(), b = kids[j].getBoundingClientRect();
          const ix = Math.min(a.right, b.right) - Math.max(a.left, b.left), iy = Math.min(a.bottom, b.bottom) - Math.max(a.top, b.top);
          if (ix > 0.5 && iy > 0.5) out.push(`overlap ${label(kids[i])} / ${label(kids[j])} (${ix.toFixed(1)} x ${iy.toFixed(1)} px)`);
        }
        // Overlays (absolute or fixed) of one parent must not cover each other either, unless one is a tooltip,
        // a menu or a dialog drawn over the rest on purpose (data-fit-over, or a z-index above its siblings').
        const ovs = [...p.children].filter((c) => shown(c) && overlay(getComputedStyle(c)) && !c.dataset.fitOver && !['CANVAS', 'IMG', 'SCRIPT', 'STYLE'].includes(c.tagName));
        for (let i = 0; i < ovs.length; i++) for (let j = i + 1; j < ovs.length; j++) {
          const za = getComputedStyle(ovs[i]).zIndex, zb = getComputedStyle(ovs[j]).zIndex;
          if (za !== zb) continue;
          const a = ovs[i].getBoundingClientRect(), b = ovs[j].getBoundingClientRect();
          const ix = Math.min(a.right, b.right) - Math.max(a.left, b.left), iy = Math.min(a.bottom, b.bottom) - Math.max(a.top, b.top);
          if (ix > 0.5 && iy > 0.5 && !(a.width >= innerWidth - 1 && a.height >= innerHeight - 1) && !(b.width >= innerWidth - 1 && b.height >= innerHeight - 1)) out.push(`overlay overlap ${label(ovs[i])} / ${label(ovs[j])} (${ix.toFixed(1)} x ${iy.toFixed(1)} px)`);
        }
        if (ps.overflow !== 'visible' && ps.textOverflow !== 'ellipsis' && p.children.length === 0 && p.textContent.trim() && p.scrollWidth > p.clientWidth + 1) out.push(`cut ${label(p)} (${p.scrollWidth} > ${p.clientWidth})`);
      }
      return [...new Set(out)];
    });
    total += faults.length;
    console.log(`${name.padEnd(18)} ${sizeName.padEnd(8)} ${faults.length ? faults.length + ' fault(s)' : 'clear'}`);
    for (const f of faults.slice(0, 12)) console.log('    ' + f);
    if (faults.length > 12) console.log(`    ... ${faults.length - 12} more`);
  }
  await page.close();
}
await browser.close();
console.log('dom fit:', total ? `FAIL (${total})` : 'PASS');
process.exit(total ? 1 : 0);
