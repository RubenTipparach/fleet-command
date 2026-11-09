// Export the faction screen for the game, from the mockup's own scene (CLAUDE.md 6.1: one copy of every shape and number).
// Mockup 1's faction screen (docs/mockups/game-flow.html, buildFactionScene) builds a walnut table and the playable factions'
// badges, each by its own builder (docs/mockups/lib/emblem-builds.js), over a brass ring let into the table, with a lamp, a
// beam of light and dust above each. The page is loaded headless and builds the scene itself; its meshes are then copied
// out as they stand. Nothing here places, sizes or shapes a part: names and material stand-ins are this file's only rules.
//
// Writes (deterministic, so a second run gives byte-identical files):
//   assets/scenes/faction/table.glb       the table top, apron, legs and the brass rings, in the scene's metres
//   assets/scenes/faction/<faction>.glb   each badge, in metres, face toward +Z, its centre at the origin, no tilt
//   assets/scenes/faction/manifest.json   the scene's numbers (TABLE, each slot's place, the lamps, the beam, the dust,
//                                         the camera) and what each stand-in material is, for src/Game/Flow/FactionStage.cs
//
//   NODE_PATH=$(npm root -g) node tools/mockups/export_faction.mjs
//
// Material stand-ins (the game draws these its own way, named in the manifest):
//   table_wood, table_under   the table_wood Material Maker maps (assets/textures/materials/table_wood/), on the glb's own
//                             uvs (one tile to TABLE.woodM metres); table_under is the same wood darkened
//   holo_field                the Concord badge's holographic field (a shader in the page): src/Game/Flow/holo_field.gdshader
//   <faction>_painted_<n>     a badge material on a map the page paints itself (the Institute's orbital): exported with its
//                             maps inside the glb, and drawn as imported
// Every other material is exported as the page has it (colours, Material Maker maps up to 1024 px, clearcoat).
import { createRequire } from 'node:module';
import path from 'node:path';
import fs from 'node:fs';
import crypto from 'node:crypto';
import { fileURLToPath } from 'node:url';

const { chromium } = createRequire(import.meta.url)('playwright');
const ROOT = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..', '..');
const VENDOR = path.join(ROOT, 'tools', 'mockups', 'vendor');
const PAGE = 'docs/mockups/game-flow.html';
const OUT = 'assets/scenes/faction';

