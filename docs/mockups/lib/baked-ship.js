// Fleet Command mockups: a baked ship on a page (the ship-detailing skill, "On the page"). One implementation of how a
// ship baked by tools/blender/painted_bake.py --ship is shown: its UV mesh from the manifest, its maps, its two looks over
// one geometry (Painted: the painted map with the painter's sun in it; PBR: base colour, ORM and tangent normals under
// the live light) with its baked hull lights added (diffuse x light^gamma x k, the light map stored with a gamma curve) and
// its emission, and the fixtures that show themselves (strobes flash, beacons pulse, the rest burn steady). Used by
// docs/mockups/kelvane-ship-concepts.html (mockup 23), and for its windows (panes) by the Ssarith Hunt page (mockup 12),
// which still carries its own copy of the rest, with damage and breakup woven in; moving it wholly onto this file is task
// 3.4 of kelvane-ship-concepts (CLAUDE.md 6.1). Edit it here, then re-run tools/mockups/inline_data.py on each page that inlines it.
//
// FCBakedShip(THREE, { onTexture, normals }) returns { tex, geometry, maps, looks, fixtures, blink, panes, masts }. onTexture(promise):
// called for every texture's load, so the page can wait for them before it calls itself ready. normals(points, deg): smooth
// normals within deg (hull-grow.js creasedNormals), for a ship baked shaded smooth (its manifest's smooth_deg).
window.FCBakedShip = function FCBakedShip(THREE, opts) {
'use strict';
const { onTexture = () => {}, normals = null } = opts || {};
/** A texture from a data URI; flipY on, as Blender's UV v runs up the image. srgb: a colour map. */
function tex(src, srgb = false) {
  const img = new Image(), t = new THREE.Texture(img); t.anisotropy = 8; if (srgb) t.encoding = THREE.sRGBEncoding;
  onTexture(new Promise((res) => { img.onload = () => { t.needsUpdate = true; res(); }; img.onerror = res; })); img.src = src; return t;
}
/** The ship's UV mesh (sh.mesh: flat triangle lists in ship space), indexed, with uv2 for the AO map and tangents; shaded
 *  smooth within sh.smooth_deg when the bake was, else flat, so its normal map lies on the normals it was baked against. */
function geometry(sh) {
  const g = new THREE.BufferGeometry(), n = sh.mesh.pos.length / 3;
  g.setAttribute('position', new THREE.Float32BufferAttribute(sh.mesh.pos, 3)); g.setAttribute('uv', new THREE.Float32BufferAttribute(sh.mesh.uv, 2));
  g.setAttribute('uv2', new THREE.Float32BufferAttribute(sh.mesh.uv, 2)); g.setIndex(Array.from({ length: n }, (_, k) => k));
  if (sh.smooth_deg && normals) { const P = []; for (let k = 0; k < n; k++) P.push(new THREE.Vector3(sh.mesh.pos[3 * k], sh.mesh.pos[3 * k + 1], sh.mesh.pos[3 * k + 2]));
    g.setAttribute('normal', new THREE.BufferAttribute(normals(P, sh.smooth_deg), 3)); } else g.computeVertexNormals();
  g.computeTangents(); return g;
}
/** A ship's maps from its manifest BK (pieces <slug>_<map>, data URIs); a map not baked yet is black. */
function maps(BK, slug) {
  const sh = BK.ships[slug];
  const t = (k, srgb) => { const pc = BK.pieces[slug + '_' + k]; if (pc) return tex(pc.data, srgb);
    const d = new THREE.DataTexture(new Uint8Array([0, 0, 0, 255]), 1, 1); d.needsUpdate = true; return d; };
  return { sh, slug, geo: geometry(sh), albedo: t('albedo', true), base: t('base', true), em: t('emit', true), nm: t('normal'), lt: t('light'), orm: t('orm'), g: sh.light_gamma || 1.6 };
}
/**
 * The two looks over one geometry. U: { lightK, selfP, none } uniforms ({ value }) the page sets for its light (the
 * hull lights' strength, and how much of the painted map's own light shows as emission). patch(shader): an optional hook
 * after this file's own changes, for a page's further rules.
 */
function looks(M, U, { patch = null, env = 0.5 } = {}) {
  const hull = (mat, self, key) => { mat.onBeforeCompile = (s) => { Object.assign(s.uniforms, { uHullLight: { value: M.lt }, uLightK: U.lightK, uSelf: self });
      s.fragmentShader = s.fragmentShader.replace('#include <common>', '#include <common>\nuniform sampler2D uHullLight; uniform float uLightK; uniform float uSelf;')
        .replace('#include <emissivemap_fragment>', `#include <emissivemap_fragment>
          totalEmissiveRadiance += diffuseColor.rgb * (pow(texture2D(uHullLight, vUv).rgb, vec3(${M.g.toFixed(2)})) * uLightK + uSelf);`);
      if (patch) patch(s); };
    mat.customProgramCacheKey = () => key; return mat; };
  return {
    painted: hull(new THREE.MeshStandardMaterial({ map: M.albedo, emissiveMap: M.em, emissive: 0xffffff, normalMap: M.nm, roughness: 0.72, metalness: 0.15, envMapIntensity: 0 }), U.selfP, 'baked-painted-' + M.slug),
    pbr: hull(new THREE.MeshStandardMaterial({ map: M.base, normalMap: M.nm, roughnessMap: M.orm, metalnessMap: M.orm, aoMap: M.orm, roughness: 1, metalness: 1,
      emissiveMap: M.em, emissive: 0xffffff, envMapIntensity: env }), U.none, 'baked-pbr-' + M.slug),
  };
}
/** The fixtures that show themselves, as additive sprites at their places (sh.fixtures): [{ s, kind }]. The nav lights only:
 *  a flood shows by the light it paints on the hull, never as a dot in space (owner, 2026-11-06: "why do you still have
 *  these little light dots?"; the Hunt's page, v26: "no dots at the floods, only the nav lights"). */
function fixtures(sh, sprite, parent, k = 1) {   // k: the glows' size, to the ship's (a 14 m fighter's are a quarter of a warship's)
  return (sh.fixtures || []).filter((F) => !F.kind.endsWith('flood')).map((F) => { const s = new THREE.Sprite(new THREE.SpriteMaterial({ map: sprite, color: new THREE.Color(...F.rgb), blending: THREE.AdditiveBlending, depthWrite: false, transparent: true }));
    s.position.set(...F.pos); const big = ({ strobe: 3.2, beacon: 3.0, position: 2.6 }[F.kind] || 1.6) * k; s.scale.setScalar(big); s.userData.big = big; parent.add(s); return { s, kind: F.kind }; });
}
/** Strobes flash twice every 1.5 s, beacons pulse, the rest burn steady; brighter and larger at night. */
function blink(navs, t, night) {
  for (const { s, kind } of navs) { const p = t % 1.5;
    const on = kind === 'strobe' ? (p < 0.06 || (p > 0.2 && p < 0.26) ? 1 : 0) : kind === 'beacon' ? 0.25 + 0.75 * Math.max(0, Math.sin(t * Math.PI)) ** 3 : 0.8;
    s.material.opacity = on * (night ? 1 : 0.55); s.scale.setScalar(s.userData.big * (night ? 1 : 0.6)); }
}
// The panes: every window and bay is a quad of its own, laid on the hull at the place the bake cut for it
// (tools/blender/hull_detail.py pane_box writes each one's centre, axes and size), so its frame is exact at any distance
// (owner, 2026-11-05: "windows and hangar bay are kinda messed up, mmake them clean, make the border clean"). The quad
// is the pane plus its frame: a bevelled rim of the hull's dark metal, lit from above, with a fine lit line round the
// glass; behind the glass, the room (roomBehind) or the hangar (hangarBehind) from docs/mockups/lib/ship-rooms.js.
// Lifted from the Ssarith Hunt page (mockup 12), which now draws its panes with it, so the Institute's windows
// (owner, 2026-11-06: "also where are windows to give scale") are the Hunt's windows (CLAUDE.md 6.1).
const PANE_KIND = { quarters: 0, mess: 1, workshop: 2, hold: 3, bay: 4 };
// A lit room's light by its kind k (GLSL), the same for every ship: warm lamplight, quarters and messes yellow, workshops
// orange, holds dimmer (owner, 2026-11-06: "can we have the rooms in the windows be more yellow/orange tinted like they are
// well lit for all ships?"). GL.light overrides it, for a page that needs another.
const PANE_LIGHT = 'k == 2 ? vec3(1.0, 0.66, 0.3) : k == 3 ? vec3(0.85, 0.62, 0.36) : vec3(1.0, 0.82, 0.52)';
const BAY_LIGHT = 'vec3(1.0, 0.84, 0.58)';   // a lit hangar behind a launch bay's mouth
const paneGLSL = (GL) => `
uniform float uRoomK; uniform float uFrameK; uniform float uLive;
${GL.hash}
${GL.box}
${GL.rooms}
varying vec2 vQ; varying vec2 vHalf; varying vec3 vU; varying vec3 vV; varying vec3 vN; varying vec3 vC; varying float vKind;   // vObj, vCamObj: declared by the head
vec3 paneColour(){
  vec3 eu = normalize(vU), ev = normalize(vV), en = normalize(vN), V = normalize(vObj - vCamObj);
  float kk = floor(vKind + 0.5); bool lit = kk >= 8.0; int k = int(lit ? kk - 8.0 : kk);   // rounded: an interpolated 8.0 can read 7.9999
  vec2 hs = vHalf, d2 = vQ - clamp(vQ, -hs, hs); float out_ = length(d2);
  vec3 L = !lit ? vec3(0.015, 0.018, 0.016) : ${GL.light || PANE_LIGHT};
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
  vec3 room = k == 4 ? hangarBehind(q, D, floor(vC*0.3), mouth, lit ? ${BAY_LIGHT} : L)
                     : roomBehind(q, D, floor(vC*0.6), k == 1 ? 1 : k == 2 ? 2 : k == 3 ? 1 : 0, L);
  float fres = pow(1.0 - clamp(-dot(V, en), 0.0, 1.0), 4.0);             // the glass: a faint sky in it at a glancing look
  return room*uRoomK*uLive*(1.0 - 0.5*fres) + vec3(0.05, 0.07, 0.06)*fres*uFrameK;
}`;
/**
 * One mesh of quads for a ship's panes (sh.panes). U: the page's { roomK, frameK } uniforms (how bright a room and a frame
 * are); live: a { value } uniform, 0 when the panes' piece is lost. GL: { hash, box, rooms, light }, the GLSL of ship-skin.js
 * (GLSL.hash, GLSL.box) and ship-rooms.js (GLSL), and optionally a lit room's light by kind k (PANE_LIGHT unless given). o.uniforms: more uniforms (a page's damage); o.head: GLSL before the
 * pane's (it declares varyings vObj and vCamObj, and anything o.main calls); o.main: the fragment's body.
 */
function panes(list, U, live, GL, { uniforms = {}, head = 'varying vec3 vObj; varying vec3 vCamObj;\n', main = 'gl_FragColor = vec4(paneColour(), 1.0);' } = {}) {
  const V3 = (x, y, z) => new THREE.Vector3(x, y, z);
  const pos = [], q = [], half = [], au = [], av = [], an = [], ac = [], kind = [], idx = [];
  list.forEach((P, j) => { const f = P.frame_m, hw = P.w_m / 2, hh = P.h_m / 2;
    const C = V3(...P.c).addScaledVector(V3(...P.n), 0.05), U_ = V3(...P.u), V_ = V3(...P.v);
    for (const [x, y] of [[-hw - f, -hh - f], [hw + f, -hh - f], [hw + f, hh + f], [-hw - f, hh + f]]) {
      const p = C.clone().addScaledVector(U_, x).addScaledVector(V_, y); pos.push(p.x, p.y, p.z); q.push(x, y); half.push(hw, hh);
      au.push(...P.u); av.push(...P.v); an.push(...P.n); ac.push(...P.c); kind.push(PANE_KIND[P.kind] + (P.lit ? 8 : 0)); }
    const b = j * 4; idx.push(b, b + 1, b + 2, b, b + 2, b + 3); });
  const g = new THREE.BufferGeometry(), A = (n, a, k) => g.setAttribute(n, new THREE.Float32BufferAttribute(a, k));
  A('position', pos, 3); A('aQ', q, 2); A('aHalf', half, 2); A('aU', au, 3); A('aV', av, 3); A('aN', an, 3); A('aC', ac, 3); A('aKind', kind, 1); g.setIndex(idx);
  const mat = new THREE.ShaderMaterial({ uniforms: { ...uniforms, uRoomK: U.roomK, uFrameK: U.frameK, uLive: live }, side: THREE.DoubleSide, polygonOffset: true, polygonOffsetFactor: -2, polygonOffsetUnits: -2,
    vertexShader: `attribute vec2 aQ; attribute vec2 aHalf; attribute vec3 aU; attribute vec3 aV; attribute vec3 aN; attribute vec3 aC; attribute float aKind;
      varying vec3 vObj; varying vec3 vCamObj; varying vec2 vQ; varying vec2 vHalf; varying vec3 vU; varying vec3 vV; varying vec3 vN; varying vec3 vC; varying float vKind;
      void main(){ vObj = position; vCamObj = (inverse(modelMatrix)*vec4(cameraPosition, 1.0)).xyz; vQ = aQ; vHalf = aHalf; vU = aU; vV = aV; vN = aN; vC = aC; vKind = aKind;
        gl_Position = projectionMatrix*modelViewMatrix*vec4(position, 1.0); }`,
    fragmentShader: head + paneGLSL(GL) + `
      void main(){ ${main} }` });
  const m = new THREE.Mesh(g, mat); m.userData.panes = true; return m; }
/**
 * The antennas as geometry (sh.masts, written by tools/blender/hull_detail.py fx_antenna): a mast stands metres off the
 * hull, so it cannot be baked into the maps; only its footing is. Each is built in its own frame (u along the ship, v
 * across, w out of the hull, m: the frame as a row-major 4x4 in ship space, mirrored for a starboard one), chunky and
 * segmented after the owner's references (2026-11-11): a block footing, the mast in three stepped segments to h_m, an
 * equipment head, a spike with a red lamp, a cross arm w_m across halfway with an aerial off each end, a dish of dish_m when
 * given. mat: the metal; lamp: the tip's material. Returns one group, to add to the ship's root.
 */
function masts(sh, mat, lamp) {
  const g = new THREE.Group();
  const box = (f, sx, sy, sz, x, y, z, m = mat) => { const b = new THREE.Mesh(new THREE.BoxGeometry(sx, sy, sz), m); b.position.set(x, y, z + sz / 2); f.add(b); return b; };
  for (const A of sh.masts || []) {
    const f = new THREE.Group(), r = A.r_m, L = A.h_m, W = A.w_m;
    let z = A.base_m;
    box(f, r * 5, r * 5, r * 3, 0, 0, z); z += r * 3;                               // a block footing on the baked plate
    for (let k = 0; k < 3; k++) { const s = 1 - k * 0.22, h = L * 0.26;               // the mast in three segments, each narrower
      box(f, r * 2.2 * s, r * 2.2 * s, h, 0, 0, z); box(f, r * 3 * s, r * 3 * s, r * 0.8, 0, 0, z + h - r * 0.8); z += h; }
    const head = box(f, r * 4.5, r * 3.5, r * 5, 0, 0, z); z += r * 5;               // a head block on top, an equipment box
    box(f, r * 1.2, r * 1.2, L * 0.18, 0, 0, z);                                     // the tip spike and its red lamp
    box(f, r * 1.8, r * 1.8, r * 1.8, 0, 0, z + L * 0.18, lamp);
    if (W) { box(f, r * 1.4, W, r * 1.4, 0, 0, A.base_m + L * 0.5);                  // a cross arm halfway up, aerials hanging off it
      for (const sv of [-1, 1]) box(f, r * 0.8, r * 0.8, L * 0.28, 0, sv * W / 2, A.base_m + L * 0.5 - L * 0.28); }
    if (A.dish_m) { const d = A.dish_m, dish = new THREE.Mesh(new THREE.CylinderGeometry(d / 2, d * 0.12, d * 0.2, 20, 1, true), mat);
      dish.rotation.z = Math.PI / 2; dish.position.set(0, r * 2.2 + d * 0.1, A.base_m + L * 0.35); f.add(dish); }
    head.userData.head = true;
    f.matrixAutoUpdate = false; f.matrix.set(...A.m); g.add(f);
  }
  return g;
}
return { tex, geometry, maps, looks, fixtures, blink, panes, masts };
};
