// Fleet Command mockups: the grown hull (Ship Design Bible chapter 14, F1-F5). One implementation (CLAUDE.md section 6.1)
// of how a ship's skin is grown as one structure, shared by every ship page that grows its hulls:
// docs/mockups/ssarith-ship-concepts.html (the Ssarith Hunt, mockup 12, which wrote it first, version 12) and
// docs/mockups/kelvane-ship-concepts.html (the Kelvane Institute, mockup 23). A faction gives it its own cross-section
// layout; nothing here is any faction's shape. Edit it here, then re-run tools/mockups/inline_data.py on each page that
// inlines it.
//
// FCHullGrow(THREE) returns { V2, seg, cutLine, at3, Skin, creasedNormals }.
//
// A hull is one closed skin. Its body is lofted through cross-sections of one fixed layout (a function from a station's
// parameters to a closed ring of points, counter-clockwise seen from the bow, the same count at every station), so any
// two stations loft point to point. Every prong, lance, hood point or rail grows out of the body: the body's last section
// is split along cut lines into regions (seg picks a run of the section's own points, cutLine the straight line across
// that closes it), and each region lofts on as its own branch, its section a piece of the body's own. Neighbouring
// branches share their cut line where they split, so the skin never opens and never overlaps. Two branches that loft on
// unscaled to the same station and meet along their cut line again can be closed by a new body from that station
// (body(..., { stern: 'open' })): the skin then runs round an opening through the ship. Light strips and dark glass
// are faces of the skin given another material, the drive a recess cut into the stern. No box, cylinder or plate is placed.
window.FCHullGrow = function FCHullGrow(THREE) {
'use strict';
const V2 = (x, y) => new THREE.Vector2(x, y);
const V3 = (x = 0, y = 0, z = 0) => new THREE.Vector3(x, y, z);
/** The run of section points from index i round to index j (counter-clockwise, wrapping), both included. */
const seg = (S, i, j) => { const o = []; for (let k = i; ; k = (k + 1) % S.length) { o.push(S[k]); if (k === j) break; } return o; };
/** m points evenly spaced on the straight line from a to b, ends excluded: the cut that closes a region. */
const cutLine = (a, b, m) => Array.from({ length: m }, (_, i) => a.clone().lerp(b, (i + 1) / (m + 1)));
/** A 2D ring placed at station z (metres along the ship, +z the bow). */
const at3 = (R, z) => R.map((q) => V3(q.x, q.y, z));
const centroid = (R) => R.reduce((a, q) => a.add(q), V2(0, 0)).multiplyScalar(1 / R.length);

// Normals shared across a vertex only by faces within `deg` of each other: a smooth skin that keeps its hard edges (a
// cap's rim, a recess's lip). pts: a triangle list (Vector3s, three a face); returns a normal for each. Positions are
// matched to 0.1 mm, so a branch and the body it grows from share normals along the seam.
function creasedNormals(pts, deg) {
  const n = pts.length, cos = Math.cos(deg * Math.PI / 180), fn = [], key = [], at = new Map();
  for (let f = 0; f < n / 3; f++) { const [a, b, c] = [pts[3 * f], pts[3 * f + 1], pts[3 * f + 2]]; const v = V3().subVectors(b, a).cross(V3().subVectors(c, a)); fn.push([v, v.clone().normalize()]); }
  for (let v = 0; v < n; v++) { const q = pts[v], k = `${Math.round(q.x * 1e4)},${Math.round(q.y * 1e4)},${Math.round(q.z * 1e4)}`; key.push(k);
    if (!at.has(k)) at.set(k, []); at.get(k).push(v); }
  const out = new Float32Array(n * 3), s = V3();
  for (let v = 0; v < n; v++) { const me = fn[Math.floor(v / 3)][1]; s.set(0, 0, 0);
    for (const w of at.get(key[v])) { const [o, on] = fn[Math.floor(w / 3)]; if (on.dot(me) >= cos) s.add(o); } // area weighted
    if (s.lengthSq() < 1e-12) s.copy(me); s.normalize(); out[3 * v] = s.x; out[3 * v + 1] = s.y; out[3 * v + 2] = s.z; }
  return out;
}

class Skin {
  /**
   * g: the group the skin's meshes go into. mats: a material for each kind of face ({ hull, glow, dark } and any other
   * kind a glow selector names, such as a faction's dark glass). sec: the faction's section layout, params to a ring.
   * crease: when set, normals are shared across faces within that many degrees (a smooth skin); else every face is flat.
   * fanCaps: close a ring with a fan from its centre whenever the ring is star-shaped about it, so every point of the ring,
   * the straight cut line's included, is a corner of the cap and the cap meets its bands edge to edge (the triangulator
   * otherwise drops points that lie on a straight line, leaving the skin open there).
   */
  constructor(g, { mats, sec, crease = 0, fanCaps = false } = {}) { this.g = g; this.mats = mats; this.sec = sec; this.crease = crease; this.fanCaps = fanCaps; }
  part(name) { const t = {}; this.cur = t; (this.parts ||= []).push([name, t]); return this; }
  tri(kind, a, b, c, hint) { const n = V3().subVectors(b, a).cross(V3().subVectors(c, a)); if (n.lengthSq() < 1e-10) return;
    const t = (this.cur[kind] ||= []); if (n.dot(hint) < 0) t.push(a, c, b); else t.push(a, b, c); }
  /**
   * A band of quads between two rings of the same layout. glow(k) picks what edge k (point k to k + 1) is: true for a
   * light strip, a kind's name for a face of that kind (outward, like the hull), anything else for `kind`. 'dark' faces
   * the other way: a recess's walls.
   */
  band(A, B, glow = () => false, kind = 'hull') { const n = A.length;
    for (let k = 0; k < n; k++) { const a = A[k], b = A[(k + 1) % n], c = B[(k + 1) % n], d = B[k];
      const e = V3().subVectors(b, a).lengthSq() > 1e-8 ? V3().subVectors(b, a) : V3().subVectors(c, d);
      const hint = V3(e.y, -e.x, 0).multiplyScalar(kind === 'dark' ? -1 : 1), s = glow(k), kd = s === true ? 'glow' : (typeof s === 'string' ? s : kind);
      this.tri(kd, a, b, c, hint); this.tri(kd, a, c, d, hint); } }
  /** A flat ring between two rings at one station (the stern's frame round its drive). */
  frame(A, B, dz, kind = 'hull') { const n = A.length, h = V3(0, 0, dz);
    for (let k = 0; k < n; k++) { this.tri(kind, A[k], A[(k + 1) % n], B[(k + 1) % n], h); this.tri(kind, A[k], B[(k + 1) % n], B[k], h); } }
  /** A flat face closing a ring; dz says which way along the ship it faces. */
  cap(R, dz, kind = 'hull') { const h = V3(0, 0, dz);
    if (this.fanCaps) { const c = R.reduce((a, q) => a.add(q), V3()).multiplyScalar(1 / R.length), n = R.length; let sg = 0, ok = true;
      for (let i = 0; i < n && ok; i++) { const a = R[i], b = R[(i + 1) % n], ar = (a.x - c.x) * (b.y - c.y) - (a.y - c.y) * (b.x - c.x);
        if (Math.abs(ar) < 1e-9) continue; if (!sg) sg = Math.sign(ar); else if (Math.sign(ar) !== sg) ok = false; }
      if (ok) { for (let i = 0; i < n; i++) this.tri(kind, c, R[i], R[(i + 1) % n], h); return; } }
    const f = THREE.ShapeUtils.triangulateShape(R.map((q) => V2(q.x, q.y)), []);
    for (const [i, j, k] of f) this.tri(kind, R[i], R[j], R[k], h); }
  /** A drive recessed into a ring at station z, facing dz: a frame in to 0.72 of the ring, walls 1.6 m deep, a lit back. */
  drive(R2, z, dz) { const c0 = centroid(R2), inset = R2.map((q) => c0.clone().add(q.clone().sub(c0).multiplyScalar(0.72)));
    this.frame(at3(R2, z), at3(inset, z), dz); this.band(at3(inset, z), at3(inset, z - dz * 1.6), () => false, 'dark'); this.cap(at3(inset, z - dz * 1.6), dz, 'glow'); }
  /**
   * The body: stations [z, section params] from the stern forward; returns the last section. sec: the layout (the skin's
   * own unless given). stern: 'drive' (the drive recessed into it), 'cap' (closed flat), 'open' (left open, where the
   * body closes branches that met again). The first section is kept as this.first, for branches reaching aft.
   */
  body(stations, glow = () => false, sec = this.sec, { stern = 'drive', name = 'body' } = {}) { this.part(name);
    const rings = stations.map(([z, p]) => at3(sec(p), z)), S0 = sec(stations[0][1]), z0 = stations[0][0];
    if (stern === 'drive') this.drive(S0, z0, -1); else if (stern === 'cap') this.cap(rings[0], -1);
    for (let i = 0; i + 1 < rings.length; i++) { const zm = (stations[i][0] + stations[i + 1][0]) / 2; this.band(rings[i], rings[i + 1], (k) => glow(k, zm)); }
    this.first = S0; return sec(stations[stations.length - 1][1]); }
  /**
   * A branch: a region of a split section lofted through [dz, sx, sy, dx, dy] (scaled about the region's centroid, or
   * about an anchor given as a fraction of its bounds, then moved), from station z; a branch with one station is only its
   * end. end: 'cap' (closed flat), 'drive' (a drive recessed into it, for a branch reaching aft), 'open' (left open, to be
   * closed by a body). capKind: what the end cap is made of (hull unless given, such as a faction's glass). glow(k, dz) as
   * for band. The end ring is kept as this.end.
   */
  branch(name, R, z, steps, { glow = () => false, anchor = null, end = 'cap', capKind = 'hull' } = {}) { this.part(name);
    let c = centroid(R);
    if (anchor) { const b = new THREE.Box2().setFromPoints(R); c = V2(b.min.x + (b.max.x - b.min.x) * anchor[0], b.min.y + (b.max.y - b.min.y) * anchor[1]); }
    const flat = steps.map(([, sx, sy, dx, dy]) => R.map((q) => V2(c.x + (q.x - c.x) * sx + dx, c.y + (q.y - c.y) * sy + dy)));
    const rings = flat.map((F, i) => at3(F, z + steps[i][0]));
    for (let i = 0; i + 1 < rings.length; i++) { const dm = (steps[i][0] + steps[i + 1][0]) / 2; this.band(rings[i], rings[i + 1], (k) => glow(k, dm)); }
    const last = rings[rings.length - 1], zl = z + steps[steps.length - 1][0], dir = Math.sign(steps[steps.length - 1][0] - (steps.length > 1 ? steps[steps.length - 2][0] : -1)) || 1;
    if (end === 'cap') this.cap(last, dir, capKind); else if (end === 'drive') this.drive(flat[flat.length - 1], zl, dir);
    this.end = last; return this; }
  /** Builds a mesh for every part and kind of face, named for its part, into the group. With a crease, the normals are
   *  worked out over the whole skin at once, so they run smoothly across every seam between parts and kinds. */
  done() { const chunks = [];
    for (const [name, t] of this.parts) for (const kind of ['hull', 'glow', 'dark', ...Object.keys(t).filter((k) => !['hull', 'glow', 'dark'].includes(k))]) {
      const pts = t[kind]; if (pts && pts.length && this.mats[kind]) chunks.push([name, kind, pts]); }
    const all = this.crease ? creasedNormals(chunks.flatMap((c) => c[2]), this.crease) : null; let o = 0;
    for (const [name, kind, pts] of chunks) { const geo = new THREE.BufferGeometry().setFromPoints(pts);
      if (all) { geo.setAttribute('normal', new THREE.BufferAttribute(all.slice(o * 3, (o + pts.length) * 3), 3)); o += pts.length; } else geo.computeVertexNormals();
      const m = new THREE.Mesh(geo, this.mats[kind]); m.name = name; this.g.add(m); } }
}
return { V2, seg, cutLine, at3, Skin, creasedNormals };
};
