// The Tarsine Remnant skin (mockup 4, OpenSpec change console-skins): a powerful ancient race that stopped changing
// long ago (owner, 2026-10-30: "a powerful ancient race. rusty, with back lit buttons clogged with dirt. their ships
// havent been upgraded in ages, rough around the edges"). Since 2026-10-31 it is rendered in Blender on the analog panel
// bible (owner: "I love the Remnant ui, i htink it could use some polish from our guide, but please dont remove the
// grimey industrial feel"): monumental corroded iron with worn rolled edges, rust, verdigris, pitting and soot in every
// crevice, great rivets with rust running down from them, worn gold bezels and brackets, arched-tablet keys and lintel
// tabs whose grimy glass carries the same caked dirt in every state (only the backlight changes), cracked gauge glass,
// lit by old lamps under the lips that do not all still burn (tools/mockups/tarsine_shapes.py,
// tools/blender/build_ui_tarsine.py, assets/ui/tarsine/). This module loads that kit and draws the live readouts, the
// carved glyph icons and the view's brackets. Registers itself with the page's skin registry (docs/mockups/skins).
(function () {
'use strict';
const F = window.FCK, P = F.P;
const TA = { soot: '#1c1814', iron: '#3a332c', ironHi: '#6a5e50', rust: '#7a3b1c', rustHi: '#b0602e', verd: '#4f6b5a', gold: '#a88a46', goldHi: '#d8bc6e',
  light: '#d6c76a', ember: '#d2482a', bone: '#e8dcc0', dim: '#a8987a', glass: '#0e0c0a' };
function hexA(hex, a) { const n = parseInt(hex.slice(1), 16); return `rgba(${n >> 16},${(n >> 8) & 255},${n & 255},${a})`; }
const RNG = (k) => F.rng(F.hash('tarsine.' + k));

// ---- materials ---------------------------------------------------------------------------------------------------
// Corroded iron: dark plate, rust blooms, pitting, grime pooled toward the bottom, a worn bright edge broken by rust.
function iron(x, draw, box, R, o = {}) {
  const [x0, y0, x1, y1] = box, w = x1 - x0, h = y1 - y0;
  x.save(); draw(); x.clip();
  const g = x.createLinearGradient(x0, y0, x0, y1); g.addColorStop(0, o.top || '#4a4038'); g.addColorStop(1, o.bottom || '#241e1a'); x.fillStyle = g; x.fillRect(x0, y0, w, h);
  const n = Math.max(3, Math.round(w * h / (o.rustEvery || 900)));
  for (let i = 0; i < n; i++) {
    const cx = x0 + R() * w, cy = y0 + R() * h, r = 3 + R() * 12, b = x.createRadialGradient(cx, cy, 0, cx, cy, r);
    b.addColorStop(0, hexA(R() < 0.7 ? TA.rust : TA.verd, 0.5 + R() * 0.3)); b.addColorStop(0.6, hexA(TA.rust, 0.18)); b.addColorStop(1, 'rgba(0,0,0,0)'); x.fillStyle = b; x.fillRect(cx - r, cy - r, 2 * r, 2 * r);
  }
  for (let i = 0; i < w * h / 70; i++) { const px = x0 + R() * w, py = y0 + R() * h; x.fillStyle = 'rgba(10,8,6,0.45)'; x.fillRect(px, py, 1, 1); x.fillStyle = 'rgba(190,150,110,0.18)'; x.fillRect(px + 0.6, py + 0.6, 0.6, 0.6); }
  const gr = x.createLinearGradient(x0, y0 + h * 0.55, x0, y1); gr.addColorStop(0, 'rgba(0,0,0,0)'); gr.addColorStop(1, 'rgba(12,9,6,0.45)'); x.fillStyle = gr; x.fillRect(x0, y0, w, h);
  x.restore();
  // the worn edge: bright where hands and time rubbed it, broken where rust ate it
  x.save(); draw(); x.clip(); x.translate(1, 1); draw(); x.strokeStyle = 'rgba(200,180,150,0.35)'; x.setLineDash([6 + R() * 10, 2 + R() * 4]); x.lineWidth = 1.4; x.stroke(); x.restore();
  draw(); x.strokeStyle = '#0c0907'; x.lineWidth = 1.4; x.stroke();
}
// A rust streak running down from a point (a rivet, a seam): rain on old metal.
function streak(x, px, py, len, R) {
  const g = x.createLinearGradient(px, py, px, py + len); g.addColorStop(0, hexA(TA.rustHi, 0.55)); g.addColorStop(1, 'rgba(0,0,0,0)');
  x.strokeStyle = g; x.lineWidth = 1.6 + R() * 1.6; x.beginPath(); x.moveTo(px, py); x.quadraticCurveTo(px + (R() - 0.5) * 3, py + len / 2, px + (R() - 0.5) * 4, py + len); x.stroke();
}
function rivet(x, px, py, r, R) {
  streak(x, px, py + r, 8 + R() * 10, R);
  const g = x.createRadialGradient(px - r * 0.4, py - r * 0.4, 0.5, px, py, r); g.addColorStop(0, '#9a8a74'); g.addColorStop(0.5, '#5a4a3a'); g.addColorStop(1, '#1c1510');
  x.fillStyle = g; x.beginPath(); x.arc(px, py, r, 0, 2 * P); x.fill(); x.strokeStyle = '#0c0907'; x.lineWidth = 0.8; x.stroke();
}
// ---- icons: worn carved glyphs ------------------------------------------------------------------------------------
function icon(name) {
  return F.paint(48, 48, (x) => {
    const R = RNG('icon.' + name);
    const S = (pts) => {
      for (const [col, dx, dy, lw] of [['rgba(0,0,0,0.7)', 0, 0, 4.4], [hexA(TA.goldHi, 0.85), 0, 0, 2.6], ['rgba(255,240,200,0.35)', -0.6, -0.6, 1]]) {
        x.strokeStyle = col; x.lineWidth = lw; x.lineCap = 'square'; x.beginPath(); x.moveTo(pts[0][0] + dx, pts[0][1] + dy); for (const p of pts.slice(1)) x.lineTo(p[0] + dx, p[1] + dy); x.stroke();
      }
    };
    ({
      helm: () => { S([[24, 42], [24, 6]]); S([[12, 18], [24, 6], [36, 18]]); S([[16, 30], [32, 30]]); },
      fire: () => { S([[8, 40], [24, 8], [40, 40]]); S([[24, 8], [24, 40]]); },
      shields: () => { S([[8, 8], [40, 8], [40, 26], [24, 42], [8, 26], [8, 8]]); S([[24, 8], [24, 42]]); },
      tractor: () => { S([[8, 8], [8, 40]]); S([[40, 8], [40, 40]]); S([[8, 24], [40, 24]]); S([[16, 16], [24, 24], [16, 32]]); },
      transport: () => { S([[24, 6], [24, 42]]); S([[10, 14], [24, 24], [38, 14]]); S([[10, 34], [24, 24], [38, 34]]); },
      science: () => { S([[24, 6], [40, 24], [24, 42], [8, 24], [24, 6]]); S([[24, 18], [24, 30]]); },
      comms: () => { S([[10, 42], [10, 6]]); S([[10, 10], [30, 18], [10, 26]]); S([[30, 30], [40, 26]]); S([[30, 38], [42, 38]]); },
      repair: () => { S([[8, 40], [36, 12]]); S([[30, 6], [42, 18]]); S([[14, 30], [20, 36]]); },
      map: () => { S([[6, 12], [18, 8], [30, 12], [42, 8]]); S([[6, 12], [6, 40], [18, 36], [30, 40], [42, 36], [42, 8]]); S([[18, 8], [18, 36]]); S([[30, 12], [30, 40]]); },
      settings: () => { S([[24, 6], [24, 42]]); S([[6, 24], [42, 24]]); S([[12, 12], [36, 36]]); S([[36, 12], [12, 36]]); },
    })[name]();
    // time wore parts of it away
    x.globalCompositeOperation = 'destination-out'; for (let i = 0; i < 5; i++) { x.fillStyle = `rgba(0,0,0,${0.4 + R() * 0.5})`; x.beginPath(); x.arc(R() * 48, R() * 48, 2 + R() * 4, 0, 2 * P); x.fill(); }
    x.globalCompositeOperation = 'source-over';
  });
}

// ---- the skin --------------------------------------------------------------------------------------------------
F.register('tarsine', {
  name: 'Tarsine Remnant', kind: 'Corroded monumental iron rendered in Blender; backlights clogged with dirt', bg: '#110e0b',
  css: { '--s-ink': TA.bone, '--s-dim': TA.dim, '--s-num': TA.light, '--s-body': '"Alegreya Sans SC", sans-serif', '--btn-ink': '#1e160c', '--btn-shadow': '0 1px 0 rgba(255,230,160,0.35)', '--btn-justify': 'center',
    '--btn-pt': '0px', '--btn-pb': '3px', '--btn-font': '700 16px/1 "Cinzel", serif', '--btn-track': '0.08em', '--btn-case': 'uppercase', '--btn-motion': 'padding-top 0.06s', '--btn-sink': '1.5px',
    '--tab-font': '700 12px/1 "Cinzel", serif', '--tab-ink': TA.dim, '--tab-ink-on': '#1e160c', '--title-font': '700 17px/1 "Cinzel", serif', '--title-ink': TA.goldHi,
    '--lbl-font': '700 16px/1 "Alegreya Sans SC", sans-serif', '--num-font': '700 24px/1 "Alegreya Sans SC", sans-serif',
    // The strips are 72 px; their open band is the beam's channel of grimy glass, rows 13-59 inside its worn rolled lip
    // (tools/mockups/tarsine_shapes.py), so each label stands over its bar with 3 px clear of the art.
    '--stt-pt': '13px', '--stt-pb': '13px', '--stb-pt': '13px', '--stb-pb': '13px', '--strip-lamp': '40px' },
  accent: TA.light, warn: TA.ember,
  // The title sits in the frame's well, right of the top-left capital, under the well's worn lip.
  title: (t, m) => { t.style.cssText = `left:${38 - m[3]}px;top:${19 - m[0]}px;letter-spacing:0.12em;text-shadow:0 1px 0 #000, 0 0 3px #000;`; },
  plateText: { left: 44, top: 13, width: 372, height: 30, font: '700 19px/1 "Cinzel", serif', color: '#f2e6c4',
    // pale carved letters outlined dark: they read over the lit gold and the grime stains alike (the dirt stays)
    shadow: '0 0 2px #0c0703, 0 0 2px #0c0703, 0 1px 0 #0c0703', sub: 'ANKHAR · ELDEST VOR' },
  helmKeys: [[150, 63], [236, 63], [236, 149], [236, 235], [150, 235], [64, 235], [64, 149], [64, 63]], shield: [TA.light, '#c9873a', TA.ember],
  fonts: ['700 20px "Cinzel"', '700 20px "Alegreya Sans SC"'], view: { title: '700 26px "Cinzel"', body: '700 24px "Alegreya Sans SC"', upper: true },
  build: () => { const d = F.kitData('assets/ui/tarsine/manifest.json'); return d ? F.fromManifest(d, icon) : null; },
  live: {
    // A carved arrow, dark in the lit tablet.
    arrow: (x) => { x.fillStyle = '#1e160c'; x.beginPath(); x.moveTo(12, 0); x.lineTo(0, -10); x.lineTo(0, -4); x.lineTo(-11, -4); x.lineTo(-11, 4); x.lineTo(0, 4); x.lineTo(0, 10); x.closePath(); x.fill(); },
    helmCentre: (x, c) => { x.fillStyle = '#1e160c'; x.font = '700 18px Cinzel'; x.textAlign = 'center'; x.fillText('HALT', c, c + 6); },
    // A long scarred dreadnought in worn gold lines; shields as dim broken arcs.
    ssd: (x, cx, cy, sh, col) => {
      for (let i = 0; i < 6; i++) { const a = -P / 2 + i * P / 3; x.strokeStyle = col(sh[i]); x.lineWidth = 6; x.setLineDash([10, 4, 3, 4]); x.beginPath(); x.arc(cx, cy, 100, a - P / 6 + 0.08, a + P / 6 - 0.08); x.stroke(); }
      x.setLineDash([]); x.strokeStyle = TA.goldHi; x.lineWidth = 2.2;
      x.beginPath(); x.moveTo(cx - 12, cy - 78); x.lineTo(cx + 12, cy - 78); x.lineTo(cx + 22, cy - 50); x.lineTo(cx + 22, cy - 10); x.lineTo(cx + 40, cy); x.lineTo(cx + 40, cy + 40); x.lineTo(cx + 22, cy + 50); x.lineTo(cx + 18, cy + 76); x.lineTo(cx - 18, cy + 76); x.lineTo(cx - 22, cy + 50); x.lineTo(cx - 40, cy + 40); x.lineTo(cx - 40, cy); x.lineTo(cx - 22, cy - 10); x.lineTo(cx - 22, cy - 50); x.closePath(); x.stroke();
      for (const y of [-40, -10, 22, 50]) { x.beginPath(); x.moveTo(cx - 20, cy + y); x.lineTo(cx + 20, cy + y); x.stroke(); }
      x.strokeStyle = 'rgba(210,72,42,0.7)'; x.lineWidth = 1.2; x.beginPath(); x.moveTo(cx + 26, cy + 4); x.lineTo(cx + 10, cy + 18); x.lineTo(cx + 16, cy + 30); x.stroke();
      x.fillStyle = TA.ember; for (const [mx, my] of [[cx, cy - 64], [cx - 32, cy + 20], [cx + 32, cy + 20], [cx, cy + 64]]) { x.fillRect(mx - 4, my - 4, 8, 8); }
    },
    // An old needle behind cracked glass, contacts as ember specks.
    dial: (x, c, cts) => {
      x.strokeStyle = TA.ember; x.lineWidth = 2.4; x.beginPath(); x.moveTo(c, c); x.lineTo(c + Math.cos(-0.9) * 34, c + Math.sin(-0.9) * 34); x.stroke();
      x.fillStyle = '#2a221a'; x.beginPath(); x.arc(c, c, 5, 0, 2 * P); x.fill();
      for (const [dx, dy] of cts) { x.fillStyle = hexA(TA.ember, 0.85); x.fillRect(c + dx * 30 - 3, c + dy * 30 - 3, 6, 6); }
    },
    // Heavy iron corner brackets with a rivet each.
    bracket: (x, bx, by, bw, bh) => {
      const R = RNG('bracket');
      for (const [px, py, sx, sy] of [[bx, by, 1, 1], [bx + bw, by, -1, 1], [bx, by + bh, 1, -1], [bx + bw, by + bh, -1, -1]]) {
        x.save(); x.translate(px, py); x.scale(sx, sy);
        iron(x, () => { x.beginPath(); x.moveTo(0, 0); x.lineTo(46, 0); x.lineTo(46, 10); x.lineTo(10, 10); x.lineTo(10, 46); x.lineTo(0, 46); x.closePath(); }, [0, 0, 46, 46], R, { top: '#6a5a48' });
        rivet(x, 5, 5, 3, R); x.restore();
      }
    },
  },
  world: ['A fallen empire that remembers being first, and will not change.', 'Huge, slow and long-lived: the crews outlast their worlds.', 'Machines older than their crews, patched and never replaced.', 'Massive, scarred, rusted dreadnoughts.'],
  shapes: 'Arched tablets, stepped frames with capitals, lintels, heavy brackets: monuments.', light: 'Old lamps under the lips, not all still burning; backlight through caked dirt; worn rolled edges on rusted iron.',
  motion: 'Almost none: a key gives a millimetre and the light behind it steadies.', type: '"Cinzel" carved capitals over "Alegreya Sans SC".',
  palette: [['Soot', TA.soot, 'Grime, shadow', 'Ages of use'], ['Rust', TA.rust, 'Frames', 'Decline they will not admit'], ['Verdigris', TA.verd, 'Corrosion', 'Time'],
    ['Old gold', TA.gold, 'Inlays, glyphs', 'Old majesty'], ['Backlight', TA.light, 'Keys, readouts', 'The fire still burning'], ['Ember', TA.ember, 'Alerts', 'Anger, slow to wake']],
});
})();
