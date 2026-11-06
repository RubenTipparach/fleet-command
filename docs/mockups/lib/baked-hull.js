// Fleet Command mockups: a hull that carries its own baked maps, drawn the same way on every page that shows one (CLAUDE.md
// 6.1: one implementation). Lifted from the Ssarith Hunt's page (docs/mockups/ssarith-ship-concepts.html, mockup 12) so the
// Concord's and Krai's baked designs (docs/mockups/hull-texture-designs.html, mockup 23) are drawn by the same code. Edit
// it here, then re-run tools/mockups/inline_data.py on each page that inlines it.
//
// FCBakedHull(THREE, { SK, ROOMS, SFX, U, env }) returns:
//   hullLooks(M, brkU)     a hull's two looks over its maps M (albedo, base, em, nm, orm, lt, g, slug, dU): 'painted', the
//                          painted map with the painter's light in it, and 'pbr', the game's set under the live light; both
//                          carry the emission and the baked hull lights (light^g x U.lightK), and the shared damage rules
//   hullShader(...)        the shader both looks are built on (damage, the light map, the paint's own light)
//   paneMesh(panes, dmgU, live)  one mesh of quads for a ship's windows and bays: a dark frame, the room or hangar behind
//                          (lib/ship-rooms.js)
//   dmgUniforms()          a ship's damage uniforms (lib/ship-fx.js DAMAGE_GLSL's half)
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
  for (const m of Object.values(looks)) { m.userData.u = u; m.userData.looks = looks; }
  return looks; }
return { hullLooks, hullShader, paneMesh, dmgUniforms, PANE_KIND };
};
