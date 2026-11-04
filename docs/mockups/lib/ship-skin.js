// Fleet Command mockups: a ship's skin, shared by every faction's ship page (CLAUDE.md 6.1: one implementation). Lifted from
// the Concord line (docs/mockups/concord-hull-concepts.html, mockup 6, where each piece was asked for by the owner) so the
// Krai frigate (docs/mockups/krai-ship-concepts.html) is built to the same standard with the same code. Edit it here, then
// re-run tools/mockups/inline_data.py on each page that inlines it.
//
// FCShipSkin(THREE) returns:
//   detail(o)            a detail tile drawn once from a seeded generator: a height field turned into a normal map, and a
//                        grime and wear map. o.style 'strake' (the Concord's: plate seams in a grid, inset and raised
//                        sub-panels, rivet rows, hatches, vents) or 'kit' (hard-surface plates:
//                        rectangles split and split again, 45 degree chamfered corners, recessed conduit channels with a
//                        pipe, vent slot banks, round hatches, bolt-hole rows, with the seams and wear); o.along, o.round: the tile in
//                        metres; o.seed. Returns { albedo, normal, canvases }.
//   GLSL.hash            ph1(vec2): a hash in [0, 1).
//   GLSL.detail(u, v)    detW, detailA, detailN: the tile projected from three sides in ship space (tri-planar), no UVs;
//                        needs uniforms uDetA and uDetN.
//   GLSL.box             bHit: a ray against an axis-aligned box, for rooms drawn by interior mapping behind a pane.
//   thruster(at, r, o)   a flared bell lathed from a profile, ribbed and ringed outside, lit inside by its own heat, a lip
//                        ring, a closed mounting fairing with bolts, a glowing throat and a faint plume. Lip at the stern
//                        (at.z), throat forward of it. o: { env, grey, dark, glow(hex, base), sides (round 28, or 6 for a
//                        faceted bell), hot: [throat rgb, lip rgb], plume: rgb }. Returns { parts: [[geometry, material,
//                        role]], heat (a uniform: 0 off, 1 full), throat (its material) }.
window.FCShipSkin = function FCShipSkin(THREE) {
'use strict';
const TAU = Math.PI * 2, PI = Math.PI;
function mulberry(seed) { let a = seed >>> 0; return () => { a = (a + 0x6D2B79F5) >>> 0; let t = a; t = Math.imul(t ^ (t >>> 15), t | 1); t ^= t + Math.imul(t ^ (t >>> 7), t | 61); return ((t ^ (t >>> 14)) >>> 0) / 4294967296; }; }
function cv(w, h) { const c = document.createElement('canvas'); c.width = w; c.height = h; return c; }

function detail(opts = {}) {
  const o = { style: 'strake', along: 8, round: 7.8, seed: 4242, ...opts };
  if (o.style === 'kit') return kitDetail(o);
  const W = 1024, H = 1024, ppmU = W / o.along, ppmV = H / o.round, rng = mulberry(o.seed);
  const hc = cv(W, H), hx = hc.getContext('2d'), ac = cv(W, H), ax = ac.getContext('2d');
  hx.fillStyle = 'rgb(128,128,128)'; hx.fillRect(0, 0, W, H); ax.fillStyle = 'rgb(236,236,236)'; ax.fillRect(0, 0, W, H);
  const g = (v) => `rgb(${v},${v},${v})`;
  // Plates: two along (4 m), three round (2.6 m), each its own shade and its own inset detail.
  for (let pu = 0; pu < 2; pu++) for (let pv = 0; pv < 3; pv++) {
    const x0 = pu * W / 2, y0 = pv * H / 3, w = W / 2, h = H / 3;
    ax.fillStyle = g(Math.round(222 + rng() * 26)); ax.fillRect(x0, y0, w, h);
    const k = Math.floor(rng() * 4);
    for (let i = 0; i < 2 + k; i++) {
      const pw = (0.6 + rng() * 1.6) * ppmU, ph = (0.4 + rng() * 1.0) * ppmV, px = x0 + 12 + rng() * (w - pw - 24), py = y0 + 12 + rng() * (h - ph - 24);
      const raised = rng() < 0.5, v = raised ? 150 : 108;
      hx.fillStyle = g(v); hx.fillRect(px, py, pw, ph);
      hx.strokeStyle = g(raised ? 96 : 150); hx.lineWidth = 2; hx.strokeRect(px, py, pw, ph);
      if (rng() < 0.3) { // a vent grille
        hx.fillStyle = g(70); for (let s = 4; s < ph - 4; s += 7) hx.fillRect(px + 4, py + s, pw - 8, 3);
        ax.fillStyle = 'rgba(40,40,44,0.35)'; ax.fillRect(px, py, pw, ph);
      }
      if (rng() < 0.25) { hx.strokeStyle = g(60); hx.lineWidth = 3; hx.strokeRect(px + 3, py + 3, pw - 6, ph - 6); } // hatch groove
    }
    // streaks of grime running back from the plate's leading seam
    for (let i = 0; i < 6; i++) { const sx = x0 + rng() * w, sy = y0 + rng() * h, len = (0.5 + rng() * 2.5) * ppmU, gr = ax.createLinearGradient(sx, sy, sx - len, sy);
      gr.addColorStop(0, 'rgba(60,54,48,0.22)'); gr.addColorStop(1, 'rgba(60,54,48,0)'); ax.fillStyle = gr; ax.fillRect(sx - len, sy, len, 2 + rng() * 6); }
  }
  // Seams: grooves with a bevel, in the paint's grid.
  const seam = (x, y, w, h) => { hx.fillStyle = g(30); hx.fillRect(x, y, w, h); };
  for (const u of [0, W / 2]) { seam(u - 3, 0, 6, H); seam(u + W - 3, 0, 6, H); }
  for (const v of [0, H / 3, 2 * H / 3]) { seam(0, v - 3, W, 6); seam(0, v + H - 3, W, 6); }
  // Rivet rows either side of every seam, every 0.25 m.
  hx.fillStyle = g(205);
  for (const u of [0, W / 2]) for (let y = 6; y < H; y += 0.25 * ppmV) for (const o of [-9, 9]) { hx.beginPath(); hx.arc((u + o + W) % W, y, 2.4, 0, TAU); hx.fill(); }
  for (const v of [0, H / 3, 2 * H / 3]) for (let x = 6; x < W; x += 0.25 * ppmU) for (const o of [-9, 9]) { hx.beginPath(); hx.arc(x, (v + o + H) % H, 2.4, 0, TAU); hx.fill(); }
  // Wear: speckle and scratches.
  for (let i = 0; i < 2600; i++) { ax.fillStyle = `rgba(${rng() < 0.5 ? '40,38,36' : '255,255,255'},${0.05 + rng() * 0.1})`; ax.fillRect(rng() * W, rng() * H, 1 + rng() * 3, 1 + rng() * 3); }
  ax.strokeStyle = 'rgba(255,255,255,0.18)'; ax.lineWidth = 1;
  for (let i = 0; i < 60; i++) { const x = rng() * W, y = rng() * H; ax.beginPath(); ax.moveTo(x, y); ax.lineTo(x + (rng() - 0.5) * 80, y + (rng() - 0.5) * 20); ax.stroke(); }
  // Height to normals (Sobel), wrapping at the tile edges.
  const hd = hx.getImageData(0, 0, W, H).data, nc = cv(W, H), nx = nc.getContext('2d'), nd = nx.createImageData(W, H), S = 2.2;
  const at = (x, y) => hd[(((y + H) % H) * W + ((x + W) % W)) * 4] / 255;
  for (let y = 0; y < H; y++) for (let x = 0; x < W; x++) {
    const dx = (at(x + 1, y - 1) + 2 * at(x + 1, y) + at(x + 1, y + 1)) - (at(x - 1, y - 1) + 2 * at(x - 1, y) + at(x - 1, y + 1));
    const dy = (at(x - 1, y + 1) + 2 * at(x, y + 1) + at(x + 1, y + 1)) - (at(x - 1, y - 1) + 2 * at(x, y - 1) + at(x + 1, y - 1));
    let a = -dx * S, b = dy * S, c = 1; const l = Math.hypot(a, b, c); a /= l; b /= l; c /= l;
    const i = (y * W + x) * 4; nd.data[i] = (a * 0.5 + 0.5) * 255; nd.data[i + 1] = (b * 0.5 + 0.5) * 255; nd.data[i + 2] = (c * 0.5 + 0.5) * 255; nd.data[i + 3] = 255;
  }
  nx.putImageData(nd, 0, 0);
  const tex = (c, srgb) => { const t = new THREE.CanvasTexture(c); t.wrapS = t.wrapT = THREE.RepeatWrapping; t.anisotropy = 8; if (srgb) t.encoding = THREE.sRGBEncoding; return t; };
  return { albedo: tex(ac, false), normal: tex(nc, false), canvases: { height: hc, albedo: ac, normal: nc } };
}

// The 'kit' tile: armour laid as hard-surface plates, every line square to the hull (no diagonals but the 45 degree corner
// chamfers). The tile is split into plates and the plates split again (a seeded binary split), each plate drawn with a
// seam groove round it and one or two corners cut at 45 degrees, raised or set in; then each plate gets at most one
// feature: a bank of vent slots, a round hatch with its ring, a row of bolt holes, a smaller inset panel with cut corners,
// or a recessed conduit channel with a pipe lying in it. Seams wrap at the tile's edges so the tile repeats with no line.
function kitDetail(o) {
  const W = 1024, H = 1024, ppmU = W / o.along, ppmV = H / o.round, rng = mulberry(o.seed);
  const hc = cv(W, H), hx = hc.getContext('2d'), ac = cv(W, H), ax = ac.getContext('2d');
  hx.fillStyle = 'rgb(128,128,128)'; hx.fillRect(0, 0, W, H); ax.fillStyle = 'rgb(226,226,226)'; ax.fillRect(0, 0, W, H);
  const g = (v) => `rgb(${v},${v},${v})`;
  // A rectangle with chosen corners cut at 45 degrees (cut: [top-left, top-right, bottom-right, bottom-left] in pixels).
  const chamfer = (ctx, x, y, w, h, cut) => { const [a, b, c, d] = cut; ctx.beginPath();
    ctx.moveTo(x + a, y); ctx.lineTo(x + w - b, y); ctx.lineTo(x + w, y + b); ctx.lineTo(x + w, y + h - c); ctx.lineTo(x + w - c, y + h);
    ctx.lineTo(x + d, y + h); ctx.lineTo(x, y + h - d); ctx.lineTo(x, y + a); ctx.closePath(); };
  // Split the tile into plates: cut the longer side at a third to two thirds, until plates are 0.8 to 2.6 m.
  const plates = [], split = (x, y, w, h, depth) => {
    const wm = w / ppmU, hm = h / ppmV, small = wm < 1.6 && hm < 1.6;
    if (depth > 5 || small || (wm < 2.6 && hm < 2.6 && rng() < 0.35)) { plates.push([x, y, w, h]); return; }
    const t = 0.33 + rng() * 0.34;
    if (wm > hm) { const a = Math.round(w * t / 8) * 8; split(x, y, a, h, depth + 1); split(x + a, y, w - a, h, depth + 1); }
    else { const a = Math.round(h * t / 8) * 8; split(x, y, w, a, depth + 1); split(x, y + a, w, h - a, depth + 1); } };
  split(0, 0, W, H, 0);
  const S = 5; // half a seam, in pixels (about 4 cm)
  for (const [x0, y0, w0, h0] of plates) {
    const x = x0 + S, y = y0 + S, w = w0 - 2 * S, h = h0 - 2 * S, ch = Math.min(w, h) * (0.12 + rng() * 0.12);
    const cut = [0, 0, 0, 0]; cut[Math.floor(rng() * 4)] = ch; if (rng() < 0.4) cut[Math.floor(rng() * 4)] = ch * 0.6;
    const raised = rng() < 0.5, v = raised ? 150 : 112;
    hx.fillStyle = g(v); chamfer(hx, x, y, w, h, cut); hx.fill();
    hx.strokeStyle = g(raised ? 176 : 92); hx.lineWidth = 3; chamfer(hx, x + 2, y + 2, w - 4, h - 4, cut.map((c) => c * 0.9)); hx.stroke(); // a lit bevel
    ax.fillStyle = g(Math.round(208 + rng() * 40)); chamfer(ax, x, y, w, h, cut); ax.fill();
    for (const [bx, by] of [[x + 10, y + 10], [x + w - 10, y + 10], [x + w - 10, y + h - 10], [x + 10, y + h - 10]]) { hx.fillStyle = g(60); hx.beginPath(); hx.arc(bx, by, 3, 0, TAU); hx.fill(); }
    const f = rng(), cx = x + w / 2, cy = y + h / 2;
    if (f < 0.2 && w > 60 && h > 60) {            // a bank of vent slots, upright
      const n = 3 + Math.floor(rng() * 4), sw = 7, sh = Math.min(h * 0.5, 0.9 * ppmV), sx0 = x + w * (0.15 + rng() * 0.4);
      hx.fillStyle = g(44); for (let i = 0; i < n; i++) { hx.beginPath(); hx.roundRect ? hx.roundRect(sx0 + i * 16, cy - sh / 2, sw, sh, 3) : hx.rect(sx0 + i * 16, cy - sh / 2, sw, sh); hx.fill(); }
      ax.fillStyle = 'rgba(30,28,26,0.35)'; ax.fillRect(sx0 - 4, cy - sh / 2 - 4, n * 16 + 2, sh + 8);
    } else if (f < 0.35 && w > 90 && h > 90) {     // a round hatch: a ring groove, a raised lid, a centre boss
      const r = Math.min(w, h) * 0.28; hx.fillStyle = g(70); hx.beginPath(); hx.arc(cx, cy, r, 0, TAU); hx.fill();
      hx.fillStyle = g(raised ? 160 : 132); hx.beginPath(); hx.arc(cx, cy, r - 6, 0, TAU); hx.fill();
      hx.fillStyle = g(90); hx.beginPath(); hx.arc(cx, cy, r * 0.25, 0, TAU); hx.fill();
    } else if (f < 0.5) {                         // a row of bolt holes along one edge
      hx.fillStyle = g(48); const along = w > h; for (let t = 18; t < (along ? w : h) - 18; t += 14) { hx.beginPath(); hx.arc(along ? x + t : x + 16, along ? y + 16 : y + t, 3.5, 0, TAU); hx.fill(); }
    } else if (f < 0.68 && w > 70 && h > 50) {     // a smaller inset panel with a cut corner
      const iw = w * (0.35 + rng() * 0.3), ih = h * (0.3 + rng() * 0.3), ix = x + 14 + rng() * (w - iw - 28), iy = y + 14 + rng() * (h - ih - 28);
      const ic = [0, 0, 0, 0]; ic[Math.floor(rng() * 4)] = Math.min(iw, ih) * 0.3;
      hx.fillStyle = g(raised ? 136 : 96); chamfer(hx, ix, iy, iw, ih, ic); hx.fill(); hx.strokeStyle = g(66); hx.lineWidth = 2; chamfer(hx, ix, iy, iw, ih, ic); hx.stroke();
    } else if (f < 0.82 && (w > 120 || h > 120)) { // a recessed conduit channel with a pipe lying in it
      const along = w > h, cw = 22, len = (along ? w : h) - 30, px = along ? x + 15 : cx - cw / 2, py = along ? cy - cw / 2 : y + 15;
      hx.fillStyle = g(52); hx.fillRect(px, py, along ? len : cw, along ? cw : len);
      const gr = along ? hx.createLinearGradient(0, py + 3, 0, py + cw - 3) : hx.createLinearGradient(px + 3, 0, px + cw - 3, 0);
      gr.addColorStop(0, g(80)); gr.addColorStop(0.5, g(196)); gr.addColorStop(1, g(80)); hx.fillStyle = gr;
      hx.fillRect(along ? px + 4 : px + 4, along ? py + 4 : py + 4, along ? len - 8 : cw - 8, along ? cw - 8 : len - 8);
      for (let t = 30; t < len - 20; t += 70) { hx.fillStyle = g(150); hx.fillRect(along ? px + t : px - 2, along ? py - 2 : py + t, along ? 6 : cw + 4, along ? cw + 4 : 6); } // clamps
      ax.fillStyle = 'rgba(40,36,32,0.3)'; ax.fillRect(px, py, along ? len : cw, along ? cw : len);
    }
  }
  // Seams: the grooves between plates (the gaps the plates were inset from), already left at the base height; deepen them.
  { const d = hx.getImageData(0, 0, W, H), a = d.data; for (let i = 0; i < a.length; i += 4) if (a[i] === 128) { a[i] = a[i + 1] = a[i + 2] = 40; }
    hx.putImageData(d, 0, 0); }
  { const d = ax.getImageData(0, 0, W, H), a = d.data, hd = hx.getImageData(0, 0, W, H).data; for (let i = 0; i < a.length; i += 4) if (hd[i] === 40) { a[i] = a[i + 1] = a[i + 2] = 120; } ax.putImageData(d, 0, 0); }
  // Wear: speckle, scratches and grime trailing aft (toward -u).
  for (let i = 0; i < 2400; i++) { ax.fillStyle = `rgba(${rng() < 0.5 ? '40,36,34' : '255,250,240'},${0.05 + rng() * 0.09})`; ax.fillRect(rng() * W, rng() * H, 1 + rng() * 3, 1 + rng() * 3); }
  ax.strokeStyle = 'rgba(255,255,255,0.14)'; ax.lineWidth = 1;
  for (let i = 0; i < 60; i++) { const x = rng() * W, y = rng() * H; ax.beginPath(); ax.moveTo(x, y); ax.lineTo(x + (rng() - 0.5) * 90, y + (rng() - 0.5) * 6); ax.stroke(); }
  return toMaps(hc, ac, W, H);
}
// Height to normals (Sobel), wrapping at the tile edges; both maps as repeating textures.
function toMaps(hc, ac, W, H) {
  const hx = hc.getContext('2d'), hd = hx.getImageData(0, 0, W, H).data, nc = cv(W, H), nx = nc.getContext('2d'), nd = nx.createImageData(W, H), S = 2.2;
  const at = (x, y) => hd[(((y + H) % H) * W + ((x + W) % W)) * 4] / 255;
  for (let y = 0; y < H; y++) for (let x = 0; x < W; x++) {
    const dx = (at(x + 1, y - 1) + 2 * at(x + 1, y) + at(x + 1, y + 1)) - (at(x - 1, y - 1) + 2 * at(x - 1, y) + at(x - 1, y + 1));
    const dy = (at(x - 1, y + 1) + 2 * at(x, y + 1) + at(x + 1, y + 1)) - (at(x - 1, y - 1) + 2 * at(x, y - 1) + at(x + 1, y - 1));
    let a = -dx * S, b = dy * S, c = 1; const l = Math.hypot(a, b, c); a /= l; b /= l; c /= l;
    const i = (y * W + x) * 4; nd.data[i] = (a * 0.5 + 0.5) * 255; nd.data[i + 1] = (b * 0.5 + 0.5) * 255; nd.data[i + 2] = (c * 0.5 + 0.5) * 255; nd.data[i + 3] = 255;
  }
  nx.putImageData(nd, 0, 0);
  const tex = (c) => { const t = new THREE.CanvasTexture(c); t.wrapS = t.wrapT = THREE.RepeatWrapping; t.anisotropy = 8; return t; };
  return { albedo: tex(ac), normal: tex(nc), canvases: { height: hc, albedo: ac, normal: nc } };
}

const f1 = (v) => (Number.isInteger(v) ? v.toFixed(1) : String(v));
const GLSL = {
  hash: 'float ph1(vec2 p){ return fract(sin(dot(p, vec2(127.1, 311.7)))*43758.5453); }\n',
  detail: (TU, TV) => { TU = f1(TU); TV = f1(TV); return 'vec3 detW(vec3 n){ vec3 w = pow(abs(n), vec3(6.0)); return w/(w.x + w.y + w.z); }' + '\n' +
    'float detailA(vec3 p, vec3 n){ vec3 w = detW(n);' + '\n' +
    '  return texture2D(uDetA, vec2(p.z/' + TU + ', p.y/' + TV + ')).r*w.x + texture2D(uDetA, vec2(p.z/' + TU + ', p.x/' + TV + ')).r*w.y + texture2D(uDetA, vec2(p.x/' + TU + ', p.y/' + TV + ')).r*w.z; }' + '\n' +
    '// Tri-planar normal map, whiteout blend in ship space.' + '\n' +
    'vec3 detailN(vec3 p, vec3 n, float k){ vec3 w = detW(n);' + '\n' +
    '  vec2 tx = (texture2D(uDetN, vec2(p.z/' + TU + ', p.y/' + TV + ')).xy*2.0 - 1.0)*k, ty = (texture2D(uDetN, vec2(p.z/' + TU + ', p.x/' + TV + ')).xy*2.0 - 1.0)*k, tz = (texture2D(uDetN, vec2(p.x/' + TU + ', p.y/' + TV + ')).xy*2.0 - 1.0)*k;' + '\n' +
    '  vec3 nx = vec3(n.x, n.y + tx.y, n.z + tx.x*sign(n.x)), ny = vec3(n.x + ty.y*sign(n.y), n.y, n.z + ty.x), nz = vec3(n.x + tz.x*sign(n.z), n.y + tz.y, n.z);' + '\n' +
    '  return normalize(nx*w.x + ny*w.y + nz*w.z); }' + '\n'; },
  box: `// Ray against an axis-aligned box: distance to the near face (1e9 if missed), and that face's normal.
float bHit(vec3 ro, vec3 rd, vec3 b0, vec3 b1, inout vec3 nOut, float tBest){
  vec3 t0 = (b0 - ro)/rd, t1 = (b1 - ro)/rd, tn3 = min(t0, t1), tf3 = max(t0, t1);
  float tn = max(max(tn3.x, tn3.y), tn3.z), tf = min(min(tf3.x, tf3.y), tf3.z);
  if (tn > tf || tn < 0.0 || tn >= tBest) return tBest;
  nOut = tn == tn3.x ? vec3(-sign(rd.x), 0.0, 0.0) : tn == tn3.y ? vec3(0.0, -sign(rd.y), 0.0) : vec3(0.0, 0.0, -sign(rd.z));
  return tn; }\n`,
};

// A thruster (owner, 2026-11-03, on the Concord line: "fix thrusters, make them more detailed, properly texture them";
// "the thruster mounting ring needs to be fixed"): see the header. The materials are made here; the page adds the parts
// to its own pieces.
function thrusterMat(heat, o) {
  const m = new THREE.MeshStandardMaterial({ color: o.shell ?? 0x4a4e57, metalness: 0.8, roughness: 0.34, side: THREE.DoubleSide, envMapIntensity: o.env });
  const [a, b] = o.hot || [[0.85, 0.12, 0.03], [1.0, 0.78, 0.45]], v3 = (c) => `vec3(${c.map(f1).join(', ')})`;
  m.onBeforeCompile = (sh) => {
    sh.uniforms.uHeat = heat;
    sh.vertexShader = sh.vertexShader.replace('#include <common>', '#include <common>\nvarying vec2 vTU;').replace('#include <begin_vertex>', '#include <begin_vertex>\nvTU = uv;');
    sh.fragmentShader = sh.fragmentShader.replace('#include <common>', '#include <common>\nvarying vec2 vTU; uniform float uHeat;')
      .replace('#include <map_fragment>', `#include <map_fragment>
        float rib = step(0.9, fract(vTU.x*${f1(o.ribs ?? 32)})), ring = step(0.86, fract(vTU.y*7.0)), groove = max(rib, ring);
        diffuseColor.rgb *= gl_FrontFacing ? 1.0 - 0.5*groove : 0.25;`)
      .replace('#include <emissivemap_fragment>', `#include <emissivemap_fragment>
        if (!gl_FrontFacing) { float d = vTU.y; vec3 hot = mix(${v3(a)}, ${v3(b)}, smoothstep(0.25, 1.0, d));
          totalEmissiveRadiance = hot*(0.15 + 1.9*d*d)*uHeat*(1.0 - 0.55*groove); }`);
  };
  m.customProgramCacheKey = () => 'fleet-thruster-' + (o.key || 'concord');
  return m;
}
function plumeMat(heat, o) {
  const c = (o.plume || [1.0, 0.55, 0.22]).map(f1).join(', ');
  return new THREE.ShaderMaterial({ transparent: true, depthWrite: false, blending: THREE.AdditiveBlending, uniforms: { uHeat: heat },
    vertexShader: 'varying vec2 vU; void main(){ vU = uv; gl_Position = projectionMatrix*modelViewMatrix*vec4(position,1.); }',
    fragmentShader: `uniform float uHeat; varying vec2 vU; void main(){ float a = vU.y*vU.y*0.35*uHeat; gl_FragColor = vec4(vec3(${c})*a, a); }` });
}
function thruster(at, r, o) {
  const N = o.sides ?? 28, L = r * 1.5, heat = { value: 1 }, parts = [], prof = [[r, 0], [r * 0.97, L * 0.12], [r * 0.86, L * 0.38], [r * 0.7, L * 0.68], [r * 0.58, L * 0.9], [r * 0.55, L]].map(([x, y]) => new THREE.Vector2(x, y));
  const lip = at.z - 0.15 - L, add = (g, m, role) => parts.push([g, m, role]);
  const bell = new THREE.LatheGeometry(prof, N); bell.rotateX(PI / 2); bell.translate(at.x, at.y, lip); add(bell, thrusterMat(heat, o), 'bell');
  const lipRing = new THREE.TorusGeometry(r * 1.0, r * 0.07, 6, N); lipRing.translate(at.x, at.y, lip); add(lipRing, o.grey, 'lip');
  // The mounting ring: a closed, round fairing that seats on the hull and narrows onto the bell's neck, so the bell grows
  // out of it; a bolt flange where it meets the plating.
  const neck = (y) => { for (let k = 1; k < prof.length; k++) if (prof[k].y >= y) { const a = prof[k - 1], b = prof[k], t = (y - a.y) / (b.y - a.y); return a.x + (b.x - a.x) * t; } return prof[prof.length - 1].x; };
  const yE = L * 0.62, mount = [[neck(L) * 0.9, L], [r * 0.98, L], [r * 0.98, L * 0.93], [neck(yE) + r * 0.1, yE + L * 0.04], [neck(yE) - 0.02, yE]].map(([x, y]) => new THREE.Vector2(x, y));
  const mg = new THREE.LatheGeometry(mount, N === 28 ? 32 : N); mg.rotateX(PI / 2); mg.translate(at.x, at.y, lip); add(mg, new THREE.MeshStandardMaterial({ color: o.mount ?? 0x3a4049, roughness: 0.55, metalness: 0.6, side: THREE.DoubleSide, envMapIntensity: o.env }), 'mount');
  for (let k = 0; k < 10; k++) { const a = k / 10 * 2 * PI, bo = new THREE.BoxGeometry(r * 0.09, r * 0.09, r * 0.12); bo.translate(at.x + Math.cos(a) * r * 0.99, at.y + Math.sin(a) * r * 0.99, lip + L - r * 0.06); add(bo, o.dark, 'bolt'); }
  // The throat: a disc of the bell's own sides, set inside its walls and well inside the bell's mouth on the hull side, so it
  // meets no other surface (owner, 2026-11-04, "look at all that zfighting": 5 cm in, it lay exactly on a Krai stern face).
  const tm = o.glow(o.throat ?? 0xffc27a, 0x3a1a08), dT = L * 0.06, rT = neck(L - dT) * 0.99; // its corners on the bell's own, so it fills the bell and stays inside it
  const throat = new THREE.CircleGeometry(rT, N, PI / 2); throat.rotateY(PI); throat.translate(at.x, at.y, lip + L - dT); add(throat, tm, 'throat');
  const pl = new THREE.ConeGeometry(r * 0.85, r * 3.2, 20, 1, true); pl.rotateX(PI / 2); pl.translate(at.x, at.y, lip - r * 1.6); add(pl, plumeMat(heat, o), 'plume');
  tm.userData.heat = heat;
  return { parts, heat, throat: tm };
}

return { detail, GLSL, thruster, mulberry };
};
