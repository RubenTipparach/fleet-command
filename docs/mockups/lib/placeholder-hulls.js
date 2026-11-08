// Placeholder hulls: the one generator that builds every placeholder ship of the nine factions with no designed ships
// (owner, 2026-11-08: "generate place holders for all of their ships, class design is your discretion"), from
// data/ships/placeholders.json (written by tools/factions/placeholder_fleets.py). It lives in a library so mockup 36
// (docs/mockups/placeholder-fleets.html) and, later, tools/mockups/export_ships.mjs build the same hulls (CLAUDE.md
// section 6.1).
//
// Every hull is lofts, never a stretched primitive (CLAUDE.md section 10, "a hull is one grown structure"): the body is a
// superellipse section swept stern to bow through the faction's plan and rise profiles, and every limb (fin, pod, ram,
// shell, petal, tendril, crest) is a second loft whose root starts inside the body. The drives are recesses in the stern
// cap that glow. Colour comes from stripes on the skin (trim, accent, glow), so a light strip is a face of the hull.
//
// Axes: metres, +Y up, bow toward +Z, starboard toward +X (the mockups' convention; the glb export turns it). The origin
// is the body's centre.
(function () {
  const P = Math.PI;

  // A profile of seven stations, stern (t 0) to bow (t 1): linear between stations for a faceted hull, Catmull-Rom for a
  // smooth one.
  function sample(arr, t, facet) {
    const n = arr.length - 1, x = Math.min(n, Math.max(0, t * n)), i = Math.min(n - 1, Math.floor(x)), u = x - i;
    if (facet) return arr[i] + (arr[i + 1] - arr[i]) * u;
    const p0 = arr[Math.max(0, i - 1)], p1 = arr[i], p2 = arr[i + 1], p3 = arr[Math.min(n, i + 2)];
    return 0.5 * (2 * p1 + (-p0 + p2) * u + (2 * p0 - 5 * p1 + 4 * p2 - p3) * u * u + (-p0 + 3 * p1 - 3 * p2 + p3) * u * u * u);
  }

  // A superellipse section point at angle phi (0 at the top, clockwise seen from the bow toward +X): exponent n, half-width
  // a, half-height b.
  function sect(phi, a, b, n) {
    const s = Math.sin(phi), c = Math.cos(phi), e = 2 / n;
    return [a * Math.sign(s) * Math.pow(Math.abs(s), e), b * Math.sign(c) * Math.pow(Math.abs(c), e)];
  }

  // The role of a face at (t along the loft, phi round it): the last stripe that covers it, else hull.
  function roleAt(stripes, t, phiDeg) {
    let r = 'hull';
    let a = ((phiDeg + 180) % 360 + 360) % 360 - 180;
    for (const s of stripes || []) if (t >= s.t[0] && t <= s.t[1] && a >= s.a_deg[0] && a <= s.a_deg[1]) r = s.role;
    return r;
  }

  const ROLES = ['hull', 'trim', 'accent', 'dark', 'glow'];

  // Mesh builder: shared vertices (smooth normals unless flat), faces bucketed by role into geometry groups.
  function Builder() { this.pos = []; this.faces = Object.fromEntries(ROLES.map((r) => [r, []])); }
  Builder.prototype.v = function (x, y, z) { this.pos.push(x, y, z); return this.pos.length / 3 - 1; };
  Builder.prototype.f = function (role, a, b, c) { (this.faces[role] || this.faces.hull).push(a, b, c); };
  Builder.prototype.geometry = function (THREE) {
    const g = new THREE.BufferGeometry(), idx = [];
    g.setAttribute('position', new THREE.Float32BufferAttribute(this.pos, 3));
    ROLES.forEach((r, m) => { const f = this.faces[r]; if (f.length) { g.addGroup(idx.length, f.length, m); idx.push(...f); } });
    g.setIndex(idx); g.computeVertexNormals(); return g;
  };

  // A loft through rings of points. rings[k] is an array of [x, y, z]; tOf(k) the loft position for stripes; caps: the
  // stern (first ring) is a drive recess when 'drive', closed when 'close', open when 'open'; the last ring likewise
  // ('close' or 'glow').
  function loft(B, rings, tOf, stripes, phis, startCap, endCap, flat) {
    const segs = rings[0].length, base = [];
    for (const ring of rings) { const row = []; for (const p of ring) row.push(B.v(p[0], p[1], p[2])); base.push(row); }
    // A flat (faceted) hull gets its own vertices per face, so normals stay crisp.
    const quad = (role, a, b, c, d) => {
      if (flat) { const P3 = (i) => B.pos.slice(i * 3, i * 3 + 3); const q = [a, b, c, d].map((i) => B.v(...P3(i))); B.f(role, q[0], q[1], q[2]); B.f(role, q[0], q[2], q[3]); }
      else { B.f(role, a, b, c); B.f(role, a, c, d); }
    };
    for (let k = 0; k < rings.length - 1; k++) {
      const t = (tOf(k) + tOf(k + 1)) / 2;
      for (let j = 0; j < segs; j++) {
        const j2 = (j + 1) % segs, phi = (phis[j] + phis[j2] + (j2 === 0 ? 2 * P : 0)) / 2;
        quad(roleAt(stripes, t, phi * 180 / P), base[k][j], base[k + 1][j], base[k + 1][j2], base[k][j2]);
      }
    }
    const cap = (row, ring, mode, outward) => {
      if (mode === 'open') return;
      const c = ring.reduce((s, p) => [s[0] + p[0] / segs, s[1] + p[1] / segs, s[2] + p[2] / segs], [0, 0, 0]);
      if (mode === 'drive') {
        // A drive recess: a dark lip to an inner ring set back, its floor glowing.
        const inner = ring.map((p) => [c[0] + (p[0] - c[0]) * 0.62, c[1] + (p[1] - c[1]) * 0.62, c[2] + outward * 0.012 * Math.abs(c[2] || 1)]);
        const ir = inner.map((p) => B.v(...p)), ci = B.v(c[0], c[1], inner[0][2]);
        for (let j = 0; j < segs; j++) { const j2 = (j + 1) % segs; if (outward < 0) quad('dark', row[j], row[j2], ir[j2], ir[j]); else quad('dark', row[j], ir[j], ir[j2], row[j2]); }
        for (let j = 0; j < segs; j++) { const j2 = (j + 1) % segs; if (outward < 0) B.f('glow', ci, ir[j2], ir[j]); else B.f('glow', ci, ir[j], ir[j2]); }
        return;
      }
      const ci = B.v(...c), role = mode === 'glow' ? 'glow' : 'hull';
      for (let j = 0; j < segs; j++) { const j2 = (j + 1) % segs; if (outward < 0) B.f(role, ci, row[j2], row[j]); else B.f(role, ci, row[j], row[j2]); }
    };
    cap(base[0], rings[0], startCap, -1);
    cap(base[base.length - 1], rings[rings.length - 1], endCap, 1);
  }

  const phisOf = (segs) => Array.from({ length: segs }, (_, j) => (j / segs) * 2 * P);

  // The body: the faction's profiles at the class's length, beam and height.
  function body(B, F, C) {
    const L = C.length_m, hb = L * C.beam_ratio / 2, hh = L * C.height_ratio / 2, h = F.hull, facet = !!h.facet;
    const N = facet ? 6 * 4 : 40, phis = phisOf(h.segs), rings = [];
    for (let k = 0; k <= N; k++) {
      const t = k / N, a = Math.max(0.01, sample(h.plan, t, facet)) * hb, b = Math.max(0.01, sample(h.rise, t, facet)) * hh;
      const yc = sample(h.centre, t, facet) * hh, z = (t - 0.5) * L;
      rings.push(phis.map((phi) => { const [x, y] = sect(phi, a, b, h.n); return [x, y + yc, z]; }));
    }
    loft(B, rings, (k) => k / N, C.stripes, phis, 'drive', 'close', facet);
  }

  // A limb: a loft along a three-point path (fractions of half-beam, half-height, half-length), its section w by h in
  // fractions of the half-beam, rooted inside the body.
  function limb(B, F, C, Lm, sx, scale, dz) {
    const L = C.length_m, hb = L * C.beam_ratio / 2, hh = L * C.height_ratio / 2, hl = L / 2;
    const p0 = Lm.path[0];
    const pts = Lm.path.map((p) => [sx * (p0[0] + (p[0] - p0[0]) * scale) * hb, (p0[1] + (p[1] - p0[1]) * scale) * hh, (p0[2] + (p[2] - p0[2]) * scale + dz) * hl]);
    const N = 14, phis = phisOf(Lm.segs), rings = [];
    const at = (u) => { const a = (1 - u) * (1 - u), b = 2 * u * (1 - u), c = u * u; return [0, 1, 2].map((i) => a * pts[0][i] + b * pts[1][i] + c * pts[2][i]); };
    const lin = (arr, u) => { const x = u * (arr.length - 1), i = Math.min(arr.length - 2, Math.floor(x)); return arr[i] + (arr[i + 1] - arr[i]) * (x - i); };
    for (let k = 0; k <= N; k++) {
      const u = k / N, c = at(u), d = at(Math.min(1, u + 0.01)), e = at(Math.max(0, u - 0.01));
      let T = [d[0] - e[0], d[1] - e[1], d[2] - e[2]]; const tl = Math.hypot(...T) || 1; T = T.map((v) => v / tl);
      let up = Math.abs(T[1]) > 0.8 ? [0, 0, 1] : [0, 1, 0];
      let S = [T[1] * up[2] - T[2] * up[1], T[2] * up[0] - T[0] * up[2], T[0] * up[1] - T[1] * up[0]]; const sl = Math.hypot(...S) || 1; S = S.map((v) => v / sl);
      const U = [S[1] * T[2] - S[2] * T[1], S[2] * T[0] - S[0] * T[2], S[0] * T[1] - S[1] * T[0]];
      const w = lin(Lm.w, u) * hb * scale, h = lin(Lm.h, u) * hb * scale;
      rings.push(phis.map((phi) => { const [x, y] = sect(phi, Math.max(0.05, w), Math.max(0.05, h), Lm.n); return [c[0] + S[0] * x + U[0] * y, c[1] + S[1] * x + U[1] * y, c[2] + S[2] * x + U[2] * y]; }));
    }
    // A limb wears the body's stripes by where it sits along the hull, so trim and glow carry on to it.
    const tOf = (k) => { const u = k / N; return Math.min(1, Math.max(0, at(u)[2] / L + 0.5)); };
    const stripes = (C.stripes || []).filter((s) => s.role !== 'glow');
    loft(B, sx < 0 ? rings.map((r) => r.slice().reverse()) : rings, tOf, stripes, sx < 0 ? phis.slice().reverse() : phis, 'open', Lm.glow_tip ? 'glow' : 'close', !!F.hull.facet);
  }

  const MATS = {};
  function materials(THREE, F) {
    const key = JSON.stringify(F.colours);
    if (MATS[key]) return MATS[key];
    const c = F.colours, std = (hex, rough, metal) => new THREE.MeshStandardMaterial({ color: new THREE.Color(hex), roughness: rough, metalness: metal, flatShading: !!F.hull.facet, side: THREE.DoubleSide });
    return (MATS[key] = [std(c.hull, 0.55, 0.35), std(c.trim, 0.5, 0.4), std(c.accent || c.trim, 0.45, 0.5), std(c.dark, 0.7, 0.3),
      new THREE.MeshBasicMaterial({ color: new THREE.Color(c.glow), side: THREE.DoubleSide })]);
  }

  // Build one class of one faction as a THREE.Mesh, named for the class id, with its length in userData.
  function build(THREE, F, C) {
    const B = new Builder();
    body(B, F, C);
    for (const Lm of C.limbs || []) {
      const copies = Math.max(1, Lm.iterate || 1);
      for (let k = 0; k < copies; k++) {
        const sc = Math.pow(Lm.scale || 0.618, k), dz = -0.22 * k;
        limb(B, F, C, Lm, 1, sc, dz);
        if (Lm.mirror) limb(B, F, C, Lm, -1, sc, dz);
      }
    }
    const mesh = new THREE.Mesh(B.geometry(THREE), materials(THREE, F));
    mesh.name = C.id; mesh.userData = { length_m: C.length_m, faction: F.name, cls: C.name };
    return mesh;
  }

  window.PlaceholderHulls = { build, sample, sect };
})();
