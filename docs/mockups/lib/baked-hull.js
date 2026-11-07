// Fleet Command mockups: a hull that carries its own baked maps, drawn as the Ssarith Hunt's page (mockup 12) draws one,
// for mockup 24 (docs/mockups/hull-texture-designs.html, the Concord's and Krai's baked designs). Lifted from the Hunt's
// page. Known duplicate (CLAUDE.md 6.1): the Hunt page still carries this code itself, and another session's
// docs/mockups/lib/baked-ship.js (change kelvane-ship-concepts, rubentipparach/fleet-command#47) does the same job; once that
// merges, mockup 24 moves onto it and this file goes (hull-texture-designs task 4.1). Edit it here, then re-run
// tools/mockups/inline_data.py on the page that inlines it.
//
// FCBakedHull(THREE, { SK, ROOMS, SFX, U, env }) returns:
//   hullLooks(M, brkU)     a hull's two looks over its maps M (albedo, base, em, nm, orm, lt, g, slug, dU): 'painted', the
//                          painted map with the painter's light in it, and 'pbr', the game's set under the live light; both
//                          carry the emission and the baked hull lights (light^g x U.lightK), and the shared damage rules
//   hullShader(...)        the shader both looks are built on (damage, the light map, the paint's own light)
//   paneMesh(panes, dmgU, live)  one mesh of quads for a ship's windows and bays: a dark frame, the room or hangar behind
//                          (lib/ship-rooms.js)
//   dmgUniforms()          a ship's damage uniforms (lib/ship-fx.js DAMAGE_GLSL's half)
//   withDetail(mat, key)   the fine detail layer laid on any standard material (the game model's own parts)
//   DETAIL                 the detail layer's tile (albedo and normal textures) and its strength (U.detK)
// SK: FCShipSkin(THREE), ROOMS: FCShipRooms, SFX: FCShipFX(THREE, ...), U: the page's shared uniforms (lightK, selfP, none,
// roomK, frameK), env(): the page's environment strength now.
window.FCBakedHull = function FCBakedHull(THREE, deps) {
'use strict';
const { SK, ROOMS, SFX, U, env } = deps;
const V3 = (x = 0, y = 0, z = 0) => new THREE.Vector3(x, y, z);
// The hull shader for both looks: the baked hull lights (diffuse x light^gamma x k) and the paint's own light, and the
// shared damage rules. The windows and bays are not in it: each is its own quad (paneMesh, below).
const HULL_GLSL = `
uniform sampler2D uHullLight; uniform float uLightK; uniform float uSelf;`;
// The fine detail layer (owner, 2026-11-07: "we're also missing detail texture layer"): a 2.5 m tile of the metal's own
// surface, below the bake's scale and with no seams of its own (CLAUDE.md section 10: tiles add detail only, never the
// main colour), drawn once from a seeded generator (lib/ship-skin.js mulberry): a slow mottle in three octaves on a
// wrapping lattice, small pits and short scratches mostly fore and aft. Its albedo shades the paint a little, its normal
// map roughens the light a little; both are projected from three sides in ship space (lib/ship-skin.js GLSL.detail).
const DETAIL_M = 2.5;
const DETAIL = (() => {
  const N = 512, rnd = SK.mulberry(9157), h = new Float32Array(N * N);
  for (const [cells, amp] of [[6, 0.45], [24, 0.3], [96, 0.18]]) {
    const L = Array.from({ length: cells * cells }, () => rnd()), at = (i, j) => L[((j % cells) * cells) + (i % cells)];
    for (let y = 0; y < N; y++) for (let x = 0; x < N; x++) {
      const u = x / N * cells, v = y / N * cells, i = Math.floor(u), j = Math.floor(v), fu = u - i, fv = v - j;
      const a = at(i, j) * (1 - fu) + at(i + 1, j) * fu, b = at(i, j + 1) * (1 - fu) + at(i + 1, j + 1) * fu;
      h[y * N + x] += amp * (a * (1 - fv) + b * fv);
    }
  }
  const dent = (cx, cy, r, d) => { for (let y = -r; y <= r; y++) for (let x = -r; x <= r; x++) { const q = (x * x + y * y) / (r * r);
    if (q < 1) { const i = ((cy + y + N) % N) * N + ((cx + x + N) % N); h[i] -= d * (1 - q); } } };
  for (let k = 0; k < 220; k++) dent(Math.floor(rnd() * N), Math.floor(rnd() * N), 1 + Math.floor(rnd() * 3), 0.35);   // pits
  for (let k = 0; k < 160; k++) {   // short scratches, mostly along u (fore and aft)
    let x = rnd() * N, y = rnd() * N; const a = (rnd() - 0.5) * 0.6, len = 8 + rnd() * 50;
    for (let t = 0; t < len; t++) { x += Math.cos(a); y += Math.sin(a); const i = ((Math.floor(y) + N) % N) * N + ((Math.floor(x) + N) % N); h[i] -= 0.22; }
  }
  let mean = 0; for (const v of h) mean += v; mean /= h.length;
  const ca = document.createElement('canvas'), cn = document.createElement('canvas'); ca.width = cn.width = ca.height = cn.height = N;
  const A = ca.getContext('2d').createImageData(N, N), Nm = cn.getContext('2d').createImageData(N, N);
  for (let y = 0; y < N; y++) for (let x = 0; x < N; x++) {
    const i = y * N + x, g = Math.max(0, Math.min(255, 128 + (h[i] - mean) * 260));
    const dx = h[y * N + (x + 1) % N] - h[y * N + (x + N - 1) % N], dy = h[((y + 1) % N) * N + x] - h[((y + N - 1) % N) * N + x];
    const nx = -dx * 3, ny = -dy * 3, l = Math.hypot(nx, ny, 1);
    A.data.set([g, g, g, 255], i * 4); Nm.data.set([(nx / l * 0.5 + 0.5) * 255, (ny / l * 0.5 + 0.5) * 255, (1 / l * 0.5 + 0.5) * 255, 255], i * 4);
  }
  ca.getContext('2d').putImageData(A, 0, 0); cn.getContext('2d').putImageData(Nm, 0, 0);
  const tex = (c) => { const t = new THREE.CanvasTexture(c); t.wrapS = t.wrapT = THREE.RepeatWrapping; t.anisotropy = 8; return t; };
  return { albedo: tex(ca), normal: tex(cn) };
})();
const DETAIL_GLSL = `
uniform sampler2D uDetA; uniform sampler2D uDetN; uniform vec2 uDetK;
varying vec3 vNObj; varying vec3 vMV0; varying vec3 vMV1; varying vec3 vMV2;
${SK.GLSL.detail(DETAIL_M, DETAIL_M)}`;
// Lays the detail layer into a material's shader (its own onBeforeCompile keeps running first).
function detailInto(s2) {
  Object.assign(s2.uniforms, { uDetA: { value: DETAIL.albedo }, uDetN: { value: DETAIL.normal }, uDetK: U.detK });
  s2.vertexShader = s2.vertexShader.replace('#include <common>', '#include <common>\nvarying vec3 vNObj; varying vec3 vMV0; varying vec3 vMV1; varying vec3 vMV2; varying vec3 vPObj;')
    .replace('#include <begin_vertex>', '#include <begin_vertex>\nvNObj = normal; vPObj = position; vMV0 = (modelViewMatrix*vec4(1.0, 0.0, 0.0, 0.0)).xyz; vMV1 = (modelViewMatrix*vec4(0.0, 1.0, 0.0, 0.0)).xyz; vMV2 = (modelViewMatrix*vec4(0.0, 0.0, 1.0, 0.0)).xyz;');
  s2.fragmentShader = s2.fragmentShader.replace('#include <common>', '#include <common>\n' + DETAIL_GLSL + '\nvarying vec3 vPObj;')
    .replace('#include <map_fragment>', `#include <map_fragment>
      diffuseColor.rgb *= 1.0 + (detailA(vPObj, normalize(vNObj)) - 0.5)*uDetK.x;`)
    .replace('#include <normal_fragment_maps>', `#include <normal_fragment_maps>
      { vec3 nO = normalize(vNObj); vec3 dO = detailN(vPObj, nO, uDetK.y) - nO;
        normal = normalize(normal + mat3(normalize(vMV0), normalize(vMV1), normalize(vMV2))*dO); }`);
}
function withDetail(mat, key) {
  const prev = mat.onBeforeCompile;
  mat.onBeforeCompile = (s2, r) => { if (prev) prev.call(mat, s2, r); detailInto(s2); };
  const pk = mat.customProgramCacheKey ? mat.customProgramCacheKey.bind(mat) : () => '';
  mat.customProgramCacheKey = () => pk() + '|detail|' + key; return mat; }
// The panes: every window and bay is a quad of its own, laid on the hull at the place the bake cut for it
// (tools/blender/hull_detail.py pane_box writes each one's centre, axes and size), so its frame is exact at any distance
// (owner, 2026-11-05: "windows and hangar bay are kinda messed up, mmake them clean, make the border clean"). The quad
// is the pane plus its frame: a bevelled rim of the hull's dark metal, lit from above, with a fine lit line round the
// glass; behind the glass, the room (roomBehind) or the hangar (hangarBehind) from docs/mockups/lib/ship-rooms.js.
const PANE_KIND = { quarters: 0, mess: 1, workshop: 2, hold: 3, bay: 4 };
const PANE_GLSL = `
uniform float uRoomK; uniform float uFrameK; uniform float uLive;
${SK.GLSL.hash}
${SK.GLSL.box}
${ROOMS.GLSL}
varying vec2 vQ; varying vec2 vHalf; varying vec3 vU; varying vec3 vV; varying vec3 vN; varying vec3 vC; varying float vKind;   // vObj, vCamObj: declared by DAMAGE_GLSL
vec3 paneColour(){
  vec3 eu = normalize(vU), ev = normalize(vV), en = normalize(vN), V = normalize(vObj - vCamObj);
  float kk = floor(vKind + 0.5); bool lit = kk >= 8.0; int k = int(lit ? kk - 8.0 : kk);   // rounded: an interpolated 8.0 can read 7.9999
  vec2 hs = vHalf, d2 = vQ - clamp(vQ, -hs, hs); float out_ = length(d2);
  vec3 L = !lit ? vec3(0.015, 0.018, 0.016) : k == 2 ? vec3(0.42, 0.95, 0.5) : k == 3 ? vec3(0.38, 0.45, 0.5) : vec3(0.78, 0.95, 0.74);
  if (out_ > 0.0) {   // the frame: a bevel rising from the glass, lit from above, a dark seam where it meets the hull
    vec2 dir = d2 / out_; vec3 nf = normalize(en*0.75 + (eu*dir.x + ev*dir.y)*0.65);
    float key = max(dot(nf, normalize(vec3(-0.25, 1.0, 0.35))), 0.0);
    vec3 col = vec3(0.105, 0.11, 0.095)*(0.35 + 0.9*key);
    return col*uFrameK;   // dark metal, no glow of its own (owner, 2026-11-06: "window borders should also not glow")
  }
  // The room or hangar stands level with the decks whatever the face's angle (owner, 2026-11-05: "Need to account for
  // normal angles on windows, and the hangar bay too"): up is the ship's up, in is the face's normal turned level, and the
  // pane cuts the room's front at a slant, its nearest corner on the room's front wall. The hangar stays painted, not
  // geometry, for its parallax (owner: "I actually like the paralax so I'd keep it").
  vec3 rv = vec3(0.0, 1.0, 0.0), ri = vec3(-en.x, 0.0, -en.z), ru, rn; float hl = length(ri);
  if (hl < 0.3) { ru = eu; rv = ev; rn = -en; } else { rn = ri/hl; ru = normalize(cross(rv, rn)); }
  vec3 Pp = eu*vQ.x + ev*vQ.y; float zext = abs(dot(eu, rn))*hs.x + abs(dot(ev, rn))*hs.y;
  vec3 D = normalize(vec3(dot(V, ru), dot(V, rv), dot(V, rn)) + vec3(1e-4)); if (D.z <= 0.0) D.z = 1e-3;
  vec3 q = vec3(dot(Pp, ru), dot(Pp, rv), dot(Pp, rn) + zext);
  vec2 mouth = 2.0*vec2(abs(dot(eu, ru))*hs.x + abs(dot(ev, ru))*hs.y, abs(dot(eu, rv))*hs.x + abs(dot(ev, rv))*hs.y);  // the mouth, level
  vec3 room = k == 4 ? hangarBehind(q, D, floor(vC*0.3), mouth, lit ? vec3(0.75, 0.8, 0.7) : L)
                     : roomBehind(q, D, floor(vC*0.6), k == 1 ? 1 : k == 2 ? 2 : k == 3 ? 1 : 0, L);
  float fres = pow(1.0 - clamp(-dot(V, en), 0.0, 1.0), 4.0);             // the glass: a faint sky in it at a glancing look
  return room*uRoomK*uLive*(1.0 - 0.5*fres) + vec3(0.05, 0.07, 0.06)*fres*uFrameK;
}`;
// One mesh of quads for a ship's panes; dmgU: the ship's damage uniforms, so a hole or a burn takes its panes with it.
function paneMesh(panes, dmgU, live) {
  const pos = [], q = [], half = [], au = [], av = [], an = [], ac = [], kind = [], idx = [];
  panes.forEach((P, j) => { const f = P.frame_m, hw = P.w_m / 2, hh = P.h_m / 2;
    const C = V3(...P.c).addScaledVector(V3(...P.n), 0.05), U_ = V3(...P.u), V_ = V3(...P.v);
    for (const [x, y] of [[-hw - f, -hh - f], [hw + f, -hh - f], [hw + f, hh + f], [-hw - f, hh + f]]) {
      const p = C.clone().addScaledVector(U_, x).addScaledVector(V_, y); pos.push(p.x, p.y, p.z); q.push(x, y); half.push(hw, hh);
      au.push(...P.u); av.push(...P.v); an.push(...P.n); ac.push(...P.c); kind.push(PANE_KIND[P.kind] + (P.lit ? 8 : 0)); }
    const b = j * 4; idx.push(b, b + 1, b + 2, b, b + 2, b + 3); });
  const g = new THREE.BufferGeometry(), A = (n, a, k) => g.setAttribute(n, new THREE.Float32BufferAttribute(a, k));
  A('position', pos, 3); A('aQ', q, 2); A('aHalf', half, 2); A('aU', au, 3); A('aV', av, 3); A('aN', an, 3); A('aC', ac, 3); A('aKind', kind, 1); g.setIndex(idx);
  const mat = new THREE.ShaderMaterial({ uniforms: { ...dmgU, uRoomK: U.roomK, uFrameK: U.frameK, uLive: live }, side: THREE.DoubleSide, polygonOffset: true, polygonOffsetFactor: -2, polygonOffsetUnits: -2,
    vertexShader: `attribute vec2 aQ; attribute vec2 aHalf; attribute vec3 aU; attribute vec3 aV; attribute vec3 aN; attribute vec3 aC; attribute float aKind;
      varying vec3 vObj; varying vec3 vCamObj; varying vec2 vQ; varying vec2 vHalf; varying vec3 vU; varying vec3 vV; varying vec3 vN; varying vec3 vC; varying float vKind;
      void main(){ vObj = position; vCamObj = (inverse(modelMatrix)*vec4(cameraPosition, 1.0)).xyz; vQ = aQ; vHalf = aHalf; vU = aU; vV = aV; vN = aN; vC = aC; vKind = aKind;
        gl_Position = projectionMatrix*modelViewMatrix*vec4(position, 1.0); }`,
    fragmentShader: SFX.DAMAGE_GLSL + PANE_GLSL + `
      void main(){ float dChar = 0.0, dEmber = 0.0; int holeIdx = -1; float nz = dfbm(vObj*0.45), nz2 = dfbm(vObj*0.9 + 7.0);
        dmgHoles(vObj, nz, dChar, dEmber, holeIdx); dmgScorch(vObj, nz2, dChar, dEmber);
        if (holeIdx >= 0 || dChar > 0.5) discard;
        gl_FragColor = vec4(paneColour()*(1.0 - smoothstep(0.0, 0.5, dChar)), 1.0); }` });
  const m = new THREE.Mesh(g, mat); m.userData.panes = true; return m; }
function hullShader(mat, lightTex, gamma, self, key, dmgU, brkU) {
  mat.onBeforeCompile = (s2) => { Object.assign(s2.uniforms, dmgU, brkU, { uHullLight: { value: lightTex }, uLightK: U.lightK, uSelf: self });
    s2.vertexShader = s2.vertexShader.replace('#include <common>', '#include <common>\nvarying vec3 vObj; varying vec3 vCamObj;')
      .replace('#include <begin_vertex>', '#include <begin_vertex>\nvObj = position; vCamObj = (inverse(modelMatrix) * vec4(cameraPosition, 1.0)).xyz;');
    s2.fragmentShader = s2.fragmentShader.replace('#include <common>', '#include <common>\n' + SFX.DAMAGE_GLSL + HULL_GLSL)
      .replace('#include <clipping_planes_fragment>', `#include <clipping_planes_fragment>
        float dChar = 0.0, dEmber = 0.0; int holeIdx = -1; float nz = dfbm(vObj*0.45), nz2 = dfbm(vObj*0.9 + 7.0);
        dmgHoles(vObj, nz, dChar, dEmber, holeIdx); dmgScorch(vObj, nz2, dChar, dEmber);
        if (dmgBreak(vObj, nz, dChar, dEmber)) discard; // a broken piece: its own cell only, its broken edges charred
        dChar = clamp(dChar, 0.0, 1.0);`)
      .replace('#include <map_fragment>', `#include <map_fragment>
        diffuseColor.rgb = mix(diffuseColor.rgb, vec3(0.03, 0.026, 0.024)*(0.6 + 0.8*nz2), smoothstep(0.0, 0.7, dChar));`)
      .replace('#include <emissivemap_fragment>', `#include <emissivemap_fragment>
        totalEmissiveRadiance *= (1.0 - smoothstep(0.0, 0.6, dChar))*uLight;   // its lamps and strips die with its piece
        totalEmissiveRadiance += diffuseColor.rgb * (pow(texture2D(uHullLight, vUv).rgb, vec3(${gamma.toFixed(2)})) * uLightK*uLight + uSelf);
        totalEmissiveRadiance += vec3(1.0, 0.36, 0.07)*dEmber*1.8*(0.75 + 0.25*sin(uTime*21.0 + nz*30.0));
        if (holeIdx >= 0) { totalEmissiveRadiance = interiorRoom(vObj, normalize(vObj - vCamObj), uHoles[holeIdx], uHoleN[holeIdx]); diffuseColor.rgb = vec3(0.0); }`); };
  mat.customProgramCacheKey = () => key; return mat; }
// Damage uniforms, one set per ship, shared by all its hull materials (the shader's half is DAMAGE_GLSL in lib/ship-fx.js).
const dmgUniforms = () => ({ uHoles: { value: Array.from({ length: 24 }, () => new THREE.Vector4()) }, uHoleN: { value: Array.from({ length: 24 }, () => new THREE.Vector4()) }, uHoleCount: { value: 0 },
  uScorch: { value: Array.from({ length: 24 }, () => new THREE.Vector4()) }, uScorchHeat: { value: new Array(24).fill(0) }, uScorchCount: { value: 0 }, uTime: SFX.sharedTime, uParallax: { value: 1 } });
// A hull material in both looks over one set of break uniforms; M: the ship's maps.
function hullLooks(M, u) {
  const looks = { painted: hullShader(new THREE.MeshStandardMaterial({ map: M.albedo, emissiveMap: M.em, emissive: 0xffffff, normalMap: M.nm, roughness: 0.72, metalness: 0.15, envMapIntensity: 0 }), M.lt, M.g, U.selfP, 'painted-' + M.slug, M.dU, u),
    pbr: hullShader(new THREE.MeshStandardMaterial({ map: M.base, normalMap: M.nm, roughnessMap: M.orm, metalnessMap: M.orm, aoMap: M.orm, roughness: 1, metalness: 1,
      emissiveMap: M.em, emissive: 0xffffff, envMapIntensity: env() }), M.lt, M.g, U.none, 'pbr-' + M.slug, M.dU, u) };
  for (const [k, m] of Object.entries(looks)) { withDetail(m, k + '-' + M.slug); m.userData.u = u; m.userData.looks = looks; }
  return looks; }
return { hullLooks, hullShader, paneMesh, dmgUniforms, PANE_KIND, withDetail, DETAIL };
};
