// Fleet Command mockups: the room behind a window, drawn by interior mapping, shared by every faction's ship page (CLAUDE.md
// 6.1: one implementation). Lifted from the Concord line (docs/mockups/concord-hull-concepts.html, mockup 6, owner,
// 2026-11-03: "make the interiors more interesting? like a room with furniture, and sci fi wall panels? carpet? maybe a plant
// in the corner?") so the Ssarith Hunt's windows (docs/mockups/ssarith-ship-concepts.html) show the same rooms (owner,
// 2026-11-05: "we need to show interior of ships with windows"). Edit it here, then re-run tools/mockups/inline_data.py on
// each page that inlines it.
//
// window.FCShipRooms.GLSL defines
//   vec3 roomBehind(vec3 q, vec3 D, vec3 room, int kindIn, vec3 LIn)
// q: the point on the pane in metres (x along the pane, y up, 0 at its centre, the floor 1.5 m below), D: the view ray in the
// pane's frame (z into the room, normalised), room: any vec3 naming the room (hashed for its layout, carpet and light),
// kindIn: 0 quarters, 1 mess, 2 office, or -1 to pick from the hash, LIn: the room's light colour, or x < 0 to pick from the
// hash (yellow, orange or dark). Returns the light leaving the pane.
//   vec3 hangarBehind(vec3 q, vec3 D, vec3 room, vec2 mouth, vec3 L)
// the hangar behind a launch bay's mouth (mouth: its width and height in metres, q at its centre): a deck 28 m deep with a
// centre stripe, three small craft parked in a row, rows of lights in the ceiling, ribbed walls, a lit back wall, in the
// bay's light L (owner, 2026-11-05: "we need a carrier"; kept painted
// over geometry for its parallax: "I actually like the paralax so I'd keep it").
// Both need ph1 (FCShipSkin GLSL.hash) and bHit (GLSL.box) defined before them.
window.FCShipRooms = (function () {
'use strict';
const GLSL = `
vec3 roomBehind(vec3 q, vec3 D, vec3 room, int kindIn, vec3 LIn){
  float r = ph1(room.xy + room.z*17.3), r2 = ph1(room.yx*3.1 + 9.0 + room.z), r3 = ph1(room.xy*7.7 + 2.0);
  float xa = -2.0 + (r2 - 0.5)*0.8, xb = xa + 4.0, DZ = 3.2; // the room, centred on its window, 3.2 m deep
  vec3 lo = vec3(xa, -1.5, 0.0), hi = vec3(xb, 1.5, DZ);
  vec3 tA = (mix(lo, hi, step(0.0, D)) - q)/D; float t = min(tA.x, min(tA.y, tA.z));
  vec3 n = t == tA.x ? vec3(-sign(D.x), 0.0, 0.0) : t == tA.y ? vec3(0.0, -sign(D.y), 0.0) : vec3(0.0, 0.0, -1.0);
  int kind = kindIn >= 0 ? kindIn : r3 < 0.45 ? 0 : r3 < 0.72 ? 1 : 2; int obj = 0; // 0 shell; 1 bed; 2 pillow; 3 blanket; 4 desk, shelf, counter; 5 screen; 6 pot; 7 plant; 8 table; 9 chair; 10 picture
  vec3 nb; float tb; float o = (r - 0.5)*0.6; // the back wall's layout slides a little room to room
  // A plant: on a shelf at eye level in quarters and offices, standing tall in the corner by the window in a mess.
  vec3 pc = kind == 1 ? vec3(xb - 0.45, -1.5, 0.55) : vec3(o + 0.85, -0.42, DZ - 0.25);
  float potH = kind == 1 ? 0.5 : 0.24, leaf = kind == 1 ? 0.42 : 0.26;
  tb = bHit(q, D, pc + vec3(-0.16, 0.0, -0.16), pc + vec3(0.16, potH, 0.16), nb, t); if (tb < t) { t = tb; n = nb; obj = 6; }
  for (int i = 0; i < 3; i++) { vec3 c = pc + vec3((float(i) - 1.0)*leaf*0.45, potH + leaf*(0.75 + 0.35*float(i % 2)), (float(i % 2) - 0.5)*leaf*0.4), oc = q - c;
    float bq = dot(oc, D), cq = dot(oc, oc) - leaf*leaf*0.55; float hq = bq*bq - cq;
    if (hq > 0.0) { float ts = -bq - sqrt(hq); if (ts > 0.0 && ts < t) { t = ts; n = normalize(q + D*ts - c); obj = 7; } } }
  if (kind != 1) { // a shelf under the plant, and a picture or a screen beside it at eye level
    tb = bHit(q, D, vec3(o + 0.3, -0.46, DZ - 0.42), vec3(o + 1.4, -0.42, DZ), nb, t); if (tb < t) { t = tb; n = nb; obj = 4; }
    tb = bHit(q, D, vec3(o - 0.95, -0.35, DZ - 0.04), vec3(o + 0.15, 0.35, DZ), nb, t); if (tb < t) { t = tb; n = nb; obj = kind == 2 ? 5 : 10; }
  }
  if (kind == 0) {        // quarters: a bed along the back wall, a pillow, a blanket
    tb = bHit(q, D, vec3(xa, -1.5, DZ - 1.0), vec3(xa + 2.0, -1.0, DZ), nb, t); if (tb < t) { t = tb; n = nb; obj = 1; }
    tb = bHit(q, D, vec3(xa + 0.05, -1.0, DZ - 0.9), vec3(xa + 0.5, -0.84, DZ - 0.1), nb, t); if (tb < t) { t = tb; n = nb; obj = 2; }
    tb = bHit(q, D, vec3(xa + 0.7, -1.03, DZ - 1.02), vec3(xa + 2.02, -0.95, DZ), nb, t); if (tb < t) { t = tb; n = nb; obj = 3; }
  } else if (kind == 1) { // mess: a counter and wall cabinets along the back, a table and chairs
    tb = bHit(q, D, vec3(xa, -1.5, DZ - 0.6), vec3(xb, -0.6, DZ), nb, t); if (tb < t) { t = tb; n = nb; obj = 4; }
    tb = bHit(q, D, vec3(xa, 0.15, DZ - 0.35), vec3(xb - 1.0, 0.75, DZ), nb, t); if (tb < t) { t = tb; n = nb; obj = 4; }
    float cx = o*0.5;
    tb = bHit(q, D, vec3(cx - 0.8, -0.78, 1.0), vec3(cx + 0.8, -0.72, 1.9), nb, t); if (tb < t) { t = tb; n = nb; obj = 8; }
    tb = bHit(q, D, vec3(cx - 0.08, -1.5, 1.35), vec3(cx + 0.08, -0.78, 1.55), nb, t); if (tb < t) { t = tb; n = nb; obj = 8; }
    for (int i = 0; i < 2; i++) { float fx = cx + (float(i) - 0.5)*0.9;
      tb = bHit(q, D, vec3(fx - 0.2, -1.5, 2.05), vec3(fx + 0.2, -1.05, 2.45), nb, t); if (tb < t) { t = tb; n = nb; obj = 9; }
      tb = bHit(q, D, vec3(fx - 0.2, -1.05, 2.4), vec3(fx + 0.2, -0.45, 2.46), nb, t); if (tb < t) { t = tb; n = nb; obj = 9; } }
  } else {                // office: a desk under the screen, a chair with its back to the window
    tb = bHit(q, D, vec3(o - 1.2, -0.78, DZ - 0.8), vec3(o + 0.4, -0.72, DZ), nb, t); if (tb < t) { t = tb; n = nb; obj = 4; }
    tb = bHit(q, D, vec3(o - 1.15, -1.5, DZ - 0.75), vec3(o - 1.05, -0.78, DZ - 0.05), nb, t); if (tb < t) { t = tb; n = nb; obj = 4; }
    tb = bHit(q, D, vec3(o - 0.65, -1.5, DZ - 1.6), vec3(o - 0.15, -1.0, DZ - 1.1), nb, t); if (tb < t) { t = tb; n = nb; obj = 9; }
    tb = bHit(q, D, vec3(o - 0.65, -1.0, DZ - 1.66), vec3(o - 0.15, -0.3, DZ - 1.6), nb, t); if (tb < t) { t = tb; n = nb; obj = 9; }
  }
  vec3 h = q + D*t, col; float emit = 0.0;
  // the fine patterns (grooves, slats, weave, fibre) fade out as a pixel grows past them, so a room seen from afar is calm,
  // not speckled (owner, 2026-11-08: "z fighting in windows here": it was these patterns aliasing)
  float fp = length(fwidth(h)), fine = clamp(1.0 - (fp - 0.02)*25.0, 0.0, 1.0);
  vec3 carpet = r2 < 0.25 ? vec3(0.13, 0.17, 0.36) : r2 < 0.5 ? vec3(0.34, 0.09, 0.10) : r2 < 0.75 ? vec3(0.07, 0.27, 0.27) : vec3(0.26, 0.24, 0.12);
  vec3 accent = r < 0.5 ? vec3(0.25, 0.75, 1.0) : vec3(1.0, 0.62, 0.28);
  if (obj == 0) {
    if (n.y > 0.5) { // carpet: woven, a border, fibre noise
      float weave = 0.85 + fine*(0.15*step(0.5, fract(h.x*6.0))*step(0.5, fract(h.z*6.0)) + 0.06*ph1(floor(h.xz*40.0))) + (1.0 - fine)*0.08;
      float border = step(min(min(h.x - xa, xb - h.x), min(h.z, DZ - h.z)), 0.3);
      col = mix(carpet*weave, carpet*0.55, border);
    } else if (n.y < -0.5) { // ceiling with light panels
      vec2 cp = fract(vec2(h.x*0.5, h.z*0.5)); float panel = step(0.2, cp.x)*step(cp.x, 0.8)*step(0.25, cp.y)*step(cp.y, 0.75);
      col = vec3(0.32); emit = panel*1.6;
    } else { // sci-fi wall panels: 0.8 m bays, grooves, a dark dado, a light strip, the odd vent or hatch
      float u = abs(n.z) > 0.5 ? h.x : h.z, bay = floor(u/0.8), fu = fract(u/0.8), y = h.y + 1.5;
      float groove = max(step(fu, 0.04), step(0.96, fu)) + step(abs(y - 0.9), 0.03) + step(abs(y - 2.55), 0.03);
      col = y < 0.9 ? vec3(0.10, 0.11, 0.13) : vec3(0.34, 0.38, 0.44)*(0.85 + 0.15*ph1(vec2(bay, n.x + n.z*3.0)));
      if (ph1(vec2(bay, 4.0 + n.x)) > 0.78 && y > 1.2 && y < 2.2 && fu > 0.15 && fu < 0.85) col = mix(col, vec3(0.1), mix(0.3, step(0.5, fract(y*8.0)), fine)*0.6);   // vent
      col *= 1.0 - 0.6*clamp(groove, 0.0, 1.0)*fine;
      float strip = step(abs(y - 2.35), 0.04); emit = strip*2.2;
      
    }
  } else if (obj == 1) col = vec3(0.30, 0.31, 0.34);
  else if (obj == 2) col = vec3(0.9, 0.9, 0.88);
  else if (obj == 3) col = carpet*1.6 + 0.1;
  else if (obj == 4) col = vec3(0.22, 0.17, 0.13);
  else if (obj == 5) { col = vec3(0.02); emit = 1.0; }
  else if (obj == 6) col = vec3(0.55, 0.32, 0.18);
  else if (obj == 7) col = vec3(0.10, 0.36, 0.10)*(0.9 + fine*0.6*(ph1(floor(h.xy*14.0 + h.zx*9.0)) - 0.5));
  else if (obj == 8) col = vec3(0.45, 0.43, 0.40);
  else if (obj == 10) { vec2 pp = vec2(h.x - o, h.y); float frame = step(0.42, max(abs(pp.x + 0.4)/0.55*0.5, abs(pp.y)/0.35*0.5)); col = frame > 0.5 ? vec3(0.12) : mix(vec3(0.16, 0.22, 0.42), vec3(0.85, 0.55, 0.3), smoothstep(-0.2, 0.25, pp.y + 0.2*sin(pp.x*5.0 + r*9.0))); } // a picture: a planet's horizon
  else col = accent*0.45 + 0.08;
  // a room picked from the hash: one in eight dark, the rest well lit in warm lamplight, yellow or orange (owner,
  // 2026-11-06: "can we have the rooms in the windows be more yellow/orange tinted like they are well lit for all ships?")
  vec3 L = LIn.x >= 0.0 ? LIn : r < 0.12 ? vec3(0.02, 0.022, 0.03) : r < 0.4 ? vec3(1.0, 0.66, 0.32) : vec3(1.0, 0.82, 0.52);
  float shade = 0.45 + 0.55*max(n.y, 0.0) + 0.2*max(-n.z, 0.0);
  float lamp = 0.55 + 0.45*clamp((h.y + 1.5)/3.0, 0.0, 1.0);
  vec3 lit = col*L*shade*lamp*1.9;   // well lit
  if (obj == 5 || (obj == 0 && emit > 0.9 && emit < 1.5)) lit += (L.x + L.y + L.z < 0.1 ? vec3(0.0) : accent)*0.7;  // screens
  else if (emit > 0.0) lit += (L.x + L.y + L.z < 0.1 ? vec3(0.0) : (emit > 2.0 ? accent : L))*emit*0.55;
  return lit*exp(-t*0.05);
}
vec3 hangarBehind(vec3 q, vec3 D, vec3 room, vec2 mouth, vec3 L){
  float hw = mouth.x*0.5 + 2.0, fl = -mouth.y*0.5, ce = mouth.y*0.5 + 1.5, DZ = 28.0;
  vec3 lo = vec3(-hw, fl, 0.0), hi = vec3(hw, ce, DZ), nb;
  vec3 tA = (mix(lo, hi, step(0.0, D)) - q)/D; float t = min(tA.x, min(tA.y, tA.z));
  vec3 n = t == tA.x ? vec3(-sign(D.x), 0.0, 0.0) : t == tA.y ? vec3(0.0, -sign(D.y), 0.0) : vec3(0.0, 0.0, -1.0);
  int obj = 0;
  for (int i = 0; i < 3; i++) { float zc = 6.0 + float(i)*7.5, xc = (ph1(room.xy + float(i)) - 0.5)*max(hw - 3.5, 0.0), tb;
    tb = bHit(q, D, vec3(xc - 1.4, fl, zc - 2.4), vec3(xc + 1.4, fl + 1.2, zc + 2.4), nb, t); if (tb < t) { t = tb; n = nb; obj = 1; }   // a small craft's body
    tb = bHit(q, D, vec3(xc - 3.0, fl + 0.5, zc - 0.2), vec3(xc + 3.0, fl + 0.7, zc + 1.0), nb, t); if (tb < t) { t = tb; n = nb; obj = 1; } } // its wings
  vec3 h = q + D*t, col = vec3(0.16, 0.17, 0.15); float emit = 0.0;
  if (obj == 1) col = vec3(0.32, 0.33, 0.27);
  else if (n.y > 0.5) col = vec3(0.11, 0.12, 0.10) + vec3(0.6, 0.5, 0.15)*step(abs(h.x), 0.35)*step(0.5, fract(h.z/4.0));   // the deck and its stripe
  else if (n.y < -0.5) emit = step(0.72, fract(h.z/4.0))*step(abs(fract(h.x/3.0) - 0.5), 0.22)*2.2;                         // ceiling light rows
  else if (n.z < -0.5) { col = vec3(0.22, 0.23, 0.2); emit = 0.3; }                                                          // the back wall
  else col *= 0.75 + 0.25*step(0.5, fract(h.z/3.0));                                                                         // ribbed walls
  float shade = 0.4 + 0.6*max(n.y, 0.0) + 0.25*max(-n.z, 0.0);
  return (col*L*shade*1.8 + L*emit*0.6)*exp(-t*0.03);
}
`;
return { GLSL };
})();
