// Fleet Command mockups: battle damage on a ship, shared by every faction's ship page (CLAUDE.md 6.1: one implementation).
// Lifted from the Concord line (docs/mockups/concord-hull-concepts.html, mockup 6, where each piece was asked for by the
// owner) so the Krai ships (docs/mockups/krai-ship-concepts.html) take damage with the same code. What colours mean is fixed
// for every faction (the Ship Design Bible, D2): orange fire, cyan-white coolant, violet plasma, sparks for power.
// Edit it here, then re-run tools/mockups/inline_data.py on each page that inlines it.
//
// FCShipFX(THREE, o): o.scene (the scene the effects live in), o.rand (the page's seeded generator, called for every
// random draw, so a scenario replays exactly), o.spriteTex (a soft round sprite for flashes and torpedoes). Returns:
//   DAMAGE_GLSL            hull shader uniforms and helpers: holes and scorch marks (uHoles, uScorch ...), noise (dfbm),
//                          interiorRoom (the decks seen through a hole), dmgHoles and dmgScorch (the char and ember rules),
//                          dmgBreak (a broken piece's cut planes and fracture cell)
//   CELL_GLSL              the fracture cell rule alone (cellDist, cellOf), for a part cut along the hull's cells
//   Pool, glowPool, smokePool, FX, emit(type, pos, dir, count, scale, inherit)
//                          particles: two pools, one table of looks, one emitter
//   holeInterior(c, n, r, type), sharedTime   a modelled hole's inside
//   oneshots, FXL, fxLight, stepFxLights, flash, shockwave, shards, shardData, debrisAdd, debrisBurst
//                          one-shot effects and weapon light
//   FLICK, inFlick, settle(ship, key, target), burst(ship, key, active), flick(t, seed, rate)
//                          lights are on or off: a change flickers a couple of times, then holds (ship.t, ship.flk)
//   beams, beamMat, shoot(kind, from, to, onHit, scale), stepBeams()   phaser beams and torpedoes, lit
//   emitterStep(e, dt, matrixWorld, inherit)   one fixed step of an emitter's programme of phases
//   stepFX(dt)             one fixed step of every effect above; clearFX() clears them all
window.FCShipFX = function FCShipFX(THREE, o) {
'use strict';
const scene = o.scene, navTex = o.spriteTex, fxRng = () => o.rand();
const PI = Math.PI, TAU = PI * 2, V3 = (x = 0, y = 0, z = 0) => new THREE.Vector3(x, y, z);
const rr = (rng, a, b) => a + (b - a) * rng(), lerp = (a, b, t) => a + (b - a) * t;
function cv(w, h) { const c = document.createElement('canvas'); c.width = w; c.height = h; return c; }

// The fracture cell rule: which cell a point is in, by distance to the cells' seeds with a little noise so a break is torn,
// not ruled. One copy, used by the hull (dmgBreak below) and by any part cut along the same lines (cellOf); guarded so a
// shader that already has it can include it again.
const CELL_GLSL = `
#ifndef FC_CELL_GLSL
#define FC_CELL_GLSL
float dh3(vec3 p){ p = fract(p*0.3183099+.1); p *= 17.0; return fract(p.x*p.y*p.z*(p.x+p.y+p.z)); }
float dn3(vec3 x){ vec3 i=floor(x), f=fract(x); f=f*f*(3.-2.*f);
  return mix(mix(mix(dh3(i),dh3(i+vec3(1,0,0)),f.x),mix(dh3(i+vec3(0,1,0)),dh3(i+vec3(1,1,0)),f.x),f.y),
             mix(mix(dh3(i+vec3(0,0,1)),dh3(i+vec3(1,0,1)),f.x),mix(dh3(i+vec3(0,1,1)),dh3(i+vec3(1,1,1)),f.x),f.y),f.z); }
float cellDist(vec3 p, vec3 s, int k){ return length(p - s) + 1.1*(dn3(p*0.32 + float(k)*7.13) - 0.5); }
int cellOf(vec3 p, vec3 seeds[16], int n){ int best = 0; float bd = 1e9;
  for (int k = 0; k < 16; k++) { if (k >= n) break; float d = cellDist(p, seeds[k], k); if (d < bd) { bd = d; best = k; } } return best; }
#endif
`;
const DAMAGE_GLSL = `
uniform vec4 uHoles[24]; uniform vec4 uHoleN[24]; uniform int uHoleCount;
uniform vec4 uScorch[24]; uniform float uScorchHeat[24]; uniform int uScorchCount;
uniform vec4 uCuts[4]; uniform vec4 uCutC[4]; uniform int uCutCount; uniform float uCutHeat;
uniform float uLight; uniform float uTime; uniform float uParallax;
uniform int uCell; uniform vec3 uSeeds[16]; uniform int uSeedN; uniform float uOpen[16]; uniform float uSec[8]; uniform vec3 uSecC; uniform float uSecOn;
varying vec3 vObj; varying vec3 vCamObj;
` + CELL_GLSL + `
float dfbm(vec3 p){ return 0.5*dn3(p)+0.3*dn3(p*2.03)+0.2*dn3(p*4.1); }
vec3 holeLight(float type){ return type < 0.5 ? vec3(1.0,0.16,0.06) : (type < 1.5 ? vec3(0.55,0.32,1.0) : (type < 2.5 ? vec3(0.35,0.85,1.0) : vec3(0.85,0.72,0.55))); }
// Interior mapping: a room grid (decks every 3.4 m, walls every 6 m) and a back wall, seen through the skin.
vec3 interiorRoom(vec3 p, vec3 V, vec4 h, vec4 hn){
  vec3 cell = vec3(6.0, 3.4, 6.0); vec3 q = p + V*0.01; vec3 t3;
  for (int a = 0; a < 3; a++) {
    float va = V[a];
    if (abs(va) < 1e-4) { t3[a] = 1e5; continue; }
    float b = va > 0. ? (floor(q[a]/cell[a])+1.)*cell[a] : floor(q[a]/cell[a])*cell[a];
    t3[a] = (b - q[a]) / va;
  }
  float depth = max(h.w*1.4, 5.0); float vn = dot(V, hn.xyz);
  float tb = vn < -1e-3 ? max((-depth - dot(q-h.xyz, hn.xyz)) / vn, 0.0) : 1e5;
  float t = min(min(t3.x, t3.y), min(t3.z, tb)); vec3 hit = q + V*t; vec3 col; vec3 room = floor(q/cell); vec3 local = hit - room*cell;
  float rh = dh3(room + 0.37);
  if (t == tb) col = vec3(0.14, 0.13, 0.12)*(0.7 + 0.3*dh3(floor(hit*0.8)));
  else if (t == t3.y) col = V.y < 0. ? vec3(0.30, 0.28, 0.25)*(0.85 + 0.15*step(0.5, fract(local.x*0.5 + 0.5*step(0.5, fract(local.z*0.5)))))
                                     * (1.0 - 0.7*step(0.7, dh3(floor(local*0.9) + room)))        // crates and consoles on the deck
                                   : vec3(0.05) + vec3(0.7, 0.66, 0.58)*step(0.82, fract(local.x/6.0*2.0 + 0.1));   // ceiling light strips
  else col = vec3(0.18, 0.17, 0.16)*(0.75 + 0.25*dh3(floor(local*0.5) + room))*(1.0 - 0.55*step(local.y, 0.25));      // walls, dark kick plate
  float lamp = 0.4 + 0.6*clamp(local.y/3.4, 0.0, 1.0);
  float fl = 1.0;
  vec3 L = rh < 0.7 || hn.w > 0.5 ? holeLight(hn.w)*1.4*fl : vec3(0.55, 0.65, 0.85);
  float sootIn = 1.0 - 0.6*(1.0 - smoothstep(h.w*0.2, h.w*0.9, length(p - h.xyz)));
  return col*L*2.4*lamp*sootIn*exp(-t*0.08);
}
// The hole and scorch rules, one implementation for every hull shader (lifted from mockup 6's hull material): call with the
// surface point in ship space and the two noise values; they raise the char and ember levels, and name the hole the point
// is in, if any.
void dmgHoles(vec3 p, float nz, inout float dChar, inout float dEmber, inout int holeIdx){
  for (int i = 0; i < 24; i++) { if (i >= uHoleCount) break; vec4 h = uHoles[i]; float d = length(p - h.xyz); float r = h.w*(0.72+0.56*nz);
    if (d < r) holeIdx = i; dChar = max(dChar, 1.0 - smoothstep(r, r*2.4, d)); dEmber = max(dEmber, step(uHoleN[i].w, 2.5)*step(r, d)*(1.0 - smoothstep(r, r*1.2, d))); } }
void dmgScorch(vec3 p, float nz2, inout float dChar, inout float dEmber){
  for (int i = 0; i < 24; i++) { if (i >= uScorchCount) break; vec4 s = uScorch[i]; float d = length(p - s.xyz); float r = s.w*(0.55+0.9*nz2);
    dChar = max(dChar, 0.92*(1.0 - smoothstep(r*0.25, r, d)));
    dEmber = max(dEmber, uScorchHeat[i]*smoothstep(r*0.45, r*0.75, d)*(1.0 - smoothstep(r*0.75, r, d))); } }
// How a piece of a broken hull is cut (lifted from mockup 6's hull material): cut planes char the plating toward the break
// with an ember line; a fracture cell keeps only the skin nearest its own seed (the border torn a little by noise) and chars
// and embers its edges where a neighbour has gone. Returns true where the fragment is not this cell's: discard it.
bool dmgBreak(vec3 p, float nz, inout float dChar, inout float dEmber){
  for (int i = 0; i < 4; i++) { if (i >= uCutCount) break; float d = abs(dot(p, uCuts[i].xyz) - uCuts[i].w) + nz*2.5 - 0.8;
    float near = 1.0 - smoothstep(uCutC[i].w*0.85, uCutC[i].w, length(p - uCutC[i].xyz));
    dChar = max(dChar, near*(1.0 - smoothstep(0.0, 7.0, d)));
    dEmber = max(dEmber, near*uCutHeat*(1.0 - smoothstep(0.0, 1.6, d))); }
  if (uCell >= 0) {
    vec3 si = uSeeds[uCell]; float di = cellDist(p, si, uCell); float edge = 1e9;
    for (int k = 0; k < 16; k++) { if (k >= uSeedN) break; if (k == uCell) continue; vec3 sk = uSeeds[k];
      float dk = cellDist(p, sk, k); if (dk < di) return true;
      if (max(uOpen[uCell], uOpen[k]) > 0.5) edge = min(edge, (dot(p - sk, p - sk) - dot(p - si, p - si))/(2.0*length(sk - si))); }
    dChar = max(dChar, 1.0 - smoothstep(0.0, 11.0, edge + nz*4.0));
    dEmber = max(dEmber, (1.0 - smoothstep(0.0, 1.6, edge + nz*1.6))*(0.45 + 0.55*uCutHeat)*1.6); }
  return false; }`;

// Particles: two pools (glowing and smoky), one draw call each, stepped on the fixed step.
const partTex = (() => { const c = cv(64, 64), g = c.getContext('2d'), gr = g.createRadialGradient(32, 32, 0, 32, 32, 32);
  gr.addColorStop(0, 'rgba(255,255,255,1)'); gr.addColorStop(0.4, 'rgba(255,255,255,0.45)'); gr.addColorStop(1, 'rgba(255,255,255,0)'); g.fillStyle = gr; g.fillRect(0, 0, 64, 64); return new THREE.CanvasTexture(c); })();
class Pool {
  constructor(max, additive) {
    this.max = max; this.n = 0; this.next = 0;
    this.p = new Float32Array(max * 3); this.v = new Float32Array(max * 3); this.life = new Float32Array(max); this.ttl = new Float32Array(max);
    this.s0 = new Float32Array(max); this.s1 = new Float32Array(max); this.c0 = new Float32Array(max * 4); this.c1 = new Float32Array(max * 4); this.drag = new Float32Array(max);
    this.col = new Float32Array(max * 4); this.size = new Float32Array(max);
    const g = new THREE.BufferGeometry();
    g.setAttribute('position', new THREE.BufferAttribute(this.p, 3).setUsage(THREE.DynamicDrawUsage));
    g.setAttribute('aColor', new THREE.BufferAttribute(this.col, 4).setUsage(THREE.DynamicDrawUsage));
    g.setAttribute('aSize', new THREE.BufferAttribute(this.size, 1).setUsage(THREE.DynamicDrawUsage));
    this.mat = new THREE.ShaderMaterial({ transparent: true, depthWrite: false, blending: additive ? THREE.AdditiveBlending : THREE.NormalBlending,
      uniforms: { uTex: { value: partTex }, uScale: { value: 500 } },
      vertexShader: 'attribute vec4 aColor; attribute float aSize; uniform float uScale; varying vec4 vC; void main(){ vec4 mv = modelViewMatrix*vec4(position,1.); gl_PointSize = clamp(aSize*uScale/max(-mv.z, 0.1), 0.0, 600.0); gl_Position = projectionMatrix*mv; vC = aColor; }',
      fragmentShader: 'uniform sampler2D uTex; varying vec4 vC; void main(){ float a = texture2D(uTex, gl_PointCoord).a; gl_FragColor = vec4(vC.rgb, vC.a*a); }' });
    this.points = new THREE.Points(g, this.mat); this.points.frustumCulled = false; this.points.renderOrder = additive ? 3 : 2; scene.add(this.points);
  }
  spawn(pos, vel, ttl, s0, s1, c0, c1, drag) {
    const i = this.next; this.next = (this.next + 1) % this.max;
    this.p.set([pos.x, pos.y, pos.z], i * 3); this.v.set([vel.x, vel.y, vel.z], i * 3);
    this.life[i] = 0; this.ttl[i] = ttl; this.s0[i] = s0; this.s1[i] = s1; this.c0.set(c0, i * 4); this.c1.set(c1, i * 4); this.drag[i] = drag;
  }
  step(dt) {
    for (let i = 0; i < this.max; i++) {
      // a dead particle has no size and no alpha: some renderers draw a zero-size point as one pixel, which left specks
      // where the smoke had been
      if (this.ttl[i] <= 0) { this.size[i] = 0; this.col[i * 4 + 3] = 0; continue; }
      this.life[i] += dt; const t = this.life[i] / this.ttl[i];
      if (t >= 1) { this.ttl[i] = 0; this.size[i] = 0; this.col[i * 4 + 3] = 0; continue; }
      const k = Math.exp(-this.drag[i] * dt);
      for (let a = 0; a < 3; a++) { this.v[i * 3 + a] *= k; this.p[i * 3 + a] += this.v[i * 3 + a] * dt; }
      this.size[i] = lerp(this.s0[i], this.s1[i], Math.sqrt(t));
      for (let a = 0; a < 4; a++) this.col[i * 4 + a] = lerp(this.c0[i * 4 + a], this.c1[i * 4 + a], t);
    }
  }
  upload() { const a = this.points.geometry.attributes; a.position.needsUpdate = a.aColor.needsUpdate = a.aSize.needsUpdate = true; }
  clear() { this.ttl.fill(0); this.size.fill(0); this.col.fill(0); this.upload(); }
}
const glowPool = new Pool(12000, true), smokePool = new Pool(5000, false);
// Each effect's look, in one table: what the colour means is fixed (orange fire, cyan-white coolant, violet plasma).
const FX = {
  fire: { pool: glowPool, life: [0.45, 0.9], speed: [34, 60], spread: 0.16, size: [1.6, 7.5], c0: [1, 0.82, 0.45, 1], c1: [0.7, 0.12, 0.02, 0], drag: 1.9, also: ['smoke', 0.25] },
  plasma: { pool: glowPool, life: [0.8, 1.5], speed: [30, 52], spread: 0.14, size: [2.5, 13], c0: [0.8, 0.6, 1, 1], c1: [0.25, 0.12, 0.9, 0], drag: 0.8 },
  coolant: { pool: smokePool, life: [1.6, 3.0], speed: [26, 46], spread: 0.15, size: [1.4, 13], c0: [0.92, 0.98, 1, 0.5], c1: [0.7, 0.88, 1, 0], drag: 0.9, also: ['glitter', 0.9] },
  glitter: { pool: glowPool, life: [1, 2], speed: [4, 10], spread: 1.0, size: [0.7, 0.3], c0: [0.85, 0.97, 1, 1], c1: [0.6, 0.85, 1, 0], drag: 0.6 },
  sparks: { pool: glowPool, life: [0.25, 0.6], speed: [22, 55], spread: 1.0, size: [0.9, 0.3], c0: [1, 0.95, 0.6, 1], c1: [1, 0.4, 0.1, 0], drag: 2 },
  arc: { pool: glowPool, life: [0.12, 0.35], speed: [8, 30], spread: 1.0, size: [0.9, 0.2], c0: [0.75, 0.88, 1, 1], c1: [0.35, 0.5, 1, 0], drag: 3 },
  embers: { pool: glowPool, life: [1, 2.5], speed: [2, 6], spread: 1.0, size: [0.8, 0.3], c0: [1, 0.6, 0.2, 1], c1: [0.6, 0.1, 0, 0], drag: 0.4 },
  smoke: { pool: smokePool, life: [2.5, 4.5], speed: [3, 9], spread: 0.6, size: [4, 16], c0: [0.1, 0.09, 0.085, 0.5], c1: [0.05, 0.05, 0.05, 0], drag: 0.5 },
  // a wreck's flames: long-lived, slow, broad, so a piece in motion leaves them streaming behind it
  flame: { pool: glowPool, life: [1.0, 2.2], speed: [3, 9], spread: 0.55, size: [3, 8], c0: [1, 0.78, 0.38, 1], c1: [0.55, 0.08, 0.02, 0], drag: 0.7, also: ['smoke', 0.12] },
  fireball: { pool: glowPool, life: [0.8, 2.0], speed: [10, 70], spread: 1.0, size: [8, 26], c0: [1, 0.9, 0.6, 1], c1: [0.6, 0.1, 0.02, 0], drag: 1.6, also: ['smoke', 0.3] },
};
const _d = V3(), _o = V3(), _q = V3();
function emit(type, pos, dir, count, scale = 1, inherit = null) {
  const f = FX[type];
  for (let i = 0; i < count; i++) {
    _d.set(rr(fxRng, -1, 1), rr(fxRng, -1, 1), rr(fxRng, -1, 1)).normalize().multiplyScalar(f.spread).add(dir).normalize();
    const sp = rr(fxRng, ...f.speed) * scale; const v = _d.clone().multiplyScalar(sp); if (inherit) v.add(inherit);
    f.pool.spawn(pos, v, rr(fxRng, ...f.life), f.size[0] * scale, f.size[1] * scale, f.c0, f.c1, f.drag);
    if (f.also && fxRng() < f.also[1]) emit(f.also[0], pos, dir, 1, scale, inherit);
  }
}

// A modelled hole's inside: deck plates every 3.4 m and walls every 6 m (the same grid the parallax shader draws),
// clipped to the hole, lit by emergency light.
function holeInterior(c, n, r, type) {
  const g = new THREE.Group();
  const mat = new THREE.ShaderMaterial({ side: THREE.DoubleSide,
    uniforms: { uC: { value: new THREE.Vector4(c.x, c.y, c.z, r * 0.98) }, uN: { value: new THREE.Vector4(n.x, n.y, n.z, type) }, uTime: sharedTime },
    vertexShader: 'varying vec3 vObj; varying vec3 vN; void main(){ vObj = position; vN = normal; gl_Position = projectionMatrix*modelViewMatrix*vec4(position,1.); }',
    fragmentShader: `uniform vec4 uC; uniform vec4 uN; uniform float uTime; varying vec3 vObj; varying vec3 vN;
      void main(){ vec3 d = vObj - uC.xyz; float dl = length(d); float s = dot(d, uN.xyz);
        if (dl > uC.w || s > -0.35 || s < -uC.w*1.3) discard;
        vec3 L = uN.w < 0.5 ? vec3(1.0,0.16,0.06) : (uN.w < 1.5 ? vec3(0.55,0.32,1.0) : (uN.w < 2.5 ? vec3(0.35,0.85,1.0) : vec3(0.85,0.72,0.55)));
        vec3 base = abs(vN.y) > 0.5 ? (gl_FrontFacing ? vec3(0.28,0.25,0.23) : vec3(0.06)) : vec3(0.13);
        float fall = 1.0 - dl/(uC.w*1.3); float fl = 1.0;
        gl_FragColor = vec4(base*(0.15 + 3.6*L*fall*fl), 1.0); }` });
  const R = r * 0.98, cell = [6, 3.4, 6];
  const back = new THREE.Mesh(new THREE.SphereGeometry(R * 0.98, 20, 12), mat.clone()); back.material.side = THREE.BackSide; back.material.uniforms = mat.uniforms; back.position.copy(c); g.add(back);
  for (let y = Math.ceil((c.y - R) / cell[1]) * cell[1]; y < c.y + R; y += cell[1]) {
    const ch = Math.sqrt(Math.max(0, R * R - (y - c.y) ** 2)); if (ch < 0.5) continue;
    const d = new THREE.CircleGeometry(ch, 20); d.rotateX(-PI / 2); d.translate(c.x, y, c.z); g.add(new THREE.Mesh(d, mat));
  }
  for (const ax of ['x', 'z']) for (let v = Math.ceil((c[ax] - R) / 6) * 6; v < c[ax] + R; v += 6) {
    const pl = new THREE.PlaneGeometry(R * 2, R * 2); if (ax === 'x') pl.rotateY(PI / 2); pl.translate(ax === 'x' ? v : c.x, c.y, ax === 'z' ? v : c.z);
    g.add(new THREE.Mesh(pl, mat));
  }
  return g;
}
const sharedTime = { value: 0 };

// One-shot effects: flashes, the shockwave ring and debris shards.
const oneshots = [];
// Weapon and explosion light (owner, 2026-11-02: "shots from the ship should have lighting, shots hitting the ship should
// also have lighting (explosions + whatever weapon was fired)"). Four lights, reused oldest first: a beam lights where it
// lands, a torpedo carries its glow, every blast flashes the hull around it, the ship's own fire lights its mounts.
const FXL = Array.from({ length: 4 }, () => { const l = new THREE.PointLight(0xffffff, 0, 100, 1.6); scene.add(l); return { l, t: 0, dur: 0, i0: 0, follow: null }; });
let fxlNext = 0;
function fxLight(pos, hex, intensity, dur, range, follow = null) {
  const L = FXL[fxlNext++ % FXL.length]; L.l.color.setHex(hex); L.l.position.copy(pos); L.l.distance = range; L.t = 0; L.dur = dur; L.i0 = intensity; L.follow = follow; L.l.intensity = intensity;
}
function stepFxLights(dt) {
  for (const L of FXL) { if (L.l.intensity <= 0) continue; L.t += dt; if (L.follow) L.follow(L.l.position);
    const k = L.t / L.dur; L.l.intensity = k >= 1 ? 0 : L.i0 * (1 - k) * (1 - k) * (0.85 + 0.15 * Math.sin(L.t * 60)); }
}
function flash(pos, size, dur) {
  fxLight(pos, 0xffb070, Math.min(8, 2 + size * 0.03), dur * 1.8, size * 2.4);
  const s = new THREE.Sprite(new THREE.SpriteMaterial({ map: navTex, color: 0xffe2b0, blending: THREE.AdditiveBlending, depthWrite: false, transparent: true }));
  s.position.copy(pos); scene.add(s); oneshots.push({ obj: s, t: 0, dur, step(o, k) { o.obj.scale.setScalar(size * (0.4 + k)); o.obj.material.opacity = (1 - k) ** 2; } });
}
function shockwave(ship, pos, size) {
  const m = new THREE.Mesh(new THREE.RingGeometry(0.992, 1, 160), new THREE.MeshBasicMaterial({ color: 0x6fb4ff, blending: THREE.AdditiveBlending, transparent: true, depthWrite: false, side: THREE.DoubleSide }));
  m.rotation.x = -PI / 2; m.position.copy(pos); scene.add(m);
  oneshots.push({ obj: m, t: 0, dur: 1.8, step(o, k) { o.obj.scale.setScalar(size * Math.sqrt(k) + 1); o.obj.material.opacity = (1 - k) ** 2 * 0.55; } });
}
const shardGeo = new THREE.TetrahedronGeometry(1, 0);
const shards = new THREE.InstancedMesh(shardGeo, new THREE.MeshStandardMaterial({ color: 0x7d7a74, metalness: 0.6, roughness: 0.6, emissive: 0x200800 }), 400);
shards.count = 0; shards.frustumCulled = false; scene.add(shards);
const shardData = [];
let shardNext = 0;
function debrisAdd(pos, n, L, spread = 0.15) {
  for (let i = 0; i < n; i++) {
    const d = { p: pos.clone().add(V3(rr(fxRng, -1, 1), rr(fxRng, -1, 1), rr(fxRng, -1, 1)).multiplyScalar(L * spread * 0.4)), v: V3(rr(fxRng, -1, 1), rr(fxRng, -0.6, 0.6), rr(fxRng, -1, 1)).normalize().multiplyScalar(rr(fxRng, 6, 40) * L / 150),
      q: new THREE.Quaternion(), w: V3(rr(fxRng, -3, 3), rr(fxRng, -3, 3), rr(fxRng, -3, 3)), s: rr(fxRng, 0.3, 1.4) * L / 150 };
    if (shardData.length < 400) shardData.push(d); else shardData[shardNext++ % 400] = d;
  }
  shards.count = shardData.length;
}
function debrisBurst(pos, L) {
  shardData.length = 0;
  for (let i = 0; i < 400; i++) shardData.push({ p: pos.clone().add(V3(rr(fxRng, -1, 1), rr(fxRng, -1, 1), rr(fxRng, -1, 1)).multiplyScalar(L * 0.15)), v: V3(rr(fxRng, -1, 1), rr(fxRng, -0.6, 0.6), rr(fxRng, -1, 1)).normalize().multiplyScalar(rr(fxRng, 8, 60) * L / 150),
    q: new THREE.Quaternion(), w: V3(rr(fxRng, -3, 3), rr(fxRng, -3, 3), rr(fxRng, -3, 3)), s: rr(fxRng, 0.3, 1.8) * L / 150 });
  shards.count = 400;
}

// Lights are on or off (owner, 2026-11-03: "flicker a couple times, then no more flickering. lights are either on or off, not
// flickering indefinitely"). When a light's state changes it flickers through these windows (seconds after the change),
// showing its old state, then holds its new one.
const FLICK = [[0.06, 0.16], [0.3, 0.37], [0.6, 0.74], [0.98, 1.06]];
const inFlick = (d) => FLICK.some(([a, b]) => d >= a && d < b);
function settle(ship, key, target) { // a light that turns on or off: flickers between old and new, then holds
  const c = ship.flk[key] || (ship.flk[key] = { v: target, prev: target, at: -9 });
  if (Math.abs(c.v - target) > 1e-3) { c.prev = c.v; c.v = target; c.at = ship.t; }
  return inFlick(ship.t - c.at) ? c.prev : target;
}
function burst(ship, key, active) { // a light that stays on but stutters a couple of times when trouble starts
  const c = ship.flk[key] || (ship.flk[key] = { on: active, at: -9 });
  if (active && !c.on) c.at = ship.t; c.on = active;
  return active && inFlick(ship.t - c.at);
}
function flick(t, seed, rate = 1) { const x = Math.sin(t * 12.9898 * rate + seed * 78.233) * 43758.5453; return x - Math.floor(x); }

const beams = [];
const beamMat = new THREE.MeshBasicMaterial({ color: 0xffb35a, blending: THREE.AdditiveBlending, transparent: true, depthWrite: false });
// One beam or torpedo, lit; used for incoming fire and the ship's own (one implementation for both directions).
function shoot(kind, from, to, onHit, scale = 1) {
  if (kind === 'phaser') {
    const L = from.distanceTo(to), g = new THREE.CylinderGeometry(0.5 * scale, 0.5 * scale, L, 6, 1, true); g.rotateX(PI / 2);
    const m = new THREE.Mesh(g, beamMat.clone()); m.position.copy(from).lerp(to, 0.5); m.lookAt(to); scene.add(m);
    beams.push({ m, t: 0, dur: 0.35, hit: null });
    fxLight(to, 0xffa040, 4, 0.45, 70 * scale); onHit && onHit();
  } else {
    const s = new THREE.Sprite(new THREE.SpriteMaterial({ map: navTex, color: 0xff6a3a, blending: THREE.AdditiveBlending, depthWrite: false, transparent: true }));
    s.scale.setScalar(8 * scale); s.position.copy(from); scene.add(s);
    fxLight(from, 0xff5530, 3, 0.5, 80 * scale, (p) => p.copy(s.position));
    beams.push({ m: s, t: 0, dur: 0.5, from, to, hit: onHit });
  }
}
function stepBeams() {
  for (let i = beams.length - 1; i >= 0; i--) {
    const b = beams[i], k = b.t / b.dur;
    if (b.from) b.m.position.copy(b.from).lerp(b.to, Math.min(1, k)); else b.m.material.opacity = 1 - k;
    if (k >= 1) { scene.remove(b.m); beams.splice(i, 1); b.hit && b.hit(); }
  }
}

// An emitter's programme (owner, 2026-11-02, on mockup 5: fire for 3 s, then smoke, then embers): one fixed step of it.
// e: { pos, dir (in its frame), phases: [{ type, rate (particles a second), dur (s), scale, light }], t, acc }; mw: the
// frame's world matrix; inherit: a velocity the particles take on (flames lag behind a drifting piece). Returns the phase
// running (for a page that lights it), or null when the programme is over.
function emitterStep(e, dt, mw, inherit = null) {
  e.t += dt;
  let acc = 0, ph = null; for (const p of e.phases) { if (e.t < acc + p.dur) { ph = p; break; } acc += p.dur; }
  if (!ph) return null;
  e.acc += ph.rate * dt; const n = Math.floor(e.acc); e.acc -= n;
  const w = e.pos.clone().applyMatrix4(mw), d = e.dir.clone().transformDirection(mw);
  if (n) emit(ph.type, w, d, n, ph.scale || 1, inherit);
  return ph;
}

// One fixed step of every shared effect: particles age, one-shot effects play out, debris tumbles, beams and torpedoes fly,
// weapon light fades (lifted from mockup 6's stepAll, in its order).
const _sm = new THREE.Matrix4(), _sq = new THREE.Quaternion(), _se = new THREE.Euler();
function stepFX(dt) {
  glowPool.step(dt); smokePool.step(dt);
  for (let i = oneshots.length - 1; i >= 0; i--) { const o = oneshots[i]; o.t += dt; const k = o.t / o.dur; if (k >= 1) { scene.remove(o.obj); oneshots.splice(i, 1); continue; } o.step(o, k); }
  for (let i = 0; i < shards.count; i++) { const d = shardData[i]; d.p.addScaledVector(d.v, dt); _sq.setFromEuler(_se.set(d.w.x * dt, d.w.y * dt, d.w.z * dt)); d.q.multiply(_sq); _sm.compose(d.p, d.q, V3(d.s, d.s, d.s)); shards.setMatrixAt(i, _sm); }
  if (shards.count) shards.instanceMatrix.needsUpdate = true;
  for (const b of beams) b.t += dt;
  stepBeams(); stepFxLights(dt);
}

// Every shared effect cleared at once (a scenario starting over).
function clearFX() {
  for (const L of FXL) L.l.intensity = 0; glowPool.clear(); smokePool.clear(); shards.count = 0;
  for (const o of oneshots) scene.remove(o.obj); oneshots.length = 0; for (const b of beams) scene.remove(b.m); beams.length = 0;
}

return { clearFX, stepFX, emitterStep, CELL_GLSL, DAMAGE_GLSL, Pool, glowPool, smokePool, FX, emit, holeInterior, sharedTime, oneshots, FXL, fxLight, stepFxLights, flash, shockwave,
  shards, shardData, debrisAdd, debrisBurst, FLICK, inFlick, settle, burst, flick, beams, beamMat, shoot, stepBeams };
};
