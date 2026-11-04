// Fleet Command mockups: the special weapons' effects and shaders, shared by every page that fires one (CLAUDE.md 6.1: one
// implementation). Written for the weapons range (docs/mockups/special-weapons.html, change weapon-fx); the battle console
// mockups inline it when the specials reach them. What a colour means follows the Ship Design Bible (D2) and the console
// skins: the Concord's light is Sky with Lilac strands (clean light, the many species), the Krai's is forge orange through
// blood red (heat, the forge). Shared damage looks (fire, sparks, shards, weapon light) come from docs/mockups/lib/ship-fx.js,
// passed in as o.SFX, never copied. Edit it here, then re-run tools/mockups/inline_data.py on each page that inlines it.
//
// FCWeaponFX(THREE, o): o.scene (the battle), o.hazeScene (heat haze, drawn into its own target), o.camera, o.renderer,
// o.SFX (a FCShipFX instance), o.rand (the page's seeded generator). Returns:
//   GLSL                         noise (wn, wfbm), voronoi (wvor), blackbody(t), the shield lattices (hexEdge, triEdge)
//   pipeline(opts)               HDR render, bloom, heat haze, ACES and sRGB in one final pass: .setSize(w, h, pr), .render()
//   ribbon(o)                    a beam: a camera-facing strip between two points with a core, a glow, flowing noise and
//                                helical strands (o.look 'chorus' | 'join' | 'lance' | 'bolt' | 'barrel'); .set(a, b), .u (uniforms)
//   flare(o)                     a muzzle or impact flare (o.look 'lens' | 'tri' | 'soft'), camera-facing
//   shield(ship, o)              the ship's shield bubble in six facings (o.pattern 'hex' | 'tri'); .hit(dirLocal, t),
//                                .facing[k] (0..1 held), .breakAt[k], .raycast(worldFrom, worldDir)
//   scars(ship)                  patches the ship's materials: molten craters, beam burns, gouges and the quell lattice,
//                                in ship space; .add(kind, pLocal, r, dirLocal, dur), .lattice(pLocal, t, stunS)
//   orb(o), spike(o), dart(o)    the quell torpedo, the forge slug and a missile
//   trail(o)                     a fading ribbon behind a moving point; beads(o) a discrete trail of glowing beads
//   plume(r, len, o)             an exhaust with shock diamonds; bowShock(r, len, o) the shell before a charging bow
//   ring(o)                      an impact shockwave ring, any orientation; haze(o) a heat haze sprite
//   motes(o)                     light gathering toward a point (the chorus charge)
//   step(dt), time()             one fixed step of everything above
window.FCWeaponFX = function FCWeaponFX(THREE, o) {
'use strict';
const scene = o.scene, hazeScene = o.hazeScene, SFX = o.SFX, rnd = () => o.rand();
const V3 = (x = 0, y = 0, z = 0) => new THREE.Vector3(x, y, z), PI = Math.PI, TAU = PI * 2;
const T = { value: 0 }; // shared time (s), stepped on the fixed step
const live = []; // effects with a step(dt) of their own
const unlive = (e) => { const i = live.indexOf(e); if (i >= 0) live.splice(i, 1); }; // removing twice is harmless

// -----------------------------------------------------------------------------------------------------------------
// Shared GLSL: value noise and fbm in 3D, a voronoi with an edge distance, the blackbody ramp (black, deep red, forge
// orange, yellow, white: what hot metal does), and the two shield lattices.
const GLSL = `
float wh(vec3 p){ p = fract(p*0.3183099 + 0.1); p *= 17.0; return fract(p.x*p.y*p.z*(p.x + p.y + p.z)); }
float wn(vec3 x){ vec3 i = floor(x), f = fract(x); f = f*f*(3.0 - 2.0*f);
  return mix(mix(mix(wh(i), wh(i + vec3(1,0,0)), f.x), mix(wh(i + vec3(0,1,0)), wh(i + vec3(1,1,0)), f.x), f.y),
             mix(mix(wh(i + vec3(0,0,1)), wh(i + vec3(1,0,1)), f.x), mix(wh(i + vec3(0,1,1)), wh(i + vec3(1,1,1)), f.x), f.y), f.z); }
float wfbm(vec3 p){ float s = 0.0, a = 0.5; for (int i = 0; i < 4; i++) { s += a*wn(p); p = p*2.03 + vec3(1.7, 9.2, 3.1); a *= 0.5; } return s; }
vec2 wvor(vec3 x){ vec3 p = floor(x), f = fract(x); float d1 = 8.0, d2 = 8.0;
  for (int k = -1; k <= 1; k++) for (int j = -1; j <= 1; j++) for (int i = -1; i <= 1; i++) {
    vec3 b = vec3(float(i), float(j), float(k)); vec3 r = b - f + vec3(wh(p + b), wh(p + b + 11.3), wh(p + b + 27.1));
    float d = dot(r, r); if (d < d1) { d2 = d1; d1 = d; } else if (d < d2) { d2 = d; } }
  return vec2(sqrt(d1), sqrt(d2) - sqrt(d1)); }
vec3 blackbody(float t){ t = clamp(t, 0.0, 1.6);
  vec3 c = vec3(1.0, 0.16, 0.02)*smoothstep(0.0, 0.3, t) + vec3(0.0, 0.42, 0.10)*smoothstep(0.25, 0.65, t) + vec3(0.0, 0.32, 0.62)*smoothstep(0.6, 1.1, t);
  return c*(0.15 + 7.0*t*t); }
float hexEdge(vec2 p){ const vec2 s = vec2(1.0, 1.7320508);
  vec4 hc = floor(vec4(p, p - vec2(0.5, 1.0))/s.xyxy) + 0.5;
  vec4 h = vec4(p - hc.xy*s, p - (hc.zw + 0.5)*s);
  vec2 q = dot(h.xy, h.xy) < dot(h.zw, h.zw) ? h.xy : h.zw; q = abs(q);
  return 0.5 - max(dot(q, s*0.5), q.x); }
// A weapon's hot core: its own colour at full strength, never white (owner, 2026-11-04: "please avoid the color white for weapons").
vec3 hotOf(vec3 c){ return c/max(max(c.r, c.g), max(c.b, 1e-3)); }
float triEdge(vec2 p){ vec2 q = vec2(p.x - p.y*0.5773503, p.y*1.1547005);
  float a = abs(fract(q.x) - 0.5), b = abs(fract(q.y) - 0.5), c = abs(fract(q.x + q.y) - 0.5);
  return 0.5 - max(max(a, b), c); }
`;

// -----------------------------------------------------------------------------------------------------------------
// The render pipeline: the sky and the battle drawn into one HDR target (half floats, 4x MSAA on WebGL 2), the heat
// haze drawn into its own target as screen offsets, bloom (three's UnrealBloomPass) added over the HDR target, then one
// final pass: the scene sampled through the haze, a touch of chromatic aberration where the haze bends it, ACES tone
// mapping, sRGB, a vignette and a dither so slow gradients never band. Quality 'phone' (the Compatibility renderer's
// budget, design section 5): bloom at half resolution, no haze, no aberration, no MSAA.
function pipeline(opt = {}) {
  const renderer = o.renderer, gl2 = renderer.capabilities.isWebGL2;
  const mk = (s) => new THREE.WebGLRenderTarget(4, 4, { type: THREE.HalfFloatType, samples: s });
  let hdr = mk(gl2 ? 4 : 0), hdr1 = mk(0);
  const hz = new THREE.WebGLRenderTarget(4, 4, { type: THREE.HalfFloatType });
  const bloom = new THREE.UnrealBloomPass(new THREE.Vector2(4, 4), opt.bloom ?? 0.85, opt.radius ?? 0.55, opt.threshold ?? 1.0);
  bloom.highPassUniforms.smoothWidth.value = 0.6;
  // bloom's own targets in half floats too: in bytes its wide blur steps into visible rings round a bright flash
  for (const rt of [bloom.renderTargetBright, ...bloom.renderTargetsHorizontal, ...bloom.renderTargetsVertical]) rt.texture.type = THREE.HalfFloatType;
  // and what goes into it clamped: a flare far brighter than 1 overran the blur's kernel and drew a square round itself
  const hp = bloom.materialHighPassFilter; hp.fragmentShader = hp.fragmentShader.replace('gl_FragColor = mix( outputColor, texel, alpha );', 'gl_FragColor = mix( outputColor, vec4( min( texel.rgb, vec3( 2.5 ) ), texel.a ), alpha );'); hp.needsUpdate = true;
  const fin = new THREE.ShaderMaterial({ depthTest: false, depthWrite: false,
    uniforms: { tScene: { value: null }, tHaze: { value: null }, uHaze: { value: 1 }, uExposure: { value: opt.exposure ?? 1.0 }, uTime: T, uAb: { value: 1 } },
    vertexShader: 'varying vec2 vUv; void main(){ vUv = uv; gl_Position = vec4(position.xy, 0.0, 1.0); }',
    fragmentShader: `uniform sampler2D tScene; uniform sampler2D tHaze; uniform float uHaze; uniform float uExposure; uniform float uTime; uniform float uAb; varying vec2 vUv;
      vec3 fit(vec3 v){ vec3 a = v*(v + 0.0245786) - 0.000090537; vec3 b = v*(0.983729*v + 0.4329510) + 0.238081; return a/b; }
      vec3 aces(vec3 c){ const mat3 I = mat3(0.59719, 0.07600, 0.02840, 0.35458, 0.90834, 0.13383, 0.04823, 0.01566, 0.83777);
        const mat3 O = mat3(1.60475, -0.10208, -0.00327, -0.53108, 1.10813, -0.07276, -0.07367, -0.00605, 1.07602);
        return clamp(O*fit(I*(c*uExposure/0.6)), 0.0, 1.0); }
      vec3 srgb(vec3 c){ return mix(c*12.92, 1.055*pow(c, vec3(1.0/2.4)) - 0.055, step(0.0031308, c)); }
      void main(){
        vec2 off = texture2D(tHaze, vUv).rg*uHaze; float bend = length(off);
        vec2 uv = vUv + off; vec2 ab = (vUv - 0.5)*0.0015*uAb + off*0.35*uAb;
        vec3 c = vec3(texture2D(tScene, uv + ab).r, texture2D(tScene, uv).g, texture2D(tScene, uv - ab).b);
        c = srgb(aces(c));
        vec2 q = vUv - 0.5; c *= 1.0 - 0.32*dot(q, q)*1.6;
        float n = fract(sin(dot(gl_FragCoord.xy + fract(uTime)*91.7, vec2(12.9898, 78.233)))*43758.5453);
        c += (n - 0.5)/255.0;
        gl_FragColor = vec4(c, 1.0); }` });
  const quad = new THREE.Mesh(new THREE.PlaneGeometry(2, 2), fin); quad.frustumCulled = false;
  const qs = new THREE.Scene(); qs.add(quad); const qc = new THREE.OrthographicCamera(-1, 1, 1, -1, 0, 1);
  const P = { quality: 'high', bloom, size: [4, 4] };
  P.setSize = (w, h, pr) => {
    const W = Math.max(4, Math.round(w * pr)), H = Math.max(4, Math.round(h * pr)); P.size = [W, H];
    hdr.setSize(W, H); hdr1.setSize(W, H); hz.setSize(Math.round(W / 2), Math.round(H / 2));
    const k = P.quality === 'phone' ? 0.5 : 1; bloom.setSize(Math.round(W * k), Math.round(H * k));
  };
  P.setQuality = (q) => { P.quality = q; const [W, H] = P.size; const k = q === 'phone' ? 0.5 : 1; bloom.setSize(Math.round(W * k), Math.round(H * k)); };
  P.render = (skyScene, cam, dt) => {
    const phone = P.quality === 'phone', target = phone ? hdr1 : hdr;
    renderer.autoClear = false;
    renderer.setRenderTarget(target); renderer.setClearColor(0x000000, 1); renderer.clear();
    if (skyScene) renderer.render(skyScene, cam);
    renderer.clearDepth(); renderer.render(scene, cam);
    if (!phone) { renderer.setRenderTarget(hz); renderer.setClearColor(0x000000, 0); renderer.clear(); renderer.render(hazeScene, cam); }
    bloom.render(renderer, null, target, dt, false);
    fin.uniforms.tScene.value = target.texture; fin.uniforms.tHaze.value = hz.texture;
    fin.uniforms.uHaze.value = phone ? 0 : 1; fin.uniforms.uAb.value = phone ? 0 : 1;
    renderer.setRenderTarget(null); renderer.render(qs, qc);
  };
  P.final = fin;
  return P;
}

// -----------------------------------------------------------------------------------------------------------------
// A camera-facing strip between two points (beams, joiner beams, a glowing barrel). Its vertex shader turns the strip
// to face the camera about the beam's axis, so it reads as a round column from any side. Looks:
//   chorus  a white core in Sky glow, flowing light, Lilac strands winding round it (two, plus one for every ship that
//           joins), a bright node where the joining beams meet it; a front that races out at uReach
//   join    a thinner beam, one strand, that ends in the chorus's node
//   lance   the standard lance (every faction's beam): a white core in the weapon's colour, flowing light, pulses racing
//           to the target
//   bolt    a disruptor bolt: a short strip from its tail to its head, narrow and dim behind, a hot core at the head
//   barrel  the forge driver's spine heating: blackbody along its length, brightest at the muzzle
const RIBBON_FS = {
  chorus: `
    float core = exp(-x*x*90.0), glow = exp(-abs(x)*4.2)*0.55 + exp(-abs(x)*1.4)*0.12;
    float flow = wfbm(vec3(s*0.035 - uTime*9.0, x*3.0, uTime*0.7));
    vec3 c = uColor*(glow*(1.3 + 1.4*flow)) + hotOf(uColor)*core*(1.5 + 1.0*flow);
    float strands = 0.0;
    for (int i = 0; i < 5; i++) { if (float(i) >= uStrands) break;
      float ph = float(i)*6.2831853/uStrands; float w = s*0.055 - uTime*11.0 + ph;
      float xs = 0.42*sin(w); float front = 0.55 + 0.45*cos(w);
      strands += exp(-pow((x - xs)/0.07, 2.0))*front; }
    c += uColor2*strands*2.4*(0.7 + 0.6*flow);
    float node = exp(-pow((s - uFocus)/(uWidth*0.9), 2.0))*step(0.5, uStrands - 2.0);
    c += (uColor2*0.6 + hotOf(uColor)*0.9)*node*(1.2 + 0.8*sin(uTime*40.0))*exp(-abs(x)*2.0);
    float rings = pow(0.5 + 0.5*sin(s*0.22 - uTime*60.0), 24.0)*exp(-abs(x)*3.0)*0.35;
    c += uColor*rings;`,
  join: `
    float core = exp(-x*x*70.0), glow = exp(-abs(x)*3.5)*0.6;
    float flow = wfbm(vec3(s*0.05 - uTime*10.0, x*3.0, 3.1));
    vec3 c = uColor*glow*(0.6 + 0.7*flow) + hotOf(uColor)*core*2.0;
    float w = s*0.07 - uTime*12.0; c += uColor2*exp(-pow((x - 0.45*sin(w))/0.09, 2.0))*(0.6 + 0.4*cos(w))*1.2;`,
  lance: `
    float core = exp(-x*x*80.0), glow = exp(-abs(x)*4.0)*0.6 + exp(-abs(x)*1.6)*0.12;
    float flow = wfbm(vec3(s*0.06 - uTime*14.0, x*3.0, 1.7));
    float pulse = pow(0.5 + 0.5*sin(s*0.12 - uTime*70.0), 6.0);
    vec3 c = uColor*glow*(1.1 + 1.3*flow + 0.8*pulse) + hotOf(uColor)*core*(1.6 + 0.8*flow);`,
  bolt: `
    float k = clamp(s/max(uLen, 1.0), 0.0, 1.0); // 0 at the tail, 1 at the head
    float w = mix(0.15, 1.0, k*k); float xx = x/max(w, 0.05);
    float core = exp(-xx*xx*18.0)*pow(k, 3.0), glow = exp(-abs(xx)*3.0)*k*k;
    float flick = 0.8 + 0.4*wn(vec3(s*0.3 - uTime*40.0, x*2.0, 4.0));
    vec3 c = uColor*glow*1.6*flick + hotOf(uColor)*core*2.6;`,
  barrel: `
    float k = s/max(uLen, 1.0); float heat = uIntensity*(0.35 + 0.65*k)*(0.75 + 0.5*wfbm(vec3(s*0.4, x*2.0, uTime*3.0)));
    vec3 c = blackbody(heat*1.1)*(exp(-x*x*6.0)*0.8 + exp(-abs(x)*2.0)*0.25);`,
};
function ribbon(opt = {}) {
  const look = opt.look || 'chorus';
  const u = { uA: { value: V3() }, uB: { value: V3(0, 0, -1) }, uWidth: { value: opt.width ?? 6 }, uLen: { value: 1 }, uReach: { value: 1e9 },
    uIntensity: { value: 0 }, uColor: { value: new THREE.Color(opt.color ?? 0x3fa9f0) }, uColor2: { value: new THREE.Color(opt.color2 ?? 0xb7a6f2) },
    uStrands: { value: opt.strands ?? 2 }, uFocus: { value: -1e6 }, uTime: T };
  const g = new THREE.PlaneGeometry(1, 1, 1, 96);
  const m = new THREE.Mesh(g, new THREE.ShaderMaterial({ uniforms: u, transparent: true, depthWrite: false, blending: THREE.AdditiveBlending, side: THREE.DoubleSide,
    vertexShader: `uniform vec3 uA; uniform vec3 uB; uniform float uWidth; uniform float uLen; varying float vX; varying float vS;
      void main(){ float k = position.y + 0.5; vec3 p = mix(uA, uB, k); vec3 ax = normalize(uB - uA);
        vec3 side = normalize(cross(ax, normalize(cameraPosition - p))); p += side*position.x*uWidth;
        vX = position.x*2.0; vS = k*uLen; gl_Position = projectionMatrix*viewMatrix*vec4(p, 1.0); }`,
    fragmentShader: `uniform float uWidth; uniform float uLen; uniform float uReach; uniform float uIntensity; uniform vec3 uColor; uniform vec3 uColor2;
      uniform float uStrands; uniform float uFocus; uniform float uTime; varying float vX; varying float vS;
      ${GLSL}
      void main(){ float x = vX, s = vS;
        ${RIBBON_FS[look]}
        float front = 1.0 - smoothstep(uReach - uWidth*1.5, uReach, s);
        float ends = smoothstep(0.0, uWidth*0.6, s)*(1.0 - smoothstep(uLen - uWidth*0.3, uLen, s));
        float a = uIntensity*front*ends*(1.0 - smoothstep(0.82, 1.0, abs(x)));
        gl_FragColor = vec4(c*a, 1.0); }` }));
  m.frustumCulled = false; m.renderOrder = 5; scene.add(m);
  const R = { mesh: m, u, set(a, b) { u.uA.value.copy(a); u.uB.value.copy(b); u.uLen.value = a.distanceTo(b); }, remove() { scene.remove(m); } };
  return R;
}

// -----------------------------------------------------------------------------------------------------------------
// A flare: a camera-facing quad at a point. lens: a round core, a long thin horizontal streak and a faint halo ring
// (the Concord's clean light); tri: a three-pointed star of blades, turned to uRot (the Krai's triangle, never a
// round lens); soft: a plain glow; six: the pulse torpedo's six-armed star, after a telescope's diffraction spikes (owner,
// 2026-11-04: "pulse torpedo needs that iconic 6 armed flare"): three thin beaded spikes crossing at 60 degrees, a warm and
// a cyan fringe beside each, a faint strut pair across, and fine radial streaks in a hot core; the arms turn slowly and
// each flickers in length (owner: "the flare arms should be moving around");
// ball: a missile's plume, a big boiling ball of its drive colour round a white heart.
function flare(opt = {}) {
  const look = opt.look || 'soft';
  const u = { uColor: { value: new THREE.Color(opt.color ?? 0xffffff) }, uI: { value: 0 }, uSize: { value: opt.size ?? 20 }, uRot: { value: opt.rot ?? 0 }, uTime: T };
  const body = {
    lens: `float core = exp(-r*r*40.0)*3.0 + exp(-r*6.0)*0.6; float streak = exp(-abs(p.y)*90.0)*exp(-abs(p.x)*2.6)*1.4;
           float halo = exp(-pow((r - 0.62)/0.03, 2.0))*0.12; vec3 c = uColor*(core*0.6 + streak + halo) + hotOf(uColor)*exp(-r*r*120.0)*2.5;`,
    tri: `float a = atan(p.y, p.x) + uRot; float blade = pow(max(0.0, cos(a*3.0)), 40.0);
          float rays = blade*exp(-r*3.2)*2.2 + pow(max(0.0, cos(a*3.0 + 3.1415927)), 60.0)*exp(-r*7.0)*0.6;
          float core = exp(-r*r*50.0)*3.0; vec3 c = uColor*(rays + exp(-r*5.0)*0.5) + hotOf(uColor)*core;`,
    soft: `vec3 c = uColor*(exp(-r*r*12.0)*1.5 + exp(-r*4.0)*0.4);`,
    ball: `float body = exp(-r*r*7.0); float heart = exp(-r*r*55.0); float rim = exp(-pow((r - 0.42)/0.12, 2.0))*0.25;
           float boil = 0.85 + 0.3*wn(vec3(p*5.0, uTime*9.0));
           vec3 c = uColor*(body*1.6*boil + rim + exp(-r*3.0)*0.35) + hotOf(uColor)*heart*2.0;`,
    six: `float core = exp(-r*r*70.0)*3.2 + exp(-r*10.0)*0.9; float spikes = 0.0; vec3 fringe = vec3(0.0);
          float spin = uRot + uTime*0.9 + 0.15*sin(uTime*2.3);
          for (int i = 0; i < 3; i++) { float a = 1.5707963 + float(i)*1.0471976 + spin; vec2 d = vec2(cos(a), sin(a));
            float along = abs(dot(p, d)), perp = abs(p.x*d.y - p.y*d.x);
            float beads = 0.7 + 0.3*cos(along*110.0 - uTime*9.0);
            float reach = 2.0/(0.75 + 0.35*sin(uTime*7.0 + float(i)*2.1) + 0.15*sin(uTime*17.0 + float(i)));
            spikes += exp(-perp*perp*12000.0 - perp*70.0)*exp(-along*reach)*beads;
            fringe += vec3(1.0, 0.62, 0.32)*exp(-perp*30.0)*exp(-along*3.4)*0.22 + vec3(0.3, 0.85, 0.8)*exp(-pow((perp - 0.012)*260.0, 2.0))*exp(-along*2.6)*0.25; }
          vec2 ps = vec2(cos(spin)*p.x + sin(spin)*p.y, -sin(spin)*p.x + cos(spin)*p.y); float strut = exp(-abs(ps.y)*420.0)*exp(-abs(ps.x)*5.0)*0.45;
          float fine = pow(wn(vec3(atan(p.y, p.x)*36.0, 1.0, uTime*1.5)), 5.0)*exp(-r*6.5)*2.2;
          vec3 c = uColor*(spikes*2.4 + strut + exp(-r*4.0)*0.6) + fringe*0.6 + hotOf(uColor)*(core + fine);`,
  }[look];
  const m = new THREE.Mesh(new THREE.PlaneGeometry(2, 2), new THREE.ShaderMaterial({ uniforms: u, transparent: true, depthWrite: false, depthTest: opt.depthTest ?? true, blending: THREE.AdditiveBlending,
    vertexShader: 'uniform float uSize; varying vec2 vP; void main(){ vP = position.xy; vec4 mv = modelViewMatrix*vec4(0.0, 0.0, 0.0, 1.0); mv.xy += position.xy*uSize; gl_Position = projectionMatrix*mv; }',
    fragmentShader: `uniform vec3 uColor; uniform float uI; uniform float uRot; uniform float uTime; varying vec2 vP;
      ${GLSL}
      void main(){ vec2 p = vP; float r = length(p); ${body} gl_FragColor = vec4(c*uI*(1.0 - smoothstep(0.8, 1.0, r)), 1.0); }` }));
  m.frustumCulled = false; m.renderOrder = 6; scene.add(m);
  return { mesh: m, u, remove() { scene.remove(m); } };
}

// -----------------------------------------------------------------------------------------------------------------
// A ship's shield bubble: an ellipsoid round the hull in six facings of 60 degrees each, facing 0 at the bow and counted
// to starboard, as the battle rules count them (the battle console mockup's facingFrom). The Concord's is a hex lattice
// in Sky, the Krai's a triangle lattice in forge orange (each skin's own geometry: the Krai's triangles, the Concord's
// calm cells). At rest it cannot be seen at all. A hit lights a hot spot, a ring that runs out over the
// surface, and the lattice round it; a facing that holds less glows dimmer, and one that fails flashes, its cells
// breaking up by noise, then it is gone until it is raised again. Up to eight hits at once.
function shield(ship, opt = {}) {
  const box = new THREE.Box3().setFromObject(ship.model), size = box.getSize(V3()), centre = box.getCenter(V3());
  ship.root.worldToLocal(centre);
  // the bubble half again as large as the hull's box with its pad (owner, 2026-11-04: "I would make the shield bubble 50% larger")
  const rad = V3(size.x * 0.5 + opt.pad, size.y * 0.5 + opt.pad * 1.2, size.z * 0.5 + opt.pad).multiplyScalar(opt.scale ?? 1.5);
  const u = { uImp: { value: Array.from({ length: 8 }, () => new THREE.Vector4(0, 0, 1, -99)) }, uFace: { value: [1, 1, 1, 1, 1, 1] },
    uBreak: { value: [-99, -99, -99, -99, -99, -99] }, uColor: { value: new THREE.Color(opt.color ?? 0x3fa9f0) }, uTri: { value: opt.pattern === 'tri' ? 1 : 0 },
    uTime: T, uRest: { value: opt.rest ?? 0 } };
  const mat = new THREE.ShaderMaterial({ uniforms: u, transparent: true, depthWrite: false, blending: THREE.AdditiveBlending, side: THREE.DoubleSide,
    vertexShader: 'varying vec3 vN; varying vec3 vW; varying vec3 vWN; void main(){ vN = normalize(position); vec4 w = modelMatrix*vec4(position, 1.0); vW = w.xyz; vWN = normalize(mat3(modelMatrix)*normal); gl_Position = projectionMatrix*viewMatrix*w; }',
    fragmentShader: `uniform vec4 uImp[8]; uniform float uFace[6]; uniform float uBreak[6]; uniform vec3 uColor; uniform float uTri; uniform float uTime; uniform float uRest;
      varying vec3 vN; varying vec3 vW; varying vec3 vWN;
      ${GLSL}
      void main(){
        vec3 n = normalize(vN);
        float az = atan(n.x, n.z); // 0 at the bow (+z in the bubble's frame, turned to the ship's bow by the mesh)
        float k = floor(mod(az/1.0471976 + 0.5, 6.0));
        float face = 1.0, brk = -99.0;
        for (int i = 0; i < 6; i++) { if (abs(float(i) - k) < 0.5) { face = uFace[i]; brk = uBreak[i]; } }
        vec2 g = vec2(az*3.0, asin(clamp(n.y, -1.0, 1.0))*3.0)*2.2;
        float e = uTri > 0.5 ? triEdge(g) : hexEdge(g);
        float line = 1.0 - smoothstep(0.0, 0.06, e);
        float fres = pow(1.0 - abs(dot(normalize(vWN), normalize(cameraPosition - vW))), 3.0);
        float lit = 0.0, hot = 0.0, ring = 0.0;
        for (int i = 0; i < 8; i++) { float age = uTime - uImp[i].w; if (age < 0.0 || age > 3.0) continue;
          float d = acos(clamp(dot(n, normalize(uImp[i].xyz)), -1.0, 1.0));
          hot += exp(-d*d*60.0)*exp(-age*5.0)*3.0;
          ring += exp(-pow((d - age*1.7)/0.07, 2.0))*exp(-age*1.6);
          lit += exp(-d*5.0)*exp(-age*1.8); }
        float on = face;
        float bAge = uTime - brk, dissolve = 1.0;
        if (bAge >= 0.0 && bAge < 1.2) { float nz = wn(vec3(g*1.7, 3.0)); dissolve = step(bAge*0.9, nz); on = 1.0; lit += (1.0 - bAge)*2.0*line; }
        else if (bAge >= 1.2 && face < 0.01) { on = 0.0; }
        // nothing at rest (owner, 2026-11-04: "shield bubble invisible unless hit"): only a hit lights it
        float a = (line*lit*0.9*(0.5 + 0.5*face) + ring*(0.6 + line) + hot)*on*dissolve + uRest*fres*face*on;
        vec3 c = uColor*a + hotOf(uColor)*hot*0.5;
        gl_FragColor = vec4(c, 1.0); }` });
  const mesh = new THREE.Mesh(new THREE.SphereGeometry(1, 72, 36), mat);
  mesh.scale.copy(rad); mesh.position.copy(centre); mesh.rotation.y = PI; // the bubble's +z to the ship's bow (-z)
  mesh.renderOrder = 4; ship.root.add(mesh);
  let next = 0;
  const S = { mesh, u, rad, centre,
    facingOf(dirLocal) { const az = Math.atan2(-dirLocal.x, -dirLocal.z); return Math.floor(((az / (PI / 3) + 0.5) % 6 + 6) % 6); },
    hit(pLocal) { const d = pLocal.clone().sub(centre).divide(rad); d.set(-d.x, d.y, -d.z).normalize(); u.uImp.value[next++ % 8].set(d.x, d.y, d.z, T.value); },
    // Where a ray from outside meets the bubble (world space), or null.
    raycast(from, dir) {
      mesh.updateMatrixWorld(true); const inv = mesh.matrixWorld.clone().invert();
      const p = from.clone().applyMatrix4(inv), d = dir.clone().transformDirection(inv);
      const b = p.dot(d), c = p.dot(p) - 1, disc = b * b - c; if (disc < 0) return null;
      const t = -b - Math.sqrt(disc); if (t < 0) return null;
      return p.addScaledVector(d, t).applyMatrix4(mesh.matrixWorld);
    },
    set(k, v) { u.uFace.value[k] = v; }, fail(k) { u.uBreak.value[k] = T.value; u.uFace.value[k] = 0; },
    reset() { u.uFace.value.fill(1); u.uBreak.value.fill(-99); for (const q of u.uImp.value) q.w = -99; } };
  return S;
}

// -----------------------------------------------------------------------------------------------------------------
// Scars on a hull, drawn by the hull's own materials (patched here, so no decal floats over the skin and nothing
// z-fights): every MeshStandardMaterial of the ship takes a ship-space position and, after its emissive map, adds:
//   kind 0 molten crater (the forge slug, the bayonet's gouge stretched along its dir): a white-hot pool that cools by
//          blackbody to a dark crust, with fissures that glow forge orange out to twice its radius and fade last, the
//          same fissure look as the Krai's own hull, so the Krai leave their metal's mark
//   kind 1 beam burn (the chorus): a small white spot, a ring of char round it, cooling fast
//   the quell lattice: Lilac and Sky cells that grow out from the hit across the whole hull at 60 m/s, light pulsing
//          along them, and every light on the ship under them goes dark (the glows dimmed) until the stun ends, when
//          the lattice fades and the lights flicker back
// Up to eight scars a ship.
const SCAR_FS = `
uniform vec4 uScar[8]; uniform vec4 uScarK[8]; uniform vec4 uScarD[8]; uniform float uTime; uniform vec4 uLat; uniform float uLatEnd; uniform float uGlow;
varying vec3 vScarP;
${GLSL}
float scarDark = 0.0; vec3 scarEmit = vec3(0.0); float latDark = 0.0;
void scarsAt(vec3 p){
  for (int i = 0; i < 8; i++) {
    float age = uTime - uScarK[i].y; if (uScar[i].w <= 0.0 || age < 0.0) continue;
    vec3 q = p - uScar[i].xyz; vec3 dir = uScarD[i].xyz; float stretch = uScarD[i].w;
    float along = dot(q, dir); vec3 qa = q - dir*along*(1.0 - 1.0/max(stretch, 1.0));
    float r = uScar[i].w, d = length(qa)/r;
    float nz = wfbm(p*0.45 + float(i)*7.0);
    d += (nz - 0.5)*0.35;
    float cool = clamp(age/max(uScarK[i].z, 0.1), 0.0, 1.0);
    if (uScarK[i].x < 0.5) {
      float pool = 1.0 - smoothstep(0.55, 1.0, d);
      float heat = (1.0 - cool)*(1.0 - cool)*1.35 + 0.08*(1.0 - cool);
      vec2 v = wvor(p*0.22 + float(i));
      float crack = (1.0 - smoothstep(0.0, 0.07, v.y))*(1.0 - smoothstep(0.6, 2.0, d));
      float crackHeat = (1.0 - smoothstep(0.0, 1.0, cool*0.8))*0.55;
      scarEmit += blackbody(heat*(0.8 + 0.4*nz))*pool + blackbody(crackHeat)*crack*1.4;
      scarDark = max(scarDark, max(pool*0.88, (1.0 - smoothstep(0.8, 1.5, d))*0.55));
    } else {
      float spot = 1.0 - smoothstep(0.2, 0.7, d), ringC = 1.0 - smoothstep(0.5, 1.3, d);
      float heat = (1.0 - cool)*(1.0 - cool);
      scarEmit += blackbody(heat*1.25)*spot*2.0 + vec3(0.4, 0.7, 1.0)*spot*heat*2.0;
      scarDark = max(scarDark, ringC*0.7);
    }
  }
  float lAge = uTime - uLat.w;
  if (lAge >= 0.0 && uTime < uLatEnd + 2.5) {
    float dist = length(p - uLat.xyz), R = lAge*60.0;
    float cover = 1.0 - smoothstep(R - 10.0, R, dist);
    float fade = 1.0 - smoothstep(uLatEnd - 1.5, uLatEnd + 0.5, uTime);
    vec2 v = wvor(p*0.11 + vec3(0.0, uTime*0.05, 0.0));
    float line = 1.0 - smoothstep(0.0, 0.06, v.y);
    float pulse = 0.5 + 0.5*sin(dist*0.25 - uTime*9.0 + v.x*6.0);
    float front = exp(-pow((dist - R)/5.0, 2.0));
    vec3 lil = vec3(0.6, 0.45, 1.0), sky = vec3(0.16, 0.5, 1.0);
    scarEmit += mix(sky, lil, pulse)*line*cover*fade*(0.45 + 1.1*pulse) + vec3(0.75, 0.7, 1.0)*front*line*1.4*step(R, 400.0);
    float flicker = step(0.5, fract(sin(floor(uTime*14.0)*12.9898 + dist)*43758.5453));
    float back = smoothstep(uLatEnd - 0.2, uLatEnd + 1.2, uTime);
    latDark = cover*(1.0 - back*(0.4 + 0.6*flicker*step(uTime, uLatEnd + 1.2)) - back*step(uLatEnd + 1.2, uTime)*0.6);
    latDark = clamp(latDark, 0.0, 1.0);
  }
}`;
function scars(ship) {
  const U = { uScar: { value: Array.from({ length: 8 }, () => new THREE.Vector4()) }, uScarK: { value: Array.from({ length: 8 }, () => new THREE.Vector4()) },
    uScarD: { value: Array.from({ length: 8 }, () => new THREE.Vector4(0, 0, 1, 1)) }, uTime: T, uLat: { value: new THREE.Vector4(0, 0, 0, 1e6) }, uLatEnd: { value: -1e6 },
    uShipInv: { value: new THREE.Matrix4() } };
  let n = 0;
  ship.model.traverse((m) => {
    if (!m.isMesh || !m.material || !m.material.isMeshStandardMaterial) return;
    const mat = m.material.clone(), glow = mat.emissive && (mat.emissive.r + mat.emissive.g + mat.emissive.b) > 0.05 && mat.emissiveIntensity > 0;
    mat.onBeforeCompile = (s) => {
      Object.assign(s.uniforms, U); s.uniforms.uGlow = { value: glow ? 1 : 0 };
      s.vertexShader = 'uniform mat4 uShipInv; varying vec3 vScarP;\n' + s.vertexShader.replace('#include <project_vertex>', '#include <project_vertex>\n vScarP = (uShipInv*modelMatrix*vec4(transformed, 1.0)).xyz;');
      s.fragmentShader = SCAR_FS + '\n' + s.fragmentShader.replace('#include <emissivemap_fragment>', `#include <emissivemap_fragment>
        scarsAt(vScarP);
        diffuseColor.rgb *= 1.0 - scarDark;
        totalEmissiveRadiance *= 1.0 - (uGlow > 0.5 ? latDark*0.94 : 0.0);
        totalEmissiveRadiance += scarEmit;`);
    };
    mat.customProgramCacheKey = () => 'fc-scar-' + (glow ? 1 : 0);
    m.material = mat;
  });
  const S = { U,
    add(kind, pLocal, r, dirLocal = V3(0, 0, 1), dur = 6, stretch = 1) {
      const i = n++ % 8; U.uScar.value[i].set(pLocal.x, pLocal.y, pLocal.z, r); U.uScarK.value[i].set(kind, T.value, dur, 0);
      U.uScarD.value[i].set(dirLocal.x, dirLocal.y, dirLocal.z, stretch); },
    lattice(pLocal, stunS) { U.uLat.value.set(pLocal.x, pLocal.y, pLocal.z, T.value); U.uLatEnd.value = T.value + stunS; },
    clear() { for (const v of U.uScar.value) v.w = 0; U.uLat.value.w = 1e6; U.uLatEnd.value = -1e6; n = 0; },
    step() { ship.root.updateMatrixWorld(true); U.uShipInv.value.copy(ship.root.matrixWorld).invert(); } };
  return S;
}

// -----------------------------------------------------------------------------------------------------------------
// The quell torpedo: a soft white core in two counter-turning lattice shells (an icosahedron's edges, Lilac outside,
// Sky inside), its flare, and its trail.
function orb(opt = {}) {
  const g = new THREE.Group(), r = opt.r ?? 3;
  const shell = (rad, col, op) => new THREE.LineSegments(new THREE.EdgesGeometry(new THREE.IcosahedronGeometry(rad, 1)), new THREE.LineBasicMaterial({ color: new THREE.Color(col).multiplyScalar(op), blending: THREE.AdditiveBlending, transparent: true, depthWrite: false }));
  const a = shell(r, 0xb7a6f2, 2.0), b = shell(r * 0.62, 0x3fa9f0, 2.4); g.add(a, b);
  const core = flare({ look: 'soft', color: 0xb7a6f2, size: r * 2.2 }); scene.remove(core.mesh); g.add(core.mesh); core.u.uI.value = 1.2;
  const lens = flare({ look: 'lens', color: 0xb7a6f2, size: r * 7 }); scene.remove(lens.mesh); g.add(lens.mesh); lens.u.uI.value = 0.5;
  scene.add(g);
  const O = { group: g, step(dt) { a.rotation.x += dt * 2.1; a.rotation.y += dt * 1.3; b.rotation.y -= dt * 2.8; b.rotation.z += dt * 1.7; }, remove() { scene.remove(g); unlive(O); } };
  live.push(O); return O;
}
// A missile: a slim four-finned dart in gunmetal, its nose to -z, and its drive flame behind it in its grade's colour
// (owner, 2026-11-04: "missiles depending on grade will have: red, orange, blue, green", red the lowest); the page gives it a
// trail. Every faction fits missiles ("all factions can have missiles too"), so the body is plain.
function dart(opt = {}) {
  const g = new THREE.Group(), len = opt.len ?? 6, r = len * 0.06, mat = new THREE.MeshStandardMaterial({ color: 0x5a5f66, metalness: 0.7, roughness: 0.4 });
  const body = new THREE.Mesh(new THREE.CylinderGeometry(r, r, len * 0.8, 8), mat); body.rotation.x = PI / 2; g.add(body);
  const nose = new THREE.Mesh(new THREE.ConeGeometry(r, len * 0.2, 8), mat); nose.rotation.x = -PI / 2; nose.position.z = -len * 0.5; g.add(nose);
  for (let i = 0; i < 4; i++) { const f = new THREE.Mesh(new THREE.BoxGeometry(r * 0.25, r * 3.2, len * 0.18), mat); f.position.z = len * 0.33; f.rotation.z = i * PI / 2; f.translateY(r * 1.4); g.add(f); }
  // the plume: a big round ball of drive light just behind the body, several times its width, with a hot white heart
  // (owner, 2026-11-04, with a picture of the reference game's missiles: "they got that 3d missile model at the front end, a
  // big ball plume, and a tail"); the page draws the tail
  const ball = flare({ look: 'ball', color: opt.color ?? 0xffd27a, size: len * 1.6 }); scene.remove(ball.mesh); ball.mesh.position.z = len * 0.75; g.add(ball.mesh);
  scene.add(g);
  return { group: g, flame: ball, remove() { scene.remove(g); } };
}
// The forge slug: a three-sided spike (the Krai's triangle) of white-hot metal, its surface flowing like a pour, the
// colour by blackbody from white at the point to orange at its tail.
function spike(opt = {}) {
  const r = opt.r ?? 1.2, len = opt.len ?? 14;
  const geo = new THREE.ConeGeometry(r, len, 3, 8); geo.rotateX(-PI / 2); // point to -z, the way it flies
  const m = new THREE.Mesh(geo, new THREE.ShaderMaterial({ uniforms: { uTime: T, uLen: { value: len } },
    vertexShader: 'varying vec3 vP; varying vec3 vN; varying vec3 vW; void main(){ vP = position; vN = normalize(normalMatrix*normal); vec4 mv = modelViewMatrix*vec4(position, 1.0); vW = mv.xyz; gl_Position = projectionMatrix*mv; }',
    fragmentShader: `uniform float uTime; uniform float uLen; varying vec3 vP; varying vec3 vN; varying vec3 vW; ${GLSL}
      void main(){ float k = clamp(0.5 - vP.z/uLen, 0.0, 1.0); // 1 at the point
        float flow = wfbm(vec3(vP.xy*1.3, vP.z*0.4 + uTime*40.0));
        float fres = pow(1.0 - abs(dot(normalize(vN), normalize(-vW))), 2.0);
        float t = min(0.92, 0.5 + 0.4*k + 0.3*flow - 0.2*fres); // yellow at the hottest, never white
        gl_FragColor = vec4(blackbody(t), 1.0); }` }));
  m.renderOrder = 3; scene.add(m);
  return { mesh: m, remove() { scene.remove(m); } };
}

// -----------------------------------------------------------------------------------------------------------------
// A trail: the last n positions of a point drawn as a camera-facing strip that narrows and fades with age. Its strip is
// rebuilt on the CPU each step (n is small); look 'light' (a colour ramp) or 'heat' (blackbody, cooling along it).
function trail(opt = {}) {
  const N = opt.n ?? 48, W = opt.width ?? 4, life = opt.life ?? 0.6;
  const pts = [], pos = new Float32Array(N * 2 * 3), age = new Float32Array(N * 2), side = new Float32Array(N * 2);
  const idx = []; for (let i = 0; i < N - 1; i++) { const a = i * 2; idx.push(a, a + 1, a + 2, a + 1, a + 3, a + 2); }
  const g = new THREE.BufferGeometry(); g.setIndex(idx);
  g.setAttribute('position', new THREE.BufferAttribute(pos, 3).setUsage(THREE.DynamicDrawUsage));
  g.setAttribute('aAge', new THREE.BufferAttribute(age, 1).setUsage(THREE.DynamicDrawUsage));
  g.setAttribute('aSide', new THREE.BufferAttribute(side, 1));
  for (let i = 0; i < N * 2; i++) side[i] = i % 2 ? 1 : -1;
  const heat = opt.look === 'heat';
  const m = new THREE.Mesh(g, new THREE.ShaderMaterial({ transparent: true, depthWrite: false, blending: THREE.AdditiveBlending, side: THREE.DoubleSide,
    uniforms: { uColor: { value: new THREE.Color(opt.color ?? 0xb7a6f2) }, uColor2: { value: new THREE.Color(opt.color2 ?? 0x3fa9f0) }, uTime: T },
    vertexShader: 'attribute float aAge; attribute float aSide; varying float vAge; varying float vSide; void main(){ vAge = aAge; vSide = aSide; gl_Position = projectionMatrix*modelViewMatrix*vec4(position, 1.0); }',
    fragmentShader: `uniform vec3 uColor; uniform vec3 uColor2; uniform float uTime; varying float vAge; varying float vSide; ${GLSL}
      void main(){ float a = clamp(1.0 - vAge, 0.0, 1.0); float x = vSide; float body = exp(-x*x*3.0);
        vec3 c = ${heat ? 'blackbody(a*a*1.1)*(0.6 + 0.6*wn(vec3(vAge*30.0 - uTime*20.0, x*2.0, 1.0)))' : 'mix(uColor2, uColor, a)*(0.4 + 1.2*a*a) + hotOf(uColor)*exp(-x*x*20.0)*a*a*a*1.5'};
        gl_FragColor = vec4(c*body*a, 1.0); }` }));
  m.frustumCulled = false; m.renderOrder = 4; scene.add(m);
  const tmp = V3(), dirv = V3(), toCam = V3(), sv = V3();
  const R = { mesh: m, head: null, push(p, t) { pts.unshift({ p: p.clone(), t }); if (pts.length > N) pts.pop(); },
    clear() { pts.length = 0; },
    step() {
      const cam = o.camera.position; const now = T.value;
      for (let i = 0; i < N; i++) {
        const q = pts[Math.min(i, pts.length - 1)]; if (!q) { age[i * 2] = age[i * 2 + 1] = 9; continue; }
        const nb = pts[Math.min(i + 1, pts.length - 1)] || q, pb = pts[Math.max(i - 1, 0)] || q;
        dirv.copy(pb.p).sub(nb.p); if (dirv.lengthSq() < 1e-6) dirv.set(0, 0, 1); dirv.normalize();
        toCam.copy(cam).sub(q.p).normalize(); sv.crossVectors(dirv, toCam).normalize();
        const a = i < pts.length ? (now - q.t) / life : 9, w = W * Math.max(0, 1 - a) * 0.5 + W * 0.15;
        tmp.copy(q.p).addScaledVector(sv, -w); pos.set([tmp.x, tmp.y, tmp.z], i * 6);
        tmp.copy(q.p).addScaledVector(sv, w); pos.set([tmp.x, tmp.y, tmp.z], i * 6 + 3);
        age[i * 2] = age[i * 2 + 1] = a;
      }
      g.attributes.position.needsUpdate = true; g.attributes.aAge.needsUpdate = true;
    },
    remove() { scene.remove(m); unlive(R); } };
  live.push(R); return R;
}

// -----------------------------------------------------------------------------------------------------------------
// Beads: a discrete trail (owner, 2026-11-04: "torpedo should have like discrete trail instead of a long streak", then "close
// the gap on those trails, they are too far appart, large spheres to small spehres"): a glowing bead dropped every every_s
// seconds where the shot is, packed close, each staying where it was dropped and shrinking from large at the head to small
// at the tail as it fades out over its life ("torpedo tail should fade out"), with a little twinkle; one draw call. Sizes are in world units, drawn at their true size at any zoom.
function beads(opt = {}) {
  const N = opt.n ?? 64, pos = new Float32Array(N * 3), born = new Float32Array(N).fill(-99);
  const g = new THREE.BufferGeometry(); g.setAttribute('position', new THREE.BufferAttribute(pos, 3).setUsage(THREE.DynamicDrawUsage)); g.setAttribute('aBorn', new THREE.BufferAttribute(born, 1).setUsage(THREE.DynamicDrawUsage));
  const u = { uTime: T, uLife: { value: opt.life ?? 0.9 }, uSize: { value: opt.size ?? 3 }, uScale: { value: 500 }, uColor: { value: new THREE.Color(opt.color ?? 0x9fd0ff) } };
  const m = new THREE.Points(g, new THREE.ShaderMaterial({ uniforms: u, transparent: true, depthWrite: false, blending: THREE.AdditiveBlending,
    vertexShader: 'attribute float aBorn; uniform float uTime; uniform float uLife; uniform float uSize; uniform float uScale; varying float vK; varying float vSeed; void main(){ float k = clamp((uTime - aBorn)/uLife, 0.0, 1.0); vK = k; vSeed = aBorn*37.0; vec4 mv = modelViewMatrix*vec4(position, 1.0); gl_PointSize = k >= 1.0 ? 0.0 : clamp(uSize*(1.0 - 0.88*k)*uScale/max(-mv.z, 0.01), 0.0, 64.0); gl_Position = projectionMatrix*mv; }',
    fragmentShader: GLSL + 'uniform vec3 uColor; uniform float uTime; varying float vK; varying float vSeed; void main(){ vec2 q = gl_PointCoord - 0.5; float r = length(q); float dot_ = exp(-r*r*40.0); float heart = exp(-r*r*260.0); float cross_ = (exp(-abs(q.x)*60.0) + exp(-abs(q.y)*60.0))*exp(-r*9.0)*0.5; float tw = 0.8 + 0.2*sin(uTime*30.0 + vSeed); float a = pow(1.0 - vK, 3.0)*tw; gl_FragColor = vec4((uColor*(dot_*2.2 + cross_*1.4) + hotOf(uColor)*heart*2.2)*a, 1.0); }' }));
  m.frustumCulled = false; m.renderOrder = 4; scene.add(m);
  let next = 0, last = -99; const size = new THREE.Vector2(), lastP = new THREE.Vector3(), q = new THREE.Vector3();
  // beads between two pushes are filled in along the line between them, so the spacing is every_s of flight however far
  // the shot moved in one step
  const R = { mesh: m, u, every_s: opt.every_s ?? 0.03,
    push(p, now) {
      if (last < -50) { last = now; lastP.copy(p); return; }
      const n = Math.min(N, Math.floor((now - last) / R.every_s)); if (n < 1) return;
      for (let i = 1; i <= n; i++) { const k = i * R.every_s / (now - last); q.copy(lastP).lerp(p, k); pos.set([q.x, q.y, q.z], next * 3); born[next] = last + i * R.every_s; next = (next + 1) % N; }
      last += n * R.every_s; lastP.copy(q); g.attributes.position.needsUpdate = true; g.attributes.aBorn.needsUpdate = true; },
    step() { o.renderer.getDrawingBufferSize(size); u.uScale.value = size.y / (2 * Math.tan(o.camera.fov * PI / 360)); },
    remove() { scene.remove(m); unlive(R); } };
  live.push(R); return R;
}

// -----------------------------------------------------------------------------------------------------------------
// An exhaust plume from a drive bell: an open cone, brightest at its throat, with standing shock diamonds (the bright
// knots a hard-pushed exhaust shows) that tighten as uPush rises, faked volumetric by its facing to the camera. The
// Krai's is forge orange to white; it lengthens with uPush (0 cruising, 1 a full blood charge).
function plume(r, len, opt = {}) {
  const geo = new THREE.CylinderGeometry(r * 0.55, r, len, 24, 32, true); geo.translate(0, -len / 2, 0); geo.rotateX(-PI / 2); // throat at 0, out along +z
  const u = { uPush: { value: 0 }, uTime: T, uColor: { value: new THREE.Color(opt.color ?? 0xff8a2a) }, uLen: { value: len } };
  const m = new THREE.Mesh(geo, new THREE.ShaderMaterial({ uniforms: u, transparent: true, depthWrite: false, blending: THREE.AdditiveBlending, side: THREE.DoubleSide,
    vertexShader: 'uniform float uPush; uniform float uLen; varying float vK; varying vec3 vN; varying vec3 vV; void main(){ vec3 p = position; p.z *= 0.35 + 1.4*uPush; vK = clamp(position.z/uLen, 0.0, 1.0); vec4 mv = modelViewMatrix*vec4(p, 1.0); vN = normalize(normalMatrix*normal); vV = normalize(-mv.xyz); gl_Position = projectionMatrix*mv; }',
    fragmentShader: `uniform float uPush; uniform float uTime; uniform vec3 uColor; varying float vK; varying vec3 vN; varying vec3 vV; ${GLSL}
      void main(){ float face = pow(abs(dot(vN, vV)), 1.4);
        float nodes = 3.0 + 4.0*uPush; float dia = pow(0.5 + 0.5*cos(vK*nodes*6.2831853), 10.0)*(1.0 - vK)*uPush;
        float flick = 0.8 + 0.4*wn(vec3(vK*12.0 - uTime*30.0, uTime*5.0, 0.0));
        float body = (1.0 - vK)*(1.0 - vK)*face;
        vec3 c = uColor*body*(0.8 + 2.4*uPush)*flick + vec3(1.0, 0.85, 0.6)*dia*face*3.0 + hotOf(uColor)*pow(1.0 - vK, 12.0)*face*2.0*(0.4 + uPush);
        gl_FragColor = vec4(c*(0.25 + 0.75*min(1.0, uPush + 0.3)), 1.0); }` }));
  m.renderOrder = 3; m.frustumCulled = false;
  return { mesh: m, u };
}
// The bow shock: a paraboloid shell before a charging bow, lit at its rim (where you see through more of it), streaming
// back with noise; blood red at the rim, forge orange to white at the nose. uI 0..1 grows with speed over cruising.
function bowShock(r, len, opt = {}) {
  const prof = []; for (let i = 0; i <= 24; i++) { const k = i / 24; prof.push(new THREE.Vector2(r * Math.sqrt(k) + 0.01, -len * (1 - k))); }
  const geo = new THREE.LatheGeometry(prof, 48); geo.rotateX(PI / 2); // nose toward -z
  const u = { uI: { value: 0 }, uTime: T, uLen: { value: len } };
  const m = new THREE.Mesh(geo, new THREE.ShaderMaterial({ uniforms: u, transparent: true, depthWrite: false, blending: THREE.AdditiveBlending, side: THREE.DoubleSide,
    vertexShader: 'varying vec3 vP; varying vec3 vN; varying vec3 vV; void main(){ vP = position; vec4 mv = modelViewMatrix*vec4(position, 1.0); vN = normalize(normalMatrix*normal); vV = normalize(-mv.xyz); gl_Position = projectionMatrix*mv; }',
    fragmentShader: `uniform float uI; uniform float uTime; uniform float uLen; varying vec3 vP; varying vec3 vN; varying vec3 vV; ${GLSL}
      void main(){ float nose = clamp(-vP.z/uLen, 0.0, 1.0); // 1 at the nose
        float rim = pow(1.0 - abs(dot(vN, vV)), 2.5);
        float stream = wfbm(vec3(atan(vP.x, vP.y)*3.0, vP.z*0.12 + uTime*14.0, uTime*2.0));
        float a = uI*(rim*(0.5 + 1.2*stream) + pow(nose, 6.0)*1.8)*(0.3 + 0.7*nose);
        vec3 c = mix(vec3(0.82, 0.10, 0.06), vec3(1.0, 0.55, 0.16), nose)*a*2.2 + vec3(1.0, 0.55, 0.16)*pow(nose, 14.0)*uI*3.0;
        gl_FragColor = vec4(c, 1.0); }` }));
  m.renderOrder = 4; m.frustumCulled = false;
  return { mesh: m, u };
}

// -----------------------------------------------------------------------------------------------------------------
// An impact ring: a thin bright ring that breaks up as it runs out, square to the surface it struck. Plays once.
function ring(opt) {
  const u = { uK: { value: 0 }, uColor: { value: new THREE.Color(opt.color ?? 0xffb070) }, uTime: T, uSeed: { value: rnd() * 10 } };
  const m = new THREE.Mesh(new THREE.PlaneGeometry(2, 2), new THREE.ShaderMaterial({ uniforms: u, transparent: true, depthWrite: false, blending: THREE.AdditiveBlending, side: THREE.DoubleSide,
    vertexShader: 'varying vec2 vP; void main(){ vP = position.xy; gl_Position = projectionMatrix*modelViewMatrix*vec4(position, 1.0); }',
    fragmentShader: `uniform float uK; uniform vec3 uColor; uniform float uSeed; varying vec2 vP; ${GLSL}
      void main(){ float r = length(vP); float R = sqrt(uK)*0.95; float w = 0.02 + 0.06*uK;
        float br = wn(vec3(atan(vP.y, vP.x)*6.0, uK*3.0, uSeed));
        float a = exp(-pow((r - R)/w, 2.0))*(1.0 - uK)*(1.0 - uK)*(0.4 + 1.2*br) + exp(-r*r*30.0)*pow(1.0 - uK, 6.0)*3.0;
        gl_FragColor = vec4(uColor*a*2.0, 1.0); }` }));
  m.position.copy(opt.at); m.lookAt(opt.at.clone().add(opt.normal)); m.scale.setScalar(opt.size); m.renderOrder = 6; scene.add(m);
  const R = { t: 0, mesh: m, step(dt) { R.t += dt; u.uK.value = Math.min(1, R.t / (opt.dur ?? 1.2)); if (R.t >= (opt.dur ?? 1.2)) { scene.remove(m); unlive(R); } } };
  live.push(R); return R;
}

// -----------------------------------------------------------------------------------------------------------------
// Heat haze: a camera-facing quad in the haze scene whose colour is a screen offset (red x, green y), from flowing
// noise, strongest at its centre. Drawn only at high quality (the pipeline skips the haze pass on phones).
function haze(opt = {}) {
  const u = { uI: { value: 0 }, uSize: { value: opt.size ?? 20 }, uTime: T, uScroll: { value: opt.scroll ?? 1 } };
  const m = new THREE.Mesh(new THREE.PlaneGeometry(2, 2), new THREE.ShaderMaterial({ uniforms: u, transparent: true, depthWrite: false, depthTest: false, blending: THREE.AdditiveBlending,
    vertexShader: 'uniform float uSize; varying vec2 vP; void main(){ vP = position.xy; vec4 mv = modelViewMatrix*vec4(0.0, 0.0, 0.0, 1.0); mv.xy += position.xy*uSize; gl_Position = projectionMatrix*mv; }',
    fragmentShader: `uniform float uI; uniform float uTime; uniform float uScroll; varying vec2 vP; ${GLSL}
      void main(){ float r = length(vP); float fall = exp(-r*r*2.6);
        vec2 d = vec2(wn(vec3(vP*3.0, uTime*3.0*uScroll)), wn(vec3(vP*3.0 + 7.7, uTime*3.0*uScroll))) - 0.5;
        gl_FragColor = vec4(d*0.03*uI*fall, 0.0, 1.0); }` }));
  m.frustumCulled = false; hazeScene.add(m);
  return { mesh: m, u, remove() { hazeScene.remove(m); } };
}

// -----------------------------------------------------------------------------------------------------------------
// Motes of light gathering to a point: each starts on a ring round it (the saucer's rim), spirals in and brightens, so a
// charge reads before it fires. uI 0..1 is the charge; n points in one draw call.
function motes(opt) {
  const n = opt.n ?? 600, pos = new Float32Array(n * 3), br = new Float32Array(n), seeds = [];
  for (let i = 0; i < n; i++) seeds.push([rnd() * TAU, 0.6 + rnd() * 0.5, rnd(), (rnd() - 0.5) * 0.25]);
  const g = new THREE.BufferGeometry(); g.setAttribute('position', new THREE.BufferAttribute(pos, 3).setUsage(THREE.DynamicDrawUsage)); g.setAttribute('aB', new THREE.BufferAttribute(br, 1).setUsage(THREE.DynamicDrawUsage));
  const m = new THREE.Points(g, new THREE.ShaderMaterial({ transparent: true, depthWrite: false, blending: THREE.AdditiveBlending,
    uniforms: { uColor: { value: new THREE.Color(opt.color ?? 0x3fa9f0) }, uColor2: { value: new THREE.Color(opt.color2 ?? 0xb7a6f2) }, uScale: { value: 900 } },
    vertexShader: 'attribute float aB; uniform float uScale; varying float vB; void main(){ vB = aB; vec4 mv = modelViewMatrix*vec4(position, 1.0); gl_PointSize = clamp((0.5 + 1.5*aB)*uScale/max(-mv.z, 1.0), 0.0, 40.0); gl_Position = projectionMatrix*mv; }',
    fragmentShader: 'uniform vec3 uColor; uniform vec3 uColor2; varying float vB; void main(){ vec2 q = gl_PointCoord - 0.5; float a = exp(-dot(q, q)*18.0); gl_FragColor = vec4(mix(uColor2, uColor, vB)*a*vB*3.0, 1.0); }' }));
  m.frustumCulled = false; m.renderOrder = 5; scene.add(m);
  const R = { mesh: m, i: 0, centre: V3(), axis: V3(0, 1, 0), r: opt.r ?? 50,
    step() { const ax = R.axis, a = V3(1, 0, 0); if (Math.abs(ax.dot(a)) > 0.9) a.set(0, 0, 1); const b1 = a.cross(ax).normalize(), b2 = ax.clone().cross(b1);
      for (let i = 0; i < n; i++) { const [th, rr, ph, h] = seeds[i];
        const k = (ph + T.value * 0.55) % 1, life = Math.min(1, R.i * 1.4), rad = R.r * rr * (1 - k) ** 1.6, ang = th + k * 7.0;
        const p = R.centre.clone().addScaledVector(b1, Math.cos(ang) * rad).addScaledVector(b2, Math.sin(ang) * rad).addScaledVector(ax, h * R.r * (1 - k));
        pos.set([p.x, p.y, p.z], i * 3); br[i] = life * k * (ph < R.i ? 1 : 0); }
      g.attributes.position.needsUpdate = true; g.attributes.aB.needsUpdate = true; },
    remove() { scene.remove(m); unlive(R); } };
  live.push(R); return R;
}

function step(dt) { T.value += dt; for (const e of live.slice()) e.step(dt); }
function clear() { for (const e of live.slice()) { if (e.remove) e.remove(); else { if (e.mesh) scene.remove(e.mesh); unlive(e); } } }

return { GLSL, T, time: () => T.value, pipeline, ribbon, flare, shield, scars, orb, spike, dart, trail, beads, plume, bowShock, ring, haze, motes, step, clear, live };
};
