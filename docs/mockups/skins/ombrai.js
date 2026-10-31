// The Ombrai skin (mockup 4, OpenSpec change console-skins): an enigmatic race (owner, 2026-10-30: "an enigmatic
// alien race with cool dark purple colors and pink buttons, progress bars are curcular, almost organic like eyes"),
// rebuilt on the analog panel bible (owner, 2026-10-31: "we need to update krai, onbrai, and hunt to use the new
// guildine standards ... Please make them look nice"). Every kit piece is modelled and rendered in Blender
// (tools/blender/build_ui_ombrai.py, from the fit-checked shapes of tools/mockups/ombrai_shapes.py): pillowed
// anodised plum metal with a thin-film sheen, lilac bezels, pink eye jewels on flat metal, and formed wells in the
// Ombrai's own shapes (crescent-bitten keys, petal wells round the helm's pupil, frame wells bitten like a waning moon,
// an eye-shaped ship display under two lids), each lit from below, so the top of every well stays veiled. Bars are rows
// of eyes that open as they fill. This module draws the live readouts (the comets on the helm, the ship and its
// shield rings, the iris radar, the target's crescents) in the same hand, and the icons. Registers itself with the
// page's skin registry (docs/mockups/skins).
(function () {
'use strict';
const F = window.FCK, P = F.P;
const OM = { void: '#120822', deep: '#3b1763', plum: '#3b2a5c', smoke: '#241a33', pink: '#ff4fb0', lilac: '#c58cff', crimson: '#ff2f5a', pale: '#f4e6ff', dim: '#b9a0d6' };
function hexA(hex, a) { const n = parseInt(hex.slice(1), 16); return `rgba(${n >> 16},${(n >> 8) & 255},${n & 255},${a})`; }

// ---- drawing helpers for the live readouts ----------------------------------------------------------------------
// Smoked violet glass: deep and dark, a faint iridescent sheen across it, a soft highlight along the top.
function glass(x, draw, box, o = {}) {
  const [x0, y0, x1, y1] = box;
  x.save(); draw(); x.clip();
  const g = x.createLinearGradient(x0, y0, x0, y1); g.addColorStop(0, o.top || '#3a2456'); g.addColorStop(0.5, OM.smoke); g.addColorStop(1, OM.void); x.fillStyle = g; x.fillRect(x0, y0, x1 - x0, y1 - y0);
  const s = x.createLinearGradient(x0, y1, x1, y0); s.addColorStop(0.35, 'rgba(255,79,176,0)'); s.addColorStop(0.5, 'rgba(255,79,176,0.10)'); s.addColorStop(0.58, 'rgba(120,200,255,0.08)'); s.addColorStop(0.7, 'rgba(197,140,255,0)');
  x.fillStyle = s; x.fillRect(x0, y0, x1 - x0, y1 - y0);
  x.restore();
  x.save(); draw(); x.clip(); x.translate(0, 1.2); draw(); x.strokeStyle = 'rgba(244,230,255,0.22)'; x.lineWidth = 1.4; x.stroke(); x.restore();
  draw(); x.strokeStyle = '#07030d'; x.lineWidth = 1; x.stroke();
}
// A soft glowing line.
function glow(x, draw, col = OM.pink, w = 1.4, blur = 6, a = 0.9) { x.save(); x.shadowColor = col; x.shadowBlur = blur; draw(); x.strokeStyle = hexA(col, a); x.lineWidth = w; x.stroke(); x.restore(); }
// An eye: rings round a slit pupil, lit pink.
function eye(x, cx, cy, r, open = 1) {
  x.fillStyle = OM.void; x.beginPath(); x.arc(cx, cy, r, 0, 2 * P); x.fill();
  glow(x, () => { x.beginPath(); x.arc(cx, cy, r, 0, 2 * P); }, OM.lilac, 1, 4, 0.7);
  glow(x, () => { x.beginPath(); x.arc(cx, cy, r * 0.62, 0, 2 * P); }, OM.pink, 1.2, 5, 0.8);
  x.save(); x.shadowColor = OM.pink; x.shadowBlur = 8; x.fillStyle = OM.pink; x.beginPath(); x.ellipse(cx, cy, r * 0.14 * open + 0.6, r * 0.5, 0, 0, 2 * P); x.fill(); x.restore();
}

// ---- icons: eyes, moons, rings, petals, dots ---------------------------------------------------------------------
function icon(name) {
  return F.paint(48, 48, (x) => {
    x.strokeStyle = OM.pink; x.fillStyle = OM.pink; x.lineWidth = 2; x.shadowColor = OM.pink; x.shadowBlur = 5;
    const C = (cx, cy, r) => { x.beginPath(); x.arc(cx, cy, r, 0, 2 * P); x.stroke(); };
    const D = (cx, cy, r) => { x.beginPath(); x.arc(cx, cy, r, 0, 2 * P); x.fill(); };
    const EYE = (cx, cy, rx, ry) => { x.beginPath(); x.moveTo(cx - rx, cy); x.quadraticCurveTo(cx, cy - ry * 2, cx + rx, cy); x.quadraticCurveTo(cx, cy + ry * 2, cx - rx, cy); x.stroke(); x.beginPath(); x.ellipse(cx, cy, 2, ry * 0.8, 0, 0, 2 * P); x.fill(); };
    const MOON = (cx, cy, r) => { x.beginPath(); x.arc(cx, cy, r, 0.3 * P, 1.7 * P); x.arc(cx + r * 0.45, cy, r * 0.8, 1.62 * P, 0.38 * P, true); x.closePath(); x.fill(); };
    ({
      helm: () => { C(24, 24, 18); x.beginPath(); x.ellipse(24, 14, 3, 8, 0, 0, 2 * P); x.fill(); },
      fire: () => { EYE(24, 24, 20, 9); C(24, 24, 4); },
      shields: () => { x.beginPath(); x.arc(24, 30, 18, P * 1.1, P * 1.9); x.stroke(); x.beginPath(); x.arc(24, 30, 11, P * 1.15, P * 1.85); x.stroke(); D(24, 30, 3); },
      tractor: () => { MOON(14, 24, 10); x.save(); x.translate(48, 0); x.scale(-1, 1); MOON(14, 24, 10); x.restore(); },
      transport: () => { for (let i = 0; i < 5; i++) D(24 + Math.cos(i * 1.3) * (4 + i * 3.4), 24 + Math.sin(i * 1.3) * (4 + i * 3.4), 1.8 + i * 0.4); },
      science: () => { EYE(24, 24, 18, 8); C(24, 24, 8); x.beginPath(); x.moveTo(24, 4); x.lineTo(24, 10); x.moveTo(24, 38); x.lineTo(24, 44); x.stroke(); },
      comms: () => { D(12, 24, 3.5); for (const r of [10, 18, 26]) { x.beginPath(); x.arc(12, 24, r, -0.7, 0.7); x.stroke(); } },
      repair: () => { for (let i = 0; i < 4; i++) { x.save(); x.translate(24, 24); x.rotate(i * P / 2); x.beginPath(); x.moveTo(0, 0); x.quadraticCurveTo(10, -8, 16, 0); x.quadraticCurveTo(10, 8, 0, 0); x.stroke(); x.restore(); } D(24, 24, 3); },
      map: () => { C(24, 24, 18); C(24, 24, 9); D(32, 14, 2.6); D(14, 30, 2.6); D(30, 34, 2); },
      settings: () => { MOON(24, 24, 18); },
    })[name]();
  });
}

// ---- the skin --------------------------------------------------------------------------------------------------
F.register('ombrai', {
  name: 'Ombrai', kind: 'Plum metal over violet wells lit from below; every gauge a watching eye', bg: '#0b0516',
  css: { '--s-ink': OM.pale, '--s-dim': OM.dim, '--s-num': OM.pink, '--s-body': '"Quicksand", sans-serif', '--btn-ink': OM.pale, '--btn-shadow': `0 0 6px ${OM.pink}`, '--btn-justify': 'center',
    '--btn-font': '700 13px/1 "Syncopate", sans-serif', '--btn-track': '0.14em', '--btn-case': 'uppercase', '--btn-motion': 'filter 0.4s, padding-top 0.06s', '--btn-sink': '1.5px',
    '--tab-font': '700 10px/1 "Syncopate", sans-serif', '--tab-ink': OM.dim, '--tab-ink-on': OM.pale, '--title-font': '700 14px/1 "Syncopate", sans-serif', '--title-ink': OM.lilac,
    '--lbl-font': '700 15px/1 "Quicksand", sans-serif', '--num-font': '500 26px/1 "Quicksand", sans-serif',
    // The strips are 72 px (owner, 2026-10-31: "we need to make parent taller"); their open band is the channel, rows 11-60,
    // measured by tools/mockups/console_fit.mjs, so each label sits over its bar with 3 px clear of the art.
    '--stt-pt': '11px', '--stt-pb': '11px', '--stb-pt': '11px', '--stb-pb': '11px', '--strip-lamp': '40px',
    // the well's bitten end glows 5-10 px past the 76 px slice margin: the strip's readouts start clear of it
    '--st-l': '88px', '--st-r': '88px' },
  accent: OM.pink, warn: OM.crimson,
  // The title sits in the frame's well, under its top lip and right of the bitten corner.
  title: (t, m) => { t.style.cssText = `left:${34 - m[3]}px;top:${24 - m[0]}px;letter-spacing:0.22em;text-shadow:0 0 8px ${OM.pink};`; },
  plateText: { left: 78, top: 12, width: 352, height: 32, font: '700 13px/1 "Syncopate", sans-serif', color: OM.pale, sub: 'VEIL OF ISH · THE WATCHER' },
  helmKeys: 'ring', helmR: 98, shield: [OM.pink, '#ffd36a', OM.crimson],
  fonts: ['700 20px "Syncopate"', '700 20px "Quicksand"', '500 20px "Quicksand"'], view: { title: '700 20px "Syncopate"', body: '500 26px "Quicksand"', upper: true },
  build: () => { const d = F.kitData('assets/ui/ombrai/manifest.json'); return d ? F.fromManifest(d, icon) : null; },
  live: {
    // A comet: a small moon trailing light.
    arrow: (x) => { x.shadowColor = OM.pink; x.shadowBlur = 8; const g = x.createLinearGradient(-14, 0, 8, 0); g.addColorStop(0, 'rgba(255,79,176,0)'); g.addColorStop(1, OM.pale); x.fillStyle = g; x.beginPath(); x.moveTo(-14, -2); x.lineTo(6, -5); x.arc(6, 0, 5, -P / 2, P / 2); x.lineTo(-14, 2); x.closePath(); x.fill(); },
    // A petal hull with an eye at its heart; each shield is a ring arc that fills like an iris.
    ssd: (x, cx, cy, sh, col) => {
      for (let i = 0; i < 6; i++) {
        const a0 = -P / 2 + i * P / 3 - P / 6 + 0.1, a1 = a0 + P / 3 - 0.2;
        x.strokeStyle = hexA(OM.lilac, 0.25); x.lineWidth = 8; x.beginPath(); x.arc(cx, cy, 100, a0, a1); x.stroke();
        glow(x, () => { x.beginPath(); x.arc(cx, cy, 100, a0, a0 + (a1 - a0) * sh[i]); }, col(sh[i]), 8, 8, 0.9);
      }
      const petal = (s) => () => { x.beginPath(); x.moveTo(cx, cy - 74); x.quadraticCurveTo(cx + s * 70, cy - 10, cx, cy + 70); x.quadraticCurveTo(cx + s * 24, cy, cx, cy - 74); x.closePath(); };
      for (const s of [1, -1]) { glass(x, petal(s), [cx - 70, cy - 74, cx + 70, cy + 70], { top: '#5a3a7a' }); glow(x, petal(s), OM.pink, 1, 5, 0.7); }
      eye(x, cx, cy, 18);
      for (const [mx, my] of [[cx, cy - 56], [cx - 30, cy + 22], [cx + 30, cy + 22]]) { x.save(); x.shadowColor = OM.pink; x.shadowBlur = 8; x.fillStyle = OM.pale; x.beginPath(); x.arc(mx, my, 3.5, 0, 2 * P); x.fill(); x.restore(); }
    },
    // An iris: three rings filling clockwise, a slit pupil, contacts as pink motes.
    dial: (x, c, cts) => {
      for (const [r, f] of [[42, 0.82], [34, 0.55], [26, 0.3]]) { x.strokeStyle = hexA(OM.lilac, 0.25); x.lineWidth = 4; x.beginPath(); x.arc(c, c, r, 0, 2 * P); x.stroke(); glow(x, () => { x.beginPath(); x.arc(c, c, r, -P / 2, -P / 2 + 2 * P * f); }, OM.pink, 4, 6, 0.9); }
      x.save(); x.shadowColor = OM.pink; x.shadowBlur = 8; x.fillStyle = OM.pale; x.beginPath(); x.ellipse(c, c, 2.4, 12, 0, 0, 2 * P); x.fill(); x.restore();
      for (const [dx, dy] of cts) { x.save(); x.shadowColor = OM.crimson; x.shadowBlur = 6; x.fillStyle = OM.crimson; x.beginPath(); x.arc(c + dx * 44, c + dy * 44, 3, 0, 2 * P); x.fill(); x.restore(); }
    },
    // Four crescents curling round the target's corners.
    bracket: (x, bx, by, bw, bh) => {
      for (const [px, py, sx, sy] of [[bx, by, 1, 1], [bx + bw, by, -1, 1], [bx, by + bh, 1, -1], [bx + bw, by + bh, -1, -1]]) {
        x.save(); x.translate(px, py); x.scale(sx, sy); glow(x, () => { x.beginPath(); x.arc(30, 30, 30, P, 1.5 * P); }, OM.pink, 3, 10, 0.95);
        x.fillStyle = OM.pale; x.beginPath(); x.arc(4, 4, 3, 0, 2 * P); x.fill(); x.restore();
      }
    },
  },
  world: ['Hidden purpose: they watch, and are seldom seen.', 'Shadowy and many-eyed; their glances do the work of hands.', 'Technology that looks back at you: every instrument an eye.', 'Dark, curved, petal-like hulls with glowing eyes.'],
  shapes: 'Crescents, petals, moons, rings and eyes; nothing straight if a curve will do.', light: 'Lit from below: pink-violet light rising from the foot of each well, its top veiled; a thin-film sheen on the plum metal.',
  motion: 'Light swells slowly, like an eye opening.', type: '"Syncopate" wide capitals over "Quicksand".',
  palette: [['Void', OM.void, 'Wells', 'Secrecy'], ['Plum', OM.plum, 'Metal', 'The dream they live in'], ['Smoke', OM.smoke, 'Shadows in the wells', 'Veils'],
    ['Pink', OM.pink, 'Keys, readouts', 'Attention: the watching gaze'], ['Lilac', OM.lilac, 'Rings, titles', 'Memory'], ['Crimson', OM.crimson, 'Alerts', 'A glance that turns hostile']],
});
})();
