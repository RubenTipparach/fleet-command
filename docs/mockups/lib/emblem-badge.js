// Fleet Command mockups: an emblem as an orbitable 3D badge, one implementation (CLAUDE.md section 6.1) of the studio, the
// Material Maker materials, the part builder, the views, the bar and the named shots, shared by every badge page: the
// Concord's (docs/mockups/concord-emblem-3d.html) and the Krai's (docs/mockups/krai-emblem-3d.html). Each page keeps only
// its own parts, heights and special materials. Edit it here, then re-run tools/mockups/inline_data.py on each page.
//
// FCEmblemBadge(THREE, { stage, bar, maps, views, onFrame }) returns { scene, badge, mmMaterial, slab, start }.
//   maps     the Material Maker maps by name (docs/mockups/lib/emblem-materials.json's pieces)
//   views    { name: [x, y, z, tx, ty] } camera positions (mm) for a wide screen; on a narrow one they move back
//   onFrame  (dt, frozen) called every frame (a page's animated material)
//   start(shots, first)  starts the loop with the bar's view buttons and sets window.MOCKUP_SHOTS from
//            { shotName: [viewName, setup] } (setup runs before the shot, after the scene is frozen)
// 1 three.js unit = 1 mm; the badge is 100 mm across, its parts given as polygons on the guide's 100 unit grid (y down).
// FCEmblemStudio(THREE, renderer) returns the studio's environment map, for any page that lights a badge (the game flow's
// faction screen lights its badges with it, under its own lights).
window.FCEmblemStudio = function FCEmblemStudio(THREE, renderer) {
const pmrem = new THREE.PMREMGenerator(renderer);
// What the polished faces reflect: the same studio as the Blender render (tools/blender/emblem_render.py environment()):
// near black toward the viewer, a bright sky above with a warm band at the horizon, a dark floor, plus a cool and a warm
// softbox at the sides, so bevels read as bright and dark edges and a thin film has light to colour.
const envScene = new THREE.Scene();
envScene.add(new THREE.Mesh(new THREE.SphereGeometry(100, 64, 32), new THREE.ShaderMaterial({ side: THREE.BackSide, vertexShader: `
  varying vec3 vDir; void main() { vDir = normalize(position); gl_Position = projectionMatrix * modelViewMatrix * vec4(position, 1.0); }`,
  fragmentShader: `
  varying vec3 vDir;
  void main() {
    vec3 d = normalize(vDir);
    vec3 sky = mix(vec3(0.01, 0.012, 0.018), vec3(1.0, 1.0, 1.05), smoothstep(-0.1, 0.85, d.y));
    sky += vec3(0.6, 0.48, 0.32) * exp(-pow((d.y - 0.05) / 0.07, 2.0));
    sky += vec3(0.35, 0.55, 1.0) * 1.4 * smoothstep(0.92, 0.98, dot(d, normalize(vec3(0.9, 0.1, 0.4))));
    sky += vec3(1.0, 0.8, 0.6) * 1.2 * smoothstep(0.93, 0.985, dot(d, normalize(vec3(-0.85, 0.35, 0.4))));
    float front = smoothstep(0.55, 0.95, d.z);
    gl_FragColor = vec4(sky * (1.0 - 0.9 * front), 1.0);
  }` })));
const env = pmrem.fromScene(envScene, 0.02).texture;
pmrem.dispose();
return env;
};

// FCEmblemParts(THREE, maps, group) returns { mmMaterial, slab, badge }: the Material Maker materials and the part builder
// alone, building into `group`, for a page that sets its own studio (the game flow's faction screen, docs/mockups/game-flow.html).
// The badges' own parts are docs/mockups/lib/emblem-builds.js.
window.FCEmblemParts = function FCEmblemParts(THREE, maps, badge) {
'use strict';
// ---- Material Maker maps ----------------------------------------------------------------------------------------
const loader = new THREE.TextureLoader();
const TILE_MM = 50; // every emblem graph tiles every 0.05 m
function tex(name, colour) {
  const t = loader.load(maps[name].data);
  t.wrapS = t.wrapT = THREE.RepeatWrapping; t.repeat.set(1 / TILE_MM, 1 / TILE_MM); t.anisotropy = 8;
  if (colour) t.encoding = THREE.sRGBEncoding;
  return t;
}
// A physical material from one graph: colour, normal, AO (ORM red), roughness (ORM green), metal (ORM blue) and the
// specular map (three.js reads a specular colour map's RGB; ours is grey, so it scales the specular strength). A `color`
// in extra tints the graph's colour, as the Blender render's tint does.
function mmMaterial(name, extra = {}) {
  const orm = tex(`${name}/orm`, false);
  return new THREE.MeshPhysicalMaterial(Object.assign({
    map: tex(`${name}/albedo`, true), normalMap: tex(`${name}/normal`, false), normalScale: new THREE.Vector2(1, 1),
    aoMap: orm, aoMapIntensity: 1.0, roughnessMap: orm, metalnessMap: orm, roughness: 1, metalness: 1,
    specularColorMap: tex(`${name}/specular`, true), specularIntensity: 1,
  }, extra));
}

// ---- the parts, built from the guide's polygons ------------------------------------------------------------------
const toV2 = (p) => new THREE.Vector2(p[0] - 50, 50 - p[1]); // grid (y down) to mm, centred, y up
const area = (ring) => { let a = 0; for (let i = 0; i < ring.length; i++) { const [x1, y1] = ring[i], [x2, y2] = ring[(i + 1) % ring.length]; a += x1 * y2 - x2 * y1; } return Math.abs(a / 2); };
// A part between z0 and z1 mm whose outline is the given rings (the largest is the outline, the rest its holes), its
// edges rounded by `bevel` mm, kept to the drawn size (the bevel is taken from inside the outline).
function slab(rings, z0, z1, bevel, material) {
  const sorted = rings.slice().sort((a, b) => area(b) - area(a));
  const shape = new THREE.Shape(sorted[0].map(toV2));
  for (const h of sorted.slice(1)) shape.holes.push(new THREE.Path(h.map(toV2)));
  const geo = new THREE.ExtrudeGeometry(shape, { depth: Math.max(0.01, z1 - z0 - 2 * bevel), bevelEnabled: true, bevelThickness: bevel, bevelSize: bevel, bevelOffset: -bevel, bevelSegments: 4, curveSegments: 1 });
  geo.translate(0, 0, z0 + bevel);
  geo.setAttribute('uv2', geo.attributes.uv); // the AO map reads the second UV set
  const mesh = new THREE.Mesh(geo, material);
  badge.add(mesh);
  return mesh;
}

return { mmMaterial, slab, badge };
};

