// Export the approved ships to glTF binary files for the game, from the mockups' own builders (CLAUDE.md 6.1: no second
// copy of any geometry rule). The Concord line is mockup 6 (docs/mockups/concord-hull-concepts.html, its buildShip() on
// docs/mockups/lib/concord-line.js) and the Krai Warhost line is mockup 7 (docs/mockups/krai-ship-concepts.html, its
// buildKrai() on data/ships/krai/*_plan.json and the shared lib/ship-plan.js, ship-skin.js, ship-break.js, ship-fx.js).
// Each page is loaded headless and builds its ships itself, intact (the Concord page's line-up shot builds every variant;
// the Krai page builds every class on load and its line-up shot puts them all in view, so each runs its fixed step and
// its lamps settle to their healthy values). Its meshes are then copied out of the scene as they stand. Nothing here
// places, sizes or shapes a part: names, merging, axes and material stand-ins are this file's only rules.
//
// Writes (CLAUDE.md 10: deterministic, so a second run gives byte-identical files):
//   assets/ships/<faction>/<id>.glb            every Concord variant but the Tern (Star Crew's yardstick), every Krai class
//   assets/ships/<faction>/detail_albedo.png   the hull shader's tri-planar detail tile: grime (red channel) and normal
//   assets/ships/<faction>/detail_normal.png   (xy), exactly the canvases the page's SK.detail() drew
//   assets/ships/manifest.json                 sizes, triangles, node names, materials, what the shaders did
//   docs/screenshots/ships/export-contact.png  every glb loaded back with GLTFLoader and rendered: look at it
//
// Conventions in every glb (assets/ships/README.md): metres; +Y up; bow toward -Z, port toward -X (the mockups' +Z bow
// and +X port turned half round Y); origin at the centre of the bounding box. Root node: the ship's id. Under it one node
// per piece of the mockup (hull, secondary, neck, command, pylon_<side>, nacelle_<side>, blade, crest, ...), placed at the
// piece's own pivot, holding its meshes merged by (zone, material) and named <piece>[_<zone>]_<material>, and the named
// empties the game drives (mount_*, engine_*, deflector, bridge, reactor, bay_*, light_*), each with its facts in extras.
// No live lights, particles, plumes, wires, damage or broken pieces in the glb: how a ship comes apart (each body's fracture
// cells, its cut faces and the parts that blow off) is written to the manifest's "break", for the game's ShipDamage.
//
//   NODE_PATH=$(npm root -g) node tools/mockups/export_ships.mjs [--no-contact]
//
// Playwright and Chromium are a global install, as for tools/mockups/shoot.mjs. GLTFExporter and GLTFLoader are three.js
// r147's, the build the pages load, vendored in tools/mockups/vendor/.
import { createRequire } from 'node:module';
import path from 'node:path';
import fs from 'node:fs';
import crypto from 'node:crypto';
import { fileURLToPath } from 'node:url';

const { chromium } = createRequire(import.meta.url)('playwright'); // global install: run with NODE_PATH=$(npm root -g)
const ROOT = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..', '..');
const VENDOR = path.join(ROOT, 'tools', 'mockups', 'vendor');
const OUT = path.join(ROOT, 'assets', 'ships');
const SHEET = path.join(ROOT, 'docs', 'screenshots', 'ships', 'export-contact.png');
const THREE_URL = 'https://cdn.jsdelivr.net/npm/three@0.147.0/build/three.min.js'; // the build both pages load
const MAX_TEX = 1024; // px on a side, for every texture written

const FACTIONS = [
  { faction: 'concord', page: 'docs/mockups/concord-hull-concepts.html', skip: ['tern'], antonio: true }, // its registry decals are painted in Antonio
  { faction: 'krai', page: 'docs/mockups/krai-ship-concepts.html', skip: [] },
  // mockup 12 (owner, 2026-11-06: "looks good, implement in game"): the Hunt's ships carry their own baked maps
  // (assets/ships/ssarith/<id>_*.png, painted_bake.py), so their glbs hold geometry, UVs and empties, and the manifest names the maps
  { faction: 'ssarith', page: 'docs/mockups/ssarith-ship-concepts.html', skip: [] },
];

