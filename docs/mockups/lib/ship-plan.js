// Fleet Command mockups: a ship's rooms as blocks, and the hull fitted round them (the Ship Design Bible's method, chapters 2
// and 3; owner, 2026-11-03: "map out approximate floor plans out of blocks, and enclose the structure with the hull shape").
// One implementation (CLAUDE.md section 6.1) shared by the guide (docs/design/ship-bible.html) and every faction's ship page
// (docs/mockups/krai-ship-concepts.html first). Edit it here, then re-run tools/mockups/inline_data.py on each page.
//
// FCShipPlan(THREE, CL) takes three and the Concord line module (for its loft geometry and loftDist) and returns:
//   rooms(plan)                the plan's rooms with their boxes in metres (x0 x1 y0 y1 z0 z1)
//   check(rooms)               P1: the overlaps and rooms off their corridor, as a list of failures
//   fit(plan, rooms, opts)     H1: hull stations [z, half beam, top, bottom, chamfer] clearing every room by the plan's
//                              clearance plus structure; opts.section 'octagon' (Concord) or 'blade' (chamfers as deep as the rooms allow)
//   margins(rooms, stations)   each room's least distance inside a hull (loftDist over its eight corners)
//   windows(plan, rooms, stations, lit)  W1, W2: person-sized windows where a lived-in room meets the hull's side, one row
//                              a deck 1.5 m above its floor, as { x, y, z, side, tilt } (tilt: the side's lean, radians)
//   planSVGs(plan, rooms, kinds)  the deck plans as SVG drawings, one per deck
window.FCShipPlan = function FCShipPlan(THREE, CL) {
'use strict';
const V3 = (x = 0, y = 0, z = 0) => new THREE.Vector3(x, y, z);
const clear = (plan) => plan.clearance_m + plan.structure_m;

function rooms(plan) {
  const F = plan.deck_floor_m, H = plan.headroom_m;
  return plan.rooms.map((r) => ({ ...r, b: { x0: r.x_m[0], x1: r.x_m[1], z0: r.z_m[0], z1: r.z_m[1],
    y0: Math.min(...r.decks.map((d) => F[d])), y1: Math.max(...r.decks.map((d) => F[d])) + H } }));
}

function check(R) {
  const bad = [], hit = (a, b, e = 0) => a.b.x0 < b.b.x1 + e && b.b.x0 < a.b.x1 + e && a.b.z0 < b.b.z1 + e && b.b.z0 < a.b.z1 + e;
  for (let i = 0; i < R.length; i++) for (let j = i + 1; j < R.length; j++)
    if (R[i].decks.some((d) => R[j].decks.includes(d)) && hit(R[i], R[j])) bad.push(`${R[i].id}/${R[j].id} overlap`);
  for (const r of R) if (r.kind !== 'corridor' && !R.some((c) => c.kind === 'corridor' && c.decks.some((d) => r.decks.includes(d)) && hit(r, c, 0.01)))
    bad.push(`${r.id} off its corridor`);
  return bad;
}

// Every 2 m along the ship (and on the first room's front face) the hull clears the rooms at that station by C.
// Half beam, top and bottom each clear by C, and the 45 degree chamfers are cut as deep as the room corners allow:
// octagon (the Concord's) takes 0.45 of the section's half size at most; blade takes all it can, so the section is as
// close to a blade's diamond as the rooms let it be.
// The bow tapers over nose_m (kept above the keel line), the stern is square tail_m past the last room, and a
// three-station running maximum keeps the hull from hugging every step of the plan.
function fit(plan, R, opts = {}) {
  const C = clear(plan), section = opts.section || 'octagon', st = [];
  const zMin = Math.min(...R.map((r) => r.b.z0)), zMax = Math.max(...R.map((r) => r.b.z1));
  const at = (z) => R.filter((r) => r.b.z0 <= z + 1 && r.b.z1 >= z - 1);
  const zs = []; for (let z = zMin - plan.tail_m; z < zMax; z += 2) zs.push(z); zs.push(zMax);
  for (const z of zs) {
    const rs = at(Math.min(Math.max(z, zMin), zMax)); if (!rs.length) continue;
    const ax = Math.max(...rs.map((r) => Math.max(Math.abs(r.b.x0), Math.abs(r.b.x1)))), yT = Math.max(...rs.map((r) => r.b.y1)), yB = Math.min(...rs.map((r) => r.b.y0));
    const hb = ax + C, top = yT + C, bot = yB - C; let ch = Math.min(hb, (top - bot) / 2) * (section === 'blade' ? 1 : 0.45);
    for (const r of rs) { const a = Math.max(Math.abs(r.b.x0), Math.abs(r.b.x1)); ch = Math.min(ch, hb + top - (a + r.b.y1) - C * Math.SQRT2, hb - bot - (a - r.b.y0) - C * Math.SQRT2); }
    st.push([z, hb, top, bot, Math.max(0.3, ch)]);
  }
  const f = st[st.length - 1];
  for (const k of [0.5, 0.15]) st.push([f[0] + plan.nose_m * (k === 0.5 ? 0.6 : 1), f[1] * k, f[3] + (f[2] - f[3]) * (0.5 + k * 0.6), f[3] + (f[2] - f[3]) * (0.5 - k * 0.6), f[4] * k]);
  return st.map((s, i) => { const n = st.slice(Math.max(0, i - 1), i + 2); return i >= st.length - 2 ? s : [s[0], Math.max(...n.map((q) => q[1])), Math.max(...n.map((q) => q[2])), Math.min(...n.map((q) => q[3])), Math.min(...n.map((q) => q[4]))]; });
}

function margins(R, stations) {
  return R.map((r) => { let m = Infinity;
    for (const x of [r.b.x0, r.b.x1]) for (const y of [r.b.y0, r.b.y1]) for (const z of [r.b.z0, r.b.z1]) m = Math.min(m, CL.loftDist(stations, V3(x, y, z)));
    return { r, m }; });
}

// The hull's half width at height y on station h (an octagon: flat sides between the chamfers, the chamfers at 45
// degrees), and the lean of the surface there.
function sideAt(h, y) {
  const [, hb, top, bot, c] = h, ch = CL.chOf(hb, top, bot, c);
  if (y > top || y < bot) return null;
  if (y > top - ch) return { x: hb - (y - (top - ch)), tilt: Math.PI / 4 };
  if (y < bot + ch) return { x: hb - ((bot + ch) - y), tilt: -Math.PI / 4 };
  return { x: hb, tilt: 0 };
}
// A pane (1.2 m long, 0.75 m tall with its rim) lies flat on one face of the section (owner, 2026-11-04: "window geometry
// extending a bit over the hull slope, need to adjust slope start height"): the face at its middle must also be the face at
// its top and bottom edges and at both ends. Where it would cross a bend there is no window. The face must also stay one plane along the pane, so a pane where the hull
// tapers along its length is not set into the taper.
const PANE = { hu: 0.66, hv: 0.5 };
function paneY(stations, z, y0) {
  const fits = (y) => { const s = sideAt(CL.hullAt(stations, z), y); if (!s) return false;
    const vy = PANE.hv * Math.cos(s.tilt);
    for (const dz of [-PANE.hu, 0, PANE.hu]) for (const dy of [-vy, 0, vy]) { const t = sideAt(CL.hullAt(stations, z + dz), y + dy);
      if (!t || t.tilt !== s.tilt || Math.abs(t.x - (s.x - dy * Math.tan(s.tilt))) > 0.05) return false; } // one flat face: same slope, same plane
    return true; };
  return fits(y0) ? y0 : null; // a row stays level: a pane that does not fit at its row's height is left out, never moved
  // (owner, 2026-11-04: "remove these windows if they cannot be placed in an aligned fasion")
}
function windows(plan, R, stations, lit) {
  const C = clear(plan), F = plan.deck_floor_m, out = [];
  for (const r of R) { if (!lit[r.kind]) continue;
    for (const d of r.decks) { const y0 = F[d] + 1.5;
      for (let z = r.b.z0 + 1.2; z <= r.b.z1 - 1.2; z += 2.4) { const y = paneY(stations, z, y0); if (y === null) continue; const s = sideAt(CL.hullAt(stations, z), y);
        for (const side of [1, -1]) { const reach = side > 0 ? r.b.x1 : -r.b.x0;
          if (reach < s.x - C * 2 - 1.2) continue; // the room does not meet this side (allowing for a sloped side's offset)
          out.push({ x: side * (s.x + 0.03), y, z, side, tilt: s.tilt }); } } } }
  return out;
}

// Deck plans as SVG drawings from the same boxes: one per deck, bow right, port up. kinds maps a room kind to
// [label, colour]; X is the half width shown, in metres.
function planSVGs(plan, R, kinds, X = 12) {
  const F = plan.deck_floor_m, zMin = Math.min(...R.map((r) => r.b.z0)) - plan.tail_m, zMax = Math.max(...R.map((r) => r.b.z1)) + plan.nose_m;
  const W = 860, sc = W / (zMax - zMin), H = Math.round(2 * X * sc);
  return F.map((fy, d) => {
    let svg = `<svg viewBox="0 0 ${W} ${H}" role="img" aria-label="Deck ${String.fromCharCode(65 + d)} plan">`;
    for (const r of R) if (r.decks.includes(d)) { const c = kinds[r.kind][1];
      const x = (r.b.z0 - zMin) * sc, y = (X - r.b.x1) * sc, w = (r.b.z1 - r.b.z0) * sc, h = (r.b.x1 - r.b.x0) * sc;
      svg += `<rect x="${x.toFixed(1)}" y="${y.toFixed(1)}" width="${w.toFixed(1)}" height="${h.toFixed(1)}" fill="${c}" fill-opacity="0.8" stroke="#0b0f18" stroke-width="1.5"><title>${r.name}</title></rect>`;
      if (r.kind !== 'corridor') { const label = r.name.replace(/ \(.*\)/, ''), fit = Math.floor(w / 7.2);
        if (fit >= 4 && h > 14) svg += `<text x="${(x + w / 2).toFixed(1)}" y="${(y + h / 2 + 4).toFixed(1)}" font-size="12" fill="#0b0f18" text-anchor="middle" font-family="IBM Plex Mono">${label.length > fit ? label.slice(0, fit - 1) + '.' : label}</text>`; } }
    svg += `<text x="${W - 8}" y="16" font-size="12" fill="#cfd8ea" text-anchor="end" font-family="IBM Plex Mono">BOW</text><text x="8" y="16" font-size="12" fill="#cfd8ea" font-family="IBM Plex Mono">PORT</text></svg>`;
    return { svg, caption: `Deck ${String.fromCharCode(65 + d)}, floor ${fy.toFixed(2)} m: bow right, port up, 10 m is ${(10 * sc / W * 100).toFixed(1)}% of the width` };
  });
}

return { rooms, check, fit, margins, windows, sideAt, clear, planSVGs };
};
