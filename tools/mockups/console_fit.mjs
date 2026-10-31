// Measure, on the real console-skins page, that every live child sits on plain art inside its kit parent: the analog
// panel bible's children-inside-parents rule (owner, 2026-10-31: "remember the overlap rule? can you fix this too...
// and every other ui systems too"). Measured, never eyeballed (blender-ui-panels skill, section 4).
//
// For every skin and every leaf (a title, label, number, bar track, bar fill, button, tab, divider, icon, the name
// plate's text, a button's or tab's own label) it shoots the leaf's kit parent twice at 1:1: the parent's art with all
// of the parent's leaves hidden, and the same with only this leaf shown. tools/mockups/console_fit.py then takes the
// leaf's own pixels from the difference, grows them by PAD and looks for frame edges (bevels, rails, rims, ledges) in
// the art under them. It also checks the console's top-level blocks (strips, column pieces) do not overlap.
//
//   NODE_PATH=$(npm root -g) node tools/mockups/console_fit.mjs docs/mockups/console-skins.html [out dir] [skin,ids]
// Exits 1 if any leaf touches an edge; writes <out>/<skin>-fit.png with each failing leaf boxed in red.
import { createRequire } from 'node:module';
import { spawnSync } from 'node:child_process';
import path from 'node:path';
import fs from 'node:fs';
const { chromium } = createRequire(import.meta.url)('playwright');
const [page_, outDir = '/tmp/console-fit', only] = process.argv.slice(2);
fs.rmSync(outDir, { recursive: true, force: true }); fs.mkdirSync(outDir, { recursive: true });
const browser = await chromium.launch({ args: ['--use-gl=angle', '--use-angle=swiftshader', '--enable-unsafe-swiftshader'] });
const page = await browser.newPage({ viewport: { width: 2000, height: 1200 }, deviceScaleFactor: 1 });
await page.goto('file://' + path.resolve(page_), { timeout: 120000 });
await page.waitForFunction(() => window.MOCKUP_READY, null, { timeout: 240000 });
await page.addStyleTag({ content: '.fit-hide { visibility: hidden !important; } .fit-notext { color: transparent !important; text-shadow: none !important; }' });
const ids = await page.evaluate(() => [...document.querySelectorAll('#switch button')].map((b) => b.dataset.id));
const index = [];
for (const id of ids) {
  if (only && !only.split(',').includes(id)) continue;
  // Lay the console out at 1:1 (1920 px wide), then tag every parent and leaf.
  const info = await page.evaluate((id) => {
    document.querySelector(`#switch button[data-id="${id}"]`).click();
    const st = document.getElementById('stage'); st.style.width = '1920px'; dispatchEvent(new Event('resize'));
    window.scrollTo(0, 0);
    const root = document.getElementById('console');
    const PARENTS = '.k-strip,.k-panel,.k-inset,.hero,.k-track';
    const LEAVES = '.ptitle,.lbl,.num,.k-track,.k-fill,.k-button,.k-tab,.k-divider,.ovl,.k-inset img,.k-strip .hero';
    const parents = [], leaves = [];
    const pid = (n) => { if (!n.dataset.fitP) { n.dataset.fitP = String(parents.length); const r = n.getBoundingClientRect(); parents.push({ cls: n.className, rect: [r.left + scrollX, r.top + scrollY, r.width, r.height] }); } return +n.dataset.fitP; };
    const name = (n) => (n.className || n.tagName).toString().replace(/\bnine\b|\bfit-\S+/g, '').trim() + (n.textContent ? ' "' + n.textContent.trim().slice(0, 18) + '"' : (n.alt ? ' ' + n.alt : ''));
    for (const n of root.querySelectorAll(LEAVES)) {
      const p = n.parentElement.closest(PARENTS); if (!p || !root.contains(p)) continue;
      n.dataset.fitL = String(leaves.length); leaves.push({ parent: pid(p), name: name(n), text: false, glyphs: n.matches('.ptitle,.lbl,.num,.ovl') });
    }
    // A button's or tab's own label is a leaf of the button (its art's open face).
    for (const n of root.querySelectorAll('.k-button,.k-tab')) { n.dataset.fitT = String(leaves.length); leaves.push({ parent: pid(n), name: name(n) + ' label', text: true }); }
    // Top-level blocks that must not overlap: the strips and every piece in the left column.
    const blocks = [...root.querySelectorAll(':scope > .k-strip, .col .hero, .col .k-panel, .col .k-inset')].map((n) => { const r = n.getBoundingClientRect(); return { name: name(n).slice(0, 30), rect: [r.left, r.top, r.width, r.height] }; });
    // Content that does not fit its fixed box: a key, label or panel clipped or overflowing (CLAUDE.md section 7).
    const clipped = [...root.querySelectorAll('.k-button,.k-tab,.lbl,.num,.ptitle,.ovl,.k-panel,.k-inset,.k-strip')].filter((n) => n.scrollWidth > n.clientWidth + 1).map((n) => name(n).slice(0, 30) + ` (${n.scrollWidth} > ${n.clientWidth})`);
    // Every column block stays inside the column's 460 px.
    const col = root.querySelector('.col').getBoundingClientRect(), right = col.left + 6 + 460;
    for (const n of root.querySelectorAll('.col .hero, .col .k-panel, .col .k-inset')) { const r = n.getBoundingClientRect(); if (r.right > right + 0.5) clipped.push(name(n).slice(0, 30) + ` (${(r.right - right).toFixed(0)} px past the column)`); }
    return { parents, leaves, blocks, clipped };
  }, id);
  await page.waitForTimeout(600);
  const leafSel = (i) => `[data-fit-l="${i}"]`;
  const setState = (pIdx, showLeaf) => page.evaluate(([pIdx, showLeaf]) => {
    for (const n of document.querySelectorAll('.fit-hide')) n.classList.remove('fit-hide');
    for (const n of document.querySelectorAll('.fit-notext')) n.classList.remove('fit-notext');
    const p = document.querySelector(`[data-fit-p="${pIdx}"]`);
    // Hide every leaf whose parent is p (a hidden track hides its fill with it), and p's own label if it is a button.
    for (const n of p.querySelectorAll('[data-fit-l]')) { const q = n.parentElement.closest('.k-strip,.k-panel,.k-inset,.hero,.k-track'); if (q === p) n.classList.add('fit-hide'); }
    if (p.dataset.fitT) p.classList.add('fit-notext');
    if (showLeaf == null) return;
    const [kind, i] = showLeaf;
    if (kind === 'l') { const n = document.querySelector(`[data-fit-l="${i}"]`); n.classList.remove('fit-hide'); for (const c of n.querySelectorAll('[data-fit-l]')) c.classList.remove('fit-hide'); for (const c of n.querySelectorAll('.k-button,.k-tab')) c.classList.remove('fit-notext'); }
    else document.querySelector(`[data-fit-t="${i}"]`).classList.remove('fit-notext');
  }, [pIdx, showLeaf]);
  const shot = async (rect, file) => { const [x, y, w, h] = rect; await page.screenshot({ path: file, clip: { x: Math.max(0, x - 1), y: Math.max(0, y - 1), width: Math.ceil(w + 2), height: Math.ceil(h + 2) }, animations: 'disabled', fullPage: true }); };
  const recs = [];
  for (let p = 0; p < info.parents.length; p++) {
    const kids = info.leaves.map((l, i) => [l, i]).filter(([l]) => l.parent === p);
    if (!kids.length) continue;
    const base = path.join(outDir, `${id}-p${p}-base.png`);
    await setState(p, null); await shot(info.parents[p].rect, base);
    for (const [l, i] of kids) {
      const f = path.join(outDir, `${id}-p${p}-l${i}.png`);
      await setState(p, [l.text ? 't' : 'l', i]); await shot(info.parents[p].rect, f);
      let own = base;
      if (l.glyphs) { own = path.join(outDir, `${id}-p${p}-l${i}-box.png`); await setState(p, ['l', i]); await page.evaluate((i) => document.querySelector(`[data-fit-l="${i}"]`).classList.add('fit-notext'), i); await shot(info.parents[p].rect, own); }
      recs.push({ leaf: i, name: l.name, parent: p, parentCls: info.parents[p].cls, rect: info.parents[p].rect, base: own, shot: f });
    }
  }
  await setState(0, null); await page.evaluate(() => { for (const n of document.querySelectorAll('.fit-hide,.fit-notext')) n.classList.remove('fit-hide', 'fit-notext'); });
  const full = path.join(outDir, `${id}-console.png`); await page.locator('#console').screenshot({ path: full });
  index.push({ skin: id, leaves: recs, blocks: info.blocks, clipped: info.clipped, full });
}
await browser.close();
fs.writeFileSync(path.join(outDir, 'index.json'), JSON.stringify(index));
const r = spawnSync('python3', [path.join(path.dirname(new URL(import.meta.url).pathname), 'console_fit.py'), path.join(outDir, 'index.json')], { stdio: 'inherit' });
process.exit(r.status ?? 1);