// ---------------------------------------------------------------------------------------------------------------
// Runs inside a ship page, after it is ready. Builds the ships with the page's own shots, copies each out, exports it.
// Self-contained: Playwright sends this function's source to the page.
async function exportInPage(cfg) {
  const THREE = window.THREE, V3 = (x = 0, y = 0, z = 0) => new THREE.Vector3(x, y, z), PI = Math.PI;
  const fail = (msg) => { throw new Error('export_ships: ' + msg); };
  const r6 = (v) => Math.round(v * 1e6) / 1e6, v3 = (v) => [r6(v.x), r6(v.y), r6(v.z)], rgb = (c) => [r6(c.r), r6(c.g), r6(c.b)];
  // Mockup ship space (+X port, +Y up, +Z bow) to the game's (+X starboard, +Y up, -Z bow): a half turn about Y, exact.
  const R = new THREE.Matrix4().set(-1, 0, 0, 0, 0, 1, 0, 0, 0, 0, -1, 0, 0, 0, 0, 1);
  const turn = (p) => V3(-p.x, p.y, -p.z);
  const hooked = (m) => Object.prototype.hasOwnProperty.call(m, 'onBeforeCompile');
  const progKey = (m) => (hooked(m) ? String(m.customProgramCacheKey()) : '');
  // The shader a hooked material compiles to, from its own onBeforeCompile run on three's stock standard shader: the page's
  // rules are read from there, never retyped.
  const compiled = (m) => { const sh = { uniforms: {}, defines: {}, vertexShader: THREE.ShaderLib.standard.vertexShader, fragmentShader: THREE.ShaderLib.standard.fragmentShader }; m.onBeforeCompile(sh, null); return sh; };
  const vecs = (src, re, what) => { const k = src.match(re); if (!k) fail(`the page's ${what} changed: update its reader in tools/mockups/export_ships.mjs`); return k.slice(1).map((s) => s.slice(s.indexOf('(') + 1).match(/-?\d+(\.\d+)?/g).map(Number)); }; // the numbers inside vec3(...)
  const shipsInScene = () => { const out = []; window.MOCKUP_SCENE.traverse((o) => { const s = o.userData && o.userData.ship; if (s && !out.includes(s)) out.push(s); }); return out; };
  const canvasPng = (c) => { if (c.width > cfg.maxTex || c.height > cfg.maxTex) fail('a detail tile is larger than ' + cfg.maxTex + ' px'); return c.toDataURL('image/png').split(',')[1]; };

  // 1. Build: every ship of the line, intact, by the page's own shots (Concord: the hero shot first, for its lamps' make).
  let lampMake = null;
  if (cfg.faction === 'concord') {
    window.MOCKUP_SHOTS.hero();
    const hero = shipsInScene()[0], l = hero.lamps[0].l, sp = hero.spots[0];
    lampMake = { colour: rgb(l.color), hex: l.color.getHexString(), cone_half_angle_deg: r6(l.angle * 180 / PI), penumbra: l.penumbra, decay: l.decay,
      distance_per_range: r6(l.distance / sp.range), intensity_per_strength: r6(l.intensity / sp.a), shadow_casters: hero.lamps.filter((q) => q.l.castShadow).length };
  }
  if (cfg.faction !== 'ssarith') window.MOCKUP_SHOTS.lineup(); // the Concord page steps its fixed 0.2 s here; the Krai and Hunt pages step each frame, so wait for it
  const ships = shipsInScene().filter((s) => !cfg.skip.includes(s.id));
  if (!ships.length) fail('no ships found in ' + cfg.faction);
  // A healthy ship's lamps settle on their first step and hold (lib/ship-fx.js settle()), so any number of steps gives the same values.
  let frames = 0; const tick = () => { frames++; if (frames < 1e6) requestAnimationFrame(tick); }; requestAnimationFrame(tick);
  const t0 = performance.now();
  for (let i = 0; i < 1200 && !ships.every((s) => s.t > 0); i++) await new Promise((r) => setTimeout(r, 100));
  const seen = frames; frames = 1e6;
  for (const s of ships) if (!(s.t > 0)) fail(`${s.id} never ran a step (t ${s.t}, ${seen} frames in ${Math.round(performance.now() - t0)} ms, visible ${s.root.visible}), so its lamps are not at their healthy values`);

  // 2. The faction's hull shader, read from a hull material.
  const isHull = (m) => (cfg.faction === 'concord' ? progKey(m) === 'fleet-tern-hull' : cfg.faction === 'ssarith' ? /^(painted|pbr)-/.test(progKey(m)) : progKey(m).startsWith('krai-hull'));
  let hullSample = null; ships[0].root.traverse((o) => { if (!hullSample && o.isMesh && isHull(o.material)) hullSample = o.material; });
  const hullSh = compiled(hullSample), frag = hullSh.fragmentShader;
  let tileU = null, tileV = null, detA = null, detN = null;
  if (cfg.faction !== 'ssarith') {   // the Hunt has no tile: each ship its own maps
    [[tileU], [tileV]] = vecs(frag, /texture2D\(uDetA, vec2\(p\.z\/([\d.]+), p\.y\/([\d.]+)\)\)/, 'tri-planar detail tile');
    detA = hullSh.uniforms.uDetA && hullSh.uniforms.uDetA.value; detN = hullSh.uniforms.uDetN && hullSh.uniforms.uDetN.value;
    if (!detA || !detN) fail('the hull shader has no detail maps');
  }
  let paintK = null, hullShader = null;
  if (cfg.faction === 'ssarith') {
    const [gk] = vecs(frag, /pow\(texture2D\(uHullLight, vUv\)\.rgb, (vec3\([^)]*\))\)/, 'hull light map curve');
    hullShader = { source: 'docs/mockups/ssarith-ship-concepts.html: hullLooks(), hullShader() (HULL_GLSL); maps by tools/blender/painted_bake.py --ship',
      did: [
        'Each ship has its own unwrap and its own maps (assets/ships/ssarith/<id>_*.png, named in the ship\'s shader_params.maps), no tile: base colour (sRGB), ORM (occlusion, roughness, metalness in red, green, blue), a tangent-space normal map (OpenGL, +Y up), emission (sRGB: lamps, light strips), and a light map of the hull lights falling on the plating (linear).',
        'PBR look: albedo = base; roughness and metalness from ORM green and blue (roughness 1, metalness 1 scaled by them); AO from ORM red; emission = the emission map + albedo * pow(light, light_gamma) * k, k the light\'s strength (0.3 in the page\'s studio light, 4.5 at night).',
        'The glb\'s UVs are the bake\'s, v flipped for glTF (v = 0 the image\'s top row), so the PNGs map as they stand.',
        'Battle damage as the other factions\' hulls (DAMAGE_GLSL, dmgBreak): the hull shader takes the scorch, holes and fracture cells; a broken piece\'s emission and light map die with it (uLight).',
      ], constants: { light_gamma: gk[0] } };
  } else if (cfg.faction === 'concord') {
    const [armour, sub, dark] = vecs(frag, /vec3 armour = (vec3\([^)]*\)), sub = (vec3\([^)]*\)), dark = (vec3\([^)]*\))/, 'paint rule (armour, subhull, seam)');
    const [copper, hazard, black] = vecs(frag, /uMode < 3\.5 \? (vec3\([^)]*\)) : uMode < 4\.5 \? uStripe : uMode < 5\.5 \? (vec3\([^)]*\)) : (vec3\([^)]*\))/, 'paint rule (rims, accents, trim)');
    paintK = { armour, sub, dark, copper, hazard, black };
    hullShader = { source: 'docs/mockups/concord-hull-concepts.html: hullMat(), PAINT_GLSL paint(), winAt(), winRoom(), pools(); docs/mockups/lib/ship-skin.js GLSL.detail()',
      did: [
        'paint(p, n) by uMode: 0 hull, 1 plain armour, 2 nacelle shell, 3 copper rims, 4 livery accent (uStripe), 5 hazard trim, 6 near black.',
        'Hull and plain armour: armour white on top and sides, subhull grey underneath (n.y < -0.72), each 4 m by 2.6 m plate shaded +-3.5%.',
        'Hull (mode 0) also: a subhull grey service band on the sides between y = uBand.x and uBand.y with a dark line at 48% and dark seams every 8 m; grey dorsal seams 0.2 m either side of |x| = uChan; a livery stripe 2.2 m wide at each z in uStripes (uStripeN of them) in uStripe, with a 0.22 m uSeam edge each side, off the service band.',
        'Nacelle shell (mode 2): armour with a subhull grey band 0.7 m wide every 8 m along z.',
        'Detail: the paint times mix(1, grime, 0.9), grime = detail_albedo.png red channel; the normal bent by detail_normal.png xy (whiteout blend), both projected tri-planar in mockup ship space (weights |n|^6), tile_m along z and x by round y and x.',
        'Roughness 0.48 + 0.3 (1 - grime); metalness the material\'s.',
        'Windows (uWinOn, hull only): 2.2 m cells along z from uWinZ.x to uWinZ.y, one at each deck floor in uDecks (uDeckN of them) + 1.5 m, skipping each fifth cell and the livery stripes, inside the chamfers; a copper rim, black glass (roughness 0.06), and a room behind it drawn by interior mapping, lit with its life-support section.',
        'Floodlight pools (baked hull lights): emissive = paint * pools(p, n), from the light_flood_* empties (position, direction, range, strength); in the page a ship in view swaps its pools for real spot lights (lamp_make).',
      ], constants: paintK };
  } else {
    const [crimson, bronze] = vecs(frag, /vec3 crim = (vec3\([^)]*\)), brz = (vec3\([^)]*\))/, 'livery colours');
    paintK = { crimson, bronze };
    hullShader = { source: 'docs/mockups/krai-ship-concepts.html: hullMat() (KRAI_HULL_GLSL); docs/mockups/lib/ship-skin.js GLSL.detail()',
      did: [
        'Base colour: the vertex colours (field steel on the hull, plate on nacelles and blades, bronze bevels) times mix(1, grime, 0.8), grime = detail_albedo.png red channel.',
        'Seams (the darkest grime, seam = 1 - smoothstep(0.45, 0.6, grime)) tinted toward bronze * 0.45 by seam * 0.5.',
        'The normal bent by detail_normal.png xy (whiteout blend), both projected tri-planar in mockup ship space (weights |n|^6), tile_m; the fine detail fades out below about 3 px a seam (metres per pixel 0.012 to 0.05).',
        'Roughness 0.3 + 0.3 (1 - grime) + 0.15 seam, 0.55 under paint; metalness the material\'s, times (1 - 0.6 paint).',
        'Livery, painted on the side faces (|n.x| > 0.3) where |x| < uLiv2.x + 0.3 m (the hull, not the nacelles): with t = (z - uLiv.x) / (uLiv.y - uLiv.x) and hgt = uLiv.w - uLiv.z, a crimson war stripe centred at y = uLiv.z + hgt (0.28 + 0.3 sw^2), sw = smoothstep(0.62, 0.93, t), half width hgt (0.03 + 0.07 smoothstep(0.1, 0.6, t)) (1 - smoothstep(0.8, 0.94, t)), for t in 0.12 to 0.94; bronze pin lines 0.32 uLiv2.z beyond each edge of it, t in 0.14 to 0.86, 0.05 to 0.1 uLiv2.z wide; bronze battle tallies above it from t = 0.14, pitch 0.75 uLiv2.z, in fives with a gap (uLiv2.y of them), each 0.42 of the pitch wide and 1.5 uLiv2.z tall, 0.9 uLiv2.z above the stripe. Paint = colour * (0.55 + 0.45 grime) * mix(1, 0.85, seam).',
      ], constants: paintK,
      livery_uniforms: { uLiv: 'vec4(zMin, zMax, botMin, topMax): the fitted hull\'s stern and bow z, and its lowest and highest y (mockup ship space, metres)',
        uLiv2: 'vec4(hbMax, tally, max(1, 0.8 s), 0): the hull\'s widest half beam (m), the class\'s battle tallies (CLASSES[id].tally), and the paint scale, s = hull length / 87 m (the frigate\'s)' } };
  }
  const bell = (() => { let m = null; ships[0].root.traverse((o) => { if (!m && o.isMesh && progKey(o.material).startsWith('fleet-thruster')) m = o.material; }); return m; })();
  const [hotA, hotB] = bell ? vecs(compiled(bell).fragmentShader, /vec3 hot = mix\((vec3\([^)]*\)), (vec3\([^)]*\))/, 'thruster heat colours') : [null, null];   // the Hunt drives are recesses, no bells

  // 3. Materials: one export material for each distinct look, named after its role; shader-only looks get PBR stand-ins.
  const images = new Map(), imageId = (img) => { if (!images.has(img)) images.set(img, images.size); return images.get(img); };
  const texCache = new Map();
  const texFor = (t) => { const k = imageId(t.image) + ':' + t.flipY + ':' + t.wrapS + ':' + t.wrapT + ':' + t.encoding; if (texCache.has(k)) return texCache.get(k);
    const n = new THREE.Texture(t.image); n.flipY = t.flipY; n.wrapS = t.wrapS; n.wrapT = t.wrapT; n.encoding = t.encoding; n.magFilter = t.magFilter; n.minFilter = t.minFilter; n.needsUpdate = true; texCache.set(k, n); return n; };
  const MAPS = ['map', 'emissiveMap', 'roughnessMap', 'metalnessMap', 'normalMap', 'alphaMap', 'aoMap'];
  // three r147 transforms every map by the first of these the material has (its uv transform); it is baked into the uvs.
  const UV_ORDER = ['map', 'specularMap', 'displacementMap', 'normalMap', 'bumpMap', 'roughnessMap', 'metalnessMap', 'alphaMap', 'emissiveMap'];
  const uvMapOf = (m) => UV_ORDER.map((k) => m[k]).find(Boolean) || null;
  const MODES = ['hull', 'plain', 'nacelle', 'rim', 'accent', 'hazard', 'dark'];
  const matByKey = new Map(), usedNames = new Set();
  const uniq = (base) => { let n = base, i = 2; while (usedNames.has(n)) n = base + '_' + i++; usedNames.add(n); return n; };
  function materialFor(m, roleOf, ship) {
    if (Array.isArray(m)) fail('a mesh with several materials');
    if (!m.isMeshStandardMaterial) fail('a ' + m.type + ' on a ship mesh');
    const pk = progKey(m), hull = isHull(m), u = m.userData.u || {}, uv = (k) => (u[k] ? u[k].value : undefined);
    const hullU = hull && cfg.faction === 'concord' ? { uMode: uv('uMode'), uBand: uv('uBand').toArray().map(r6), uChan: uv('uChan').toArray().map(r6), uStripes: uv('uStripes').toArray().map(r6), uStripeN: uv('uStripeN'),
      uStripe: rgb(uv('uStripe')), uSeam: rgb(uv('uSeam')), uWinZ: uv('uWinZ').toArray().map(r6), uDecks: uv('uDecks').toArray().map(r6), uDeckN: uv('uDeckN'), uWinOn: uv('uWinOn'), uPane: uv('uPane') } : null;
    const texIds = MAPS.map((k) => (m[k] ? k + '=' + imageId(m[k].image) : '')).filter(Boolean);
    const key = JSON.stringify([m.type, pk, rgb(m.color), rgb(m.emissive), r6(m.emissiveIntensity), r6(m.metalness), r6(m.roughness), !!m.vertexColors, m.side, !!m.transparent, r6(m.opacity), !!m.flatShading, texIds,
      m.isMeshPhysicalMaterial ? [r6(m.clearcoat), r6(m.clearcoatRoughness)] : 0, hullU, hull && cfg.faction === 'krai' ? 'krai-hull' : 0]);
    if (matByKey.has(key)) return matByKey.get(key);
    if (hull && cfg.faction === 'ssarith') {   // the Hunt hull: a plain stand-in; its look is its own maps, named in the ship's shader_params
      const o = new THREE.MeshStandardMaterial({ color: new THREE.Color(0.36, 0.38, 0.28), metalness: 0.4, roughness: 0.6 }); o.name = uniq('hull');
      const rec = { name: o.name, hull_shader: false, baked_hull: true, keep_uv: true, livery: false, base_colour: rgb(o.color), metalness: 0.4, roughness: 0.6, emissive: [0, 0, 0], emissive_strength: 0,
        vertex_colours: false, double_sided: false, transparent: false, textures: [],
        stand_in: 'a plain khaki; the hull\'s look is its own baked maps (shader_params.maps), drawn by src/Game/View/baked_hull.gdshader' };
      const out = { key, mat: o, rec }; matByKey.set(key, out); return out;
    }
    const P = m.isMeshPhysicalMaterial ? THREE.MeshPhysicalMaterial : THREE.MeshStandardMaterial, o = new P();
    o.color.copy(m.color); o.metalness = m.metalness; o.roughness = m.roughness; o.emissive.copy(m.emissive); o.emissiveIntensity = m.emissiveIntensity;
    o.vertexColors = !!m.vertexColors; o.side = m.side; o.transparent = !!m.transparent; o.opacity = m.opacity; o.flatShading = !!m.flatShading;
    for (const k of MAPS) if (m[k]) o[k] = texFor(m[k]);
    if (m.isMeshPhysicalMaterial) { o.clearcoat = m.clearcoat; o.clearcoatRoughness = m.clearcoatRoughness; }
    const rec = { hull_shader: false, livery: false, stand_in: null };
    let name;
    if (hull && cfg.faction === 'concord') {
      const mode = Math.round(hullU.uMode); name = 'paint_' + (MODES[mode] || 'mode' + mode);
      const c = mode <= 2 ? paintK.armour : mode === 3 ? paintK.copper : mode === 4 ? hullU.uStripe : mode === 5 ? paintK.hazard : paintK.black;
      o.color.setRGB(...c);
      Object.assign(rec, { hull_shader: true, livery: (mode === 0 && hullU.uStripeN > 0) || mode === 4, tile_m: [tileU, tileV], uniforms: hullU,
        stand_in: 'base colour is the paint rule\'s ' + (mode <= 2 ? 'armour white' : mode === 4 ? 'livery stripe colour' : 'colour for this mode') + '; the paint, detail, windows and pools are the hull shader\'s (hull_shader)' });
    } else if (hull) {
      name = 'hull';
      Object.assign(rec, { hull_shader: true, livery: true, tile_m: [tileU, tileV],
        stand_in: 'the vertex colours are the plating; the detail, seams and livery are the hull shader\'s (hull_shader); the livery paints only where |x| < uLiv2.x + 0.3 m' });
    } else if (pk.startsWith('fleet-thruster')) {
      name = 'bell';
      rec.stand_in = 'the bell\'s outer shell; in the page the inside of the bell (its back faces) glows with heat, ' + JSON.stringify(hotA) + ' at the throat to ' + JSON.stringify(hotB) + ' at the lip, times (0.15 + 1.9 d^2) by depth d, with ribs and rings cut in the shell (lib/ship-skin.js thrusterMat)';
      rec.heat_rgb = { throat: hotA, lip: hotB };
      rec.uv = 'the lathe\'s own: u round the bell, v along it from the lip (0) to the throat (1); src/Game/View/bell.gdshader reads them';
    } else if (pk.startsWith('krai-window')) {
      name = 'window';
      o.color.setRGB(0.02, 0.02, 0.022); o.emissive.setRGB(1.0, 0.55, 0.22); o.emissiveIntensity = 1.2; o.roughness = 0.06; o.metalness = 0;
      rec.stand_in = 'a warm lit pane (this exporter\'s choice); in the page each pane is black glass with a Krai room behind it drawn by interior mapping (kraiRoom), forge-orange lamps or dark red, its light following its life-support section';
    } else if (hooked(m)) {
      fail('a material with an unknown shader (' + pk + ')');
    } else if (m.isMeshPhysicalMaterial) {
      name = 'blade_steel';
    } else if (m.emissiveMap) {
      name = roleOf.get(m) ? 'glow_' + roleOf.get(m) : 'lit_panel';
    } else if (m.map) {
      const img = m.map.image, rep = m.map.repeat.x !== 1 || m.map.repeat.y !== 1;
      name = rep ? 'radiator' : !m.transparent ? 'flight_deck' : img.width === img.height ? 'bay_number' : 'registry';
      if (m.transparent) rec.note = 'a transparent decal plane a few centimetres off the plating (the page also used polygonOffset)';
    } else if (m.vertexColors) {
      name = 'marking';
    } else if (roleOf.get(m)) {
      name = 'glow_' + roleOf.get(m);
    } else if (m.emissiveIntensity > 0 && (m.emissive.r + m.emissive.g + m.emissive.b) > 0) {
      name = 'glow_' + m.emissive.getHexString();
    } else {
      name = 'metal_' + m.color.getHexString();
    }
    o.name = uniq(name);
    const e = o.emissive.clone().multiplyScalar(o.emissiveIntensity), ek = Math.max(e.r, e.g, e.b);
    Object.assign(rec, { name: o.name, base_colour: rgb(o.color), metalness: r6(o.metalness), roughness: r6(o.roughness), emissive: ek > 0 ? rgb(e.clone().multiplyScalar(1 / ek)) : [0, 0, 0],
      emissive_strength: r6(ek), vertex_colours: o.vertexColors, double_sided: o.side === THREE.DoubleSide, transparent: o.transparent,
      textures: MAPS.filter((k) => o[k]).map((k) => k === 'roughnessMap' ? 'metallicRoughness (roughness from the page\'s map)' : k === 'map' ? 'baseColor' : k === 'emissiveMap' ? 'emissive' : k) });
    if (m.isMeshPhysicalMaterial) rec.clearcoat = [r6(m.clearcoat), r6(m.clearcoatRoughness)];
    const out = { key, mat: o, rec };
    matByKey.set(key, out); return out;
  }

  // 4. One ship: its pieces, meshes merged by (piece, zone, material), and its empties.
  function exportShip(ship) {
    const root = ship.root; root.updateMatrixWorld(true);
    const inv = root.matrixWorld.clone().invert(), toShip = (o) => new THREE.Matrix4().multiplyMatrices(inv, o.matrixWorld), shipPos = (o) => o.getWorldPosition(V3()).applyMatrix4(inv);
    const vis = (o) => { for (let p = o; p && p !== root; p = p.parent) if (!p.visible) return false; return true; };
    const pieces = Object.values(ship.pieces).filter((P) => !P.frag), innerOf = new Map(pieces.map((P) => [P.inner, P]));
    const pieceOf = (o) => { for (let p = o; p && p !== root; p = p.parent) if (innerOf.has(p)) return innerOf.get(p); return null; };
    const meshWith = (mat) => { let f = null; root.traverse((o) => { if (!f && o.isMesh && vis(o) && o.material === mat) f = o; }); return f; };
    const pieceOfMat = (mat) => { const o = mat && meshWith(mat); return (o && pieceOf(o)) || pieces[0]; };
    // names: the mockup's piece ids, said in words
    const SIDE = { P: 'port', S: 'starboard', D: 'dorsal', V: 'ventral' };
    const side = (k) => SIDE[k[0]] + (k.length > 1 ? '_' + k.slice(1) : '');
    const partName = (P) => {
      const id = P.id; let m;
      if (cfg.faction === 'concord') { if (id === 'hull' || id === 'neck' || id === 'wing') return id; if (id === 'sec') return 'secondary'; if (id === 'crown') return 'command';
        if ((m = id.match(/^thigh(.+)$/))) return 'pylon_' + side(m[1]); if ((m = id.match(/^shin(.+)$/))) return 'nacelle_' + side(m[1]); if ((m = id.match(/^pod(.)$/))) return 'pod_' + side(m[1]); }
      else if (cfg.faction === 'ssarith') { if (id === 'hull') return id; }
      else { if (id === 'hull' || id === 'blade' || id === 'crest') return id; if ((m = id.match(/^nac([PS])(\d+)$/))) return 'nacelle_' + side(m[1]) + (+m[2] ? '_' + (+m[2] + 1) : ''); }
      fail('a piece with no name rule: ' + id);
    };
    const ZONE = { life: 'hull', hull: 'hull', weapons: 'weapons', shields: 'deflector', impulse: 'drive', drive: 'drive', reactor: 'reactor', bridge: 'bridge' };
    const zoneName = (z) => ZONE[z] || (/^nac/.test(z) ? 'nacelle' : fail('a zone with no name: ' + z));
    // the page's own records of what each glowing material is for
    const roleOf = new Map();
    if (ship.glows) for (const [k, v] of Object.entries(ship.glows)) for (const m of (Array.isArray(v) ? v : Object.values(v).flat())) if (m && !roleOf.has(m)) roleOf.set(m, k);
    for (const n of ship.nacs || []) roleOf.set(n.grille, 'grille');
    for (const T of ship.thrusters || []) roleOf.set(T.throat, 'throat');

    const groups = new Map(), skipped = {};
    root.traverse((o) => {
      if (!o.isMesh || !vis(o)) return;
      const m = o.material;
      if (o === ship.shield || m.blending === THREE.AdditiveBlending) { skipped.additive_glow = (skipped.additive_glow || 0) + 1; return; } // plumes, halos: effects
      if (o.userData.panes) { skipped.panes = (skipped.panes || 0) + 1; return; } // the Hunt's window and bay quads: their places go out as empties
      const P = pieceOf(o); if (!P) fail('a mesh outside every piece in ' + ship.id);
      const mi = materialFor(m, roleOf, ship), part = partName(P), zone = zoneName(o.userData.sys || P.sys), zone0 = zoneName(P.sys);
      let g = new THREE.BufferGeometry(); const src = mi.rec.baked_hull ? ship.maps.lowGeo : o.geometry; // the Hunt: the bake's own low-poly hull
      for (const k of ['position', 'normal', 'color', 'uv']) if (src.attributes[k]) g.setAttribute(k, src.attributes[k].clone());
      if (src.index) g.setIndex(src.index.clone());
      if (!mi.mat.vertexColors) g.deleteAttribute('color');
      if (mi.rec.heat_rgb) { if (!g.attributes.uv) fail('a bell with no uvs'); } // a bell keeps its lathe's uvs: the game's bell shader cuts its ribs and depth from them
      else if (mi.rec.keep_uv) { const a = g.attributes.uv; if (!a) fail('a baked hull with no uvs'); for (let i = 0; i < a.count; i++) a.setY(i, 1 - a.getY(i)); } // the bake's UVs (v up), as glTF's (v down)
      else if (!MAPS.some((k) => mi.mat[k])) g.deleteAttribute('uv');
      else { const t = uvMapOf(m), a = g.attributes.uv, q = new THREE.Vector2(); if (!a) fail('a textured mesh with no uvs'); // this mesh's own map transform
        t.updateMatrix(); for (let i = 0; i < a.count; i++) { q.set(a.getX(i), a.getY(i)).applyMatrix3(t.matrix); a.setXY(i, q.x, q.y); } }
      g.applyMatrix4(new THREE.Matrix4().multiplyMatrices(R, toShip(o)));
      if (mi.mat.flatShading) { if (g.index) g = g.toNonIndexed(); g.computeVertexNormals(); } // glTF has no flat shading: faceted normals instead
      else if (!g.attributes.normal) g.computeVertexNormals();
      const key = part + '|' + zone + '|' + mi.key;
      if (!groups.has(key)) groups.set(key, { P, part, zone, zone0, mi, geos: [], sys: new Set() });
      groups.get(key).geos.push(g); groups.get(key).sys.add(o.userData.sys || P.sys);
    });
    const box = new THREE.Box3(); for (const G of groups.values()) for (const g of G.geos) { g.computeBoundingBox(); box.union(g.boundingBox); }
    const c = box.getCenter(V3()), size = box.getSize(V3());

    // the export scene: root, one node per piece at its pivot, merged meshes, empties
    const out = new THREE.Group(); out.name = ship.id;
    const partNodes = new Map();
    const pivotOf = (P) => turn(shipPos(P.pivot));
    const nodeFor = (P) => { const name = partName(P); if (partNodes.has(name)) return partNodes.get(name);
      const n = new THREE.Object3D(); n.name = name; n.position.copy(pivotOf(P)).sub(c); n.userData = { piece: P.id, system: P.sys }; out.add(n); partNodes.set(name, { n, P }); return partNodes.get(name); };
    for (const P of pieces) { let used = false; for (const G of groups.values()) if (G.P === P) used = true; if (used) nodeFor(P); }
    let tris = 0, flipped = 0, flippedSingle = 0; const meshNames = [], drawn = [];
    for (const G of groups.values()) {
      const { n, P } = nodeFor(G.P), piv = pivotOf(P);
      const g = merge(G.geos, G.mi.mat.vertexColors, MAPS.some((k) => G.mi.mat[k]) || !!G.mi.rec.heat_rgb || !!G.mi.rec.keep_uv);
      g.translate(-piv.x, -piv.y, -piv.z);
      const f = flippedTris(g); flipped += f; if (G.mi.mat.side !== THREE.DoubleSide) flippedSingle += f;
      const mesh = new THREE.Mesh(g, G.mi.mat); mesh.name = G.part + (G.zone !== G.zone0 ? '_' + G.zone : '') + '_' + G.mi.mat.name;
      mesh.userData = { piece: P.id, zones: [...G.sys].sort(), material: G.mi.mat.name }; n.add(mesh);
      const t = g.index.count / 3; tris += t; meshNames.push(mesh.name); drawn.push(G.mi.rec.name);
    }
    // empties: what the game aims from, fires from, lights and hits (CLAUDE.md 10)
    const empties = [], taken = new Set();
    const empty = (name, pShip, P, extras = {}, quat = null) => {
      let nm = name, i = 2; while (taken.has(nm)) nm = name + '_' + i++; taken.add(nm);
      const { n } = nodeFor(P || pieces[0]), piv = pivotOf(P || pieces[0]);
      const e = new THREE.Object3D(); e.name = nm; e.position.copy(turn(pShip)).sub(piv); if (quat) e.quaternion.copy(quat); e.userData = extras; n.add(e); empties.push(nm); return e; };
    const qZ = (a) => new THREE.Quaternion().setFromAxisAngle(V3(0, 0, 1), a);
    const aim = (dShip) => new THREE.Quaternion().setFromUnitVectors(V3(0, 0, -1), turn(dShip).normalize()); // the empty's -Z along the light
    const hullP = ship.pieces.hull;
    const navName = (kind, colour, pShip, P) => {
      if (kind === 'port' || kind === 'stbd') return 'light_nav_' + (kind === 'port' ? 'port' : 'starboard');
      if (kind === 'aft') return 'light_nav_aft';
      if (kind === 'strobe') return 'light_strobe_' + partName(P).replace(/^nacelle_/, '');
      if (kind === 'beacon') return 'light_beacon_' + (turn(pShip).y - c.y >= 0 ? 'dorsal' : 'ventral');
      if (colour.r > 0.9 && colour.g > 0.9 && colour.b > 0.9) return 'light_nav_aft';
      return 'light_nav_' + (pShip.x > 0 ? 'port' : 'starboard'); // the mockups put port at +X
    };
    for (const nv of ship.navs) { if (/flood/.test(nv.kind)) continue; const p = shipPos(nv.s), col = nv.s.material.color;   // the Hunt's floods are baked, below
      empty(navName(nv.kind, col, p, nv.P), p, nv.P || hullP, { kind: 'nav_' + nv.kind, colour: rgb(col), hex: col.getHexString(), behaviour: nv.kind === 'strobe' ? 'double flash every 1.4 s' : nv.kind === 'beacon' ? 'pulse, 0.25 + 0.75 max(0, sin 2.6 t)^3' : 'steady',
        // the Hunt's lamps are the page's glow sprites, drawn in the game too (change ssarith-ships-engine 2.4): their width, metres
        ...(cfg.faction === 'ssarith' && nv.s.userData.big ? { glow_m: r6(nv.s.userData.big) } : {}) }); }
    if (cfg.faction === 'concord') {
      const C = ship.C, kinds = [];
      for (const t of C.turrets) kinds.push({ kind: C.pdc ? 'point_defence' : 'turret', facing: t[2] === 'bottom' ? 'ventral' : 'dorsal' });
      for (const G2 of C.guns || []) kinds.push({ kind: 'main_gun', facing: G2.under ? 'ventral' : 'dorsal', arc: G2.face > 0 ? 'fore' : 'aft' });
      if (C.sponsons) for (const sx of [1, -1]) for (let i = 0; i < C.sponsons.cannons.length; i++) kinds.push({ kind: 'sponson', facing: sx > 0 ? 'port' : 'starboard' });
      if (kinds.length !== ship.turrets.length) fail(ship.id + ': the mount kinds do not match the builder\'s mounts');
      const turretP = pieceOfMat((ship.glows.phaser || [])[0]);
      ship.turrets.forEach((p, i) => { const k = kinds[i]; empty('mount_turret_' + i, p, turretP, { kind: k.kind, facing: k.facing, ...(k.arc ? { arc: k.arc } : {}) },
        k.facing === 'ventral' ? qZ(PI) : k.facing === 'port' ? qZ(PI / 2) : k.facing === 'starboard' ? qZ(-PI / 2) : null); });
      empty('mount_torpedo_0', V3(...C.torpedo), pieceOfMat(ship.glows.torpedo[0]), { kind: 'torpedo', arc: 'fore' });
      const impulse = ship.glows.impulse || [];
      ship.bellPos.forEach((p, i) => empty('engine_' + i, p, pieceOfMat(impulse[i]), { kind: 'impulse', radius_m: (ship.bellR || [])[i] ?? C.bells.r }));
      empty('deflector', ship.deflector, pieceOfMat(ship.glows.dish[0]), { radius_m: C.deflector[3] });
      empty('reactor', ship.corePos, pieceOfMat(ship.glows.core[0]), {});
      empty('bridge', ship.bridgeTop, ship.pieces.crown || hullP, { at: 'the top of the command block' });
      let b = 0; root.traverse((o) => { if (o.isMesh && vis(o) && o.material.emissiveMap && roleOf.get(o.material) === 'hangar') { o.geometry.computeBoundingBox(); const p = o.geometry.boundingBox.getCenter(V3()).applyMatrix4(toShip(o));
        const nrm = V3().fromBufferAttribute(o.geometry.attributes.normal, 0).transformDirection(toShip(o)); empty('bay_' + b++, p, pieceOf(o), { kind: 'hangar_door', facing: v3(turn(nrm)) }); } });
      (ship.spots || []).forEach((s, i) => empty('light_flood_' + i, s.p, hullP, { kind: 'flood', colour: lampMake.colour, hex: lampMake.hex, direction: v3(turn(s.d)), range_m: r6(s.range), strength: r6(s.a) }, aim(s.d)));
    } else if (cfg.faction === 'ssarith') {
      const sh = ship.maps.sh, lb = new THREE.Box3().setFromBufferAttribute(ship.maps.lowGeo.attributes.position);
      empty('engine_0', V3(0, (lb.min.y + lb.max.y) / 2, lb.min.z), hullP, { kind: 'drive', at: 'the drive recess in the stern' });
      empty('reactor', ship.corePos, hullP, {});
      (sh.fixtures || []).filter((F) => /flood/.test(F.kind)).forEach((F, i) => empty('light_baked_flood_' + i, V3(...F.pos), hullP,
        { kind: 'flood_baked', colour: F.rgb.map(r6), hex: new THREE.Color(...F.rgb).getHexString(), note: 'its light is baked into the hull light map; the game draws only its lamp' }));
      (sh.panes || []).forEach((P2, i) => { const n = V3(...P2.n), v = V3(...P2.v), u = V3(...P2.u);
        empty((P2.kind === 'bay' ? 'bay_' : 'window_') + i, V3(...P2.c), hullP, { kind: P2.kind === 'bay' ? 'launch_bay' : 'window', room: P2.kind, lit: P2.lit, w_m: P2.w_m, h_m: P2.h_m, frame_m: P2.frame_m,
          normal: v3(turn(n)), up: v3(turn(v)), across: v3(turn(u)) }); });   // across: a mirrored pane's own, not up x normal
    } else {
      ship.muzzles.forEach((p, i) => { let room = null, dz = Infinity; for (const r of ship.rooms) if (/^Spinal|^Bow guns/.test(r.name) && p.z >= r.b.z1 && p.z - r.b.z1 < dz) { dz = p.z - r.b.z1; room = r.name; }
        empty('mount_gun_' + i, p, hullP, { kind: 'gun_muzzle', arc: 'fore', room }); });
      ship.thrusters.forEach((T, i) => empty('engine_' + i, T.at, pieceOfMat(T.throat), { kind: 'drive', radius_m: r6(T.r) }));
      empty('reactor', ship.corePos, hullP, { room: ship.reactorRoom ? ship.reactorRoom.name : null });
      if (ship.bridgeRoom) { const b = ship.bridgeRoom.b; empty('bridge', V3((b.x0 + b.x1) / 2, (b.y0 + b.y1) / 2, (b.z0 + b.z1) / 2), hullP, { room: ship.bridgeRoom.name }); }
      ship.floods.forEach((l, i) => { const p = l.position.clone(), d = l.userData.aim.clone().sub(p).normalize();
        empty('light_flood_' + i, p, hullP, { kind: 'flood', colour: rgb(l.color), hex: l.color.getHexString(), direction: v3(turn(d)), aim_point: v3(turn(l.userData.aim).sub(c)), range_m: r6(l.distance),
          cone_half_angle_deg: r6(l.angle * 180 / PI), penumbra: l.penumbra, decay: l.decay, strength: r6(l.userData.k), casts_shadow: l.castShadow }, aim(d)); });
    }
    // How the ship comes apart (owner, 2026-11-04: "if a part gets hit enough break it appart like in the damage demo"): the
    // page's own fracture, read off the pieces it built and hid (lib/ship-break.js fracture(), cutFace()). Each body's cells
    // are its seeds; the game draws one copy of the body per cell and the hull shader keeps only that cell's skin (dmgBreak),
    // and each cut face closes a cell toward a neighbour (the cut-face shader). All in mockup ship space, as the shaders are.
    const breakup = { space: 'mockup ship space (+X port, +Y up, +Z bow, metres): the hull shader\'s v_obj', core: v3(ship.corePos), bodies: [], pieces: [] };
    for (const b of ship.bodies) {
      if (!b.seeds || !b.frags) fail(ship.id + ': body ' + b.name + ' was never fractured');
      const faces = []; let st = null;
      for (const f of b.frags) for (const cap of f.caps) {
        if (cap.userData.other === undefined) continue; // a cut face, closing cell f.cell toward cell other
        const U = compiled(cap.material).uniforms, pos = cap.geometry.attributes.position, corners = [];
        for (let i = 0; i < pos.count; i++) corners.push(v3(V3().fromBufferAttribute(pos, i)));
        if (!st) st = { stations: U.uSt.value.slice(0, U.uStN.value).map((q) => [r6(q.x), r6(q.y), r6(q.z), r6(q.w)]), ch: U.uCh.value.slice(0, U.uStN.value).map(r6), ox: r6(U.uOx.value) };
        faces.push({ cell: U.uA.value, other: U.uB.value, corners, mid: v3(U.uMid.value), across: v3(U.uAcross.value), depth: v3(U.uDepth.value) });
      }
      if (!st) fail(ship.id + ': body ' + b.name + ' has no cut faces');
      breakup.bodies.push({ node: partName(b.intact), name: b.name, system: b.sys, seeds: b.seeds.map(v3), centers: b.frags.map((f) => v3(f.center)), ...st, faces,
        ...(b.sdf ? { sdf: { file: cfg.faction + '/' + ship.id + '_sdf.bin', lo: b.sdf.lo, hi: b.sdf.hi, dims: b.sdf.dims, scale: b.sdf.scale,
          format: 'one byte a cell, x fastest then y then z: 128 + scale x metres inside the hull (tools/blender/hull_sdf.py), sampled at cell centres; the cut faces\' inside test in place of stations' } } : {}) });
    }
    const bodyPieces = new Set(ship.bodies.map((b) => b.intact));
    for (const P of pieces) if (!bodyPieces.has(P) && partNodes.has(partName(P)))
      breakup.pieces.push({ node: partName(P), system: P.sys, center: v3(P.center || shipPos(P.pivot)), cuts: (P.cuts || []).map((k) => ({ n: v3(k.n), d: r6(k.d), c: v3(k.c), r: r6(k.r) })) });
    out.updateMatrixWorld(true);
    const shaderParams = cfg.faction === 'ssarith' ? { maps: Object.fromEntries(['base', 'orm', 'normal', 'emit', 'light', 'albedo'].map((k) => [k, 'ssarith/' + ship.id + '_' + k + '.png'])),
        light_gamma: r6(ship.maps.sh.light_gamma || 1.6), light_ref: r6(ship.maps.sh.light_ref || 0.9), bake_px: ship.maps.sh.tex_px }
      : cfg.faction === 'krai' ? { uLiv: ship.dmgU.uLiv.value.toArray().map(r6), uLiv2: ship.dmgU.uLiv2.value.toArray().map(r6) }
      : { pools: (ship.spots || []).map((s) => ({ p: v3(s.p), d: v3(s.d), range_m: r6(s.range), strength: r6(s.a) })), note: 'pools in mockup ship space, as uPools / uPoolD / uPoolA (the same lamps as the light_flood_* empties)' };
    const info = cfg.faction === 'ssarith' ? { label: 'Ssarith ' + ship.cls.toLowerCase() + ': ' + ship.name, cls: ship.cls, name: ship.name }
      : cfg.faction === 'concord' ? { label: ship.C.label, cls: ship.C.cls, name: ship.C.name, registry: ship.C.reg, livery: ship.liv }
      : { label: 'Krai ' + ship.D.label.toLowerCase(), role: ship.D.role, plan: 'data/ships/krai/' + ship.id + '_plan.json' };
    return { out, man: { faction: cfg.faction, id: ship.id, ...info, file: cfg.faction + '/' + ship.id + '.glb', length_m: r6(size.z), beam_m: r6(size.x), height_m: r6(size.y), triangles: tris, draw_calls: meshNames.length,
      origin_offset_m: v3(c), nodes: { pieces: [...partNodes.keys()], meshes: meshNames, empties }, materials: [...new Set(drawn)], shader_params: shaderParams, break: breakup,
      checks: { flipped_triangles: flipped, flipped_on_single_sided: flippedSingle, skipped_meshes: skipped } } };
  }
  function merge(list, colour, uvs) {
    let nv = 0, ni = 0; for (const g of list) { nv += g.attributes.position.count; ni += g.index ? g.index.count : g.attributes.position.count; }
    const pos = new Float32Array(nv * 3), nor = new Float32Array(nv * 3), col = colour ? new Float32Array(nv * 3) : null, uv = uvs ? new Float32Array(nv * 2) : null;
    const idx = nv > 65535 ? new Uint32Array(ni) : new Uint16Array(ni); let vo = 0, io = 0;
    for (const g of list) {
      const P = g.attributes.position, N = g.attributes.normal, C = g.attributes.color, U = g.attributes.uv, n = P.count;
      for (let i = 0; i < n; i++) { const j = (vo + i) * 3; pos[j] = P.getX(i); pos[j + 1] = P.getY(i); pos[j + 2] = P.getZ(i); nor[j] = N.getX(i); nor[j + 1] = N.getY(i); nor[j + 2] = N.getZ(i);
        if (col) { col[j] = C ? C.getX(i) : 1; col[j + 1] = C ? C.getY(i) : 1; col[j + 2] = C ? C.getZ(i) : 1; }
        if (uv) { uv[(vo + i) * 2] = U ? U.getX(i) : 0; uv[(vo + i) * 2 + 1] = U ? U.getY(i) : 0; } }
      if (g.index) for (let k = 0; k < g.index.count; k++) idx[io++] = g.index.getX(k) + vo; else for (let k = 0; k < n; k++) idx[io++] = k + vo;
      vo += n;
    }
    const g = new THREE.BufferGeometry(); g.setAttribute('position', new THREE.BufferAttribute(pos, 3)); g.setAttribute('normal', new THREE.BufferAttribute(nor, 3));
    if (col) g.setAttribute('color', new THREE.BufferAttribute(col, 3)); if (uv) g.setAttribute('uv', new THREE.BufferAttribute(uv, 2)); g.setIndex(new THREE.BufferAttribute(idx, 1)); return g;
  }
  // Triangles whose winding faces against their own normals (they would vanish single-sided).
  function flippedTris(g) {
    const P = g.attributes.position, N = g.attributes.normal, I = g.index, a = V3(), b = V3(), d = V3(), n = V3(); let bad = 0;
    for (let t = 0; t < I.count; t += 3) { const i0 = I.getX(t), i1 = I.getX(t + 1), i2 = I.getX(t + 2);
      a.fromBufferAttribute(P, i1).sub(d.fromBufferAttribute(P, i0)); b.fromBufferAttribute(P, i2).sub(d); const f = a.cross(b);
      n.fromBufferAttribute(N, i0).add(d.fromBufferAttribute(N, i1)).add(d.fromBufferAttribute(N, i2)); if (f.lengthSq() > 1e-12 && f.dot(n) < 0) bad++; }
    return bad;
  }

  // 5. Export each ship (glTF binary, KHR_materials_emissive_strength where a glow is brighter than 1).
  const exporter = new THREE.GLTFExporter();
  exporter.register((writer) => ({ name: 'KHR_materials_emissive_strength', writeMaterial(material, def) {
    if (!material.emissive) return; const e = material.emissive.clone().multiplyScalar(material.emissiveIntensity), k = Math.max(e.r, e.g, e.b); if (k <= 1) return;
    def.emissiveFactor = e.multiplyScalar(1 / k).toArray(); def.extensions = def.extensions || {}; def.extensions[this.name] = { emissiveStrength: k }; writer.extensionsUsed[this.name] = true; } }));
  const b64 = (buf) => { const u = new Uint8Array(buf); let s = ''; for (let i = 0; i < u.length; i += 0x8000) s += String.fromCharCode.apply(null, u.subarray(i, i + 0x8000)); return btoa(s); };
  const result = [];
  for (const ship of ships) {
    const { out, man } = exportShip(ship);
    const glb = await new Promise((res, rej) => exporter.parse(out, res, rej, { binary: true, trs: true, onlyVisible: false, maxTextureSize: cfg.maxTex }));
    result.push({ man, glb: b64(glb) });
  }
  const mats = {}; for (const { rec } of matByKey.values()) mats[rec.name] = rec;
  return { ships: result, materials: mats, hull_shader: hullShader, lamp_make: lampMake,
    detail: detA ? { tile_m: [tileU, tileV], albedo: canvasPng(detA.image), normal: canvasPng(detN.image), size_px: [detA.image.width, detA.image.height] } : null };
}

