// Fleet Command mockups: the finished faction emblems as 3D badges, one implementation (CLAUDE.md section 6.1) of each
// badge's parts, heights and special materials, shared by its own badge page (docs/mockups/concord-emblem-3d.html,
// krai-emblem-3d.html, ssarith-emblem-3d.html) and the game flow's faction screen (docs/mockups/game-flow.html). Moved
// here from the badge pages unchanged. Each builder takes the shared part builder (FCEmblemParts, or FCEmblemBadge's B:
// { mmMaterial, slab, badge }) and the badge's data, builds into `badge`, and returns { tick(dt), still() } for a badge
// with an animated material (the Concord's holographic field). 1 unit = 1 mm, the badge 100 mm across, face toward +Z.
// Needs docs/mockups/lib/emblem-badge.js inlined first. Edit it here, then re-run tools/mockups/inline_data.py on each page.
window.FCEmblemBuilds = (function () {
'use strict';
const none = { tick() {}, still() {} };

// The Concord, B-E27 the smooth keyhole. PARTS: assets/emblems/concord/b-e27.parts.json.
function concord(THREE, { mmMaterial, slab, badge }, PARTS) {
  let holoTime = 0;
  const platinum = mmMaterial('emblem_platinum', { envMapIntensity: 1.0 });
  // The ship is iridescent pearl (owner, 2026-11-04: "Central shape should be like irridescent"): a thin film over the
  // pearl enamel whose colour runs through blue, violet and green with the viewing angle.
  const pearl = mmMaterial('emblem_enamel_pearl', { metalness: 0, clearcoat: 0.5, clearcoatRoughness: 0.06, envMapIntensity: 0.7,
    iridescence: 1.0, iridescenceIOR: 1.7, iridescenceThicknessRange: [280, 620], color: new THREE.Color('#c9d1e6') });
  // The pearl's own colour shift: a pastel rainbow that runs with the viewing angle and drifts across the face, mixed
  // into the lit enamel, so the ship reads iridescent face on as well as at a slant (the thin film above colours only
  // its reflections). The Blender render does the same with a facing ramp (tools/blender/emblem_concord.py, pearl_film).
  pearl.onBeforeCompile = (sh) => {
    sh.fragmentShader = sh.fragmentShader.replace('#include <output_fragment>', `
      float ndv = clamp(dot(normal, normalize(vViewPosition)), 0.0, 1.0);
      vec3 film = 0.62 + 0.38 * cos(6.2832 * (vec3(0.0, 0.33, 0.67) + (1.0 - ndv) * 1.5 + (vUv.x - vUv.y) * 0.6));
      outgoingLight *= mix(vec3(1.0), film * 1.15, 0.85);
      #include <output_fragment>`);
  };
  // The star's floor was satin Signal blue (owner, 2026-11-04: "make the start like a satin in blue"), now cyan below: the enamel
  // graph with its roughness doubled to a satin and a soft pale blue sheen, as in the Blender render (satin()).
  // Cyan (owner, 2026-11-04: "a cyan colored star in the middle"): the pearl enamel's maps tinted cyan, as in Blender.
  const starFloor = mmMaterial('emblem_enamel_pearl', { metalness: 0, roughness: 2.0, envMapIntensity: 0.6, color: new THREE.Color('#2fd0f0'),
    sheen: 1.0, sheenRoughness: 0.45, sheenColor: new THREE.Color('#b8f4ff') });
  const bowLight = new THREE.MeshStandardMaterial({ color: '#2fbf5a', emissive: '#2fbf5a', emissiveIntensity: 2.2, roughness: 0.1 });

  // ---- the holographic field --------------------------------------------------------------------------------------
  // The bell's night: stars on three layers set at different depths below the surface and two nebula layers, each
  // offset by the view direction (parallax), so the sky reads as deep space behind glass; a thin-film sheen whose hue
  // shifts with the viewing angle (the holographic look); a slow drift in time.
  const holo = new THREE.ShaderMaterial({
    uniforms: { time: { value: 0 } },
    vertexShader: `
      varying vec3 vWorld; varying vec3 vNormalW; varying vec2 vXY;
      void main() {
        vec4 w = modelMatrix * vec4(position, 1.0);
        vWorld = w.xyz; vXY = position.xy; vNormalW = normalize(mat3(modelMatrix) * normal);
        gl_Position = projectionMatrix * viewMatrix * w;
      }`,
    fragmentShader: `
      uniform float time;
      varying vec3 vWorld; varying vec3 vNormalW; varying vec2 vXY;
      float hash(vec2 p) { vec3 p3 = fract(vec3(p.xyx) * 0.1031); p3 += dot(p3, p3.yzx + 33.33); return fract((p3.x + p3.y) * p3.z); }
      float noise(vec2 p) { vec2 i = floor(p), f = fract(p); vec2 u = f * f * (3.0 - 2.0 * f);
        return mix(mix(hash(i), hash(i + vec2(1, 0)), u.x), mix(hash(i + vec2(0, 1)), hash(i + vec2(1, 1)), u.x), u.y); }
      float fbm(vec2 p) { float v = 0.0, a = 0.5; for (int i = 0; i < 6; i++) { v += a * noise(p); p *= 2.03; a *= 0.5; } return v; }
      // One layer of stars: a cell grid, a few cells lit with a soft point, twinkling slowly.
      float stars(vec2 p, float density, float size) {
        vec2 c = floor(p), f = fract(p); float h = hash(c);
        vec2 at = vec2(hash(c + 17.0), hash(c + 31.0)) * 0.7 + 0.15;
        float lit = step(1.0 - density, h);
        float tw = 0.75 + 0.25 * sin(time * (1.0 + 3.0 * hash(c + 5.0)) + h * 40.0);
        return lit * tw * smoothstep(size, 0.0, length(f - at));
      }
      void main() {
        vec3 V = normalize(cameraPosition - vWorld);
        vec3 N = normalize(vNormalW);
        // The view's slant across the face, for parallax: a layer at depth d (mm) is seen offset by d * slant.
        vec2 slant = V.xy / max(V.z, 0.3);
        vec2 p = vXY;
        float n1 = fbm((p - slant * 6.0) * 0.045 + vec2(time * 0.01, 0.0));
        float n2 = fbm((p - slant * 12.0) * 0.022 + vec2(3.7, time * 0.006));
        float lanes = smoothstep(0.38, 0.7, fbm((p - slant * 9.0) * 0.03 + 11.0));
        vec3 night = vec3(0.012, 0.018, 0.055);
        vec3 neb = mix(vec3(0.106, 0.165, 0.42), vec3(0.29, 0.16, 0.54), smoothstep(0.35, 0.65, n1));
        neb = mix(neb, vec3(0.10, 0.64, 0.77), smoothstep(0.62, 0.82, n2) * 0.8);
        neb = mix(neb, vec3(0.72, 0.65, 0.95), smoothstep(0.78, 0.92, n1) * 0.6);
        vec3 col = night + neb * lanes * 0.55;
        float s = stars((p - slant * 3.0) * 0.55, 0.06, 0.11) * 1.2 + stars((p - slant * 8.0) * 0.9, 0.05, 0.12) * 0.8
                + stars((p - slant * 16.0) * 1.6, 0.04, 0.14) * 0.5;
        col += vec3(0.85, 0.9, 1.0) * s;
        // Thin-film sheen: a hue that runs round with the viewing angle and across the face.
        float fres = pow(1.0 - max(dot(N, V), 0.0), 2.0);
        vec3 film = 0.5 + 0.5 * cos(6.2832 * (vec3(0.0, 0.33, 0.67) + fres * 1.6 + (p.x - p.y) * 0.006));
        col += film * (0.03 + 0.25 * fres);
        gl_FragColor = vec4(col, 1.0);
        #include <tonemapping_fragment>
        #include <encodings_fragment>
      }`,
  });
  const fieldSide = new THREE.MeshStandardMaterial({ color: '#0a1030', roughness: 0.5 });

  // ---- the parts, built from the guide's polygons (the shared slab builder) ----------------------------------------
  // Heights (mm) as in the Blender render: no two faces share a plane.
  slab(PARTS.frame, 0.0, 3.6, 0.8, platinum);
  slab(PARTS.field, 0.3, 1.0, 0.15, [holo, fieldSide]);
  // Each leg's body, and its segments over it: the marks between them are grooves 0.5 mm deep.
  PARTS.legs.forEach((leg, i) => { slab([leg], 0.5, 1.9, 0.35, pearl); PARTS.legSegments[i].forEach((seg) => slab([seg], 0.5, 2.4, 0.35, pearl)); });
  slab([PARTS.tail], 0.5, 2.4, 0.4, pearl);
  slab([PARTS.lens, PARTS.star], 0.6, 2.3, 0.45, pearl);    // the saucer's lower tier, the star cut through it
  slab([PARTS.lensTop, PARTS.star], 1.9, 2.9, 0.3, pearl);  // and its upper tier 1.6 mm inside the edge: the double bevel
  slab([PARTS.star], 0.5, 1.9, 0.2, starFloor);            // the star's floor set in the hole: a pocket 1.0 mm deep
  const lt = PARTS.light;
  const dome = new THREE.Mesh(new THREE.SphereGeometry(lt.r, 32, 16), bowLight);
  dome.scale.z = 0.4; dome.position.set(lt.cx - 50, 50 - lt.cy, 2.4); badge.add(dome);
  return { tick(dt) { holoTime += dt; holo.uniforms.time.value = holoTime; }, still() { holoTime = 4.0; holo.uniforms.time.value = holoTime; } };
}

// The Krai Warhost, KC+ the fang torc. PARTS: assets/emblems/krai/kc.parts.json.
function krai(THREE, { mmMaterial, slab }, PARTS) {
  // ---- the Krai's metals: Material Maker graphs, tinted as the Blender render tints them (tools/blender/emblem_krai.py) --
  const steel = mmMaterial('emblem_platinum', { color: new THREE.Color('#b9c2cc'), envMapIntensity: 1.0 });      // blade steel
  const blackened = mmMaterial('emblem_platinum', { color: new THREE.Color('#2b2426'), envMapIntensity: 0.6 });  // forged black
  const blood = mmMaterial('emblem_enamel_pearl', { metalness: 0, color: new THREE.Color('#c4161f'), clearcoat: 0.6, clearcoatRoughness: 0.08, envMapIntensity: 0.7 });

  // ---- the parts, built from the guide's polygons. Heights (mm) as in the Blender render: no two faces share a plane --
  slab(PARTS.ring, 0.0, 3.6, 0.8, steel);            // the rim, highest
  slab(PARTS.field, 0.3, 1.0, 0.15, blackened);      // the blackened field
  slab([PARTS.thin], 0.6, 2.2, 0.3, steel);          // the thin crescent outside the torc
  slab([PARTS.body], 0.6, 3.0, 0.6, steel);          // the faceted torc
  PARTS.fangs.forEach((f, i) => slab([f], 0.6, 2.6, 0.35, i === PARTS.founder ? blood : steel)); // the fangs, the founder's in blood enamel
  slab([PARTS.star], 0.6, 3.3, 0.5, steel);          // the forge star, struck proud
  return none;
}

// The Ssarith Hunt, SD the Bow Rites. PARTS: assets/emblems/ssarith/sd.parts.json; GROWN: assets/emblems/ssarith/sd.ferns.json.
function ssarith(THREE, { mmMaterial, slab, badge }, PARTS, GROWN) {
  // ---- the materials: Material Maker graphs, as the Blender render uses them (tools/blender/emblem_ssarith.py) -----------
  const rim = mmMaterial('emblem_platinum', { color: new THREE.Color('#d8cfb4'), envMapIntensity: 1.0 });            // bone-pale metal
  const wall = mmMaterial('emblem_wood_laminate', { color: new THREE.Color('#5a3e28'), envMapIntensity: 0.3 });           // the inner walls, a shadow box
  const floor = mmMaterial('emblem_enamel_forest', { color: new THREE.Color('#141812'), envMapIntensity: 0.2 });     // the recess's floor, in shadow
  const wood = mmMaterial('emblem_wood_laminate', { clearcoat: 0.15, clearcoatRoughness: 0.2, envMapIntensity: 0.5 });   // the bows
  const cord = mmMaterial('emblem_wood_laminate', { color: new THREE.Color('#5a4a38'), envMapIntensity: 0.3 });          // the strings
  const steel = mmMaterial('emblem_platinum', { color: new THREE.Color('#b9c2cc'), envMapIntensity: 1.0 });              // the shafts
  const green = mmMaterial('emblem_enamel_pearl', { metalness: 0, color: new THREE.Color('#1f9d3a'), clearcoat: 1.0, clearcoatRoughness: 0.04, envMapIntensity: 0.6 }); // the heads, hard green
  const feather = mmMaterial('emblem_enamel_pearl', { metalness: 0, color: new THREE.Color('#9e0c12'), envMapIntensity: 0.1, specularIntensity: 0.3 });  // deeper than the render's: the page's filmic tone map lifts it
  const featherTip = mmMaterial('emblem_enamel_pearl', { metalness: 0, color: new THREE.Color('#e0600a'), envMapIntensity: 0.1, specularIntensity: 0.3 });
  // the ferns: the graph's green pushed toward the photograph's (as the render's tint), waxy rather than glossy, each
  // vertex's light from the generator as its colour so the deep fronds fall into shadow
  const fern = mmMaterial('emblem_fern', { color: new THREE.Color('#7dff5c'), vertexColors: true, metalness: 0, specularIntensity: 0.25, envMapIntensity: 0.3, side: THREE.DoubleSide });
  const MATS = { string: cord, vanes: feather, vaneTips: featherTip, shaft: steel, head: green, limb: wood };

  // ---- the badge as a deep frame (owner, 2026-11-05: "it should be 3d"): the rim stands 10.4 mm, the field sinks to a floor at
  // 0.6 mm, and the fern bed fills the recess up to 6.4 mm. The bows float over the ferns as thin parts, each bow 0.9 mm
  // prouder than the one behind it. [top at level 0, thickness, bevel] in mm, the thicknesses differing so no two bottoms
  // share a plane; a feather's body and tip meet edge to edge, never overlapping (owner: "z fighting"). As the Blender render.
  const LEVEL = 0.9;
  const HEIGHTS = { string: [6.9, 0.73, 0.15], vanes: [7.1, 0.47, 0.15], vaneTips: [7.1, 0.47, 0.15], shaft: [7.3, 1.03, 0.3], head: [7.5, 0.91, 0.3], limb: [7.9, 1.79, 0.5] };
  slab(PARTS.frame, 0.0, 10.4, 0.8, rim);
  slab(PARTS.field, 0.3, 0.6, 0.1, floor);
  slab(PARTS.wall, 0.6, 9.8, 0.3, wall);   // the recess's inner walls, a band 2 mm wide inside the rim (owner: "need walls inner, on emblem")
  PARTS.bows.forEach((bow, i) => {
    const level = PARTS.bows.length - 1 - i;   // bows[0], the smallest, is in front
    for (const [part, [top, thick, bevel]] of Object.entries(HEIGHTS)) {
      const z1 = top + LEVEL * level;
      for (const ring of part === 'vanes' || part === 'vaneTips' ? bow[part] : [bow[part]]) slab([ring], z1 - thick, z1, bevel, MATS[part]);
    }
    // the limb's trim (the sight fin, the tally spurs, the forked hooks), a hair under the limb's top and over its bottom
    const [lt, lth] = HEIGHTS.limb, tz = lt + LEVEL * level - 0.12;
    for (const ring of bow.trim) slab([ring], tz - lth + 0.2, tz, 0.25, wood);
  });

  // ---- the fern bed (owner, 2026-11-05: "render a fern forrest sccene", "it should be 3d"), as tools/emblems/ssarith_ferns.py
  // grew it: bipinnate fronds in layers, finished triangles in hundredths of a mm (grid, y down), each vertex's light ---------
  {
    const v = GROWN.ferns.v, pos = new Float32Array(v.length), col = new Float32Array(v.length), uv = new Float32Array(v.length / 3 * 2);
    for (let i = 0, k = 0; i < v.length; i += 3, k += 2) {
      pos[i] = v[i] / 100 - 50; pos[i + 1] = 50 - v[i + 1] / 100; pos[i + 2] = v[i + 2] / 100;
      // the generator's light, lifted for this page's lighting (three.js's lights here are dimmer than the render's studio)
      col[i] = col[i + 1] = col[i + 2] = Math.min(1.6, 0.1 + 1.5 * Math.pow(GROWN.ferns.s[i / 3] / 100, 0.85));
      uv[k] = pos[i]; uv[k + 1] = pos[i + 1];   // the fern map projected flat in mm, as the badge's parts take theirs
    }
    const g = new THREE.BufferGeometry();
    g.setAttribute('position', new THREE.BufferAttribute(pos, 3));
    g.setAttribute('color', new THREE.BufferAttribute(col, 3));
    g.setAttribute('uv', new THREE.BufferAttribute(uv, 2));
    g.setAttribute('uv2', new THREE.BufferAttribute(uv, 2));
    g.setIndex(GROWN.ferns.f);
    g.computeVertexNormals();
    badge.add(new THREE.Mesh(g, fern));
  }
  return none;
}

// The Kelvane Institute, KID the hydrogen eigen plot, on an oval coin (owner, 2026-11-06: "I would like this hydrogen atom
// eigen plot used as the logo/emblem", "shade the 3d plot in the 3d render of the emblem, use our emblem designer for this
// please", "green hydrogen icon, irrdescent and red satin soft fabric background on an oval emblem coin"). All from
// tools/mockups/kelvane_emblem.py: ORBITAL (assets/emblems/kelvane/ka.orbital.json: the relief's vertices in badge mm, the
// full orbital's normals, UVs on longitude and colatitude), SATIN (ka.satin.json: the draped cloth's height field, the oval's
// size, the rim's height, the emblem's palette) and GRID (the wireframe mask over the orbital's UVs, an image, white on the
// lines). Parts, heights and materials as the Blender render (tools/blender/emblem_kelvane.py).
function kelvane(THREE, { mmMaterial, slab, badge }, ORBITAL, SATIN, GRID) {
  const K = SATIN.palette;
  const champagne = mmMaterial('emblem_platinum', { color: new THREE.Color(K.frame), envMapIntensity: 1.0 });   // the instruments' plate
  const ellipse = (a, b, n = 180) => Array.from({ length: n }, (_, i) => [50 + Math.cos(i / n * 2 * Math.PI) * a, 50 - Math.sin(i / n * 2 * Math.PI) * b]);
  slab([ellipse(...SATIN.oval_out), ellipse(...SATIN.oval_in)], 0.0, SATIN.rim_z, 0.8, champagne);   // the rim
  const mesh = (D, material) => {
    const g = new THREE.BufferGeometry();
    g.setAttribute('position', new THREE.BufferAttribute(new Float32Array(D.positions), 3));
    g.setAttribute('normal', new THREE.BufferAttribute(new Float32Array(D.normals), 3));
    g.setAttribute('uv', new THREE.BufferAttribute(new Float32Array(D.uvs), 2));
    g.setIndex(D.faces);
    const m = new THREE.Mesh(g, material); m.castShadow = m.receiveShadow = true; badge.add(m); return m;
  };
  // ---- the satin: red, a soft sheen at grazing angles, on the generator's draped folds ---------------------------------
  mesh(SATIN, new THREE.MeshPhysicalMaterial({ color: new THREE.Color(K.field), roughness: 0.42, metalness: 0,
    sheen: 1.0, sheenColor: new THREE.Color(K.sheen), sheenRoughness: 0.35, specularIntensity: 0.6, envMapIntensity: 0.5 }));
  // ---- the orbital: iridescent green under a hard coat, its wireframe inlaid light green, the mask choosing between them
  // (colour, metal, roughness and a faint light on the lines), on the full orbital's normals so it shades as the 3D orbital
  const w = GRID.width, h = GRID.height, cv = (fill) => { const c = document.createElement('canvas'); c.width = w; c.height = h; const x = c.getContext('2d'); fill(x); return c; };
  const mask = cv((x) => x.drawImage(GRID, 0, 0)).getContext('2d').getImageData(0, 0, w, h).data;
  const rgb = (hex) => { const n = parseInt(hex.slice(1), 16); return [n >> 16, (n >> 8) & 255, n & 255]; };
  const paint = (lineRGB, fieldRGB) => cv((x) => { const im = x.createImageData(w, h);
    for (let i = 0; i < mask.length; i += 4) { const m = mask[i] / 255; for (let c = 0; c < 3; c++) im.data[i + c] = fieldRGB[c] * (1 - m) + lineRGB[c] * m; im.data[i + 3] = 255; }
    x.putImageData(im, 0, 0); });
  const tex = (c, srgb) => { const t = new THREE.CanvasTexture(c); t.flipY = false; if (srgb) t.encoding = THREE.sRGBEncoding; t.anisotropy = 8; return t; };
  mesh(ORBITAL, new THREE.MeshPhysicalMaterial({
    map: tex(paint(rgb(K.wire), rgb(K.lit)), true),
    metalnessMap: tex(paint([0, 0, 0], [0, 0, 64])), metalness: 1.0,             // the film 0.25 metal, the lines none (B channel)
    roughnessMap: tex(paint([0, 89, 0], [0, 56, 0])), roughness: 1.0,            // lines 0.35, film 0.22 (G channel)
    emissive: new THREE.Color(K.wire), emissiveMap: tex(paint([255, 255, 255], [0, 0, 0]), true), emissiveIntensity: 0.25,
    clearcoat: 1.0, clearcoatRoughness: 0.04, iridescence: 1.0, iridescenceIOR: 1.7, iridescenceThicknessRange: [380, 660],
    envMapIntensity: 0.9, side: THREE.DoubleSide }));
  return none;
}

return { concord, krai, ssarith, kelvane };
})();
