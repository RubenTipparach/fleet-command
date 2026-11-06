// Fleet Command mockups: how a ship comes apart, shared by every faction's ship page (CLAUDE.md 6.1: one implementation).
// Lifted from the Concord line (docs/mockups/concord-hull-concepts.html, mockup 6, where each piece was asked for by the
// owner) so a Krai hull (docs/mockups/krai-ship-concepts.html) breaks apart the same way (owner, 2026-11-04: "Ship isn't
// breaking apart like concord. Please fix"). Edit it here, then re-run tools/mockups/inline_data.py on each page.
//
// A body is a lofted hull: { name, sys, intact (the piece it is while whole), stations (its loft), ox (x offset), geos
// ([[geometry, paint key]]), n (how many pieces) }; or, for a hull that is not a loft (the Ssarith Hunt's grown skins), the
// same with sdf in place of stations: { lo, hi, dims, scale, bytes (Uint8Array, x fastest: 128 + scale x metres inside),
// tex (the same as a THREE.Data3DTexture) }, written by tools/blender/hull_sdf.py (owner, 2026-11-05: "ship piece breaking
// should be built next"). FCShipBreak(THREE, o) needs:
//   o.scene, o.rand (the page's seeded generator), o.fx (the FCShipFX instance), o.CL ({ loftDist, chOf }), o.env() (the
//   light's reflection strength), o.hullMat(paintKey, ship) (a hull material whose userData.u holds the damage uniforms:
//   uCell, uSeeds, uSeedN, uOpen, uCuts, uCutC, uCutCount, uCutHeat, and the hull shader runs dmgBreak from ship-fx.js),
//   o.extras(ship) (the ids of the outer pieces a breach blows off after the hull's outer cells), o.special(ship, P)
//   (true when the page broke P its own way, a nacelle snapping), o.onChange(ship) (refresh the page's panel).
// A ship: { root, pieces, frags, bodies, meshes, emitters, navs, mats, corePos, sys, hull, broken, breach, t, len }.
// A piece: { id, pivot, inner, center, vel, spin, detached, mats, cuts, caps, sys }; a nav: { s (its sprite), kind, P }.
// Returns: NO_SEEDS, fracture, cutFace, addCap, dedupe, ellipse, addEmitter, wreckFire, swapToPieces, chunkOff, BREACH,
// breakup, detach, charStump, nearestFrag, centerOf, liveMesh, swapBody, blowOff, stepBreach, mainBlast, makeWires,
// stepWires, uploadWires, clearWires.
window.FCShipBreak = function FCShipBreak(THREE, o) {
'use strict';
const scene = o.scene, fxRng = () => o.rand(), { loftDist, chOf } = o.CL, { emit, flash, shockwave, debrisAdd, debrisBurst, sharedTime } = o.fx;
const PI = Math.PI, TAU = PI * 2, V3 = (x = 0, y = 0, z = 0) => new THREE.Vector3(x, y, z);
const rr = (rng, a, b) => a + (b - a) * rng(), lerp = (a, b, t) => a + (b - a) * t, len = (ship) => ship.len ?? ship.C.length;

const NO_SEEDS = { uSeeds: { value: Array.from({ length: 16 }, () => V3()) }, uSeedN: { value: 0 }, uOpen: { value: new Array(16).fill(0) } };
// Random fracture (owner, 2026-11-02: "the ship breaks very programmatically, how do we make these chunks look more structurally
// random?"). Seeds scattered through a body (the primary hull, the secondary hull) split it into cells: every piece is the body
// clipped to one cell, its skin torn a little at the edge by noise. Where two cells meet, a cut face closes the piece. A body is
// a lofted hull, so "inside" is the signed distance into its stations (loftDist), the same test the cut-face shader runs.
function sdfAt(f, p) { // the field's distance at p, trilinear between cell centres, as the shader's texture() reads it
  const g = [0, 1, 2].map((k) => ((p.getComponent(k) - f.lo[k]) / (f.hi[k] - f.lo[k])) * f.dims[k] - 0.5);
  if (g.some((v, k) => v < -0.5 || v > f.dims[k] - 0.5)) return -1;
  const i0 = g.map((v, k) => Math.max(0, Math.min(f.dims[k] - 1, Math.floor(v)))), t = g.map((v, k) => Math.max(0, Math.min(1, v - i0[k])));
  const at = (x, y, z) => f.bytes[Math.min(x, f.dims[0] - 1) + f.dims[0] * (Math.min(y, f.dims[1] - 1) + f.dims[1] * Math.min(z, f.dims[2] - 1))];
  let d = 0; for (let c = 0; c < 8; c++) { const dx = c & 1, dy = (c >> 1) & 1, dz = (c >> 2) & 1;
    d += at(i0[0] + dx, i0[1] + dy, i0[2] + dz) * (dx ? t[0] : 1 - t[0]) * (dy ? t[1] : 1 - t[1]) * (dz ? t[2] : 1 - t[2]); }
  return (d - 128) / f.scale; }
function insideBody(b, p) { return b.sdf ? sdfAt(b.sdf, p) > 0 : loftDist(b.stations, V3(p.x - (b.ox || 0), p.y, p.z)) > 0; }
function bodyBox(b) {
  if (b.sdf) return [V3(...b.sdf.lo), V3(...b.sdf.hi)];
  const S = b.stations, hb = Math.max(...S.map((q) => q[1]));
  const o = b.ox || 0; return [V3(o - hb, Math.min(...S.map((q) => q[3])), Math.min(...S.map((q) => q[0]))), V3(o + hb, Math.max(...S.map((q) => q[2])), Math.max(...S.map((q) => q[0])))];
}
function subsetGeo(g, i, seeds, margin) {
  const P = g.attributes.position, near = new Float32Array(P.count), v = V3();
  for (let k = 0; k < P.count; k++) { v.fromBufferAttribute(P, k); let dmin = 1e9; for (const sd of seeds) dmin = Math.min(dmin, v.distanceTo(sd)); near[k] = v.distanceTo(seeds[i]) - dmin; }
  const idx = g.index.array, out = [];
  for (let t = 0; t < idx.length; t += 3) if (near[idx[t]] < margin || near[idx[t + 1]] < margin || near[idx[t + 2]] < margin) out.push(idx[t], idx[t + 1], idx[t + 2]);
  if (!out.length) return null;
  const o = new THREE.BufferGeometry(); for (const k in g.attributes) o.setAttribute(k, g.attributes[k]); o.setIndex(out); return o;
}
function fracture(ship, b, rng) {
  const [lo, hi] = bodyBox(b), size = hi.clone().sub(lo), seeds = [];
  const spacing = Math.cbrt(size.x * size.y * size.z / b.n) * 0.55;
  for (let tries = 0; seeds.length < b.n && tries < 6000; tries++) {
    const p = V3(lo.x + rng() * size.x, lo.y + rng() * size.y, lo.z + rng() * size.z);
    if (insideBody(b, p) && seeds.every((q) => q.distanceTo(p) > spacing * (0.6 + rng() * 0.8))) seeds.push(p);
  }
  b.seeds = seeds; b.u = { uSeeds: { value: Array.from({ length: 16 }, (_, k) => (seeds[k] || V3()).clone()) }, uSeedN: { value: seeds.length }, uOpen: { value: new Array(16).fill(0) } };
  b.frags = [];
  seeds.forEach((sd, i) => {
    const pivot = new THREE.Group(), inner = new THREE.Group(); pivot.position.copy(sd); inner.position.copy(sd).negate(); pivot.add(inner); ship.root.add(pivot); pivot.visible = false;
    const P = { id: b.name + i, pivot, inner, center: sd.clone(), vel: V3(), spin: V3(), detached: false, mats: [], sys: b.sys, cuts: [], caps: [], frag: true, body: b, cell: i };
    ship.pieces[P.id] = P; b.frags.push(P); ship.frags.push(P);
    for (const [g, pp] of b.geos) {
      const sub = subsetGeo(g, i, seeds, 3); if (!sub) continue;
      const m = o.hullMat(pp, ship); m.userData.u.uCell.value = i; if (b.sys === 'life' && pp.mode === 0) m.userData.u.uSecOn.value = 1; m.userData.u.uSeeds = b.u.uSeeds; m.userData.u.uSeedN = b.u.uSeedN; m.userData.u.uOpen = b.u.uOpen;
      const mesh = new THREE.Mesh(sub, m); mesh.userData = { ship, sys: b.sys, piece: P }; inner.add(mesh); P.mats.push(m); ship.mats.push(m); ship.meshes.push(mesh);
    }
  });
  // Neighbouring cells, found by sampling the body; each pair gets a cut face on its shared plane, one for each side.
  const pairs = new Map(), p = V3();
  for (let k = 0; k < 90000; k++) {
    p.set(lo.x + rng() * size.x, lo.y + rng() * size.y, lo.z + rng() * size.z); if (!insideBody(b, p)) continue;
    let a = -1, da = 1e9, c = -1, dc = 1e9;
    seeds.forEach((sd, i) => { const d = p.distanceTo(sd); if (d < da) { c = a; dc = da; a = i; da = d; } else if (d < dc) { c = i; dc = d; } });
    if (c >= 0 && dc - da < 1.5) { const key = Math.min(a, c) + ':' + Math.max(a, c); if (!pairs.has(key)) pairs.set(key, []); pairs.get(key).push(p.clone()); }
  }
  b.pairs = [];
  for (const [key, pts] of pairs) { if (pts.length < 3) continue; const [a, c] = key.split(':').map(Number); cutFace(ship, b, a, c, pts); cutFace(ship, b, c, a, pts); b.pairs.push({ a, c, pts }); }
}
// A cut face: the plane between two cells, clipped in the shader to the body and to this pair's shared border. It shows the
// structure it cuts through (deck plates every 3.4 m, bulkheads every 6 m, the skin's thickness), and between them rooms with
// depth, drawn by interior mapping (owner, 2026-11-02, after Spider-Man PS4's interior mapping). Soot over all of it, char and
// embers toward the broken edges, flame licking up the walls near the break.
const CUTFACE_GLSL = `
uniform vec3 uSeeds[16]; uniform int uSeedN; uniform int uA; uniform int uB;
uniform vec4 uSt[48]; uniform float uCh[48]; uniform int uStN; uniform float uOx;
uniform float uHeat; uniform float uTime; uniform vec3 uAcross; uniform vec3 uDepth; uniform vec3 uMid;
varying vec3 vObj; varying vec3 vCamObj;
float fh(vec3 p){ p = fract(p*0.3183099+.1); p *= 17.0; return fract(p.x*p.y*p.z*(p.x+p.y+p.z)); }
float fn(vec3 x){ vec3 i=floor(x), f=fract(x); f=f*f*(3.-2.*f);
  return mix(mix(mix(fh(i),fh(i+vec3(1,0,0)),f.x),mix(fh(i+vec3(0,1,0)),fh(i+vec3(1,1,0)),f.x),f.y),
             mix(mix(fh(i+vec3(0,0,1)),fh(i+vec3(1,0,1)),f.x),mix(fh(i+vec3(0,1,1)),fh(i+vec3(1,1,1)),f.x),f.y),f.z); }
// Signed distance into the lofted body (positive inside): the octagon at z, as loftDist() in the page's script.
float loftD(vec3 p){
  p.x -= uOx;
  if (p.z > uSt[0].x || p.z < uSt[uStN - 1].x) return -1.0;
  vec4 s = uSt[0]; float ch = uCh[0];
  for (int i = 0; i < 47; i++) { if (i >= uStN - 1) break; vec4 a = uSt[i], b = uSt[i + 1];
    if (p.z <= a.x && p.z >= b.x) { float t = (a.x - p.z)/max(a.x - b.x, 1e-4); s = mix(a, b, t); ch = mix(uCh[i], uCh[i + 1], t); break; } }
  float ax = abs(p.x);
  float d = min(min(s.y - ax, s.z - p.y), p.y - s.w);
  d = min(d, min((s.y + s.z - ch - ax - p.y)*0.70710678, (s.y - s.w - ch - ax + p.y)*0.70710678));
  return min(d, min(uSt[0].x - p.z, p.z - uSt[uStN - 1].x)); }
#ifdef BODY_SDF
uniform highp sampler3D uSdf; uniform vec3 uSdfLo; uniform vec3 uSdfHi; uniform float uSdfScale;
float bodyD(vec3 p){ vec3 t = (p - uSdfLo)/(uSdfHi - uSdfLo);   // the hull's field (tools/blender/hull_sdf.py), as sdfAt()
  if (any(lessThan(t, vec3(0.0))) || any(greaterThan(t, vec3(1.0)))) return -1.0;
  return (texture(uSdf, t).r*255.0 - 128.0)/uSdfScale; }
#else
float bodyD(vec3 p){ return loftD(p); }
#endif
float band(float v, float period, float w){ float m = mod(v + 1000.0*period, period); return 1.0 - smoothstep(w*0.5, w, min(m, period - m)); }
vec3 toF(vec3 p){ vec3 d = p - uMid; return vec3(dot(d, uAcross), p.y, dot(d, uDepth)); }
vec3 dirF(vec3 v){ return vec3(dot(v, uAcross), v.y, dot(v, uDepth)); }
vec3 deckCell(float deckN){ return vec3(6.0 + 3.0*floor(fh(vec3(deckN, 4.1, 0.7))*3.0), 3.4, 5.0 + 3.0*fh(vec3(deckN, 8.3, 1.9))); }
float deckShift(float deckN){ return fh(vec3(deckN, 2.0, 5.0))*12.0; }
float cutFrameW(vec3 f, float k){ float deckN = floor(f.y/3.4); vec3 cell = deckCell(deckN);
  return max(band(f.y, 3.4, 0.32*k), band(f.x + deckShift(deckN), cell.x, 0.26*k)); }
float cutFrame(vec3 f){ return cutFrameW(f, 1.0); }
vec3 interiorCut(vec3 f, vec3 V, out float solid){
  float deckN = floor((f.y + V.y*0.03)/3.4); vec3 cell = deckCell(deckN);
  vec3 q = f + vec3(deckShift(deckN), 0.0, 0.0) + V*0.03; q.z = max(q.z, 0.001);
  vec3 room = floor(q/cell); room.z = 0.0; float rh = fh(room + vec3(0.37, 1.3, 2.9) + deckN*0.11);
  solid = 0.0;
  vec3 t3;
  for (int a = 0; a < 3; a++) { float va = V[a]; if (abs(va) < 1e-4) { t3[a] = 1e5; continue; }
    float b = va > 0. ? (room[a] + 1.0)*cell[a] : room[a]*cell[a]; t3[a] = (b - q[a])/va; }
  if (V.z <= 0.0) t3.z = 1e5;
  float t = min(t3.x, min(t3.y, t3.z)); vec3 hit = q + V*t; vec3 local = hit - room*cell; vec3 base;
  if (t == t3.y) base = V.y < 0. ? vec3(0.32, 0.30, 0.27)*(0.85 + 0.15*step(0.5, fract(local.x*0.5 + 0.5*step(0.5, fract(local.z*0.5)))))
                               * (1.0 - 0.7*step(0.72, fh(floor(local*0.9) + room)))
                               : vec3(0.05) + vec3(0.7, 0.66, 0.58)*step(0.8, fract(local.x/cell.x*2.0 + 0.1))*step(0.5, rh);
  else if (t == t3.z) base = vec3(0.13, 0.125, 0.12)*(0.7 + 0.3*fh(floor(local*0.5) + room)) * (1.0 - 0.55*step(local.y, 0.25));
  else base = vec3(0.19, 0.185, 0.18)*(0.75 + 0.25*fh(floor(local*0.5) + room)) * (1.0 - 0.55*step(local.y, 0.25));
  float lamp = 0.35 + 0.65*clamp(local.y/3.4, 0.0, 1.0);
  float fl = 1.0; vec3 L;
  if (rh < 0.62) L = vec3(0.03, 0.035, 0.05);
  else if (rh < 0.86) L = vec3(0.9, 0.07, 0.04)*fl;
  else L = vec3(0.55, 0.65, 0.85);
  float sootP = smoothstep(0.42, 0.7, fn(hit*0.7 + room*3.1)) * (1.0 - smoothstep(0.0, 4.5, local.z));
  base *= 1.0 - 0.8*sootP;
  float lick = (1.0 - smoothstep(0.0, 2.4, local.z)) * smoothstep(0.5, 0.8, fn(vec3(hit.x*0.9, hit.y*1.7 - uTime*1.6, hit.z*0.9 + room.x)));
  vec3 flame = vec3(1.0, 0.3, 0.07)*lick*(1.0 - step(t3.y, t)*step(V.y, 0.0)*0.5);
  return base*L*2.0*lamp*exp(-t*0.13) + flame*(0.35 + 0.35*uHeat)*(0.85 + 0.15*sin(uTime*14.0 + hit.y*3.0));
}`;
// The stations the cut-face shader clips to: the body's own, bow first, with any station that lies on the straight line
// between its neighbours dropped, so a hull fitted every 2 m (a Krai hull) fits the shader's 48. It held 12, which every
// Concord hull fits, so on a Krai hull every cut face past the bow was thrown away and a break left a hole (owner,
// 2026-11-04: "hole in the hull", "missing geometry, fix urgently").
function cutStations(b) {
  if (b.cutSt) return b.cutSt;
  const S = [...b.stations].sort((p, q) => q[0] - p[0]);
  for (let tol = 0.01; ; tol *= 2) {
    const keep = [S[0]];
    for (let i = 1; i < S.length - 1; i++) { const a = keep[keep.length - 1], c = S[i + 1], t = (a[0] - S[i][0]) / (a[0] - c[0] || 1);
      if ([1, 2, 3, 4].some((j) => Math.abs(S[i][j] - (a[j] + (c[j] - a[j]) * t)) > tol)) keep.push(S[i]); }
    keep.push(S[S.length - 1]);
    if (keep.length <= 48) return (b.cutSt = keep);
  }
}
function cutFace(ship, b, a, c, pts) {
  const sa = b.seeds[a], sb = b.seeds[c], n = sb.clone().sub(sa).normalize(), mid = sa.clone().add(sb).multiplyScalar(0.5);
  const u = Math.abs(n.y) < 0.9 ? V3(0, 1, 0).cross(n).normalize() : V3(1, 0, 0).cross(n).normalize(), v = n.clone().cross(u);
  let u0 = 1e9, u1 = -1e9, v0 = 1e9, v1 = -1e9;
  for (const q of pts) { const d = q.clone().sub(mid), x = d.dot(u), y = d.dot(v); u0 = Math.min(u0, x); u1 = Math.max(u1, x); v0 = Math.min(v0, y); v1 = Math.max(v1, y); }
  u0 -= 7; u1 += 7; v0 -= 7; v1 += 7;
  const g = new THREE.PlaneGeometry(u1 - u0, v1 - v0); g.translate((u0 + u1) / 2, (v0 + v1) / 2, 0); g.applyMatrix4(new THREE.Matrix4().makeBasis(u, v, n).setPosition(mid));
  const nh = V3(n.x, 0, n.z); const depth = nh.length() > 0.3 ? nh.normalize().negate() : V3(1, 0, 0), across = V3(0, 1, 0).cross(depth).normalize();
  const P = b.frags[a], S = b.sdf ? [] : cutStations(b);
  const st = Array.from({ length: 48 }, (_, i) => new THREE.Vector4(...(S[i] ? [S[i][0], S[i][1], S[i][2], S[i][3]] : [0, 0, 0, 0])));
  const ch = Array.from({ length: 48 }, (_, i) => (S[i] ? chOf(S[i][1], S[i][2], S[i][3], S[i][4]) : 0));
  const m = new THREE.MeshStandardMaterial({ color: 0xffffff, metalness: 0.3, roughness: 0.85, emissive: 0xffffff, envMapIntensity: o.env() });
  const U = { uSeeds: b.u.uSeeds, uSeedN: b.u.uSeedN, uA: { value: a }, uB: { value: c }, uSt: { value: st }, uCh: { value: ch }, uStN: { value: S.length }, uOx: { value: b.ox || 0 },
    uHeat: P.mats[0] ? P.mats[0].userData.u.uCutHeat : { value: 0 }, uTime: sharedTime, uAcross: { value: across }, uDepth: { value: depth }, uMid: { value: mid.clone() } };
  if (b.sdf) Object.assign(U, { uSdf: { value: b.sdf.tex }, uSdfLo: { value: V3(...b.sdf.lo) }, uSdfHi: { value: V3(...b.sdf.hi) }, uSdfScale: { value: b.sdf.scale } });
  m.onBeforeCompile = (sh) => {
    Object.assign(sh.uniforms, U);
    sh.vertexShader = sh.vertexShader.replace('#include <common>', '#include <common>\nvarying vec3 vObj; varying vec3 vCamObj;')
      .replace('#include <begin_vertex>', '#include <begin_vertex>\nvObj = position; vCamObj = (inverse(modelMatrix) * vec4(cameraPosition, 1.0)).xyz;');
    sh.fragmentShader = sh.fragmentShader.replace('#include <common>', '#include <common>\n' + (b.sdf ? '#define BODY_SDF\n' : '') + CUTFACE_GLSL)
      .replace('#include <map_fragment>', `#include <map_fragment>
        float sd = bodyD(vObj); if (sd < 0.0) discard;
        vec3 sA = uSeeds[uA]; float dA = length(vObj - sA); float edgeC = 1e9;
        for (int k = 0; k < 16; k++) { if (k >= uSeedN) break; if (k == uA || k == uB) continue; vec3 sk = uSeeds[k]; float dk = length(vObj - sk);
          if (dk < dA - 0.02) discard; edgeC = min(edgeC, (dk*dk - dA*dA)/(2.0*length(sk - sA))); }
        float ed = min(sd, edgeC); float nz = 0.6*fn(vObj*0.35) + 0.4*fn(vObj*1.3 + 5.0);
        vec3 fP = toF(vObj); float frame = cutFrame(fP);
        float skin = 1.0 - smoothstep(0.45, 1.1, sd);
        float solid; vec3 roomCol = interiorCut(fP, dirF(normalize(vObj - vCamObj)), solid);
        float soot = clamp(1.0 - smoothstep(0.0, 12.0, ed + nz*5.0), 0.0, 1.0);
        float isStruct = max(max(step(0.5, frame), step(0.5, skin)), solid);
        vec3 metal = mix(vec3(0.33, 0.32, 0.30), vec3(0.58, 0.57, 0.54), skin);
        metal = mix(metal, vec3(0.018, 0.016, 0.015), clamp(0.35 + 0.6*soot + 0.25*(nz - 0.5), 0.0, 1.0));
        diffuseColor.rgb = isStruct > 0.5 ? metal : vec3(0.0);
        vec3 capEmit = isStruct > 0.5 ? vec3(0.0) : roomCol*(1.0 - 0.8*soot);
        float capEmber = (1.0 - smoothstep(0.0, 1.1, ed + nz*1.3))*(0.45 + 0.55*uHeat)*(0.8 + 0.2*sin(uTime*17.0 + nz*30.0));
        float rim = clamp(cutFrameW(fP, 1.9 + 0.8*nz) - cutFrame(fP), 0.0, 1.0) * smoothstep(0.3, 0.55, fn(vObj*0.9 + 2.0)) * (1.0 - solid);
        capEmit += vec3(1.0, 0.36, 0.07)*rim*(0.35 + 0.8*uHeat);
        diffuseColor.rgb = mix(diffuseColor.rgb, vec3(0.02, 0.016, 0.014), rim);`)
      .replace('#include <emissivemap_fragment>', `#include <emissivemap_fragment>
        totalEmissiveRadiance = capEmit + vec3(1.0, 0.36, 0.07)*capEmber*2.8;`);
  };
  m.customProgramCacheKey = () => 'fleet-tern-cutface' + (b.sdf ? '-sdf' : '');
  const mesh = new THREE.Mesh(g, m); mesh.visible = false; mesh.userData.other = c; P.inner.add(mesh); P.caps.push(mesh);
}

// A break never shows a hollow shell (owner, 2026-11-02: "interiors are hollowed, this is not what I want"). Every place a
// piece can break is closed by a cross-section: the skin cut edge on, deck plates every 3.4 m and bulkheads every 6 m (the
// grid the holes use), rooms between them, some lit red by emergency light, charred toward the broken edge with an ember
// line that cools with the piece. Hidden while the piece is whole; shown when it breaks away.
const CAP_GLSL = `
uniform vec2 uPoly[48]; uniform int uPolyN; uniform float uHeat; uniform float uTime;
varying vec2 vRH;
float ch3(vec3 p){ p = fract(p*0.3183099+.1); p *= 17.0; return fract(p.x*p.y*p.z*(p.x+p.y+p.z)); }
float cn3(vec3 x){ vec3 i=floor(x), f=fract(x); f=f*f*(3.-2.*f);
  return mix(mix(mix(ch3(i),ch3(i+vec3(1,0,0)),f.x),mix(ch3(i+vec3(0,1,0)),ch3(i+vec3(1,1,0)),f.x),f.y),
             mix(mix(ch3(i+vec3(0,0,1)),ch3(i+vec3(1,0,1)),f.x),mix(ch3(i+vec3(0,1,1)),ch3(i+vec3(1,1,1)),f.x),f.y),f.z); }
float polyDist(vec2 p){ float d = 1e9;
  for (int i = 0; i < 48; i++) { if (i >= uPolyN) break; vec2 a = uPoly[i], b = uPoly[(i + 1) % uPolyN];
    vec2 pa = p - a, ba = b - a; float h = clamp(dot(pa, ba)/max(dot(ba, ba), 1e-6), 0.0, 1.0); d = min(d, length(pa - ba*h)); }
  return d; }`;
function dedupe(pts) { return pts.filter((q, i) => i === 0 || Math.hypot(q[0] - pts[i - 1][0], q[1] - pts[i - 1][1]) > 1e-3); }
function ellipse(a, b, n) { return Array.from({ length: n }, (_, i) => [Math.cos(i / n * TAU) * a, Math.sin(i / n * TAU) * b]); }
function addCap(ship, P, poly, to3D, nOut) {
  const g = new THREE.ShapeGeometry(new THREE.Shape(poly.map(([x, y]) => new THREE.Vector2(x, y))));
  const Pp = g.attributes.position, rh = new Float32Array(Pp.count * 2);
  for (let i = 0; i < Pp.count; i++) { const x = Pp.getX(i), y = Pp.getY(i), v = to3D(x, y); rh[i * 2] = x; rh[i * 2 + 1] = y; Pp.setXYZ(i, v.x, v.y, v.z); }
  g.setAttribute('aRH', new THREE.BufferAttribute(rh, 2));
  const idx = g.index.array, A = V3(), B = V3(), C = V3();
  A.fromBufferAttribute(Pp, idx[0]); B.fromBufferAttribute(Pp, idx[1]); C.fromBufferAttribute(Pp, idx[2]);
  if (B.clone().sub(A).cross(C.clone().sub(A)).dot(nOut) < 0) for (let t = 0; t < idx.length; t += 3) { const k = idx[t + 1]; idx[t + 1] = idx[t + 2]; idx[t + 2] = k; }
  g.index.needsUpdate = true; g.computeVertexNormals();
  const flat = new Array(48).fill(0).map((_, i) => new THREE.Vector2(...(poly[i] || [0, 0])));
  const m = new THREE.MeshStandardMaterial({ color: 0xffffff, metalness: 0.35, roughness: 0.8, emissive: 0xffffff, envMapIntensity: o.env() });
  const u = { uPoly: { value: flat }, uPolyN: { value: Math.min(48, poly.length) }, uHeat: P.mats[0].userData.u.uCutHeat, uTime: sharedTime };
  m.onBeforeCompile = (sh) => {
    Object.assign(sh.uniforms, u);
    sh.vertexShader = sh.vertexShader.replace('#include <common>', '#include <common>\nattribute vec2 aRH; varying vec2 vRH;')
      .replace('#include <begin_vertex>', '#include <begin_vertex>\nvRH = aRH;');
    sh.fragmentShader = sh.fragmentShader.replace('#include <common>', '#include <common>\n' + CAP_GLSL)
      .replace('#include <map_fragment>', `#include <map_fragment>
        vec2 p = vRH; float ed = polyDist(p); float nz = 0.6*cn3(vec3(p*0.7, 3.0)) + 0.4*cn3(vec3(p*2.3, 9.0));
        float deck = 1.0 - smoothstep(0.14, 0.32, abs(mod(p.y + 340.0, 3.4) - 0.2));
        float deckN = floor((p.y + 340.0)/3.4); float wx = p.x + 600.0 + ch3(vec3(deckN, 2.0, 5.0))*6.0; // bulkheads stagger deck to deck
        float wall = 1.0 - smoothstep(0.10, 0.26, abs(mod(wx, 6.0) - 0.15));
        vec2 cell = vec2(floor(wx/6.0), deckN); float roomH = ch3(vec3(cell, 7.0));
        float frame = max(deck, wall);
        vec3 col = mix(vec3(0.07, 0.068, 0.066), vec3(0.46, 0.45, 0.43), frame);
        float skin = 1.0 - smoothstep(0.35, 0.9, ed); col = mix(col, vec3(0.62, 0.61, 0.58), skin);
        float charK = 1.0 - smoothstep(0.6, 6.0, ed + nz*2.5);
        col = mix(col, vec3(0.025, 0.022, 0.02), charK*0.9);
        diffuseColor.rgb = col;
        float glowRoom = step(roomH, 0.16) * (1.0 - frame) * (1.0 - charK);`)
      .replace('#include <emissivemap_fragment>', `#include <emissivemap_fragment>
        float fl = 1.0;
        totalEmissiveRadiance = vec3(0.75, 0.1, 0.04)*glowRoom*fl*(0.2 + 0.5*uHeat)
          + vec3(1.0, 0.36, 0.07)*(1.0 - smoothstep(0.0, 1.3, ed + nz*0.9))*uHeat*2.2;`);
  };
  m.customProgramCacheKey = () => 'fleet-cap';
  const mesh = new THREE.Mesh(g, m); mesh.visible = false; P.inner.add(mesh); P.caps.push(mesh);
  return mesh;
}

// An emitter on the ship: a programme of phases (fire for 3 s, then smoke, then embers), carried by its piece.
function addEmitter(ship, P, pos, dir, phases, tag) {
  const e = { P, pos: pos.clone(), dir: dir.clone().normalize(), phases, t: 0, acc: 0, tag };
  ship.emitters.push(e); return e;
}

// A broken piece burns (owner, 2026-11-03: "more fire when ship pieces break off, more particles and flames trailing off"):
// flames along its broken border for about 30 s that stream behind it as it drifts, blue electrical arcs crackling in the
// break, and embers after. Points are taken from the border the piece shared with its neighbours, or from where it was cut.
function wreckFire(ship, P, at = null) {
  const s = Math.max(0.5, len(ship) / 150), pts = at ? at.map((p) => p.clone()) : [];
  if (!at && P.frag && P.body.pairs) for (const pr of P.body.pairs) if ((pr.a === P.cell || pr.c === P.cell) && pts.length < 5) pts.push(pr.pts[Math.floor(fxRng() * pr.pts.length)].clone());
  if (!pts.length) pts.push(P.cuts[0] ? P.cuts[0].c.clone() : P.center.clone());
  pts.forEach((p, i) => {
    const out = p.clone().sub(P.center); if (out.lengthSq() < 1e-3) out.set(0, 1, 0); out.normalize();
    addEmitter(ship, P, p, out, [{ type: 'fireball', rate: 30, dur: 0.6, scale: 0.35 * s, light: i === 0 }, { type: 'flame', rate: 55, dur: 9, scale: 0.75 * s, light: i === 0 }, { type: 'flame', rate: 20, dur: 22, scale: 0.6 * s }, { type: 'embers', rate: 5, dur: 1e9, scale: s }], 'debris');
    addEmitter(ship, P, p, out, [{ type: 'arc', rate: 6, dur: 18 }, { type: 'sparks', rate: 2, dur: 1e9 }], 'debris');
  });
}
function swapToPieces(ship) { for (const b of ship.bodies) swapBody(ship, b); }
// A heavy hit on a badly damaged hull knocks off the outer piece it lands on.
function chunkOff(ship, hit) {
  const b = ship.bodies[0];
  if (hit.mesh.userData.piece.sys !== 'life' || ship.hull > 60) return;
  swapBody(ship, b);
  const f = nearestFrag(b, hit.point);
  if (f.center.distanceTo(ship.corePos) > len(ship) * 0.18) blowOff(ship, f, 0.7);
}
// A core breach is a sequence, not one bang (owner, 2026-11-02): for about 3.6 s the core burns, secondary blasts go off and the
// outer pieces, the command block and the nacelles come off one by one, outermost first; then the main blast.
const BREACH = { first: 0.5, last: 3.0, main: 3.6, share: 0.55 };
function breakup(ship) {
  if (ship.broken || ship.breach) return;
  ship.breach = { t: 0, events: [], done: false };
  swapToPieces(ship);
  const core = ship.corePos, early = [...ship.frags.filter((f) => !f.detached && f.body === ship.bodies[0])].sort((p, q) => q.center.distanceTo(core) - p.center.distanceTo(core));
  const pick = early.slice(0, Math.ceil(early.length * BREACH.share)).concat(o.extras(ship).map((k) => ship.pieces[k]).filter((P) => P && !P.detached));
  pick.forEach((P, i) => ship.breach.events.push({ at: lerp(BREACH.first, BREACH.last, pick.length > 1 ? i / (pick.length - 1) : 0) + rr(fxRng, -0.15, 0.15), P }));
  ship.breach.events.sort((a, b) => a.at - b.at);
  o.onChange(ship);
}

// A piece breaks away: it gets its own motion, its broken edges are charred toward the break, its lights die.
function detach(ship, P, vel, spin, heat = 1) {
  if (P.detached) return; P.detached = true; P.vel.copy(vel); P.spin.copy(spin);
  for (const m of P.mats) { const u = m.userData.u; u.uCutCount.value = Math.min(4, P.cuts.length); P.cuts.slice(0, 4).forEach((c, i) => { u.uCuts.value[i].set(c.n.x, c.n.y, c.n.z, c.d); u.uCutC.value[i].set(c.c.x, c.c.y, c.c.z, c.r); }); u.uCutHeat.value = heat; }
  P.dieAt = ship.t; P.lightLeft = 1;
  for (const c of P.caps) c.visible = true;
  if (P.frag) { // its border opens on the pieces still attached too: their faces toward it close, their edges char
    const b = P.body; b.u.uOpen.value[P.cell] = 1;
    for (const f of b.frags) for (const c of f.caps) if (c.userData.other === P.cell) { c.visible = true; f.mats.forEach((m) => { if (!f.detached) m.userData.u.uCutHeat.value = 1; }); }
  }
}
function charStump(ship, P, cut) { // the part left on the ship is charred at the break too
  for (const m of P.mats) { const u = m.userData.u, i = u.uCutCount.value; if (i >= 4) continue; u.uCuts.value[i].set(cut.n.x, cut.n.y, cut.n.z, cut.d); u.uCutC.value[i].set(cut.c.x, cut.c.y, cut.c.z, cut.c.r ?? cut.r); u.uCutCount.value = i + 1; u.uCutHeat.value = 1; }
}
function nearestFrag(b, p) { let best = b.frags[0], d = 1e9; for (const f of b.frags) { const k = f.center.distanceTo(p); if (k < d) { d = k; best = f; } } return best; }
function centerOf(o) { if (o.userData.center) return o.userData.center; if (o.geometry) { o.geometry.computeBoundingSphere(); return o.geometry.boundingSphere.center.clone(); } return o.position.clone(); }
const liveMesh = (m) => { const P = m.userData.piece; return !P.detached && !P.gone && P.pivot.visible; };
// A body is swapped for its pieces the first time it starts to come apart; the pieces sit where the body was.
function swapBody(ship, b) {
  const I = b.intact; if (I.gone) return; I.gone = true; I.pivot.visible = false;
  for (const o of I.inner.children.slice()) { if (o.userData.skin) continue; nearestFrag(b, centerOf(o)).inner.add(o); }
  for (const e of ship.emitters) if (e.P === I) e.P = nearestFrag(b, e.pos);
  for (const n of ship.navs) if (n.P === I) n.P = nearestFrag(b, n.s.position);
  for (const f of b.frags) f.pivot.visible = true;
  makeWires(ship, b);
}
// One piece blown off: a small blast, sparks along its broken border, a fire on it.
function blowOff(ship, P, speed = 1) {
  if (P.detached || P.gone) return;
  const s = len(ship) / 150, core = ship.corePos.clone(), out = P.center.clone().sub(core); out.y *= 1.4; out.normalize();
  detach(ship, P, out.clone().multiplyScalar((4 + 5 * fxRng()) * s * speed).add(V3(rr(fxRng, -1, 1), rr(fxRng, -1, 1), rr(fxRng, -1, 1)).multiplyScalar(s)),
    V3(rr(fxRng, -0.25, 0.25), rr(fxRng, -0.25, 0.25), rr(fxRng, -0.25, 0.25)));
  // A big blast (owner: "There should be a big explosion when a chunk falls off"): fireball, flash, a small shockwave, debris, smoke.
  const w = P.center.clone().applyMatrix4(ship.root.matrixWorld);
  emit('fireball', w, out, 280, 1.0 * s); emit('fireball', w, V3(0, 1, 0), 120, 0.7 * s); emit('sparks', w, out, 220, 1.2); emit('smoke', w, out, 60, 1.2 * s);
  flash(w, 150 * s, 0.7); shockwave(ship, w, 110 * s); debrisAdd(w, 90, len(ship), 0.08);
  wreckFire(ship, P);
}
function stepBreach(ship, dt) {
  const B = ship.breach; if (!B || B.done) return;
  B.t += dt;
  while (B.events.length && B.events[0].at <= B.t) { const e = B.events.shift(); if (!o.special(ship, e.P)) blowOff(ship, e.P); }
  if (fxRng() < dt * 3) { const pts = ship.meshes.filter(liveMesh); const m = pts[Math.floor(fxRng() * pts.length)]; if (m) { m.geometry.computeBoundingSphere(); const w = m.geometry.boundingSphere.center.clone().applyMatrix4(m.matrixWorld); emit('fireball', w, V3(0, 1, 0), 25, 0.4 * len(ship) / 150); flash(w, 30, 0.3); } }
  if (B.t >= BREACH.main) { B.done = true; mainBlast(ship); }
}
function mainBlast(ship) {
  ship.broken = true; ship.hull = 0;
  for (const k in ship.sys) { ship.sys[k].health = 0; ship.sys[k].state = 'destroyed'; }
  const s = len(ship) / 150, core = ship.corePos.clone(), cw = core.clone().applyMatrix4(ship.root.matrixWorld);
  for (const P of Object.values(ship.pieces)) {
    if (P.detached || P.gone) continue;
    const out = P.center.clone().sub(core); out.y *= 1.6; const d = out.length() || 1; out.normalize();
    const sp = (6 + 10 * fxRng()) * s * (1 + 40 / (d + 30));
    detach(ship, P, out.multiplyScalar(sp).add(V3(rr(fxRng, -2, 2), rr(fxRng, -2, 2), rr(fxRng, -2, 2)).multiplyScalar(s)),
      V3(rr(fxRng, -0.35, 0.35), rr(fxRng, -0.35, 0.35), rr(fxRng, -0.35, 0.35)));
    const ec = P.cuts[0] ? P.cuts[0].c.clone().lerp(P.center, 0.5) : P.center.clone();
    wreckFire(ship, P);
  }
  ship.emitters = ship.emitters.filter((e) => e.tag === 'debris' || e.tag === 'hole');
  emit('fireball', cw, V3(0, 1, 0), 500, 1.4 * s); emit('coolant', cw, V3(0, 0, 1), 260, 2.2 * s); emit('sparks', cw, V3(0, 1, 0), 300, 1.4);
  flash(cw, 260 * s, 1.4); shockwave(ship, cw, len(ship) * 1.6); debrisBurst(cw, len(ship));
  if (ship.shield) ship.shield.visible = false; o.onChange(ship);
}

// Wires (owner, 2026-11-02: "add a shader for like wires and sparks as they fly apart"). Cables cross every border two
// pieces share. As the pieces separate they stretch, glow at their hot ends and arc with blue-white sparks, then snap at a
// random length and hang from each piece, cooling.
const WIRE_MAX = 600;
const wireGeo = new THREE.BufferGeometry();
wireGeo.setAttribute('position', new THREE.BufferAttribute(new Float32Array(WIRE_MAX * 4 * 3), 3).setUsage(THREE.DynamicDrawUsage));
wireGeo.setAttribute('color', new THREE.BufferAttribute(new Float32Array(WIRE_MAX * 4 * 3), 3).setUsage(THREE.DynamicDrawUsage));
wireGeo.setDrawRange(0, 0);
const wireLines = new THREE.LineSegments(wireGeo, new THREE.LineBasicMaterial({ vertexColors: true, toneMapped: false }));
wireLines.frustumCulled = false; scene.add(wireLines);
let wires = [];
const clearWires = () => { wires = []; wireGeo.setDrawRange(0, 0); };
function makeWires(ship, b) {
  for (const pr of b.pairs) {
    const n = b.seeds[pr.c].clone().sub(b.seeds[pr.a]).normalize(), k = Math.min(4, Math.floor(pr.pts.length / 6));
    for (let i = 0; i < k && wires.length < WIRE_MAX; i++) {
      const pt = pr.pts[Math.floor(fxRng() * pr.pts.length)];
      wires.push({ ship, A: b.frags[pr.a], B: b.frags[pr.c], a: pt.clone().addScaledVector(n, -0.6), b: pt.clone().addScaledVector(n, 0.6),
        snapAt: rr(fxRng, 2, 16) * len(ship) / 150 + 1.2, snapped: false, t: 0, arc: 0, tipA: null, tipB: null, hang: rr(fxRng, 0.8, 3.5) });
    }
  }
}
const _wa = V3(), _wb = V3(), _q2 = new THREE.Quaternion();
function stepWires(ship, dt) {
  for (const w of wires) {
    if (w.ship !== ship) continue;
    if (!w.A.detached && !w.B.detached) continue;
    w.t += dt;
    _wa.copy(w.a).applyMatrix4(w.A.inner.matrixWorld); _wb.copy(w.b).applyMatrix4(w.B.inner.matrixWorld);
    if (!w.snapped && _wa.distanceTo(_wb) > w.snapAt) {
      w.snapped = true; const mid = _wa.clone().lerp(_wb, 0.5);
      emit('arc', mid, _wb.clone().sub(_wa).normalize(), 30); emit('sparks', mid, V3(0, 1, 0), 20);
      // each end keeps a loose tail pointing where the wire was, in its own piece's frame
      const inv = (P) => P.pivot.getWorldQuaternion(new THREE.Quaternion()).invert();
      w.tipA = mid.clone().sub(_wa).normalize().applyQuaternion(inv(w.A)); w.tipB = mid.clone().sub(_wb).normalize().applyQuaternion(inv(w.B));
    }
    if (!w.snapped && fxRng() < dt * 5) emit('arc', _wa.clone().lerp(_wb, fxRng()), V3(fxRng() - 0.5, fxRng() - 0.5, fxRng() - 0.5).normalize(), 4);
  }
}
function uploadWires() {
  const P = wireGeo.attributes.position.array, Cc = wireGeo.attributes.color.array; let n = 0;
  for (const w of wires) {
    if (!w.A.detached && !w.B.detached) continue;
    _wa.copy(w.a).applyMatrix4(w.A.inner.matrixWorld); _wb.copy(w.b).applyMatrix4(w.B.inner.matrixWorld);
    let ta, tb;
    if (!w.snapped) { ta = _wa.clone().lerp(_wb, 0.5); tb = ta; }
    else {
      const wig = Math.sin(w.t * 3 + w.hang) * 0.15;
      ta = _wa.clone().addScaledVector(w.tipA.clone().applyQuaternion(w.A.pivot.getWorldQuaternion(_q2)).add(V3(0, wig, 0)).normalize(), w.hang);
      tb = _wb.clone().addScaledVector(w.tipB.clone().applyQuaternion(w.B.pivot.getWorldQuaternion(_q2)).add(V3(0, -wig, 0)).normalize(), w.hang * 0.8);
    }
    const heat = Math.max(0, 1 - w.t / 25), hot = [0.12 + 1.0 * heat, 0.06 + 0.42 * heat, 0.03 + 0.1 * heat], cool = [0.1 + 0.25 * heat, 0.06 + 0.08 * heat, 0.04];
    const arc = !w.snapped && Math.sin(w.t * 40 + w.hang * 9) > 0.6;
    const mid = arc ? [0.7, 0.85, 1.4] : cool;
    for (const [v, c] of [[_wa, hot], [ta, mid], [_wb, hot], [tb, mid]]) { P.set([v.x, v.y, v.z], n * 3); Cc.set(c, n * 3); n++; }
  }
  wireGeo.setDrawRange(0, n); wireGeo.attributes.position.needsUpdate = wireGeo.attributes.color.needsUpdate = true;
}

return { NO_SEEDS, fracture, cutFace, addCap, dedupe, ellipse, addEmitter, wreckFire, swapToPieces, chunkOff, BREACH, breakup, detach, charStump,
  nearestFrag, centerOf, liveMesh, swapBody, blowOff, stepBreach, mainBlast, makeWires, stepWires, uploadWires, clearWires };
};
