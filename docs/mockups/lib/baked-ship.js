// Fleet Command mockups: a baked ship on a page (the ship-detailing skill, "On the page"). One implementation of how a
// ship baked by tools/blender/painted_bake.py --ship is shown: its UV mesh from the manifest, its maps, its two looks over
// one geometry (Painted: the painted map with the painter's sun in it; PBR: base colour, ORM and tangent normals under
// the live light) with its baked hull lights added (diffuse x light^gamma x k, the light map stored with a gamma curve) and
// its emission, and the fixtures that show themselves (strobes flash, beacons pulse, the rest burn steady). Used by
// docs/mockups/kelvane-ship-concepts.html (mockup 23). The Ssarith Hunt page (mockup 12) still carries its own fuller copy
// of these rules, with damage, panes and breakup woven in; moving it onto this file is a task of kelvane-ship-concepts
// (CLAUDE.md 6.1). Edit it here, then re-run tools/mockups/inline_data.py on each page that inlines it.
//
// FCBakedShip(THREE, { onTexture, normals }) returns { tex, geometry, maps, looks, fixtures, blink }. onTexture(promise):
// called for every texture's load, so the page can wait for them before it calls itself ready. normals(points, deg): smooth
// normals within deg (hull-grow.js creasedNormals), for a ship baked shaded smooth (its manifest's smooth_deg).
window.FCBakedShip = function FCBakedShip(THREE, opts) {
'use strict';
const { onTexture = () => {}, normals = null } = opts || {};
/** A texture from a data URI; flipY on, as Blender's UV v runs up the image. srgb: a colour map. */
function tex(src, srgb = false) {
  const img = new Image(), t = new THREE.Texture(img); t.anisotropy = 8; if (srgb) t.encoding = THREE.sRGBEncoding;
  onTexture(new Promise((res) => { img.onload = () => { t.needsUpdate = true; res(); }; img.onerror = res; })); img.src = src; return t;
}
/** The ship's UV mesh (sh.mesh: flat triangle lists in ship space), indexed, with uv2 for the AO map and tangents; shaded
 *  smooth within sh.smooth_deg when the bake was, else flat, so its normal map lies on the normals it was baked against. */
function geometry(sh) {
  const g = new THREE.BufferGeometry(), n = sh.mesh.pos.length / 3;
  g.setAttribute('position', new THREE.Float32BufferAttribute(sh.mesh.pos, 3)); g.setAttribute('uv', new THREE.Float32BufferAttribute(sh.mesh.uv, 2));
  g.setAttribute('uv2', new THREE.Float32BufferAttribute(sh.mesh.uv, 2)); g.setIndex(Array.from({ length: n }, (_, k) => k));
  if (sh.smooth_deg && normals) { const P = []; for (let k = 0; k < n; k++) P.push(new THREE.Vector3(sh.mesh.pos[3 * k], sh.mesh.pos[3 * k + 1], sh.mesh.pos[3 * k + 2]));
    g.setAttribute('normal', new THREE.BufferAttribute(normals(P, sh.smooth_deg), 3)); } else g.computeVertexNormals();
  g.computeTangents(); return g;
}
/** A ship's maps from its manifest BK (pieces <slug>_<map>, data URIs); a map not baked yet is black. */
function maps(BK, slug) {
  const sh = BK.ships[slug];
  const t = (k, srgb) => { const pc = BK.pieces[slug + '_' + k]; if (pc) return tex(pc.data, srgb);
    const d = new THREE.DataTexture(new Uint8Array([0, 0, 0, 255]), 1, 1); d.needsUpdate = true; return d; };
  return { sh, slug, geo: geometry(sh), albedo: t('albedo', true), base: t('base', true), em: t('emit', true), nm: t('normal'), lt: t('light'), orm: t('orm'), g: sh.light_gamma || 1.6 };
}
/**
 * The two looks over one geometry. U: { lightK, selfP, none } uniforms ({ value }) the page sets for its light (the
 * hull lights' strength, and how much of the painted map's own light shows as emission). patch(shader): an optional hook
 * after this file's own changes, for a page's further rules.
 */
function looks(M, U, { patch = null, env = 0.5 } = {}) {
  const hull = (mat, self, key) => { mat.onBeforeCompile = (s) => { Object.assign(s.uniforms, { uHullLight: { value: M.lt }, uLightK: U.lightK, uSelf: self });
      s.fragmentShader = s.fragmentShader.replace('#include <common>', '#include <common>\nuniform sampler2D uHullLight; uniform float uLightK; uniform float uSelf;')
        .replace('#include <emissivemap_fragment>', `#include <emissivemap_fragment>
          totalEmissiveRadiance += diffuseColor.rgb * (pow(texture2D(uHullLight, vUv).rgb, vec3(${M.g.toFixed(2)})) * uLightK + uSelf);`);
      if (patch) patch(s); };
    mat.customProgramCacheKey = () => key; return mat; };
  return {
    painted: hull(new THREE.MeshStandardMaterial({ map: M.albedo, emissiveMap: M.em, emissive: 0xffffff, normalMap: M.nm, roughness: 0.72, metalness: 0.15, envMapIntensity: 0 }), U.selfP, 'baked-painted-' + M.slug),
    pbr: hull(new THREE.MeshStandardMaterial({ map: M.base, normalMap: M.nm, roughnessMap: M.orm, metalnessMap: M.orm, aoMap: M.orm, roughness: 1, metalness: 1,
      emissiveMap: M.em, emissive: 0xffffff, envMapIntensity: env }), U.none, 'baked-pbr-' + M.slug),
  };
}
/** The fixtures that show themselves, as additive sprites at their places (sh.fixtures): [{ s, kind }]. */
function fixtures(sh, sprite, parent) {
  return (sh.fixtures || []).map((F) => { const s = new THREE.Sprite(new THREE.SpriteMaterial({ map: sprite, color: new THREE.Color(...F.rgb), blending: THREE.AdditiveBlending, depthWrite: false, transparent: true }));
    s.position.set(...F.pos); const big = { strobe: 3.2, beacon: 3.0, position: 2.6 }[F.kind] || 1.6; s.scale.setScalar(big); s.userData.big = big; parent.add(s); return { s, kind: F.kind }; });
}
/** Strobes flash twice every 1.5 s, beacons pulse, the rest burn steady; brighter and larger at night. */
function blink(navs, t, night) {
  for (const { s, kind } of navs) { const p = t % 1.5;
    const on = kind === 'strobe' ? (p < 0.06 || (p > 0.2 && p < 0.26) ? 1 : 0) : kind === 'beacon' ? 0.25 + 0.75 * Math.max(0, Math.sin(t * Math.PI)) ** 3 : 0.8;
    s.material.opacity = on * (night ? 1 : 0.55); s.scale.setScalar(s.userData.big * (night ? 1 : 0.6)); }
}
return { tex, geometry, maps, looks, fixtures, blink };
};
