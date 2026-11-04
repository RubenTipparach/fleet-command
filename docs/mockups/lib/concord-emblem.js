// Fleet Command mockups: the Concord's emblem painted on a Concord hull, one implementation (CLAUDE.md section 6.1) of where
// it goes, how the livery band breaks round it and how it is painted, shared by every page that shows it on a ship: the
// Concord line (docs/mockups/concord-hull-concepts.html, mockup 6, the real model) and the hull study
// (docs/mockups/concord-emblem-hull.html, mockup 11, retired but kept as a record). Edit it here, then re-run tools/mockups/inline_data.py on each page.
//
// The design (openspec/changes/faction-emblems/design.md; owner, 2026-11-04): B-E27, the smooth keyhole, once per ship,
// upright ("don't rotate our logo") on the port side ("1/2 size on the left side", "only one logo"), 3 % of the ship's
// length tall, on the forward livery band ("place emblem on liver stripe"), its field the band's own stripe colour with no
// outline ("No outline", later "lets add that border back in": a border in the band's seam colour), a pearl ship and a cyan star ("a cyan colored star in the middle"). The band is broken round it
// by bare hull ("The hull should break the stripe livery at that spot"), its cut edges bordered in the seam colour, 0.35 m
// clear of the emblem's border where the band crosses it: an arc over the head and a flat edge under the foot ("bottom line can be flat to conform to the key hole/beacon
// shape more"). No livery is added; the band is the ship's own.
//
// FCConcordEmblem(THREE, parts) takes the guide's exported shapes (assets/emblems/concord/b-e27.parts.json) and returns:
//   texture  the emblem as a mask: red the field, green the ship, blue the star (linear data, so a shader picks the colours)
//   mark(C, hullAt, chOf)  where it goes on class C: { c, n, u, v (ship space, metres), uniforms } or null for a reference
//            ship (C.ref, not ours)
//   GLSL     uniforms and functions for a hull shader: embBreak(p, n) is the band's break, embEdge(p, n) the seam border on
//            its cut edge, embCover(p, n) the area kept clear of windows, embPaint(p, n, c, field, seam) the paint over
//            colour c, its field in `field` and its border in `seam`
window.FCConcordEmblem = function FCConcordEmblem(THREE, parts) {
'use strict';
const V3 = (x = 0, y = 0, z = 0) => new THREE.Vector3(x, y, z);
const LENGTH_SHARE = 0.03;      // the emblem's height, a share of the ship's length (half of E7's 6 %; owner: "1/2 size")
const BAND_HALF = 1.1;          // half the livery band's width along the hull, m (the Concord line's stripe rule, d < 1.1)
const GAP = 0.35;               // bare hull between the emblem's border and the broken band's border, m (owner, 2026-11-04:
                                // "add some space between strip and emblem")
const EDGE = 0.22;              // the border on the band's cut edge, m: the band's own seam width (the Concord line's
                                // stripe rule, 1.1 m to 1.32 m from its centre)
const ABOVE_SERVICE = 0.3;      // the emblem's foot stands this far above the waterline service band, m
const INK = [0.80, 0.81, 0.82]; // the ship: the hull's own armour pearl (linear), so it reads as plating left bare
// The emblem's border is the frame, in the band's seam colour (owner, 2026-11-04: "lets add that border back in here. so it
// appears more consistent ... border on both edge of lines and emblem"): the band's sides, its cut edges and the emblem all
// carry the same dark seam.
const STAR = '#2fd0f0';         // the star, cyan

// The frame's height on the 100 unit grid: the emblem's size is set by it.
const ys = parts.frame.flat().map((p) => p[1]), GRID_H = Math.max(...ys) - Math.min(...ys);

// The mask: field red, ship green, star blue, the frame black (opaque with no colour), drawn from the guide's own shapes.
function maskCanvas() {
  const N = 1024, k = N / 100, cv = document.createElement('canvas'); cv.width = cv.height = N;
  const g = cv.getContext('2d');
  const fill = (rings, col) => { g.beginPath(); for (const r of rings) { r.forEach(([x, y], i) => (i ? g.lineTo(x * k, y * k) : g.moveTo(x * k, y * k))); g.closePath(); } g.fillStyle = col; g.fill(); };
  g.beginPath(); for (const r of parts.frame) { r.forEach(([x, y], i) => (i ? g.lineTo(x * k, y * k) : g.moveTo(x * k, y * k))); g.closePath(); } g.fillStyle = '#000000'; g.fill('evenodd');
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
  // The break: an arc over the head, clear of the emblem's border by GAP plus the cut edge's own border where the band
  // crosses it, and a flat edge as far under the foot.
  const frameM = parts.frame.flat().map(([x, yy]) => [(x - 50) * mPerUnit, (50 - yy) * mPerUnit]);
  const clear = GAP + EDGE;
  const breakR = Math.max(...frameM.filter(([x, vv]) => vv >= 0 && Math.abs(x) <= BAND_HALF + EDGE + clear).map(([x, vv]) => Math.hypot(x, vv))) + clear;
  const breakFlat = Math.min(...frameM.map(([, vv]) => vv)) - clear;
  const uniforms = {
    uEmbTex: { value: texture }, uEmbC: { value: c }, uEmbN: { value: n }, uEmbU: { value: u }, uEmbV: { value: v },
    uEmbP: { value: new THREE.Vector4(100 * mPerUnit, breakR, breakFlat, 1) },
    uEmbInk: { value: new THREE.Color(...INK) }, uEmbStar: { value: new THREE.Color(STAR).convertSRGBToLinear() }, uEmbEdge: { value: EDGE },
  };
  return { c, n, u, v, height_m: H, uniforms };
}
// Uniforms for a material with no emblem (a reference ship): everything off.
const off = () => ({ uEmbTex: { value: texture }, uEmbC: { value: V3() }, uEmbN: { value: V3(1, 0, 0) }, uEmbU: { value: V3(0, 0, -1) }, uEmbV: { value: V3(0, 1, 0) },
  uEmbP: { value: new THREE.Vector4(1, 0, 0, 0) }, uEmbInk: { value: new THREE.Color() }, uEmbStar: { value: new THREE.Color() }, uEmbEdge: { value: 0 } });

const GLSL = `
uniform sampler2D uEmbTex; uniform vec3 uEmbC, uEmbN, uEmbU, uEmbV; uniform vec4 uEmbP; uniform vec3 uEmbInk, uEmbStar; uniform float uEmbEdge;
// uEmbP: x the texture's size (m), y the break's arc radius, z its flat edge (below the centre), w on.
bool embNear(vec3 p, vec3 n) { return uEmbP.w > 0.5 && abs(dot(p - uEmbC, uEmbN)) < 1.5 && dot(n, uEmbN) > -0.5; }
vec2 embQ(vec3 p) { vec3 d = p - uEmbC; return vec2(dot(d, uEmbU), dot(d, uEmbV)); }
// How far outside the break a point is (m; negative inside), 1e3 when not near the emblem.
float embBreakDist(vec3 p, vec3 n) {
  if (!embNear(p, n)) return 1e3;
  vec2 q = embQ(p);
  float below = uEmbP.z - q.y;                       // under the flat edge
  // Outside the arc over the head, and, below the centre, outside the arc's own width. Without that bound the flat part
  // ran the whole length of the flank and cut every band crossing that height (owner, 2026-11-04: "why is the middle one
  // cut short?", "it needs to be a full stripe").
  float arc = q.y >= 0.0 ? length(q) - uEmbP.y : abs(q.x) - uEmbP.y;
  return max(below, arc);
}
bool embBreak(vec3 p, vec3 n) { return embBreakDist(p, n) < 0.0; }
// The band's cut edge: the seam border just outside the break.
bool embEdge(vec3 p, vec3 n) { float d = embBreakDist(p, n); return d >= 0.0 && d < uEmbEdge; }
bool embCover(vec3 p, vec3 n) { if (!embNear(p, n)) return false; vec2 q = embQ(p); return embBreakDist(p, n) < uEmbEdge || (abs(q.x) < uEmbP.x * 0.46 && abs(q.y) < uEmbP.x * 0.46); }
// The emblem over colour c: field in \`field\`, the border (frame) in \`seam\`, the ship pearl, the star cyan.
vec3 embPaint(vec3 p, vec3 n, vec3 c, vec3 field, vec3 seam) {
  if (!embNear(p, n)) return c;
  vec2 uv = embQ(p) / uEmbP.x + 0.5;
  if (uv.x < 0.0 || uv.x > 1.0 || uv.y < 0.0 || uv.y > 1.0) return c;
  vec4 t = texture2D(uEmbTex, uv);
  if (t.a < 0.004) return c;
  float s = t.r + t.g + t.b;
  vec3 e = field * t.r + uEmbInk * t.g + uEmbStar * t.b + seam * max(1.0 - s, 0.0);
  return mix(c, e, t.a);
}`;

return { texture, mark, off, GLSL, LENGTH_SHARE };
};
