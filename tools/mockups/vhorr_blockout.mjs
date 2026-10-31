// Fleet Command: measure the Vhorr Brood console's slots for the organic rebuild, and write the blockout.
//
// The Organic Panel Bible's rule 4 (owner, 2026-10-31: "due to the nature of this ui, nine slice is not possible and
// the UI will have to be bocked out using placeholders then a full detail agent will go in and sculpt components
// manually"): an organic skin has no nine-slice pieces, so every piece is sculpted whole at its exact slot. This script
// is phase 1, the blockout: it opens docs/mockups/console-skins.html on the Vhorr skin at the desktop reference (1920 x
// 1080), and measures from the live DOM every element the console draws with a kit piece (frames, insets, strips,
// dividers, bar tracks, keys, tabs and the hero pieces), each slot's rect in the console's own unscaled CSS px, and
// the rectangles inside it that growth must never cover (labels, readouts, live drawings, icons, bar cells, key and
// tab labels, child slots). It writes assets/ui/vhorr/organic/blockout.json and the game view the console floats over
// (rule 5: an overlay, not a scene), and tools/mockups/vhorr_blockout_draw.py draws the blockout picture from them.
//
//   NODE_PATH=$(npm root -g) node tools/mockups/vhorr_blockout.mjs
//   python3 tools/mockups/vhorr_blockout_draw.py
//
// Units: CSS px of the 1920 x 1080 console (1 px = 1 mm of a kit piece at 1x).
import { createRequire } from 'module';
import fs from 'fs';
import path from 'path';
import { fileURLToPath } from 'url';

const { chromium } = createRequire(import.meta.url)('playwright');
const ROOT = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '../..');
const PAGE = path.join(ROOT, 'docs/mockups/console-skins.html');
const OUT = path.join(ROOT, 'assets/ui/vhorr/organic');
// The helm pad's keys are drawn inside its hero image, not as page elements. Their centres come from the page (the
// skin's helmKeys, plus the pad's centre key); a key face is 62 px of the 300 px pad (the v23 kit's key wells,
// tools/mockups/vhorr_shapes.py), drawn at the pad's displayed size.
const HELM_PAD_PX = 300, HELM_CENTRE = [150, 149], KEY_FACE_PX = 62;

