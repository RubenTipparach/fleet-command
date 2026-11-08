// The Vhorr Brood skin (mockup 4, OpenSpec change console-skins): an organic race (owner, 2026-10-30: "an organic alien
// race, lots of leather shining through light, slimy carapace, tendrils"). Since version 25 it is built on the Organic
// Panel Bible (docs/design/organic-bible.html; owner, 2026-10-31: "Nah that's bad lol. Let's make a new design system for
// organic", then of the blockout: "Go"): no nine-slice (rule 4) and no raw primitives (rule 1). Every slot of the console
// is sculpted at its exact, pinned size by the parametric sculptors (tools/blender/organic_components.py, run by
// tools/blender/build_ui_vhorr_organic.py over assets/ui/vhorr/organic/blockout.json): bone, then muscle, then one skin of
// leather stretched over both, veins and sacs of light under it (rule 2), black smoked glass with a bronze rim in every
// window, dark key caps with hot rims set into collars of flesh, the crystal heart as the crest; the panels float over the
// game with nothing behind them (rule 5), and the portrait is live pixels under a fine hex-cell filter (rule 3). The kit's
// symbol cells are still the version 23 renders (assets/ui/vhorr/); its icons are docs/mockups/lib/vhorr.js's. This
// module loads both and draws the live readouts. Registers itself with the page's skin registry (docs/mockups/skins).
(function () {
'use strict';
const F = window.FCK, P = F.P;
// The palette, the icons, the live arrow and target bracket and the tendril they are drawn with have one
// implementation, docs/mockups/lib/vhorr.js (window.FCVhorr, inlined before this module), shared with the battle
// console; this module keeps the skins page's own tokens, its slot overlay and the readouts only this page draws.
const H = window.FCVhorr, VH = H.VH, tendril = H.tendril, curl = H.curl;

// ---- organic shape helpers -------------------------------------------------------------------------------------
// A closed path through points, smoothed by quadratic curves between midpoints (no corners anywhere: nothing is cut).
function smooth(x, pts) {
  const n = pts.length, m = (i) => [(pts[i % n][0] + pts[(i + 1) % n][0]) / 2, (pts[i % n][1] + pts[(i + 1) % n][1]) / 2];
  x.beginPath(); const s = m(0); x.moveTo(s[0], s[1]);
  for (let i = 1; i <= n; i++) { const p = pts[i % n], q = m(i); x.quadraticCurveTo(p[0], p[1], q[0], q[1]); }
  x.closePath();
}
// A blob: an ellipse whose radius breathes with seeded noise.
function blob(cx, cy, rx, ry, R, amp = 0.08, n = 28) {
  const nz = F.noise1(R); const o = [];
  for (let i = 0; i < n; i++) { const a = i / n * 2 * P, k = 1 + nz(a * 1.7) * amp; o.push([cx + Math.cos(a) * rx * k, cy + Math.sin(a) * ry * k]); }
  return o;
}
// Light through hide: a leathery fill, the glow behind it, veins, pores, and a dark rim where the skin thickens.
function hide(x, path, R, box, glow = VH.bile, strength = 0.55) {
  const [x0, y0, x1, y1] = box, w = x1 - x0, h = y1 - y0;
  x.save(); path(); x.clip();
  x.fillStyle = VH.hide; x.fillRect(x0, y0, w, h);
  const g = x.createRadialGradient(x0 + w / 2, y0 + h / 2, 0, x0 + w / 2, y0 + h / 2, Math.max(w, h) * 0.62);
  g.addColorStop(0, hexA(glow, strength)); g.addColorStop(0.55, hexA(VH.flesh, strength * 0.55)); g.addColorStop(1, 'rgba(0,0,0,0)');
  x.fillStyle = g; x.fillRect(x0, y0, w, h);
  veins(x, box, R, Math.max(3, Math.round(w * h / 1800)));
  for (let i = 0; i < w * h / 40; i++) { x.fillStyle = `rgba(30,12,10,${0.08 + R() * 0.12})`; x.beginPath(); x.arc(x0 + R() * w, y0 + R() * h, 0.4 + R() * 0.7, 0, 2 * P); x.fill(); }
  path(); x.strokeStyle = 'rgba(20,8,8,0.55)'; x.lineWidth = 5; x.stroke();
  x.restore();
}
// Branching veins, dark red, thinning as they go.
function veins(x, [x0, y0, x1, y1], R, n) {
  x.strokeStyle = 'rgba(110,20,30,0.55)';
  const grow = (px, py, a, len, w, d) => {
    if (d > 3 || len < 3) return;
    const pts = [[px, py]]; let cx = px, cy = py;
    for (let i = 0; i < 4; i++) { a += (R() - 0.5) * 0.7; cx += Math.cos(a) * len / 4; cy += Math.sin(a) * len / 4; pts.push([cx, cy]); }
    x.lineWidth = w; x.beginPath(); x.moveTo(pts[0][0], pts[0][1]); for (const p of pts.slice(1)) x.lineTo(p[0], p[1]); x.stroke();
    grow(cx, cy, a + 0.6, len * 0.6, w * 0.6, d + 1); grow(cx, cy, a - 0.6, len * 0.55, w * 0.6, d + 1);
  };
  for (let i = 0; i < n; i++) grow(x0 + R() * (x1 - x0), y0 + R() * (y1 - y0), R() * 2 * P, 14 + R() * 26, 1.6, 0);
}
// Wet chitin: a dark plate, a purple rim light, one hard white specular streak, a few cracks.
function chitin(x, path, R, box, hi = 0.75) {
  const [x0, y0, x1, y1] = box;
  x.save(); path(); x.clip();
  const g = x.createLinearGradient(x0, y0, x1, y1); g.addColorStop(0, VH.chitinHi); g.addColorStop(0.45, VH.chitin); g.addColorStop(1, '#0e080e');
  x.fillStyle = g; x.fillRect(x0, y0, x1 - x0, y1 - y0);
  x.strokeStyle = 'rgba(0,0,0,0.5)'; x.lineWidth = 0.8;
  for (let i = 0; i < 3; i++) { x.beginPath(); let cx = x0 + R() * (x1 - x0), cy = y0 + R() * (y1 - y0); x.moveTo(cx, cy); for (let k = 0; k < 3; k++) { cx += (R() - 0.5) * 10; cy += (R() - 0.5) * 10; x.lineTo(cx, cy); } x.stroke(); }
  x.restore();
  x.save(); path(); x.clip(); path(); x.strokeStyle = 'rgba(190,140,220,0.35)'; x.lineWidth = 3; x.stroke(); x.restore();
  path(); x.strokeStyle = '#0a050a'; x.lineWidth = 1.6; x.stroke();
  // the slime: a bright streak and a few droplets near the top left
  x.save(); path(); x.clip(); x.globalAlpha = hi;
  const sx = x0 + (x1 - x0) * 0.22, sy = y0 + (y1 - y0) * 0.24;
  x.strokeStyle = VH.slime; x.lineWidth = 1.6; x.beginPath(); x.ellipse(sx + 4, sy + 3, (x1 - x0) * 0.22, (y1 - y0) * 0.12, -0.5, P * 1.05, P * 1.55); x.stroke();
  for (let i = 0; i < 3; i++) { x.fillStyle = VH.slime; x.beginPath(); x.arc(sx + R() * (x1 - x0) * 0.3, sy + R() * (y1 - y0) * 0.3, 0.6 + R() * 0.9, 0, 2 * P); x.fill(); }
  x.restore();
}
function hexA(hex, a) { const n = parseInt(hex.slice(1), 16); return `rgba(${n >> 16},${(n >> 8) & 255},${n & 255},${a})`; }
const RNG = (k) => F.rng(F.hash('vhorr.' + k));

// ---- live drawing helpers (the kit itself is rendered in Blender: tools/blender/build_ui_vhorr.py) ---------------
// One pod seen from above, its long axis along x, lit from within.
function podDisc(x, rx, ry, core, R, col = VH.bile) {
  const pts = blob(0, 0, rx, ry, R, 0.07, 18);
  x.save(); smooth(x, pts); x.clip();
  x.fillStyle = VH.hide; x.fillRect(-rx - 4, -ry - 4, 2 * rx + 8, 2 * ry + 8);
  const g = x.createRadialGradient(0, 0, 1, 0, 0, Math.max(rx, ry)); g.addColorStop(0, hexA(col, core)); g.addColorStop(0.6, hexA(VH.flesh, 0.3)); g.addColorStop(1, 'rgba(30,10,12,0.9)');
  x.fillStyle = g; x.fillRect(-rx - 4, -ry - 4, 2 * rx + 8, 2 * ry + 8); veins(x, [-rx, -ry, rx, ry], R, 2); x.restore();
  smooth(x, pts); x.strokeStyle = '#1a0a0c'; x.lineWidth = 2; x.stroke();
  x.fillStyle = 'rgba(255,248,235,0.7)'; x.beginPath(); x.ellipse(-rx * 0.4, -ry * 0.45, rx * 0.22, ry * 0.14, -0.4, 0, 2 * P); x.fill();
}
// ---- the skin --------------------------------------------------------------------------------------------------
F.register('vhorr', {
  name: 'Vhorr Brood', kind: 'Organic, sculpted in Blender per slot on the Organic Panel Bible: leather over muscle and bone, no nine-slice, an overlay', bg: '#0b080a',
  css: { '--s-ink': VH.ink, '--s-dim': VH.dim, '--s-num': VH.readout, '--s-body': '"Barlow Condensed", sans-serif', '--btn-ink': VH.keyInk, '--btn-shadow': '0 0 6px rgba(255,132,0,0.55)', '--btn-justify': 'center',
    '--btn-font': '400 16px/1 "Metamorphous", serif', '--btn-track': '0.02em', '--btn-case': 'none', '--btn-motion': 'padding-top 0.06s', '--btn-sink': '1px', '--btn-pt': '2px', '--btn-pb': '0px',
    '--tab-font': '400 13px/1 "Metamorphous", serif', '--tab-ink': VH.dim, '--tab-ink-on': VH.readout, '--title-font': '400 21px/1 "Metamorphous", serif', '--title-ink': VH.ink,
    '--lbl-font': '600 17px/1 "Barlow Condensed", sans-serif', '--num-font': '600 27px/1 "Barlow Condensed", sans-serif',
    // The strips are 72 px; their open band is the captured beam's channel of dark glass, rows 13-59 inside its rolled lip
    // (tools/mockups/vhorr_shapes.py), so each label stands over its bar with 3 px clear of the art.
    '--stt-pt': '13px', '--stt-pb': '13px', '--stb-pt': '13px', '--stb-pb': '13px', '--strip-lamp': '40px' },
  accent: VH.bile, warn: VH.blood,
  // The title sits in the frame's well, right of the top-left creep, under the well's rolled lip.
  title: (t, m) => { t.style.cssText = `left:${38 - m[3]}px;top:${19 - m[0]}px;text-shadow:0 1px 2px #000;`; },
  plateText: { left: 76, top: 13, width: 352, height: 30, font: '400 21px/1 "Metamorphous", serif', color: VH.ink, shadow: '0 1px 2px #000', sub: 'Sskarr · Matriarch Vhaal' },
  helmKeys: [[150, 63], [236, 63], [236, 149], [236, 235], [150, 235], [64, 235], [64, 149], [64, 63]], shield: [VH.bile, VH.amber, VH.blood],
  fonts: ['400 20px "Metamorphous"', '600 20px "Barlow Condensed"'], view: { title: '400 28px "Metamorphous"', body: '600 25px "Barlow Condensed"', upper: false },
  // The sculpted slots (assets/ui/vhorr/organic/manifest.json, kind "organic") over the version 23 kit, which still gives
  // the symbol cells, the icons and the layout margins the console was measured with (so the blockout's slots hold).
  build: () => {
    const d = F.kitData('assets/ui/vhorr/manifest.json'), o = F.kitData('assets/ui/vhorr/organic/manifest.json');
    if (!d || !o) return null;
    // the icons are the lib's (the icon guide's subjects in the brood's hand), not the version 23 renders
    const pieces = Object.fromEntries(Object.entries(d.pieces).filter(([k]) => !k.startsWith('icon.')));
    const K = F.fromManifest(Object.assign({}, d, { pieces }), (n) => H.icon(n)), img = {};
    for (const [k, p] of Object.entries(o.pieces)) img[k] = p.data;
    K.organic = { slots: o.slots, img, portrait: img.portrait, portraitRect: o.portrait.rect, hex: o.hex, backing: o.backing ? { src: img.backing, rect: o.backing.image_rect } : null };
    for (const id of Object.keys(K.hero)) {   // the hero pieces are sculpted slots too (the lamp's three colours are one slot's states)
      const lamp = id.startsWith('lamp.alert.'), sl = lamp ? 'lamp.alert.yellow' : id, so = o.slots[sl];
      const src = so && img[`${sl}.${lamp ? id.split('.').pop() : so.rest}`];
      if (src) K.hero[id] = { src, size: so.rect.slice(2) };
    }
    return K;
  },
  live: {
    arrow: H.live.arrow,
    // A living hull: carapace segments over glowing flesh, trailing tendrils; shields are membranes.
    ssd: (x, cx, cy, sh, col) => {
      for (let i = 0; i < 6; i++) { const a = -P / 2 + i * P / 3; x.save(); x.shadowColor = col(sh[i]); x.shadowBlur = 10; x.strokeStyle = hexA(col(sh[i]), 0.75); x.lineWidth = 9; x.beginPath(); x.arc(cx, cy, 100, a - P / 6 + 0.1, a + P / 6 - 0.1); x.stroke(); x.restore(); }
      for (const s of [-1, 1]) tendril(x, [[cx + s * 10, cy + 40], [cx + s * 22, cy + 62], [cx + s * 14, cy + 84]], 5, 1, VH.flesh);
      const R = RNG('ssd.ship');
      x.save(); x.translate(cx, cy); x.rotate(P / 2); podDisc(x, 62, 30, 0.75, R); x.restore();
      for (let i = 0; i < 4; i++) { x.save(); x.translate(cx, cy - 40 + i * 22); chitin(x, () => smooth(x, [[-24, 0], [0, -9], [24, 0], [0, 7]]), R, [-24, -9, 24, 8], 0.5); x.restore(); }
      for (const [mx, my] of [[cx, cy - 58], [cx - 26, cy + 4], [cx + 26, cy + 4]]) { x.save(); x.shadowColor = VH.bileHot; x.shadowBlur = 8; x.fillStyle = VH.bileHot; x.beginPath(); x.arc(mx, my, 5, 0, 2 * P); x.fill(); x.restore(); }
    },
    // A pulse: rings spreading from the heart; contacts glow by threat, the nearer the hotter (bile far, amber, blood near).
    dial: (x, c, cts) => {
      for (const [r, a] of [[14, 0.7], [26, 0.45], [36, 0.25]]) { x.strokeStyle = hexA(VH.bile, a); x.lineWidth = 2; x.beginPath(); x.arc(c, c, r, 0, 2 * P); x.stroke(); }
      for (const [dx, dy] of cts) {
        const d = Math.hypot(dx, dy), col = d < 0.56 ? VH.blood : d < 0.8 ? VH.amber : VH.bile;
        x.save(); x.shadowColor = col; x.shadowBlur = 8; x.fillStyle = col; x.beginPath(); x.arc(c + dx * 32, c + dy * 32, d < 0.56 ? 5 : 4, 0, 2 * P); x.fill(); x.restore();
      }
    },
    bracket: H.live.bracket,
  },
  world: ['The brood is one body: it grows, consumes and adapts.', 'Its consoles are grown, not built: bone, then muscle, then leather stretched over both.', 'The instruments it holds are hard and crisp: black glass, dark keys with hot rims.', 'It floats over the battle like a living thing; nothing stands behind it.'],
  shapes: 'One sculpted body per slot: a thick rolled lip round every window, a vertebral ridge with horns, muscle lobes, veins under the skin. No raw primitives, no nine-slice.', light: 'A warm key from above and grazing rims on wet ridges; sacs of light glow green through the skin; the crystal heart glows violet.',
  motion: 'A pressed key\'s cap sinks into its collar of flesh and its rim burns hotter, its letters sinking with it; the flesh stays put.', type: '"Metamorphous" for keys and titles over "Barlow Condensed" numbers.',
  palette: [['Leather', VH.leather, 'Every frame', 'The brood\'s one skin'], ['Bone', VH.bone, 'Crests through thin skin', 'The frame under the flesh'], ['Glass', VH.glass, 'Every screen', 'Calm behind the readouts'],
    ['Readout', VH.readout, 'Readouts, life', 'Growth: the light inside the body'], ['Key', '#ff8400', 'Key rims', 'Touch'], ['Heart', VH.hero, 'The crest', 'The brood\'s heartbeat'], ['Blood', VH.blood, 'Alerts', 'A wound']],
});
})();
