// Fleet Command mockups: the Vos Syndicate skin (openspec/changes/console-skins/design.md, "Vos Syndicate: hand-drawn";
// openspec/changes/vos-battle-console), shared by the console skins page (docs/mockups/console-skins.html, whose Vos entry
// and whose FCK drawing helpers come from here) and the battle console, where it is the skin of the Vos ship you command
// (docs/mockups/battle-console-vos.html, written from battle-console.html by tools/mockups/skin_page.py). One
// implementation of the Syndicate's look (CLAUDE.md section 6.1): the hand-drawn helpers (ink, wash, hatch, dry, spiral),
// the kit's painters, the icons, the live readouts, and the pieces only the battle console draws (square tag keys, wide
// claw tags, allocation cells, the title ticket and the patched sailcloth behind the column).
// It exposes the same fields as docs/mockups/lib/concord.js and lib/kelvane.js, so the battle console swaps one for
// another and changes no rule and no layout. Needs docs/mockups/lib/kitdraw.js inlined first. Our own design: no other
// game's names, shapes, glyphs or art.
//
// Where it comes from (design.md; owner, 2026-10-30: "I really like espeically the mirak ui design, its so playful and
// pirate like", "design a hand drawn aesthetic of the mirak ui"): a crew's own console, inked and painted by hand over
// salvage. Free clans of raiders, luck, loot and loyalty to the crew (the provisional account, survey U7); the look comes
// from that tradition of salvage and crew marks, never from what the Vos look like (CLAUDE.md section 1).
// Every stroke comes from a seeded generator (FCDraw.rng, Mulberry32), so a piece is drawn the same way every time.
window.FCVos = (function () {
'use strict';
const { rng, hash, paint, P } = window.FCDraw;
// The palette and what it stands for (design.md): tattoo ink, salvage rust, loot brass, old-coin verdigris, stolen bone
// charts, a warning in blood.
const VC = { ink: '#2a1410', rust: '#a8482a', brass: '#d6a043', brassHi: '#efc56a', verd: '#3fb7a3', bone: '#efe0bd', blood: '#d23a4a', wine: '#24110d', caution: '#e8b33a' };
function noise1(R) { const p = [0, 1, 2, 3].map(() => [R() * 6.283, 0.6 + R() * 1.4]); return (t) => p.reduce((s, [ph, f], i) => s + Math.sin(t * f * (i + 1) + ph) / (i + 1), 0) / 1.6; }
// Resample a polyline and push each point sideways by smooth noise: the line of a hand.
function wob(pts, closed, amp, R, step = 2.5) {
  const n = noise1(R), out = []; let s = 0; const segs = closed ? pts.length : pts.length - 1;
  for (let i = 0; i < segs; i++) {
    const a = pts[i], b = pts[(i + 1) % pts.length], L = Math.hypot(b[0] - a[0], b[1] - a[1]) || 1, k = Math.max(1, Math.ceil(L / step));
    const nx = -(b[1] - a[1]) / L, ny = (b[0] - a[0]) / L;
    for (let j = 0; j < k; j++) { const t = j / k, d = n((s + t * L) * 0.07) * amp; out.push([a[0] + (b[0] - a[0]) * t + nx * d, a[1] + (b[1] - a[1]) * t + ny * d]); }
    s += L;
  }
  if (!closed) out.push(pts[pts.length - 1]);
  return out;
}
function arcPts(cx, cy, rx, ry, a0, a1, n = 24) { const o = []; for (let i = 0; i <= n; i++) { const a = a0 + (a1 - a0) * i / n; o.push([cx + Math.cos(a) * rx, cy + Math.sin(a) * ry]); } return o; }
function rrPts(X, Y, W, H, r) { return [...arcPts(X + W - r, Y + r, r, r, -P / 2, 0, 6), ...arcPts(X + W - r, Y + H - r, r, r, 0, P / 2, 6), ...arcPts(X + r, Y + H - r, r, r, P / 2, P, 6), ...arcPts(X + r, Y + r, r, r, P, 1.5 * P, 6)]; }
function trace(x, pts, closed) { x.beginPath(); x.moveTo(pts[0][0], pts[0][1]); for (const p of pts.slice(1)) x.lineTo(p[0], p[1]); if (closed) x.closePath(); }
// An ink line: a full stroke, then a thinner second pass a little off it, as a brush leaves.
function ink(x, pts, closed, w = 2.4, col = VC.ink) {
  trace(x, pts, closed); x.strokeStyle = col; x.lineWidth = w; x.globalAlpha = 1; x.stroke();
  x.save(); x.translate(0.5, 0.35); trace(x, pts, closed); x.lineWidth = w * 0.45; x.globalAlpha = 0.55; x.stroke(); x.restore(); x.globalAlpha = 1;
}
// A wash: the colour, then blotches lighter and darker, then paper grain, clipped to the shape.
function wash(x, pts, col, R, dark = 'rgba(42,20,16,0.20)', light = 'rgba(255,240,210,0.13)') {
  const xs = pts.map((p) => p[0]), ys = pts.map((p) => p[1]), x0 = Math.min(...xs), x1 = Math.max(...xs), y0 = Math.min(...ys), y1 = Math.max(...ys);
  x.save(); trace(x, pts, true); x.clip(); x.fillStyle = col; x.fillRect(x0, y0, x1 - x0, y1 - y0);
  const n = Math.max(6, Math.round((x1 - x0) * (y1 - y0) / 500));
  for (let i = 0; i < n; i++) {
    const cx = x0 + R() * (x1 - x0), cy = y0 + R() * (y1 - y0), r = 6 + R() * 22, g = x.createRadialGradient(cx, cy, 0, cx, cy, r);
    g.addColorStop(0, R() < 0.55 ? dark : light); g.addColorStop(1, 'rgba(0,0,0,0)'); x.fillStyle = g; x.fillRect(cx - r, cy - r, 2 * r, 2 * r);
  }
  for (let i = 0; i < n * 14; i++) { x.fillStyle = R() < 0.5 ? 'rgba(0,0,0,0.10)' : 'rgba(255,255,255,0.06)'; x.fillRect(x0 + R() * (x1 - x0), y0 + R() * (y1 - y0), 0.8, 0.8); }
  // A darker rim inside the edge, as pigment pools where a wash dries.
  trace(x, pts, true); x.strokeStyle = 'rgba(42,20,16,0.22)'; x.lineWidth = 3; x.stroke();
  x.restore();
}
// Hatching on the shadow side (bottom right), wobbly diagonal strokes clipped to the shape.
function hatch(x, pts, R, from = 0.55, gap = 3.4, col = 'rgba(42,20,16,0.55)') {
  const xs = pts.map((p) => p[0]), ys = pts.map((p) => p[1]), x0 = Math.min(...xs), x1 = Math.max(...xs), y0 = Math.min(...ys), y1 = Math.max(...ys);
  const W = x1 - x0, H = y1 - y0;
  x.save(); trace(x, pts, true); x.clip();
  x.beginPath(); x.moveTo(x0 + W, y0 + H * from * 0.6); x.lineTo(x0 + W, y1); x.lineTo(x0 + W * from * 0.6, y1); x.closePath(); x.clip();
  x.strokeStyle = col; x.lineWidth = 0.9;
  for (let d = -H; d < W + H; d += gap) { const l = wob([[x0 + d, y1 + 2], [x0 + d + H + 4, y0 - 2]], false, 0.6, R, 6); trace(x, l, false); x.stroke(); }
  x.restore();
}
// A dry-brush highlight: broken light strokes just inside the top-left of the edge.
function dry(x, pts, R) {
  const cx = pts.reduce((s, p) => s + p[0], 0) / pts.length, cy = pts.reduce((s, p) => s + p[1], 0) / pts.length;
  x.strokeStyle = 'rgba(255,244,215,0.55)'; x.lineWidth = 1.3;
  for (let i = 0; i < pts.length - 3; i += 3) {
    const [px, py] = pts[i]; if (px - cx > (cy - py) * 0.6 || R() < 0.35) continue;
    const q = pts[i + 2]; const ix = (cx - px) * 0.07, iy = (cy - py) * 0.07;
    x.beginPath(); x.moveTo(px + ix, py + iy); x.lineTo(q[0] + ix, q[1] + iy); x.stroke();
  }
}
function spiral(x, cx, cy, r, turns, R, w = 1.8, col = VC.ink) { const o = []; const n = 40 * turns; for (let i = 0; i <= n; i++) { const t = i / n, a = t * turns * 2 * P; o.push([cx + Math.cos(a) * r * t, cy + Math.sin(a) * r * t]); } ink(x, wob(o, false, 0.4, R, 1.5), false, w, col); }
// A painted body: wash, hatching, highlight, ink outline.
function body(x, pts, col, R, opt = {}) { wash(x, pts, col, R); if (opt.hatch !== false) hatch(x, pts, R, opt.from ?? 0.55); dry(x, pts, R); ink(x, pts, true, opt.w ?? 2.4); }
function vos(name, w, h, draw) { return (x) => draw(x, w, h, rng(hash('vos.' + name))); }
// The claw tag: a lopsided luggage tag with an eyelet, its right end hooked like a claw.
function clawTag(X, Y, W, H) {
  return [[X + 18, Y + 3], [X + W - 40, Y + 5], ...[[X + W - 26, Y + 6], [X + W - 14, Y + 12], [X + W - 6, Y + 24], [X + W - 2, Y + H - 2], [X + W - 12, Y + H - 9], [X + W - 24, Y + H - 6]],
    [X + 18, Y + H - 4], ...arcPts(X + 18, Y + H / 2, 15, H / 2 - 4, P / 2, 1.5 * P, 10)];
}
function vosButton(col, hi) {
  return (x, w, h, R) => {
    const pts = wob(clawTag(1, 1, w - 2, h - 2), true, 1.1, R); body(x, pts, col, R, { from: 0.4 });
    if (hi) { x.fillStyle = 'rgba(255,236,170,0.22)'; trace(x, pts, true); x.fill(); }
    x.fillStyle = VC.wine; x.beginPath(); x.arc(12, h / 2, 4.2, 0, 2 * P); x.fill(); ink(x, wob(arcPts(12, h / 2, 5.5, 5.5, 0, 2 * P, 14), true, 0.4, R), true, 1.6);
  };
}
function vosTab(col) {
  return (x, w, h, R) => {
    const bottom = []; for (let i = 0; i <= 12; i++) bottom.push([w - 2 - i * (w - 4) / 12, h - 3 - (i % 2 ? 3.5 : 0) - R() * 1.5]);
    const pts = wob([[2, 3], [w - 2, 2], ...bottom], true, 0.8, R); body(x, pts, col, R, { from: 0.5, w: 2 });
  };
}
const vosKit = {
  'frame.panel': { size: [160, 120], m: [32, 32, 32, 32], draw: (x, w, h, R) => {
    const outer = wob(rrPts(2, 2, w - 4, h - 4, 16), true, 1.3, R), inner = wob(rrPts(21, 21, w - 42, h - 42, 8), true, 0.9, R);
    wash(x, outer, VC.rust, R); hatch(x, outer, R, 0.6); dry(x, outer, R);
    wash(x, inner, VC.wine, R, 'rgba(0,0,0,0.25)', 'rgba(255,200,160,0.05)'); ink(x, outer, true, 2.6); ink(x, inner, true, 2);
    for (const [cx, cy] of [[11.5, 11.5], [w - 11.5, 11.5], [11.5, h - 11.5], [w - 11.5, h - 11.5]]) spiral(x, cx, cy, 6.5, 2.2, R, 1.6, VC.brassHi);
  } },
  'frame.inset': { size: [64, 48], m: [14, 14, 14, 14], draw: (x, w, h, R) => { const p = wob(rrPts(2, 2, w - 4, h - 4, 9), true, 0.8, R); wash(x, p, VC.wine, R, 'rgba(0,0,0,0.25)', 'rgba(255,200,160,0.05)'); ink(x, p, true, 2, VC.brass); } },
  'bar.track': { size: [96, 16], m: [0, 12, 0, 12], inset: [3, 3], draw: (x, w, h, R) => { const p = wob(rrPts(1.5, 1.5, w - 3, h - 3, 7), true, 0.6, R); wash(x, p, VC.wine, R, 'rgba(0,0,0,0.3)'); ink(x, p, true, 1.8); } },
  'bar.fill': { size: [64, 10], m: [0, 10, 0, 6], draw: (x, w, h, R) => vosStroke(x, w, h, R, VC.verd) },
  'bar.fill.red': { size: [64, 10], m: [0, 10, 0, 6], draw: (x, w, h, R) => vosStroke(x, w, h, R, VC.blood) },
  'button.idle': { size: [140, 52], m: [22, 40, 22, 28], draw: vosButton(VC.brass, false) },
  'button.hover': { size: [140, 52], m: [22, 40, 22, 28], draw: vosButton(VC.brassHi, true) },
  'button.pressed': { size: [140, 52], m: [22, 40, 22, 28], draw: vosButton(VC.rust, false) },
  'tab.idle': { size: [120, 40], m: [12, 14, 14, 14], draw: vosTab(VC.bone) },
  'tab.active': { size: [120, 40], m: [12, 14, 14, 14], draw: vosTab(VC.verd) },
  'strip.top': { size: [512, 72], m: [12, 100, 12, 100], draw: (x, w, h, R) => vosPlank(x, w, h, R) },
  'strip.bottom': { size: [512, 72], m: [12, 100, 12, 100], draw: (x, w, h, R) => vosPlank(x, w, h, R) },
  'divider': { size: [128, 10], m: [0, 16, 0, 16], draw: (x, w, h, R) => { const l = []; for (let i = 0; i <= 30; i++) l.push([12 + i * (w - 24) / 30, h / 2 + Math.sin(i * 1.3) * 1.4]); ink(x, wob(l, false, 0.5, R), false, 1.8); spiral(x, 6, h / 2, 4.5, 1.8, R, 1.4); spiral(x, w - 6, h / 2, 4.5, 1.8, R, 1.4); } },
};
function vosStroke(x, w, h, R, col) {
  // A loaded brush pulled left to right, drying out at its end into streaks.
  x.save(); x.globalAlpha = 0.95; const p = wob([[1, 2], [w - 6, 1.5], [w - 1, h / 2], [w - 6, h - 1.5], [1, h - 2]], true, 0.5, R); wash(x, p, col, R, 'rgba(0,0,0,0.18)', 'rgba(255,255,255,0.18)');
  x.strokeStyle = 'rgba(255,250,230,0.5)'; x.lineWidth = 1; for (let i = 0; i < 3; i++) { x.beginPath(); x.moveTo(3, 3 + i * 2); x.lineTo(w - 10 - R() * 20, 3 + i * 2); x.stroke(); }
  x.restore();
}
function vosPlank(x, w, h, R) {
  const p = wob([[22, 6], [w - 30, 5], [w - 8, 12], [w - 2, h - 8], [w - 30, h - 6], [22, h - 7], ...arcPts(22, h / 2, 18, h / 2 - 6, P / 2, 1.5 * P, 10)], true, 1.2, R);
  wash(x, p, VC.rust, R); hatch(x, p, R, 0.75);
  x.save(); trace(x, p, true); x.clip(); x.strokeStyle = 'rgba(42,20,16,0.45)'; x.lineWidth = 0.9;
  for (const y of [16, 27, 39].map((v) => v * h / 56)) { const l = []; for (let i = 0; i <= 40; i++) l.push([0 + i * w / 40, y + Math.sin(i * 0.7 + y) * 1.2]); trace(x, l, false); x.stroke(); }
  x.restore(); dry(x, p, R); ink(x, p, true, 2.6);
  spiral(x, 22, h / 2, 13, 2.6, R, 2.2, VC.brassHi);
  ink(x, wob([[w - 26, 14], [w - 14, 22], [w - 20, 36], [w - 30, 42]], false, 0.5, R), false, 2.2, VC.brassHi);
}
const vosHero = {
  // A scroll: a bone banner whose ends curl.
  'nameplate': (x, w, h, R) => {
    const p = wob([[34, 6], [w - 34, 4], [w - 26, h / 2], [w - 34, h - 5], [34, h - 6], [26, h / 2]], true, 1.1, R); body(x, p, VC.bone, R, { from: 0.7, w: 2.2 });
    spiral(x, 20, h / 2, 14, 2.3, R, 2.2); spiral(x, w - 20, h / 2, 14, 2.3, R, 2.2);
  },
  // Helm: a ship's wheel. Eight spokes end in claw handles, the hub is a brass coin with a spiral.
  'helm.pad': (x, w, h, R) => {
    const c = w / 2;
    for (let i = 0; i < 8; i++) { const a = -P / 2 + i * P / 4; const s = wob([[c + Math.cos(a) * 40, c + Math.sin(a) * 40], [c + Math.cos(a) * 132, c + Math.sin(a) * 132]], false, 0.8, R); ink(x, s, false, 9, VC.ink); ink(x, s, false, 5, VC.rust); }
    const rimO = wob(arcPts(c, c, 110, 110, 0, 2 * P, 60), true, 1.4, R), rimI = wob(arcPts(c, c, 88, 88, 0, 2 * P, 50), true, 1.2, R).reverse();
    x.save(); x.beginPath(); for (const ring of [rimO, rimI]) { x.moveTo(ring[0][0], ring[0][1]); for (const q of ring.slice(1)) x.lineTo(q[0], q[1]); x.closePath(); } x.clip('evenodd'); wash(x, rimO, VC.rust, R); hatch(x, rimO, R, 0.6); x.restore(); ink(x, rimO, true, 2.6); ink(x, rimI, true, 2.2);
    for (let i = 0; i < 8; i++) { const a = -P / 2 + i * P / 4, hx = c + Math.cos(a) * 128, hy = c + Math.sin(a) * 128; const k = wob(arcPts(hx, hy, 16, 16, 0, 2 * P, 16), true, 1, R); body(x, k, VC.brass, R, { w: 2 }); }
    const hub = wob(arcPts(c, c, 40, 40, 0, 2 * P, 30), true, 1, R); body(x, hub, VC.brass, R); spiral(x, c, c, 26, 2.5, R, 2.2);
  },
  // The systems display: a stolen chart, torn at the edges, stained, with a spiral at each corner.
  'ssd.bezel': (x, w, h, R) => {
    const p = wob([[6, 8], [w * 0.4, 4], [w - 8, 9], [w - 4, h * 0.5], [w - 9, h - 6], [w * 0.55, h - 3], [8, h - 8], [3, h * 0.45]], true, 2.2, R);
    body(x, p, VC.bone, R, { from: 0.8, w: 2.6 });
    x.strokeStyle = 'rgba(120,60,30,0.25)'; x.lineWidth = 5; x.beginPath(); x.arc(w * 0.78, h * 0.25, 30, 0.3, 5.6); x.stroke();
    const b = wob(rrPts(22, 22, w - 44, h - 44, 6), true, 1.1, R); ink(x, b, true, 1.6);
    for (const [cx, cy] of [[22, 22], [w - 22, 22], [22, h - 22], [w - 22, h - 22]]) spiral(x, cx, cy, 10, 2.2, R, 1.8, VC.rust);
  },
  // A gauge drawn as an eye: the iris is drawn live, so the eye looks toward the nearest contact.
  'dial': (x, w, h, R) => {
    const c = w / 2; const lid = wob([...arcPts(c, c + 26, 56, 52, -P * 0.86, -P * 0.14, 20), ...arcPts(c, c - 26, 56, 52, P * 0.14, P * 0.86, 20)], true, 1, R);
    body(x, lid, VC.bone, R, { from: 0.6, w: 2.6 });
    for (let i = 0; i < 6; i++) { const a = -P * 0.8 + i * P * 0.12; ink(x, wob([[c + Math.cos(a) * 46, c + 8 + Math.sin(a) * 40], [c + Math.cos(a) * 58, c + 2 + Math.sin(a) * 52]], false, 0.6, R), false, 2); }
  },
  'lamp.alert.green': (x, w, h, R) => vosLamp(x, w, R, VC.verd), 'lamp.alert.yellow': (x, w, h, R) => vosLamp(x, w, R, VC.caution), 'lamp.alert.red': (x, w, h, R) => vosLamp(x, w, R, VC.blood),
  // The Vos mark (original): a coin with a spiral, raked by three claws.
  'crest': (x, w, h, R) => {
    const c = w / 2; const coin = wob(arcPts(c, c, 40, 40, 0, 2 * P, 32), true, 1.2, R); body(x, coin, VC.brass, R); spiral(x, c, c, 28, 2.6, R, 2.4);
    for (let i = 0; i < 3; i++) { const o = (i - 1) * 13; const cl = wob([[c - 26 + o, 10], [c - 4 + o, c], [c + 18 + o, w - 10]], false, 0.6, R); ink(x, cl, false, 5, VC.ink); ink(x, cl, false, 2.4, VC.blood); }
  },
};
function vosLamp(x, w, R, col) {
  const c = w / 2; ink(x, wob(arcPts(c, 12, 9, 9, P, 2 * P, 10), false, 0.5, R), false, 2);
  const g = wob(arcPts(c, c + 6, 20, 22, 0, 2 * P, 22), true, 1, R); wash(x, g, col, R, 'rgba(0,0,0,0.15)', 'rgba(255,255,230,0.35)');
  x.save(); x.globalAlpha = 0.35; x.fillStyle = col; x.shadowColor = col; x.shadowBlur = 14; trace(x, g, true); x.fill(); x.restore();
  ink(x, g, true, 2.4); for (const dx of [-8, 0, 8]) ink(x, wob([[c + dx, c - 14], [c + dx * 1.2, c + 26]], false, 0.5, R), false, 1.4);
}
function hexA(hex, a) { const n = parseInt(hex.slice(1), 16); return `rgba(${n >> 16},${(n >> 8) & 255},${n & 255},${a})`; }
// Relative luminance of a #rrggbb colour (WCAG), for picking the ink that reads on a fill.
function lum(hex) {
  if (typeof hex !== 'string' || hex[0] !== '#' || hex.length < 7) return 0;
  const c = [1, 3, 5].map((i) => parseInt(hex.slice(i, i + 2), 16) / 255).map((v) => (v <= 0.03928 ? v / 12.92 : ((v + 0.055) / 1.055) ** 2.4));
  return 0.2126 * c[0] + 0.7152 * c[1] + 0.0722 * c[2];
}

// ---- The battle console's colour roles -------------------------------------------------------------------------
// The same role names as the Concord's (lib/concord.js), so the page's rules pick a role, never a colour. Inked over the
// stained washes and over the battle: pale verdigris for a reading (Sky), the deeper verdigris of old coin for whole and
// engaged (Accord: the spoils kept), brass gold for damaged (Caution), blood only for down and alarms (Alert), a faded
// tan for a label (Lilac), bone for prose and the rust of salvage for a key at rest (Signal). The shields' ramp is the
// skins page's: verdigris, gold, blood.
const CC = {
  deep: '#4a2418', signal: '#6a3422', sky: '#7fdcc8', accord: '#3fb79a', lilac: '#cfae8c', caution: VC.caution, alert: '#e23a48', text: VC.bone,
  ink: VC.ink, inkDis: '#6a4a3a', night: '#1e0e0b', ghost: '#7a5040', void: '#120806', slate: '#4a2a20', mute: '#3a2620', ver: '#a88a70',
  char: '#3a0b09', plate: '#1c0d0a', bg: '#140a08', title: VC.brassHi,
};
// The ink that reads on a fill: tattoo ink on a washed colour, bone on the dark washes.
const inkOn = (fill) => (lum(fill) > 0.18 ? VC.ink : VC.bone);
// Ink on the kit's tags and tickets: ink on the brass tag at rest and on hover, bone on the rust tag pressed; ink on the
// bone ticket and the verdigris one alike.
const keyInk = { idle: VC.ink, hover: VC.ink, pressed: VC.bone, tabOn: VC.ink, tabOff: VC.ink };
// Type: "Permanent Marker" for headings, as the skins page's keys and titles, and "Patrick Hand" for everything else (the
// skin's pair). The page asks for Antonio sizes. Patrick Hand at the same size runs 1.09 times as wide (measured on the
// console's own labels, 2026-11-07); rule T (the keys' and tabs' fit) shrinks what still runs long. Weight 700 at 22 px and
// up is a heading: Permanent Marker, 1.48 times as wide, at 0.72 of the size.
function font(spec) {
  const m = /^(\d+) ([\d.]+)px Antonio$/.exec(spec); if (!m) return spec;
  const w = +m[1], s = +m[2];
  if (w >= 700 && s >= 22) return `400 ${Math.round(s * 0.72 * 10) / 10}px "Permanent Marker"`;
  return `400 ${Math.round(s * 1.0 * 10) / 10}px "Patrick Hand"`;
}
const fonts = ['400 20px "Permanent Marker"', '400 20px "Patrick Hand"'];

// ---- Icons: one subject list for every skin (docs/design/icon-guide.html) ---------------------------------------
// Each a picture of the job in a crew's brush: crooked ink strokes that taper and double, filled where the thing is solid,
// single strokes for what it gives off; a spiral where a crew would sign it (the wheel's hub, the cog's bore). One ink;
// on a 48 x 48 grid with 4 px clear, read at 20 px and 48 px. The same seed draws a subject in every colour, so an icon
// only changes its ink between states.
function icon(name, col = CC.sky) {
  return paint(48, 48, (x) => {
    const R = rng(hash('vos.icon.' + name));
    const L = (pts, wd = 3.2) => ink(x, wob(pts, false, 0.7, R, 2), false, wd, col);
    const C = (cx, cy, r, wd = 3, a0 = 0, a1 = 2 * P) => ink(x, wob(arcPts(cx, cy, r, r, a0, a1, 18), a1 - a0 >= 2 * P - 1e-6, 0.5, R, 2), a1 - a0 >= 2 * P - 1e-6, wd, col);
    const F = (pts) => { const p = wob(pts, true, 0.6, R, 2); trace(x, p, true); x.fillStyle = col; x.fill(); ink(x, p, true, 1.4, col); };
    const D = (cx, cy, r) => F(arcPts(cx, cy, r, r, 0, 2 * P, 10).slice(0, -1));
    ({
      // a ship's wheel: a crooked rim, eight spokes out past it to handles, a spiral hub
      helm: () => { C(24, 24, 12, 3); for (let i = 0; i < 8; i++) { const a = -P / 2 + i * P / 4; L([[24 + Math.cos(a) * 4, 24 + Math.sin(a) * 4], [24 + Math.cos(a) * 19, 24 + Math.sin(a) * 19]], 2.6); D(24 + Math.cos(a) * 20, 24 + Math.sin(a) * 20, 2.6); }
        spiral(x, 24, 24, 5, 1.4, R, 1.8, col); },
      // power: a crooked bolt, solid
      power: () => { F([[29, 4], [13, 27], [23, 26], [17, 44], [36, 18], [25, 20]]); },
      // tactical: a deck gun, its barrel up to the right over a wheeled carriage
      tactical: () => { L([[14, 31], [40, 11]], 5.5); F([[7, 30], [27, 30], [30, 38], [5, 38]]); C(11, 40, 4, 2.4); C(25, 40, 4, 2.4); },
      // point defence: a shield with a cross of shot on it
      pd: () => { L([[24, 5], [39, 10], [37, 27], [24, 43], [11, 27], [9, 10], [24, 5]], 3); L([[24, 15], [24, 31]], 2.4); L([[16, 22], [32, 22]], 2.4); },
      // science: an open eye, its iris a spiral (a crew studies by looking hard)
      science: () => { L(arcPts(24, 34, 20, 18, -P * 0.86, -P * 0.14, 12), 3); L(arcPts(24, 14, 20, 18, P * 0.14, P * 0.86, 12), 3); D(24, 24, 6.5); x.fillStyle = VC.ink; x.globalAlpha = lum(col) > 0.18 ? 0.8 : 0.4; x.beginPath(); x.arc(24, 24, 2.4, 0, 2 * P); x.fill(); x.globalAlpha = 1; },
      // comms: a speaking horn, two calls going out
      comms: () => { F([[6, 20], [14, 20], [28, 10], [28, 38], [14, 28], [6, 28]]); C(28, 24, 9, 2.6, -0.8, 0.8); C(28, 24, 16, 2.6, -0.7, 0.7); },
      // repair: a spanner, its jaw open to the upper right
      repair: () => { L([[9, 40], [27, 22]], 5.5); D(31, 17, 8.5); x.save(); x.globalCompositeOperation = 'destination-out'; x.fillRect(31, 12, 12, 7); x.restore(); },
      // transport: a figure, its lower half scattering into dabs as it beams
      transport: () => { D(24, 9, 4.6); F([[16, 29], [18, 17], [30, 17], [32, 29]]); for (const [px, py] of [[17, 34], [24, 35], [31, 34], [20, 40], [28, 40], [24, 44]]) D(px, py, 1.7); },
      // fleet: three hulls in formation, the lead ahead
      fleet: () => { const sh = (cx, cy, k) => F([[cx, cy - 10 * k], [cx + 6 * k, cy + 2 * k], [cx + 4 * k, cy + 9 * k], [cx - 4 * k, cy + 9 * k], [cx - 6 * k, cy + 2 * k]]); sh(24, 13, 1); sh(11, 33, 0.85); sh(37, 33, 0.85); },
      // the ship from above: a spiked salvage hull, its prow forward
      ship: () => { F([[24, 4], [30, 16], [38, 12], [33, 26], [40, 38], [29, 34], [24, 44], [19, 34], [8, 38], [15, 26], [10, 12], [18, 16]]); },
      // shields: a hull inside six crooked arcs
      shields: () => { for (let i = 0; i < 6; i++) { const a = -P / 2 + i * P / 3; C(24, 24, 19, 3.2, a - 0.42, a + 0.42); } D(24, 24, 5.5); },
      // sensors: a scope, its sweep and two marks
      sensors: () => { C(24, 24, 18, 2.8); L([[24, 24], [36, 11]], 2.6); D(31, 31, 2.8); D(14, 17, 2.8); },
      // tractor: a grapnel line from an emitter, hooked round a hull
      tractor: () => { F([[3, 19], [12, 19], [12, 29], [3, 29]]); L([[12, 24], [30, 24]], 2.4); L([[30, 24], ...arcPts(36, 24, 8, 9, P, 2.2 * P, 8)], 3); L([[30, 24], ...arcPts(36, 24, 8, 9, P, -0.2 * P, 8)], 3); },
      // craft: a little skiff with swept wings
      craft: () => { F([[24, 4], [29, 21], [44, 31], [29, 32], [24, 44], [19, 32], [4, 31], [19, 21]]); },
      // weapons: a shot from a muzzle, ending in a burst
      weapons: () => { D(9, 39, 4.6); L([[12, 36], [29, 19]], 3.2); for (let i = 0; i < 6; i++) { const a = i * P / 3; L([[34 + Math.cos(a) * 4, 14 + Math.sin(a) * 4], [34 + Math.cos(a) * 10, 14 + Math.sin(a) * 10]], 2.2); } },
      // warp: a chevron trailing speed strokes
      warp: () => { L([[22, 10], [38, 24], [22, 38]], 4.6); L([[5, 16], [17, 16]], 2.6); L([[3, 24], [20, 24]], 2.6); L([[5, 32], [17, 32]], 2.6); },
      // engines: a nozzle and its exhaust curling away
      engines: () => { F([[14, 5], [34, 5], [38, 20], [10, 20]]); spiral(x, 19, 33, 6, 1.3, R, 2.2, col); spiral(x, 31, 35, 5, 1.2, R, 2.2, col); },
      // reactor: a solid core in two crossed orbits
      reactor: () => { D(24, 24, 5.5); for (const r of [P / 4, -P / 4]) { const o = arcPts(0, 0, 19, 8, 0, 2 * P, 24).map(([a, b]) => [24 + a * Math.cos(r) - b * Math.sin(r), 24 + a * Math.sin(r) + b * Math.cos(r)]); ink(x, wob(o, true, 0.4, R, 2), true, 2.4, col); } },
      // map: a dotted course to an X (the chart they stole)
      map: () => { for (const [px, py] of [[8, 41], [11, 34], [13, 27], [18, 21], [24, 18]]) D(px, py, 2.2); L([[29, 6], [41, 18]], 3.6); L([[41, 6], [29, 18]], 3.6); },
      pause: () => { L([[17, 10], [17, 38]], 6.5); L([[31, 10], [31, 38]], 6.5); },
      menu: () => { for (const y of [13, 24, 35]) L([[9, y], [39, y]], 4.2); },
      // fire: four claw brackets closing on a solid mark
      fire: () => { for (const [a, b, c] of [[[8, 18], [8, 8], [18, 8]], [[30, 8], [40, 8], [40, 18]], [[40, 30], [40, 40], [30, 40]], [[18, 40], [8, 40], [8, 30]]]) L([a, b, c], 3.4); D(24, 24, 5); },
      // cargo: a loot chest, its lid arched, a coin on it
      cargo: () => { L([[7, 22], [41, 22], [41, 41], [7, 41], [7, 22]], 3); L(arcPts(24, 22, 17, 10, P, 2 * P, 10), 3); D(24, 30, 3.4); },
      // settings: a crooked cog round a spiral bore
      settings: () => { const o = []; for (let i = 0; i <= 16; i++) { const a = i * 2 * P / 16, r = i % 2 ? 13 : 19; o.push([24 + Math.cos(a) * r, 24 + Math.sin(a) * r]); } L(o, 3); spiral(x, 24, 24, 6, 1.4, R, 2.2, col); },
    })[name]();
  });
}
const ICON_NAMES = ['helm', 'power', 'tactical', 'pd', 'science', 'comms', 'repair', 'transport', 'fleet', 'ship', 'shields', 'sensors', 'tractor', 'craft', 'weapons', 'warp', 'engines', 'reactor', 'map', 'pause', 'menu', 'fire', 'cargo', 'settings'];

// ---- Live readouts (the console skins page and the battle console) ---------------------------------------------
const live = {
  arrow: (x, a) => { const R = rng(hash('helm' + a)); ink(x, wob([[-12, 2], [10, -1]], false, 0.8, R), false, 3, VC.ink); ink(x, wob([[2, -8], [11, -1], [3, 7]], false, 0.6, R), false, 3, VC.ink); },
  ssd: (x, cx, cy, sh, col) => {
    const R = rng(7);
    for (let i = 0; i < 6; i++) { const a = -P / 2 + i * P / 3; ink(x, wob(arcPts(cx, cy, 96, 92, a - P / 6 + 0.12, a + P / 6 - 0.12, 10), false, 0.9, R), false, 6, col(sh[i])); }
    const hull = wob([[cx, cy - 70], [cx + 16, cy - 40], [cx + 40, cy - 56], [cx + 26, cy - 10], [cx + 46, cy + 30], [cx + 14, cy + 22], [cx, cy + 62], [cx - 14, cy + 22], [cx - 46, cy + 30], [cx - 26, cy - 10], [cx - 40, cy - 56], [cx - 16, cy - 40]], true, 1, R);
    ink(x, hull, true, 2.4); for (const [mx, my] of [[cx, cy - 46], [cx - 30, cy + 18], [cx + 30, cy + 18]]) spiral(x, mx, my, 6, 1.5, R, 1.6, VC.blood);
  },
  dial: (x, c, cts) => {
    const [dx, dy] = cts[0], L = Math.hypot(dx, dy), ix = c + dx / L * 12, iy = c + dy / L * 7, R = rng(3);
    const iris = wob(arcPts(ix, iy, 16, 16, 0, 2 * P, 20), true, 0.8, R); wash(x, iris, VC.verd, R); ink(x, iris, true, 2); spiral(x, ix, iy, 9, 1.7, R, 1.4); x.fillStyle = VC.ink; x.beginPath(); x.arc(ix + 1, iy, 4, 0, 2 * P); x.fill();
  },
  // The target's brackets: four claw strokes in bone, the same strokes every frame (one seed)
  bracket: (x, bx, by, bw, bh) => { const Rr = rng(11); for (const [px, py, sx, sy] of [[bx, by, 1, 1], [bx + bw, by, -1, 1], [bx, by + bh, 1, -1], [bx + bw, by + bh, -1, -1]]) ink(x, wob([[px + sx * 44, py + sy * 2], [px, py], [px + sx * 3, py + sy * 44]], false, 1.4, Rr), false, 3.4, VC.bone); },
  // STOP lettered in the wheel's hub (the skins page's helm)
  helmCentre: (x, c) => { x.save(); x.fillStyle = VC.ink; x.font = '400 17px "Permanent Marker"'; x.textAlign = 'center'; x.fillText('Stop', c, c + 6); x.restore(); },
};

// ---- The battle console's own pieces ------------------------------------------------------------------------------
// The console draws its square keys, wide keys, allocation cells and title tickets from kit pieces (rendered in the
// modelled skins); the Syndicate's are drawn here by the same hand as the rest of its kit. A key is a square tag with an
// eyelet in its upper left corner; its state is the wash inside the ink, the same strokes in every state (one seed per
// piece), so lighting a key changes only its colour: dark wine at rest, pale verdigris on hover, old-coin verdigris engaged
// and whole, gold damaged, blood down, near black off.
const STATE = { idle: '#3a1a12', hover: CC.sky, on: CC.accord, ok: CC.accord, warn: CC.caution, alert: CC.alert, off: '#1e0d09' };
function squareTag(X, Y, W, H) { return [[X + 9, Y + 2], [X + W - 4, Y + 3], [X + W - 2, Y + H - 6], [X + W - 7, Y + H - 2], [X + 3, Y + H - 3], [X + 2, Y + 10]]; }
function keyPaint(state) {
  return (x, w, h) => {
    const R = rng(hash('vos.key')), pts = wob(squareTag(1, 1, w - 2, h - 2), true, 0.7, R, 2), dark = state === 'idle' || state === 'off';
    wash(x, pts, STATE[state], R, dark ? 'rgba(0,0,0,0.25)' : 'rgba(42,20,16,0.18)', dark ? 'rgba(255,200,160,0.05)' : 'rgba(255,248,220,0.2)');
    hatch(x, pts, R, 0.62, 3.4, dark ? 'rgba(0,0,0,0.45)' : 'rgba(42,20,16,0.4)'); if (!dark) dry(x, pts, R);
    ink(x, pts, true, 2, state === 'off' ? '#5a3a2a' : dark ? VC.brass : VC.ink);
    x.fillStyle = VC.wine; x.beginPath(); x.arc(8, 8, 2.4, 0, 2 * P); x.fill();
  };
}
// The wide key: the claw tag of the kit's buttons in the key's light.
function keywPaint(state) { return (x, w, h) => { const R = rng(hash('vos.keyw')), pts = wob(clawTag(1, 1, w - 2, h - 2), true, 1.0, R); const dark = state === 'idle' || state === 'off';
  wash(x, pts, STATE[state], R, dark ? 'rgba(0,0,0,0.25)' : 'rgba(42,20,16,0.18)', dark ? 'rgba(255,200,160,0.05)' : 'rgba(255,248,220,0.2)'); hatch(x, pts, R, 0.45); if (!dark) dry(x, pts, R);
  ink(x, pts, true, 2.2, state === 'off' ? '#5a3a2a' : dark ? VC.brass : VC.ink); x.fillStyle = VC.wine; x.beginPath(); x.arc(12, h / 2, 4, 0, 2 * P); x.fill(); }; }
// An allocation cell: a daub of paint inside an inked box, one per energy point; an empty one inked in faded brass so it
// shows on the dark wash.
function cellPaint(col, locked) {
  return (x, w, h) => { const R = rng(hash('vos.cell')), pts = wob(rrPts(2, 2, w - 4, h - 4, 5), true, 0.6, R, 2);
    if (locked) { hatch(x, pts, R, 0, 3, 'rgba(207,174,140,0.45)'); ink(x, pts, true, 1.6, '#7a5040'); return; }
    wash(x, pts, col, R, 'rgba(0,0,0,0.22)', 'rgba(255,240,210,0.16)'); ink(x, pts, true, 1.8, lum(col) < 0.1 ? '#9a7450' : VC.ink); };
}
// The title ticket: a dark wine ticket torn at both ends, inked in brass, its title in brass over it.
function ticket(x, w, h) {
  const R = rng(hash('vos.ticket')), top = [], bot = [];
  for (let i = 0; i <= 10; i++) { top.push([10 + i * (w - 20) / 10, 1.5 + R() * 1.2]); bot.push([w - 10 - i * (w - 20) / 10, h - 1.5 - R() * 1.2]); }
  const pts = wob([...top, [w - 2, h * 0.3], [w - 8, h * 0.5], [w - 2, h * 0.72], ...bot, [2, h * 0.72], [8, h * 0.5], [2, h * 0.3]], true, 0.5, R, 3);
  wash(x, pts, '#2a120c', R, 'rgba(0,0,0,0.3)', 'rgba(255,200,160,0.05)'); ink(x, pts, true, 1.6, VC.brass);
}
// The ground behind the column: patched sailcloth, stained dark, its patches stitched on in ink, a crew's spiral painted
// faint in one corner (CLAUDE.md section 7: the column has a painted ground of its own, never the black of a flat skin).
function sailcloth(x, w, h) {
  const R = rng(hash('vos.backing'));
  const all = [[0, 0], [w, 0], [w, h], [0, h]]; wash(x, all, '#1c0c08', R, 'rgba(0,0,0,0.35)', 'rgba(255,190,150,0.05)');
  // patches laid in a loose grid, each its own rough quad and wash, stitched round
  for (let j = 0; j < 7; j++) for (let i = 0; i < 2; i++) {
    if (R() < 0.35) continue;
    const pw = 150 + R() * 110, ph = 110 + R() * 90, px = i * 240 + R() * 60 - 20, py = j * 160 + R() * 50 - 20;
    const q = wob([[px, py + R() * 8], [px + pw, py + R() * 8], [px + pw - R() * 8, py + ph], [px + R() * 8, py + ph]], true, 1.6, R, 4);
    wash(x, q, ['#2c140c', '#24100c', '#301810', '#1e0e0a'][Math.floor(R() * 4)], R, 'rgba(0,0,0,0.3)', 'rgba(255,190,150,0.05)');
    x.save(); x.setLineDash([5, 4]); trace(x, q, true); x.strokeStyle = 'rgba(214,160,67,0.35)'; x.lineWidth = 1.2; x.stroke(); x.restore();
  }
  hatch(x, all, R, 0.8, 6, 'rgba(0,0,0,0.35)');
  x.save(); x.globalAlpha = 0.18; spiral(x, w - 70, h - 90, 46, 3, R, 4, VC.brass); x.restore();
}

// ---- The kit ------------------------------------------------------------------------------------------------------
// Every piece drawn from its own seed ('vos.<piece>'), with the skins page's painters for the pieces both pages draw.
const kit = {}, hero = {}, heroSize = {}, cells = {};
const seeded = (id, draw) => (x, w, h) => draw(x, w, h, rng(hash('vos.' + id)));
for (const [id, k] of Object.entries(vosKit)) kit[id] = { size: k.size, m: k.m, inset: k.inset || [3, 3], draw: seeded(id, k.draw) };
for (const [id, d] of Object.entries(vosHero)) hero[id] = seeded(id, d);
for (const s of Object.keys(STATE)) { hero['key.' + s] = keyPaint(s); heroSize['key.' + s] = [48, 48]; kit['keyw.' + s] = { size: [144, 48], m: [0, 40, 0, 26], draw: keywPaint(s) }; }
hero['console.backing'] = sailcloth; heroSize['console.backing'] = [480, 1080];
Object.assign(cells, {
  'cell.idle': { size: [32, 28], m: [10, 10, 10, 10], draw: cellPaint('#3a1a12') }, 'cell.lit': { size: [32, 28], m: [10, 10, 10, 10], draw: cellPaint(CC.sky) },
  'cell.warn': { size: [32, 28], m: [10, 10, 10, 10], draw: cellPaint(CC.alert) }, 'cell.accord': { size: [32, 28], m: [10, 10, 10, 10], draw: cellPaint(CC.accord) },
  'cell.locked': { size: [32, 28], m: [10, 10, 10, 10], draw: cellPaint(null, true) },
  'bar.fill.accord': { size: [64, 10], m: [0, 10, 0, 6], draw: seeded('bar.fill.accord', (x, w, h, R) => vosStroke(x, w, h, R, CC.accord)) },
  'plate.title': { size: [140, 24], m: [0, 18, 0, 18], draw: ticket },
});
// The crew's mark wherever the console draws one (a portrait, the holder of a ship): the crest, r its radius.
function emblem(G, cx, cy, r) { G.save(); G.translate(cx - r, cy - r); G.scale(2 * r / 96, 2 * r / 96); hero.crest(G, 96, 96); G.restore(); }
// Drawn keys and knobs (the slider handles, the expand key): a lopsided rounded box, its corners cut unevenly by hand, and
// a knob a little out of round. Fixed per size, so nothing wobbles from frame to frame.
function rr(G, X, Y, W, H, r) { G.beginPath(); if (!(W > 0 && H > 0)) { G.rect(X, Y, W, H); return; } const q = Math.min(Array.isArray(r) ? Math.max(...r) : (+r || 0), W / 2, H / 2); G.roundRect(X, Y, W, H, [q * 1.0, q * 0.55, q * 1.0, q * 0.7]); }
function knob(G, cx, cy, r) { G.beginPath(); G.ellipse(cx, cy, r * 1.04, r * 0.94, -0.3, 0, 2 * P); }

const api = {
  id: 'vos', name: 'Vos Syndicate', VC, CC, kit, hero, heroSize, cells, barCells: null, live, icon, ICON_NAMES, emblem, ink: inkOn, keyInk, font, fonts, load: () => Promise.resolve(), hexA, rr, knob,
  family: '"Patrick Hand", cursive',
  // The content inside a panel: inside the frame's wine field (its inked edge 21 px in), with the corner spirals clear.
  pad: [30, 26, 26, 26],
  // The name plate's live field: the bone scroll between its curled ends (60-400), clear of the Fleet key at 370.
  plate: { x: 60, y: 36, max: 300, font: '400 17px "Permanent Marker"', col: VC.ink },
  // The battle's helm ring under the ship, in verdigris.
  ring: ['rgba(63,183,163,0)', 'rgba(63,183,163,0.14)', 'rgba(140,230,210,0.9)'],
  // Names for the player's side where the page shows it as the Syndicate: ships named as a crew names its luck and its
  // debts, captains called Mother and Father by their crews (the scenario's own Vos raider is Mother Skeel). Placeholders
  // until the owner sets the Syndicate's culture (CLAUDE.md section 1; openspec/changes/vos-battle-console, open question V1).
  player: { faction: 'Vos Syndicate', call: 'Captain', trim: 0xe3a33a, hull: 0xb39a6e,
    ships: { resolute: ['Grinning Debt', 'Mother Ysa'], tallis: ['Lucky Rake', 'Bosun Tibb'], meridian: ['Second Helping', 'Father Crane'] },
    ranks: ['Deckhand', 'Bosun', 'Mate', 'Quartermaster'],
    officers: { o1: 'Mags Orrow', o2: 'Tibb Calloran', o3: 'Nell Sparrowgate', o4: 'Dace Hollin', o5: 'Brannoc Wey', o6: 'Lise Merrow', o7: 'Oskar Tallow',
      o8: 'Pim Rattray', o9: 'Hesk Dunmore', o10: 'Ivo Scarrow', o11: 'Rena Gallick', o12: 'Wyn Boddle', o13: 'Corra Fenwick' } },
  link: VC.brassHi,
  // Labels centred on the tags, clear of the eyelet on the left and the claw on the right (the kit's 9-slice caps).
  keyCenter: true,
  // A hover name on the brass tag lit, in ink.
  tip: ['button.hover', 'hover'],
  // Icon tabs (the station and ship rails) on the square tag key, the icon inside the ink.
  iconKeys: true,
  // Panel tab icons at rest in bone, apart from the functional icons' verdigris; glyphs on a dark tag in brass.
  tabIcon: VC.bone, glyphOnDark: VC.brassHi, mark: { hover: VC.brassHi, ready: CC.accord, count: VC.bone },
  // The strips (strip.top and strip.bottom, 72 px planks) at their full height; their content, laid out for 56 px, moved
  // down into the plank's open band.
  stripTop: { h: 72, dy: 8 }, stripBottom: { h: 72, dy: 8 },
  // the skins page's painters and helpers (docs/mockups/console-skins.html takes these, one copy)
  painters: { kit: vosKit, hero: vosHero }, draw: { vos, noise1, wob, arcPts, rrPts, trace, ink, wash, hatch, dry, spiral, body, clawTag },
};
window.FCSkins = window.FCSkins || {}; window.FCSkins.vos = api;
return api;
})();