// The skin's own data, read from its module (the page's script is a closure, so nothing of it is global).
const SKIN_JS = fs.readFileSync(path.join(ROOT, 'docs/mockups/skins/vhorr.js'), 'utf8');
const HELM_KEYS = JSON.parse(/helmKeys:\s*(\[\[[\d,\s\[\]]+\]\])/.exec(SKIN_JS)[1]);
const PLATE_LEFT = +/plateText:\s*\{\s*left:\s*(\d+)/.exec(SKIN_JS)[1];

const browser = await chromium.launch();
const page = await browser.newPage({ viewport: { width: 1920, height: 1080 }, deviceScaleFactor: 1 });
const errors = [];
page.on('pageerror', (e) => errors.push(String(e)));
await page.goto('file://' + PAGE);
await page.waitForFunction(() => window.MOCKUP_READY, null, { timeout: 60000 });
await page.evaluate(() => window.MOCKUP_SHOTS.vhorr());
await page.waitForTimeout(1500);

const data = await page.evaluate(({ HELM_PAD_PX, HELM_CENTRE, KEY_FACE_PX, HELM_KEYS, PLATE_LEFT }) => {
  const con = document.getElementById('console');
  const s = +(/scale\(([\d.]+)\)/.exec(con.style.transform) || [0, 1])[1];
  const r0 = con.getBoundingClientRect();
  const q = (v) => Math.round(v * 2) / 2;
  const rel = (r) => [q((r.left - r0.left) / s), q((r.top - r0.top) / s), q(r.width / s), q(r.height / s)];
  const man = JSON.parse(document.querySelector('script[data-src="assets/ui/vhorr/manifest.json"]').textContent).pieces;
  const heroId = (e) => { const im = e.querySelector(':scope > img'); return im ? Object.keys(man).find((k) => man[k].data === im.src) || 'hero' : 'hero'; };
  // Which kit piece an element shows, from its classes (as the page's own pieceOf reads them).
  const pieceOf = (e) => { const c = e.classList;
    if (c.contains('k-panel')) return 'frame.panel'; if (c.contains('k-inset')) return 'frame.inset'; if (c.contains('k-track')) return 'bar.track';
    if (c.contains('k-button')) return c.contains('pressed') ? 'button.pressed' : c.contains('hover') ? 'button.hover' : 'button.idle';
    if (c.contains('k-tab')) return c.contains('sel') ? 'tab.active' : 'tab.idle';
    if (c.contains('k-strip')) return c.contains('top') ? 'strip.top' : 'strip.bottom';
    if (c.contains('k-divider')) return 'divider'; return null; };
  const isSlot = (e) => (e.classList.contains('nine') && !e.classList.contains('k-fill')) || e.classList.contains('hero');
  const slotEls = [...con.querySelectorAll('.nine, .hero')].filter(isSlot);
  const text = (e) => (e.textContent || '').trim().replace(/\s+/g, ' ');
  const textRect = (e) => { const g = document.createRange(); const tn = [...e.childNodes].find((n) => n.nodeType === 3 && n.textContent.trim()); if (tn) g.selectNode(tn); else g.selectNodeContents(e); const r = g.getBoundingClientRect(); return r.width ? r : null; };
  // The tight box of the pixels a live canvas draws (its readout), measured, not its whole element.
  const inked = (c) => {
    const d = c.getContext('2d').getImageData(0, 0, c.width, c.height).data; let x0 = c.width, y0 = c.height, x1 = -1, y1 = -1;
    for (let y = 0; y < c.height; y++) for (let x = 0; x < c.width; x++) if (d[(y * c.width + x) * 4 + 3] > 8) { if (x < x0) x0 = x; if (x > x1) x1 = x; if (y < y0) y0 = y; if (y > y1) y1 = y; }
    if (x1 < 0) return null;
    const r = c.getBoundingClientRect(), kx = r.width / c.width, ky = r.height / c.height;
    return { left: r.left + x0 * kx, top: r.top + y0 * ky, width: (x1 - x0 + 1) * kx, height: (y1 - y0 + 1) * ky };
  };
  const names = new Map(), count = {};
  const role = (e, piece) => {
    let n;
    if (e.classList.contains('k-button') || e.classList.contains('k-tab')) n = (e.classList.contains('k-button') ? 'key.' : 'tab.') + text(e).toLowerCase();
    else if (e.classList.contains('k-strip')) n = piece;
    else if (e.classList.contains('k-track')) n = 'track.' + text(e.closest('.barbox').querySelector('.lbl')).toLowerCase().replace(/ /g, '-');
    else if (e.classList.contains('k-panel')) n = 'panel.' + text(e.querySelector('.ptitle')).toLowerCase();
    else if (e.classList.contains('k-inset')) n = e.querySelector('.k-button') ? 'inset.helm-readout' : 'inset.contacts';
    else if (e.classList.contains('k-divider')) n = 'divider';
    else n = piece;
    count[n] = (count[n] || 0) + 1;
    return count[n] > 1 ? `${n}.${count[n]}` : n;
  };
  const slots = slotEls.map((e) => {
    const piece = e.classList.contains('hero') ? heroId(e) : pieceOf(e);
    const id = role(e, piece); names.set(e, id);
    return { e, id, piece, kind: e.classList.contains('hero') ? 'hero' : 'nine', rect: rel(e.getBoundingClientRect()) };
  });
  for (const sl of slots) {
    const e = sl.e, prot = [];
    const mine = (d) => { let p = d.parentElement; while (p && p !== e) { if (isSlot(p)) return false; p = p.parentElement; } return true; };
    for (const d of e.querySelectorAll('.lbl, .num, .ptitle, .ovl, img, canvas.live, .k-cells')) {
      if (!mine(d) || (d.tagName === 'IMG' && (d.parentElement === e || d.closest('.k-cells')))) continue;
      const kind = d.matches('canvas.live') ? 'live-drawing' : d.matches('.k-cells') ? 'bar-cells' : d.tagName === 'IMG' ? 'icon' : 'label';
      const r = d.matches('canvas.live') ? inked(d) : d.matches('.lbl, .num, .ptitle, .ovl') ? textRect(d) : d.getBoundingClientRect();
      if (r && r.width) prot.push({ kind, what: d.matches('canvas.live') ? sl.piece : (d.alt || text(d) || d.className), rect: rel(r) });
    }
    if (e.matches('.k-button, .k-tab')) { const r = textRect(e); if (r) prot.push({ kind: 'key-label', what: text(e), rect: rel(r) }); }
    for (const c of slots) if (c.e !== e && e.contains(c.e) && mine(c.e)) prot.push({ kind: 'child-slot', what: c.id, rect: c.rect });
    if (sl.piece === 'helm.pad') {   // the pad's nine key faces (see HELM_PAD_PX above)
      const r = e.getBoundingClientRect(), k = r.width / s / HELM_PAD_PX, [ex, ey] = rel(r);
      [...HELM_KEYS, HELM_CENTRE].forEach(([cx, cy], i) => prot.push({ kind: 'key-face', what: i < 8 ? `helm key ${i}` : 'helm centre (stop)',
        rect: [q(ex + (cx - KEY_FACE_PX / 2) * k), q(ey + (cy - KEY_FACE_PX / 2) * k), q(KEY_FACE_PX * k), q(KEY_FACE_PX * k)] }));
    }
    if (sl.piece === 'nameplate') {   // the portrait window: the plate's pocket, left of the name field
      const [ex, ey, , eh] = sl.rect;
      prot.push({ kind: 'portrait-window', what: 'hex-filtered portrait', rect: [q(ex + 6), q(ey + 4), q(PLATE_LEFT - 14), q(eh - 8)] });
    }
    sl.protected = prot;
  }
  // The game view the console floats over (rule 5): the page's own painted view.
  return { scale: s, view: con.querySelector('.view canvas').toDataURL('image/png'),
    slots: slots.map(({ id, piece, kind, rect, protected: p }) => ({ id, piece, kind, rect, protected: p })) };
}, { HELM_PAD_PX, HELM_CENTRE, KEY_FACE_PX, HELM_KEYS, PLATE_LEFT });
await browser.close();
if (errors.length) { console.error(errors.join('\n')); process.exit(1); }

// Group slots that share one sculpt: the same kind of piece (states of a key or tab are one shape) at the same size.
const family = (p) => p.replace(/\.(idle|hover|pressed|active)$/, '').replace(/^lamp\.alert\..*/, 'lamp.alert');
const groups = {};
for (const sl of data.slots) {
  const g = `${family(sl.piece)} ${sl.rect[2]}x${sl.rect[3]}`;
  (groups[g] ||= { piece: family(sl.piece), size: [sl.rect[2], sl.rect[3]], slots: [] }).slots.push(sl.id);
  sl.sculpt = g;
}
const helm = data.slots.find((s) => s.piece === 'helm.pad');
const helmKeys = helm.protected.filter((p) => p.kind === 'key-face');
fs.mkdirSync(OUT, { recursive: true });
const out = {
  skin: 'vhorr', method: 'organic: no nine-slice; every slot a hero piece sculpted at its exact rect (Organic Panel Bible rule 4)',
  units: 'CSS px of the 1920 x 1080 console', measured_from: 'docs/mockups/console-skins.html, live DOM, by tools/mockups/vhorr_blockout.mjs',
  layouts: {
    desktop: { size: [1920, 1080], slots: data.slots },
    phone_landscape: { note: 'The console page lays out only the desktop reference; a phone in landscape shows that console scaled to fit, so its slots are the desktop slots at the same reference px. A phone-specific landscape layout, if one is designed, gets its own slot set and sculpts.', size: [1920, 1080], slots: 'same as desktop' },
    phone_portrait: { note: 'No mockup lays out the battle console in portrait yet (CLAUDE.md 7: the console stacks above the view). Its slot set is to be measured when that layout exists; until then it has no sizes.', slots: [] },
  },
  sculpt_groups: Object.entries(groups).map(([k, v]) => ({ key: k, ...v, count: v.slots.length })),
  helm_keys: { count: helmKeys.length, face_px: helmKeys[0].rect[2], note: 'Nine key faces inside helm.pad: one key cap shape (the bible: crisp dark caps with lit rims), placed nine times; the pad around them is one sculpt.' },
};
fs.writeFileSync(path.join(OUT, 'blockout.json'), JSON.stringify(out, null, 1) + '\n');
fs.writeFileSync(path.join(OUT, 'game-view.png'), Buffer.from(data.view.split(',')[1], 'base64'));
console.log(`${data.slots.length} slots in ${out.sculpt_groups.length} sculpt groups; ${helmKeys.length} helm keys`);
for (const g of out.sculpt_groups) console.log(`  ${g.key.padEnd(28)} x${g.count}  ${g.slots.join(', ')}`);
