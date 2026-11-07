// Fleet Command mockups: a line of baked ships that take battle damage and break apart (the ship-detailing skill's baked
// ships, on lib/ship-fx.js and lib/ship-break.js). One implementation (CLAUDE.md 6.1) of what the Ssarith Hunt's page
// (mockup 12) built for itself and the Kelvane Institute's (mockup 23) now shares (kelvane-ship-concepts task 3.3, change
// kelvane-ships-engine: "perfect, implment these ships in game", owner, 2026-11-07): each ship's hull from its bake (the
// manifest's UV mesh and maps, tools/blender/painted_bake.py --ship), its two looks (Painted, PBR) with the baked hull
// lights and the shared damage rules in one shader, its windows and bays as panes (lib/baked-ship.js), its lamps as glows,
// and its hull cut into seeded cells by its signed distance field (tools/blender/hull_sdf.py) so it breaks apart. The ship
// objects are what tools/mockups/export_ships.mjs reads for the game (ship.maps.lowGeo, navs, bodies, corePos), so the game
// draws the same ships. Edit it here, then re-run tools/mockups/inline_data.py on each page that inlines it.
//
// FCBakedFleet(THREE, ctx) returns { SFX, BRK, HB, MAPS, load, build, hitShip, fireAt, repair, breakFocus, targets,
// stepDamage, hullLooks, dmgUniforms, brkUniforms, paneMesh, tessellate, sdfOf }. ctx:
//   scene, camera, U ({ lightK, selfP, none, roomK, frameK }), SK (FCShipSkin), ROOMS (FCShipRooms), BS (FCBakedShip),
//   CL ({ loftDist, chOf }), glowSprite, rand (the page's seeded fx generator), look() and night() (the page's state),
//   group(i) (the ship's group on the page), info(i) ({ cls, name, len }), onTexture(promise), normals (creased normals for
//   a ship baked shaded smooth), glowK(i) (its lamps' glow size, 1 for a warship), paneLight (GLSL, the rooms' light),
//   afterBuild(ship) (a page's own additions, such as a carrier's parked craft).
window.FCBakedFleet = function FCBakedFleet(THREE, ctx) {
'use strict';
const V3 = (x = 0, y = 0, z = 0) => new THREE.Vector3(x, y, z);
const { scene, U, SK, ROOMS, BS, glowSprite } = ctx, fxRng = ctx.rand;
const look = () => ctx.look(), night = () => ctx.night();
// The hull shader for both looks: the baked hull lights (diffuse x light^gamma x k) and the paint's own light, and the
// shared damage rules. The windows and bays are not in it: each is its own quad (paneMesh, below).
const HULL_GLSL = `
uniform sampler2D uHullLight; uniform float uLightK; uniform float uSelf;`;
const SFX = window.FCShipFX(THREE, { scene, rand: fxRng, spriteTex: glowSprite });
// The panes: every window and bay a quad of its own (lib/baked-ship.js panes), with the damage in its fragment so a hole
// or a burn takes its panes with it.
const PANE_MAIN = `float dChar = 0.0, dEmber = 0.0; int holeIdx = -1; float nz = dfbm(vObj*0.45), nz2 = dfbm(vObj*0.9 + 7.0);
        dmgHoles(vObj, nz, dChar, dEmber, holeIdx); dmgScorch(vObj, nz2, dChar, dEmber);
        if (holeIdx >= 0 || dChar > 0.5) discard;
        gl_FragColor = vec4(paneColour()*(1.0 - smoothstep(0.0, 0.5, dChar)), 1.0);`;
function paneMesh(panes, dmgU, live) {
  return BS.panes(panes, U, live, { hash: SK.GLSL.hash, box: SK.GLSL.box, rooms: ROOMS.GLSL, ...(ctx.paneLight ? { light: ctx.paneLight } : {}) }, { uniforms: dmgU, head: SFX.DAMAGE_GLSL, main: PANE_MAIN }); }
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
// Breaking apart, on the shared rules of lib/ship-break.js: the hull cut into seeded cells, every break closed by a cut
// face showing decks, bulkheads and rooms, wires stretching and snapping, fire on the broken edges. A grown hull has no loft
// stations, so its inside is its signed distance field. A torpedo on a hull at 60 % or less knocks off the piece it lands
// on; at 0 % the core breaches: the outer pieces blow off one by one, outermost first, then the main blast. Each piece
// carries the windows and lamps nearest its cell, and they stutter and die when it breaks away.
const BRK = window.FCShipBreak(THREE, { scene, rand: () => fxRng(), fx: SFX, CL: ctx.CL, env: () => (night() ? 0.06 : 0.5),
  hullMat: (pp, ship) => hullLooks(ship.maps, brkUniforms())[look() === 'pbr' ? 'pbr' : 'painted'], extras: () => [], special: () => false, onChange: () => {} });
// The break uniforms, one set per hull material (shared by its two looks): which fracture cell it draws, the cells' seeds and
// open borders (set by BRK.fracture), its cut planes and their heat, and its piece's lights.
const brkUniforms = () => ({ uCell: { value: -1 }, uSeeds: BRK.NO_SEEDS.uSeeds, uSeedN: BRK.NO_SEEDS.uSeedN, uOpen: BRK.NO_SEEDS.uOpen,
  uCuts: { value: [0, 0, 0, 0].map(() => new THREE.Vector4()) }, uCutC: { value: [0, 0, 0, 0].map(() => new THREE.Vector4()) }, uCutCount: { value: 0 }, uCutHeat: { value: 0 }, uLight: { value: 1 } });
// A hull material in both looks over one set of break uniforms; M: the ship's maps.
function hullLooks(M, u) {
  const looks = { painted: hullShader(new THREE.MeshStandardMaterial({ map: M.albedo, emissiveMap: M.em, emissive: 0xffffff, normalMap: M.nm, roughness: 0.72, metalness: 0.15, envMapIntensity: 0 }), M.lt, M.g, U.selfP, 'painted-' + M.slug, M.dU, u),
    pbr: hullShader(new THREE.MeshStandardMaterial({ map: M.base, normalMap: M.nm, roughnessMap: M.orm, metalnessMap: M.orm, aoMap: M.orm, roughness: 1, metalness: 1,
      emissiveMap: M.em, emissive: 0xffffff, envMapIntensity: night() ? 0.06 : 0.5 }), M.lt, M.g, U.none, 'pbr-' + M.slug, M.dU, u) };
  for (const m of Object.values(looks)) { m.userData.u = u; m.userData.looks = looks; }
  return looks; }
// The field as the shared breakup reads it: its bytes for the script's inside test, and the same as a 3D texture for the
// cut faces' shader.
function sdfOf(f) { const bytes = Uint8Array.from(atob(f.data), (c) => c.charCodeAt(0)), tex = new THREE.Data3DTexture(bytes, ...f.dims);
  tex.format = THREE.RedFormat; tex.type = THREE.UnsignedByteType; tex.minFilter = tex.magFilter = THREE.LinearFilter; tex.unpackAlignment = 1; tex.needsUpdate = true;
  return { lo: f.lo, hi: f.hi, dims: f.dims, scale: f.scale, bytes, tex }; }
const HB = {}, MAPS = {};
// The hull as the breakup needs it: every triangle split until no edge is longer than maxM (owner, 2026-11-05: "it created a
// big empty hole, no chunks, no fill"). The shared fracture (lib/ship-break.js subsetGeo) gives each cell the triangles that
// have a corner near it, which holds for a loft (a triangle every 2 m or so) but not for a grown hull of a few hundred big
// triangles: a cell in the middle of one got nothing, and the cells round it discard it, so it was a hole. Split at edge
// middles, position and UV together, so the maps stay where they were.
function tessellate(pos, uv, maxM) {
  const P = [], T = [], stack = [];
  for (let t = 0; t < pos.length / 9; t++) stack.push([0, 1, 2].map((k) => [pos.slice(t * 9 + k * 3, t * 9 + k * 3 + 3), uv.slice(t * 6 + k * 2, t * 6 + k * 2 + 2)]));
  const d = (a, b) => Math.hypot(a[0][0] - b[0][0], a[0][1] - b[0][1], a[0][2] - b[0][2]);
  const mid = (a, b) => [a[0].map((v, k) => (v + b[0][k]) / 2), a[1].map((v, k) => (v + b[1][k]) / 2)];
  while (stack.length) { const [a, b, c] = stack.pop(), e = [d(a, b), d(b, c), d(c, a)], m = Math.max(...e);
    if (m <= maxM) { for (const v of [a, b, c]) { P.push(...v[0]); T.push(...v[1]); } continue; }
    if (m === e[0]) { const q = mid(a, b); stack.push([a, q, c], [q, b, c]); } else if (m === e[1]) { const q = mid(b, c); stack.push([a, b, q], [a, q, c]); } else { const q = mid(c, a); stack.push([a, b, q], [q, b, c]); } }
  return { pos: P, uv: T }; }
// Normals for a ship's geometry: flat, or creased within its bake's smooth_deg (so its tangent normals lie on the normals
// they were baked against).
function setNormals(geo, deg) {
  if (deg && ctx.normals) { const p = geo.attributes.position, P = []; for (let k = 0; k < p.count; k++) P.push(V3().fromBufferAttribute(p, k));
    geo.setAttribute('normal', new THREE.BufferAttribute(ctx.normals(P, deg), 3)); } else geo.computeVertexNormals(); }
// A piece that can break away (the shared piece of lib/ship-break.js): a pivot at its middle, so it tumbles about itself.
function hbPiece(ship, key, center, sys) { const pivot = new THREE.Group(), inner = new THREE.Group(); pivot.position.copy(center); inner.position.copy(center).negate(); pivot.add(inner); ship.root.add(pivot);
  const P = { id: key, pivot, inner, center: center.clone(), vel: V3(), spin: V3(), detached: false, lightLeft: 1, mats: [], cuts: [], caps: [], sys }; ship.pieces[key] = P; return P; }
// One baked ship, whole: built at load and again by Repair.
function build(i) {
  const M = MAPS[i], sh = M.sh, G = ctx.group(i), inf = ctx.info(i);
  if (HB[i]) G.remove(HB[i].root);
  M.dU = dmgUniforms();
  const root = new THREE.Group(); root.visible = look() !== 'clay' && (ctx.shown ? ctx.shown(look()) : true); root.userData.breakRoot = true; root.userData.baked = true; G.add(root);
  // id, cls, name: what tools/mockups/export_ships.mjs names the ship's glb and manifest entry by
  const ship = { i, id: M.slug, cls: inf.cls, name: inf.name, root, maps: M, dU: M.dU, pieces: {}, frags: [], bodies: [], meshes: [], emitters: [], navs: [], mats: [], sys: {}, hull: 100, broken: false, breach: null, t: 0,
    len: inf.len, holes: 0, scorch: 0, cellLive: [] };
  const lo = V3(...sh.sdf.lo), hi = V3(...sh.sdf.hi), mid = lo.clone().add(hi).multiplyScalar(0.5);
  ship.corePos = V3(0, mid.y, lo.z + 0.42 * (hi.z - lo.z));   // the core, a little aft of the middle
  const intact = hbPiece(ship, 'hull', mid, 'life');
  const looks = hullLooks(M, brkUniforms());
  const mesh = new THREE.Mesh(M.geo, looks[look() === 'pbr' ? 'pbr' : 'painted']); mesh.userData = { ship, sys: 'life', piece: intact, skin: true }; intact.inner.add(mesh); intact.mats.push(looks.painted); ship.meshes.push(mesh); ship.hullMesh = mesh;
  if (sh.panes && sh.panes.length) { const pm = paneMesh(sh.panes, M.dU, { value: 1 }); pm.userData.skin = true; intact.inner.add(pm); }
  // the fixtures that show themselves: strobes flash, beacons pulse, position lights burn steady; each goes with its cell. A
  // flood shows only by the light it throws on the plating (owner, 2026-11-06: "I dont think we need the light source dots for
  // hul lights, just nav lights are good enough"), as in the game (ShipSkin skips flood lamps)
  const gk = ctx.glowK ? ctx.glowK(i) : 1;
  for (const F of (sh.fixtures || []).filter((F) => !F.kind.endsWith('flood'))) { const sp = new THREE.Sprite(new THREE.SpriteMaterial({ map: glowSprite, color: new THREE.Color(...F.rgb), blending: THREE.AdditiveBlending, depthWrite: false, transparent: true }));
    sp.position.set(...F.pos); const big = ({ strobe: 3.2, beacon: 3.0, position: 2.6 }[F.kind] || 1.6) * gk; sp.scale.setScalar(big); sp.userData = { kind: F.kind, big, center: sp.position.clone() }; intact.inner.add(sp); ship.navs.push({ s: sp, kind: F.kind, P: intact }); }
  // the body that breaks: the hull cut into seeded cells, more for a bigger ship
  const L = ship.len, body = { name: 'hb', sys: 'life', intact, sdf: M.sdf, geos: [[M.geo, { mode: 1 }]], n: L > 150 ? 13 : L > 120 ? 11 : 9 };
  ship.bodies.push(body); BRK.fracture(ship, body, SK.mulberry(331 + i * 17));
  // each cell carries the windows nearest its seed (the whole hull's set stays hidden with the whole hull)
  const near = (c) => { let k = 0, d = 1e9; body.seeds.forEach((q, j) => { const e = q.distanceTo(V3(...c)); if (e < d) { d = e; k = j; } }); return k; };
  for (const f of body.frags) { const live = { value: 1 }; ship.cellLive[f.cell] = live; const mine = (sh.panes || []).filter((P) => near(P.c) === f.cell); if (mine.length) f.inner.add(paneMesh(mine, M.dU, live)); }
  HB[i] = ship; if (ctx.afterBuild) ctx.afterBuild(ship); return ship; }
// A texture from a data URI; flipY on, as Blender's UV v runs up the image.
function tex2(src) { const img = new Image(), t = new THREE.Texture(img); t.anisotropy = 8;
  ctx.onTexture(new Promise((res) => { img.onload = () => { t.needsUpdate = true; res(); }; img.onerror = res; })); img.src = src; return t; }
// Every baked ship of a manifest BK: its maps, its fine geometry for the page's breakup and the bake's own low-poly hull
// (what the game draws), then built. indexOf(sh): the page's index of the ship the entry is for, or -1.
function load(BK, indexOf) {
  for (const [slug, sh] of Object.entries(BK.ships)) { const i = indexOf(slug, sh); if (i < 0) continue;
    const tex = (k, srgb) => { const pc = BK.pieces[slug + '_' + k]; // a map a ship has not been baked with yet is black
      const t = pc ? tex2(pc.data) : new THREE.DataTexture(new Uint8Array([0, 0, 0, 255]), 1, 1); if (!pc) t.needsUpdate = true; if (srgb) t.encoding = THREE.sRGBEncoding; return t; };
    const fine = tessellate(sh.mesh.pos, sh.mesh.uv, 2.5), geo = new THREE.BufferGeometry(), n = fine.pos.length / 3;
    geo.setAttribute('position', new THREE.Float32BufferAttribute(fine.pos, 3)); geo.setAttribute('uv', new THREE.Float32BufferAttribute(fine.uv, 2));
    geo.setAttribute('uv2', new THREE.Float32BufferAttribute(fine.uv, 2)); geo.setIndex(Array.from({ length: n }, (_, k) => k));
    setNormals(geo, sh.smooth_deg); geo.computeTangents();
    // the bake's own low-poly hull, as it left Blender: what the game draws (tools/mockups/export_ships.mjs); the game's breakup
    // keeps each cell's skin by its shader on a whole copy, so it needs none of the page's splitting
    const lowGeo = new THREE.BufferGeometry(); lowGeo.setAttribute('position', new THREE.Float32BufferAttribute(sh.mesh.pos, 3)); lowGeo.setAttribute('uv', new THREE.Float32BufferAttribute(sh.mesh.uv, 2));
    lowGeo.setIndex(Array.from({ length: sh.mesh.pos.length / 3 }, (_, k) => k)); setNormals(lowGeo, sh.smooth_deg);
    MAPS[i] = { sh, slug, geo, lowGeo, albedo: tex('albedo', true), base: tex('base', true), em: tex('emit', true), nm: tex('normal'), lt: tex('light'), orm: tex('orm'), g: sh.light_gamma || 1.6, sdf: sdfOf(sh.sdf) };
    build(i); } }
// A hit at pL (ship space, the hull's own geometry space) on piece P of ship: a phaser scorches with a cooling ember line,
// a torpedo blows a hole into the decks behind with fire, smoke and embers, 18 % of the hull (a phaser 5 %).
function hitShip(ship, pL, nL, big, P = null, mesh = null) {
  if (!ship || ship.broken) return; const U2 = ship.dU; mesh = mesh || ship.hullMesh; P = P || mesh.userData.piece;
  const sc = ship.scorch++ % 24; U2.uScorch.value[sc].set(pL.x, pL.y, pL.z, big ? 6 : 2.2); U2.uScorchHeat.value[sc] = big ? 1 : 0.6; U2.uScorchCount.value = Math.min(24, ship.scorch);
  ship.root.updateMatrixWorld(true); const w = pL.clone().applyMatrix4(mesh.matrixWorld), wn = nL.clone().transformDirection(mesh.matrixWorld);
  SFX.emit('sparks', w, wn, big ? 60 : 30);
  ship.hull = Math.max(0, ship.hull - (big ? 18 : 5));
  if (big && ship.holes < 24) { const h = ship.holes++, r = 2.4 + fxRng() * 1.6; U2.uHoles.value[h].set(pL.x, pL.y, pL.z, r); U2.uHoleN.value[h].set(nL.x, nL.y, nL.z, 0); U2.uHoleCount.value = ship.holes;
    SFX.emit('fireball', w, wn, 40, 0.6); SFX.flash(w, 30, 0.35);
    BRK.addEmitter(ship, P, pL.clone().add(nL.clone().multiplyScalar(0.4)), nL, [{ type: 'fire', rate: 110, dur: 3.2, scale: r / 3.5 }, { type: 'smoke', rate: 14, dur: 7, scale: r / 3.5 }, { type: 'embers', rate: 3, dur: 1e9, scale: r / 3.5 }], 'hole');
    if (ship.hull <= 60) BRK.chunkOff(ship, { mesh, point: pL }); }
  if (ship.hull <= 0) BRK.breakup(ship); }
function fireAt(ship, hit, weapon) { const inv = new THREE.Matrix4().copy(hit.object.matrixWorld).invert();
  const pL = hit.point.clone().applyMatrix4(inv), nL = hit.face.normal.clone().normalize(), mesh = hit.object, P = mesh.userData.piece;
  const from = ctx.camera.position.clone().add(V3(60, 40, 0).applyQuaternion(ctx.camera.quaternion)), big = weapon === 'torpedo';
  SFX.shoot(weapon, from, hit.point.clone(), () => hitShip(ship, pL, nL, big, P, mesh), 1); }
function repair() { SFX.clearFX(); BRK.clearWires(); for (const i of Object.keys(HB)) build(+i); }
function breakFocus(i) { const ship = HB[i]; if (ship) { ship.hull = 0; BRK.breakup(ship); } }
// The meshes a shot can land on: every piece still in the scene, the whole hull until it comes apart.
const targets = (visible) => Object.values(HB).filter((sh) => sh.root.visible && visible(sh.i)).flatMap((sh) => sh.meshes.filter((m) => { const P = m.userData.piece; return P && !P.gone && P.pivot.visible; }));
// The fixed step: the breach and the wires (shared), scorch cooling, drifting pieces tumbling and cooling at their breaks,
// their lights stuttering out, emitters carried by their pieces.
const _qq = new THREE.Quaternion(), _e = new THREE.Euler();
function stepShip(ship, dt) {
  ship.t += dt; BRK.stepBreach(ship, dt); BRK.stepWires(ship, dt);
  for (let k = 0; k < 24; k++) ship.dU.uScorchHeat.value[k] = Math.max(0, ship.dU.uScorchHeat.value[k] - dt / 20);
  ship.root.updateMatrixWorld(true);
  for (const P of Object.values(ship.pieces)) { if (!P.detached) continue;
    P.pivot.position.addScaledVector(P.vel, dt); _qq.setFromEuler(_e.set(P.spin.x * dt, P.spin.y * dt, P.spin.z * dt)); P.pivot.quaternion.multiply(_qq);
    const d = ship.t - P.dieAt; P.lightLeft = d >= 1.1 ? 0 : SFX.inFlick(d) ? 0.12 : 1; // a broken-off piece's lights stutter, then go out
    for (const m of P.mats) { m.userData.u.uCutHeat.value = Math.max(0, m.userData.u.uCutHeat.value - dt / 70); m.userData.u.uLight.value = P.lightLeft; }
    if (P.frag && ship.cellLive[P.cell]) ship.cellLive[P.cell].value = P.lightLeft; }
  ship.emitters = ship.emitters.filter((e) => { e.P.inner.updateMatrixWorld(true); const inh = e.P.detached ? e.P.vel.clone().multiplyScalar(0.3) : null;
    return SFX.emitterStep(e, dt, e.P.inner.matrixWorld, inh) !== null; }); }
function stepDamage(dt) { for (const ship of Object.values(HB)) stepShip(ship, dt); SFX.stepFX(dt); }
return { SFX, BRK, HB, MAPS, load, build, hitShip, fireAt, repair, breakFocus, targets, stepDamage, hullLooks, dmgUniforms, brkUniforms, paneMesh, tessellate, sdfOf };
};
