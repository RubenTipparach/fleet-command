// Fleet Command mockups: hand-drawn pieces for the paper star chart (openspec/changes/paper-map; owner, 2026-11-08: "make
// the nebula or storm clouds like hand drawn billboard puffs of cloud. And make the planets have that hand drawn quality
// too", "I just want everything in space campaign map to look very pirate and very space"). A pirate's chart of the stars:
// sepia ink that wobbles and swells with the pen, watercolour washes that pool darker at their edges, and hatching on the
// side away from the lamp (the light comes from the upper left, as on the chart). Each piece is drawn into its own canvas,
// which the page stands on the chart as a billboard.
// The planets are not drawn here: the planet shader draws them live (docs/mockups/lib/planet-shader.js), in the same ink.
//
// Everything is seeded: the same seed draws the same piece, and the randomness is Mulberry32 (CLAUDE.md section 10, never
// a cheap hash). No three.js here, only the 2D canvas, so any page can use it.
//
//   FCDrawn.cloud({ px, main, accent, seed, storm })   a cumulus puff (storm: darker, with a forked bolt)
//   FCDrawn.sun({ px, seed })                          a sun with alternate straight and flame rays
//   FCDrawn.rock({ px, seed })                         one lumpy asteroid
//   FCDrawn.compass(g, x, y, r, seed)                  a compass rose drawn into an existing context
//   FCDrawn.INK                                        the ink colour
// Each piece returns { canvas, foot } where foot is the share of the canvas height from the top at which the piece's
// lowest point sits (for standing it on the chart). Colours are [r, g, b] in 0..1 or CSS strings.
(function () {
  const TAU = Math.PI * 2;
  const INK = '#2a1b10';
  const PAPER = [0.93, 0.87, 0.74];

  function rng(seed) { let a = seed >>> 0; return () => { a = (a + 0x6d2b79f5) >>> 0; let t = a; t = Math.imul(t ^ (t >>> 15), t | 1); t ^= t + Math.imul(t ^ (t >>> 7), t | 61); return ((t ^ (t >>> 14)) >>> 0) / 4294967296; }; }
  const mix = (a, b, k) => a.map((v, i) => v + (b[i] - v) * k);
  const css = (c, a = 1) => (typeof c === 'string' ? c : `rgba(${Math.round(c[0] * 255)}, ${Math.round(c[1] * 255)}, ${Math.round(c[2] * 255)}, ${a})`);
  const hsl = (h, s, l) => { // h in degrees, s and l in 0..1, to [r, g, b]
    const k = (n) => (n + h / 30) % 12, a = s * Math.min(l, 1 - l), f = (n) => l - a * Math.max(-1, Math.min(k(n) - 3, 9 - k(n), 1));
    return [f(0), f(8), f(4)];
  };
  function canvas(w, h) { const c = document.createElement('canvas'); c.width = Math.ceil(w); c.height = Math.ceil(h); return c; }

  // A smooth random wave along a stroke: values at knots every `every` samples, eased between, so a line wanders gently
  // instead of jittering. Returns f(i) in -1..1.
  function wave(R, n, every) {
    const knots = Array.from({ length: Math.ceil(n / every) + 2 }, () => R() * 2 - 1);
    return (i) => { const s = i / every, k = Math.floor(s), t = s - k, e = t * t * (3 - 2 * t); return knots[k] + (knots[k + 1] - knots[k]) * e; };
  }
  // Points along a polyline every `step` px.
  function resample(pts, step, closed) {
    const src = closed ? [...pts, pts[0]] : pts, out = [];
    for (let i = 0; i < src.length - 1; i++) {
      const [ax, ay] = src[i], [bx, by] = src[i + 1], d = Math.hypot(bx - ax, by - ay), n = Math.max(1, Math.round(d / step));
      for (let k = 0; k < n; k++) out.push([ax + (bx - ax) * k / n, ay + (by - ay) * k / n]);
    }
    if (!closed) out.push(src[src.length - 1]);
    return out;
  }
  // Move each point off the line along its normal by a wandering amount: the hand's wobble.
  function wobble(pts, amp, R, closed) {
    const n = pts.length, f = wave(R, n, 9);
    return pts.map(([x, y], i) => {
      const a = pts[(i - 1 + n) % n], b = pts[(i + 1) % n], dx = b[0] - a[0], dy = b[1] - a[1], l = Math.hypot(dx, dy) || 1;
      const k = (!closed && (i === 0 || i === n - 1)) ? 0 : f(i) * amp;
      return [x - dy / l * k, y + dx / l * k];
    });
  }
  // A pen stroke: the line wobbled, its width swelling and thinning with the pen's pressure, tapered at the ends of an
  // open line, and a faint second pass beside it where the ink bled.
  function ink(g, pts, { w = 2, col = INK, R, closed = false, amp = 1.2, alpha = 1 } = {}) {
    const p = wobble(resample(pts, 2.5, closed), amp, R, closed), n = p.length, press = wave(R, n, 14);
    g.save(); g.lineCap = 'round'; g.lineJoin = 'round'; g.strokeStyle = css(col); g.globalAlpha = alpha;
    for (let i = 0; i < n - (closed ? 0 : 1); i++) {
      const a = p[i], b = p[(i + 1) % n], t = i / (n - 1);
      const taper = closed ? 1 : Math.min(1, t * 6, (1 - t) * 6) * 0.75 + 0.25;
      g.lineWidth = Math.max(0.4, w * (0.75 + 0.35 * press(i)) * taper);
      g.beginPath(); g.moveTo(a[0], a[1]); g.lineTo(b[0], b[1]); g.stroke();
    }
    g.globalAlpha = alpha * 0.25; g.lineWidth = w * 0.6;
    g.beginPath(); p.forEach(([x, y], i) => (i ? g.lineTo(x + 0.8, y + 0.6) : g.moveTo(x + 0.8, y + 0.6))); if (closed) g.closePath(); g.stroke();
    g.restore();
  }
  const pathOf = (g, pts) => { g.beginPath(); pts.forEach(([x, y], i) => (i ? g.lineTo(x, y) : g.moveTo(x, y))); g.closePath(); };
  // A watercolour wash inside a closed outline: an even coat, blooms of thicker paint, the paper's grain lifting through,
  // and the pigment pooled darker along the edge where it dried.
  function wash(g, pts, col, R, { alpha = 0.85, blooms = 10, edge = 0.5 } = {}) {
    const xs = pts.map((p) => p[0]), ys = pts.map((p) => p[1]);
    const x0 = Math.min(...xs), x1 = Math.max(...xs), y0 = Math.min(...ys), y1 = Math.max(...ys), sz = Math.max(x1 - x0, y1 - y0);
    g.save(); pathOf(g, pts); g.clip();
    g.fillStyle = css(col, alpha); g.fillRect(x0 - 2, y0 - 2, x1 - x0 + 4, y1 - y0 + 4);
    for (let i = 0; i < blooms; i++) {
      const x = x0 + R() * (x1 - x0), y = y0 + R() * (y1 - y0), r = sz * (0.12 + R() * 0.3), dk = R() < 0.5;
      const gr = g.createRadialGradient(x, y, 0, x, y, r);
      gr.addColorStop(0, css(dk ? mix(col, [0, 0, 0], 0.25) : mix(col, PAPER, 0.35), 0.35)); gr.addColorStop(1, css(col, 0));
      g.fillStyle = gr; g.fillRect(x - r, y - r, 2 * r, 2 * r);
    }
    for (let i = 0; i < sz * sz / 60; i++) { g.fillStyle = css(R() < 0.5 ? PAPER : mix(col, [0, 0, 0], 0.4), 0.12); g.fillRect(x0 + R() * (x1 - x0), y0 + R() * (y1 - y0), 1.2, 1.2); }
    g.restore();
    g.save(); pathOf(g, pts); g.clip(); g.strokeStyle = css(mix(col, [0, 0, 0], 0.35), edge); g.lineWidth = Math.max(2, sz * 0.03); pathOf(g, pts); g.stroke(); g.restore();
  }
  // Hatching: wobbly parallel pen lines at `angle`, `gap` px apart, inside a clip the caller has set, over a box.
  function hatch(g, box, angle, gap, R, { w = 1, col = INK, alpha = 0.7 } = {}) {
    const [x0, y0, x1, y1] = box, cx = (x0 + x1) / 2, cy = (y0 + y1) / 2, L = Math.hypot(x1 - x0, y1 - y0) / 2 + 4;
    const dx = Math.cos(angle), dy = Math.sin(angle), nx = -dy, ny = dx;
    for (let s = -L; s <= L; s += gap * (0.8 + 0.4 * R())) {
      const ox = cx + nx * s, oy = cy + ny * s, a = -L * (0.85 + 0.15 * R()), b = L * (0.85 + 0.15 * R());
      ink(g, [[ox + dx * a, oy + dy * a], [ox + dx * b, oy + dy * b]], { w, col, R, amp: 0.6, alpha });
    }
  }
  // The outline of a union of circles seen from a point inside them: the farthest crossing along each ray, so lobes
  // meet in cusps the way a drawn cloud's do.
  function lobeOutline(lobes, cx, cy, n, flat) {
    const out = [];
    for (let i = 0; i < n; i++) {
      const a = i / n * TAU, dx = Math.cos(a), dy = Math.sin(a);
      let t = 0;
      for (const [ox, oy, r] of lobes) {
        const b = dx * (ox - cx) + dy * (oy - cy), c = (ox - cx) ** 2 + (oy - cy) ** 2 - r * r, d = b * b - c;
        if (d >= 0) t = Math.max(t, b + Math.sqrt(d));
      }
      let x = cx + dx * t, y = cy + dy * t;
      if (flat != null && y > flat) { const k = (flat - cy) / (y - cy); x = cx + (x - cx) * k; y = flat; }
      out.push([x, y]);
    }
    return out;
  }

  // A cumulus puff on the chart, drawn the way a cartoonist heaps one: puffs at the back first, then puffs in front that
  // overlap them, each puff its own washed disc with its shadowed crescent (lower right, away from the lamp) hatched and its
  // edge inked, so the overlaps leave the inner curls; then the whole silhouette inked heavier, the base cut flat. The
  // main colour sits low and the accent on the crowns. A storm is the same cloud in slate, hatched heavier, with a forked
  // bolt of lightning out of its base.
  function cloud({ px = 256, main = [0.5, 0.6, 0.9], accent = [0.85, 0.6, 0.8], seed = 1, storm = false } = {}) {
    const R = rng(seed), W = px, H = px * (storm ? 0.95 : 0.78), c = canvas(W, H), g = c.getContext('2d');
    const base = H * (storm ? 0.66 : 0.82), cx = W * 0.5, half = W * (0.3 + R() * 0.05), lobes = [];
    // the back row: crowns heaped above the middle, biggest in the centre
    const nt = 3 + Math.floor(R() * 2);
    for (let i = 0; i < nt; i++) { const t = (i + 0.5) / nt, mid = 1 - Math.abs(t - 0.5) * 1.6, r = W * (0.11 + 0.07 * mid + R() * 0.03);
      lobes.push({ x: cx - half * 0.7 + 1.4 * half * t + (R() - 0.5) * W * 0.03, y: base - r * 0.6 - W * (0.05 + 0.07 * mid) - R() * W * 0.02, r, crown: true }); }
    // the front row along the base, overlapping the crowns
    const nb = 4 + Math.floor(R() * 2);
    for (let i = 0; i < nb; i++) { const t = i / (nb - 1), r = W * (0.1 + R() * 0.035) * (1 - Math.abs(t - 0.5) * 0.45);
      lobes.push({ x: cx - half + 2 * half * t + (R() - 0.5) * W * 0.02, y: base - r * 0.42, r, crown: false }); }
    const body = storm ? mix([0.34, 0.36, 0.48], main, 0.25) : mix(main, PAPER, 0.28), crown = storm ? mix([0.5, 0.5, 0.62], accent, 0.2) : mix(mix(accent, main, 0.35), PAPER, 0.3);
    const circle = (l, k = 1, n = 64) => Array.from({ length: n }, (_, i) => { const a = i / n * TAU; return [l.x + Math.cos(a) * l.r * k, l.y + Math.sin(a) * l.r * k]; });
    // the heavy silhouette: every puff grown by the pen's width and filled in ink, under the puffs, the base ruled flat
    const pen = W / 80, ends = lobes.filter((l) => !l.crown);
    g.save(); g.beginPath(); g.rect(0, 0, W, base + pen); g.clip(); g.fillStyle = INK;
    for (const l of lobes) { pathOf(g, wobble(circle({ x: l.x, y: l.y, r: l.r + pen }), W / 500, R, true)); g.fill(); }
    g.fillRect(ends[0].x - ends[0].r * 0.6, base - pen, ends[ends.length - 1].x - ends[0].x + ends[0].r * 1.2, pen * 2);
    g.restore();
    g.save(); g.beginPath(); g.rect(0, 0, W, base); g.clip();
    for (const l of lobes) {
      const disc = circle(l), col = l.crown ? mix(crown, body, Math.max(0, (l.y - (base - W * 0.3)) / (W * 0.3)) * 0.6) : body;
      wash(g, disc, col, R, { blooms: 5, alpha: 1, edge: 0.35 });
      // the lit side, paper showing through, upper left
      const hl = g.createRadialGradient(l.x - l.r * 0.4, l.y - l.r * 0.45, 0, l.x - l.r * 0.4, l.y - l.r * 0.45, l.r * 0.7);
      hl.addColorStop(0, css(mix(col, PAPER, 0.65), 0.75)); hl.addColorStop(1, css(col, 0));
      g.save(); pathOf(g, disc); g.clip(); g.fillStyle = hl; g.fillRect(l.x - l.r, l.y - l.r, 2 * l.r, 2 * l.r);
      // tone by the pen, three zones deeper into the shadow (lower right): single lines through the mid-tone, crossed in
      // the shadow, a third set in the deepest part
      const box = [l.x - l.r, l.y - l.r, l.x + l.r, l.y + l.r], zone = (ox, oy, k) => { g.beginPath(); g.rect(l.x - l.r - 2, l.y - l.r - 2, 2 * l.r + 4, 2 * l.r + 4); g.arc(l.x + ox * l.r, l.y + oy * l.r, l.r * k, 0, TAU, true); g.clip('evenodd'); };
      const lw = W / (storm ? 150 : 175), gap = W * (storm ? 0.026 : 0.032);
      g.save(); zone(-0.85, -0.8, 1.12);
      g.fillStyle = css(mix(col, [0.05, 0.03, 0.1], 0.35), storm ? 0.45 : 0.25); g.fillRect(l.x - l.r, l.y - l.r, 2 * l.r, 2 * l.r);
      hatch(g, box, -0.9, gap, R, { w: lw, alpha: storm ? 0.7 : 0.55 });
      g.restore();
      g.save(); zone(-0.6, -0.55, 1.08);
      g.fillStyle = css(mix(col, [0.05, 0.03, 0.1], 0.5), storm ? 0.45 : 0.3); g.fillRect(l.x - l.r, l.y - l.r, 2 * l.r, 2 * l.r);
      hatch(g, box, 0.75, gap * 1.05, R, { w: lw * 0.9, alpha: storm ? 0.7 : 0.55 });
      g.restore();
      g.save(); zone(-0.32, -0.3, 1.02);
      hatch(g, box, 0.05, gap * 0.8, R, { w: lw * 0.85, alpha: 0.6 });
      g.restore();
      g.restore();
      ink(g, disc, { w: W / 150, R, closed: true, amp: W / 400 });
    }
    // the flat base: a band of shadow along it
    const sh = g.createLinearGradient(0, base - W * 0.05, 0, base); sh.addColorStop(0, css(body, 0)); sh.addColorStop(1, css(mix(body, [0.05, 0.03, 0.1], 0.6), storm ? 0.7 : 0.5));
    g.globalCompositeOperation = 'source-atop'; g.fillStyle = sh; g.fillRect(0, base - W * 0.05, W, W * 0.05); g.globalCompositeOperation = 'source-over';
    g.restore();
    if (storm) {
      // a forked bolt out of the base: yellow, inked round
      let x = cx + (R() - 0.5) * W * 0.2, y = base - 2; const bolt = [[x, y]];
      for (let k = 0; k < 4; k++) { x += (k % 2 ? 1 : -1) * W * (0.05 + R() * 0.03); y += (H - base) * 0.22; bolt.push([x, y]); }
      const back = bolt.slice().reverse().map(([bx, by], i) => [bx + W * 0.04 * (0.15 + 0.85 * i / 4), by - (i === 0 ? 0 : 3)]);   // widest where it leaves the cloud
      const zig = [...bolt, ...back];
      g.save(); pathOf(g, zig); g.fillStyle = '#f5cf4e'; g.fill(); g.restore();
      ink(g, zig, { w: W / 170, R, closed: true, amp: 0.4 });
      // a short fork off the second bend
      const [fx, fy] = bolt[2], fork = [[fx, fy], [fx + W * 0.06, fy + (H - base) * 0.12], [fx + W * 0.05, fy + (H - base) * 0.2]];
      ink(g, fork, { w: W / 140, R, col: '#c99a1e', amp: 0.3 });
    }
    return { canvas: c, foot: storm ? 0.97 : Math.min(1, (base + W / 85) / H) };
  }

  // A sun as a chart maker draws it: a round disc in warm wash with a darker rim, ringed by rays that alternate a straight
  // spike and a wavy flame, each washed and inked.
  function sun({ px = 256, seed = 1 } = {}) {
    const R = rng(seed), W = px, H = px, c = canvas(W, H), g = c.getContext('2d'), cx = W / 2, cy = H / 2, r = W * 0.2, n = 16;
    for (let i = 0; i < n; i++) {
      const a = i / n * TAU + R() * 0.05, half = TAU / n * 0.32, flame = i % 2 === 1, len = r * (flame ? 1.85 : 2.25) * (0.92 + R() * 0.12);
      let pts;
      if (!flame) pts = [[cx + Math.cos(a - half) * r * 0.95, cy + Math.sin(a - half) * r * 0.95], [cx + Math.cos(a) * len, cy + Math.sin(a) * len], [cx + Math.cos(a + half) * r * 0.95, cy + Math.sin(a + half) * r * 0.95]];
      else {
        pts = [];
        for (let k = 0; k <= 10; k++) { const t = k / 10, w = half * (1 - t) * 1.1, s = Math.sin(t * Math.PI * 2.2) * 0.18 * (1 - t * 0.5); pts.push([cx + Math.cos(a - w + s) * (r + (len - r) * t), cy + Math.sin(a - w + s) * (r + (len - r) * t)]); }
        for (let k = 10; k >= 0; k--) { const t = k / 10, w = half * (1 - t) * 1.1, s = Math.sin(t * Math.PI * 2.2) * 0.18 * (1 - t * 0.5); pts.push([cx + Math.cos(a + w + s) * (r + (len - r) * t), cy + Math.sin(a + w + s) * (r + (len - r) * t)]); }
      }
      wash(g, pts, flame ? [0.95, 0.55, 0.2] : [0.98, 0.78, 0.3], R, { blooms: 2, alpha: 0.95, edge: 0.3 });
      if (!flame) { g.save(); pathOf(g, [[cx, cy], pts[1], pts[2]]); g.clip(); hatch(g, [cx - len, cy - len, cx + len, cy + len], a + 0.6, W * 0.014, R, { w: W / 420, col: '#8a3a10', alpha: 0.6 }); g.restore(); }
      ink(g, pts, { w: W / 220, R, closed: true, amp: 0.5 });
    }
    const disc = Array.from({ length: 100 }, (_, i) => { const a = i / 100 * TAU; return [cx + Math.cos(a) * r, cy + Math.sin(a) * r]; });
    wash(g, disc, [0.99, 0.84, 0.36], R, { blooms: 8, alpha: 1 });
    g.save(); pathOf(g, disc); g.clip();
    const rim = g.createRadialGradient(cx - r * 0.2, cy - r * 0.2, r * 0.2, cx, cy, r * 1.05);
    rim.addColorStop(0, 'rgba(255, 250, 220, 0.6)'); rim.addColorStop(0.7, 'rgba(255, 200, 80, 0)'); rim.addColorStop(1, 'rgba(200, 90, 20, 0.55)');
    g.fillStyle = rim; g.fillRect(0, 0, W, H);
    // the disc's far side (lower right) hatched and cross-hatched in burnt ink
    g.save(); g.beginPath(); g.rect(0, 0, W, H); g.arc(cx - r * 0.3, cy - r * 0.3, r * 1.08, 0, TAU, true); g.clip('evenodd');
    hatch(g, [cx - r, cy - r, cx + r, cy + r], -0.8, W * 0.022, R, { w: W / 380, col: '#8a3a10', alpha: 0.7 });
    hatch(g, [cx - r, cy - r, cx + r, cy + r], 0.8, W * 0.026, R, { w: W / 400, col: '#8a3a10', alpha: 0.55 });
    g.restore();
    // a few curling strokes of the surface's churn
    for (let k = 0; k < 4; k++) { const a = R() * TAU, rr = r * (0.3 + R() * 0.45), pts = []; for (let j = 0; j <= 10; j++) { const b = a + j * 0.12; pts.push([cx + Math.cos(b) * rr, cy + Math.sin(b) * rr]); } ink(g, pts, { w: W / 360, R, col: '#8a3a10', alpha: 0.6 }); }
    g.restore();
    ink(g, disc, { w: W / 120, R, closed: true, amp: W / 320 });
    return { canvas: c, foot: 0.5 + 0.2 * 2.3 };
  }

  // An asteroid: a lumpy outline, a grey-brown wash, a crater or two and the shadowed side hatched.
  function rock({ px = 96, seed = 1 } = {}) {
    const R = rng(seed), W = px, H = px, c = canvas(W, H), g = c.getContext('2d'), cx = W / 2, cy = H / 2, r = W * 0.36, n = 9;
    const rad = Array.from({ length: n }, () => 0.72 + R() * 0.3), out = [];
    for (let i = 0; i < 72; i++) { const t = i / 72 * n, k = Math.floor(t), f = t - k, e = f * f * (3 - 2 * f), a = i / 72 * TAU, rr = r * (rad[k % n] + (rad[(k + 1) % n] - rad[k % n]) * e); out.push([cx + Math.cos(a) * rr, cy + Math.sin(a) * rr * 0.82]); }
    const tone = mix([0.55, 0.47, 0.38], [0.45, 0.45, 0.48], R());
    wash(g, out, tone, R, { blooms: 4, alpha: 1 });
    g.save(); pathOf(g, out); g.clip();
    g.beginPath(); g.rect(0, 0, W, H); g.arc(cx - r * 0.35, cy - r * 0.35, r * 1.15, 0, TAU, true); g.clip('evenodd');
    g.fillStyle = 'rgba(30, 20, 15, 0.45)'; g.fillRect(0, 0, W, H); hatch(g, [0, 0, W, H], -0.8, W * 0.085, R, { w: W / 60, alpha: 0.7 }); hatch(g, [0, 0, W, H], 0.8, W * 0.095, R, { w: W / 70, alpha: 0.6 });
    g.restore();
    for (let k = 0; k < 1 + Math.floor(R() * 2); k++) { const x = cx + (R() - 0.6) * r, y = cy + (R() - 0.6) * r * 0.6, cr = r * (0.12 + R() * 0.12), pts = []; for (let j = 0; j <= 16; j++) { const a = Math.PI * 0.9 + j / 16 * Math.PI * 1.2; pts.push([x + Math.cos(a) * cr, y + Math.sin(a) * cr * 0.7]); } ink(g, pts, { w: W / 110, R, amp: 0.3 }); }
    ink(g, out, { w: W / 45, R, closed: true, amp: 0.6 });
    return { canvas: c, foot: (cy + r * 0.82 + 2) / H };
  }

  // A compass rose drawn into the chart: two rings, sixteen points (the cardinal points long, the half points shorter,
  // the quarter points short), each point half washed and half left bare as on an old chart, and a fleur at north.
  function compass(g, x, y, r, seed = 1, { ink: col = INK, wash: wcol = [0.75, 0.2, 0.15] } = {}) {
    const R = rng(seed);
    const ring = (rr) => Array.from({ length: 90 }, (_, i) => { const a = i / 90 * TAU; return [x + Math.cos(a) * rr, y + Math.sin(a) * rr]; });
    ink(g, ring(r * 0.98), { w: r / 60, R, closed: true, col, amp: 0.6 }); ink(g, ring(r * 0.9), { w: r / 120, R, closed: true, col, amp: 0.5 });
    for (let k = 0; k < 64; k++) { const a = k / 64 * TAU; ink(g, [[x + Math.cos(a) * r * 0.9, y + Math.sin(a) * r * 0.9], [x + Math.cos(a) * r * (k % 4 ? 0.94 : 0.98), y + Math.sin(a) * r * (k % 4 ? 0.94 : 0.98)]], { w: r / 160, R, col, amp: 0.2 }); }
    for (const [lenK, wK, step, off] of [[0.55, 0.06, 16, TAU / 32], [0.68, 0.08, 8, TAU / 16], [0.86, 0.11, 4, 0]]) {
      for (let i = 0; i < step; i++) {
        const a = i / step * TAU + off - Math.PI / 2, tip = [x + Math.cos(a) * r * lenK, y + Math.sin(a) * r * lenK];
        const l = [x + Math.cos(a - Math.PI / 2) * r * wK, y + Math.sin(a - Math.PI / 2) * r * wK], rt = [x + Math.cos(a + Math.PI / 2) * r * wK, y + Math.sin(a + Math.PI / 2) * r * wK];
        g.save(); pathOf(g, [[x, y], l, tip]); g.fillStyle = css(step === 4 ? wcol : mix(wcol, [0.1, 0.08, 0.06], 0.6), 0.85); g.fill(); g.restore();
        g.save(); pathOf(g, [[x, y], rt, tip]); g.fillStyle = css(PAPER, 0.85); g.fill(); g.restore();
        ink(g, [l, tip, rt], { w: r / 110, R, col, amp: 0.3 }); ink(g, [[x, y], tip], { w: r / 220, R, col, amp: 0.2 });
      }
    }
    // the fleur at north
    const ny = y - r * 1.08, fl = [[x, ny - r * 0.16], [x + r * 0.07, ny - r * 0.02], [x + r * 0.03, ny + r * 0.02], [x, ny - r * 0.02], [x - r * 0.03, ny + r * 0.02], [x - r * 0.07, ny - r * 0.02]];
    g.save(); pathOf(g, fl); g.fillStyle = css(wcol, 0.9); g.fill(); g.restore(); ink(g, fl, { w: r / 110, R, closed: true, col, amp: 0.3 });
    ink(g, [[x - r * 0.06, ny + r * 0.05], [x + r * 0.06, ny + r * 0.05]], { w: r / 90, R, col, amp: 0.2 });
  }

  window.FCDrawn = { cloud, sun, rock, compass, ink, wash, hatch, rng, INK };
})();
