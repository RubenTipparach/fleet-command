// Fleet Command mockups: the cargo container, one asset for every page that carries one (assets/containers/, written by
// tools/blender/build_container.py from a CC0 set; owner, 2026-11-07: "you need like better cargo textures, lookem up
// online for cc0 options, back ao and get diffuse and normals and specular for each container"; owner, 2026-11-11, of the
// Vos ships' plain boxes: "Containers need materials. We can reuse that from freighters"). Lifted from the freighters page
// (docs/mockups/freighter-concepts.html, mockup 24), which still carries its own copy until it next moves onto this file
// (CLAUDE.md 6.1; vos-ship-concepts task 1k.3). Edit it here, then re-run tools/mockups/inline_data.py on each page.
//
// FCContainer(THREE, M, onTexture) takes the container manifest M (assets/containers/manifest.json, inlined) and returns
//   geometry(scale)   the low-poly box built from the manifest's faces, so its UVs are the bake's; scale 2 gives the game's
//                     12 by 12 by 24 m container from the drafted 6 by 6 by 12 m
//   material(tint)    the baked maps (diffuse, tangent normals, AO, roughness, specular) on a material whose colour is the
//                     tint times the day level (the diffuse is grey, so each container's own colour tints it)
//   K                 the paint's level by the light: by day a little under white, at night as dark as an unlit hull
// onTexture(promise) is called for every map's load, so a page can wait for them before it calls itself ready.
window.FCContainer = function FCContainer(THREE, M, onTexture) {
'use strict';
onTexture = onTexture || (() => {});
const K = { day: 0.62, night: 0.16 };
const tex = (k, srgb) => { const pc = M.pieces[k], img = new Image(), t = new THREE.Texture(img); t.anisotropy = 8; if (srgb) t.encoding = THREE.sRGBEncoding;
  onTexture(new Promise((res) => { img.onload = () => { t.needsUpdate = true; res(); }; img.onerror = res; })); img.src = pc.data; return t; };
let maps = null;   // loaded once, shared by every tint
function geometry(scale = 1) {
  const [w, h, l] = M.size_m.map((v) => v * scale), A = M.atlas_m, P = [], N = [], UV = [], I = [], V3 = (x, y, z) => new THREE.Vector3(x, y, z);
  for (const F of Object.values(M.faces)) { const n = V3(...F.n), u = V3(...F.u), v = V3(...F.v), [su, sv] = F.size, c = V3(n.x * w / 2, n.y * h / 2, n.z * l / 2), b = P.length / 3;
    const pts = [[-1, -1], [1, -1], [1, 1], [-1, 1]].map(([a, e]) => { const p = c.clone().addScaledVector(u, a * su * scale / 2).addScaledVector(v, e * sv * scale / 2);
      P.push(p.x, p.y, p.z); N.push(n.x, n.y, n.z); UV.push((F.at[0] + (a + 1) / 2 * su) / A, (F.at[1] + (e + 1) / 2 * sv) / A); return p; });
    const out = V3().subVectors(pts[1], pts[0]).cross(V3().subVectors(pts[2], pts[0])).dot(n) > 0;
    I.push(...(out ? [b, b + 1, b + 2, b, b + 2, b + 3] : [b, b + 2, b + 1, b, b + 3, b + 2])); }
  const g = new THREE.BufferGeometry(); g.setAttribute('position', new THREE.Float32BufferAttribute(P, 3)); g.setAttribute('normal', new THREE.Float32BufferAttribute(N, 3));
  g.setAttribute('uv', new THREE.Float32BufferAttribute(UV, 2)); g.setAttribute('uv2', new THREE.Float32BufferAttribute(UV, 2)); g.setIndex(I); g.computeTangents(); return g;
}
function material(tint = 0xffffff) {
  maps ||= { map: tex('diffuse', true), normalMap: tex('normal'), aoMap: tex('ao'), roughnessMap: tex('rough'), specularIntensityMap: tex('specular') };
  const c = new THREE.Color(tint).multiplyScalar(K.day);
  return new THREE.MeshPhysicalMaterial({ color: c, ...maps, specularIntensity: 1, roughness: 1, metalness: 0.12, envMapIntensity: 0.25 });   // lit by the key light like the hulls
}
return { geometry, material, K };
};
