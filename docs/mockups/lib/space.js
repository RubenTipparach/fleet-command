// Fleet Command mockups: the space backdrop and the stand-in ship, shared by every page that shows space
// (docs/mockups/game-flow.html, docs/mockups/battle-console.html). One implementation (CLAUDE.md section 6.1): edit it
// here, then re-run tools/mockups/inline_data.py on each page that inlines it.
//
// FCSpace(THREE, renderer, DATA, rng) returns { makeSpace, parallaxStars, shipModel, nebulaMesh }. DATA maps a data file's path to
// its parsed JSON (the page's inlined data/space/ files); rng(seed) is the page's seeded generator.
//

// Space backdrop, ported from Star Crew Bridge Simulator (RubenTipparach/Star-Crew-Bridge-Simulator 9a35f08:
// crates/sc-render/shaders/sky.glsl and docs/mockups/nebula-skies.html) at the owner's request (2026-10-30: "Star crew
// bridge simulator! also borrow its star shader from there too, so when we scroll around the stars trace an outline").
// Three layers:
// - the nebula: a sky file's vertex-coloured sphere (data/space/skies/, inlined below by tools/mockups/inline_data.py)
//   with the faint hashed stars, hidden where the nebula is dense, and an ordered dither so slow gradients never band;
// - the vector stars: FreeSpace's lines, each drawn from where it is on screen back toward where it was tail_s ago,
//   capped and dimmed as it grows, alpha-blended and whitened as it streaks (data/space/starfield.json's rule);
// - parallax stars below the board: the same lines for points at a finite depth, so panning the map, which turns
//   nothing, still streaks them, the nearer ones sliding and streaking more.
window.FCSpace = function FCSpace(THREE, renderer, DATA, rng) {
'use strict';
const SF = DATA['data/space/starfield.json'];
function bytesOf(rows) {
  const parts = rows.map((r) => Uint8Array.from(atob(r), (c) => c.charCodeAt(0)));
  const out = new Uint8Array(parts.reduce((n, q) => n + q.length, 0));
  let o = 0; for (const q of parts) { out.set(q, o); o += q.length; }
  return out;
}
function skyGeometry(S) {
  // 8 bytes a vertex: octahedral direction (two 16-bit), sRGB colour, the share of the stars behind it hidden.
  const vb = bytesOf(S.mesh.vertices_b64), n = vb.length / 8;
  const pos = new Float32Array(n * 3), col = new Uint8Array(n * 4);
  for (let i = 0; i < n; i++) {
    const px = (vb[8 * i] | (vb[8 * i + 1] << 8)) / 65535 * 2 - 1, py = (vb[8 * i + 2] | (vb[8 * i + 3] << 8)) / 65535 * 2 - 1;
    const z = 1 - Math.abs(px) - Math.abs(py), k = Math.max(-z, 0);
    const x = px + (px >= 0 ? -k : k), y = py + (py >= 0 ? -k : k), l = Math.hypot(x, y, z);
    pos[3 * i] = x / l; pos[3 * i + 1] = y / l; pos[3 * i + 2] = z / l;
    for (let c = 0; c < 4; c++) col[4 * i + c] = vb[8 * i + 4 + c];
  }
  const tb = bytesOf(S.mesh.triangles_b64), g = new THREE.BufferGeometry();
  g.setAttribute('position', new THREE.BufferAttribute(pos, 3));
  g.setAttribute('aCol', new THREE.BufferAttribute(col, 4, true));
  g.setIndex(new THREE.BufferAttribute(new Uint16Array(tb.buffer, tb.byteOffset, tb.length / 2), 1));
  return g;
}
const SKY_GLSL = `
float hash13(vec3 p) { p = fract(p * 0.1031); p += dot(p, p.zyx + 31.32); return fract((p.x + p.y) * p.z); }
vec3 hash33(vec3 p) { p = fract(p * vec3(0.1031, 0.1030, 0.0973)); p += dot(p, p.yxz + 33.33); return fract((p.xxy + p.yxx) * p.zyx); }`;
function nebulaMesh(S) {
  const mat = new THREE.ShaderMaterial({
    uniforms: { uStars: { value: new THREE.Vector4(S.dust_stars.density, S.dust_stars.brightness, 0.001, 1) } },
    vertexShader: `attribute vec4 aCol; varying vec4 vCol; varying vec3 vDir;
      void main() { vCol = aCol; vDir = position; gl_Position = projectionMatrix * modelViewMatrix * vec4(position, 1.0); }`,
    fragmentShader: `uniform vec4 uStars; varying vec4 vCol; varying vec3 vDir; ${SKY_GLSL}
      vec3 star_grid(vec3 d, float scale, float size_px) {
        vec3 p = d * scale; vec3 cell = floor(p); vec3 h = hash33(cell);
        if (h.z > uStars.x) return vec3(0.0);
        vec3 at = normalize(cell + 0.2 + 0.6 * h);
        float k = clamp(1.0 - (length(d - at) / uStars.z) / size_px, 0.0, 1.0);
        vec3 c = mix(vec3(0.75, 0.85, 1.0), vec3(1.0, 0.9, 0.75), hash13(cell + 7.0));
        return c * k * k * uStars.y * (0.15 + 0.85 * h.y * h.y * h.y);
      }
      void main() {
        vec3 d = normalize(vDir);
        vec3 c = vCol.rgb + (star_grid(d, 90.0, 1.6) + star_grid(d, 160.0, 1.1) + star_grid(d, 300.0, 0.8)) * (1.0 - vCol.a) * uStars.w;
        vec2 px = floor(gl_FragCoord.xy); vec2 h2 = floor(px * 0.5);
        float bayer = fract(h2.x * 0.5 + h2.y * h2.y * 0.75) * 0.25 + fract(px.x * 0.5 + px.y * px.y * 0.75);
        gl_FragColor = vec4(c + (bayer - 0.47) / 255.0, 1.0);
      }`,
    depthTest: false, depthWrite: false, side: THREE.DoubleSide,
  });
  const m = new THREE.Mesh(skyGeometry(S), mat); m.renderOrder = -10; m.frustumCulled = false;
  return m;
}
// One streak program for both kinds of star: aPos is a direction (uFinite 0, drawn with the view's rotation only) or a
// point in the world (uFinite 1). The tail is measured on screen: where the star is now, back toward where it was last
// frame, scaled to tail_s and capped at max_deg's length in pixels; the star dims as its tail grows.
const STREAK = SF.streak, DEG = Math.PI / 180;
function streakMaterials() {
  const u = { uVP: { value: new THREE.Matrix4() }, uPrevVP: { value: new THREE.Matrix4() }, uViewport: { value: new THREE.Vector2(1, 1) },
    uTailScale: { value: 0 }, uMaxPx: { value: 40 }, uMinPx: { value: STREAK.min_px }, uDimPerPx: { value: 0.01 }, uDimFloor: { value: STREAK.dim_floor },
    uTailFade: { value: STREAK.tail_fade }, uAlphaMin: { value: STREAK.alpha_min }, uWhiten: { value: STREAK.whiten }, uFinite: { value: 0 }, uPxRatio: { value: 1 } };
  const common = `uniform mat4 uVP, uPrevVP; uniform vec2 uViewport; uniform float uTailScale, uMaxPx, uMinPx, uDimPerPx, uDimFloor, uTailFade, uAlphaMin, uWhiten, uFinite, uPxRatio;
    attribute vec3 aPos; attribute float aBright; attribute vec3 aColor;
    vec4 starColour(float dim, float len) {
      float s = smoothstep(0.0, 3.0 * uPxRatio, len);
      return vec4(mix(aColor, mix(aColor, vec3(1.0), uWhiten), s), mix(aBright, uAlphaMin + (1.0 - uAlphaMin) * aBright, s) * dim);
    }
    void ends(out vec4 p, out vec2 hp, out vec2 tp, out float len) {
      vec4 P = vec4(aPos, uFinite);
      p = uVP * P; vec4 q = uPrevVP * P;
      hp = p.xy / p.w * 0.5 * uViewport; vec2 qp = q.xy / q.w * 0.5 * uViewport;
      vec2 dv = q.w > 0.0 ? (qp - hp) * uTailScale : vec2(0.0);
      len = length(dv);
      if (len > uMaxPx) { dv *= uMaxPx / len; len = uMaxPx; }
      tp = hp + dv;
    }`;
  const lines = new THREE.ShaderMaterial({ uniforms: u, transparent: true, depthTest: false, depthWrite: false,
    vertexShader: `${common} attribute float aEnd; varying vec4 vCol;
      void main() {
        vec4 p; vec2 hp, tp; float len; ends(p, hp, tp, len);
        if (len < uMinPx * uPxRatio) tp = hp + (len > 1e-4 ? (tp - hp) / len : vec2(1.0, 0.0)) * uMinPx * uPxRatio;
        vec2 o = aEnd < 0.5 ? hp : tp;
        gl_Position = p.w > 0.0 ? vec4(o / (0.5 * uViewport) * p.w, 0.0, p.w) : vec4(0.0, 0.0, 2.0, 1.0);
        vCol = starColour(max(uDimFloor, 1.0 - uDimPerPx * len), len);
        vCol.a *= aEnd < 0.5 ? 1.0 : mix(1.0, uTailFade, smoothstep(0.0, 3.0 * uPxRatio, len));
      }`,
    fragmentShader: 'varying vec4 vCol; void main() { gl_FragColor = vCol; }' });
  const heads = new THREE.ShaderMaterial({ uniforms: u, transparent: true, depthTest: false, depthWrite: false,
    vertexShader: `${common} attribute float aSize; varying vec4 vCol;
      void main() {
        vec4 p; vec2 hp, tp; float len; ends(p, hp, tp, len);
        gl_Position = p.w > 0.0 ? vec4(p.xy, 0.0, p.w) : vec4(0.0, 0.0, 2.0, 1.0);
        gl_PointSize = (aSize + 1.0) * uPxRatio;
        vCol = starColour(max(uDimFloor, 1.0 - uDimPerPx * len), len);
      }`,
    fragmentShader: 'varying vec4 vCol; void main() { vec2 q = gl_PointCoord * 2.0 - 1.0; float r = dot(q, q); if (r > 1.0) discard; gl_FragColor = vec4(vCol.rgb, vCol.a * (1.0 - r) * (1.0 - r)); }' });
  return { u, lines, heads };
}
// stars: [x, y, z, brightness, class, colour], as a sky file lists them.
function starObjects(stars, finite) {
  const M = streakMaterials(); M.u.uFinite.value = finite ? 1 : 0;
  const build = (heads) => {
    const per = heads ? 1 : 2, out = { pos: [], br: [], col: [], end: [], size: [] };
    for (const s of stars) {
      const cls = SF.classes[s[4]], c = SF.colours[s[5]].srgb;
      if (heads && cls.size_px < 2) continue;
      for (let e = 0; e < per; e++) { out.pos.push(s[0], s[1], s[2]); out.br.push(s[3]); out.col.push(...c); out.end.push(e); out.size.push(cls.size_px); }
    }
    const g = new THREE.BufferGeometry();
    g.setAttribute('position', new THREE.Float32BufferAttribute(out.pos, 3)); g.setAttribute('aPos', new THREE.Float32BufferAttribute(out.pos, 3));
    g.setAttribute('aBright', new THREE.Float32BufferAttribute(out.br, 1)); g.setAttribute('aColor', new THREE.Float32BufferAttribute(out.col, 3));
    g.setAttribute('aEnd', new THREE.Float32BufferAttribute(out.end, 1)); g.setAttribute('aSize', new THREE.Float32BufferAttribute(out.size, 1));
    const o = heads ? new THREE.Points(g, M.heads) : new THREE.LineSegments(g, M.lines);
    o.frustumCulled = false; o.renderOrder = -8;
    return o;
  };
  return { M, lines: build(false), heads: build(true) };
}
// Stars at a finite depth below the board, drawn from the starfield's own classes and colours.
function parallaxStars(centre, spread, depths, seed) {
  const R = rng(seed), list = [];
  const pick = (arr) => { let x = R(); for (let i = 0; i < arr.length; i++) { x -= arr[i].share; if (x <= 0) return i; } return arr.length - 1; };
  for (const [y0, y1, count] of depths) for (let i = 0; i < count; i++) {
    const cls = pick(SF.classes), [b0, b1] = SF.classes[cls].brightness;
    list.push([centre.x + (R() - 0.5) * spread, y0 + (y1 - y0) * R(), centre.z + (R() - 0.5) * spread, b0 + (b1 - b0) * R(), cls, pick(SF.colours)]);
  }
  return list;
}
// A scene's whole backdrop. update() follows the camera and feeds the streaks this frame's and last frame's views.
function makeSpace(skyId, finiteStars) {
  const S = DATA[`data/space/skies/${skyId}.json`];
  const g = new THREE.Group(), neb = nebulaMesh(S), far = starObjects(S.stars.list, false);
  g.add(neb, far.lines, far.heads);
  const near = finiteStars ? starObjects(finiteStars, true) : null;
  if (near) g.add(near.lines, near.heads);
  let lastCam = null;
  const prevRot = new THREE.Matrix4(), prevFull = new THREE.Matrix4(), rot = new THREE.Matrix4(), full = new THREE.Matrix4(), size = new THREE.Vector2();
  g.userData.update = (cam, dt) => {
    neb.position.copy(cam.position); neb.scale.setScalar(cam.far * 0.9);
    cam.updateMatrixWorld();
    rot.extractRotation(cam.matrixWorldInverse).premultiply(cam.projectionMatrix);
    full.multiplyMatrices(cam.projectionMatrix, cam.matrixWorldInverse);
    const cut = lastCam !== cam;
    if (cut) { prevRot.copy(rot); prevFull.copy(full); lastCam = cam; }
    renderer.getDrawingBufferSize(size);
    const pr = renderer.getPixelRatio(), pxRad = (cam.fov * DEG) / size.y;
    neb.material.uniforms.uStars.value.z = pxRad;
    for (const [set, now, prev] of [[far, rot, prevRot], [near, full, prevFull]]) {
      if (!set) continue;
      const u = set.M.u;
      u.uVP.value.copy(now); u.uPrevVP.value.copy(prev); u.uViewport.value.copy(size); u.uPxRatio.value = pr;
      u.uTailScale.value = cut || dt <= 0 ? 0 : STREAK.tail_s / dt;
      u.uMaxPx.value = (STREAK.max_deg * DEG) / pxRad; u.uDimPerPx.value = (STREAK.dim_per_deg / DEG) * pxRad;
    }
    prevRot.copy(rot); prevFull.copy(full);
  };
  return g;
}
// A stand-in ship, our own shape (a spine, a wedge bow, two drive pods): the look is mockup 3's to decide.
function shipModel(color, scale = 1, hullColor = 0xb9c2cc) {
  const g = new THREE.Group();
  const hull = new THREE.MeshStandardMaterial({ color: hullColor, metalness: 0.55, roughness: 0.42 });
  const trim = new THREE.MeshStandardMaterial({ color, metalness: 0.4, roughness: 0.5 });
  const glow = new THREE.MeshStandardMaterial({ color: 0x9fd0ff, emissive: 0x6fb6ff, emissiveIntensity: 2.2 });
  const spine = new THREE.Mesh(new THREE.BoxGeometry(0.5, 0.32, 3.2), hull); g.add(spine);
  const bow = new THREE.Mesh(new THREE.ConeGeometry(0.9, 1.8, 4), hull); bow.rotation.x = Math.PI / 2; bow.rotation.y = Math.PI / 4; bow.scale.set(1, 1, 0.35); bow.position.set(0, 0.05, 2.2); g.add(bow);
  const band = new THREE.Mesh(new THREE.BoxGeometry(0.54, 0.34, 0.25), trim); band.position.z = 1.1; g.add(band);
  for (const s of [-1, 1]) {
    const pylon = new THREE.Mesh(new THREE.BoxGeometry(1.2, 0.08, 0.5), hull); pylon.position.set(s * 0.7, 0, -0.9); pylon.rotation.z = s * 0.25; g.add(pylon);
    const pod = new THREE.Mesh(new THREE.CylinderGeometry(0.22, 0.26, 2.2, 12), hull); pod.rotation.x = Math.PI / 2; pod.position.set(s * 1.25, 0.15, -0.9); g.add(pod);
    const stripe = new THREE.Mesh(new THREE.CylinderGeometry(0.235, 0.235, 0.3, 12), trim); stripe.rotation.x = Math.PI / 2; stripe.position.set(s * 1.25, 0.15, -0.1); g.add(stripe);
    const exhaust = new THREE.Mesh(new THREE.CircleGeometry(0.2, 12), glow); exhaust.position.set(s * 1.25, 0.15, -2.01); exhaust.rotation.y = Math.PI; g.add(exhaust);
  }
  const bridge = new THREE.Mesh(new THREE.BoxGeometry(0.3, 0.2, 0.5), hull); bridge.position.set(0, 0.25, 0.6); g.add(bridge);
  g.scale.setScalar(scale);
  return g;
}
return { makeSpace, parallaxStars, shipModel, nebulaMesh };
};
