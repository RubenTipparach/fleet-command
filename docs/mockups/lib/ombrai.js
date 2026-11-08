// Fleet Command mockups: the Ombrai skin (openspec/changes/console-skins/design.md, "Ombrai";
// openspec/changes/ombrai-battle-console), shared by the console skins page (docs/mockups/skins/ombrai.js registers it
// there) and the battle console, where it is the skin of every Ombrai ship you command (the console follows the ship you
// are on; openspec/changes/fleet-skins). One implementation of the Ombrai's look (CLAUDE.md section 6.1): its palette, its
// icons, its live readouts and, for the battle console, its kit: anodised plum metal modelled and rendered in Blender
// (assets/ui/ombrai/manifest.json, tools/blender/build_ui_ombrai.py from the fit-checked shapes of
// tools/mockups/ombrai_shapes.py), with the light in the console's own keys and cells laid into those renders' wells.
// It exposes the same fields as docs/mockups/lib/concord.js and lib/kelvane.js, so the battle console swaps one for
// another and changes no rule and no layout. Needs docs/mockups/lib/kitdraw.js inlined first. Our own design: no other
// game's or film's names, shapes, glyphs or art.
//
// Where it comes from (design.md; owner, 2026-10-30: "an enigmatic alien race with cool dark purple colors and pink
// buttons, progress bars are curcular, almost organic like eyes"). Its tradition is hidden purpose and watching: they
// watch, and are seldom seen, so their instruments are veiled from above and lit from below, and every gauge is an eye
// that looks back. The palette: void (secrecy), plum (the dream they live in), smoke (veils), pink (attention: the
// watching gaze), lilac (memory), crimson (a glance that turns hostile). Nothing here comes from what the Ombrai look
// like (CLAUDE.md section 1): eyes, moons and veils are what their culture watches with, not their bodies.
window.FCOmbrai = (function () {
'use strict';
const { paint, P } = window.FCDraw;
const OM = { void: '#120822', deep: '#3b1763', plum: '#3b2a5c', smoke: '#241a33', pink: '#ff4fb0', lilac: '#c58cff', crimson: '#ff2f5a', pale: '#f4e6ff', dim: '#b9a0d6',
  moon: '#9ff2e0', amber: '#ffd36a', bg: '#0b0516' };
function hexA(hex, a) { const n = parseInt(hex.slice(1), 16); return `rgba(${n >> 16},${(n >> 8) & 255},${n & 255},${a})`; }
// Relative luminance of a #rrggbb colour (WCAG), for picking the ink that reads on a fill.
function lum(hex) {
  if (typeof hex !== 'string' || hex[0] !== '#' || hex.length < 7) return 0;
  const c = [1, 3, 5].map((i) => parseInt(hex.slice(i, i + 2), 16) / 255).map((v) => (v <= 0.03928 ? v / 12.92 : ((v + 0.055) / 1.055) ** 2.4));
  return 0.2126 * c[0] + 0.7152 * c[1] + 0.0722 * c[2];
}
function path(x, pts) { x.beginPath(); x.moveTo(pts[0][0], pts[0][1]); for (const p of pts.slice(1)) x.lineTo(p[0], p[1]); x.closePath(); }
const bounds = (pts) => { const xs = pts.map((p) => p[0]), ys = pts.map((p) => p[1]); return [Math.min(...xs), Math.min(...ys), Math.max(...xs), Math.max(...ys)]; };

// ---- Drawing helpers for the live readouts ----------------------------------------------------------------------
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
// Light in a well, the Ombrai's way: risen from its foot, its top veiled (design.md: "Lit from below"). level 0 is the
// well dark, 1 a lit key; a level of null is light made nearly solid, for a key that is on or shows a state, so dark
// ink reads on it.
function lightBelow(x, draw, box, level, col) {
  const [x0, y0, x1, y1] = box, w = x1 - x0, h = y1 - y0;
  x.save(); draw(); x.clip();
  x.fillStyle = hexA(OM.void, 0.82); x.fillRect(x0, y0, w, h);
  if (level == null) {
    const g = x.createLinearGradient(0, y0, 0, y1); g.addColorStop(0, hexA(col, 0.72)); g.addColorStop(0.45, hexA(col, 0.92)); g.addColorStop(1, hexA(col, 1));
    x.fillStyle = g; x.fillRect(x0, y0, w, h);
    const r = x.createRadialGradient(x0 + w / 2, y1, 0, x0 + w / 2, y1, Math.max(w, h) * 0.7); r.addColorStop(0, 'rgba(255,255,255,0.45)'); r.addColorStop(1, 'rgba(255,255,255,0)');
    x.fillStyle = r; x.fillRect(x0, y0, w, h);
  } else if (level > 0) {
    const r = x.createRadialGradient(x0 + w / 2, y1 + h * 0.15, 0, x0 + w / 2, y1 + h * 0.15, Math.max(w, h) * 0.85);
    r.addColorStop(0, hexA(col, 0.25 + level * 0.6)); r.addColorStop(0.6, hexA(col, level * 0.22)); r.addColorStop(1, hexA(col, 0));
    x.fillStyle = r; x.fillRect(x0, y0, w, h);
  }
  // the veil over the top of the well
  const v = x.createLinearGradient(0, y0, 0, y0 + h * 0.5); v.addColorStop(0, hexA(OM.void, level == null ? 0.25 : 0.55)); v.addColorStop(1, hexA(OM.void, 0));
  x.fillStyle = v; x.fillRect(x0, y0, w, h);
  x.restore();
  glow(x, draw, level == null ? col : OM.lilac, 1, level == null ? 6 : 3, level == null ? 0.9 : 0.35);
}
// The console's veil over a panel's well, a strip's channel or the name plate's field: the well darkened most at its top
// and least at its foot, where its light rises, so pink letters read on it (pink on the bare lit plum measured about 2.5
// to 1; on the veil 6 to 1). No lines: a frame is stretched many times its height on the console.
function veil(x, pts, a = 0.72) {
  const [x0, y0, x1, y1] = bounds(pts);
  x.save(); path(x, pts); x.clip();
  const g = x.createLinearGradient(0, y0, 0, y1); g.addColorStop(0, hexA(OM.void, a)); g.addColorStop(0.7, hexA(OM.void, a * 0.85)); g.addColorStop(1, hexA(OM.void, a * 0.55));
  x.fillStyle = g; x.fillRect(x0, y0, x1 - x0, y1 - y0);
  x.restore();
}

// ---- The battle console's colour roles -------------------------------------------------------------------------
// The same role names as the Concord's (lib/concord.js), so the page's rules pick a role, never a colour. The Ombrai:
// pink for a reading and a hover (Sky; the watching gaze), a soft lilac print for labels (Lilac; memory), pale for prose,
// lilac titles, a pale moon for whole and engaged (Accord: an eye wide open in calm), amber for damaged, crimson only for
// down and alarms (a glance that turns hostile); the dark roles are the void's and the plum's.
const CC = {
  deep: '#2a1245', signal: '#3a1a5c', sky: OM.pink, accord: OM.moon, lilac: OM.dim, caution: OM.amber, alert: OM.crimson, text: OM.pale,
  ink: '#12061f', inkDis: '#4a3a60', night: '#140a24', ghost: '#5a4a72', void: '#0c0518', slate: '#2e2048', mute: '#221836', ver: '#9a86b8',
  char: '#3a0a18', plate: '#160b26', bg: OM.bg, title: OM.lilac,
};
// The ink that reads on a fill: dark on lit light, pale on the dark void.
const ink = (fill) => (lum(fill) > 0.2 ? CC.ink : OM.pale);
// Ink on the keys and tabs: pale on every rendered key (the skins page's --btn-ink: its well glows behind the name, dim
// at rest, hot when pressed); a tab not chosen in the dim lilac, the chosen one pale on its lit well.
const keyInk = { idle: OM.pale, hover: '#ffffff', pressed: '#ffffff', tabOn: OM.pale, tabOff: OM.dim };
// Type: "Quicksand" for every readout and label, "Syncopate" wide capitals for titles (the skins page's pair). The page
// asks for Antonio sizes; Quicksand at weight 700 runs about 1.35 times as wide and Syncopate about 2 (measured on the
// console's labels), so they are set smaller and rule T shrinks what still runs long.
function font(spec) {
  const m = /^(\d+) ([\d.]+)px Antonio$/.exec(spec); if (!m) return spec;
  const px = +m[2];
  if (m[1] === '700' && px >= 20) return `700 ${Math.round(px * 0.58 * 10) / 10}px Syncopate`;
  return `700 ${Math.round(px * 0.76 * 10) / 10}px Quicksand`;
}
const fonts = ['700 20px Syncopate', '700 20px Quicksand', '500 20px Quicksand'];

// ---- Icons: one subject list for every skin (docs/design/icon-guide.html) ---------------------------------------
// Eyes, moons, rings, petals and dots: nothing straight if a curve will do. Even strokes with a soft glow; a solid where
// the thing itself is solid (a hull, a core, a figure), rings and dots for what is given off. One ink; on a 48 x 48 grid
// with 4 px clear, read at 20 px and 48 px.
function icon(name, col = OM.pink) {
  return paint(48, 48, (x) => {
    const bright = lum(col) > 0.2;
    x.strokeStyle = col; x.fillStyle = col; x.lineWidth = 2.6; x.lineJoin = 'round'; x.lineCap = 'round';
    if (bright) { x.shadowColor = hexA(col, 0.6); x.shadowBlur = 4; }
    const C = (cx, cy, r, lw = 2.6) => { x.lineWidth = lw; x.beginPath(); x.arc(cx, cy, r, 0, 2 * P); x.stroke(); };
    const A = (cx, cy, r, a0, a1, lw = 2.6) => { x.lineWidth = lw; x.beginPath(); x.arc(cx, cy, r, a0, a1); x.stroke(); };
    const D = (cx, cy, r) => { x.beginPath(); x.arc(cx, cy, r, 0, 2 * P); x.fill(); };
    const L = (pts, lw = 2.6) => { x.lineWidth = lw; x.beginPath(); x.moveTo(pts[0][0], pts[0][1]); for (const p of pts.slice(1)) x.lineTo(p[0], p[1]); x.stroke(); };
    const EYE = (cx, cy, rx, ry, lw = 2.6) => { x.lineWidth = lw; x.beginPath(); x.moveTo(cx - rx, cy); x.quadraticCurveTo(cx, cy - ry * 2, cx + rx, cy); x.quadraticCurveTo(cx, cy + ry * 2, cx - rx, cy); x.stroke(); x.beginPath(); x.ellipse(cx, cy, Math.max(1.6, rx * 0.12), ry * 0.8, 0, 0, 2 * P); x.fill(); };
    const MOON = (cx, cy, r) => { x.beginPath(); x.arc(cx, cy, r, 0.3 * P, 1.7 * P); x.arc(cx + r * 0.45, cy, r * 0.8, 1.62 * P, 0.38 * P, true); x.closePath(); x.fill(); };
    const PETAL = (cx, cy, a, len, wid) => { x.save(); x.translate(cx, cy); x.rotate(a); x.beginPath(); x.moveTo(0, 0); x.quadraticCurveTo(len / 2, -wid, len, 0); x.quadraticCurveTo(len / 2, wid, 0, 0); x.fill(); x.restore(); };
    ({
      // the helm: a ring with a slit pupil turned to the heading
      helm: () => { C(24, 24, 18); x.beginPath(); x.ellipse(24, 14, 3.4, 8, 0, 0, 2 * P); x.fill(); D(24, 30, 2.4); },
      // power: a full moon inside a ring, light rising from it
      power: () => { C(24, 24, 19, 2.2); D(24, 24, 9); for (const a of [-P / 2, -P / 6, -5 * P / 6]) D(24 + Math.cos(a) * 15, 24 + Math.sin(a) * 15, 1.8); },
      // tactical: an eye narrowed on a mark
      tactical: () => { EYE(20, 26, 15, 7); D(40, 10, 3.2); A(40, 10, 6.5, 0, 2 * P, 1.8); },
      // point defence: a crescent guarding a dot
      pd: () => { x.beginPath(); x.arc(24, 26, 18, P * 1.05, P * 1.95); x.stroke(); x.beginPath(); x.arc(24, 26, 12, P * 1.1, P * 1.9); x.stroke(); D(24, 27, 4); },
      // science: an eye with a ring for its study
      science: () => { EYE(24, 24, 18, 8); C(24, 24, 8); L([[24, 4], [24, 10]]); L([[24, 38], [24, 44]]); },
      // comms: a mote and its widening rings going out
      comms: () => { D(12, 24, 3.6); for (const r of [10, 18, 26]) A(12, 24, r, -0.7, 0.7); },
      // repair: four petals closing on a heart
      repair: () => { for (let i = 0; i < 4; i++) PETAL(24, 24, i * P / 2 + P / 4, 17, 6); D(24, 24, 3); },
      // transport: a figure dissolving into a spiral of motes
      transport: () => { D(24, 9, 4.6); x.beginPath(); x.ellipse(24, 21, 7, 6, 0, P, 2 * P); x.lineTo(31, 24); x.lineTo(17, 24); x.closePath(); x.fill(); for (let i = 0; i < 5; i++) D(24 + Math.cos(i * 1.9) * (3 + i * 2.2), 32 + i * 2.4, 1.3 + i * 0.25); },
      // fleet: three moons in a triangle, the lead ahead
      fleet: () => { MOON(24, 12, 8); MOON(12, 34, 7.5); MOON(36, 34, 7.5); },
      // the ship from above: a petal hull with an eye at its heart
      ship: () => { x.beginPath(); x.moveTo(24, 4); x.quadraticCurveTo(44, 26, 24, 44); x.quadraticCurveTo(4, 26, 24, 4); x.fill(); x.save(); x.globalCompositeOperation = 'destination-out'; D(24, 24, 5); x.restore(); D(24, 24, 2.2); },
      // shields: rings of crescents round a mote
      shields: () => { x.beginPath(); x.arc(24, 30, 18, P * 1.1, P * 1.9); x.stroke(); x.beginPath(); x.arc(24, 30, 11, P * 1.15, P * 1.85); x.stroke(); D(24, 30, 3.2); },
      // sensors: an iris, three rings, a mote seen
      sensors: () => { C(24, 24, 19, 2.2); C(24, 24, 11, 1.8); D(24, 24, 3); D(36, 14, 2.6); },
      // tractor: two moons facing, holding
      tractor: () => { MOON(14, 24, 10); x.save(); x.translate(48, 0); x.scale(-1, 1); MOON(14, 24, 10); x.restore(); },
      // craft: a small petal flying, a trail of motes
      craft: () => { PETAL(10, 34, -P / 4, 32, 9); D(8, 41, 2); D(4, 44, 1.4); },
      // weapons: an eye opening into a beam
      weapons: () => { EYE(14, 34, 10, 5); L([[22, 26], [42, 6]], 3); D(42, 6, 2.6); },
      // warp: a crescent drawn out into arcs of speed
      warp: () => { MOON(32, 24, 12); A(32, 24, 19, P * 0.72, P * 1.28, 2.2); A(32, 24, 26, P * 0.78, P * 1.22, 2.2); },
      // engines: a ring and the light falling from it
      engines: () => { C(24, 15, 10, 2.8); D(24, 15, 3); L([[24, 29], [24, 44]]); L([[17, 29], [12, 41]]); L([[31, 29], [36, 41]]); },
      // reactor: a moon core and two crossed orbits
      reactor: () => { D(24, 24, 5.5); x.lineWidth = 2.2; for (const r of [P / 4, -P / 4]) { x.beginPath(); x.ellipse(24, 24, 19, 7.5, r, 0, 2 * P); x.stroke(); } },
      // map: rings round a centre, places as motes
      map: () => { C(24, 24, 18); C(24, 24, 9); D(32, 14, 2.6); D(14, 30, 2.6); D(30, 34, 2); },
      pause: () => { MOON(14, 24, 13); x.save(); x.translate(48, 0); x.scale(-1, 1); MOON(14, 24, 13); x.restore(); },
      menu: () => { for (const y of [12, 24, 36]) { D(9, y, 3); x.lineWidth = 3; x.beginPath(); x.moveTo(17, y); x.quadraticCurveTo(29, y - 3, 41, y); x.stroke(); } },
      // fire: a wide eye with its pupil on the mark
      fire: () => { EYE(24, 24, 20, 9); C(24, 24, 4); },
      cargo: () => { for (const [cx, cy] of [[15, 15], [33, 15], [15, 33], [33, 33]]) C(cx, cy, 7, 2.4); },
      // settings: a waning moon
      settings: () => { MOON(24, 24, 18); D(30, 24, 2.4); },
    })[name]();
  });
}
const ICON_NAMES = ['helm', 'power', 'tactical', 'pd', 'science', 'comms', 'repair', 'transport', 'fleet', 'ship', 'shields', 'sensors', 'tractor', 'craft', 'weapons', 'warp', 'engines', 'reactor', 'map', 'pause', 'menu', 'fire', 'cargo', 'settings'];

// ---- Live readouts (the console skins page and the battle console) ---------------------------------------------
const live = {
  // A comet: a small moon trailing light.
  arrow: (x) => { x.shadowColor = OM.pink; x.shadowBlur = 8; const g = x.createLinearGradient(-14, 0, 8, 0); g.addColorStop(0, 'rgba(255,79,176,0)'); g.addColorStop(1, OM.pale); x.fillStyle = g; x.beginPath(); x.moveTo(-14, -2); x.lineTo(6, -5); x.arc(6, 0, 5, -P / 2, P / 2); x.lineTo(-14, 2); x.closePath(); x.fill(); },
  // The helm's pupil: nothing drawn over it (the eye is the stop).
  helmCentre: () => {},
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
};

// ---- Shapes for the battle console's vector keys ----------------------------------------------------------------
// Nothing straight if a curve will do (design.md): every rounded rectangle the page asks for is drawn with fully round
// ends, as the Ombrai's capsule keys, and a knob is round.
function rr(G, X, Y, W, H, r) { G.beginPath(); if (!(W > 0 && H > 0)) { G.rect(X, Y, W, H); return; } G.roundRect(X, Y, W, H, Math.min(W, H) / 2); }
function knob(G, cx, cy, r) { G.beginPath(); G.arc(cx, cy, r, 0, 2 * P); }

// ---- The kit ------------------------------------------------------------------------------------------------------
// Every shell is rendered in Blender and comes from the inlined manifest, with its wells from the inlined shapes;
// load() decodes them once. The skins page shows the kit's pieces as rendered. The battle console draws its readouts in
// light over a panel's middle, its strips' channels and the name plate's field, so those wells are veiled from above
// for it; and its own pieces are composed here from the same renders, no new shell: the square key is the dial's plum
// ring round a well lit from below in each state the console shows, the wide key the tab's capsule with its well lit
// the same way, the allocation cells the inset's small plate with the light in its well, the
// title placard the tab's capsule drawn 24 px tall, and the name plate drawn shorter so the Fleet key stands beside it.
const MANIFEST = 'assets/ui/ombrai/manifest.json', SHAPES = 'assets/ui/ombrai/shapes.json';
const IMG = {}, kit = {}, hero = {}, heroSize = {}, barCells = {}, cells = {};
// the key states the console asks for (facePiece in the page) and the light each shows (null: lit solid)
const KEY_STATES = { idle: [0.12, OM.pink], hover: [0.55, OM.pink], on: [null, OM.pink], ok: [null, OM.moon], warn: [null, OM.amber], alert: [null, OM.crimson], off: [0, OM.pink] };
// the wells the console veils, with how dark (the role names in shapes.json)
const VEILED = { 'frame.panel': ['void', 0.72], 'frame.inset': ['void', 0.6], 'strip.top': ['void', 0.6], 'strip.bottom': ['void', 0.6], nameplate: ['void', 0.6], dial: ['void', 0.5] };
let loading = null;
function json(src) { const n = document.querySelector(`script[data-src="${src}"]`); return n ? JSON.parse(n.textContent) : null; }
const well = (sh, name, kind) => (sh.pieces[name].wells.find(([k]) => k === kind) || [])[1];
function load() {
  if (loading) return loading;
  const d = json(MANIFEST), sh = json(SHAPES);
  if (!d || !sh) return (loading = Promise.reject(new Error(`the Ombrai kit (${MANIFEST}, ${SHAPES}) is not inlined in this page`)));
  loading = Promise.all(Object.entries(d.pieces).map(([id, p]) => new Promise((ok, bad) => { const im = new Image(); im.onload = () => { IMG[id] = im; ok(); }; im.onerror = () => bad(new Error(`Ombrai piece ${id} failed to decode`)); im.src = p.data; }))).then(() => {
    const at = (c) => (x, w, h) => x.drawImage(c, 0, 0, w, h);
    for (const [id, p] of Object.entries(d.pieces)) {
      const v = VEILED[id], pts = v && well(sh, id, v[0]);
      const c = paint(p.size[0], p.size[1], (x) => { x.drawImage(IMG[id], 0, 0, p.size[0], p.size[1]); if (pts) veil(x, pts, v[1]); });
      if (p.kind === 'nine') kit[id] = { size: p.size, m: p.margins, inset: p.inset || [3, 3], draw: at(c) };
      else if (p.kind === 'hero') { hero[id] = at(c); heroSize[id] = p.size; }
      else if (p.kind === 'cell') barCells[id] = { size: p.size, gap: p.gap ?? 2, draw: at(c) };
    }
    // the moon light for whole: the lit eye's light turned to the pale moon, and the plain fill the same
    const tint = (src, col) => (x, w, h) => { src.draw(x, w, h); x.globalCompositeOperation = 'color'; x.fillStyle = col; x.fillRect(0, 0, w, h); x.globalCompositeOperation = 'destination-in'; src.draw(x, w, h); x.globalCompositeOperation = 'source-over'; };
    barCells['bar.cell.ok'] = { ...barCells['bar.cell.on'], draw: tint(barCells['bar.cell.on'], OM.moon) };
    kit['bar.fill.accord'] = { ...kit['bar.fill'], draw: tint(kit['bar.fill'], OM.moon) };
    // the square key: the dial's plum ring (its well a disc of radius 49 in 120) at 48 px, lit from below
    for (const [st, [level, col]] of Object.entries(KEY_STATES)) {
      const c = paint(48, 48, (x) => { x.drawImage(IMG.dial, 0, 0, 48, 48); const r = 49 * 0.4 - 0.6; lightBelow(x, () => { x.beginPath(); x.arc(24, 24, r, 0, 2 * P); }, [24 - r, 24 - r, 24 + r, 24 + r], level, col); });
      hero['key.' + st] = at(c); heroSize['key.' + st] = [48, 48];
    }
    // the wide key: the tab's capsule (tab.idle, 120 x 40), even at both ends so a label centred on it is never under a
    // jewel, its well lit from below in the state's light
    const kp = well(sh, 'tab.idle', 'key');
    for (const [st, [level, col]] of Object.entries(KEY_STATES)) {
      const c = paint(120, 40, (x) => { x.drawImage(IMG['tab.idle'], 0, 0, 120, 40); lightBelow(x, () => path(x, kp), bounds(kp), level, col); });
      kit['keyw.' + st] = { size: [120, 40], m: [0, 24, 0, 24], draw: at(c) };
    }
    // the allocation cells: the inset's small plate (64 x 48) at 32 x 28, its well lit; locked, the plate at rest, dimmed
    const ip = well(sh, 'frame.inset', 'void');
    for (const [id, level, col] of [['cell.idle', 0.08, OM.pink], ['cell.lit', null, OM.pink], ['cell.warn', null, OM.crimson], ['cell.accord', null, OM.moon], ['cell.locked', 0, OM.pink]]) {
      const c = paint(64, 48, (x) => { if (id === 'cell.locked') x.globalAlpha = 0.45; x.drawImage(IMG['frame.inset'], 0, 0, 64, 48); lightBelow(x, () => path(x, ip), bounds(ip), level, col); });
      cells[id] = { size: [32, 28], m: [8, 8, 8, 8], draw: (x, w, h) => x.drawImage(c, 0, 0, w, h) };
    }
    // the title placard (every panel title and officer slot on a frame's top rail): the tab's capsule (tab.idle, 120 x
    // 40, its plate rows 4-36) drawn 24 px tall, its well (rows 4.9-19.1 of the placard) veiled so a title reads on it
    const tp = well(sh, 'tab.idle', 'key');
    const pt = paint(120, 40, (x) => { x.drawImage(IMG['tab.idle'], 0, 0, 120, 40); veil(x, tp, 0.75); });
    kit['plate.title'] = { size: [120, 24], m: [0, 24, 0, 24], draw: (x, w, h) => x.drawImage(pt, 0, 4 * 2, pt.width, 32 * 2, 0, 0, w, h) };
    // the name plate drawn shorter (its field's straight middle, x 140-380 of 460, taken in to 146 px), so the plate ends
    // at 366 and the Fleet key (x 370-456) stands beside it on the backing
    const np = paint(460, 56, hero.nameplate);
    hero.nameplate = (x, w, h) => {
      const k = w / 460, sk = np.width / 460;
      for (const [s0, s1, d0, d1] of [[0, 140, 0, 140], [140, 380, 140, 286], [380, 460, 286, 366]]) x.drawImage(np, s0 * sk, 0, (s1 - s0) * sk, np.height, d0 * k, 0, (d1 - d0) * k, h);
    };
  });
  return loading;
}

const api = {
  id: 'ombrai', name: 'Ombrai', OM, CC, kit, hero, cells, barCells, live, icon, ICON_NAMES, ink, keyInk, font, fonts, load, hexA, rr, knob, heroSize,
  family: 'Quicksand, sans-serif',
  // The content inside a panel: clear of the plum plate and the well's bitten corners (36 px margins; the well's straight
  // edges 18 px in, its corner bites reaching 31 px), 8 px into the veiled well.
  pad: [28, 26, 26, 26],
  // The name plate's live field: its field as drawn shorter (62-352 x 9-47 of the plate), 14 px clear of its round end.
  plate: { x: 78, y: 35, max: 262, font: '700 17px Quicksand', col: OM.pale },
  // The battle's helm ring under the ship, in pink.
  ring: ['rgba(255,79,176,0)', 'rgba(255,79,176,0.14)', 'rgba(255,170,220,0.9)'],
  // Names for the player's side where the page shows it as the Ombrai: ships named for what is hidden or seen, their
  // captains Watchers (the skins page's VEIL OF ISH, THE WATCHER). Placeholders until the owner sets the Ombrai's
  // culture (CLAUDE.md section 1; openspec/changes/ombrai-battle-console, open question O1).
  player: { faction: 'Ombrai', call: 'Watcher', trim: 0xff4fb0, hull: 0x3b2a5c,
    ships: { resolute: ['Veil of Ish', 'Watcher Oun'], tallis: ['Lantern Below', 'Watcher Sael'], meridian: ['Quiet Lid', 'Watcher Imre'] },
    ranks: ['Glance', 'Gaze', 'Watcher', 'Far-seer'],
    officers: { o1: 'Sela Oun', o2: 'Imre Vash', o3: 'Teiv Anu', o4: 'Lio Sereth', o5: 'Ushe Marn', o6: 'Navi Ost', o7: 'Corrow Ia',
      o8: 'Veth Ilun', o9: 'Ama Isk', o10: 'Pell Onu', o11: 'Rhue Tamsel', o12: 'Isse Varn', o13: 'Olo Kethe' } },
  link: OM.pink,
  // Labels centred in each key's crescent well, beside the bite where its moon jewel rests (the well runs 30-140 of
  // 150, bitten to x 37 by the moon at its middle row: the field 34-138, so a label as long as its key allows may touch
  // the bite by 2 px, the price of reading "PROXIMITY" on a 95 px key); on a tab, in its well (13-107 of 120), 14 px
  // from each end, so a long label (OVERLOAD on a 101 px tab) keeps its size and is never trimmed.
  keyCenter: true, keyField: [34, 12], tabField: [14, 14],
  // A hover name on the wide key in its hover light, pale on it.
  tip: ['button.hover', 'hover'],
  // Icon tabs (the station and ship rails) on the square key: a well in a plum ring, lit from below.
  iconKeys: true,
  // Panel tab icons at rest in the lilac print, apart from the functional icons' pink; glyphs on a dark key in pink.
  tabIcon: OM.dim, glyphOnDark: OM.pink, mark: { hover: OM.pale, ready: OM.moon, count: OM.dim },
  // The strips (72 px rendered, as the skins page's) at their full height; the channel's open band is rows 11-60, and
  // the content, laid out for 56 px, moved down to centre in it.
  stripTop: { h: 72, dy: 13 }, stripBottom: { h: 72, dy: 2 },
  // A panel title's baseline 2 px higher than the console's, its Syncopate capitals centred in the placard's well.
  titleDy: 2,
};
window.FCSkins = window.FCSkins || {}; window.FCSkins.ombrai = api;
return api;
})();
