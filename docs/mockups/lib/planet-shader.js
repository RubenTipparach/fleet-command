// Fleet Command mockups: the paper chart's planets, drawn live by a shader (openspec/changes/paper-map; owner, 2026-11-08:
// "use this to draw our planets in space ... but maybe less pixelated. More cross hatching", of Francisco Cidade's Pixel
// Planet Godot shader, https://store.godotengine.org/asset/fcidade/pixel-planet-godot-shader/, CC0 1.0, itself inspired by
// Deep-Fold's Pixel Planet Generator). Its features, written here in our own code for three.js r147: a spinning procedural
// world of six kinds (terran, desert, ice, lava, gas giant, moon), its light cut into flat bands, clouds turning twice as
// fast as the ground and throwing shadows on it, polar caps, an axis tilt, rings, and a rim of atmosphere. Ours differs in
// two ways the owner asked for: far finer pixels (`pixels`, 220 across the disc against the original's 64), and the dark
// bands cross-hatched in sepia ink instead of dithered, one set of lines per band deeper into the night, with the
// coastlines and the limb inked, so the world sits on the chart as a drawing.
//
// No texture files and no trigonometric hash: the noise is value noise over a 256 x 256 lattice of Mulberry32 numbers made
// here (CLAUDE.md section 10), read with the two-channel trick so one 2D lookup gives 3D noise. It runs on WebGL 1 and 2.
//
//   const mat = FCPlanet.material(THREE, { kind: 'terran', seed: 7, ring: '#4f8fe8' });
//   const quad = FCPlanet.billboard(THREE, mat, 1.6);   // a quad facing the camera, its foot at its origin, 1.6 units wide
//   each frame: mat.uniforms.uTime.value = seconds;
//   FCPlanet.KINDS: the kinds and their palettes
(function () {
  function rng(seed) { let a = seed >>> 0; return () => { a = (a + 0x6d2b79f5) >>> 0; let t = a; t = Math.imul(t ^ (t >>> 15), t | 1); t ^= t + Math.imul(t ^ (t >>> 7), t | 61); return ((t ^ (t >>> 14)) >>> 0) / 4294967296; }; }

  // Each kind's colours, light to dark: [lit, mid, low, deep] for the ground, then what else it carries.
  const KINDS = {
    terran: { ground: ['#9fc86a', '#5f9a4a', '#3d6e3c', '#24452c'], sea: ['#7fb8e0', '#4a86c0', '#2f5f96', '#1b3a66'], cloud: '#f4efe2', caps: true, clouds: 0.45, atmo: '#9fd0ff', land: 0.52 },
    desert: { ground: ['#f0c98a', '#d79a5a', '#b06a3c', '#6e3c24'], dune: true, caps: false, clouds: 0.12, atmo: '#ffcf9a' },
    ice: { ground: ['#f4f8fb', '#c8dcec', '#93b4d0', '#5a7ea4'], caps: true, clouds: 0.25, atmo: '#d8f0ff', crack: true },
    lava: { ground: ['#6a5048', '#4a3632', '#2f2220', '#1a1212'], glow: '#ff8a2a', caps: false, clouds: 0, atmo: '#ff9a5a' },
    gas: { bands: ['#f8e8c6', '#ecc893', '#d8a26c', '#c08656', '#f4dcae', '#e2b884'], caps: false, clouds: 0, atmo: '#f6dcb4' },
    moon: { ground: ['#d4cfc6', '#a9a399', '#7f7a72', '#55514c'], craters: true, caps: false, clouds: 0, atmo: null },
  };
  const hex = (THREE, s) => new THREE.Color(s);

  let lattice = null;
  // The noise lattice: red is the random value, green the same value 37 across and 17 down, so a lookup at z reads the
  // layer above in its green channel (Inigo Quilez's 3D noise from one 2D texture).
  function latticeTex(THREE) {
    if (lattice) return lattice;
    const N = 256, R = rng(0x5eed1e57), v = new Uint8Array(N * N), d = new Uint8Array(N * N * 4);
    for (let i = 0; i < v.length; i++) v[i] = Math.floor(R() * 256);
    for (let y = 0; y < N; y++) for (let x = 0; x < N; x++) {
      const i = (y * N + x) * 4;
      d[i] = v[y * N + x]; d[i + 1] = v[((y - 17 + N) % N) * N + ((x - 37 + N) % N)]; d[i + 2] = 0; d[i + 3] = 255;
    }
    lattice = new THREE.DataTexture(d, N, N, THREE.RGBAFormat);
    lattice.wrapS = lattice.wrapT = THREE.RepeatWrapping; lattice.magFilter = lattice.minFilter = THREE.LinearFilter; lattice.needsUpdate = true;
    return lattice;
  }

  const VERT = `
    uniform float uWidth; varying vec2 vUv;
    void main() {
      vUv = uv;
      vec4 mv = modelViewMatrix * vec4(0.0, 0.0, 0.0, 1.0);   // a billboard: the quad laid in the view plane at its origin
      mv.xy += vec2(position.x, position.y + 0.5) * uWidth;    // its foot on the origin
      gl_Position = projectionMatrix * mv;
    }`;
  const FRAG = `
    #ifdef GL_FRAGMENT_PRECISION_HIGH
    precision highp float;
    #endif
    uniform sampler2D uLat; uniform float uTime, uPixels, uSteps, uSeed, uTilt, uSpin, uKind, uRing, uClouds, uLand, uCaps;
    uniform vec3 uG0, uG1, uG2, uG3, uS0, uS1, uS2, uS3, uCloud, uAtmo, uRingCol, uGlow, uInk;
    uniform vec3 uB0, uB1, uB2, uB3, uB4, uB5;
    varying vec2 vUv;
    // value noise in 3D from the lattice (two layers per lookup)
    float vn(vec3 x) {
      vec3 p = floor(x), f = fract(x); f = f * f * (3.0 - 2.0 * f);
      vec2 uv = (p.xy + vec2(37.0, 17.0) * p.z) + f.xy;
      vec2 rg = texture2D(uLat, (uv + 0.5) / 256.0).yx;
      return mix(rg.x, rg.y, f.z);
    }
    float fbm(vec3 p) { float v = 0.0, a = 0.5; for (int i = 0; i < 5; i++) { v += a * vn(p); p = p * 2.03 + vec3(1.7, 9.2, 4.1); a *= 0.5; } return v; }
    // the planet's surface point for a disc point q (radius 1), turned by its spin
    vec3 surf(vec2 q, float turn) {
      float z = sqrt(max(0.0, 1.0 - dot(q, q)));
      vec3 n = vec3(q, z);
      float c = cos(turn), s = sin(turn);
      return vec3(c * n.x + s * n.z, n.y, -s * n.x + c * n.z);
    }
    // ink lines across the disc at angle a, every k pixels, lw wide
    float lines(vec2 px, float a, float k, float lw) { float d = dot(px, vec2(cos(a), sin(a))); return step(abs(fract(d / k) - 0.5) * k, lw); }
    vec3 pick4(float t, vec3 c0, vec3 c1, vec3 c2, vec3 c3) { return t < 0.25 ? c0 : t < 0.5 ? c1 : t < 0.75 ? c2 : c3; }
    void main() {
      // pixelate: the disc is uPixels across (fine, so it reads drawn, not pixel art)
      vec2 grid = floor(vUv * uPixels * 1.6) / (uPixels * 1.6) + 0.5 / (uPixels * 1.6);
      vec2 p = (grid - 0.5) * 3.2;            // the quad spans 3.2 planet radii (room for the ring)
      vec2 px = grid * uPixels * 1.6;          // pixel coordinates, for the hatching
      float ct = cos(uTilt), st = sin(uTilt);
      vec2 q = vec2(ct * p.x + st * p.y, -st * p.x + ct * p.y);   // the planet's own frame, its axis tilted
      vec3 L = normalize(vec3(-0.55, 0.5, 0.67));                   // light from the upper left, as the lamp
      float r = length(p);
      vec4 col = vec4(0.0);
      // the ring, behind the disc where it passes behind
      float ringR = length(vec2(q.x, q.y / 0.32));
      bool ringHit = uRing > 0.5 && ringR > 1.42 && ringR < 1.82;
      bool behind = q.y > 0.0;
      if (r < 1.0) {
        vec3 n = vec3(p, sqrt(1.0 - r * r));
        float lit = clamp(dot(n, L), 0.0, 1.0);
        vec3 sp = surf(q, uTime * uSpin + uSeed);
        vec3 sc = surf(q, uTime * uSpin * 2.0 + uSeed * 1.7);
        float h = fbm(sp * 2.2 + uSeed);
        vec3 base; float coast = 0.0, glow = 0.0;
        float lat = q.y;
        if (uKind < 0.5) {                      // terran: sea and land
          float t = clamp((h - uLand) / 0.25, 0.0, 1.0);   // the lowlands light, the highlands dark
          base = h > uLand ? pick4(t * 0.99, uG0, uG1, uG2, uG3) : pick4(clamp((uLand - h) / 0.2, 0.0, 0.99), uS0, uS1, uS2, uS3);
          coast = 1.0 - smoothstep(0.0, 0.012, abs(h - uLand));
        } else if (uKind < 1.5) {               // desert: dunes in long waves
          float d = fbm(sp * vec3(1.0, 6.0, 1.0) * 2.0 + uSeed) * 0.6 + h * 0.4;
          base = pick4(clamp(1.0 - d, 0.0, 0.99), uG0, uG1, uG2, uG3);
        } else if (uKind < 2.5) {               // ice: pale, cracked
          base = pick4(clamp(1.0 - h * 1.1, 0.0, 0.99), uG0, uG1, uG2, uG3);
          coast = 1.0 - smoothstep(0.0, 0.01, abs(fbm(sp * 4.0 + 3.0) - 0.5));
        } else if (uKind < 3.5) {               // lava: dark rock, glowing rivers
          base = pick4(clamp(1.0 - h, 0.0, 0.99), uG0, uG1, uG2, uG3);
          glow = 1.0 - smoothstep(0.0, 0.03, abs(fbm(sp * 3.0 + 7.0) - 0.5));
        } else if (uKind < 4.5) {               // gas giant: bands along the latitude, stirred
          float b = sp.y * 3.0 + fbm(sp * vec3(2.0, 0.5, 2.0) * 2.0 + uSeed) * 1.4;
          float i = mod(floor(b * 2.0), 6.0);
          base = i < 0.5 ? uB0 : i < 1.5 ? uB1 : i < 2.5 ? uB2 : i < 3.5 ? uB3 : i < 4.5 ? uB4 : uB5;
          coast = 1.0 - smoothstep(0.0, 0.04, 0.5 - abs(fract(b * 2.0) - 0.5));   // inked only near each band's edge
        } else {                                // moon: grey, cratered
          base = pick4(clamp(1.0 - h, 0.0, 0.99), uG0, uG1, uG2, uG3);
          vec3 cp = sp * 4.0; vec3 cf = fract(cp) - 0.5; float cr = vn(floor(cp) * 0.37 + 11.0);
          float cd = length(cf) / (0.15 + cr * 0.3);
          if (cr > 0.45 && cd < 1.0) { base *= 0.78; coast = step(0.8, cd); }   // the crater's floor darker, its rim inked
        }
        // polar caps
        if (uCaps > 0.5 && abs(lat) > 0.78 - 0.12 * fbm(sp * 5.0)) { base = mix(base, vec3(0.95, 0.97, 1.0), 0.9); }
        // clouds, and their shadow a little toward the light's far side
        float cl = 0.0, csh = 0.0;
        if (uClouds > 0.0) {
          cl = smoothstep(0.62 - uClouds * 0.2, 0.7 - uClouds * 0.2, fbm(sc * 3.0 + 5.0));
          vec3 so = surf(q + L.xy * -0.05, uTime * uSpin * 2.0 + uSeed * 1.7);
          csh = smoothstep(0.62 - uClouds * 0.2, 0.7 - uClouds * 0.2, fbm(so * 3.0 + 5.0)) * (1.0 - cl);
        }
        // the light cut into flat bands
        float band = floor(lit * uSteps) / uSteps;
        float shade = 0.55 + 0.45 * band;
        vec3 c = base * shade * (1.0 - 0.35 * csh);
        c = mix(c, uCloud * (0.6 + 0.4 * band), cl);
        c += uGlow * glow * 1.2;
        // the night side cross-hatched: one more set of lines for each band deeper into shadow
        float dark = 1.0 - band;
        float ink = 0.0;
        if (dark > 0.35) ink = max(ink, lines(px, 0.785, 8.0, 0.7));
        if (dark > 0.55) ink = max(ink, lines(px, -0.785, 8.0, 0.7));
        if (dark > 0.8) ink = max(ink, lines(px, 0.0, 6.0, 0.6));
        ink *= 1.0 - glow;
        c = mix(c, uInk, ink * 0.62);
        // coastlines, band edges and crater rims inked
        c = mix(c, uInk, coast * (1.0 - cl) * 0.8);
        // the limb inked heavy, with a wandering pen
        float pen = 0.06 + 0.02 * (vn(vec3(atan(p.y, p.x) * 3.0, uSeed, 0.0)) - 0.5);
        c = mix(c, uInk, smoothstep(1.0 - pen, 1.0 - pen * 0.5, r));
        col = vec4(c, 1.0);
      } else if (r < 1.12 && uAtmo.r + uAtmo.g + uAtmo.b > 0.0) {
        // the atmosphere: a thin washed rim on the lit side, hatched faintly
        vec2 d = p / r; float side = clamp(dot(vec3(d, 0.2), L) * 1.5 + 0.2, 0.0, 1.0);
        float a = (1.0 - (r - 1.0) / 0.12) * side * 0.65;
        col = vec4(uAtmo, a * (1.0 - 0.5 * lines(px, 0.785, 7.0, 0.7)));
      }
      if (ringHit && (!behind || r >= 1.0)) {   // the far half only where the disc does not cover it
        // the ring: washed in its colour, its far side darker, its edges inked, hatched where the planet's shadow falls
        float e = min(ringR - 1.42, 1.82 - ringR);
        vec3 rc = mix(uRingCol, vec3(0.93, 0.87, 0.74), 0.3) * (behind ? 0.72 : 1.0);
        float shadowed = (!behind && q.x > 0.0 && abs(q.x) < 1.0) ? 1.0 : 0.0;
        rc = mix(rc, uInk, lines(px, -0.785, 7.0, 0.7) * (0.2 + 0.55 * shadowed));
        rc = mix(rc, uInk, 1.0 - smoothstep(0.0, 0.05, e));
        rc = mix(rc, uInk, (1.0 - smoothstep(0.0, 0.018, abs(ringR - 1.6))) * 0.7);   // the gap in the ring
        col = vec4(rc, 1.0);
      }
      if (col.a <= 0.01) discard;
      gl_FragColor = col;
    }`;

  /** A planet material: kind from KINDS, seed a number, ring a CSS colour or null, pixels across the disc, steps of light. */
  function material(THREE, { kind = 'terran', seed = 1, ring = null, pixels = 220, steps = 5, tilt, spin = 0.12 } = {}) {
    const K = KINDS[kind] || KINDS.terran, R = rng(seed), g = K.ground || K.bands, s = K.sea || g, b = K.bands || g.concat(g.slice(0, 2));
    const u = {
      uLat: { value: latticeTex(THREE) }, uTime: { value: 0 }, uPixels: { value: pixels }, uSteps: { value: steps },
      uSeed: { value: R() * 40 }, uTilt: { value: tilt ?? (R() - 0.5) * 0.7 }, uSpin: { value: spin }, uWidth: { value: 1 },
      uKind: { value: ['terran', 'desert', 'ice', 'lava', 'gas', 'moon'].indexOf(kind) }, uRing: { value: ring ? 1 : 0 },
      uClouds: { value: K.clouds || 0 }, uLand: { value: K.land ?? 0.5 }, uCaps: { value: K.caps ? 1 : 0 },
      uCloud: { value: hex(THREE, K.cloud || '#f4efe2') }, uAtmo: { value: K.atmo ? hex(THREE, K.atmo) : new THREE.Color(0, 0, 0) },
      uRingCol: { value: hex(THREE, ring || '#888888') }, uGlow: { value: hex(THREE, K.glow || '#000000') }, uInk: { value: hex(THREE, '#2a1b10') },
    };
    ['uG0', 'uG1', 'uG2', 'uG3'].forEach((k, i) => (u[k] = { value: hex(THREE, g[i]) }));
    ['uS0', 'uS1', 'uS2', 'uS3'].forEach((k, i) => (u[k] = { value: hex(THREE, s[i]) }));
    ['uB0', 'uB1', 'uB2', 'uB3', 'uB4', 'uB5'].forEach((k, i) => (u[k] = { value: hex(THREE, b[i % b.length]) }));
    return new THREE.ShaderMaterial({ uniforms: u, vertexShader: VERT, fragmentShader: FRAG, transparent: true });
  }
  /** A quad of `width` units that always faces the camera, its foot (the ring's lowest point) at its origin. */
  function billboard(THREE, mat, width) {
    mat.uniforms.uWidth.value = width;
    const m = new THREE.Mesh(new THREE.PlaneGeometry(1, 1), mat);
    m.frustumCulled = false;
    return m;
  }
  window.FCPlanet = { material, billboard, KINDS };
})();
