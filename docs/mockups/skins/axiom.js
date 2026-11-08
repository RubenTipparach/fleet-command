// The Axiom skin (mockup 4, OpenSpec change console-skins): a purely mathematical faction (owner, 2026-11-07: "Id like a
// faction that is purely mathematical. A faction based on fractal patterns and designs."). Name and culture are ours and
// provisional until the owner sets them on the survey. Their creed: every true thing is a rule and its iteration, and a
// structure is honoured by how deep its rule has been carried. They keep three schools at once, and each one owns part of
// the console: the escape-time school (boundary art of iterated maps) owns the screens, the tilers (self-similar and
// substitution tilings) own the frames and plates, and the branchers (rewriting systems) own the icons and marks.
// Nothing is drawn from their bodies (CLAUDE.md section 1): their bodies are not described and shape nothing here.
//
// Method: flat and exact, drawn by the mathematics itself in canvas, with no painted material or depth (CLAUDE.md
// section 7: vector for flat styles). Thin lines and filled regions on the void, glowing a little; the depth of an
// iteration is shown by its colour, indigo for shallow, cyan for deep. Every shape comes from an explicit rule (Koch
// edges, Sierpinski triangles and carpets, Hilbert curves, Cantor sets, the dragon curve, a Julia set computed per
// pixel, golden-ratio proportions), so every piece is deterministic and there is no randomness at all.
// The key is the Koch key: an elongated hexagon whose short ends are Koch edges, one level deeper on hover, filled solid
// when pressed. Registers itself with the page's skin registry (docs/mockups/skins). Our own design throughout.
(function () {
'use strict';
const F = window.FCK, P = F.P;
const AX = { void: '#05060c', plate: '#080a18', proof: '#e8ecf6', dim: '#9aa8e0', indigo: '#4b3fd8', indigoHi: '#6a5cf0', cyan: '#3fe0e6',
  gold: '#f2c14e', magenta: '#ff3d8b', solid: '#2f2696' };
const PHI = (1 + Math.sqrt(5)) / 2;
function hexA(hex, a) { const n = parseInt(hex.slice(1), 16); return `rgba(${n >> 16},${(n >> 8) & 255},${n & 255},${a})`; }
// The colour of an iteration depth t in [0, 1]: indigo (shallow) to cyan (deep).
function band(t, a = 1) {
  const A = [0x4b, 0x3f, 0xd8], B = [0x3f, 0xe0, 0xe6], k = Math.max(0, Math.min(1, t));
  return `rgba(${A.map((v, i) => Math.round(v + (B[i] - v) * k)).join(',')},${a})`;
}

// ---- the rules ---------------------------------------------------------------------------------------------------
// A Koch edge from a to b, d levels deep, its bumps on the side s (1: left of a->b, which is outward for a polygon wound
// clockwise on screen). Returns the points from a up to, but not including, b.
function koch(a, b, d, s = 1) {
  if (d <= 0) return [a];
  const dx = b[0] - a[0], dy = b[1] - a[1], L = Math.hypot(dx, dy), hgt = L / 3 * Math.sqrt(3) / 2 * s;
  const p1 = [a[0] + dx / 3, a[1] + dy / 3], p3 = [a[0] + 2 * dx / 3, a[1] + 2 * dy / 3];
  const ap = [(a[0] + b[0]) / 2 + dy / L * hgt, (a[1] + b[1]) / 2 - dx / L * hgt];
  return [...koch(a, p1, d - 1, s), ...koch(p1, ap, d - 1, s), ...koch(ap, p3, d - 1, s), ...koch(p3, b, d - 1, s)];
}
// A closed polygon whose every edge is a Koch edge (wound clockwise on screen, so s = 1 bumps outward, -1 inward).
function kochPoly(verts, d, s = 1) { const o = []; for (let i = 0; i < verts.length; i++) o.push(...koch(verts[i], verts[(i + 1) % verts.length], d, s)); return o; }
// The Koch snowflake of circumradius r round (cx, cy), d levels deep, one point up.
function snowflake(cx, cy, r, d) { const v = [0, 1, 2].map((i) => [cx + Math.cos(-P / 2 + i * 2 * P / 3) * r / 1, cy + Math.sin(-P / 2 + i * 2 * P / 3) * r]); return kochPoly(v, d, 1); }
// The Koch key: an elongated hexagon whose short ends are Koch edges. Its ends are e px long (tip to corner) and its
// long sides are plain, so it stretches along its length.
function kochKey(X, Y, W, H, d, e) {
  const cy = Y + H / 2, tl = [X + e, Y], tr = [X + W - e, Y], rt = [X + W, cy], br = [X + W - e, Y + H], bl = [X + e, Y + H], lt = [X, cy];
  return [...koch(lt, tl, d), tl, ...koch(tr, rt, d), ...koch(rt, br, d), br, ...koch(bl, lt, d)];
}
// The Cantor set on [x0, x1], d levels deep: the segments that remain.
function cantor(x0, x1, d) { if (d <= 0) return [[x0, x1]]; const t = (x1 - x0) / 3; return [...cantor(x0, x0 + t, d - 1), ...cantor(x1 - t, x1, d - 1)]; }
// A Sierpinski triangle on the triangle (a, b, c), d levels deep: the filled triangles.
function sierpinski(a, b, c, d) {
  if (d <= 0) return [[a, b, c]];
  const m = (p, q) => [(p[0] + q[0]) / 2, (p[1] + q[1]) / 2], ab = m(a, b), bc = m(b, c), ca = m(c, a);
  return [...sierpinski(a, ab, ca, d - 1), ...sierpinski(ab, b, bc, d - 1), ...sierpinski(ca, bc, c, d - 1)];
}
// The Hilbert curve of order n in the unit square, as 4^n points (the classic index-to-point rule).
function hilbert(n) {
  const N = 1 << n, out = [];
  for (let i = 0; i < N * N; i++) {
    let t = i, x = 0, y = 0;
    for (let s = 1; s < N; s *= 2) {
      const rx = 1 & (t / 2), ry = 1 & (t ^ rx);
      if (ry === 0) { if (rx === 1) { x = s - 1 - x; y = s - 1 - y; } const q = x; x = y; y = q; }
      x += s * rx; y += s * ry; t = Math.floor(t / 4);
    }
    out.push([(x + 0.5) / N, (y + 0.5) / N]);
  }
  return out;
}
// The Heighway dragon of n folds: the turn sequence by the regular paper-folding rule, as points on a unit grid.
function dragon(n) {
  const pts = [[0, 0]]; let x = 0, y = 0, dir = 0;
  const steps = [[1, 0], [0, 1], [-1, 0], [0, -1]];
  for (let i = 1; i <= (1 << n); i++) {
    x += steps[dir][0]; y += steps[dir][1]; pts.push([x, y]);
    const k = i & -i, turn = (((i / k) - 1) / 2) % 2 === 0 ? 1 : -1;   // the paper-folding sequence
    dir = (dir + turn + 4) % 4;
  }
  return pts;
}

// ---- drawing -----------------------------------------------------------------------------------------------------
function path(x, pts, closed = true) { x.beginPath(); x.moveTo(pts[0][0], pts[0][1]); for (let i = 1; i < pts.length; i++) x.lineTo(pts[i][0], pts[i][1]); if (closed) x.closePath(); }
// A line of light: a soft glow and an exact core.
function glow(x, pts, col, lw = 1.2, blur = 4, closed = true) {
  x.save(); x.lineJoin = 'miter'; x.miterLimit = 3; x.shadowColor = col; x.shadowBlur = blur; x.strokeStyle = col; x.lineWidth = lw; path(x, pts, closed); x.stroke(); x.restore();
}
function fillTris(x, tris, col, blur = 0) { x.save(); x.fillStyle = col; x.shadowColor = col; x.shadowBlur = blur; x.beginPath(); for (const [a, b, c] of tris) { x.moveTo(a[0], a[1]); x.lineTo(b[0], b[1]); x.lineTo(c[0], c[1]); x.closePath(); } x.fill(); x.restore(); }
// A Cantor rule: the set's segments as one line at height y.
function cantorLine(x, x0, x1, y, d, col, lw = 2, blur = 3) { x.save(); x.strokeStyle = col; x.lineWidth = lw; x.lineCap = 'butt'; x.shadowColor = col; x.shadowBlur = blur; x.beginPath(); for (const [a, b] of cantor(x0, x1, d)) { x.moveTo(a, y); x.lineTo(b, y); } x.stroke(); x.restore(); }
// A Sierpinski corner: the right triangle whose right angle sits at (cx, cy), legs of n px along sx and sy.
function sierCorner(x, cx, cy, n, sx, sy, d, col, blur = 3) { fillTris(x, sierpinski([cx, cy], [cx + sx * n, cy], [cx, cy + sy * n], d), col, blur); }
// A Hilbert trace filling the n px square at (X, Y).
function hilbertTrace(x, X, Y, n, order, col, lw = 1) { glow(x, hilbert(order).map(([u, v]) => [X + u * n, Y + v * n]), col, lw, 2, false); }

// ---- the kit -----------------------------------------------------------------------------------------------------
// The Koch key in a state: idle a thin outline round a faint fill, hover one level deeper and brighter with the shallower
// rule left faintly behind it, pressed filled solid inside a gold outline (the statement proved).
const KEY_E = 22;
function keyState(st) {
  return (x, w, h) => {
    const d = st === 'idle' ? 1 : 2, pts = kochKey(3, 7, w - 6, h - 14, d, KEY_E);
    x.fillStyle = st === 'pressed' ? AX.solid : st === 'hover' ? hexA(AX.cyan, 0.13) : hexA(AX.indigo, 0.16); path(x, pts); x.fill();
    if (st !== 'idle') glow(x, kochKey(3, 7, w - 6, h - 14, 1, KEY_E), hexA(AX.indigoHi, 0.45), 0.8, 0);
    glow(x, pts, st === 'pressed' ? AX.gold : st === 'hover' ? AX.cyan : AX.indigoHi, st === 'idle' ? 1.2 : 1.5, st === 'idle' ? 2 : 6);
  };
}
// A tab: an open bracket whose foot is a Cantor rule; the chosen tab's rule turns gold and its head closes.
function tabState(on) {
  return (x, w, h) => {
    x.fillStyle = on ? hexA(AX.gold, 0.10) : hexA(AX.indigo, 0.10); x.fillRect(3, 4, w - 6, h - 8);
    const col = on ? AX.gold : AX.indigoHi;
    glow(x, [[3.5, 10], [3.5, h - 4.5], [12, h - 4.5]], col, 1.2, 2, false); glow(x, [[w - 3.5, 10], [w - 3.5, h - 4.5], [w - 12, h - 4.5]], col, 1.2, 2, false);
    glow(x, [[12, h - 4.5], [w - 12, h - 4.5]], hexA(col, 0.6), 0.8, 0, false);
    cantorLine(x, 3, 13, h - 4.5, 2, col, on ? 2.6 : 1.8, on ? 5 : 2); cantorLine(x, w - 13, w - 3, h - 4.5, 2, col, on ? 2.6 : 1.8, on ? 5 : 2);
    if (on) glow(x, [[3.5, 4.5], [w - 3.5, 4.5]], hexA(AX.gold, 0.7), 1, 3, false);
  };
}
// The tilers' frame: a fine rule round a plain plate, a Sierpinski triangle in each corner.
function framePanel(x, w, h) {
  x.fillStyle = hexA('#080a18', 0.94); x.fillRect(4, 4, w - 8, h - 8);
  glow(x, [[4.5, 4.5], [w - 4.5, 4.5], [w - 4.5, h - 4.5], [4.5, h - 4.5]], AX.indigoHi, 1, 2);
  for (const [cx, cy, sx, sy] of [[4.5, 4.5, 1, 1], [w - 4.5, 4.5, -1, 1], [4.5, h - 4.5, 1, -1], [w - 4.5, h - 4.5, -1, -1]]) sierCorner(x, cx, cy, 19, sx, sy, 3, AX.cyan, 3);
}
function frameInset(x, w, h) {
  x.fillStyle = hexA('#03040a', 0.9); x.fillRect(2, 2, w - 4, h - 4);
  glow(x, [[2.5, 2.5], [w - 2.5, 2.5], [w - 2.5, h - 2.5], [2.5, h - 2.5]], hexA(AX.indigoHi, 0.8), 1, 1);
  for (const [X, Y] of [[0, 0], [w - 10, 0], [0, h - 10], [w - 10, h - 10]]) hilbertTrace(x, X + 1, Y + 1, 8, 2, AX.cyan, 0.9);
}
// The strips: a fine rule along the inner edge; at the left end the Cantor set iterated row by row (its depth ladder),
// at the right the Koch curve iterated row by row. The open band between them is plain.
function strip(x, w, h, flip) {
  x.save(); if (flip) { x.translate(0, h); x.scale(1, -1); }
  x.fillStyle = hexA('#06081a', 0.96); x.fillRect(0, 0, w, h - 4);
  glow(x, [[0, h - 4.5], [w, h - 4.5]], AX.indigoHi, 1.2, 3, false);
  glow(x, [[0, 1.5], [w, 1.5]], hexA(AX.indigo, 0.5), 0.8, 0, false);
  for (let d = 0; d < 5; d++) cantorLine(x, 12, 102, 13 + d * 11, d, band(d / 4), 2.4, 3);
  for (let d = 0; d < 4; d++) glow(x, [...koch([w - 102, 56], [w - 12, 56], d), [w - 12, 56]], band(d / 3, 0.55 + d * 0.15), d === 3 ? 1.4 : 1, d === 3 ? 4 : 0, false);
  x.restore();
}
const nine = {
  'frame.panel': { size: [160, 120], m: [26, 26, 26, 26], draw: framePanel },
  'frame.inset': { size: [64, 48], m: [12, 12, 12, 12], draw: frameInset },
  'bar.track': { size: [96, 20], m: [0, 12, 0, 12], inset: [8, 4], draw: (x, w, h) => {
    x.fillStyle = hexA('#03040a', 0.95); x.fillRect(4, 2, w - 8, h - 4);
    glow(x, [[4.5, 2.5], [w - 4.5, 2.5], [w - 4.5, h - 2.5], [4.5, h - 2.5]], hexA(AX.indigoHi, 0.85), 1, 1);
    x.save(); x.strokeStyle = AX.cyan; x.lineWidth = 1.4; x.lineCap = 'butt'; x.beginPath(); for (const [a, b] of cantor(2, h - 2, 2)) { x.moveTo(1.5, a); x.lineTo(1.5, b); x.moveTo(w - 1.5, a); x.lineTo(w - 1.5, b); } x.stroke(); x.restore();
  } },
  'bar.fill': { size: [64, 10], m: [0, 8, 0, 8], draw: (x, w, h) => fillBar(x, w, h, AX.cyan) },
  'bar.fill.red': { size: [64, 10], m: [0, 8, 0, 8], draw: (x, w, h) => fillBar(x, w, h, AX.magenta) },
  // Symbol bars: a row of Sierpinski carpet cells (one level: eight squares round an empty centre), the tilers' plate.
  'bar.cell.off': { size: [12, 12], gap: 3, draw: (x, w, h) => carpet(x, w, h, hexA(AX.indigoHi, 0.75), false) },
  'bar.cell.on': { size: [12, 12], gap: 3, draw: (x, w, h) => carpet(x, w, h, AX.cyan, true) },
  'bar.cell.warn': { size: [12, 12], gap: 3, draw: (x, w, h) => carpet(x, w, h, AX.magenta, true) },
  'button.idle': { size: [160, 48], m: [20, 28, 20, 28], draw: keyState('idle') },
  'button.hover': { size: [160, 48], m: [20, 28, 20, 28], draw: keyState('hover') },
  'button.pressed': { size: [160, 48], m: [20, 28, 20, 28], draw: keyState('pressed') },
  'tab.idle': { size: [120, 40], m: [12, 14, 12, 14], draw: tabState(false) },
  'tab.active': { size: [120, 40], m: [12, 14, 12, 14], draw: tabState(true) },
  'strip.top': { size: [512, 72], m: [12, 116, 12, 116], draw: (x, w, h) => strip(x, w, h, false) },
  'strip.bottom': { size: [512, 72], m: [12, 116, 12, 116], draw: (x, w, h) => strip(x, w, h, true) },
  'divider': { size: [128, 10], m: [0, 24, 0, 24], draw: (x, w, h) => {
    glow(x, [[0, 5], [w, 5]], hexA(AX.indigoHi, 0.8), 1, 1, false);
    cantorLine(x, 1, 22, 5, 2, AX.cyan, 3, 3); cantorLine(x, w - 22, w - 1, 5, 2, AX.cyan, 3, 3);
  } },
};
function fillBar(x, w, h, col) {
  x.save(); x.shadowColor = col; x.shadowBlur = 4; x.fillStyle = hexA(col, 0.9); x.fillRect(1, 2, w - 2, h - 4); x.restore();
  x.fillStyle = AX.void; for (const [a, b] of [[1, 2], [w - 2, w - 1]]) x.fillRect(a, 2, b - a, h - 4);
}
function carpet(x, w, h, col, lit) {
  const s = Math.min(w, h) - 1, X = (w - s) / 2, Y = (h - s) / 2, c = s / 3;
  x.save(); if (lit) { x.shadowColor = col; x.shadowBlur = 3; }
  for (let j = 0; j < 3; j++) for (let i = 0; i < 3; i++) {
    if (i === 1 && j === 1) continue;
    if (lit) { x.fillStyle = col; x.fillRect(X + i * c + 0.3, Y + j * c + 0.3, c - 0.6, c - 0.6); }
    else { x.strokeStyle = col; x.lineWidth = 0.6; x.strokeRect(X + i * c + 0.6, Y + j * c + 0.6, c - 1.2, c - 1.2); }
  }
  x.restore();
}

// ---- hero pieces -------------------------------------------------------------------------------------------------
// The escape-time school's screen: the Julia set of z -> z^2 + c, coloured by how fast each point escapes (its depth of
// iteration), drawn faint behind a field so the live readouts stay first. Computed per device pixel.
function juliaField(x, X, Y, W, H, opt = {}) {
  const k = 2, cw = Math.round(W * k), ch = Math.round(H * k), off = document.createElement('canvas'); off.width = cw; off.height = ch;
  const o = off.getContext('2d'), img = o.createImageData(cw, ch), d = img.data;
  const [cr, ci] = opt.c || [-0.7269, 0.1889], MAX = opt.max || 72, span = opt.span || 1.6, alpha = opt.alpha ?? 0.34;
  const A = [0x4b, 0x3f, 0xd8], B = [0x3f, 0xe0, 0xe6];
  for (let py = 0; py < ch; py++) for (let px = 0; px < cw; px++) {
    let zr = (px / cw - 0.5) * 2 * span * (cw / ch), zi = (py / ch - 0.5) * 2 * span, n = 0;
    while (n < MAX && zr * zr + zi * zi < 16) { const t = zr * zr - zi * zi + cr; zi = 2 * zr * zi + ci; zr = t; n++; }
    const i = (py * cw + px) * 4;
    if (n >= MAX) { d[i] = A[0]; d[i + 1] = A[1]; d[i + 2] = A[2]; d[i + 3] = Math.round(255 * alpha * 0.18); continue; }
    const mu = Math.max(0, n + 1 - Math.log2(Math.log2(Math.sqrt(zr * zr + zi * zi) + 1e-9) + 1e-9)), v = Math.min(1, Math.log(mu + 1) / Math.log(MAX + 1));
    for (let j = 0; j < 3; j++) d[i + j] = Math.round(A[j] + (B[j] - A[j]) * v);
    d[i + 3] = Math.round(255 * alpha * Math.pow(v, 2.2));
  }
  o.putImageData(img, 0, 0); x.drawImage(off, X, Y, W, H);
}
const hero = {
  // The name plate: a long Koch key three levels deep at its ends, its middle a plain field for the captain's name.
  'nameplate': (x, w, h) => {
    const pts = kochKey(3, 5, w - 6, h - 10, 3, 42);
    x.fillStyle = hexA(AX.indigo, 0.14); path(x, pts); x.fill();
    glow(x, kochKey(3, 5, w - 6, h - 10, 2, 42), hexA(AX.indigo, 0.5), 0.8, 0);
    glow(x, pts, AX.cyan, 1.3, 5);
    glow(x, [[54, h - 10.5], [w - 54, h - 10.5]], hexA(AX.indigoHi, 0.6), 0.8, 0, false);
  },
  // The helm: a recursive ring. Eight Koch snowflakes round a larger one (the stop key, gold: the fixed point), on one
  // circle, inside a ring of Koch edges turned inward.
  'helm.pad': (x, w, h) => {
    const c = w / 2, ring = [];
    for (let i = 0; i < 24; i++) { const a = -P / 2 + i * 2 * P / 24; ring.push([c + Math.cos(a) * 144, c + Math.sin(a) * 144]); }
    glow(x, kochPoly(ring, 2, -1), hexA(AX.indigoHi, 0.75), 1, 2);
    glow(x, F.arcPts(c, c, 100, 100, 0, 2 * P, 96), hexA(AX.indigo, 0.6), 0.8, 0);
    for (let i = 0; i < 8; i++) {
      const a = -P / 2 + i * P / 4, kx = c + Math.cos(a) * 100, ky = c + Math.sin(a) * 100, s = snowflake(kx, ky, 30, 2);
      x.fillStyle = hexA(AX.indigo, 0.2); path(x, s); x.fill(); glow(x, s, AX.indigoHi, 1.2, 3);
    }
    const s = snowflake(c, c, 50, 3); x.fillStyle = hexA(AX.gold, 0.08); path(x, s); x.fill(); glow(x, s, AX.gold, 1.3, 5);
  },
  // The ship systems display: a screen of the escape-time school inside a tilers' frame.
  'ssd.bezel': (x, w, h) => {
    x.fillStyle = AX.void; x.fillRect(4, 4, w - 8, h - 8);
    juliaField(x, 10, 10, w - 20, h - 20, { alpha: 0.5, c: [-0.8, 0.156], span: 1.25, max: 96 });
    glow(x, [[4.5, 4.5], [w - 4.5, 4.5], [w - 4.5, h - 4.5], [4.5, h - 4.5]], AX.indigoHi, 1.2, 3);
    for (const [cx, cy, sx, sy] of [[4.5, 4.5, 1, 1], [w - 4.5, 4.5, -1, 1], [4.5, h - 4.5, 1, -1], [w - 4.5, h - 4.5, -1, -1]]) sierCorner(x, cx, cy, 40, sx, sy, 4, AX.cyan, 3);
  },
  // The dial: a ring cut by the Cantor set (three levels), two finer rings inside.
  'dial': (x, w) => {
    const c = w / 2; x.fillStyle = hexA('#03040a', 0.95); x.beginPath(); x.arc(c, c, 57, 0, 2 * P); x.fill();
    x.save(); x.strokeStyle = AX.cyan; x.lineWidth = 3; x.lineCap = 'butt'; x.shadowColor = AX.cyan; x.shadowBlur = 3;
    for (let q = 0; q < 4; q++) for (const [a, b] of cantor(q * P / 2, (q + 1) * P / 2, 2)) { x.beginPath(); x.arc(c, c, 54, a - P / 2, b - P / 2); x.stroke(); }
    x.restore();
    glow(x, F.arcPts(c, c, 36, 36, 0, 2 * P, 64), hexA(AX.indigoHi, 0.7), 0.9, 0);
    glow(x, F.arcPts(c, c, 36 / PHI / PHI, 36 / PHI / PHI, 0, 2 * P, 48), hexA(AX.indigoHi, 0.5), 0.8, 0);
  },
  'lamp.alert.green': (x, w) => lamp(x, w, AX.cyan), 'lamp.alert.yellow': (x, w) => lamp(x, w, AX.gold), 'lamp.alert.red': (x, w) => lamp(x, w, AX.magenta),
  // The Axiom mark (our own): a Heighway dragon of ten folds, in gold, inside a circle.
  'crest': (x, w) => {
    const c = w / 2, pts = dragon(10), xs = pts.map((p) => p[0]), ys = pts.map((p) => p[1]);
    const x0 = Math.min(...xs), x1 = Math.max(...xs), y0 = Math.min(...ys), y1 = Math.max(...ys), s = 56 / Math.max(x1 - x0, y1 - y0);
    x.fillStyle = AX.void; x.beginPath(); x.arc(c, c, 46, 0, 2 * P); x.fill();
    glow(x, F.arcPts(c, c, 45, 45, 0, 2 * P, 96), AX.cyan, 1.4, 4);
    glow(x, F.arcPts(c, c, 45 / PHI, 45 / PHI, 0, 2 * P, 64), hexA(AX.indigo, 0.5), 0.8, 0);
    glow(x, pts.map(([px, py]) => [c + (px - (x0 + x1) / 2) * s, c + (py - (y0 + y1) / 2) * s]), AX.gold, 0.9, 3, false);
  },
};
// An alert lamp: a Koch snowflake three levels deep, filled with its light.
function lamp(x, w, col) {
  const c = w / 2, s = snowflake(c, c + 2, 27, 3);
  x.save(); x.shadowColor = col; x.shadowBlur = 10; const g = x.createRadialGradient(c, c, 2, c, c, 26); g.addColorStop(0, '#ffffff'); g.addColorStop(0.35, col); g.addColorStop(1, hexA(col, 0.55));
  x.fillStyle = g; path(x, s); x.fill(); x.restore();
  glow(x, s, hexA('#ffffff', 0.7), 0.8, 0);
}

// ---- icons: the branchers' marks. One cyan ink, a 3 px line, solid for the thing, iterated strokes for what it gives off.
function icon(name) {
  return F.paint(48, 48, (x) => {
    const C = AX.cyan; x.strokeStyle = C; x.fillStyle = C; x.lineWidth = 3; x.shadowColor = C; x.shadowBlur = 3; x.lineCap = 'butt';
    const L = (pts, closed = false) => { path(x, pts, closed); x.stroke(); };
    const D = (cx, cy, r) => { x.beginPath(); x.arc(cx, cy, r, 0, 2 * P); x.fill(); };
    const O = (cx, cy, r) => { x.beginPath(); x.arc(cx, cy, r, 0, 2 * P); x.stroke(); };
    const CANT = (x0, y0, x1, y1, d) => { const L0 = Math.hypot(x1 - x0, y1 - y0); x.beginPath(); for (const [a, b] of cantor(0, L0, d)) { x.moveTo(x0 + (x1 - x0) * a / L0, y0 + (y1 - y0) * a / L0); x.lineTo(x0 + (x1 - x0) * b / L0, y0 + (y1 - y0) * b / L0); } x.stroke(); };
    ({
      // A ship's wheel: an exact rim, a solid hub, eight spokes to eight handles.
      helm: () => { O(24, 24, 13); D(24, 24, 4.5); for (let i = 0; i < 8; i++) { const a = i * P / 4, cs = Math.cos(a), sn = Math.sin(a); L([[24 + cs * 4, 24 + sn * 4], [24 + cs * 18, 24 + sn * 18]]); D(24 + cs * 19.5, 24 + sn * 19.5, 2.6); } },
      // A sight: a ring, its cross-hairs cut by the Cantor set, the mark solid at the centre.
      fire: () => { O(24, 24, 15); x.lineWidth = 2.6; CANT(24, 2, 24, 20, 1); CANT(24, 28, 24, 46, 1); CANT(2, 24, 20, 24, 1); CANT(28, 24, 46, 24, 1); D(24, 24, 3.5); },
      // Shields: six arcs, each cut by the Cantor set, round a solid ship.
      shields: () => { x.lineWidth = 4; for (let i = 0; i < 6; i++) { const a = -P / 2 + i * P / 3; for (const [p, q] of cantor(a - P / 6 + 0.14, a + P / 6 - 0.14, 1)) { x.beginPath(); x.arc(24, 24, 19, p, q); x.stroke(); } } fillTris(x, [[[24, 15], [31, 30], [17, 30]]], C, 3); },
      // A tractor: a solid emitter, a beam that widens in Cantor steps, the held box.
      tractor: () => { x.fillRect(3, 18, 9, 12); x.lineWidth = 2.4; CANT(12, 21, 33, 11, 2); CANT(12, 27, 33, 37, 2); x.lineWidth = 3; x.strokeRect(34, 15, 10, 18); },
      // Transport: a figure solid above the waist, dissolving below into Cantor dust, finer row by row.
      transport: () => {
        D(24, 8, 5.5); x.beginPath(); x.moveTo(14, 26); x.lineTo(15, 19); x.quadraticCurveTo(24, 13, 33, 19); x.lineTo(34, 26); x.closePath(); x.fill();
        x.lineWidth = 2.6; for (let r = 0; r < 4; r++) CANT(14, 30 + r * 4.4, 34, 30 + r * 4.4, r + 1);
      },
      // Science: an atom; three exact orbits and a solid nucleus.
      science: () => { x.lineWidth = 2.4; for (const r of [0, P / 3, -P / 3]) { x.beginPath(); x.ellipse(24, 24, 20, 7.5, r, 0, 2 * P); x.stroke(); } D(24, 24, 4.5); },
      // Comms: a solid dish on its stand, its waves spaced by the golden ratio.
      comms: () => { x.beginPath(); x.arc(15, 27, 11, P * 0.75, P * 1.75, true); x.closePath(); x.fill(); x.lineWidth = 2.6; L([[15, 27], [11, 44]]); L([[15, 27], [19, 44]]); L([[8, 44], [22, 44]]); D(23, 19, 2.5); for (let i = 0; i < 3; i++) { const r = 6 * Math.pow(PHI, i); x.beginPath(); x.arc(23, 19, r, -P * 0.5, 0); x.stroke(); } },
      // Repair: a solid wrench.
      repair: () => { x.lineWidth = 6; x.lineCap = 'round'; L([[10, 38], [26, 22]]); x.lineWidth = 5; x.beginPath(); x.arc(31, 17, 9, -P / 4 + 0.95, -P / 4 - 0.95 + 2 * P); x.stroke(); x.lineCap = 'butt'; },
      // The map: a star chart; solid stars joined by routes that branch, the branchers' rule.
      map: () => { x.lineWidth = 2.2; L([[24, 40], [24, 28]]); L([[24, 28], [13, 16]]); L([[24, 28], [35, 16]]); L([[13, 16], [8, 8]]); L([[13, 16], [18, 7]]); L([[35, 16], [30, 7]]); L([[35, 16], [40, 8]]); D(24, 40, 4); D(24, 28, 3.2); D(13, 16, 3.2); D(35, 16, 3.2); for (const [px, py] of [[8, 8], [18, 7], [30, 7], [40, 8]]) D(px, py, 2.4); },
      // Settings: a solid gear with an exact bore.
      settings: () => { const o = []; for (let i = 0; i < 32; i++) { const a = i * 2 * P / 32 - P / 32, r = (i >> 1) % 2 ? 14.5 : 20; o.push([24 + Math.cos(a) * r, 24 + Math.sin(a) * r]); } x.beginPath(); path(x, o); x.arc(24, 24, 6, 0, 2 * P, true); x.fill('evenodd'); },
    })[name]();
  });
}

// ---- the skin ----------------------------------------------------------------------------------------------------
F.register('axiom', {
  name: 'The Axiom', kind: 'Flat and exact, drawn by the mathematics: Koch keys, Sierpinski corners, Cantor rules, escape-time screens', bg: AX.void,
  css: { '--s-ink': AX.proof, '--s-dim': AX.dim, '--s-num': AX.cyan, '--s-body': '"Space Mono", monospace', '--btn-ink': AX.proof, '--btn-shadow': '0 0 2px #05060c, 0 0 6px rgba(63,224,230,0.45)', '--btn-justify': 'center',
    '--btn-font': '700 15px/1 "Space Mono", monospace', '--btn-track': '0.12em', '--btn-case': 'uppercase', '--btn-motion': 'padding-top 0.06s', '--btn-sink': '1px',
    '--tab-font': '400 13px/1 "Space Mono", monospace', '--tab-ink': AX.dim, '--tab-ink-on': AX.gold, '--title-font': '700 14px/1 "Martian Mono", monospace', '--title-ink': AX.cyan,
    '--lbl-font': '400 13px/1 "Space Mono", monospace', '--num-font': '700 24px/1 "Space Mono", monospace',
    // The strips are 72 px; their open band lies between the outer hairline (row 1.5) and the inner rule (row 67.5 of the
    // top strip, 4.5 of the bottom), so each label stands over its bar clear of both.
    '--stt-pt': '7px', '--stt-pb': '10px', '--stb-pt': '10px', '--stb-pb': '7px', '--strip-lamp': '44px' },
  accent: AX.cyan, warn: AX.magenta,
  // The title sits in the frame's top band, right of the corner's Sierpinski triangle, under the fine rule.
  title: (t, m) => { t.style.cssText = `left:${30 - m[3]}px;top:${9 - m[0]}px;letter-spacing:0.08em;`; },
  // The captain's name sits in the plain middle of the name plate's Koch key (54-406), clear of its Koch ends.
  plateText: { left: 62, top: 12, width: 336, height: 28, font: '700 19px/28px "Space Mono", monospace', color: AX.proof, sub: 'FIXED POINT · ITERANT ORU' },
  helmKeys: 'ring', helmR: 100, shield: [AX.cyan, AX.gold, AX.magenta],
  fonts: ['400 20px "Space Mono"', '700 20px "Space Mono"', '700 20px "Martian Mono"'], view: { title: '700 26px "Space Mono"', body: '400 22px "Space Mono"', upper: true, ink: AX.proof, dim: AX.dim },
  build: () => F.fromPainters(nine, hero, icon, false),
  live: {
    // A direction: a Sierpinski triangle of one level, pointing the way.
    arrow: (x) => { fillTris(x, sierpinski([12, 0], [-7, -10], [-7, 10], 1), AX.proof, 3); },
    helmCentre: (x, c) => { x.fillStyle = AX.gold; x.font = '700 15px "Space Mono"'; x.textAlign = 'center'; x.fillText('STOP', c, c + 7); },
    // The ship from above, a Koch key turned on end; its shield facings as arcs cut by the Cantor set, mounts in gold.
    ssd: (x, cx, cy, sh, col) => {
      x.save(); x.lineCap = 'butt';
      for (let i = 0; i < 6; i++) {
        const a = -P / 2 + i * P / 3; x.strokeStyle = col(sh[i]); x.shadowColor = col(sh[i]); x.shadowBlur = 5; x.lineWidth = 6;
        for (const [p, q] of cantor(a - P / 6 + 0.07, a + P / 6 - 0.07, 2)) { x.beginPath(); x.arc(cx, cy, 100, p, q); x.stroke(); }
      }
      x.restore();
      const hull = kochKey(0, 0, 150, 52, 2, 34).map(([u, v]) => [cx + (v - 26), cy - 75 + u]);
      x.fillStyle = hexA(AX.void, 0.7); path(x, hull); x.fill(); glow(x, hull, AX.cyan, 2, 5);
      glow(x, [[cx, cy - 62], [cx, cy + 62]], hexA(AX.cyan, 0.7), 1, 2, false);
      for (const t of [1 / PHI / PHI, 1 / PHI]) { const y = cy - 75 + 150 * t; glow(x, [[cx - 24, y], [cx + 24, y]], hexA(AX.cyan, 0.7), 1, 2, false); }
      for (const [mx, my] of [[cx, cy - 58], [cx - 16, cy + 4], [cx + 16, cy + 4], [cx, cy + 50]]) fillTris(x, [[[mx, my - 6], [mx + 6, my + 5], [mx - 6, my + 5]]], AX.gold, 4);
    },
    // The radar: a sweep in cyan, its wake a fan of fading rays; contacts as small magenta triangles.
    dial: (x, c, cts) => {
      const a0 = -0.9; for (let i = 0; i < 6; i++) glow(x, [[c, c], [c + Math.cos(a0 - i * 0.09) * 50, c + Math.sin(a0 - i * 0.09) * 50]], hexA(AX.cyan, 0.9 - i * 0.15), i ? 1 : 2, i ? 0 : 4, false);
      for (const [dx, dy] of cts) { const px = c + dx * 36, py = c + dy * 36; fillTris(x, [[[px, py - 5], [px + 5, py + 4], [px - 5, py + 4]]], AX.magenta, 4); }
    },
    // The target: a Sierpinski triangle at each corner on two fine legs.
    bracket: (x, bx, by, bw, bh) => {
      for (const [px, py, sx, sy] of [[bx, by, 1, 1], [bx + bw, by, -1, 1], [bx, by + bh, 1, -1], [bx + bw, by + bh, -1, -1]]) {
        sierCorner(x, px, py, 26, sx, sy, 3, AX.cyan, 4);
        glow(x, [[px + sx * 26, py], [px + sx * 48, py]], AX.cyan, 1.4, 3, false); glow(x, [[px, py + sy * 26], [px, py + sy * 48]], AX.cyan, 1.4, 3, false);
      }
    },
  },
  world: ['Every true thing is a rule and its iteration; a structure is honoured by how deep its rule has been carried.',
    'Their bodies are not described and shape nothing here: the console answers to their three schools of thought.',
    'Three schools at once: the escape-time school\'s boundary screens, the tilers\' self-similar frames and plates, the branchers\' grown marks.',
    'Hulls laid out by substitution: every part a smaller copy of the whole (provisional).'],
  shapes: 'Koch keys (hexagons with snowflake ends), Sierpinski corners and carpet cells, Hilbert traces, Cantor rules, a dragon-curve crest.',
  light: 'None painted: exact thin lines and flat fills on the void, glowing a little; depth of iteration is colour, indigo to cyan.',
  motion: 'A hovered key iterates one level deeper; a pressed key fills solid in a gold outline; bar cells light one by one.',
  type: '"Martian Mono" titles, wide and even, over "Space Mono" keys and readouts.',
  palette: [['Void', AX.void, 'The ground', 'The unbounded plane every rule is drawn on'], ['Proof white', AX.proof, 'Text, labels', 'Stated truth'],
    ['Iteration indigo', AX.indigo, 'Frames, idle keys', 'A rule applied once or twice: shallow'], ['Iteration cyan', AX.cyan, 'Live lines, readouts, icons', 'A rule carried deep'],
    ['Convergence gold', AX.gold, 'Pressed keys, the chosen tab, stop', 'What converges is true; ready'], ['Divergence magenta', AX.magenta, 'Alerts only', 'What escapes to infinity: danger']],
});
})();
