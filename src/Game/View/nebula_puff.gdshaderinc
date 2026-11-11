// nebula_puff.gdshaderinc: how a nebula puff is lit, the one implementation for both kinds of puff (NebulaPuffs): the
// camera-facing billboards (nebula_billboard.gdshader) and the displaced meshes (nebula_mesh.gdshader). After Crab
// Breeze's clouds (RubenTipparach/crab-breeze at 5ad3de22, assets/shaders/cloud.wesl, the function puff; owner, 2026-11-10:
// "third technique to try out: crab-breezes cloud system", "cheap 3d billboards with lighting"): a puff is lit as a sphere,
// the sun wrapped round it (cloud scatters light deep into its shade side), darker toward its own underside, a share of
// the sun scattered inside it so a puff with the sun behind it is never black, a silver lining where the eye looks toward
// the sun through a thin edge, and the light of the nebula round it from all sides in its deep colour. The battle's one
// sun (sun_dir, sun_energy), as the planets and the plumes have it.

// A puff's light: nrm its surface's direction (world space), under 0 at its underside to 1 at its top, thin how thin it
// is here (0 its middle, 1 its edge, for the silver lining), look the eye's ray, lit and deep the look's colours.
vec3 puff_light(vec3 nrm, vec3 sun_l, float sun_energy, float under, float thin, vec3 look, vec3 lit, vec3 deep) {
	float wrap = 0.6;
	float diffuse = clamp((dot(nrm, sun_l) + wrap) / (1.0 + wrap), 0.0, 1.0);
	float shadow = mix(0.35, 1.0, under);
	float sun_k = 0.35 + min(sun_energy, 1.5);
	float silver = pow(max(dot(look, sun_l), 0.0), 6.0) * thin * thin * 0.9;
	float scattered = 0.25 * shadow;
	return lit * sun_k * (diffuse * shadow * 0.85 + silver + scattered) + deep * (0.6 + 0.6 * under);
}

// How much of a deck's puff shows as the camera climbs (or sinks) toward that deck (side +1 above the plane, -1 below,
// 0 a column's puff): the deck thins away so a captain who zooms out over the battle looks down through it.
float puff_seen(float side, float cam_y, float underside) {
	return abs(side) < 0.5 ? 1.0 : 1.0 - smoothstep(0.45, 0.85, side * cam_y / underside);
}