// ---------------------------------------------------------------------------------------------------------------
// Runs in a blank page: loads each glb back with GLTFLoader and renders it three-quarter from above, port bow toward the
// viewer, all on one sheet with labels and lengths.
async function contactInPage(list) {
  const THREE = window.THREE, W = 640, H = 400, COLS = 4, rows = Math.ceil(list.length / COLS);
  const renderer = new THREE.WebGLRenderer({ antialias: true, preserveDrawingBuffer: true }); renderer.setSize(W, H); renderer.setPixelRatio(1);
  renderer.outputEncoding = THREE.sRGBEncoding; renderer.toneMapping = THREE.ACESFilmicToneMapping;
  const sheet = document.createElement('canvas'); sheet.width = W * COLS; sheet.height = H * rows; const g = sheet.getContext('2d');
  g.fillStyle = '#0b0e14'; g.fillRect(0, 0, sheet.width, sheet.height);
  const envScene = new THREE.Scene(); envScene.add(new THREE.Mesh(new THREE.SphereGeometry(50, 32, 16), new THREE.ShaderMaterial({ side: THREE.BackSide,
    vertexShader: 'varying vec3 vD; void main(){ vD = normalize(position); gl_Position = projectionMatrix*modelViewMatrix*vec4(position,1.); }',
    fragmentShader: 'varying vec3 vD; void main(){ float t = vD.y*0.5 + 0.5; gl_FragColor = vec4(mix(vec3(0.03,0.03,0.045), vec3(0.4,0.44,0.55), pow(t, 1.6)), 1.); }' })));
  const env = new THREE.PMREMGenerator(renderer).fromScene(envScene, 0.02).texture;
  const loader = new THREE.GLTFLoader();
  for (let i = 0; i < list.length; i++) {
    const it = list[i], bin = atob(it.glb), u = new Uint8Array(bin.length); for (let k = 0; k < bin.length; k++) u[k] = bin.charCodeAt(k);
    const gltf = await new Promise((res, rej) => loader.parse(u.buffer, '', res, rej));
    const scene = new THREE.Scene(); scene.background = new THREE.Color(0x141a26); scene.environment = env;
    scene.add(new THREE.HemisphereLight(0xbfcfff, 0x2a2622, 0.5));
    const sun = new THREE.DirectionalLight(0xfff1dc, 2.0); sun.position.set(-0.6, 1, -0.4); scene.add(sun);
    const rim = new THREE.DirectionalLight(0x86a6ff, 0.6); rim.position.set(1, 0.3, 0.8); scene.add(rim);
    scene.add(gltf.scene);
    // hull paint single-sided here, so a face wound inward would show as a hole (the glb keeps the page's double-sided hulls)
    gltf.scene.traverse((o) => { if (o.isMesh && /^(paint_|hull)/.test(o.material.name)) o.material.side = THREE.FrontSide; });
    const box = new THREE.Box3().setFromObject(gltf.scene), r = box.getSize(new THREE.Vector3()).length() / 2, c = box.getCenter(new THREE.Vector3());
    const cam = new THREE.PerspectiveCamera(28, W / H, Math.max(0.1, r / 50), r * 20), dir = new THREE.Vector3(-1.0, 0.75, -1.15).normalize();
    cam.position.copy(c).addScaledVector(dir, r / Math.sin(14 * Math.PI / 180) * 0.92); cam.lookAt(c);
    renderer.render(scene, cam);
    const x = (i % COLS) * W, y = Math.floor(i / COLS) * H; g.drawImage(renderer.domElement, x, y);
    g.strokeStyle = '#2b3446'; g.strokeRect(x + 0.5, y + 0.5, W - 1, H - 1);
    g.fillStyle = '#e8edf6'; g.font = '600 20px sans-serif'; g.fillText(`${it.faction}/${it.id}.glb`, x + 14, y + 28);
    g.font = '15px sans-serif'; g.fillStyle = '#aab6cc'; g.fillText(`${it.label}: ${it.length.toFixed(1)} m long, ${it.beam.toFixed(1)} m beam, ${it.tris.toLocaleString('en')} triangles`, x + 14, y + 50);
    g.fillText('bow toward -Z, viewed from port, ahead and above', x + 14, y + H - 14);
  }
  return sheet.toDataURL('image/png').split(',')[1];
}