window.FCEmblemBadge = function FCEmblemBadge(THREE, opts) {
'use strict';
const { stage, bar, maps, views } = opts;
const renderer = new THREE.WebGLRenderer({ antialias: true, preserveDrawingBuffer: true });
renderer.setPixelRatio(Math.min(devicePixelRatio, 2));
renderer.outputEncoding = THREE.sRGBEncoding;
renderer.toneMapping = THREE.ACESFilmicToneMapping;
renderer.toneMappingExposure = 1.0;
stage.append(renderer.domElement);
const scene = new THREE.Scene();
const BLACK = 0x05070d;
scene.background = new THREE.Color(BLACK);
scene.environment = window.FCEmblemStudio(THREE, renderer);
const camera = new THREE.PerspectiveCamera(32, 1, 1, 5000);
const controls = new THREE.OrbitControls(camera, renderer.domElement);
controls.enableDamping = true;
controls.minDistance = 80; controls.maxDistance = 600;
// three.js 0.147 uses the legacy light units: about pi times brighter than r155 and later for the same number.
const key = new THREE.DirectionalLight(0xfff2e0, 0.55); key.position.set(-120, 140, 260); scene.add(key);
const rim = new THREE.DirectionalLight(0xbcd6ff, 0.45); rim.position.set(220, -60, 90); scene.add(rim);
scene.add(new THREE.HemisphereLight(0x9fb4ff, 0x080a12, 0.08));

// ---- Material Maker materials and the part builder (FCEmblemParts, above) ---------------------------------------
const badge = new THREE.Group();
scene.add(badge);
const { mmMaterial, slab } = window.FCEmblemParts(THREE, maps, badge);

// ---- views, background, motion, shots -----------------------------------------------------------------------------
const fit = () => Math.max(1, 1.15 / camera.aspect);
const VIEWS = {};
for (const [n, [x, y, z, tx, ty]] of Object.entries(views)) VIEWS[n] = () => { const k = fit(); camera.position.set(tx + (x - tx) * k, ty + (y - ty) * k, 1 + (z - 1) * k); controls.target.set(tx, ty, 1); };
let spin = false, frozen = false;
const btn = (label, fn) => { const b = document.createElement('button'); b.type = 'button'; b.textContent = label; b.onclick = fn; bar.append(b); return b; };
function resize() { const w = stage.clientWidth, h = stage.clientHeight; renderer.setSize(w, h, false); camera.aspect = w / h; camera.updateProjectionMatrix(); }
function start(shots, first) {
  for (const [n, f] of Object.entries(VIEWS)) btn(n, f);
  const spinB = btn('Turn', () => { spin = !spin; spinB.setAttribute('aria-pressed', String(spin)); });
  const bgB = btn('White background', () => { const w = scene.background.getHex() === BLACK; scene.background.set(w ? 0xffffff : BLACK); bgB.textContent = w ? 'Black background' : 'White background'; });
  addEventListener('resize', resize); resize();
  VIEWS[first]();
  const meter = document.createElement('span'); meter.className = 'meter'; bar.append(meter);
  const clock = new THREE.Clock();
  renderer.setAnimationLoop(() => {
    const dt = clock.getDelta();
    if (opts.onFrame) opts.onFrame(dt, frozen);
    if (spin) badge.rotation.y += dt * 0.5;
    controls.update();
    renderer.render(scene, camera);
    meter.textContent = `${renderer.info.render.triangles.toLocaleString()} tris, ${renderer.info.render.calls} draws`;
  });
  // Shots: frozen, so every shot is the same each run.
  window.MOCKUP_SHOTS = {};
  for (const [name, [v, setup]] of Object.entries(shots)) window.MOCKUP_SHOTS[name] = () => { frozen = true; badge.rotation.y = 0; spin = false; if (setup) setup(); VIEWS[v](); };
  window.MOCKUP_SCENE = scene;
  requestAnimationFrame(() => requestAnimationFrame(() => { window.MOCKUP_READY = true; }));
}

return { scene, badge, mmMaterial, slab, start };
};
