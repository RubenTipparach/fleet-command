// Export the paper chart for the game, from mockup 1's own scene (CLAUDE.md section 6.1: one copy of every shape, drawing
// and number; openspec/changes/paper-map). The page (docs/mockups/game-flow.html) is loaded headless, a new Concord
// campaign is started on the paper look, and what it built is copied out as it stands. Nothing here draws, places or
// sizes anything: names, file layout and material stand-ins are this file's only rules.
//
// Writes (deterministic for one browser build: a second run gives the same files):
//   assets/scenes/chart/room.glb            the table, carpet, walls, trim and sconces, in map units (1 unit = 5 cm)
//   assets/scenes/chart/paper.glb           the sheet lying on the table, curled and folded, its material `chart_sheet`
//   assets/scenes/chart/sheet_base.png      the sheet's still layers, each the sheet's own pixels (MAP_LOOK.px a unit):
//   assets/scenes/chart/sheet_screen.png      base: paper, border, print, stars, every hex's number and printed sun;
//   assets/scenes/chart/sheet_over.png        screen: the ink worn pale (screen blend); over: rhumb lines, compass,
//   assets/scenes/chart/sheet_mul.png         title, scale bar, folds (normal blend); mul: frame, stains, foxing, the
//                                             browned edge (multiply blend). The game draws what changes in play (the
//                                             holders' washes, the grid, the fog) between base and the rest.
//   assets/scenes/chart/drawn/<id>.png      the bank of drawn pieces (lib/drawn.js; the page's chartBank): a cloud and a storm
//                                             in three variants for every colour of the nebula palette, three suns, twelve
//                                             rocks, the cloud's shadow. The game stands them on its own map by the page's
//                                             rules (buildMap's paper branch, ported in src/Game/Flow/ChartRoom.cs).
//   assets/scenes/chart/manifest.json       the sheet's bounds and pixels, the lights, each drawing's foot, the room's
//                                             graph materials, the planets' kinds and palettes
//
//   NODE_PATH=$(npm root -g) node tools/mockups/export_chart.mjs
import { createRequire } from 'node:module';
import path from 'node:path';
import fs from 'node:fs';
import crypto from 'node:crypto';
import { fileURLToPath } from 'node:url';

const { chromium } = createRequire(import.meta.url)('playwright');
const ROOT = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..', '..');
const VENDOR = path.join(ROOT, 'tools', 'mockups', 'vendor');
const PAGE = 'docs/mockups/game-flow.html';
const OUT = 'assets/scenes/chart';

