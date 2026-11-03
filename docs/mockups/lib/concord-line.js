// Fleet Command mockups: the Concord line, the standard for Concord ships (owner, 2026-11-03: "Concord line (hull concepts) -
// new standard, get rid of the old concord ships"). One implementation (CLAUDE.md section 6.1) of the ship table and its hull
// geometry, shared by every page that shows a Concord ship: docs/mockups/concord-hull-concepts.html (mockup 6, lit and damaged)
// and, through docs/mockups/lib/space.js, the battle console and the game flow. Edit it here, then re-run
// tools/mockups/inline_data.py on each page that inlines it.
//
// FCConcordLine(THREE) returns the table (COL, DECKS, BASES, VARIANTS, MODE_NAMES, CLASSES, LIVERIES, TERN_NACELLE_PROFILE,
// GAP_UP, SIDE_NAMES), the station arithmetic (chOf, hullAt, loftDist, hullsOf, alongNacelle, arrange, sideName), the geometry
// (pushTri, geoFrom, octRing, loft, hullGeo, nacelleGeo, pylonGeo, boxGeo) and plainModel(id), a ship built from the same
// geometry with plain materials for pages that show it small (no windows, damage or lamps).
window.FCConcordLine = function FCConcordLine(THREE) {
'use strict';
const V3 = (x = 0, y = 0, z = 0) => new THREE.Vector3(x, y, z);
const lerp = (a, b, t) => a + (b - a) * t;

// ---------------------------------------------------------------------------------------------------------------
// The ships: the Concord line in Star Crew Bridge Simulator's Tern family. Axes and units follow Star Crew's
// exteriors.json: +X port, +Y dorsal, +Z bow, metres. A station is [z, half beam, top, bottom, chamfer]; decks are floor
// heights, and windows sit 1.5 m above a floor. One table; the builder reads nothing else.
const COL = { pearl: '#e4e2dc', signal: '#2f62e0', deep: '#1b2a6b', lilac: '#b7a6f2', sky: '#3fa9f0', alert: '#e8352b', glass: '#141a26' };
const DECKS = [3.5, 0, -3.5];
const BASES = {
  swift: {
    cls: 'Swift-class corvette', name: 'CNS SWIFT', reg: 'CC-031', length: 51, tell: 'A broad command body forward and a narrow spine aft, the Osprey\'s proportions.',
    stations: [[25, 2.6, 2.4, -1.6, 0.8], [21, 5.8, 3.9, -2.6, 1.2], [13, 8.0, 4.6, -3.0, 1.3], [3, 7.6, 4.5, -3.0, 1.3], [-5, 4.4, 3.6, -2.6, 1.0], [-20, 4.0, 3.3, -2.4, 0.9], [-25, 3.2, 2.5, -1.9, 0.7]],
    decks: [0], live: [-2, 20], band: [-0.9, 0.0], channels: [2.6], stripes: [8.5, -12],
    command: { z: [19, 7], hb: 4.2, top: 6.4, ch: 0.8 },
    pylons: [{ root: [3.4, -0.6, -8], tip: [12.5, -3.0, -14], rc: 10, tc: 5, t: 0.8 }],
    nacelles: [{ x: 12.5, y: -3.0, z: -9, len: 30, hw: 1.6, hh: 1.5 }],
    radiators: [[-11, 1.4, 5.0]], turrets: [[0, 14, 'cmd'], [0, 4, 'bottom']], bells: { z: -25, y: 0.3, x: 1.4, r: 0.9 },
    core: { pos: [0, -2.9, -4], size: [1.2, 0.4, 14] }, torpedo: [0, -0.6, 25.1], deflector: [0, 0.4, 25.05, 1.4],
  },
  petrel: {
    cls: 'Petrel-class frigate', name: 'CNS PETREL', reg: 'CC-117', length: 88, tell: 'The Tern\'s own build at the Concord\'s colours: a short broad bow and three decks of windows.',
    stations: [[43, 4.2, 3.6, -2.6, 1.2], [38, 7.6, 6.2, -4.0, 1.6], [30, 10.0, 7.4, -5.0, 1.8], [14, 11.6, 7.6, -5.4, 1.8], [-6, 12.0, 7.8, -5.6, 2.2], [-24, 11.4, 7.8, -5.6, 2.2], [-36, 9.6, 7.0, -4.8, 2.0], [-43, 7.6, 5.4, -3.4, 1.6]],
    decks: DECKS, live: [-8, 30], band: [-0.65, 0.35], channels: [5.0], stripes: [21, -2, -28],
    command: { z: [33, 15], hb: 6.2, top: 10.2, ch: 1.2 },
    pylons: [{ root: [11, 1.2, -12], tip: [21, 1.2, -22], rc: 16, tc: 10, t: 1.2 }],
    nacelles: [{ x: 21, y: 1.2, z: -14, len: 54, hw: 2.3, hh: 2.2 }],
    radiators: [[-30, 3.1, 8.0], [-30, -3.1, 8.0]], turrets: [[0, 25, 'cmd'], [8.6, -14, 'top'], [-8.6, -14, 'top'], [0, 10, 'bottom']], bells: { z: -43, y: 1.0, x: 3.2, r: 1.6 },
    core: { pos: [0, -5.45, -8], size: [1.8, 0.5, 26] }, torpedo: [0, -1.4, 43.05], deflector: [0, 0.9, 43.05, 2.2],
  },
  albatross: {
    cls: 'Albatross-class cruiser', name: 'CNS ALBATROSS', reg: 'CC-002', length: 112, tell: 'The family at cruiser size, in two hulls: a shorter primary hull with four decks of windows, and a secondary hull beneath it on a neck, and its nacelle pair on level pylons swept from the primary hull\'s waist, the secondary hull clear beneath.',
    stations: [[50, 5.2, 4.4, -3.2, 1.4], [44, 9.6, 7.6, -5.0, 2.0], [34, 13.6, 9.4, -6.6, 2.4], [14, 15.6, 10.0, -7.4, 2.6], [-8, 16.0, 10.2, -7.6, 2.8], [-24, 14.4, 9.6, -7.0, 2.6], [-34, 11.0, 7.8, -5.4, 2.2]],
    decks: [7.0, 3.5, 0, -3.5], live: [-20, 38], band: [-0.65, 0.35], channels: [4.0, 9.0], stripes: [28, 4, -18],
    command: { z: [36, 16], hb: 8.0, top: 13.2, ch: 1.4 },
    // The secondary hull, in the same station form at its own height; the neck joins the two.
    secondary: { stations: [[-6, 4.0, -11.0, -17.0, 1.2], [-12, 7.0, -10.4, -20.4, 1.8], [-30, 7.6, -10.4, -21.0, 2.0], [-48, 7.0, -10.8, -20.4, 1.8], [-58, 5.0, -12.0, -19.0, 1.4]],
      band: [-16.3, -15.3], channels: [2.6], stripes: [-40], neck: { z: [-10, -14, -28, -32], hb: [2.4, 4.0, 4.0, 2.8], top: -6.0, bottom: -11.5 } },
    // The nacelle pylons come from the primary hull's waist, level and swept as the Tern's, with the secondary hull clear
    // beneath (owner, 2026-11-03: "can we try placing pylons attached to the main hull instead?", then "I asked for the
    // albatross pylons to be connected to the main hull instead"). The earlier pylons out of the secondary hull are gone.
    pylons: [{ root: [14.5, 0.6, -14], tip: [25, 0.6, -26], rc: 18, tc: 11, t: 1.5 }],
    nacelles: [{ x: 25, y: 0.6, z: -24, len: 64, hw: 2.8, hh: 2.7 }],
    radiators: [[-22, 6.5, 10], [-22, -6.5, 10]], turrets: [[0, 27, 'cmd'], [11.5, -6, 'top'], [-11.5, -6, 'top'], [9, 22, 'bottom'], [-9, 22, 'bottom']], bells: { z: -58, y: -15.6, x: 2.4, r: 1.7 },
    core: { pos: [0, -20.9, -30], size: [2.2, 0.5, 30] }, torpedo: [0, -1.8, 50.05], deflector: [0, 1.0, 50.05, 2.6],
  },
  // The battleship (owner, 2026-11-03: "we need 2 additional ship classes: a battle ship and a carrier", "please make these
  // somewhat different than the standard ship shape ... battleship needs an array of cannons", "large cannons"): a long armoured
  // hull round a tall citadel, three triple-gun turrets forward stepped up so each fires over the one ahead, two aft facing aft,
  // and a sponson down each flank carrying a row of broadside cannons; two nacelle pairs aft.
  condor: {
    cls: 'Condor-class battleship', name: 'CNS CONDOR', reg: 'CC-001', length: 184, tell: 'A long armoured hull round a tall citadel: four main guns, two on the back (the second raised to fire over the first) and two under the keel, fifteen point-defence cannons along its shoulders, and two nacelle pairs.',
    stations: [[92, 3.0, 4.0, -3.0, 1.0], [74, 12.0, 8.5, -7.0, 2.2], [40, 17.0, 10.5, -9.0, 2.8], [-40, 17.0, 10.5, -9.0, 2.8], [-78, 14.0, 9.0, -7.5, 2.6], [-92, 10.0, 7.0, -5.5, 2.0]],
    decks: [7.0, 3.5, 0, -3.5, -7.0], live: [-40, 50], band: [-0.65, 0.35], channels: [5.0, 12.0], stripes: [60, -32, -66],
    command: { z: [24, -6], hb: 9.0, top: 18.0, ch: 1.6 },
    // Four main guns (owner, 2026-11-03: "battleship only has 4 main cannons, 2 on top and 2 on bottom, the remainder should be
    // pdcs"): two on the back, the second raised to fire over the first, and the same two mirrored under the keel.
    guns: [{ z: 62, face: 1, lift: 0 }, { z: 46, face: 1, lift: 3.0 }, { z: 62, face: 1, lift: 0, under: true }, { z: 46, face: 1, lift: 3.0, under: true }],
    pdc: true,
    pylons: [{ root: [16, 2.0, -48], tip: [30, 2.0, -60], rc: 24, tc: 15, t: 1.8 }, { root: [12, -8.0, -40], tip: [24, -17.0, -52], rc: 20, tc: 12, t: 1.6 }],
    nacelles: [{ x: 30, y: 2.0, z: -54, len: 80, hw: 3.2, hh: 3.1 }, { x: 24, y: -17.0, z: -48, len: 64, hw: 2.6, hh: 2.6 }],
    radiators: [[-66, 6, 12], [-66, -6, 12]], turrets: [[9, 70, 'top'], [-9, 70, 'top'], [12, 20, 'top'], [-12, 20, 'top'], [12, -20, 'top'], [-12, -20, 'top'], [11, -52, 'top'], [-11, -52, 'top'], [0, -70, 'top'], [12, 30, 'bottom'], [-12, 30, 'bottom'], [12, -10, 'bottom'], [-12, -10, 'bottom'], [11, -50, 'bottom'], [-11, -50, 'bottom']],
    bells: { z: -92, y: 0.6, x: 5.0, r: 2.6 }, core: { pos: [0, -9.45, -10], size: [2.4, 0.5, 40] }, torpedo: [0, -1.6, 92.05], deflector: [0, 1.4, 92.05, 1.6],
  },
  // The carrier (owner, 2026-11-03: "carrier needs bays for fighters", then "the carrier doesnt need a landing pad, this is
  // space ... it has doors on each side, its a diamond cross section, with the main ship crew area in front"): the crew hull
  // forward, as the family's, and behind it a long hangar hull of diamond cross section (broad sloped flanks, a narrow vertical
  // band at the waist), three numbered bay doors down each side lit by lamps above them, the nacelle pair on level pylons aft.
  pelican: {
    cls: 'Pelican-class carrier', name: 'CNS PELICAN', reg: 'CC-040', length: 128, tell: 'The crew hull forward, as the family\'s, and behind it a long hangar hull of diamond cross section, three numbered fighter bay doors down each side under their own lamps, the nacelle pair on level pylons from the hangar hull.',
    stations: [[66, 4.0, 4.0, -3.0, 1.2], [58, 10.0, 6.5, -5.0, 1.8], [40, 13.0, 7.0, -5.6, 2.0], [20, 12.0, 6.6, -5.2, 2.0]],
    secondary: { stations: [[24, 9.0, 7.0, -7.0, 6.6], [14, 13.0, 9.0, -9.0, 8.6], [-38, 13.0, 9.0, -9.0, 8.6], [-62, 9.0, 6.0, -6.0, 5.6]], band: [-0.65, 0.35], channels: [], stripes: [-50] },
    decks: [3.5, 0, -3.5], live: [24, 56], band: [-0.65, 0.35], channels: [4.5], stripes: [48],
    command: { z: [56, 40], hb: 6.0, top: 10.0, ch: 1.2 },
    pylons: [{ root: [10, 1.0, -36], tip: [27, 1.0, -48], rc: 20, tc: 12, t: 1.5 }],
    nacelles: [{ x: 27, y: 1.0, z: -38, len: 66, hw: 2.6, hh: 2.5 }],
    radiators: [[-50, 2.4, 8], [-50, -2.4, 8]], turrets: [[0, 30, 'top'], [0, -20, 'top'], [0, 0, 'bottom'], [8, 46, 'bottom'], [-8, 46, 'bottom']],
    bells: { z: -62, y: 0.0, x: 3.0, r: 2.0 }, core: { pos: [0, -9.45, -10], size: [2.0, 0.5, 36] }, torpedo: [0, -1.0, 66.05], deflector: [0, 1.4, 66.05, 1.4],
    carrier: { doors: [6, -10, -26], door: { w: 12.0, h: 4.6 } },
  },
  // Star Crew's SCS Tern, from its own data (star-crew-bridge-simulator 95a49b2: data/ships/tern/layout.json hull.sections,
  // data/ships/exteriors.json ships[tern] pylon, nacelle and service_band). The yardstick for the family; not a Concord ship.
  tern: {
    cls: 'Heron-class frigate, Star Crew', name: 'SCS TERN', reg: 'SC-084', length: 86, ref: true, livery: 'copper', glb: true, tell: 'Star Crew\'s player ship from its own data, at true scale: the family the Concord line is built in.',
    stations: [[42, 1.2, 2.2, -0.8, 0.4], [37, 4.5, 4.5, -2.8, 1.5], [32, 8.6, 7.9, -4.6, 1.8], [20, 10.8, 7.8, -5.4, 1.6], [8, 12.2, 7.8, -5.6, 1.6], [-2, 12.2, 8.0, -5.8, 2.2], [-18, 11.6, 8.2, -5.8, 2.2], [-32, 10.6, 8.2, -5.2, 2.0], [-38, 8.0, 6.0, -3.6, 1.8], [-42, 6.5, 4.6, -2.6, 1.4]],
    decks: DECKS, live: [-6, 26], band: [-0.65, 0.35], channels: [5.2], stripes: [16, -17],
    pylons: [{ root: [10, 1.2, -15], tip: [20, 1.2, -24], rc: 16, tc: 10, t: 1.2 }],
    nacelles: [{ x: 20, y: 1.2, z: -16, len: 56, hw: 2.3, hh: 2.2 }],
    radiators: [[-30, 3.1, 8.0], [-30, -3.1, 8.0]], turrets: [[0, 8, 'top'], [0, 4, 'bottom']], bells: { z: -42, y: 1.0, x: 3.0, r: 1.5 },
    core: { pos: [0, -5.65, -8], size: [1.8, 0.5, 24] }, torpedo: [0, -0.4, 39.0], deflector: [0, 1.2, 36.0, 1.6],
  },
};
// Nacelle variants (owner, 2026-11-03: "swift having 1 below would be better, petrel having 1 above and below", then "instead of
// opptions. please just apply the variants for both petrel and swift and include horizontal aligned ones aas well", then
// "remove petrel with 1 pylon"). One below: one nacelle under the centreline on an upright pylon. Above and below: one over and
// one under. Level: the family's nacelle pair on level swept pylons.
const VARIANTS = [
  ['swift_below', 'swift', 'below1', 'Swift, one below'], ['swift_updown', 'swift', 'updown', 'Swift, above and below'], ['swift_level', 'swift', 'level', 'Swift, level'],
  ['petrel_updown', 'petrel', 'updown', 'Petrel, above and below'], ['petrel_level', 'petrel', 'level', 'Petrel, level'],
  ['albatross', 'albatross', 'level', 'Albatross'], ['condor', 'condor', 'level', 'Condor battleship'], ['pelican', 'pelican', 'level', 'Pelican carrier'], ['tern', 'tern', 'level', 'Tern (Star Crew)'],
];
const MODE_NAMES = { level: 'Level pair', below1: 'One below', updown: 'One above, one below' };
const CLASSES = {};
for (const [id, base, mode, label] of VARIANTS) CLASSES[id] = { ...BASES[base], id, base, mode, label,
  tell: BASES[base].tell + (base === 'swift' || base === 'petrel' ? ' Nacelles: ' + MODE_NAMES[mode].toLowerCase() + '.' : '') };
// Liveries: Concord is ours; Copper, Cyan and Rescue are the Tern's (exteriors.json liveries), for comparison.
const LIVERIES = {
  concord: { label: 'Concord', stripe: [0.10, 0.40, 0.86], seam: [0.04, 0.08, 0.22] },
  copper: { label: 'Copper', stripe: [0.82, 0.31, 0.09], seam: [0.30, 0.12, 0.05] },
  cyan: { label: 'Cyan', stripe: [0.008, 0.655, 0.851], seam: [0.03, 0.20, 0.28] },
  rescue: { label: 'Rescue', stripe: [1.0, 0.75, 0.05], seam: [0.86, 0.15, 0.09] },
};
const TERN_NACELLE_PROFILE = [[-0.5, 0.48], [-0.44, 0.68], [-0.29, 1.0], [0.44, 1.0], [0.5, 0.9]]; // exteriors.json nacelle.profile_t_scale

// Station arithmetic: the chamfer is clamped the same way everywhere (geometry, inside tests, the cut-face shader).
const chOf = (hb, top, bot, ch) => Math.min(ch, hb - 0.3, (top - bot) / 2 - 0.3); // a large chamfer makes a diamond with narrow flats
function hullAt(S, z) {
  const s = [...S].sort((a, b) => b[0] - a[0]);
  if (z >= s[0][0]) return s[0]; if (z <= s[s.length - 1][0]) return s[s.length - 1];
  for (let i = 0; i + 1 < s.length; i++) if (z <= s[i][0] && z >= s[i + 1][0]) { const t = (s[i][0] - z) / (s[i][0] - s[i + 1][0]); return s[i].map((v, j) => lerp(v, s[i + 1][j], t)); }
}
// Signed distance into a lofted hull (positive inside), the octagon at z: four sides and four 45 degree chamfers.
function loftDist(S, p) {
  const zs = S.map((q) => q[0]), z0 = Math.min(...zs), z1 = Math.max(...zs);
  if (p.z > z1 || p.z < z0) return -1;
  const [, hb, top, bot, c] = hullAt(S, p.z), ch = chOf(hb, top, bot, c), ax = Math.abs(p.x);
  return Math.min(hb - ax, top - p.y, p.y - bot, (hb + top - ch - ax - p.y) * Math.SQRT1_2, (hb - bot - ch - ax + p.y) * Math.SQRT1_2, z1 - p.z, p.z - z0);
}

// Nacelle arrangements. level: the data's own pylons and nacelles. below1: one nacelle on the centreline under the hull on one
// upright pylon. updown: one over the hull and one under it. GAP_UP is the daylight to the nearest hull along the nacelle.
const GAP_UP = 5.5; // metres (owner, 2026-11-03: "would also be nice for the vertical pyons to extend out a little further")
const hullsOf = (C) => [C.stations].concat(C.secondary ? [C.secondary.stations] : []);
function alongNacelle(C, n) {
  let top = -Infinity, bot = Infinity;
  for (const st of hullsOf(C)) { const zs = st.map((q) => q[0]), zMax = Math.max(...zs), zMin = Math.min(...zs);
    for (let z = Math.max(zMin, n.z - n.len / 2); z <= Math.min(zMax, n.z + n.len / 2); z += 0.5) { const h = hullAt(st, z); top = Math.max(top, h[2]); bot = Math.min(bot, h[3]); } }
  return { top, bot };
}
// Each nacelle gets a side key: P port, S starboard, D dorsal, V ventral; its system is 'nac' + side.
function arrange(C) {
  const sfx = (i) => (i ? String(i + 1) : ''); // a second pair is P2 and S2
  if (C.mode === 'level') return { pylons: C.pylons.flatMap((p, i) => [{ ...p, side: 'P' + sfx(i), sx: 1 }, { ...p, side: 'S' + sfx(i), sx: -1 }]), nacelles: C.nacelles.flatMap((n, i) => [{ ...n, side: 'P' + sfx(i), sx: 1 }, { ...n, side: 'S' + sfx(i), sx: -1 }]) };
  const n0 = C.nacelles[0], p0 = C.pylons[0], pylons = [], nacelles = [], { top, bot } = alongNacelle(C, n0);
  const zs = [n0.z - n0.len * 0.15, n0.z - n0.len * 0.35], at = hullAt(C.stations, zs[0]);
  const upright = (side, y, rootY) => { nacelles.push({ ...n0, x: 0, y, side, sx: 1 }); pylons.push({ root: [0, rootY, zs[0] + 3], tip: [0, y, zs[1]], rc: p0.rc, tc: p0.tc, t: p0.t, up: true, side, sx: 1 }); };
  if (C.mode === 'updown') upright('D', top + GAP_UP + n0.hh, at[2] - 0.4);
  upright('V', bot - GAP_UP - n0.hh, at[3] + 0.4);
  return { pylons, nacelles };
}
const SIDE_NAMES = { P: 'Port nacelle', S: 'Starboard nacelle', D: 'Dorsal nacelle', V: 'Ventral nacelle' };
const sideName = (k) => SIDE_NAMES[k[0]] + (k.length > 1 ? ' ' + k.slice(1) : '');

// ---------------------------------------------------------------------------------------------------------------
// Geometry. Every piece is built in ship space (the mesh sits at identity inside its piece), so the damage shader's
// object-space position is ship space even after a piece breaks away. Flat faces, each triangle turned to face away from
// its centre line so winding never matters (the shader uses gl_FrontFacing for the inside of the skin).
function pushTri(out, a, b, c, centre) {
  const n = V3().subVectors(b, a).cross(V3().subVectors(c, a)), m = V3().addVectors(a, b).add(c).multiplyScalar(1 / 3);
  if (n.dot(m.sub(centre(m))) < 0) out.push(a, c, b); else out.push(a, b, c);
}
function geoFrom(tris) {
  const g = new THREE.BufferGeometry().setFromPoints(tris); g.computeVertexNormals();
  g.setIndex(Array.from({ length: tris.length }, (_, i) => i)); return g;
}
function octRing(z, hb, top, bot, ch) {
  ch = chOf(hb, top, bot, ch);
  return [V3(hb - ch, top, z), V3(hb, top - ch, z), V3(hb, bot + ch, z), V3(hb - ch, bot, z), V3(-(hb - ch), bot, z), V3(-hb, bot + ch, z), V3(-hb, top - ch, z), V3(-(hb - ch), top, z)];
}
// A loft through rings; long spans are split every `step` metres so the damage shader has vertices to work with.
function loft(rings, axis, step = 4) {
  const R = [rings[0]];
  for (let i = 1; i < rings.length; i++) { const a = rings[i - 1], b = rings[i], k = Math.max(1, Math.ceil(Math.abs(a[0].z - b[0].z) / step));
    for (let j = 1; j <= k; j++) R.push(a.map((p, q) => p.clone().lerp(b[q], j / k))); }
  const t = [];
  for (let i = 0; i + 1 < R.length; i++) for (let k = 0; k < 8; k++) {
    const a = R[i][k], b = R[i][(k + 1) % 8], c = R[i + 1][(k + 1) % 8], d = R[i + 1][k];
    pushTri(t, a, b, c, axis); pushTri(t, a, c, d, axis);
  }
  for (const [r, nb] of [[R[0], R[1]], [R[R.length - 1], R[R.length - 2]]]) {
    const cen = r.reduce((s, p) => s.add(p), V3()).multiplyScalar(1 / 8), out = V3(0, 0, Math.sign(r[0].z - nb[0].z));
    for (let k = 0; k < 8; k++) pushTri(t, cen, r[k], r[(k + 1) % 8], (m) => m.clone().sub(out));
  }
  return geoFrom(t);
}
function hullGeo(S, ox = 0) {
  if (ox) { const g = hullGeo(S); g.translate(ox, 0, 0); return g; }
  const s = [...S].sort((a, b) => b[0] - a[0]);
  return loft(s.map(([z, hb, top, bot, ch]) => octRing(z, hb, top, bot, ch)), (m) => { const h = hullAt(S, m.z); return V3(0, (h[2] + h[3]) / 2, m.z); });
}
function nacelleGeo(n, x, y) {
  const ch = Math.min(n.hw, n.hh) * 0.42, rings = [];
  for (const [t, k] of TERN_NACELLE_PROFILE) rings.push(octRing(n.z - t * n.len, n.hw * k, n.hh * k, -n.hh * k, ch * k).map((p) => p.add(V3(x, y, 0))));
  rings.reverse();
  return loft(rings, (m) => V3(x, y, m.z), 5);
}
// A pylon from root to tip, split at `f0..f1` of its length (0..0.5 the inner half, 0.5..1 the outer). A level pylon is thick
// in Y; an upright one is thick in X.
function pylonGeo(p, sx, f0 = 0, f1 = 1) {
  const r = V3(p.root[0] * sx, p.root[1], p.root[2]), tp = V3(p.tip[0] * sx, p.tip[1], p.tip[2]), h = p.t / 2;
  const A = r.clone().lerp(tp, f0), B = r.clone().lerp(tp, f1), ca = lerp(p.rc, p.tc, f0), cb = lerp(p.rc, p.tc, f1);
  const off = p.up ? V3(h, 0, 0) : V3(0, h, 0);
  const P = [A.clone().add(V3(0, 0, ca / 2)), A.clone().add(V3(0, 0, -ca / 2)), B.clone().add(V3(0, 0, -cb / 2)), B.clone().add(V3(0, 0, cb / 2))].map((v) => v.add(off));
  const Q = P.map((v) => v.clone().sub(off).sub(off));
  const c = P.concat(Q).reduce((s, v) => s.add(v), V3()).multiplyScalar(1 / 8), t = [];
  const quad = (a, b, cc, d) => { pushTri(t, a, b, cc, () => c); pushTri(t, a, cc, d, () => c); };
  quad(P[0], P[1], P[2], P[3]); quad(Q[0], Q[1], Q[2], Q[3]);
  for (let i = 0; i < 4; i++) quad(P[i], P[(i + 1) % 4], Q[(i + 1) % 4], Q[i]);
  return geoFrom(t);
}
function boxGeo(sx, sy, sz, x, y, z) { const g = new THREE.BoxGeometry(sx, sy, sz); g.translate(x, y, z); return g; }

// ---------------------------------------------------------------------------------------------------------------
// A plain model for pages that show the ship small (the battle console's view, the game flow): the same hulls, neck,
// command block, pylons and nacelles as mockup 6, in pearl with the livery's stripes, lit grilles, drive bells and the
// deflector. Units are metres, +Z the bow; userData.length_m is the class's length; every mesh's userData.part names its
// component (primary, secondary, neck, command, strut, nacelle, drive, deflector) for the Ship Design Bible's exploded view.
function plainModel(id, o = {}) {
  const C = CLASSES[id]; if (!C) throw new Error('FCConcordLine: no class ' + id);
  const L = LIVERIES[o.livery || 'concord'], g = new THREE.Group(), A = arrange(C);
  const pearl = new THREE.MeshStandardMaterial({ color: o.hull ?? 0xe4e2dc, metalness: 0.35, roughness: 0.5, flatShading: true });
  const stripe = new THREE.MeshStandardMaterial({ color: new THREE.Color(...L.stripe), metalness: 0.3, roughness: 0.5, flatShading: true });
  const glow = (hex) => new THREE.MeshStandardMaterial({ color: 0x10141c, emissive: hex, emissiveIntensity: 1.6 });
  const mesh = (geo, m, part) => { const k = new THREE.Mesh(geo, m); k.userData.part = part; g.add(k); return k; };
  const bands = (S, zs, part) => { for (const z of zs || []) { const zz = S.map((q) => q[0]); if (z > Math.max(...zz) - 1 || z < Math.min(...zz) + 1) continue;
    const ring = (dz) => { const h = hullAt(S, z + dz); return octRing(z + dz, h[1] + 0.06, h[2] + 0.06, h[3] - 0.06, h[4]); };
    mesh(loft([ring(1.2), ring(-1.2)], (m) => { const h = hullAt(S, m.z); return V3(0, (h[2] + h[3]) / 2, m.z); }), stripe, part); } };
  mesh(hullGeo(C.stations), pearl, 'primary'); bands(C.stations, C.stripes, 'primary');
  if (C.secondary) { const D = C.secondary; mesh(hullGeo(D.stations), pearl, 'secondary'); bands(D.stations, D.stripes, 'secondary');
    if (D.neck) { const N = D.neck; mesh(loft(N.z.map((z, i) => octRing(z, N.hb[i], N.top, N.bottom, Math.min(1.2, N.hb[i] * 0.4))), (m) => V3(0, (N.top + N.bottom) / 2, m.z)), pearl, 'neck'); } }
  if (C.command) { const K = C.command, [z0, z1] = K.z, base = hullAt(C.stations, (z0 + z1) / 2)[2] - 0.6;
    mesh(loft([octRing(z0, K.hb * 0.62, K.top - 0.8, base, K.ch), octRing(z0 - 3, K.hb, K.top, base, K.ch), octRing(z1, K.hb, K.top, base, K.ch), octRing(z1 - 2.5, K.hb * 0.8, K.top - 1.2, base, K.ch)], (m) => V3(0, (K.top + base) / 2, m.z), 3), pearl, 'command');
    mesh(boxGeo(K.hb * 1.12, 0.7, 0.25, 0, K.top - 1.3, z0 - 1.45), glow(0xd8ecff), 'command'); }
  for (const p of A.pylons) mesh(pylonGeo(p, p.sx), pearl, 'strut');
  for (const n of A.nacelles) { const x = n.x * n.sx; mesh(nacelleGeo(n, x, n.y), pearl, 'nacelle');
    for (const s2 of [1, -1]) mesh(boxGeo(0.14, n.hh * 0.32, n.len * 0.72, x + s2 * (n.hw + 0.02), n.y, n.z), glow(0x2fb8ff), 'nacelle');
    const cap = new THREE.CircleGeometry(n.hh * 0.38, 16); cap.translate(x, n.y, n.z + n.len / 2 + 0.03); mesh(cap, glow(0xff6a1f), 'nacelle'); }
  { const b = C.bells; for (const sx of b.x ? [1, -1] : [1]) { const d = new THREE.CircleGeometry(b.r, 20); d.rotateY(Math.PI); d.translate(sx * b.x, b.y, b.z - 0.05); mesh(d, glow(0xffb070), 'drive'); } }
  { const [x, y, z, r] = C.deflector; mesh(boxGeo(r * 2, r * 1.1, 0.25, x, y, z), glow(0x6fc6ff), 'deflector'); }
  g.userData.length_m = C.length; g.userData.cls = C.cls;
  return g;
}

return { COL, DECKS, BASES, VARIANTS, MODE_NAMES, CLASSES, LIVERIES, TERN_NACELLE_PROFILE, GAP_UP, SIDE_NAMES,
  chOf, hullAt, loftDist, hullsOf, alongNacelle, arrange, sideName, pushTri, geoFrom, octRing, loft, hullGeo, nacelleGeo, pylonGeo, boxGeo, plainModel };
};