// Runs inside the page once the faction screen is built.
async function exportInPage() {
  const THREE = window.THREE, F = window.FC_FACTION, FAC = F.FAC;
  const r6 = (v) => Math.round(v * 1e6) / 1e6, v3 = (v) => [r6(v.x), r6(v.y), r6(v.z)];
  const b64 = (buf) => { const u = new Uint8Array(buf); let s = ''; for (let i = 0; i < u.length; i += 0x8000) s += String.fromCharCode.apply(null, u.subarray(i, i + 0x8000)); return btoa(s); };
  const exporter = new THREE.GLTFExporter();
  const glb = (obj) => new Promise((res, rej) => exporter.parse(obj, (g) => res(b64(g)), rej, { binary: true, trs: true, onlyVisible: false, maxTextureSize: 1024 }));
  F.scene.updateMatrixWorld(true);

  // the table: each piece copied at its place in the scene, its look by name
  const stand = { table_wood: new THREE.MeshStandardMaterial({ name: 'table_wood', roughness: 0.3 }),
    table_under: new THREE.MeshStandardMaterial({ name: 'table_under', color: new THREE.Color(0.55, 0.5, 0.46), roughness: 0.4 }) };
  const table = new THREE.Group(); table.name = 'faction_table';
  for (const m of FAC.table) {
    const name = m.material.name;
    const mat = stand[name] || m.material;
    if (!mat.name) throw new Error('export_faction: an unnamed table material on ' + m.name);
    const c = new THREE.Mesh(m.geometry, mat); c.name = m.name;
    m.matrixWorld.decompose(c.position, c.quaternion, c.scale);
    table.add(c);
  }
  const out = { table: await glb(table), badges: {} };

  // the badges: each copied upright (the page leans it back 0.12 rad on its holder; the game does the same). A material on
  // Material Maker maps is exported plain, under a name, with its graph and settings in the manifest: the game builds it
  // from the committed maps (assets/textures/materials/<graph>/), so the maps have one copy.
  const graphOf = new Map();
  for (const src of ['docs/mockups/lib/emblem-materials.json', 'docs/mockups/lib/emblem-materials-ssarith.json'])
    for (const [k, v] of Object.entries(F.data[src].pieces)) graphOf.set(v.data, k.split('/')[0]);
  const rgb = (c) => [r6(c.r), r6(c.g), r6(c.b)];
  const mats = {};
  const named = new Map();
  for (const s of FAC.slots) {
    const g = new THREE.Group(); g.name = 'emblem_' + s.fac;
    const b = s.badge.clone(true); b.rotation.set(0, 0, 0); b.position.set(0, 0, 0); g.add(b);
    let n = 0;
    const one = (m) => {
      if (named.has(m)) return named.get(m);
      let r;
      if (m.isShaderMaterial) r = new THREE.MeshStandardMaterial({ name: 'holo_field', color: 0x0a1030, roughness: 0.5 });
      else if (m.map && m.map.isCanvasTexture) {
        // a map the badge paints itself (the Institute's orbital, from its grid): the material goes into the glb with its
        // maps, and the game keeps it as imported (it is in no badge_materials entry)
        r = new THREE.MeshStandardMaterial({ name: `${s.fac}_painted_${n++}`, map: m.map, emissive: m.emissive, emissiveMap: m.emissiveMap, emissiveIntensity: m.emissiveIntensity,
          metalness: m.metalness, roughness: m.roughness, side: m.side });
      } else {
        const name = `${s.fac}_${n++}`, graph = m.map ? graphOf.get(m.map.image.src) : null;
        if (m.map && !graph) throw new Error('export_faction: a map from no known graph on ' + s.fac);
        r = new THREE.MeshStandardMaterial({ name, color: m.color, metalness: graph ? 1 : m.metalness, roughness: graph ? 1 : m.roughness, emissive: m.emissive, emissiveIntensity: m.emissiveIntensity,
          transparent: m.transparent, opacity: m.opacity, side: m.side });
        mats[name] = { graph, tint: rgb(m.color), metalness: r6(m.metalness), roughness: r6(m.roughness), emissive: rgb(m.emissive), emissive_strength: r6(m.emissiveIntensity),
          clearcoat: r6(m.clearcoat || 0), clearcoat_roughness: r6(m.clearcoatRoughness || 0), sheen: r6(m.sheen || 0), sheen_colour: m.sheenColor ? rgb(m.sheenColor) : null,
          iridescence: r6(m.iridescence || 0), normal_scale: m.normalScale ? r6(m.normalScale.x) : 1, uv_scale: graph ? r6(m.map.repeat.x) : 1, env: r6(m.envMapIntensity ?? 1) };
      }
      named.set(m, r); return r;
    };
    // a mesh with a material per group (a slab's faces and its sides) becomes one mesh per group: three r147's exporter
    // writes every group of an unindexed geometry over all of its triangles, so two materials would fight on every face
    const split = [];
    b.traverse((o) => { if (o.isMesh && Array.isArray(o.material)) split.push(o); });
    for (const o of split) {
      const geo = o.geometry;
      if (geo.index) throw new Error('export_faction: an indexed multi-material mesh');
      for (const gr of geo.groups) {
        const part = new THREE.BufferGeometry();
        for (const [k, a] of Object.entries(geo.attributes))
          part.setAttribute(k, new THREE.BufferAttribute(a.array.slice(gr.start * a.itemSize, (gr.start + gr.count) * a.itemSize), a.itemSize, a.normalized));
        const m = new THREE.Mesh(part, o.material[gr.materialIndex]);
        m.position.copy(o.position); m.quaternion.copy(o.quaternion); m.scale.copy(o.scale);
        o.parent.add(m);
      }
      o.parent.remove(o);
    }
    b.traverse((o) => { if (o.isMesh) o.material = one(o.material); });
    out.badges[s.fac] = await glb(g);
  }
  out.materials = mats;

  // the numbers the game needs, read from the scene
  const s0 = FAC.slots[0];
  out.manifest = {
    table: { ...F.TABLE },
    slots: FAC.slots.map((s) => ({ faction: s.fac, x: r6(s.root.position.x), z: r6(s.root.position.z), badge_scale: r6(s.badge.scale.x), badge_tilt_rad: r6(s.badge.rotation.x),
      lamp: { position: v3(s.lamp.position), target: v3(s.lamp.target.position) } })),
    lamp: { colour: '#' + s0.lamp.color.getHexString(), angle_rad: r6(s0.lamp.angle), penumbra: s0.lamp.penumbra, decay: s0.lamp.decay, range_m: s0.lamp.distance,
      intensity: 'idle 0.25, plus 6.5 times its level: 1 on the picked faction, 0.55 under the pointer (stepFaction)' },
    beam: { colour: '#' + s0.cone.material.uniforms.uCol.value.getHexString(), cone_radius_m: r6(s0.cone.geometry.parameters.radius), cone_length_m: r6(s0.cone.geometry.parameters.height),
      shader: 'additive: colour * pow(|N.V|, 1.6) * pow(v, 0.7) * 0.16 * level, v running 0 at the lamp to 1 at the base (beamMat)' },
    dust: { count: s0.seed.length, size_px: '(1.6 + 3.2 glint) * 8 / depth', glint: 'pow(0.5 + 0.5 sin(t (1.5 + 3 fract(seed 7.3)) + seed 40), 10)', lit: 'inside the cone only, (0.25 + 1.6 glint) * level',
      drift_m: [0.12, 0.18, 0.12], drift_rate: [0.21, 0.13, 0.17], colour: [1.0, 0.86, 0.66] },
    turn: { yaw_per_ndc: 1.6, pitch_per_ndc: 1.0, max_yaw_rad: 0.5, max_pitch_rad: 0.3, others: 0.35, rate_per_s: 6, bob_rate: 1.1, bob_phase: 2.1 },
    camera: { position: F.view.position, target: F.view.target, fov_deg: F.cam.fov, fit: 'on a screen narrower than 16:9 the camera backs off along its line to the target until table.fitW either side of the middle fits across (fitFacCam)' },
    badge_materials: out.materials,
    materials: { table_wood: 'the table_wood maps (assets/textures/materials/table_wood/), clearcoat 0.35 at roughness 0.28', table_under: 'the same wood times (0.55, 0.5, 0.46)',
      holo_field: 'the Concord badge\'s field: src/Game/Flow/holo_field.gdshader, a port of emblem-builds.js concord() holo',
      painted: '<faction>_painted_<n>: a badge material on a map the page paints (the Institute orbital), its maps inside the glb, drawn as imported',
      badge: 'each other badge material is named in badge_materials: on a graph, its maps (assets/textures/materials/<graph>/: albedo tinted by tint, normal, ORM) at uv_scale per glb uv unit, metalness and roughness 1 times the ORM; otherwise its own colour, metalness and roughness' },
  };
  return out;
}