async function exportInPage() {
  const THREE = window.THREE, C = window.FC_CHART;
  const r6 = (v) => Math.round(v * 1e6) / 1e6, v3 = (v) => [r6(v.x), r6(v.y), r6(v.z)], hex = (c) => '#' + c.getHexString();
  const png = (cv) => cv.toDataURL('image/png').split(',')[1];
  const b64 = (buf) => { const u = new Uint8Array(buf); let s = ''; for (let i = 0; i < u.length; i += 0x8000) s += String.fromCharCode.apply(null, u.subarray(i, i + 0x8000)); return btoa(s); };
  const exporter = new THREE.GLTFExporter();
  const glb = (obj) => new Promise((res, rej) => exporter.parse(obj, (g) => res(b64(g)), rej, { binary: true, trs: true, onlyVisible: false, maxTextureSize: 1024 }));
  C.mapRoot.updateMatrixWorld(true);

  // the sheet's still layers, each drawn alone
  const regions = C.nebulaRegions(), layers = {};
  for (const k of ['base', 'screen', 'over', 'mul']) layers[k] = png(C.paperSheet(regions, k).canvas);

  // the room and the sheet's mesh, each piece copied at its place
  const sheetTex = C.mapRoot.userData.sheet.tex;
  const room = new THREE.Group(); room.name = 'chart_room';
  const paper = new THREE.Group(); paper.name = 'chart_paper';
  const lights = [];
  // a material on Material Maker maps is exported plain, under a name, with its graph and tiling in the manifest: the game
  // builds it from the committed maps (assets/textures/materials/<graph>/), so the maps keep one copy and the glb stays small
  const graphOf = new Map();
  for (const src of ['docs/mockups/lib/room-materials.json', 'docs/mockups/lib/table-materials.json', 'docs/mockups/lib/emblem-materials.json'])
    for (const [k, v] of Object.entries(C.DATA[src]?.pieces || {})) graphOf.set(v.data, k.split('/')[0]);
  const rgb = (c) => [r6(c.r), r6(c.g), r6(c.b)], mats = {}, stand = new Map();
  const standIn = (m) => {
    if (stand.has(m)) return stand.get(m);
    let r = m;
    if (m.map && graphOf.has(m.map.image?.src)) {
      const graph = graphOf.get(m.map.image.src);
      const spec = { graph, repeat: [r6(m.map.repeat.x), r6(m.map.repeat.y)], tint: rgb(m.color), roughness: r6(m.roughness), metalness: r6(m.metalness),
        normal_scale: m.normalScale ? r6(m.normalScale.x) : 1, clearcoat: r6(m.clearcoat || 0), env: r6(m.envMapIntensity ?? 1) };
      const key = JSON.stringify(spec);
      let name = Object.keys(mats).find((n) => JSON.stringify(mats[n]) === key);
      if (!name) { name = `${graph}_${Object.keys(mats).filter((n) => n.startsWith(graph + '_')).length}`; mats[name] = spec; }
      r = new THREE.MeshStandardMaterial({ name, color: m.color, roughness: m.roughness, metalness: m.metalness });
    } else if (m.map) throw new Error('export_chart: a room map from no known graph');
    stand.set(m, r); return r;
  };
  C.mapRoot.traverse((o) => {
    if (!o.userData.room) return;
    if (o.isLight) {
      const l = { kind: o.isSpotLight ? 'spot' : o.isPointLight ? 'point' : 'other', colour: hex(o.color), intensity: r6(o.intensity), position: v3(o.getWorldPosition(new THREE.Vector3())) };
      if (o.isSpotLight) Object.assign(l, { angle_rad: r6(o.angle), penumbra: r6(o.penumbra), range: r6(o.distance), decay: r6(o.decay), target: v3(o.target.getWorldPosition(new THREE.Vector3())), shadow: o.castShadow });
      if (o.isPointLight) Object.assign(l, { range: r6(o.distance), decay: r6(o.decay) });
      lights.push(l);
      return;
    }
    if (!o.isMesh) return;
    const isSheet = o.material && o.material.map === sheetTex;
    const c = new THREE.Mesh(o.geometry, isSheet ? new THREE.MeshStandardMaterial({ name: 'chart_sheet' }) : standIn(o.material));
    c.name = o.name || (isSheet ? 'chart_sheet' : 'room_piece');
    o.matrixWorld.decompose(c.position, c.quaternion, c.scale);
    (isSheet ? paper : room).add(c);
  });
  const out = { layers, room: await glb(room), paper: await glb(paper), drawn: {}, materials: mats };

  // the drawn pieces: the whole bank, each drawing once
  C.chartBank();
  for (const [id, d] of C.DRAWN) out.drawn[id] = { png: png(d.tex.image), foot: r6(d.foot), aspect: r6(d.aspect) };

  const { print, sheet } = C.sheetBounds(), hemi = C.mapHemi(), sun = C.mapSun();
  out.manifest = {
    sheet: { bounds: sheet.map(r6), print: print.map(r6), px_per_unit: C.MAP_LOOK.px, unit_m: C.MAP_LOOK.unitM, table_top: C.MAP_LOOK.tableTop },
    lights: { hemisphere: { sky: hex(hemi.color), ground: hex(hemi.groundColor), intensity: r6(hemi.intensity) },
      sun: { colour: hex(sun.color), intensity: r6(sun.intensity), position: v3(sun.position) }, room: lights,
      units: 'three.js r147 legacy units: an intensity here is about pi times a later three.js one; the game sets its own energies by eye against the mockup\'s shots' },
    drawn: Object.fromEntries(Object.entries(out.drawn).map(([id, d]) => [id, { file: `drawn/${id.replace(/[:]/g, '_')}.png`, foot: d.foot, aspect: d.aspect }])),
    palette: C.NEBULA_TINTS.length,
    planets: { kinds: C.PLANET_KIND, palettes: window.FCPlanet.KINDS, lattice: 'made in code: 256 x 256 Mulberry32 values from seed 0x5eed1e57, green the value 37 across and 17 down (planet-shader.js latticeTex)', pixels: 220, steps: 5,
      shader: 'src/Game/Flow/planet.gdshader, a port of docs/mockups/lib/planet-shader.js' },
    sprite_tint: '#e8e0d2',
    materials: out.materials,
  };
  return out;
}

const browser = await chromium.launch({ args: ['--use-gl=angle', '--use-angle=swiftshader', '--enable-unsafe-swiftshader'] });
const page = await browser.newPage({ viewport: { width: 1280, height: 800 } });
const errors = [];
page.on('pageerror', (e) => errors.push(e.message));
await page.goto('file://' + path.join(ROOT, PAGE), { timeout: 600000, waitUntil: 'domcontentloaded' });
await page.waitForFunction(() => window.MOCKUP_READY, null, { timeout: 600000, polling: 1000 });
await page.evaluate(() => window.MOCKUP_SHOTS.map());
await page.waitForTimeout(1500);
await page.addScriptTag({ path: path.join(VENDOR, 'GLTFExporter.r147.js') });
const res = await page.evaluate(exportInPage);
await browser.close();
if (errors.length) throw new Error(PAGE + ': ' + errors.join('\n'));

const sha = (b) => crypto.createHash('sha256').update(b).digest('hex');
const write = (rel, buf) => { const p = path.join(ROOT, OUT, rel); fs.mkdirSync(path.dirname(p), { recursive: true }); fs.writeFileSync(p, buf); console.log(sha(buf).slice(0, 12), rel, buf.length); };
for (const [k, b] of Object.entries(res.layers)) write(`sheet_${k}.png`, Buffer.from(b, 'base64'));
write('room.glb', Buffer.from(res.room, 'base64'));
write('paper.glb', Buffer.from(res.paper, 'base64'));
for (const [id, d] of Object.entries(res.drawn)) write(`drawn/${id.replace(/[:]/g, '_')}.png`, Buffer.from(d.png, 'base64'));
const man = { _doc: 'The paper chart, exported from mockup 1 (docs/mockups/game-flow.html, buildRoom, paperSheet, paperMesh, the drawn pieces) by tools/mockups/export_chart.mjs. Map units (1 unit = 5 cm), +Y up. Drawn by src/Game/Flow/ChartRoom.cs.',
  source: { page: PAGE, sha256: sha(fs.readFileSync(path.join(ROOT, PAGE))) }, ...res.manifest };
write('manifest.json', Buffer.from(JSON.stringify(man, null, 1) + '\n'));
