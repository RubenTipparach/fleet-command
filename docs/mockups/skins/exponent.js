// The Exponent skin (mockup 4, OpenSpec change console-skins). The owner, 2026-11-07, replacing the earlier hybrid idea:
// "Exponent is not a mix of two races. It is its own thing. Inspired by Borg. It's ship ui is a metalic utilitarian look.
// With veins a and tentacles and barnacles growing. Like am abandoned rusted ship at sea. Take over by reefs and coral".
// Take the shape, not the text (CLAUDE.md section 1): what we take is a collective that takes in whatever it meets and
// runs on plain machinery. No names, glyphs, shapes, colours or art from any film or game.
//
// Provisional culture, ours, the owner's to correct on the survey: the Exponent is one collective that grows by taking in
// ships, stations and crews. Nothing is decorated; everything is function. Its machines are kept running long past any
// refit, and what it has absorbed keeps growing on them, like a hulk that has lain on a sea floor. Three traditions,
// and which piece comes from which (never from a species' body, CLAUDE.md section 1):
//   - the assimilators: whatever is taken is bolted on as found. The keys are access hatches on hinges with dog latches,
//     the frames riveted plate with mismatched patches, the ship display a hull of joined modules, the crest a ring of
//     seven unlike plates bolted shut;
//   - the conduit-keepers: power carried in veins of cable and fluid line under translucent sheaths, a dim pulse in each;
//     they run along the frames' feet, between the helm's hatches and up the column, and the bars fill as a lit conduit;
//   - the reef: growth the collective tolerates and builds on, accreting in layers: barnacles (the bars' cells), brain and
//     branching coral, tube worms, cabled kelp-like tendrils curling over edges, salt crust and verdigris on rusted plate.
//     Growth keeps to margins and corners: it never covers a label, a key's slot or a readout.
//
// First pass, painted in canvas with shading lit from the upper left. Every scatter comes from the page's seeded generator
// (FCK.rng over FCK.hash, Mulberry32), never a cheap hash (CLAUDE.md section 10). The kit's nine-slice pieces keep their
// detail in the margins and their centres plain. A metal skin, so the column has a painted ground (CLAUDE.md section 7):
// `column`, a deck bulkhead with conduits and reef, drawn behind the panels. Registers itself with the page's skin
// registry (docs/mockups/skins). Our own design: nothing traced from another game.
(function () {
'use strict';
const F = window.FCK, P = F.P;
// The palette (six rows below) and the shades painted between them.
const EX = {
  steel: '#5d686d', steelHi: '#97a3a6', steelLo: '#262e31', deep: '#0b1113', bg: '#0d1315', glass: '#091012',
  rust: '#8c4522', rustHi: '#c46d36', rustLo: '#43200f', verd: '#4f8c7a', verdHi: '#8cc4ae',
  coral: '#d27a6a', coralHi: '#f2b19c', coralLo: '#7e3a3a', ochre: '#c6935a', ochreLo: '#6b4526',
  bone: '#e3dbc6', boneLo: '#7d776a', kelp: '#46634c', kelpHi: '#a8cca6',
  lumen: '#8be6dc', lumenHot: '#e2fffa', lumenLo: '#1d5e5c', breach: '#ff5a36', breachHot: '#ffd2c2', amber: '#f0b04a', sea: '#5fd68e',
};
const RNG = (k) => F.rng(F.hash('exponent.' + k));
function hexA(hex, a) { const n = parseInt(hex.slice(1), 16); return `rgba(${n >> 16},${(n >> 8) & 255},${n & 255},${a})`; }
function mix(h1, h2, t) {
  const a = parseInt(h1.slice(1), 16), b = parseInt(h2.slice(1), 16), l = (s) => Math.round(((a >> s) & 255) * (1 - t) + ((b >> s) & 255) * t);
  return `rgb(${l(16)},${l(8)},${l(0)})`;
}
const clamp = (v, a, b) => Math.max(a, Math.min(b, v));

// ---- shapes ------------------------------------------------------------------------------------------------------
// A chamfered rectangle: plate cut at 45 degrees at each corner by c (an array of four for unequal corners).
function cham(x, X, Y, W, H, c) {
  const [a, b, d, e] = Array.isArray(c) ? c : [c, c, c, c];
  x.beginPath(); x.moveTo(X + a, Y); x.lineTo(X + W - b, Y); x.lineTo(X + W, Y + b); x.lineTo(X + W, Y + H - d); x.lineTo(X + W - d, Y + H);
  x.lineTo(X + e, Y + H); x.lineTo(X, Y + H - e); x.lineTo(X, Y + a); x.closePath();
}
function disc(x, cx, cy, r) { x.beginPath(); x.arc(cx, cy, r, 0, 2 * P); }
// Points along a Catmull-Rom curve through pts, n per span.
function curve(pts, n = 10) {
  const out = [];
  for (let i = 0; i < pts.length - 1; i++) {
    const p0 = pts[Math.max(0, i - 1)], p1 = pts[i], p2 = pts[i + 1], p3 = pts[Math.min(pts.length - 1, i + 2)];
    for (let k = 0; k < n; k++) {
      const t = k / n, t2 = t * t, t3 = t2 * t;
      out.push([0, 1].map((j) => 0.5 * ((2 * p1[j]) + (-p0[j] + p2[j]) * t + (2 * p0[j] - 5 * p1[j] + 4 * p2[j] - p3[j]) * t2 + (-p0[j] + 3 * p1[j] - 3 * p2[j] + p3[j]) * t3)));
    }
  }
  out.push(pts[pts.length - 1]); return out;
}

// ---- materials: the assimilators' plate ------------------------------------------------------------------------------
// Painted steel kept running long past any refit: grey-blue paint over steel, lit from the top, brushed grain, paint
// chipped through to rust bloom with its edge catching the light, rust run down from the chips, salt crust settled toward
// the foot, a stain of verdigris. where(px, py) gives 0 to 1: how much wear a spot may take (0 keeps a stretching centre plain).
function plate(x, path, box, R, o = {}) {
  const [x0, y0, x1, y1] = box, w = x1 - x0, h = y1 - y0, where = o.where || (() => 1);
  x.save(); path(); x.clip();
  const g = x.createLinearGradient(x0, y0, x0 + w * 0.25, y1); g.addColorStop(0, o.hi || '#76838a'); g.addColorStop(0.5, o.mid || EX.steel); g.addColorStop(1, o.lo || '#3a4448');
  x.fillStyle = g; x.fillRect(x0, y0, w, h);
  // brushed grain, along the plate
  for (let y = y0; y < y1; y += 1.3) { x.fillStyle = R() < 0.5 ? `rgba(255,255,255,${0.02 + R() * 0.035})` : `rgba(0,0,0,${0.03 + R() * 0.05})`; x.fillRect(x0, y, w, 0.6); }
  // mottle
  const nm = Math.round(w * h / 900);
  for (let i = 0; i < nm; i++) {
    const cx = x0 + R() * w, cy = y0 + R() * h, r = 4 + R() * 16, dark = R() < 0.6, k = where(cx, cy);
    if (k <= 0.05) continue;
    const b = x.createRadialGradient(cx, cy, 0, cx, cy, r); b.addColorStop(0, dark ? `rgba(15,20,22,${0.18 * k})` : `rgba(200,210,212,${0.08 * k})`); b.addColorStop(1, 'rgba(0,0,0,0)');
    x.fillStyle = b; x.fillRect(cx - r, cy - r, 2 * r, 2 * r);
  }
  // rust: patches where the paint has failed, each a soft bloom stain round a crowd of chips (dense at its heart, thin
  // at its edge), the bigger chips' lower lips catching the light, and a run of rust down from some
  const nr = Math.round(w * h / (o.rustEvery || 6000)), sc = o.rustScale || 1;
  for (let i = 0; i < nr; i++) {
    const cx = x0 + R() * w, cy = y0 + R() * h, k = where(cx, cy), rc = (3 + R() * 10) * sc;
    if (R() > k) continue;
    for (let j = 0; j < 3; j++) {
      const bx = cx + (R() - 0.5) * rc, by = cy + (R() - 0.5) * rc, br = rc * (0.7 + R() * 0.7), b = x.createRadialGradient(bx, by, 0, bx, by, br);
      b.addColorStop(0, hexA(EX.rust, 0.28)); b.addColorStop(0.6, hexA(EX.rustLo, 0.12)); b.addColorStop(1, 'rgba(67,32,15,0)'); x.fillStyle = b; x.fillRect(bx - br, by - br, 2 * br, 2 * br);
    }
    const nchip = Math.round(rc * 1.6 + 2);
    for (let j = 0; j < nchip; j++) {
      const g3 = (R() + R() + R() - 1.5) / 1.5, g4 = (R() + R() + R() - 1.5) / 1.5, px = cx + g3 * rc, py = cy + g4 * rc * 0.8;
      const r = (0.5 + Math.pow(R(), 2.2) * 3.2) * Math.sqrt(sc) * (1.2 - Math.hypot(g3, g4) * 0.5), n = 5 + Math.floor(R() * 3), pts = [];
      if (r < 0.35) continue;
      for (let q = 0; q < n; q++) { const a = q / n * 2 * P + R() * 0.5, rr = r * (0.5 + R() * 0.7); pts.push([px + Math.cos(a) * rr, py + Math.sin(a) * rr]); }
      x.beginPath(); pts.forEach(([a, bb], q) => (q ? x.lineTo(a, bb) : x.moveTo(a, bb))); x.closePath();
      x.fillStyle = mix(EX.rustLo, R() < 0.3 ? EX.rustHi : EX.rust, 0.3 + R() * 0.7); x.fill();
      if (r > 1) { x.save(); x.translate(0.35, 0.45); x.strokeStyle = 'rgba(205,215,215,0.32)'; x.lineWidth = 0.45; x.stroke(); x.restore(); }
    }
    if (R() < 0.75) {
      const L = 12 + R() * 46 * sc, sw = 1 + R() * 2.6, sx = cx + (R() - 0.5) * rc * 0.6, sg = x.createLinearGradient(0, cy, 0, cy + L);
      sg.addColorStop(0, hexA(EX.rust, 0.5)); sg.addColorStop(1, 'rgba(140,69,34,0)'); x.fillStyle = sg;
      x.beginPath(); x.moveTo(sx - sw / 2, cy); x.lineTo(sx + sw / 2, cy); x.lineTo(sx + sw * 0.2, cy + L); x.lineTo(sx - sw * 0.2, cy + L); x.closePath(); x.fill();
    }
  }
  // verdigris and sea stain
  const nv = Math.round(w * h / (o.verdEvery || 5000));
  for (let i = 0; i < nv; i++) {
    const cx = x0 + R() * w, cy = y0 + R() * h, k = where(cx, cy), r = 5 + R() * 14;
    if (R() > k) continue;
    const b = x.createRadialGradient(cx, cy, 0, cx, cy, r); b.addColorStop(0, hexA(EX.verd, 0.4)); b.addColorStop(1, 'rgba(79,140,122,0)'); x.fillStyle = b; x.fillRect(cx - r, cy - r, 2 * r, 2 * r);
  }
  // salt crust, settled toward the foot
  const ns = Math.round(w * h / (o.saltEvery || 40));
  for (let i = 0; i < ns; i++) {
    const px = x0 + R() * w, py = y0 + Math.pow(R(), 0.55) * h, k = where(px, py);
    if (R() > k * (0.25 + 0.75 * (py - y0) / h)) continue;
    x.fillStyle = `rgba(232,228,212,${0.18 + R() * 0.35})`; x.fillRect(px, py, 0.5 + R() * 0.9, 0.5 + R() * 0.7);
  }
  x.restore();
}
// The bevel of a plate's edge: lit along its top and left, shaded along its bottom and right.
function bevel(x, path, lw = 1.2) {
  x.save(); path(); x.clip();
  x.save(); x.translate(lw * 0.6, lw * 0.6); path(); x.strokeStyle = 'rgba(210,222,224,0.45)'; x.lineWidth = lw * 1.6; x.stroke(); x.restore();
  x.save(); x.translate(-lw * 0.6, -lw * 0.6); path(); x.strokeStyle = 'rgba(8,10,11,0.6)'; x.lineWidth = lw * 1.6; x.stroke(); x.restore();
  x.restore();
  path(); x.strokeStyle = '#070b0c'; x.lineWidth = lw; x.stroke();
}
// A recess (a slot, a screen well): dark, its top lip in shadow and its lower lip lit.
function recess(x, path, box, fill = EX.glass) {
  const [x0, y0, , y1] = box;
  path(); x.fillStyle = fill; x.fill();
  x.save(); path(); x.clip();
  const g = x.createLinearGradient(0, y0, 0, y0 + Math.min(10, (y1 - y0) * 0.4)); g.addColorStop(0, 'rgba(0,0,0,0.75)'); g.addColorStop(1, 'rgba(0,0,0,0)'); x.fillStyle = g; x.fillRect(x0 - 2, y0, box[2] - x0 + 4, y1 - y0);
  x.restore();
  x.save(); path(); x.clip(); x.translate(0, -1); path(); x.strokeStyle = 'rgba(190,205,205,0.35)'; x.lineWidth = 1.4; x.stroke(); x.restore();
  path(); x.strokeStyle = '#050809'; x.lineWidth = 1; x.stroke();
}
// A domed rivet, lit from the upper left, with rust bled round it and sometimes run below.
function rivet(x, cx, cy, r, R) {
  x.fillStyle = 'rgba(0,0,0,0.45)'; x.beginPath(); x.ellipse(cx + r * 0.35, cy + r * 0.45, r * 1.1, r, 0, 0, 2 * P); x.fill();
  if (R && R() < 0.55) { const L = 4 + R() * 12, g = x.createLinearGradient(0, cy, 0, cy + L); g.addColorStop(0, hexA(EX.rust, 0.6)); g.addColorStop(1, 'rgba(140,69,34,0)'); x.fillStyle = g; x.fillRect(cx - r * 0.4, cy, r * 0.8, L); }
  const g = x.createRadialGradient(cx - r * 0.35, cy - r * 0.4, r * 0.1, cx, cy, r); g.addColorStop(0, '#c9d2d3'); g.addColorStop(0.45, '#6d787c'); g.addColorStop(1, '#1d2326');
  disc(x, cx, cy, r); x.fillStyle = g; x.fill();
  x.strokeStyle = hexA(EX.rust, 0.55); x.lineWidth = 0.7; x.stroke();
}
// A hex bolt, its flats catching the light in steps.
function bolt(x, cx, cy, r, rot = 0.3) {
  x.fillStyle = 'rgba(0,0,0,0.5)'; disc(x, cx + r * 0.3, cy + r * 0.4, r * 1.05); x.fill();
  for (let i = 0; i < 6; i++) {
    const a0 = rot + i * P / 3, a1 = a0 + P / 3, mid = (a0 + a1) / 2, lit = 0.5 - 0.5 * Math.cos(mid - (-3 * P / 4));
    x.beginPath(); x.moveTo(cx, cy); x.lineTo(cx + Math.cos(a0) * r, cy + Math.sin(a0) * r); x.lineTo(cx + Math.cos(a1) * r, cy + Math.sin(a1) * r); x.closePath();
    x.fillStyle = mix('#c4cdce', '#283034', 1 - lit); x.fill();
  }
  disc(x, cx, cy, r * 0.55); x.fillStyle = '#4c575b'; x.fill(); x.strokeStyle = 'rgba(10,12,12,0.6)'; x.lineWidth = 0.5; x.stroke();
}

// ---- the reef --------------------------------------------------------------------------------------------------------
// A barnacle: a low cone of six wall plates seen from above, lit from the upper left, its dark aperture at the top.
// lit: 0 dark, otherwise a colour the aperture glows (the symbol cells).
function barnacle(x, cx, cy, r, R, lit) {
  const rot = R() * P, n = 6, apx = cx - r * 0.08, apy = cy - r * 0.12, ar = r * 0.42;
  x.fillStyle = 'rgba(0,0,0,0.42)'; x.beginPath(); x.ellipse(cx + r * 0.3, cy + r * 0.35, r * 1.08, r * 0.98, 0, 0, 2 * P); x.fill();
  const base = [], top = [];
  for (let i = 0; i < n; i++) { const a = rot + i * 2 * P / n, j = 0.85 + R() * 0.25; base.push([cx + Math.cos(a) * r * j, cy + Math.sin(a) * r * j]); top.push([apx + Math.cos(a) * ar, apy + Math.sin(a) * ar * 0.8]); }
  for (let i = 0; i < n; i++) {
    const a = base[i], b = base[(i + 1) % n], c = top[(i + 1) % n], d = top[i], mx = (a[0] + b[0]) / 2 - cx, my = (a[1] + b[1]) / 2 - cy, L = Math.hypot(mx, my) || 1;
    const lit2 = clamp(0.5 - 0.5 * ((mx / L) * 0.62 + (my / L) * 0.78), 0, 1);
    x.beginPath(); x.moveTo(a[0], a[1]); x.lineTo(b[0], b[1]); x.lineTo(c[0], c[1]); x.lineTo(d[0], d[1]); x.closePath();
    x.fillStyle = mix(EX.boneLo, EX.bone, 0.15 + lit2 * 0.85); x.fill();
    x.strokeStyle = 'rgba(40,36,30,0.55)'; x.lineWidth = Math.max(0.35, r * 0.07); x.stroke();
    // growth ridges across the plate
    x.strokeStyle = 'rgba(60,54,44,0.25)'; x.lineWidth = Math.max(0.25, r * 0.04);
    for (const t of [0.35, 0.65]) { x.beginPath(); x.moveTo(a[0] + (d[0] - a[0]) * t, a[1] + (d[1] - a[1]) * t); x.lineTo(b[0] + (c[0] - b[0]) * t, b[1] + (c[1] - b[1]) * t); x.stroke(); }
  }
  // the aperture: two closing plates over a dark opening, or a light deep inside
  x.save(); x.beginPath(); x.ellipse(apx, apy, ar * 0.82, ar * 0.62, rot * 0.3, 0, 2 * P);
  if (lit) { x.shadowColor = lit; x.shadowBlur = r * 1.4; x.fillStyle = lit; x.fill(); x.shadowBlur = 0; const g = x.createRadialGradient(apx, apy, 0, apx, apy, ar * 0.8); g.addColorStop(0, '#ffffff'); g.addColorStop(1, hexA(lit === EX.breach ? EX.breach : EX.lumen, 0)); x.fillStyle = g; x.fill(); }
  else { x.fillStyle = '#16140f'; x.fill(); x.strokeStyle = 'rgba(160,150,130,0.4)'; x.lineWidth = Math.max(0.3, r * 0.05); x.beginPath(); x.moveTo(apx - ar * 0.6, apy); x.lineTo(apx + ar * 0.6, apy); x.stroke(); }
  x.restore();
}
// A cluster of barnacles round a point, big ones first and small ones filling between, drawn back to front.
function barnacles(x, cx, cy, sx, sy, n, rmin, rmax, R) {
  const got = [];
  for (let t = 0; t < n * 12 && got.length < n; t++) {
    const k = got.length / n, r = rmax - (rmax - rmin) * Math.sqrt(k) * (0.7 + R() * 0.3), a = R() * 2 * P, d = Math.sqrt(R());
    const p = [cx + Math.cos(a) * sx * d, cy + Math.sin(a) * sy * d, r];
    if (got.every((q) => Math.hypot(q[0] - p[0], q[1] - p[1]) > (q[2] + r) * 0.82)) got.push(p);
  }
  got.sort((a, b) => a[1] - b[1]);
  for (const [px, py, r] of got) barnacle(x, px, py, r, R, 0);
}
// Brain coral: a dome grooved by meanders, lit from the upper left.
function brain(x, cx, cy, rx, ry, R, col = EX.ochre) {
  x.fillStyle = 'rgba(0,0,0,0.45)'; x.beginPath(); x.ellipse(cx + rx * 0.18, cy + ry * 0.25, rx * 1.05, ry * 1.02, 0, 0, 2 * P); x.fill();
  x.save(); x.beginPath(); x.ellipse(cx, cy, rx, ry, 0, 0, 2 * P); x.clip();
  const g = x.createRadialGradient(cx - rx * 0.35, cy - ry * 0.4, 0, cx, cy, Math.max(rx, ry) * 1.1); g.addColorStop(0, mix(col, '#fff3e0', 0.45)); g.addColorStop(0.55, col); g.addColorStop(1, EX.ochreLo);
  x.fillStyle = g; x.fillRect(cx - rx, cy - ry, 2 * rx, 2 * ry);
  // the ridges: radial septa from a mouth near the top of the dome to its rim, each wavering a little and some forking,
  // a dark groove between each pair, and growth bands round them
  const rm = Math.max(rx, ry), n = Math.max(14, Math.round(2 * P * rm / 2.4)), mx = cx - rx * 0.08, my = cy - ry * 0.1, ph = R() * 2 * P;
  for (let i = 0; i < n; i++) {
    const a0 = i / n * 2 * P + (R() - 0.5) * 0.08, wob = 0.08 + R() * 0.1, t0 = i % 3 === 0 ? 0.12 : 0.3 + R() * 0.15, pts = [];
    for (let t = t0; t <= 1.02; t += 0.06) { const a = a0 + Math.sin(t * 7 + ph + i) * wob * t; pts.push([mx + Math.cos(a) * rx * t, my + Math.sin(a) * ry * t]); }
    const line = () => { x.beginPath(); pts.forEach(([a1, b1], j) => (j ? x.lineTo(a1, b1) : x.moveTo(a1, b1))); };
    const lit = clamp(0.5 - 0.5 * (Math.cos(a0) * 0.62 + Math.sin(a0) * 0.78), 0, 1);
    line(); x.strokeStyle = 'rgba(50,30,14,0.55)'; x.lineWidth = Math.max(0.9, rm * 0.085); x.stroke();
    line(); x.strokeStyle = `rgba(255,240,214,${0.25 + 0.45 * lit})`; x.lineWidth = Math.max(0.45, rm * 0.035); x.stroke();
  }
  for (const t of [0.45, 0.72]) { x.beginPath(); x.ellipse(mx, my, rx * t, ry * t, 0, 0, 2 * P); x.strokeStyle = 'rgba(60,34,16,0.22)'; x.lineWidth = Math.max(0.6, rm * 0.05); x.stroke(); }
  x.beginPath(); x.ellipse(mx, my, rx * 0.14, ry * 0.1, 0, 0, 2 * P); x.fillStyle = 'rgba(40,22,10,0.7)'; x.fill();
  const sh = x.createRadialGradient(cx - rx * 0.3, cy - ry * 0.35, Math.min(rx, ry) * 0.4, cx, cy, Math.max(rx, ry) * 1.05); sh.addColorStop(0, 'rgba(0,0,0,0)'); sh.addColorStop(1, 'rgba(25,12,5,0.55)');
  x.fillStyle = sh; x.fillRect(cx - rx, cy - ry, 2 * rx, 2 * ry);
  x.restore();
}
// Plate coral: shelves stacked in tiers, each a fan grooved from its root to a pale lip, its underside in shadow.
// a: the way the shelves face.
function plateCoral(x, cx, cy, r, a, R, col = '#b9895c') {
  for (let k = 0; k < 3; k++) {
    const rr = r * (1 - k * 0.22), ox = cx + Math.cos(a) * r * 0.18 * k + (R() - 0.5) * r * 0.2, oy = cy + Math.sin(a) * r * 0.18 * k + (R() - 0.5) * r * 0.2, s = 0.85 + R() * 0.35;
    const fan = () => { x.beginPath(); x.moveTo(ox, oy); x.arc(ox, oy, rr, a - s, a + s); x.closePath(); };
    x.save(); x.translate(1.2, 1.8); fan(); x.fillStyle = 'rgba(0,0,0,0.45)'; x.fill(); x.restore();
    fan(); const g = x.createRadialGradient(ox, oy, 0, ox, oy, rr); g.addColorStop(0, mix(col, '#2a1a10', 0.45)); g.addColorStop(0.75, col); g.addColorStop(1, mix(col, '#fff0dc', 0.45)); x.fillStyle = g; x.fill();
    x.save(); fan(); x.clip(); x.strokeStyle = 'rgba(60,36,18,0.4)'; x.lineWidth = Math.max(0.4, rr * 0.03);
    for (let i = 0; i <= 16; i++) { const b = a - s + 2 * s * i / 16; x.beginPath(); x.moveTo(ox, oy); x.lineTo(ox + Math.cos(b) * rr, oy + Math.sin(b) * rr); x.stroke(); }
    for (const t of [0.5, 0.75]) { x.beginPath(); x.arc(ox, oy, rr * t, a - s, a + s); x.stroke(); }
    x.restore();
    x.beginPath(); x.arc(ox, oy, rr - 0.6, a - s, a + s); x.strokeStyle = mix(col, '#fff6e8', 0.6); x.lineWidth = Math.max(0.6, rr * 0.06); x.stroke();
  }
}
// A sponge: an upright barrel seen from above and in front, its rim open on a dark well, its skin pitted with pores.
function sponge(x, cx, cy, w, h, R, col = '#c07a3a') {
  x.fillStyle = 'rgba(0,0,0,0.45)'; x.beginPath(); x.ellipse(cx + w * 0.25, cy + h * 0.1, w * 0.6, h * 0.2, 0, 0, 2 * P); x.fill();
  const body = () => { x.beginPath(); x.moveTo(cx - w * 0.5, cy - h); x.bezierCurveTo(cx - w * 0.62, cy - h * 0.4, cx - w * 0.5, cy, cx, cy); x.bezierCurveTo(cx + w * 0.5, cy, cx + w * 0.62, cy - h * 0.4, cx + w * 0.5, cy - h); x.closePath(); };
  body(); const g = x.createLinearGradient(cx - w * 0.6, 0, cx + w * 0.6, 0); g.addColorStop(0, mix(col, '#fff0d8', 0.3)); g.addColorStop(0.45, col); g.addColorStop(1, mix(col, '#1a0e08', 0.55)); x.fillStyle = g; x.fill();
  x.save(); body(); x.clip(); for (let i = 0; i < w * h * 0.25; i++) { disc(x, cx + (R() - 0.5) * w * 1.1, cy - R() * h, 0.4 + R() * 0.6); x.fillStyle = 'rgba(40,20,8,0.45)'; x.fill(); } x.restore();
  x.beginPath(); x.ellipse(cx, cy - h, w * 0.5, w * 0.2, 0, 0, 2 * P); x.fillStyle = mix(col, '#fff0d8', 0.35); x.fill();
  x.beginPath(); x.ellipse(cx, cy - h + w * 0.02, w * 0.36, w * 0.13, 0, 0, 2 * P); x.fillStyle = '#140a06'; x.fill();
}
// An anemone: a ring of tapering tentacles round a disc, each curling at its tip, pale-tipped and translucent.
function anemone(x, cx, cy, r, R, col = '#d9768a') {
  const n = 16 + Math.floor(R() * 8), ph = R() * 2 * P;
  for (let pass = 0; pass < 2; pass++) for (let i = 0; i < n; i++) {
    const a = ph + i / n * 2 * P + (R() - 0.5) * 0.2, L = r * (0.75 + R() * 0.4), c = (R() - 0.5) * 1.4;
    const ex = cx + Math.cos(a) * L, ey = cy + Math.sin(a) * L * 0.85, mx = cx + Math.cos(a + c * 0.4) * L * 0.6, my = cy + Math.sin(a + c * 0.4) * L * 0.5;
    x.lineCap = 'round'; x.beginPath(); x.moveTo(cx, cy); x.quadraticCurveTo(mx, my, ex, ey);
    if (pass === 0) { x.strokeStyle = 'rgba(0,0,0,0.35)'; x.lineWidth = r * 0.22; x.stroke(); }
    else { x.strokeStyle = hexA(col, 0.85); x.lineWidth = r * 0.16; x.stroke(); x.strokeStyle = 'rgba(255,230,236,0.45)'; x.lineWidth = r * 0.05; x.stroke(); disc(x, ex, ey, r * 0.09); x.fillStyle = '#fbe2e6'; x.fill(); }
  }
  disc(x, cx, cy, r * 0.28); x.fillStyle = mix(col, '#2a1016', 0.4); x.fill(); disc(x, cx, cy, r * 0.1); x.fillStyle = '#1a0a0e'; x.fill();
}
// A tide line of salt: a ragged pale crust along a run, thickest where it dried last.
function salt(x, x0, x1, y, th, R) {
  for (let px = x0; px < x1; px += 0.7) {
    const t = th * (0.3 + R() * 0.7) * (0.6 + 0.4 * Math.sin(px * 0.13 + y));
    x.fillStyle = `rgba(234,230,216,${0.25 + R() * 0.4})`; x.fillRect(px, y - t * R(), 0.8 + R() * 0.6, t);
  }
}
// A reef mass: the reef's layers over a patch, crust first, then shelves, sponges and domes, then branching coral,
// anemones, worms and a dense crowd of barnacles over all. k scales it.
function reef(x, cx, cy, rx, ry, R, k = 1, face = -P / 2) {
  const at = () => [cx + (R() - 0.5) * rx * 1.6, cy + (R() - 0.5) * ry * 1.6];
  for (let i = 0; i < 4; i++) { const [px, py] = at(); crust(x, px, py, rx * (0.35 + R() * 0.35), ry * (0.35 + R() * 0.35), R, ['#a1707a', '#567f6c', '#8f6f86', '#7d6a4e'][i]); }
  { const [px, py] = at(); plateCoral(x, px, py, 9 * k + R() * 5 * k, face + (R() - 0.5), R); }
  if (k > 0.7) { const [px, py] = at(); sponge(x, px, py, 7 * k, 10 * k, R, R() < 0.5 ? '#c07a3a' : '#7d4f7a'); }
  { const [px, py] = at(); brain(x, px, py, 7 * k + R() * 4 * k, 6 * k + R() * 3 * k, R, R() < 0.5 ? '#b6a06a' : '#8fa07a'); }
  { const [px, py] = at(); coral(x, px, py, face + (R() - 0.5) * 0.8, 6 * k + R() * 4 * k, 2.6 * k, 2, R, R() < 0.5 ? EX.coral : '#e0a24a'); }
  for (let i = 0; i < (k > 0.7 ? 2 : 1); i++) { const [px, py] = at(); anemone(x, px, py, 5 * k + R() * 3 * k, R, ['#d9768a', '#d8b860', '#8fd0c0'][Math.floor(R() * 3)]); }
  { const [px, py] = at(); tubes(x, px, py, 4, 7 * k, R, face); }
  barnacles(x, cx, cy, rx, ry, Math.round(10 * k), 0.9, 2.2 + 2.6 * k, R);
}
// Encrusting growth: a flat crust of coralline pink (or sea green) spread over the plate in an irregular lobe, its
// surface pebbled, its edge raised a little. The reef's first layer, laid down before anything else settles.
function crust(x, cx, cy, rx, ry, R, col = '#a1707a') {
  const n = 18, pts = [];
  for (let i = 0; i < n; i++) { const a = i / n * 2 * P, k = 0.55 + R() * 0.55; pts.push([cx + Math.cos(a) * rx * k, cy + Math.sin(a) * ry * k]); }
  const path = () => { const m = (i) => [(pts[i % n][0] + pts[(i + 1) % n][0]) / 2, (pts[i % n][1] + pts[(i + 1) % n][1]) / 2]; x.beginPath(); const s0 = m(0); x.moveTo(s0[0], s0[1]); for (let i = 1; i <= n; i++) { const p = pts[i % n], q = m(i); x.quadraticCurveTo(p[0], p[1], q[0], q[1]); } x.closePath(); };
  x.save(); x.translate(0.6, 0.8); path(); x.fillStyle = 'rgba(0,0,0,0.35)'; x.fill(); x.restore();
  x.save(); x.globalAlpha = 0.88;
  path(); const g = x.createRadialGradient(cx - rx * 0.3, cy - ry * 0.3, 0, cx, cy, Math.max(rx, ry)); g.addColorStop(0, mix(col, '#e8e0d8', 0.12)); g.addColorStop(1, mix(col, '#1a1416', 0.5));
  x.fillStyle = g; x.fill();
  x.save(); path(); x.clip();
  for (let i = 0; i < rx * ry * 1.1; i++) { const px = cx + (R() - 0.5) * 2 * rx, py = cy + (R() - 0.5) * 2 * ry, r = 0.4 + R() * 1.1; disc(x, px, py, r); x.fillStyle = R() < 0.45 ? 'rgba(240,230,225,0.2)' : 'rgba(30,18,22,0.35)'; x.fill(); }
  x.restore();
  path(); x.strokeStyle = 'rgba(20,14,16,0.6)'; x.lineWidth = 0.8; x.stroke();
  x.restore();
}
// Branching coral: limbs that fork by a seeded rule, rounded tips with polyps, shaded along their lower side.
function coral(x, x0, y0, a, len, w, depth, R, col = EX.coral) {
  const segs = [];
  const grow = (px, py, ang, L, W, d) => {
    const bend = (R() - 0.5) * 0.5, mx = px + Math.cos(ang + bend) * L * 0.5, my = py + Math.sin(ang + bend) * L * 0.5, ex = px + Math.cos(ang) * L, ey = py + Math.sin(ang) * L;
    segs.push([px, py, mx, my, ex, ey, W, d === 0]);
    if (d <= 0) return;
    const k = R() < 0.3 ? 3 : 2;
    for (let i = 0; i < k; i++) grow(ex, ey, ang + (i - (k - 1) / 2) * (0.55 + R() * 0.35), L * (0.62 + R() * 0.18), W * 0.74, d - 1);
  };
  grow(x0, y0, a, len, w, depth);
  const stroke = (dx, dy, wk, style) => { x.strokeStyle = style; x.lineCap = 'round'; for (const s of segs) { x.lineWidth = s[6] * wk; x.beginPath(); x.moveTo(s[0] + dx, s[1] + dy); x.quadraticCurveTo(s[2] + dx, s[3] + dy, s[4] + dx, s[5] + dy); x.stroke(); } };
  stroke(1.2, 1.5, 1.15, 'rgba(0,0,0,0.4)');
  stroke(0, 0, 1.25, mix(EX.coralLo, '#000000', 0.25));
  stroke(0, 0, 1, mix(col, EX.coralLo, 0.35));
  stroke(-0.25, -0.3, 0.6, col);
  stroke(-0.45, -0.55, 0.22, hexA(EX.coralHi, 0.8));
  for (const s of segs) if (s[7]) { disc(x, s[4], s[5], s[6] * 0.62); x.fillStyle = EX.coralHi; x.fill(); }
  // polyps: pale pores along the limbs
  for (const s of segs) for (let t = 0.3; t < 1; t += 0.35) { const px = s[0] + (s[4] - s[0]) * t + (R() - 0.5) * s[6] * 0.5, py = s[1] + (s[5] - s[1]) * t + (R() - 0.5) * s[6] * 0.5; x.fillStyle = 'rgba(255,225,210,0.55)'; x.fillRect(px - 0.4, py - 0.4, 0.8, 0.8); }
}
// Tube worms: chalk tubes with growth rings and dark mouths, some with a red plume fanned out.
function tubes(x, cx, cy, n, len, R, dir = -P / 2) {
  for (let i = 0; i < n; i++) {
    const a = dir + (R() - 0.5) * 1.3, L = len * (0.55 + R() * 0.5), w = 1.2 + R() * 1.1, bx = cx + (R() - 0.5) * len * 0.8, by = cy + (R() - 0.5) * len * 0.4;
    const ex = bx + Math.cos(a) * L, ey = by + Math.sin(a) * L, mx = (bx + ex) / 2 + (R() - 0.5) * L * 0.4, my = (by + ey) / 2 + (R() - 0.5) * L * 0.4;
    x.lineCap = 'round';
    x.strokeStyle = 'rgba(0,0,0,0.45)'; x.lineWidth = w + 1.4; x.beginPath(); x.moveTo(bx + 0.8, by + 1); x.quadraticCurveTo(mx + 0.8, my + 1, ex + 0.8, ey + 1); x.stroke();
    x.strokeStyle = EX.boneLo; x.lineWidth = w + 0.6; x.beginPath(); x.moveTo(bx, by); x.quadraticCurveTo(mx, my, ex, ey); x.stroke();
    x.strokeStyle = EX.bone; x.lineWidth = w * 0.55; x.beginPath(); x.moveTo(bx - 0.3, by - 0.3); x.quadraticCurveTo(mx - 0.3, my - 0.3, ex - 0.3, ey - 0.3); x.stroke();
    x.fillStyle = '#17110c'; disc(x, ex, ey, w * 0.5); x.fill();
    if (R() < 0.45) { x.strokeStyle = 'rgba(226,72,60,0.85)'; x.lineWidth = 0.45; for (let k = -3; k <= 3; k++) { const b = a + k * 0.24; x.beginPath(); x.moveTo(ex, ey); x.lineTo(ex + Math.cos(b) * w * 2.4, ey + Math.sin(b) * w * 2.4); x.stroke(); } }
  }
}
// A cabled tendril: a kelp-like cable along a curve, tapering, wrapped in bands, its end curled.
function tendril(x, pts, w0, w1, R, curl = 1) {
  let c = curve(pts, 12);
  if (curl) {   // curl the last point into a small spiral turning to one side
    const [ex, ey] = c[c.length - 1], [px, py] = c[c.length - 2]; let a = Math.atan2(ey - py, ex - px), r = w0 * 1.6, cx = ex, cy = ey; const tail = [];
    for (let i = 0; i < 18; i++) { a += curl * 0.38; r *= 0.93; cx += Math.cos(a) * r * 0.45; cy += Math.sin(a) * r * 0.45; tail.push([cx, cy]); }
    c = c.concat(tail);
  }
  const n = c.length, W = (i) => w0 + (w1 - w0) * i / (n - 1);
  const pass = (dx, dy, wk, style) => { x.strokeStyle = style; x.lineCap = 'round'; for (let i = 1; i < n; i++) { x.lineWidth = W(i) * wk; x.beginPath(); x.moveTo(c[i - 1][0] + dx, c[i - 1][1] + dy); x.lineTo(c[i][0] + dx, c[i][1] + dy); x.stroke(); } };
  pass(1.4, 1.8, 1.05, 'rgba(0,0,0,0.42)');
  pass(0, 0, 1.15, '#101a16');
  pass(0, 0, 1, EX.kelp);
  pass(-W(0) * 0.12, -W(0) * 0.16, 0.42, hexA(EX.kelpHi, 0.7));
  // the cable's wrap: a slanted band every few pixels
  let acc = 0;
  for (let i = 1; i < n; i++) {
    const dx = c[i][0] - c[i - 1][0], dy = c[i][1] - c[i - 1][1], L = Math.hypot(dx, dy); acc += L;
    if (acc < 3.4 || L === 0) continue; acc = 0;
    const nx = -dy / L, ny = dx / L, ww = W(i) * 0.5, sx = dx / L * ww * 0.5, sy = dy / L * ww * 0.5;
    x.strokeStyle = 'rgba(8,14,12,0.55)'; x.lineWidth = Math.max(0.35, W(i) * 0.14); x.beginPath(); x.moveTo(c[i][0] + nx * ww - sx, c[i][1] + ny * ww - sy); x.lineTo(c[i][0] - nx * ww + sx, c[i][1] - ny * ww + sy); x.stroke();
  }
  // a few barnacle spat riding on it
  for (let i = 0; i < 3; i++) { const p = c[Math.floor((0.15 + R() * 0.55) * n)]; if (W(0) > 3) barnacle(x, p[0] + (R() - 0.5), p[1] + (R() - 0.5), 1 + R() * 1.2, R, 0); }
}
// The conduit-keepers' vein: a line of power under a translucent sheath, a dim core with brighter beads of pulse.
// k: how lit, 0 to 1. Clamps of steel hold it at the ends.
function vein(x, pts, w, R, k = 0.6, clampEnds = true, col = EX.lumen, hot = EX.lumenHot) {
  const c = curve(pts, 10), n = c.length;
  const pass = (wd, style, dx = 0, dy = 0) => { x.strokeStyle = style; x.lineWidth = wd; x.lineCap = 'round'; x.lineJoin = 'round'; x.beginPath(); c.forEach(([a, b], i) => (i ? x.lineTo(a + dx, b + dy) : x.moveTo(a + dx, b + dy))); x.stroke(); };
  pass(w + 2.2, 'rgba(0,0,0,0.45)', 0.8, 1.1);
  pass(w + 1.4, '#08100f');
  pass(w, '#1b3533');
  if (k > 0) { x.save(); x.shadowColor = col; x.shadowBlur = 4 * k; pass(w * 0.42, hexA(col, 0.25 + 0.45 * k)); x.restore(); }
  pass(w * 0.22, hexA(hot, 0.15 + 0.5 * k));
  pass(w * 0.3, 'rgba(210,235,230,0.28)', -w * 0.18, -w * 0.24);   // the sheath's wet highlight
  // beads of pulse along it
  let acc = R() * 10;
  for (let i = 1; i < n; i++) {
    acc += Math.hypot(c[i][0] - c[i - 1][0], c[i][1] - c[i - 1][1]);
    if (acc < 11) continue; acc = R() * 4;
    const b = (0.3 + R() * 0.7) * k, g = x.createRadialGradient(c[i][0], c[i][1], 0, c[i][0], c[i][1], w * 1.3);
    g.addColorStop(0, hexA(hot, 0.8 * b)); g.addColorStop(0.5, hexA(col, 0.4 * b)); g.addColorStop(1, hexA(col, 0)); x.fillStyle = g; x.fillRect(c[i][0] - w * 1.3, c[i][1] - w * 1.3, w * 2.6, w * 2.6);
  }
  if (clampEnds) for (const i of [2, n - 3]) {
    const p = c[i], q = c[i + 1] || c[i - 1], a = Math.atan2(q[1] - p[1], q[0] - p[0]);
    x.save(); x.translate(p[0], p[1]); x.rotate(a); const g = x.createLinearGradient(0, -w, 0, w); g.addColorStop(0, '#b5bfc1'); g.addColorStop(1, '#2a3134');
    x.fillStyle = g; x.fillRect(-1.6, -w * 0.95, 3.2, w * 1.9); x.strokeStyle = '#070b0c'; x.lineWidth = 0.6; x.strokeRect(-1.6, -w * 0.95, 3.2, w * 1.9); x.restore();
  }
}

// ---- the kit -----------------------------------------------------------------------------------------------------
// The frame: riveted plate round a plain dark well. Rivets and growth only in the corners (the margins); along the top
// edge a cabled tendril grown out of the upper-right corner's coral, along the foot a vein under its sheath, and the
// sides plain (they stretch the most).
function frame(x, w, h, o) {
  const { m, band, c, key } = o, R = RNG(key), small = band < 14;
  const outer = () => cham(x, 1, 1, w - 2, h - 2, c);
  const inCorner = (px, py) => (Math.min(px, w - px) < m * 1.4 && Math.min(py, h - py) < m * 1.4 ? 1 : 0);
  plate(x, outer, [0, 0, w, h], R, { where: (px, py) => Math.max(inCorner(px, py), 0.15), rustEvery: small ? 900 : 1300, rustScale: small ? 0.5 : 1, saltEvery: 26 });
  bevel(x, outer, 1.3);
  // a patch plate, bolted on as found (the assimilators), across the lower-left corner
  if (!small) {
    const pp = () => cham(x, 4, h - m - 6, m + 10, m + 3, [0, 0, 6, 0]);
    plate(x, pp, [4, h - m - 6, m + 14, h - 3], R, { hi: '#6f6a5c', mid: '#57534a', lo: '#36332d', rustEvery: 500, rustScale: 0.7 });
    bevel(x, pp, 0.8);
  }
  // the well, plain
  const well = () => cham(x, band, band, w - 2 * band, h - 2 * band, Math.max(3, c - band * 0.6));
  recess(x, well, [band, band, w - band, h - band], EX.glass);
  // corner rivets (the margins only)
  const rr = small ? 1.6 : 2.6;
  for (const [sx, sy] of [[0, 0], [1, 0], [0, 1], [1, 1]]) {
    const px = sx ? w - band / 2 - 1 : band / 2 + 1, py = sy ? h - band / 2 - 1 : band / 2 + 1, along = small ? 0 : 10;
    rivet(x, sx ? px - along * 0.2 : px + along * 0.2, py, rr, R);
    if (!small) { rivet(x, sx ? px - m * 0.75 : px + m * 0.75, py, rr * 0.85, R); rivet(x, px, sy ? py - m * 0.75 : py + m * 0.75, rr * 0.85, R); }
  }
  if (small) {   // an inset: a little crust in one corner
    x.save(); outer(); x.rect(w - m, m, -(w - 2 * m), h - 2 * m); x.clip('evenodd');
    crust(x, w - 8, h - 7, 10, 7, R); crust(x, 7, 7, 8, 6, R, '#567f6c');
    barnacles(x, w - 7, h - 6, 7, 5, 7, 0.9, 3, R); barnacles(x, 6, 6, 5, 4, 4, 0.8, 2.4, R);
    anemone(x, w - 10, 8, 4.5, R, '#d8b860');
    x.restore();
    outer(); x.strokeStyle = '#060909'; x.lineWidth = 1; x.stroke();
    return;
  }
  // the foot: a vein under its sheath along the bottom band (the conduit-keepers)
  salt(x, m, w - m, h - 3, 3, R); salt(x, 6, w - 6, band - 1, 2, R);
  vein(x, [[m * 0.4, h - band / 2 + 1], [w * 0.3, h - band / 2 - 0.5], [w * 0.7, h - band / 2 + 1.5], [w - m * 0.4, h - band / 2 + 1]], 5, R, 0.85);
  // the reef, swelling out of every corner, kept outside the content box (inside the margins: nothing reaches a label)
  x.save(); outer(); x.rect(w - m, m, -(w - 2 * m), h - 2 * m); x.clip('evenodd');
  reef(x, 15, 14, 16, 13, R, 1.1, -P * 0.75);
  reef(x, w - 15, 15, 15, 13, R, 1, -P * 0.25);
  reef(x, 16, h - 15, 15, 12, R, 1, P * 0.75);
  reef(x, w - 16, h - 15, 16, 13, R, 1.15, P * 0.25);
  brain(x, w - 18, h - 16, 12, 10, R, '#b6a06a');
  coral(x, w - 6, 40, P * 1.05, 10, 3.6, 3, R);
  coral(x, 6, h - 40, P * -0.05, 9, 3.2, 3, R, '#e0a24a');
  barnacles(x, 45, h - 9, 14, 6, 10, 1, 3.6, R); barnacles(x, w - 48, 9, 14, 6, 10, 1, 3.6, R);
  // tendrils grown from the corners: one along the top band, one down the right side, both curling at their ends
  tendril(x, [[w - 12, 15], [w - m - 20, 9], [w * 0.55, 13], [w * 0.3, 8], [m + 22, 12]], 6.4, 2.2, R, -1);
  tendril(x, [[w - 8, h - 20], [w - 13, h * 0.6], [w - 6, h * 0.35], [w - 12, m + 14]], 5.2, 2, R, 1);
  x.restore();
  outer(); x.strokeStyle = '#060909'; x.lineWidth = 1.2; x.stroke();
}

// The hatch key (the assimilators' access hatch, bolted on as found): a rectangular steel hatch on a hinge at its left,
// a dog latch at its right, a hex bolt in each corner, a lit slot across its face, barnacles crusted on the hinge end.
// Idle the slot is dim; hover lights it; pressed the hatch sinks a pixel and the slot's light flares.
function hatch(state) {
  return (x, w) => {
    const R = RNG('hatch'), dy = state === 2 ? 1 : 0;
    // the hatch's shadow on the panel
    cham(x, 6, 4 + 2 - dy, w - 8, 42, 5); x.fillStyle = 'rgba(0,0,0,0.5)'; x.fill();
    x.save(); x.translate(0, dy);
    // the hinge: two knuckles on a pin, standing proud of the plate's left edge
    for (const [y0, y1] of [[7, 21], [27, 41]]) {
      const g = x.createLinearGradient(0, 0, 11, 0); g.addColorStop(0, '#1e2528'); g.addColorStop(0.4, '#a9b4b6'); g.addColorStop(1, '#2c3437');
      F.rr(x, 1, y0, 10, y1 - y0, 3); x.fillStyle = g; x.fill(); x.strokeStyle = '#070b0c'; x.lineWidth = 0.8; x.stroke();
    }
    x.fillStyle = '#39423f'; x.fillRect(4.5, 21, 3, 6);
    const body = () => cham(x, 7, 3, w - 9, 42, 5);
    plate(x, body, [7, 3, w - 2, 45], R, { where: (px) => (px < 28 || px > w - 26 ? 1 : 0), rustEvery: 700, rustScale: 0.45, saltEvery: 18 });
    bevel(x, body, 1.2);
    // the slot: recessed, its light by state
    const sx = 24, sw = w - 48, slot = () => F.rr(x, sx, 12, sw, 25, 3);
    const glow = [0.0, 0.55, 1][state];
    recess(x, slot, [sx, 12, sx + sw, 37], ['#0a1a1c', '#0f3b3d', '#16605f'][state]);
    if (glow) {
      x.save(); slot(); x.clip();
      const g = x.createLinearGradient(0, 12, 0, 37); g.addColorStop(0, hexA(EX.lumen, 0.14 * glow)); g.addColorStop(0.5, hexA(EX.lumen, 0.2 * glow)); g.addColorStop(1, hexA(EX.lumen, 0.26 * glow));
      x.fillStyle = g; x.fillRect(sx, 12, sw, 25);
      x.restore();
      x.save(); x.shadowColor = EX.lumen; x.shadowBlur = 8 * glow; slot(); x.strokeStyle = hexA(EX.lumen, 0.5 * glow); x.lineWidth = 1; x.stroke(); x.restore();
    } else {
      x.strokeStyle = hexA(EX.lumen, 0.18); x.lineWidth = 0.8; x.beginPath(); x.moveTo(sx + 4, 35.5); x.lineTo(sx + sw - 4, 35.5); x.stroke();
    }
    // bolts, the latch
    for (const [bx, by] of [[17, 9], [17, 39], [w - 16, 9], [w - 16, 39]]) bolt(x, bx, by, 3, 0.2 + bx * 0.01);
    const lg = x.createLinearGradient(w - 10, 0, w - 3, 0); lg.addColorStop(0, '#b8c2c4'); lg.addColorStop(1, '#2a3235');
    x.beginPath(); x.moveTo(w - 10, 16); x.lineTo(w - 4, 18); x.lineTo(w - 4, 30); x.lineTo(w - 10, 32); x.closePath(); x.fillStyle = lg; x.fill(); x.strokeStyle = '#070b0c'; x.lineWidth = 0.8; x.stroke();
    disc(x, w - 7, 24, 1.4); x.fillStyle = '#20282a'; x.fill();
    // the hinge end crusted with barnacles and salt
    crust(x, 10, 38, 10, 8, R);
    barnacles(x, 10, 38, 8, 6, 6, 1, 4.4, R);
    barnacles(x, 8, 9, 5, 4, 3, 0.8, 2.6, R);
    x.restore();
  };
}
// A tab: a stencil plate with its upper corners cut, a rivet at each end, a lamp strip along its foot (lit when active).
function tab(on) {
  return (x, w, h) => {
    const R = RNG('tab'), body = () => cham(x, 1, 2, w - 2, h - 4, [8, 8, 2, 2]);
    plate(x, body, [0, 0, w, h], R, { where: (px) => (px < 16 || px > w - 16 ? 1 : 0), rustEvery: 800, rustScale: 0.4, saltEvery: 22 });
    bevel(x, body, 1.1);
    const lamp = () => F.rr(x, 14, h - 9, w - 28, 4, 2);
    lamp(); x.fillStyle = on ? EX.lumen : '#14292a'; if (on) { x.save(); x.shadowColor = EX.lumen; x.shadowBlur = 6; x.fill(); x.restore(); } else x.fill();
    if (on) { lamp(); x.fillStyle = hexA(EX.lumenHot, 0.6); x.fill(); }
    lamp(); x.strokeStyle = '#060909'; x.lineWidth = 0.7; x.stroke();
    rivet(x, 7, h / 2 + 1, 2.1, R); rivet(x, w - 7, h / 2 + 1, 2.1, R);
    barnacles(x, w - 6, h - 6, 4, 3, 3, 0.7, 1.8, R);
  };
}
// A strip: a long plate with a rail along its outer lip and a plain dark channel for the readouts. At its left end a
// hand wheel on a valve (the conduit-keepers' cock), crusted; at its right a bolted junction box under brain coral.
function strip(flip) {
  return (x, w, h) => {
    const R = RNG('strip' + (flip ? 'b' : 't'));
    x.save(); if (flip) { x.translate(0, h); x.scale(1, -1); }
    const outer = () => cham(x, 1, 1, w - 2, h - 2, [2, 2, 14, 14]);
    plate(x, outer, [0, 0, w, h], R, { where: (px) => (px < 100 || px > w - 100 ? 1 : 0.04), rustEvery: 2200, saltEvery: 30 });
    bevel(x, outer, 1.2);
    const ch = () => F.rr(x, 92, 11, w - 184, h - 22, 3);
    recess(x, ch, [92, 11, w - 92, h - 11], EX.glass);
    // the rail on the outer lip: a vein in its sheath
    vein(x, [[96, 5.5], [w / 2, 5.5], [w - 96, 5.5]], 3.2, R, 0.45, false);
    // left: the valve wheel
    const vx = 46, vy = 36;
    disc(x, vx + 2, vy + 3, 27); x.fillStyle = 'rgba(0,0,0,0.45)'; x.fill();
    disc(x, vx, vy, 27); const rg = x.createRadialGradient(vx - 8, vy - 10, 2, vx, vy, 27); rg.addColorStop(0, '#9aa5a8'); rg.addColorStop(1, '#2a3235'); x.strokeStyle = rg; x.lineWidth = 5; x.stroke();
    for (let i = 0; i < 5; i++) { const a = i * 2 * P / 5 + 0.3; x.strokeStyle = '#3c4649'; x.lineWidth = 3.4; x.beginPath(); x.moveTo(vx, vy); x.lineTo(vx + Math.cos(a) * 25, vy + Math.sin(a) * 25); x.stroke(); x.strokeStyle = 'rgba(200,212,214,0.35)'; x.lineWidth = 1; x.beginPath(); x.moveTo(vx - 0.8, vy - 0.8); x.lineTo(vx + Math.cos(a) * 25 - 0.8, vy + Math.sin(a) * 25 - 0.8); x.stroke(); }
    bolt(x, vx, vy, 6, 0.1);
    salt(x, 96, w - 96, h - 2, 3, R);
    x.save(); outer(); x.rect(90, 10, w - 180, h - 20); x.clip('evenodd');
    reef(x, 70, 56, 20, 12, R, 1.1, P * 0.3); reef(x, 14, 22, 12, 18, R, 1, -P * 0.8);
    reef(x, 30, 64, 16, 7, R, 0.9, P * 0.5);
    barnacles(x, vx + 16, vy + 18, 14, 7, 8, 1, 4, R);
    tendril(x, [[86, 66], [70, 40], [84, 18], [70, 6]], 6, 2.2, R, 1);
    x.restore();
    // right: the junction box under the reef
    const jx = w - 82;
    const box = () => cham(x, jx, 12, 62, 48, 4);
    plate(x, box, [jx, 12, jx + 62, 60], R, { hi: '#6b7478', rustEvery: 600, rustScale: 0.6 }); bevel(x, box, 1);
    for (const [bx, by] of [[jx + 6, 18], [jx + 56, 18], [jx + 6, 54], [jx + 56, 54]]) bolt(x, bx, by, 2.4);
    x.save(); outer(); x.rect(90, 10, w - 180, h - 20); x.clip('evenodd');
    reef(x, w - 46, 34, 30, 22, R, 1.3, -P * 0.4);
    brain(x, w - 40, 42, 18, 14, R, '#b6a06a');
    reef(x, w - 76, 60, 14, 8, R, 0.9, P * 0.5);
    tendril(x, [[w - 60, 64], [w - 80, 68], [w - 98, 63], [w - 104, 56]], 5, 1.8, R, 1);
    x.restore();
    x.restore();
  };
}
// A symbol cell: one barnacle, the same seed lit or unlit; its aperture glows lumen (on) or breach (warn).
function cell(mode) {
  return (x) => {
    const R = RNG('cell');
    if (mode) { const g = x.createRadialGradient(6, 7, 0, 6, 7, 8); g.addColorStop(0, hexA(mode === 2 ? EX.breach : EX.lumen, 0.55)); g.addColorStop(1, 'rgba(0,0,0,0)'); x.fillStyle = g; x.fillRect(0, 0, 12, 14); }
    barnacle(x, 6, 7.4, 5.4, R, mode === 2 ? EX.breach : mode ? EX.lumen : 0);
    if (!mode) { x.fillStyle = 'rgba(8,12,12,0.35)'; disc(x, 6, 7.4, 5.6); x.fill(); }
  };
}
const nine = {
  'frame.panel': { size: [420, 300], m: [32, 32, 32, 32], draw: (x, w, h) => frame(x, w, h, { m: 32, band: 24, c: 10, key: 'panel' }) },
  'frame.inset': { size: [220, 160], m: [14, 14, 14, 14], draw: (x, w, h) => frame(x, w, h, { m: 14, band: 9, c: 5, key: 'inset' }) },
  'bar.track': { size: [96, 22], m: [0, 14, 0, 14], inset: [10, 4], draw: (x, w, h) => {
    const R = RNG('track'), body = () => cham(x, 1, 1, w - 2, h - 2, 4);
    plate(x, body, [0, 0, w, h], R, { where: (px) => (px < 12 || px > w - 12 ? 1 : 0), rustEvery: 500, rustScale: 0.35, saltEvery: 20 });
    bevel(x, body, 1);
    recess(x, () => F.rr(x, 9, 3.5, w - 18, h - 7, 2), [9, 3.5, w - 9, h - 3.5], '#071012');
    rivet(x, 5, h / 2, 1.8); rivet(x, w - 5, h / 2, 1.8);
  } },
  'bar.fill': { size: [64, 10], m: [0, 8, 0, 8], draw: (x, w, h) => vein(x, [[4, h / 2], [w - 4, h / 2]], 6, RNG('fill'), 1, false) },
  'bar.fill.red': { size: [64, 10], m: [0, 8, 0, 8], draw: (x, w, h) => vein(x, [[4, h / 2], [w - 4, h / 2]], 6, RNG('fillr'), 1, false, EX.breach, EX.breachHot) },
  'bar.cell.off': { size: [12, 14], gap: 2, draw: cell(0) },
  'bar.cell.on': { size: [12, 14], gap: 2, draw: cell(1) },
  'bar.cell.warn': { size: [12, 14], gap: 2, draw: cell(2) },
  'button.idle': { size: [150, 48], m: [24, 26, 24, 28], draw: hatch(0) },
  'button.hover': { size: [150, 48], m: [24, 26, 24, 28], draw: hatch(1) },
  'button.pressed': { size: [150, 48], m: [24, 26, 24, 28], draw: hatch(2) },
  'tab.idle': { size: [120, 40], m: [16, 16, 16, 16], draw: tab(false) },
  'tab.active': { size: [120, 40], m: [16, 16, 16, 16], draw: tab(true) },
  'strip.top': { size: [1200, 72], m: [12, 100, 12, 100], draw: strip(false) },
  'strip.bottom': { size: [1200, 72], m: [12, 100, 12, 100], draw: strip(true) },
  'divider': { size: [128, 10], m: [0, 18, 0, 18], draw: (x, w, h) => {
    const R = RNG('divider');
    vein(x, [[6, h / 2], [w - 6, h / 2]], 3.6, R, 0.5, false);
    for (const cx of [9, w - 9]) { const g = x.createLinearGradient(0, 0, 0, h); g.addColorStop(0, '#b5bfc1'); g.addColorStop(1, '#262d30'); x.fillStyle = g; x.fillRect(cx - 3, 0.5, 6, h - 1); x.strokeStyle = '#060909'; x.lineWidth = 0.7; x.strokeRect(cx - 3, 0.5, 6, h - 1); }
  } },
};

// ---- hero pieces -------------------------------------------------------------------------------------------------
// The helm: a bulkhead plate holding a three by three bank of square hatch keys, the stop a round valve hatch at the
// heart, veins run in the gaps between keys, reef in the corners. Key centres clockwise from the north-west.
const HELM = [[60, 60], [150, 60], [240, 60], [240, 150], [240, 240], [150, 240], [60, 240], [60, 150]];
function squareHatch(x, cx, cy, s, R) {
  const h = s / 2;
  cham(x, cx - h + 2, cy - h + 3, s, s, 6); x.fillStyle = 'rgba(0,0,0,0.5)'; x.fill();
  const body = () => cham(x, cx - h, cy - h, s, s, 6);
  plate(x, body, [cx - h, cy - h, cx + h, cy + h], R, { where: (px, py) => (Math.max(Math.abs(px - cx), Math.abs(py - cy)) > h - 10 ? 1 : 0), rustEvery: 1000, rustScale: 0.5, saltEvery: 22 });
  bevel(x, body, 1.2);
  const win = () => F.rr(x, cx - h + 11, cy - h + 11, s - 22, s - 22, 4);
  recess(x, win, [cx - h + 11, cy - h + 11, cx + h - 11, cy + h - 11], '#0a1a1c');
  x.save(); win(); x.clip(); const g = x.createRadialGradient(cx, cy, 0, cx, cy, h); g.addColorStop(0, hexA(EX.lumen, 0.16)); g.addColorStop(1, 'rgba(0,0,0,0)'); x.fillStyle = g; x.fillRect(cx - h, cy - h, s, s); x.restore();
  for (const [sx, sy] of [[-1, -1], [1, -1], [-1, 1], [1, 1]]) bolt(x, cx + sx * (h - 5.5), cy + sy * (h - 5.5), 2.6, 0.3 + sx * 0.2);
}
const hero = {
  nameplate: (x, w, h) => {
    const R = RNG('plate'), outer = () => cham(x, 1, 1, w - 2, h - 2, [10, 4, 10, 4]);
    plate(x, outer, [0, 0, w, h], R, { where: (px) => (px < 62 || px > 428 ? 1 : 0.15), rustEvery: 1400, rustScale: 0.7, saltEvery: 24 });
    bevel(x, outer, 1.3);
    const field = () => F.rr(x, 62, 9, 366, 38, 3);
    recess(x, field, [62, 9, 428, 47], EX.glass);
    // left: a porthole lens on the plate, ringed with bolts
    disc(x, 32, 29, 21); x.fillStyle = 'rgba(0,0,0,0.5)'; x.fill();
    disc(x, 31, 28, 21); const rg = x.createRadialGradient(25, 21, 2, 31, 28, 21); rg.addColorStop(0, '#a8b3b5'); rg.addColorStop(1, '#283033'); x.fillStyle = rg; x.fill(); x.strokeStyle = '#060909'; x.lineWidth = 1; x.stroke();
    disc(x, 31, 28, 13); const lg = x.createRadialGradient(28, 24, 1, 31, 28, 13); lg.addColorStop(0, EX.lumenHot); lg.addColorStop(0.35, EX.lumen); lg.addColorStop(1, EX.lumenLo); x.save(); x.shadowColor = EX.lumen; x.shadowBlur = 8; x.fillStyle = lg; x.fill(); x.restore();
    x.strokeStyle = '#060909'; x.lineWidth = 1.2; x.stroke();
    for (let i = 0; i < 6; i++) { const a = i * P / 3 + 0.4; bolt(x, 31 + Math.cos(a) * 17, 28 + Math.sin(a) * 17, 1.9, a); }
    // both ends crusted over, kept off the name field and the lens
    x.save(); outer(); x.rect(428, 7, -368, 42); x.moveTo(45, 28); x.arc(31, 28, 14, 0, 2 * P, true); x.closePath(); x.clip('evenodd');
    reef(x, 14, 46, 14, 9, R, 0.9, P * 0.7); reef(x, 50, 8, 12, 6, R, 0.7, -P / 2); reef(x, 52, 50, 10, 6, R, 0.7, P / 2);
    barnacles(x, 31, 28, 21, 21, 14, 0.9, 3.2, R);
    reef(x, w - 16, 28, 14, 24, R, 1.15, 0); reef(x, w - 26, 50, 12, 6, R, 0.7, P / 2);
    coral(x, w - 18, 54, -P / 2 - 0.25, 13, 3.2, 2, R);
    tendril(x, [[w - 6, 6], [w - 20, 4], [w - 30, 12], [w - 26, 26]], 4.4, 1.6, R, 1);
    x.restore();
    rivet(x, 440, 9, 2, R); rivet(x, 440, 47, 2, R);
  },
  'helm.pad': (x, w) => {
    const R = RNG('helm'), outer = () => cham(x, 2, 2, w - 4, w - 4, 26);
    plate(x, outer, [0, 0, w, w], R, { rustEvery: 2400, saltEvery: 28 });
    bevel(x, outer, 1.5);
    // the bank's floor: a grating seen through the gaps
    x.save(); F.rr(x, 18, 18, w - 36, w - 36, 6); x.clip(); x.fillStyle = '#121a1c'; x.fillRect(18, 18, w - 36, w - 36);
    for (let i = 18; i < w - 18; i += 6) { x.fillStyle = 'rgba(120,135,138,0.22)'; x.fillRect(i, 18, 1.4, w - 36); }
    for (let j = 18; j < w - 18; j += 18) { x.fillStyle = 'rgba(0,0,0,0.55)'; x.fillRect(18, j, w - 36, 2); x.fillStyle = 'rgba(150,165,168,0.18)'; x.fillRect(18, j + 2, w - 36, 0.8); }
    x.restore();
    recess(x, () => F.rr(x, 18, 18, w - 36, w - 36, 6), [18, 18, w - 18, w - 18], 'rgba(0,0,0,0)');
    // veins in the gaps between keys (the conduit-keepers)
    vein(x, [[22, 105], [105, 106], [195, 104], [278, 105]], 3.6, R, 0.55);
    vein(x, [[22, 196], [105, 195], [195, 196], [278, 194]], 3.6, R, 0.55);
    vein(x, [[105, 22], [104, 105]], 3, R, 0.45); vein(x, [[196, 278], [195, 196]], 3, R, 0.45);
    // the keys
    for (const [kx, ky] of HELM) squareHatch(x, kx, ky, 68, R);
    // the stop: a round valve hatch with a lit ring
    disc(x, 152, 153, 36); x.fillStyle = 'rgba(0,0,0,0.5)'; x.fill();
    disc(x, 150, 150, 36); const rg = x.createRadialGradient(140, 138, 4, 150, 150, 36); rg.addColorStop(0, '#9ca7aa'); rg.addColorStop(1, '#2b3336'); x.fillStyle = rg; x.fill(); x.strokeStyle = '#060909'; x.lineWidth = 1.2; x.stroke();
    disc(x, 150, 150, 25); x.fillStyle = '#0a1a1c'; x.fill(); x.save(); x.shadowColor = EX.lumen; x.shadowBlur = 6; x.strokeStyle = hexA(EX.lumen, 0.7); x.lineWidth = 1.6; x.stroke(); x.restore();
    for (let i = 0; i < 8; i++) { const a = i * P / 4 + P / 8; bolt(x, 150 + Math.cos(a) * 30.5, 150 + Math.sin(a) * 30.5, 2.2, a); }
    // the reef packed into every gap and round the rim, clipped clear of every key and the stop
    x.save(); outer();
    for (const [kx, ky] of HELM) { x.moveTo(kx - 36, ky - 36); x.lineTo(kx - 36, ky + 36); x.lineTo(kx + 36, ky + 36); x.lineTo(kx + 36, ky - 36); x.closePath(); }
    x.moveTo(188, 150); x.arc(150, 150, 38, 0, 2 * P, true); x.closePath(); x.clip('evenodd');
    vein(x, [[22, 150], [60, 113], [150, 108], [240, 113], [278, 150]], 2.6, R, 0.9, false);
    for (const [px, py, rx, ry] of [[105, 60, 9, 24], [195, 60, 9, 24], [105, 240, 9, 24], [195, 240, 9, 24], [60, 105, 24, 9], [240, 105, 24, 9], [60, 195, 24, 9], [240, 195, 24, 9]]) reef(x, px, py, rx, ry, R, 0.6, rx > ry ? -P / 2 : 0);
    for (const [px, py] of [[105, 105], [195, 105], [105, 195], [195, 195]]) { crust(x, px, py, 11, 11, R); barnacles(x, px, py, 9, 9, 8, 1, 3.8, R); }
    anemone(x, 105, 196, 7, R, '#d8b860'); anemone(x, 196, 104, 6, R, '#8fd0c0');
    reef(x, 18, 18, 16, 16, R, 1.1, -P * 0.75); reef(x, 282, 18, 16, 16, R, 1, -P * 0.25);
    reef(x, 18, 282, 18, 16, R, 1.2, P * 0.75); reef(x, 282, 282, 16, 16, R, 1.1, P * 0.25);
    brain(x, 24, 278, 15, 13, R, '#b6a06a'); plateCoral(x, 286, 150, 14, P, R);
    for (const [px, py, rx, ry] of [[150, 12, 50, 8], [150, 288, 50, 8], [12, 150, 8, 50], [288, 150, 8, 40]]) reef(x, px, py, rx, ry, R, 0.7);
    tendril(x, [[6, 40], [12, 110], [5, 175], [12, 228], [30, 284]], 6, 2, R, 1);
    tendril(x, [[290, 260], [294, 200], [288, 140], [292, 70]], 5, 1.8, R, -1);
    tendril(x, [[110, 6], [150, 12], [190, 5], [230, 12]], 4.6, 1.8, R, 1);
    x.restore();
  },
  'ssd.bezel': (x, w, h) => {
    const R = RNG('ssd'), outer = () => cham(x, 2, 2, w - 4, h - 4, 18);
    plate(x, outer, [0, 0, w, h], R, { where: (px) => (px < 46 || px > w - 46 ? 1 : 0.4), rustEvery: 2000, saltEvery: 26 });
    bevel(x, outer, 1.5);
    const well = () => cham(x, 46, 14, w - 92, h - 28, 14);
    recess(x, well, [46, 14, w - 46, h - 14], EX.glass);
    // a faint graticule in the well
    x.save(); well(); x.clip(); x.strokeStyle = hexA(EX.lumen, 0.06); x.lineWidth = 1;
    for (let gx = 50; gx < w - 46; gx += 24) { x.beginPath(); x.moveTo(gx, 14); x.lineTo(gx, h - 14); x.stroke(); }
    for (let gy = 18; gy < h - 14; gy += 24) { x.beginPath(); x.moveTo(46, gy); x.lineTo(w - 46, gy); x.stroke(); }
    x.restore();
    // side veins, rivets
    vein(x, [[23, 40], [24, 150], [22, 260]], 5, R, 0.85);
    vein(x, [[w - 23, 40], [w - 22, 150], [w - 24, 260]], 5, R, 0.85);
    for (const py of [24, 276]) { rivet(x, 12, py, 2.4, R); rivet(x, w - 12, py, 2.4, R); }
    salt(x, 30, w - 30, h - 4, 3, R);
    // the reef: up both side bands and into the well's corners, clear of the display's ring
    x.save(); outer(); x.moveTo(230 + 118, 150); x.arc(230, 150, 118, 0, 2 * P, true); x.closePath(); x.clip('evenodd');
    reef(x, 24, 262, 22, 34, R, 1.3, P * 0.7); reef(x, 22, 40, 20, 26, R, 1, -P * 0.7); reef(x, 22, 150, 12, 40, R, 0.8, P);
    reef(x, w - 24, 34, 22, 30, R, 1.25, -P * 0.25); reef(x, w - 22, 262, 20, 30, R, 1.1, P * 0.25); reef(x, w - 20, 150, 12, 36, R, 0.8, 0);
    reef(x, 72, 266, 26, 18, R, 1, P / 2); reef(x, w - 74, 30, 24, 16, R, 0.9, -P / 2);
    brain(x, w - 26, 28, 16, 13, R, '#b6a06a'); plateCoral(x, 56, 250, 18, -P * 0.3, R);
    tendril(x, [[8, 22], [36, 10], [70, 18], [96, 10], [120, 18]], 6, 2, R, 1);
    tendril(x, [[w - 8, 290], [w - 40, 288], [w - 70, 294], [w - 100, 286]], 5.4, 2, R, -1);
    x.restore();
  },
  dial: (x, w) => {
    const R = RNG('dial'), c = w / 2;
    disc(x, c + 2, c + 3, 57); x.fillStyle = 'rgba(0,0,0,0.5)'; x.fill();
    plate(x, () => disc(x, c, c, 57), [0, 0, w, w], R, { rustEvery: 900, rustScale: 0.6, saltEvery: 22 });
    bevel(x, () => disc(x, c, c, 57), 1.3);
    recess(x, () => disc(x, c, c, 45), [c - 45, c - 45, c + 45, c + 45], EX.glass);
    for (let i = 0; i < 6; i++) { const a = i * P / 3 + 0.5; bolt(x, c + Math.cos(a) * 51, c + Math.sin(a) * 51, 2.6, a); }
    x.save(); x.beginPath(); x.arc(c, c, 58, 0, 2 * P); x.arc(c, c, 46, 0, 2 * P, true); x.clip();
    barnacles(x, c + 34, c + 38, 14, 10, 8, 1, 3.6, R);
    x.restore();
  },
  'lamp.alert.green': (x, w) => lamp(x, w, EX.sea, 'green'), 'lamp.alert.yellow': (x, w) => lamp(x, w, EX.amber, 'yellow'), 'lamp.alert.red': (x, w) => lamp(x, w, EX.breach, 'red'),
  // The Exponent's mark (original, ours): a ring of seven unlike plates bolted together (whatever is taken is joined as
  // found), round a conduit that branches upward from one root to three lit ends (power carried where it is needed).
  crest: (x, w) => {
    const R = RNG('crest'), c = w / 2;
    disc(x, c + 1.5, c + 2, 46); x.fillStyle = 'rgba(0,0,0,0.5)'; x.fill();
    disc(x, c, c, 46); x.fillStyle = EX.deep; x.fill();
    let a = -P / 2 + 0.2;
    for (let i = 0; i < 7; i++) {
      const span = 2 * P / 7 * (0.8 + R() * 0.4), a1 = Math.min(a + span, -P / 2 + 0.2 + 2 * P - 0.06 * (6 - i)) - 0.06, r0 = 31 + R() * 3, r1 = 44 - R() * 3;
      const seg = () => { x.beginPath(); x.arc(c, c, r1, a, a1); x.arc(c, c, r0, a1, a, true); x.closePath(); };
      plate(x, seg, [0, 0, w, w], R, { hi: i % 2 ? '#7a8589' : '#6d6a5e', mid: i % 3 ? EX.steel : '#5c5a50', rustEvery: 500, rustScale: 0.35 });
      bevel(x, seg, 0.9);
      const m = (a + a1) / 2; bolt(x, c + Math.cos(m) * (r0 + r1) / 2, c + Math.sin(m) * (r0 + r1) / 2, 2.2, m);
      a = a1 + 0.06;
    }
    vein(x, [[c, c + 24], [c, c + 4]], 4.2, R, 0.9, false);
    vein(x, [[c, c + 4], [c - 7, c - 6], [c - 12, c - 16]], 3.2, R, 0.9, false);
    vein(x, [[c, c + 4], [c, c - 20]], 3.2, R, 0.9, false);
    vein(x, [[c, c + 4], [c + 7, c - 6], [c + 12, c - 16]], 3.2, R, 0.9, false);
    for (const [px, py] of [[c - 12, c - 17], [c, c - 21], [c + 12, c - 17]]) { x.save(); x.shadowColor = EX.lumen; x.shadowBlur = 8; disc(x, px, py, 3.4); x.fillStyle = EX.lumenHot; x.fill(); x.restore(); }
    disc(x, c, c + 25, 4.5); x.fillStyle = '#3a4447'; x.fill(); x.strokeStyle = '#060909'; x.lineWidth = 0.8; x.stroke();
  },
};
// A caged bulkhead lamp: a bolted housing, a glowing lens behind two guard bars, a little crust on the rim.
function lamp(x, w, col, key) {
  const R = RNG('lamp.' + key), c = w / 2;
  disc(x, c + 1.5, c + 2, 30); x.fillStyle = 'rgba(0,0,0,0.5)'; x.fill();
  plate(x, () => disc(x, c, c, 30), [0, 0, w, w], R, { rustEvery: 700, rustScale: 0.4, saltEvery: 20 });
  bevel(x, () => disc(x, c, c, 30), 1);
  disc(x, c, c, 21); const g = x.createRadialGradient(c - 5, c - 6, 1, c, c, 21); g.addColorStop(0, '#ffffff'); g.addColorStop(0.3, col); g.addColorStop(1, mix(col, '#000000', 0.55));
  x.save(); x.shadowColor = col; x.shadowBlur = 12; x.fillStyle = g; x.fill(); x.restore(); x.strokeStyle = '#060909'; x.lineWidth = 1.2; x.stroke();
  for (const dx of [-7, 7]) { const bg = x.createLinearGradient(c + dx - 2, 0, c + dx + 2, 0); bg.addColorStop(0, '#2a3235'); bg.addColorStop(0.5, '#b5bfc1'); bg.addColorStop(1, '#2a3235'); x.fillStyle = bg; x.fillRect(c + dx - 1.8, c - 21, 3.6, 42); }
  for (let i = 0; i < 4; i++) { const a = i * P / 2 + P / 4; bolt(x, c + Math.cos(a) * 25.5, c + Math.sin(a) * 25.5, 2.2, a); }
  barnacles(x, c + 18, c + 22, 7, 5, 4, 0.8, 2.4, R);
}
// The column's ground (CLAUDE.md section 7: no black behind a metal skin's panels): a bulkhead of riveted plates over
// deck grating, two veins climbing it, and the reef grown up from its foot. 472 x 1080, drawn behind the panels.
function column(x, w, h) {
  const R = RNG('column');
  x.fillStyle = '#121a1c'; x.fillRect(0, 0, w, h);
  const seamsX = [0, 150, 322, w], seamsY = [0, 260, 540, 810, h];
  for (let i = 0; i < seamsX.length - 1; i++) for (let j = 0; j < seamsY.length - 1; j++) {
    const X = seamsX[i] + 1, Y = seamsY[j] + 1, W = seamsX[i + 1] - seamsX[i] - 2, H = seamsY[j + 1] - seamsY[j] - 2, p = () => { x.beginPath(); x.rect(X, Y, W, H); };
    const tone = R();
    plate(x, p, [X, Y, X + W, Y + H], R, { hi: mix('#4e585c', '#5a574d', tone), mid: mix('#3a4347', '#46443c', tone), lo: '#22292c', rustEvery: 4000, saltEvery: 34, rustScale: 1.6, where: (px, py) => (Math.min(Math.abs(px - X), Math.abs(px - X - W), Math.abs(py - Y), Math.abs(py - Y - H)) < 16 ? 1 : 0.35) });
    bevel(x, p, 1.2);
    for (let k = X + 10; k < X + W - 6; k += 22) { rivet(x, k, Y + 7, 1.9, R); rivet(x, k, Y + H - 7, 1.9, R); }
  }
  // veins climbing the bulkhead at its edges, and along the seams
  vein(x, [[4, h], [5, 700], [3, 380], [5, 0]], 5, R, 0.5, false);
  vein(x, [[w - 4, h], [w - 3, 600], [w - 5, 250], [w - 4, 0]], 5, R, 0.5, false);
  vein(x, [[150, h], [151, 540], [149, 0]], 3.4, R, 0.35, false);
  // the reef, grown up from the foot and down from the head, in layers: crust first, then what settles on it
  for (let i = 0; i < 26; i++) crust(x, R() * w, h - Math.pow(R(), 1.6) * 160, 14 + R() * 26, 8 + R() * 16, R, R() < 0.6 ? '#a1707a' : '#567f6c');
  for (let i = 0; i < 10; i++) crust(x, R() * w, Math.pow(R(), 1.6) * 60, 10 + R() * 20, 6 + R() * 10, R, R() < 0.5 ? '#a1707a' : '#567f6c');
  // most of the bulkhead is reef now: masses on a jittered grid, a few left out so patches of plate still show
  for (let gy = 30; gy < h; gy += 58) for (let gx = 30; gx < w; gx += 62) {
    if (R() < 0.22) continue;
    reef(x, gx + (R() - 0.5) * 30, gy + (R() - 0.5) * 30, 26 + R() * 10, 22 + R() * 10, R, 1.2 + R() * 0.5, -P / 2 + (R() - 0.5) * 2);
  }
  for (let i = 0; i < 16; i++) { const px = R() * w, py = R() * h; if (R() < 0.5) brain(x, px, py, 10 + R() * 12, 9 + R() * 9, R, R() < 0.5 ? '#b6a06a' : '#8fa07a'); else plateCoral(x, px, py, 12 + R() * 10, -P / 2 + (R() - 0.5) * 2, R); }
  for (let i = 0; i < 6; i++) { const sx = 20 + R() * (w - 40); tendril(x, [[sx, h], [sx + (R() - 0.5) * 80, h * 0.75], [sx + (R() - 0.5) * 80, h * 0.5], [sx + (R() - 0.5) * 80, h * 0.25], [sx + (R() - 0.5) * 60, 20 + R() * 100]], 8, 3, R, R() < 0.5 ? 1 : -1); }
  for (let i = 0; i < 9; i++) brain(x, 20 + R() * (w - 40), h - 10 - R() * 40, 12 + R() * 16, 10 + R() * 10, R);
  for (let i = 0; i < 6; i++) coral(x, 10 + R() * (w - 20), h - 4, -P / 2 + (R() - 0.5) * 0.8, 16 + R() * 14, 4, 3, R);
  barnacles(x, w / 2, h - 30, w / 2, 30, 60, 1.2, 6, R);
  barnacles(x, w / 2, 14, w / 2, 14, 40, 1, 4.6, R);
  for (const [px, py] of [[8, 300], [w - 8, 520], [8, 780], [w - 8, 900]]) barnacles(x, px, py, 8, 40, 14, 1, 4, R);
  tendril(x, [[2, 120], [16, 200], [4, 300], [14, 420]], 5, 2, R, 1);
  tendril(x, [[w - 2, 640], [w - 14, 720], [w - 4, 820], [w - 14, 930]], 5, 2, R, -1);
}

// The overgrowth (the page's `overgrow` hook): what has grown across the console, between and over its pieces. It keeps
// to what no label, readout or key face uses: the column's outer edges (14 px in), the gaps between pieces (a cable lies
// in each, no thicker than the pieces' plain bands), the strips' outer lips and their ends, and a little way over the
// battle's edge where the console meets it. r gives every slot's rectangle in console px.
function overgrow(x, r) {
  const R = RNG('over'), col = 472;
  const blocks = ['nameplate', 'helm.pad', 'ssd.bezel', 'panel.weapons', 'dial'].map((k) => r[k]).filter(Boolean);
  // the column's left edge: reef climbing it, a thick cable winding up through it
  for (let y = 40; y < 1060; y += 64 + R() * 30) reef(x, 6 + R() * 4, y, 8, 22, R, 0.8, P);
  tendril(x, [[4, 1080], [12, 900], [3, 720], [13, 540], [4, 360], [12, 200], [6, 70]], 8, 3, R, 1);
  // its right edge, swelling a little over the battle
  for (let y = 100; y < 1000; y += 70 + R() * 30) reef(x, col - 2 + R() * 6, y, 9, 24, R, 0.85, 0);
  tendril(x, [[col + 4, 990], [col - 6, 820], [col + 6, 640], [col - 4, 470], [col + 6, 300], [col - 2, 120]], 8, 3, R, -1);
  // a cable in every gap between the column's pieces, wrapping in from the edges, barnacles where it crosses
  for (let i = 0; i < blocks.length - 1; i++) {
    const a = blocks[i], gy = a[1] + a[3] + 3;
    tendril(x, [[-4, gy + 2], [40, gy - 1], [90, gy + 1.5], [140, gy], [170, gy - 1]], 6, 2, R, i % 2 ? 1 : -1);
    tendril(x, [[col + 6, gy - 1], [430, gy + 1.5], [380, gy - 1], [330, gy]], 6, 2, R, i % 2 ? -1 : 1);
    barnacles(x, 14, gy, 8, 4, 6, 1, 3.4, R); barnacles(x, col - 12, gy, 8, 4, 6, 1, 3.4, R);
    crust(x, 230 + (R() - 0.5) * 120, gy, 26, 3.5, R, R() < 0.5 ? '#a1707a' : '#567f6c');
  }
  // the gap between the helm and its readout
  const hp = r['helm.pad'];
  if (hp) { const gx = hp[0] + hp[2] + 3; tendril(x, [[gx, hp[1] + hp[3] + 4], [gx - 1.5, hp[1] + hp[3] * 0.6], [gx + 1.5, hp[1] + hp[3] * 0.3], [gx, hp[1] - 2]], 5, 2, R, 1); barnacles(x, gx, hp[1] + hp[3] * 0.5, 3, 20, 6, 0.9, 2.6, R); }
  // where the column meets the strips: big reef masses over the corners
  for (const [cy, face] of [[74, P * 0.25], [1006, -P * 0.25]]) { reef(x, col + 2, cy, 30, 20, R, 1.5, face); reef(x, col + 30, cy + (cy < 500 ? -8 : 8), 22, 10, R, 1.1, face); brain(x, col - 6, cy, 13, 11, R, '#b6a06a'); }
  // the strips: growth along their outer lips and hanging over the battle's edge, crusted ends
  for (const [y, dir] of [[72, 1], [1008, -1]]) {
    for (let px = 540; px < 1900; px += 70 + R() * 70) {
      if (dir < 0 && px < 940) continue;   // keep the battle's readouts clear
      const L = dir > 0 ? 18 + R() * 30 : 6 + R() * 8, k = R();
      if (k < 0.35) tendril(x, [[px, y + dir * 2], [px + (R() - 0.5) * 16, y + dir * L * 0.5], [px + (R() - 0.5) * 20, y + dir * L]], 4.4, 1.6, R, R() < 0.5 ? 1 : -1);
      else if (k < 0.7) coral(x, px, y + dir * 2, dir > 0 ? P / 2 + (R() - 0.5) * 0.7 : -P / 2 + (R() - 0.5) * 0.7, L * 0.42, 3.2, 2, R, R() < 0.5 ? EX.coral : '#e0a24a');
      else if (dir > 0) anemone(x, px, y + 6, 7 + R() * 4, R, ['#d9768a', '#d8b860', '#8fd0c0'][Math.floor(R() * 3)]);
      crust(x, px + 24, y + dir * 1, 26, 5, R, R() < 0.5 ? '#a1707a' : '#567f6c');
      barnacles(x, px + 26, y + dir * 3, 22, 4, 9, 1, 3.8, R);
    }
    tendril(x, [[560, y + dir * 3], [900, y + dir * 8], [1250, y + dir * 2], [1600, y + dir * 9], [1900, y + dir * 3]], 7, 3, R, dir);
    reef(x, 1904, y, 24, 18, R, 1.3, dir > 0 ? P * 0.75 : -P * 0.75);
  }
}

// ---- icons: pictures of the job, cut as stencils ---------------------------------------------------------------------
// Utilitarian stencil marks: solid shapes in one ink (lumen), broken by the bridges a stencil needs. Colour is for state.
function icon(name) {
  return F.paint(48, 48, (x) => {
    const ink = EX.lumen;
    x.fillStyle = ink; x.strokeStyle = ink; x.lineCap = 'butt'; x.lineJoin = 'miter';
    const ring = (cx, cy, r, lw) => { x.lineWidth = lw; x.beginPath(); x.arc(cx, cy, r, 0, 2 * P); x.stroke(); };
    const bar = (a, b, c, d, lw) => { x.lineWidth = lw; x.beginPath(); x.moveTo(a, b); x.lineTo(c, d); x.stroke(); };
    // a stencil bridge: a gap cut across the mark at (cx, cy), at angle a, gw wide
    const cut = (cx, cy, a, gw = 2.4, L = 30) => { x.save(); x.globalCompositeOperation = 'destination-out'; x.translate(cx, cy); x.rotate(a); x.fillRect(-gw / 2, -L / 2, gw, L); x.restore(); };
    ({
      // a ship's wheel: rim, hub and eight handles, the rim bridged four times
      helm: () => { ring(24, 24, 13, 4); disc(x, 24, 24, 5); x.fill(); for (let i = 0; i < 8; i++) { const a = i * P / 4; bar(24 + Math.cos(a) * 5, 24 + Math.sin(a) * 5, 24 + Math.cos(a) * 21, 24 + Math.sin(a) * 21, 3.2); } for (let i = 0; i < 4; i++) { const a = P / 8 + i * P / 2; cut(24 + Math.cos(a) * 13, 24 + Math.sin(a) * 13, a, 2.2, 7); } },
      // a target: two rings and four ticks, bridged on the diagonals
      fire: () => { ring(24, 24, 17, 3.6); ring(24, 24, 8.5, 3.2); disc(x, 24, 24, 2.6); x.fill(); for (const a of [0, P / 2, P, 1.5 * P]) bar(24 + Math.cos(a) * 12, 24 + Math.sin(a) * 12, 24 + Math.cos(a) * 23, 24 + Math.sin(a) * 23, 3.2); for (let i = 0; i < 4; i++) { const a = P / 4 + i * P / 2; cut(24 + Math.cos(a) * 13, 24 + Math.sin(a) * 13, a, 2.2, 16); } },
      // six shield facings round a hull
      shields: () => { for (let i = 0; i < 6; i++) { const a = -P / 2 + i * P / 3; x.lineWidth = 4.4; x.beginPath(); x.arc(24, 24, 18, a - P / 6 + 0.16, a + P / 6 - 0.16); x.stroke(); } x.beginPath(); x.moveTo(24, 13); x.lineTo(30, 22); x.lineTo(29, 35); x.lineTo(19, 35); x.lineTo(18, 22); x.closePath(); x.fill(); cut(24, 27, P / 2, 2, 14); },
      // an emitter's widening beam drawing a crate in
      tractor: () => { x.fillRect(4, 18, 8, 12); x.lineWidth = 2.4; x.beginPath(); x.moveTo(12, 21); x.lineTo(32, 12); x.moveTo(12, 27); x.lineTo(32, 36); x.stroke(); for (const r of [11, 17]) { x.beginPath(); x.arc(8, 24, r, -0.38, 0.38); x.stroke(); } x.fillRect(33, 15, 12, 18); cut(39, 24, P / 2, 2, 14); cut(8, 24, 0, 2, 14); },
      // a figure, whole above, dissolving into slices below
      transport: () => { disc(x, 24, 9, 5.5); x.fill(); x.beginPath(); x.moveTo(14, 36); x.lineTo(15, 21); x.lineTo(33, 21); x.lineTo(34, 36); x.closePath(); x.fill(); x.fillRect(16, 35, 6, 9); x.fillRect(26, 35, 6, 9); x.globalCompositeOperation = 'destination-out'; for (let y = 26, k = 0; y < 46; y += 3.4, k++) x.fillRect(0, y, 48, 1 + k * 0.32); x.globalCompositeOperation = 'source-over'; },
      // an atom: three orbits and a nucleus, each orbit bridged
      science: () => { x.lineWidth = 3; for (const r of [0, P / 3, -P / 3]) { x.beginPath(); x.ellipse(24, 24, 20, 7.5, r, 0, 2 * P); x.stroke(); } disc(x, 24, 24, 4.4); x.fill(); for (const r of [0, P / 3, -P / 3]) cut(24 + Math.cos(r) * 20, 24 + Math.sin(r) * 20, r, 2.2, 6); },
      // a dish on its stand, sending
      comms: () => { x.beginPath(); x.arc(17, 27, 12, P * 0.75, P * 1.75, true); x.closePath(); x.fill(); bar(17, 27, 12, 44, 3.4); bar(17, 27, 22, 44, 3.4); disc(x, 27, 17, 2.6); x.fill(); x.lineWidth = 2.6; for (const r of [8, 15]) { x.beginPath(); x.arc(27, 17, r, -P / 2 + 0.1, -0.1); x.stroke(); } cut(17, 27, P * 0.25, 2.2, 26); },
      // a wrench
      repair: () => { bar(10, 38, 27, 21, 6.4); x.lineWidth = 5.4; x.beginPath(); x.arc(31, 17, 8.5, -P / 4 + 0.95, -P / 4 - 0.95 + 2 * P); x.stroke(); cut(19, 29, P / 4, 2.2, 12); },
      // a star map: three systems joined by lanes
      map: () => { x.lineWidth = 2.6; x.beginPath(); x.moveTo(11, 13); x.lineTo(37, 19); x.lineTo(20, 37); x.closePath(); x.stroke(); for (const [cx, cy] of [[11, 13], [37, 19], [20, 37]]) { x.fillRect(cx - 5, cy - 5, 10, 10); cut(cx, cy, 0, 1.8, 12); } },
      // a gear: a ring and its teeth, cut at the hub
      settings: () => { ring(24, 24, 11, 5); for (let i = 0; i < 8; i++) { const a = i * P / 4; bar(24 + Math.cos(a) * 13, 24 + Math.sin(a) * 13, 24 + Math.cos(a) * 20, 24 + Math.sin(a) * 20, 5.6); } cut(24, 24, P / 4, 2.2, 30); },
    })[name]();
  });
}

// ---- the live readouts -----------------------------------------------------------------------------------------------
// A stencil arrow: a solid head and shaft in lumen, a bridge across the shaft, outlined dark so it reads on the hatch.
function stencilArrow(x, col) {
  const shape = () => { x.beginPath(); x.moveTo(13, 0); x.lineTo(1, -10); x.lineTo(1, -4); x.lineTo(-12, -4); x.lineTo(-12, 4); x.lineTo(1, 4); x.lineTo(1, 10); x.closePath(); };
  shape(); x.strokeStyle = '#040707'; x.lineWidth = 3.2; x.lineJoin = 'round'; x.stroke();
  x.save(); x.shadowColor = col; x.shadowBlur = 5; shape(); x.fillStyle = col; x.fill(); x.restore();
  x.fillStyle = '#0a1a1c'; x.fillRect(-5.5, -5, 2.2, 10);
}

// ---- the skin --------------------------------------------------------------------------------------------------
F.register('exponent', {
  name: 'Exponent', kind: 'Painted in canvas (first pass): a utilitarian steel console overgrown like a wreck on a reef: rust, salt, barnacles, coral, cabled tendrils and lit veins, hatch keys', bg: EX.bg, sampleBg: '#2a3236',
  css: { '--s-ink': EX.bone, '--s-dim': '#9fb0ad', '--s-num': EX.lumen, '--s-body': '"Chivo Mono", monospace', '--btn-ink': '#eef8f4', '--btn-shadow': '0 1px 2px #000, 0 0 3px #000', '--btn-justify': 'center',
    '--btn-font': '400 15px/1 "Allerta Stencil", sans-serif', '--btn-track': '0.08em', '--btn-case': 'uppercase', '--btn-motion': 'padding-top 0.06s', '--btn-sink': '1px', '--btn-pb': '2px',
    '--tab-font': '400 12px/1 "Allerta Stencil", sans-serif', '--tab-ink': '#9fb0ad', '--tab-ink-on': EX.lumenHot, '--title-font': '400 18px/1 "Allerta Stencil", sans-serif', '--title-ink': EX.bone,
    '--lbl-font': '500 13px/1 "Chivo Mono", monospace', '--num-font': '700 24px/1 "Chivo Mono", monospace',
    // The strips are 72 px; their open band is the dark channel, rows 11-61.
    '--stt-pt': '12px', '--stt-pb': '12px', '--stb-pt': '12px', '--stb-pb': '12px', '--strip-lamp': '44px', '--tab-pb': '4px' },
  accent: EX.lumen, warn: EX.breach,
  // The title stencilled at the head of the well, in bone, over a shadow.
  title: (t) => { t.style.cssText = 'position:relative;justify-self:start;align-self:flex-start;margin-left:2px;letter-spacing:0.1em;text-transform:uppercase;text-shadow:0 1px 2px #000;'; },
  plateText: { left: 72, top: 13, width: 344, height: 30, font: '400 19px/1 "Allerta Stencil", sans-serif', color: EX.bone, shadow: '0 1px 2px #000', sub: 'ACCRETION 41 · KEEPER OF LINES' },
  helmKeys: HELM, shield: [EX.lumen, EX.amber, EX.breach],
  fonts: ['400 20px "Allerta Stencil"', '700 20px "Chivo Mono"', '500 20px "Chivo Mono"'], view: { title: '400 26px "Allerta Stencil"', body: '700 21px "Chivo Mono"', upper: true, ink: EX.bone, dim: '#a9c4bf' },
  build: () => Object.assign(F.fromPainters(nine, hero, icon, false), { column: F.png(F.paint(472, 1080, column)) }),
  overgrow,
  live: {
    arrow: (x) => stencilArrow(x, EX.lumen),
    helmCentre: (x, c) => { x.fillStyle = EX.bone; x.font = '400 13px "Allerta Stencil"'; x.textAlign = 'center'; x.shadowColor = '#000'; x.shadowBlur = 3; x.fillText('STOP', c, c + 5); x.shadowBlur = 0; },
    // The hull as the assimilators keep it: modules taken from other ships, joined by plain collars, a conduit down its
    // spine to each mount. Shields: six banks of ticks, coloured by strength.
    ssd: (x, cx, cy, sh, col) => {
      for (let i = 0; i < 6; i++) {
        const a = -P / 2 + i * P / 3; x.save(); x.strokeStyle = col(sh[i]); x.shadowColor = col(sh[i]); x.shadowBlur = 5;
        for (let k = -5; k <= 5; k++) { const t = a + k * (P / 3 - 0.22) / 10, r0 = 94, r1 = k % 5 === 0 ? 106 : 101; x.lineWidth = 2.4; x.beginPath(); x.moveTo(cx + Math.cos(t) * r0, cy + Math.sin(t) * r0); x.lineTo(cx + Math.cos(t) * r1, cy + Math.sin(t) * r1); x.stroke(); }
        x.restore();
      }
      const mods = [[-12, -70, 24, 26, 4], [-20, -44, 40, 30, 2], [-30, -14, 60, 34, 6], [-22, 20, 44, 26, 2], [-14, 46, 28, 22, 3], [-44, -6, 14, 26, 2], [30, -6, 14, 26, 2]];
      for (const [mx, my, mw, mh, c] of mods) { cham(x, cx + mx, cy + my, mw, mh, c); x.fillStyle = hexA(EX.lumen, 0.12); x.fill(); x.strokeStyle = EX.lumen; x.lineWidth = 1.6; x.stroke(); }
      for (const yy of [-44, -14, 20, 46]) { x.fillStyle = hexA(EX.lumen, 0.7); x.fillRect(cx - 8, cy + yy - 1.5, 16, 3); }
      x.strokeStyle = hexA(EX.lumenHot, 0.8); x.lineWidth = 1.2; x.setLineDash([3, 2]); x.beginPath(); x.moveTo(cx, cy - 66); x.lineTo(cx, cy + 64); x.stroke(); x.setLineDash([]);
      for (const [mx, my] of [[cx, cy - 58], [cx - 37, cy + 7], [cx + 37, cy + 7], [cx, cy + 58]]) { x.save(); x.shadowColor = EX.lumen; x.shadowBlur = 6; x.fillStyle = EX.lumenHot; x.fillRect(mx - 3, my - 3, 6, 6); x.restore(); }
    },
    // A sonar sweep in the gauge: range rings, a fading wedge, contacts as squares (lumen far, amber, breach near).
    dial: (x, c, cts) => {
      for (const r of [14, 28, 40]) { x.strokeStyle = hexA(EX.lumen, 0.22); x.lineWidth = 1; x.beginPath(); x.arc(c, c, r, 0, 2 * P); x.stroke(); }
      x.strokeStyle = hexA(EX.lumen, 0.16); x.beginPath(); x.moveTo(c - 42, c); x.lineTo(c + 42, c); x.moveTo(c, c - 42); x.lineTo(c, c + 42); x.stroke();
      const a0 = -0.9; for (let i = 0; i < 14; i++) { x.fillStyle = hexA(EX.lumen, 0.05 + 0.02 * i); x.beginPath(); x.moveTo(c, c); x.arc(c, c, 41, a0 - 0.9 + i * 0.065, a0 - 0.9 + (i + 1) * 0.065); x.closePath(); x.fill(); }
      x.strokeStyle = EX.lumenHot; x.lineWidth = 1.4; x.beginPath(); x.moveTo(c, c); x.lineTo(c + Math.cos(a0) * 41, c + Math.sin(a0) * 41); x.stroke();
      for (const [dx, dy] of cts) {
        const d = Math.hypot(dx, dy), cl = d < 0.56 ? EX.breach : d < 0.8 ? EX.amber : EX.lumen, s = d < 0.56 ? 7 : 5.5;
        x.save(); x.shadowColor = cl; x.shadowBlur = 6; x.fillStyle = cl; x.fillRect(c + dx * 34 - s / 2, c + dy * 34 - s / 2, s, s); x.restore();
      }
    },
    // Four corner brackets of flat bar, each bolted at its corner and bridged like a stencil.
    bracket: (x, bx, by, bw, bh) => {
      for (const [px, py, sx, sy] of [[bx, by, 1, 1], [bx + bw, by, -1, 1], [bx, by + bh, 1, -1], [bx + bw, by + bh, -1, -1]]) {
        x.save(); x.translate(px, py); x.scale(sx, sy);
        const L = () => { x.beginPath(); x.moveTo(0, 0); x.lineTo(34, 0); x.lineTo(34, 5); x.lineTo(5, 5); x.lineTo(5, 34); x.lineTo(0, 34); x.closePath(); };
        L(); x.strokeStyle = '#040707'; x.lineWidth = 2.4; x.stroke(); x.save(); x.shadowColor = EX.lumen; x.shadowBlur = 6; L(); x.fillStyle = EX.lumen; x.fill(); x.restore();
        x.fillStyle = '#020306'; x.fillRect(18, -1, 2.4, 7); x.fillRect(-1, 18, 7, 2.4);
        disc(x, 2.5, 2.5, 1.6); x.fillStyle = '#0a1a1c'; x.fill();
        x.restore();
      }
    },
  },
  world: ['One collective that grows by taking in ships, stations and crews; nothing is decorated, everything is function, and nothing taken is thrown away.',
    'Provisional and the owner\'s to set; the design draws on none of it. Three traditions shape its things: the assimilators, the conduit-keepers, the reef.',
    'Machinery kept running long past any refit: whatever is taken is bolted on as found, power runs in sheathed veins of cable and fluid line, and the growth it has absorbed is left to accrete in layers.',
    'Hulks of joined hulls under barnacle, coral and cable, like wrecks that have lain on a sea floor and still run.'],
  shapes: 'Hatch keys (a steel access hatch on a hinge, a dog latch, four bolts, a lit slot), riveted plate with patches bolted on, and a reef over all of it: coralline crust, plate, brain and branching coral, sponges, anemones, tube worms, dense barnacles, thick cabled tendrils across the gaps between panels, sheathed veins pulsing along every foot. Growth keeps to margins, edges and gaps, never a label or key face.',
  light: 'A hard work light from the upper left over chipped paint, rust and salt; the only glow is the cold lumen in the slots, the veins and the lenses.',
  motion: 'Hover lights a hatch\'s slot; pressed, the hatch sinks a pixel and its slot flares; the growth on it never moves.',
  type: '"Allerta Stencil", a stencilled sans, for keys and titles, over "Chivo Mono" for labels and numbers.',
  palette: [['Hull steel', EX.steel, 'Frames, keys, plates', 'The assimilators: nothing decorated, the plate as it was taken'], ['Rust', EX.rust, 'Wear, chips, runs', 'Kept running long past any refit'],
    ['Reef coral', EX.coral, 'Coral, tube worm plumes', 'The reef: growth tolerated and built on, in layers'], ['Barnacle bone', EX.bone, 'Text, barnacles, labels', 'What has settled and stays'],
    ['Lumen', EX.lumen, 'Lit slots, veins, readouts, icons', 'The conduit-keepers: power carried where it is needed, no further'], ['Breach', EX.breach, 'Alerts only', 'The hull open to the sea']],
});
})();