const browser = await chromium.launch({ args: ['--use-gl=angle', '--use-angle=swiftshader', '--enable-unsafe-swiftshader'] });
const page = await browser.newPage({ viewport: { width: 1280, height: 800 } });
const errors = [];
page.on('pageerror', (e) => errors.push(e.message));
await page.goto('file://' + path.join(ROOT, PAGE), { timeout: 240000 });
await page.waitForFunction(() => window.MOCKUP_READY, null, { timeout: 240000 });
await page.evaluate(() => window.MOCKUP_SHOTS.faction());
await page.addScriptTag({ path: path.join(VENDOR, 'GLTFExporter.r147.js') });
const res = await page.evaluate(exportInPage);
await browser.close();
if (errors.length) throw new Error(PAGE + ': ' + errors.join('\n'));

const sha = (b) => crypto.createHash('sha256').update(b).digest('hex');
const write = (rel, buf) => { const p = path.join(ROOT, rel); fs.mkdirSync(path.dirname(p), { recursive: true }); fs.writeFileSync(p, buf); console.log(sha(buf).slice(0, 12), rel, buf.length); };
write(`${OUT}/table.glb`, Buffer.from(res.table, 'base64'));
for (const [fac, g] of Object.entries(res.badges)) write(`${OUT}/${fac}.glb`, Buffer.from(g, 'base64'));
const man = { _doc: 'The faction screen, exported from mockup 1 (docs/mockups/game-flow.html, buildFactionScene) by tools/mockups/export_faction.mjs. Metres, +Y up, the camera looking toward -Z. Drawn by src/Game/Flow/FactionStage.cs.',
  source: { page: PAGE, sha256: sha(fs.readFileSync(path.join(ROOT, PAGE))) }, ...res.manifest };
write(`${OUT}/manifest.json`, Buffer.from(JSON.stringify(man, null, 1) + '\n'));
