// The Aelith Conclave skin (mockup 4, OpenSpec change console-skins): a wise and advanced race (owner, 2026-10-30:
// "an wise and advanced alien race. holographic buttons, with organic shaped metal armor peices surrounding the
// edges"). The one hybrid skin (owner: "the conclave is one exception. They have a hybrid of both vector graphics for
// holographic buttons and icons, but 3d rendered shell around the UI panels and elements"), built on the analog panel
// bible (owner, 2026-10-31: "Using these design principles. Update conclave UI"): every piece's shell is satin gold and
// pearl modelled and rendered in Blender (tools/blender/build_ui_aelith.py), its wells a rim-lit void; this module
// draws the holograms as vectors into those wells: pointed lozenges of light for keys, fields with scanlines and a
// split-colour edge, crystals, sacred-geometry icons. Both sides read one set of shapes (assets/ui/aelith/shapes.json,
// fit-checked by tools/mockups/aelith_shapes.py), and every hologram is clipped to its well, so none spills onto the
// gold. A piece is the render and its hologram layered in one SVG image, so the page's 9-slice and hero slots take it
// like any other kit image. Registers itself with the page's skin registry (docs/mockups/skins).
(function () {
'use strict';
const F = window.FCK, P = F.P;
const AE = { pearl: '#e8e4da', gold: '#c9a646', goldHi: '#f6e2a0', goldLo: '#6a4a14', void: '#0b2a33', deep: '#04141a', holo: '#6ff3ff', holoHi: '#dffcff',
  violet: '#9a8cff', coral: '#ff6a5a', amber: '#ffd27a' };
function hexA(hex, a) { const n = parseInt(hex.slice(1), 16); return `rgba(${n >> 16},${(n >> 8) & 255},${n & 255},${a})`; }
function path(x, pts, closed = true) { x.beginPath(); x.moveTo(pts[0][0], pts[0][1]); for (const p of pts.slice(1)) { if (p.length === 4) x.quadraticCurveTo(p[0], p[1], p[2], p[3]); else x.lineTo(p[0], p[1]); } if (closed) x.closePath(); }
const bounds = (pts) => { const xs = pts.map((p) => p[0]), ys = pts.map((p) => p[1]); return [Math.min(...xs), Math.min(...ys), Math.max(...xs), Math.max(...ys)]; };

// A hologram field: translucent light, scanlines, a glowing edge split slightly into colours.
function holo(x, draw, box, level, col = AE.holo) {
  const [x0, y0, x1, y1] = box;
  x.save(); draw(); x.clip();
  const g = x.createLinearGradient(0, y0, 0, y1); g.addColorStop(0, hexA(col, 0.10 + level * 0.32)); g.addColorStop(0.5, hexA(col, 0.04 + level * 0.18)); g.addColorStop(1, hexA(col, 0.12 + level * 0.3));
  x.fillStyle = g; x.fillRect(x0, y0, x1 - x0, y1 - y0);
  x.fillStyle = hexA(AE.holoHi, 0.06 + level * 0.08); for (let y = y0; y < y1; y += 3) x.fillRect(x0, y, x1 - x0, 1);
  x.restore();
  x.save(); x.translate(-0.8, 0); draw(); x.strokeStyle = 'rgba(255,120,200,0.35)'; x.lineWidth = 1; x.stroke(); x.restore();
  x.save(); x.translate(0.8, 0); draw(); x.strokeStyle = 'rgba(120,140,255,0.35)'; x.lineWidth = 1; x.stroke(); x.restore();
  x.save(); x.shadowColor = col; x.shadowBlur = 4 + level * 8; draw(); x.strokeStyle = hexA(level > 0.8 ? AE.holoHi : col, 0.55 + level * 0.45); x.lineWidth = 1.4; x.stroke(); x.restore();
}
const shape = (x, pts) => () => path(x, pts);
const field = (x, pts, level, col) => holo(x, shape(x, pts), bounds(pts), level, col);
// A key's sheen when pressed: a soft bar of light across its middle.
function sheen(x, pts) { const [x0, y0, x1, y1] = bounds(pts); const g = x.createLinearGradient(0, y0, 0, y1); g.addColorStop(0, 'rgba(255,255,255,0)'); g.addColorStop(0.5, 'rgba(240,255,255,0.4)'); g.addColorStop(1, 'rgba(255,255,255,0)'); x.save(); path(x, pts); x.clip(); x.fillStyle = g; x.fillRect(x0, y0, x1 - x0, y1 - y0); x.restore(); }

// ---- the holograms in each piece's wells (role names from shapes.json) ---------------------------------------------
const HOLO = {
  'frame.panel': (x, s) => field(x, s.wells[0], 0.1),
  'frame.inset': (x, s) => field(x, s.wells[0], 0.08),
  'button.idle': (x, s) => field(x, s.holo.key, 0.15),
  'button.hover': (x, s) => field(x, s.holo.key, 0.6),
  'button.pressed': (x, s) => { field(x, s.holo.key, 1); sheen(x, s.holo.key); },
  'tab.idle': (x, s) => field(x, s.holo.tab, 0.08),
  'tab.active': (x, s) => field(x, s.holo.tab, 0.75),
  'strip.top': (x, s) => field(x, s.holo.edge, 0.06),
  'strip.bottom': (x, s) => field(x, s.holo.edge, 0.06),
  'divider': (x, s) => field(x, s.holo.line, 0.6),
  'nameplate': (x, s) => field(x, s.holo.field, 0.15),
  'helm.pad': (x, s) => { for (let i = 0; i < 8; i++) field(x, s.holo['key ' + i], 0.28); field(x, s.holo.crystal, 0.7, AE.violet); },
  'ssd.bezel': (x, s) => field(x, s.wells[0], 0.08),
  'dial': (x, s) => field(x, s.wells[0], 0.1),
  'lamp.alert.green': (x, s) => gem(x, s, '#6fffb0'), 'lamp.alert.yellow': (x, s) => gem(x, s, AE.amber), 'lamp.alert.red': (x, s) => gem(x, s, AE.coral),
  'crest': (x, s) => { field(x, s.holo.triangle, 0.5); field(x, s.holo.crystal, 1, AE.violet); },
};
// An alert lamp: a crystal of coloured light standing in its gold setting.
function gem(x, s, col) {
  const pts = s.holo.crystal, [x0, y0, x1, y1] = bounds(pts), c = (x0 + x1) / 2;
  x.save(); x.shadowColor = col; x.shadowBlur = 12; field(x, pts, 1, col); x.restore();
  x.strokeStyle = 'rgba(255,255,255,0.6)'; x.lineWidth = 1; x.beginPath(); x.moveTo(c, y0 + 2); x.lineTo(c, y1 - 2); x.stroke();
}
// The two fills ride inside the track's channel, so they are light alone (no shell).
const FILLS = { 'bar.fill': AE.holo, 'bar.fill.red': AE.coral };

// One kit image: the rendered shell, with the piece's holograms (clipped to its wells) laid over it in one SVG.
function layered(name, p, s) {
  const [w, h] = p.size, draw = HOLO[name];
  if (!draw) return p.data;
  const c = F.paint(w, h, (x) => { x.save(); x.beginPath(); for (const wl of s.wells) { x.moveTo(wl[0][0], wl[0][1]); for (const q of wl.slice(1)) x.lineTo(q[0], q[1]); x.closePath(); } x.clip(); draw(x, s); x.restore(); });
  const W = w * 2, H = h * 2;
  const svg = `<svg xmlns="http://www.w3.org/2000/svg" width="${W}" height="${H}" viewBox="0 0 ${W} ${H}"><image width="${W}" height="${H}" href="${p.data}"/><image width="${W}" height="${H}" href="${F.png(c)}"/></svg>`;
  return 'data:image/svg+xml;base64,' + btoa(svg);
}
function build() {
  const d = F.kitData('assets/ui/aelith/manifest.json'), shapes = F.kitData('assets/ui/aelith/shapes.json');
  if (!d || !shapes) return null;
  const pieces = {};
  for (const [name, p] of Object.entries(d.pieces)) {
    const raw = shapes.pieces[name], s = { wells: raw.wells, holo: Object.fromEntries(raw.holo) };
    pieces[name] = Object.assign({}, p, { data: layered(name, p, s) });
  }
  for (const [name, col] of Object.entries(FILLS)) {
    pieces[name] = { size: [64, 10], margins: [0, 6, 0, 6], data: F.png(F.paint(64, 10, (x) => holo(x, () => { x.beginPath(); x.roundRect(1, 1, 62, 8, 4); }, [0, 0, 64, 10], 0.9, col))) };
  }
  // The symbol bar: a row of small lozenges of light, the Conclave key's own shape (owner, 2026-10-31: "have different
  // repeated symbols for them"). Unlit, a faint outline in the same scanlines; lit, the lozenge fills with light.
  for (const [name, level, col] of [['bar.cell.off', 0.05, AE.holo], ['bar.cell.on', 0.95, AE.holo], ['bar.cell.warn', 0.95, AE.coral]]) {
    pieces[name] = { kind: 'cell', size: [8, 12], gap: 3, data: F.png(F.paint(8, 12, (x) => lozenge(x, level, col))) };
  }
  return F.fromManifest({ pieces }, icon);
}
function lozenge(x, level, col) {
  const pts = [[4, 0.6], [7.4, 6], [4, 11.4], [0.6, 6]];
  holo(x, () => path(x, pts), [0, 0, 8, 12], level, col);
  if (level > 0.5) {   // lit: the lozenge fills with light, brightest at its heart
    const g = x.createRadialGradient(4, 6, 0.5, 4, 6, 6); g.addColorStop(0, '#ffffff'); g.addColorStop(0.35, AE.holoHi); g.addColorStop(1, hexA(col, 0.7));
    x.save(); path(x, pts); x.clip(); x.fillStyle = g; x.fillRect(0, 0, 8, 12); x.restore();
  }
  x.save(); x.shadowColor = col; x.shadowBlur = level > 0.5 ? 4 : 0; x.strokeStyle = hexA(col, 0.35 + level * 0.6); x.lineWidth = 0.9; path(x, pts); x.stroke(); x.restore();
}

// ---- icons: sacred geometry in hairlines of light ---------------------------------------------------------------
function icon(name) {
  return F.paint(48, 48, (x) => {
    x.strokeStyle = AE.holo; x.fillStyle = AE.holoHi; x.lineWidth = 1.6; x.shadowColor = AE.holo; x.shadowBlur = 5;
    const L = (pts, closed = false) => { path(x, pts, closed); x.stroke(); };
    const D = (cx, cy, r = 2.4) => { x.beginPath(); x.arc(cx, cy, r, 0, 2 * P); x.fill(); };
    const TRI = (cx, cy, r, a = -P / 2) => L([0, 1, 2].map((i) => [cx + Math.cos(a + i * 2 * P / 3) * r, cy + Math.sin(a + i * 2 * P / 3) * r]), true);
    const GEM = (cx, cy, rx, ry) => L([[cx, cy - ry], [cx + rx, cy], [cx, cy + ry], [cx - rx, cy]], true);
    ({
      helm: () => { x.beginPath(); x.arc(24, 24, 19, 0, 2 * P); x.stroke(); TRI(24, 24, 15); D(24, 9); },
      fire: () => { L([[6, 10], [36, 24]]); L([[6, 38], [36, 24]]); L([[4, 24], [36, 24]]); GEM(40, 24, 5, 7); },
      shields: () => { L([[24, 4], [42, 14], [42, 32], [24, 44], [6, 32], [6, 14]], true); TRI(24, 25, 9); },
      tractor: () => { GEM(10, 24, 6, 9); GEM(38, 24, 6, 9); x.setLineDash([2, 3]); L([[16, 24], [32, 24]]); x.setLineDash([]); },
      transport: () => { GEM(24, 16, 8, 11); D(16, 36); D(24, 40); D(32, 36); },
      science: () => { TRI(24, 26, 19); L([[24, 7], [24, 36]]); D(24, 26, 3.2); },
      comms: () => { D(24, 24, 3.4); for (let i = 0; i < 3; i++) { const a = -P / 2 + i * 2 * P / 3; TRI(24 + Math.cos(a) * 14, 24 + Math.sin(a) * 14, 6, a); } },
      repair: () => { GEM(24, 24, 11, 18); L([[18, 20], [24, 26], [30, 22]]); D(24, 26, 2); },
      map: () => { const n = [[8, 12], [22, 8], [38, 16], [30, 32], [12, 38]]; L(n); for (const [px, py] of n) D(px, py); },
      settings: () => { L([[24, 4], [41, 14], [41, 34], [24, 44], [7, 34], [7, 14]], true); TRI(24, 25, 10); D(24, 25, 2.4); },
    })[name]();
  });
}

// ---- the skin --------------------------------------------------------------------------------------------------
F.register('aelith', {
  name: 'Aelith Conclave', kind: 'Hybrid: a gold and pearl shell rendered in Blender, holograms drawn as vectors in its wells', bg: '#03111a',
  css: { '--s-ink': AE.holoHi, '--s-dim': '#9fc9cf', '--s-num': AE.holo, '--s-body': '"Exo 2", sans-serif', '--btn-ink': AE.holoHi, '--btn-shadow': `0 0 6px ${AE.holo}`, '--btn-justify': 'center',
    '--btn-font': '400 11px/1 "Michroma", sans-serif', '--btn-track': '0.08em', '--btn-case': 'uppercase', '--btn-motion': 'filter 0.25s', '--btn-press': 'none',
    '--tab-font': '400 9.5px/1 "Michroma", sans-serif', '--tab-pb': '1px', '--btn-pb': '3px', '--tab-ink': '#9fc9cf', '--tab-ink-on': AE.holoHi, '--title-font': '400 15px/1 "Michroma", sans-serif', '--title-ink': AE.goldHi,
    '--lbl-font': '600 15px/1 "Exo 2", sans-serif', '--num-font': '300 28px/1 "Exo 2", sans-serif',
    // The strips are 72 px (owner, 2026-10-31: "we need to make parent taller"); their open band is the channel, rows 12-59,
    // measured by tools/mockups/console_fit.mjs, so each label sits over its bar with 3 px clear of the art.
    '--stt-pt': '12px', '--stt-pb': '12px', '--stb-pt': '12px', '--stb-pb': '12px', '--strip-lamp': '40px' },
  accent: AE.holo, warn: AE.coral,
  // The title sits inside the well, just under its top rim and right of the corner knuckle (well from 16 px, knuckles to
  // 41 px), so it never crosses the gold.
  title: (t, m) => { t.style.cssText = `left:${48 - m[3]}px;top:${-m[0] + 21}px;letter-spacing:0.18em;text-shadow:0 0 6px ${AE.holo};color:${AE.holoHi};`; },
  plateText: { left: 60, top: 12, width: 340, height: 32, font: '400 15px/1 "Michroma", sans-serif', color: AE.holoHi, sub: 'SERENE TIDE · ELDER IAVEN' },
  helmKeys: 'ring', helmR: 98, shield: [AE.holo, AE.amber, AE.coral],
  fonts: ['400 20px "Michroma"', '600 20px "Exo 2"', '300 20px "Exo 2"'], view: { title: '400 22px "Michroma"', body: '300 24px "Exo 2"', upper: true },
  build,
  live: {
    // A pointed mote of light leading a dot.
    arrow: (x) => { x.shadowColor = AE.holo; x.shadowBlur = 6; x.fillStyle = AE.holoHi; path(x, [[12, 0], [-2, -5], [-8, 0], [-2, 5]]); x.fill(); x.beginPath(); x.arc(-13, 0, 2, 0, 2 * P); x.fill(); },
    // The ship as a hologram: a shell outlined in light round a crystal core; shields are arcs of light in dotted rings.
    ssd: (x, cx, cy, sh, col) => {
      for (let i = 0; i < 6; i++) { const a = -P / 2 + i * P / 3; x.save(); x.shadowColor = col(sh[i]); x.shadowBlur = 8; x.strokeStyle = col(sh[i]); x.lineWidth = 3; x.beginPath(); x.arc(cx, cy, 104, a - P / 6 + 0.09, a + P / 6 - 0.09); x.stroke(); x.setLineDash([1, 5]); x.lineWidth = 1.5; x.beginPath(); x.arc(cx, cy, 112, a - P / 6 + 0.09, a + P / 6 - 0.09); x.stroke(); x.restore(); }
      const hull = [[cx, cy - 76], [cx + 52, cy - 50, cx + 46, cy + 14], [cx + 36, cy + 58, cx, cy + 66], [cx - 36, cy + 58, cx - 46, cy + 14], [cx - 52, cy - 50, cx, cy - 76]];
      holo(x, () => path(x, hull), [cx - 52, cy - 76, cx + 52, cy + 66], 0.35);
      holo(x, () => path(x, [[cx, cy - 28], [cx + 14, cy], [cx, cy + 28], [cx - 14, cy]]), [cx - 14, cy - 28, cx + 14, cy + 28], 1, AE.violet);
      for (const [mx, my] of [[cx, cy - 60], [cx - 36, cy + 20], [cx + 36, cy + 20]]) holo(x, () => path(x, [[mx, my - 6], [mx + 5, my], [mx, my + 6], [mx - 5, my]]), [mx - 5, my - 6, mx + 5, my + 6], 1);
    },
    // Rings of light, a soft sweep, contacts as coral motes.
    dial: (x, c, cts) => {
      x.strokeStyle = hexA(AE.holo, 0.5); x.lineWidth = 1; for (const r of [16, 30, 42]) { x.beginPath(); x.arc(c, c, r, 0, 2 * P); x.stroke(); }
      const g = x.createConicGradient ? x.createConicGradient(-P / 2, c, c) : null;
      if (g) { g.addColorStop(0, hexA(AE.holo, 0.45)); g.addColorStop(0.18, hexA(AE.holo, 0)); g.addColorStop(1, hexA(AE.holo, 0)); x.fillStyle = g; x.beginPath(); x.arc(c, c, 44, 0, 2 * P); x.fill(); }
      for (const [dx, dy] of cts) { x.save(); x.shadowColor = AE.coral; x.shadowBlur = 6; x.fillStyle = AE.coral; path(x, [[c + dx * 34, c + dy * 34 - 5], [c + dx * 34 + 4, c + dy * 34], [c + dx * 34, c + dy * 34 + 5], [c + dx * 34 - 4, c + dy * 34]]); x.fill(); x.restore(); }
    },
    // Four crescents of light cupping the target, a hairline of light between them.
    bracket: (x, bx, by, bw, bh) => {
      x.save(); x.shadowColor = AE.holo; x.shadowBlur = 6; x.strokeStyle = hexA(AE.holo, 0.6); x.lineWidth = 1; x.strokeRect(bx + 10, by + 10, bw - 20, bh - 20);
      x.strokeStyle = AE.holoHi; x.lineWidth = 2.4;
      for (const [px, py, a] of [[bx, by, P], [bx + bw, by, -P / 2], [bx, by + bh, P / 2], [bx + bw, by + bh, 0]]) { x.beginPath(); x.arc(px, py, 18, a - 0.1, a + P / 2 + 0.1 - P / 2 + P / 2); x.stroke(); }
      x.restore();
    },
  },
  world: ['Patient knowledge kept over aeons: act rarely, act exactly.', 'Graceful bodies and psionic minds: they touch light, not switches.', 'Light shaped by thought, held in sculpted alloy shells.', 'Smooth golden shells round crystal cores.'],
  shapes: 'Pointed lozenges of light held in pillowed gold shells whose wells are grown into knuckles, scallops and embracing arms; pearls set in the metal.', light: 'A soft main light from the upper left on satin gold; each well a dark void rim-lit in cyan from under its lip; holograms with scanlines and a split-colour edge.',
  motion: 'Light brightens and settles; nothing moves.', type: '"Michroma" wide capitals over "Exo 2".',
  palette: [['Pearl', AE.pearl, 'Inlays', 'Wisdom: the slow layering of ages'], ['Gold', AE.gold, 'Armour', 'Permanence'], ['Void', AE.void, 'Fields', 'Serenity, depth'],
    ['Holo', AE.holo, 'Light, readouts', 'Thought made visible'], ['Violet', AE.violet, 'Crystal cores', 'The psionic'], ['Coral', AE.coral, 'Alerts', 'A rare disturbance']],
});
})();