// ---------------------------------------------------------------------------------------------------------------
const sha = (b) => crypto.createHash('sha256').update(b).digest('hex');
const browser = await chromium.launch({ args: ['--use-gl=angle', '--use-angle=swiftshader', '--enable-unsafe-swiftshader'] });
// The Concord page paints its registries in Antonio: that page's Google Fonts request is answered with assets/fonts' Antonio, so every
// run paints them the same. Only that page: under the same route the Krai page (which paints no text on its ships) never steps.
const ANTONIO_B64 = fs.readFileSync(path.join(ROOT, 'assets', 'fonts', 'Antonio-VariableFont_wght.ttf')).toString('base64');
async function openPage(rel, antonio = false) {
  const page = await browser.newPage({ viewport: { width: 1280, height: 800 } });
  const errors = [];
  page.on('pageerror', (e) => errors.push(e.message));
  page.on('console', (m) => { if (m.type() === 'error') errors.push(m.text()); });
  if (antonio) await page.route('https://fonts.googleapis.com/**', (r) => r.fulfill({ contentType: 'text/css', body: `@font-face { font-family: 'Antonio'; font-weight: 100 700; src: url(data:font/ttf;base64,${ANTONIO_B64}) format('truetype'); }` }));
  if (antonio) await page.route('https://fonts.gstatic.com/**', (r) => r.abort());
  if (rel) {
    await page.goto('file://' + path.join(ROOT, rel), { timeout: 240000 });
    await page.waitForFunction(() => window.MOCKUP_READY, null, { timeout: 240000 });
  } else {
    await page.setContent('<!DOCTYPE html><html><body style="margin:0;background:#000"></body></html>');
    await page.addScriptTag({ url: THREE_URL });
  }
  return { page, errors };
}

