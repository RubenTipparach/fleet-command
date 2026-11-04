// Fleet Command mockups: the Concord's emblem painted on a Concord hull, one implementation (CLAUDE.md section 6.1) of where
// it goes, how the livery band breaks round it and how it is painted, shared by every page that shows it on a ship: the
// Concord line (docs/mockups/concord-hull-concepts.html, mockup 6, the real model) and the hull study
// (docs/mockups/concord-emblem-hull.html, mockup 11). Edit it here, then re-run tools/mockups/inline_data.py on each page.
//
// The design (openspec/changes/faction-emblems/design.md; owner, 2026-11-04): B-E27, the smooth keyhole, once per ship,
// upright ("don't rotate our logo") on the port side ("1/2 size on the left side", "only one logo"), 3 % of the ship's
// length tall, on the forward livery band ("place emblem on liver stripe"), its field the band's own stripe colour with no
// outline ("No outline"), a pearl ship and a cyan star ("a cyan colored star in the middle"). The band is broken round it
// by bare hull ("The hull should break the stripe livery at that spot"), 0.15 m clear of the emblem where the band crosses
// it: an arc over the head and a flat edge under the foot ("bottom line can be flat to conform to the key hole/beacon
// shape more"). No livery is added; the band is the ship's own.
//
// FCConcordEmblem(THREE, parts) takes the guide's exported shapes (assets/emblems/concord/b-e27.parts.json) and returns:
//   texture  the emblem as a mask: red the field, green the ship, blue the star (linear data, so a shader picks the colours)
//   mark(C, hullAt, chOf)  where it goes on class C: { c, n, u, v (ship space, metres), uniforms } or null for a reference
//            ship (C.ref, not ours)
//   GLSL     uniforms and functions for a hull shader: embBreak(p, n) is the band's break, embCover(p, n) the area kept
//            clear of windows, embPaint(p, n, c, field) the paint over colour c with the field in colour `field`
window.FCConcordEmblem = function FCConcordEmblem(THREE, parts) {
'use strict';
const V3 = (x = 0, y = 0, z = 0) => new THREE.Vector3(x, y, z);
const LENGTH_SHARE = 0.03;      // the emblem's height, a share of the ship's length (half of E7's 6 %; owner: "1/2 size")
const BAND_HALF = 1.1;          // half the livery band's width along the hull, m (the Concord line's stripe rule, d < 1.1)
const GAP = 0.15;               // bare hull between the emblem and the broken band, m
const ABOVE_SERVICE = 0.3;      // the emblem's foot stands this far above the waterline service band, m
const INK = [0.80, 0.81, 0.82]; // the ship: the hull's own armour pearl (linear), so it reads as plating left bare
const STAR = '#2fd0f0';         // the star, cyan

// The frame's height on the 100 unit grid: the emblem's size is set by it.
const ys = parts.frame.flat().map((p) => p[1]), GRID_H = Math.max(...ys) - Math.min(...ys);

// The mask: field red, ship green, star blue, drawn from the guide's own shapes.
function maskCanvas() {
  const N = 1024, k = N / 100, cv = document.createElement('canvas'); cv.width = cv.height = N;
  const g = cv.getContext('2d');
  const fill = (rings, col) => { g.beginPath(); for (const r of rings) { r.forEach(([x, y], i) => (i ? g.lineTo(x * k, y * k) : g.moveTo(x * k, y * k))); g.closePath(); } g.fillStyle = col; g.fill(); };
  fill(parts.field, '#ff0000');
  for (const leg of parts.legs) fill([leg], '#00ff00');
  fill([parts.tail], '#00ff00');
  fill([parts.lens], '#00ff00');
  fill([parts.star], '#0000ff');
  const L = parts.light; g.beginPath(); g.arc(L.cx * k, L.cy * k, L.r * k, 0, Math.PI * 2); g.fillStyle = '#00ff00'; g.fill();
  return cv;
}
const texture = new THREE.CanvasTexture(maskCanvas());
texture.anisotropy = 8;

// Where it goes on class C, and its uniforms.
function mark(C, hullAt, chOf) {
  if (C.ref || !C.stripes || !C.stripes.length) return null;
  const H = C.length * LENGTH_SHARE, mPerUnit = H / GRID_H;
  const z = Math.max(...C.stripes), h = hullAt(C.stations, z), ch = chOf(h[1], h[2], h[3], h[4]);
  const sideTop = h[2] - ch, sideBot = h[3] + ch, footAbove = (C.band ? C.band[1] : sideBot) + ABOVE_SERVICE;
  const y = Math.min(Math.max((sideTop + sideBot) / 2, footAbove + H / 2), sideTop - H / 2);
  const dhb = (hullAt(C.stations, z + 1)[1] - hullAt(C.stations, z - 1)[1]) / 2;
  const n = V3(1, 0, -dhb).normalize(), v = V3(0, 1, 0), u = V3().crossVectors(v, n), c = V3(h[1], y, z);
  // The break: an arc over the head, clear by GAP where the band crosses it, and a flat edge GAP under the foot.
  const fieldM = parts.field.flat().map(([x, yy]) => [(x - 50) * mPerUnit, (50 - yy) * mPerUnit]);
  const breakR = Math.max(...fieldM.filter(([x, vv]) => vv >= 0 && Math.abs(x) <= BAND_HALF + GAP).map(([x, vv]) => Math.hypot(x, vv))) + GAP;
  const breakFlat = Math.min(...fieldM.map(([, vv]) => vv)) - GAP;
  const uniforms = {
    uEmbTex: { value: texture }, uEmbC: { value: c }, uEmbN: { value: n }, uEmbU: { value: u }, uEmbV: { value: v },
    uEmbP: { value: new THREE.Vector4(100 * mPerUnit, breakR, breakFlat, 1) },
    uEmbInk: { value: new THREE.Color(...INK) }, uEmbStar: { value: new THREE.Color(STAR).convertSRGBToLinear() },
  };
  return { c, n, u, v, height_m: H, uniforms };
}
// Uniforms for a material with no emblem (a reference ship): everything off.
const off = () => ({ uEmbTex: { value: texture }, uEmbC: { value: V3() }, uEmbN: { value: V3(1, 0, 0) }, uEmbU: { value: V3(0, 0, -1) }, uEmbV: { value: V3(0, 1, 0) },
  uEmbP: { value: new THREE.Vector4(1, 0, 0, 0) }, uEmbInk: { value: new THREE.Color() }, uEmbStar: { value: new THREE.Color() } });

const GLSL = `
uniform sampler2D uEmbTex; uniform vec3 uEmbC, uEmbN, uEmbU, uEmbV; uniform vec4 uEmbP; uniform vec3 uEmbInk, uEmbStar;
// uEmbP: x the texture's size (m), y the break's arc radius, z its flat edge (below the centre), w on.
bool embNear(vec3 p, vec3 n) { return uEmbP.w > 0.5 && abs(dot(p - uEmbC, uEmbN)) < 1.5 && dot(n, uEmbN) > -0.5; }
vec2 embQ(vec3 p) { vec3 d = p - uEmbC; return vec2(dot(d, uEmbU), dot(d, uEmbV)); }
bool embBreak(vec3 p, vec3 n) { if (!embNear(p, n)) return false; vec2 q = embQ(p); return q.y > uEmbP.z && (q.y < 0.0 || length(q) < uEmbP.y); }
bool embCover(vec3 p, vec3 n) { if (!embNear(p, n)) return false; vec2 q = embQ(p); return embBreak(p, n) || (abs(q.x) < uEmbP.x * 0.46 && abs(q.y) < uEmbP.x * 0.46); }
vec3 embPaint(vec3 p, vec3 n, vec3 c, vec3 field) {
  if (!embNear(p, n)) return c;
  vec2 uv = embQ(p) / uEmbP.x + 0.5;
  if (uv.x < 0.0 || uv.x > 1.0 || uv.y < 0.0 || uv.y > 1.0) return c;
  vec4 t = texture2D(uEmbTex, uv);
  float s = t.r + t.g + t.b; if (t.a < 0.004 || s < 0.004) return c;
  vec3 e = (field * t.r + uEmbInk * t.g + uEmbStar * t.b) / s;
  return mix(c, e, t.a);
}`;

return { texture, mark, off, GLSL, LENGTH_SHARE };
};
