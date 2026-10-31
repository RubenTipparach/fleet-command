// The Krai Warhost skin (mockup 4, OpenSpec change console-skins): a combat race (owner, 2026-10-30: "a combat focused
// race, lots of triangular motifs, angry red colors and orange lit buttons that are activated. strong metal framing,
// almost blade like"), rebuilt on the analog panel bible (owner, 2026-10-31: "update krai, onbrai, and hunt to use the
// new guildine standards"). The kit is modelled and rendered in Blender (tools/blender/build_ui_krai.py) from fit-checked
// shapes (tools/mockups/krai_shapes.py, assets/ui/krai/shapes.json): blackened blade steel plates with a tight honed
// roll, red enamel bezels round every well, ember glass lit from below like a forge, Blade keys dark at rest, red on
// hover and forge orange when engaged, and bars of blade chevrons that light one by one. This module adds the type,
// the ink, the icons and the live readouts drawn into the measured windows. Kept apart from the Kethran Hegemony
// (gunmetal plates, wedge keycaps, amber chevrons): thinner, sharper, black and red. Registers itself with the page's
// skin registry (docs/mockups/skins).
(function () {
'use strict';
const F = window.FCK, P = F.P;
const KR = { black: '#15100f', steel: '#2a2526', edge: '#c9d0d8', blood: '#b3121b', red: '#ff2a2a', orange: '#ff7a1a', hot: '#ffb070', bone: '#e8dccb', dim: '#a8908a' };
function hexA(hex, a) { const n = parseInt(hex.slice(1), 16); return `rgba(${n >> 16},${(n >> 8) & 255},${n & 255},${a})`; }
function poly(x, pts) { x.beginPath(); x.moveTo(pts[0][0], pts[0][1]); for (const p of pts.slice(1)) x.lineTo(p[0], p[1]); x.closePath(); }

// ---- materials ---------------------------------------------------------------------------------------------------
// Blade steel: near-black, brushed along its length, with a honed edge that catches a hard white line of light.
function steel(x, pts, box, o = {}) {
  const [x0, y0, x1, y1] = box;
  x.save(); poly(x, pts); x.clip();
  const g = x.createLinearGradient(x0, y0, x0, y1); g.addColorStop(0, o.top || '#3a3335'); g.addColorStop(0.5, KR.steel); g.addColorStop(1, KR.black); x.fillStyle = g; x.fillRect(x0, y0, x1 - x0, y1 - y0);
  x.globalAlpha = 0.06; for (let y = y0; y < y1; y += 2) { x.fillStyle = (y / 2) % 2 ? '#ffffff' : '#000000'; x.fillRect(x0, y, x1 - x0, 1); } x.globalAlpha = 1;
  x.restore();
  // the honed edge: the upper and left faces catch the light
  x.save(); x.lineWidth = 1.3; x.strokeStyle = hexA(KR.edge, o.edge ?? 0.85);
  for (let i = 0; i < pts.length; i++) { const a = pts[i], b = pts[(i + 1) % pts.length], nx = b[1] - a[1], ny = a[0] - b[0]; if (ny < -0.1 || nx < -0.1 && ny <= 0.1) { x.beginPath(); x.moveTo(a[0], a[1]); x.lineTo(b[0], b[1]); x.stroke(); } }
  x.restore();
  poly(x, pts); x.strokeStyle = '#050303'; x.lineWidth = 1; x.stroke();
}
function enamel(x, pts, col = KR.blood, glow = 0) {
  x.save(); if (glow) { x.shadowColor = col; x.shadowBlur = glow; } poly(x, pts);
  const [xs, ys] = [pts.map((p) => p[0]), pts.map((p) => p[1])], g = x.createLinearGradient(0, Math.min(...ys), 0, Math.max(...ys));
  g.addColorStop(0, col === KR.blood ? '#d8232c' : KR.hot); g.addColorStop(1, col); x.fillStyle = g; x.fill(); x.restore();
  poly(x, pts); x.strokeStyle = 'rgba(0,0,0,0.6)'; x.lineWidth = 0.8; x.stroke();
}

// ---- icons: triangles and blades only ---------------------------------------------------------------------------
function icon(name) {
  return F.paint(48, 48, (x) => {
    const T = (pts, col = KR.red) => { x.save(); x.shadowColor = col; x.shadowBlur = 4; poly(x, pts); x.fillStyle = col; x.fill(); x.restore(); x.strokeStyle = hexA(KR.edge, 0.7); x.lineWidth = 0.8; x.beginPath(); x.moveTo(pts[0][0], pts[0][1]); x.lineTo(pts[1][0], pts[1][1]); x.stroke(); };
    ({
      helm: () => { T([[24, 3], [40, 40], [24, 30], [8, 40]]); },
      fire: () => { T([[4, 8], [30, 22], [4, 20]]); T([[4, 28], [30, 26], [4, 40]]); T([[30, 18], [46, 24], [30, 30]], KR.orange); },
      shields: () => { T([[4, 6], [44, 6], [24, 44]]); x.fillStyle = '#15100f'; poly(x, [[14, 12], [34, 12], [24, 32]]); x.fill(); },
      tractor: () => { T([[2, 10], [20, 24], [2, 38]]); T([[46, 10], [28, 24], [46, 38]]); },
      transport: () => { T([[8, 4], [40, 4], [24, 16]]); T([[12, 18], [36, 18], [24, 28]], KR.orange); T([[16, 30], [32, 30], [24, 38]], KR.blood); },
      science: () => { T([[24, 4], [46, 42], [2, 42]]); x.fillStyle = '#15100f'; poly(x, [[24, 22], [30, 31], [24, 38], [18, 31]]); x.fill(); },
      comms: () => { T([[4, 20], [14, 24], [4, 28]]); T([[18, 12], [32, 24], [18, 36]]); T([[34, 4], [46, 24], [34, 44]], KR.orange); },
      repair: () => { T([[4, 6], [10, 4], [44, 40], [40, 44]]); T([[44, 6], [38, 4], [4, 40], [8, 44]], KR.orange); },
      map: () => { T([[24, 2], [29, 19], [19, 19]]); T([[24, 46], [19, 29], [29, 29]]); T([[2, 24], [19, 19], [19, 29]]); T([[46, 24], [29, 29], [29, 19]]); },
      settings: () => { T([[24, 4], [44, 40], [4, 40]]); x.fillStyle = '#15100f'; for (const [px, py] of [[24, 18], [32, 32], [16, 32]]) { poly(x, [[px, py - 4], [px + 4, py + 3], [px - 4, py + 3]]); x.fill(); } },
    })[name]();
  });
}

// ---- the skin --------------------------------------------------------------------------------------------------
F.register('krai', {
  name: 'Krai Warhost', kind: 'Black blade steel and red enamel; keys lit orange when engaged', bg: '#0a0707',
  css: { '--s-ink': KR.bone, '--s-dim': KR.dim, '--s-num': KR.orange, '--s-body': '"Teko", sans-serif', '--btn-ink': KR.bone, '--btn-shadow': '0 1px 2px #000, 0 0 3px #000', '--btn-justify': 'center',
    '--btn-font': '400 18px/1 "Russo One", sans-serif', '--btn-track': '0.06em', '--btn-case': 'uppercase', '--btn-motion': 'none', '--btn-press': 'translateX(2px)',
    '--tab-font': '400 14px/1 "Russo One", sans-serif', '--tab-ink': KR.dim, '--tab-ink-on': KR.bone, '--title-font': '400 18px/1 "Russo One", sans-serif', '--title-ink': KR.red,
    '--lbl-font': '500 19px/1 "Teko", sans-serif', '--num-font': '600 28px/1 "Teko", sans-serif',
    // The strips' blade channel leaves a 30 px band (rows 13-43): each label sits beside its bar of chevrons, and the
    // lamp is sized to the band (measured by tools/mockups/console_fit.mjs).
    '--bar-flow': 'column', '--bar-cols': 'auto 1fr', '--bar-gap': '10px', '--stt-pt': '14px', '--stt-pb': '13px', '--stb-pt': '14px', '--stb-pb': '13px', '--strip-lamp': '22px' },
  accent: KR.orange, warn: KR.red,
  // The title heads the open field of the well (in flow, below the corner fangs), never on the steel or its enamel.
  title: (t, m) => { t.style.cssText = `position:relative;justify-self:start;align-self:flex-start;margin-left:4px;letter-spacing:0.1em;text-shadow:0 1px 0 #000;`; },
  plateText: { left: 86, top: 11, width: 318, height: 34, font: '400 20px/1 "Russo One", sans-serif', color: KR.bone, sub: 'KHARZ · WARLORD UTREK' },
  helmKeys: 'ring', helmR: 96, shield: [KR.orange, '#ffc23a', KR.red],
  fonts: ['400 20px "Russo One"', '500 20px "Teko"', '600 20px "Teko"'], view: { title: '400 26px "Russo One"', body: '500 28px "Teko"', upper: true },
  build: () => F.fromManifest(F.kitData('assets/ui/krai/manifest.json'), icon),
  live: {
    arrow: (x) => { x.fillStyle = KR.bone; poly(x, [[12, 0], [-6, -6], [-2, 0], [-6, 6]]); x.fill(); x.strokeStyle = KR.blood; x.lineWidth = 1; x.stroke(); },
    helmCentre: (x, c) => { x.fillStyle = KR.bone; x.font = '400 13px "Russo One"'; x.textAlign = 'center'; x.fillText('HOLD', c, c + 8); },
    // A knife-blade hull swept back into wings; shields as rows of red teeth.
    ssd: (x, cx, cy, sh, col) => {
      for (let i = 0; i < 6; i++) { const a0 = -P / 2 + i * P / 3 - P / 6 + 0.1, a1 = a0 + P / 3 - 0.2; x.fillStyle = col(sh[i]); for (let k = 0; k <= 6; k++) { const a = a0 + (a1 - a0) * k / 6; poly(x, [[cx + Math.cos(a) * 98, cy + Math.sin(a) * 98], [cx + Math.cos(a + 0.04) * 110, cy + Math.sin(a + 0.04) * 110], [cx + Math.cos(a - 0.04) * 110, cy + Math.sin(a - 0.04) * 110]]); x.fill(); } }
      steel(x, [[cx, cy - 82], [cx + 10, cy + 10], [cx + 50, cy + 56], [cx + 12, cy + 44], [cx, cy + 66], [cx - 12, cy + 44], [cx - 50, cy + 56], [cx - 10, cy + 10]], [cx - 50, cy - 82, cx + 50, cy + 66]);
      enamel(x, [[cx, cy - 50], [cx + 5, cy + 10], [cx - 5, cy + 10]], KR.red, 6);
      for (const [mx, my] of [[cx, cy - 70], [cx - 36, cy + 46], [cx + 36, cy + 46]]) enamel(x, [[mx, my - 6], [mx + 5, my + 4], [mx - 5, my + 4]], KR.orange, 6);
    },
    // A thin wedge sweeps; contacts are red triangles.
    dial: (x, c, cts) => {
      x.save(); x.shadowColor = KR.orange; x.shadowBlur = 8; x.fillStyle = hexA(KR.orange, 0.55); poly(x, [[c, c], [c + Math.cos(-1.1) * 40, c + Math.sin(-1.1) * 40], [c + Math.cos(-0.8) * 40, c + Math.sin(-0.8) * 40]]); x.fill(); x.restore();
      for (const [dx, dy] of cts) { const px = c + dx * 32, py = c + dy * 32; enamel(x, [[px, py - 5], [px + 5, py + 4], [px - 5, py + 4]], KR.red, 6); }
    },
    // Four red spikes stabbing at the target's corners.
    bracket: (x, bx, by, bw, bh) => {
      for (const [px, py, sx, sy] of [[bx, by, 1, 1], [bx + bw, by, -1, 1], [bx, by + bh, 1, -1], [bx + bw, by + bh, -1, -1]]) {
        x.save(); x.translate(px, py); x.scale(sx, sy); enamel(x, [[-8, -8], [48, 0], [8, 6], [0, 48]], KR.red, 10); x.restore();
      }
    },
  },
  world: ['War is the only honest test; rank is won in blood.', 'Lean predators with long claws: short, sharp gestures.', 'Forged steel, edged everything; nothing round survives the forge.', 'Knife-blade hulls and swept triangles.'],
  shapes: 'Blades, fangs, spikes and triangles; nothing round.', light: 'A forge glow rising from under every lip; honed steel edges; red enamel; keys dark until engaged, then lit orange.',
  motion: 'A key thrusts forward 2 px when struck.', type: '"Russo One" for keys and titles over "Teko" numbers.',
  palette: [['Black steel', KR.black, 'Frames, keys', 'The forge'], ['Edge', KR.edge, 'Honed highlights', 'Readiness'], ['Blood', KR.blood, 'Inlays', 'Honour paid in blood'],
    ['Red', KR.red, 'Titles, warnings', 'Rage'], ['Orange', KR.orange, 'Engaged keys, readouts', 'The strike'], ['Bone', KR.bone, 'Text', 'Trophies']],
});
})();