const manifest = { _doc: '', conventions: {}, factions: {}, materials: {}, ships: [] };
const written = [];
const write = (rel, buf) => { const p = path.join(ROOT, rel); fs.mkdirSync(path.dirname(p), { recursive: true }); fs.writeFileSync(p, buf); written.push([rel, buf.length, sha(buf)]); };
const forSheet = [];
// Where each ship came from, so a check can tell a stale export: the page file's sha256, and its published version, the
// newest "version N" in its record (docs/artifacts/<page>/README.md).
const pageSha = (rel) => sha(fs.readFileSync(path.join(ROOT, rel)));
function pageVersion(rel) {
  const rec = path.join(ROOT, 'docs', 'artifacts', path.basename(rel, '.html'), 'README.md');
  if (!fs.existsSync(rec)) return null;
  let v = null; for (const m of fs.readFileSync(rec, 'utf8').matchAll(/\bversions? (\d+)(?: to (\d+))?/g)) v = Math.max(v ?? 0, +(m[2] || m[1]));
  return v;
}
const sources = {};
// EXPORT_ONLY=krai (a comma list) exports only those factions, for working on one; the manifest is then partial, so commit a full run.
const ONLY = process.env.EXPORT_ONLY ? process.env.EXPORT_ONLY.split(',') : null;
for (const F of FACTIONS.filter((f) => !ONLY || ONLY.includes(f.faction))) {
  const src = { page: F.page, version: pageVersion(F.page), sha256: pageSha(F.page) };
  sources[F.faction] = src;
  const { page, errors } = await openPage(F.page, !!F.antonio);
  await page.addScriptTag({ path: path.join(VENDOR, 'GLTFExporter.r147.js') });
  const res = await page.evaluate(exportInPage, { faction: F.faction, skip: F.skip, maxTex: MAX_TEX }).catch((e) => { throw new Error(e.message + (errors.length ? '\npage errors:\n' + [...new Set(errors)].join('\n') : '')); });
  if (errors.length) throw new Error(F.page + ': ' + errors.join('\n'));
  await page.close();
  if (pageSha(F.page) !== src.sha256) throw new Error(F.page + ' changed during the export: run it again');
  const dir = `assets/ships/${F.faction}`;
  if (res.detail) {
    write(`${dir}/detail_albedo.png`, Buffer.from(res.detail.albedo, 'base64'));
    write(`${dir}/detail_normal.png`, Buffer.from(res.detail.normal, 'base64'));
  }
  manifest.factions[F.faction] = { source: src, ...(res.detail ? { detail: { albedo: `${F.faction}/detail_albedo.png`, normal: `${F.faction}/detail_normal.png`, size_px: res.detail.size_px, tile_m: res.detail.tile_m,
      channels: 'albedo: grime and wear in red (green and blue match); normal: tangent-space xy in red and green, 0.5 flat' } } : {}),
    hull_shader: res.hull_shader, ...(res.lamp_make ? { lamp_make: { ...res.lamp_make, note: 'how the page turns each flood pool into a SpotLight for the ship in view: distance = range * distance_per_range, intensity = strength * intensity_per_strength; the first shadow_casters cast shadows' } } : {}) };
  for (const [k, v] of Object.entries(res.materials)) manifest.materials[`${F.faction}/${k}`] = v;
  for (const s of res.ships) {
    const buf = Buffer.from(s.glb, 'base64');
    write(`assets/ships/${s.man.file}`, buf);
    for (const b of s.man.break.bodies) if (b.sdf) {   // the Hunt's fields, as baked with the ship (assets/ships/<faction>/manifest.json)
      const baked = JSON.parse(fs.readFileSync(path.join(ROOT, 'assets', 'ships', F.faction, 'manifest.json'), 'utf8')).ships[s.man.id].sdf;
      if (JSON.stringify(baked.dims) !== JSON.stringify(b.sdf.dims)) throw new Error(s.man.id + ': the page\'s field and the baked one differ: re-inline the page');
      write(`assets/ships/${b.sdf.file}`, Buffer.from(baked.data, 'base64'));
    }
    s.man.materials = s.man.materials.map((n) => `${F.faction}/${n}`);
    s.man.source = src;
    manifest.ships.push(s.man);
    forSheet.push({ faction: F.faction, id: s.man.id, label: s.man.label, length: s.man.length_m, beam: s.man.beam_m, tris: s.man.triangles, glb: s.glb });
    console.log(`${s.man.file}: ${s.man.length_m.toFixed(1)} m, ${s.man.triangles} triangles, ${s.man.draw_calls} meshes, ${buf.length} bytes; flipped ${s.man.checks.flipped_triangles} (single-sided ${s.man.checks.flipped_on_single_sided})`);
  }
}
const said = (f) => (sources[f] ? `page v${sources[f].version ?? '?'}, sha256 ${sources[f].sha256.slice(0, 12)}` : 'not in this run');
manifest._doc = `Written by tools/mockups/export_ships.mjs from the approved ship mockups (mockup 6, docs/mockups/concord-hull-concepts.html, Concord ${said('concord')}; mockup 7, docs/mockups/krai-ship-concepts.html, Krai ${said('krai')}; mockup 12, docs/mockups/ssarith-ship-concepts.html, Ssarith Hunt ${said('ssarith')}): each ship built intact by its page\'s own builder and copied out, so this is exactly the approved geometry. Do not edit; re-run the exporter, the only step needed when a mockup changes. ships[].source and factions[].source give the page file\'s sha256 and published version each ship came from: an export is stale when the page\'s sha256 differs. Colours are linear RGB, as glTF. Positions are metres in each glb\'s frame unless a key says mockup space. materials: every exported material by faction/name, with what the page\'s shader did where the glb carries a PBR stand-in. ships[].shader_params: the per-ship values the hull shader read.`;
manifest.conventions = { units: 'metres', up: '+Y', bow: '-Z', port: '-X', origin: 'the centre of the ship\'s bounding box (all three axes)',
  mockup_space: 'the pages build in +X port, +Y up, +Z bow; p_mockup = (-(x + o.x), y + o.y, -(z + o.z)) for a glb point (x, y, z), o = the ship\'s origin_offset_m; the hull shaders\' tri-planar detail and livery are in mockup space',
  nodes: 'root: the ship id; under it one node per piece of the mockup at the piece\'s pivot (hull, secondary, neck, command, wing, pod_<side>, pylon_<side>, nacelle_<side>, blade, crest); under each piece its meshes, merged by zone and material, named <piece>[_<zone>]_<material>, and its empties',
  empties: 'mount_turret_<i>, mount_torpedo_<i>, mount_gun_<i>, engine_<i>, deflector, reactor, bridge, bay_<i>, light_nav_port / starboard / aft, light_strobe_<side>, light_beacon_dorsal / ventral, light_flood_<i>; facts in glTF extras (colour linear RGB with its hex, kind, radius_m, range_m, direction in the glb frame). A mount\'s +Y is the way it faces out of the hull; a flood\'s -Z is the way it shines (as a Godot SpotLight3D).',
  not_exported: 'live lights, additive glows (thruster plumes, the Krai nacelle panels\' halos), particles, wires, damage, shields and broken pieces; the windows of the Concord line (drawn by the hull shader, see hull_shader)' };
write('assets/ships/manifest.json', Buffer.from(JSON.stringify(manifest, null, 1) + '\n'));

if (!process.argv.includes('--no-contact')) {
  const { page, errors } = await openPage(null);
  await page.addScriptTag({ path: path.join(VENDOR, 'GLTFLoader.r147.js') });
  const png = await page.evaluate(contactInPage, forSheet);
  if (errors.length) throw new Error('contact sheet: ' + errors.join('\n'));
  fs.mkdirSync(path.dirname(SHEET), { recursive: true }); fs.writeFileSync(SHEET, Buffer.from(png, 'base64'));
  console.log('contact sheet', path.relative(ROOT, SHEET));
}
await browser.close();
for (const [rel, n, h] of written) console.log(`sha256 ${h}  ${n.toString().padStart(9)}  ${rel}`);
