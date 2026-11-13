// planet_rings.gdshader: a planet's rings (owner, 2026-11-09: "gas giants with rings"; the planet-shaders skill), on a flat
// square in the planet's equator plane, cut to the annulus between ring_inner and ring_outer (the planet's radius is 1).
// Their density by radius is pn_ring_density (planet_noise.gdshaderinc), the same the planet's tops take the rings'
// shadow from. Each is lit by the battle's sun: brighter seen from the lit face, dim but not dark from the other (light
// comes through thin ice), and dark where the planet's shadow falls across them (the sun's ray from the point meets the
// planet). The colour runs between two of the kind's colours by radius. Unshaded and drawn from both sides.
shader_type spatial;
render_mode unshaded, shadows_disabled, depth_draw_never, blend_mix, cull_disabled;

#include "res://src/Game/View/planet_noise.gdshaderinc"

uniform float ring_inner = 1.4;
uniform float ring_outer = 2.3;
uniform float ring_opacity = 0.85;
uniform float ring_seed = 0.0;
uniform vec3 color_in : source_color = vec3(0.70, 0.62, 0.52);
uniform vec3 color_out : source_color = vec3(0.90, 0.84, 0.74);
uniform vec3 sun_dir = vec3(0.4, 0.7, 0.6);
uniform vec3 sun_color : source_color = vec3(1.0, 0.96, 0.9);
uniform float sun_energy = 1.0;

varying vec3 mpos;

void vertex() {
	mpos = VERTEX;
#ifdef PLANET_BAKE
	POSITION = pn_bake_position(UV);
#endif
}

void fragment() {
	float r = length(mpos.xz);
	float d = pn_ring_density(r, ring_inner, ring_outer, ring_seed);
	if (d <= 0.002) { discard; }
	float t = (r - ring_inner) / (ring_outer - ring_inner);
	vec3 col = mix(color_in, color_out, smoothstep(0.0, 1.0, t));
	col *= 0.85 + 0.3 * pn_vnoise3(vec3(t * 220.0, ring_seed, 1.0));
	vec3 Lm = pn_sun(normalize(inverse(mat3(MODEL_MATRIX)) * normalize(sun_dir)), vec3(0.0, 1.0, 0.0));
	// the face toward the sun is lit; seen from the far face, light comes through
	vec3 Vm = pn_sun(normalize(inverse(mat3(MODEL_MATRIX)) * (INV_VIEW_MATRIX * vec4(-VERTEX, 0.0)).xyz), vec3(0.0, 1.0, 0.0));
	bool same_side = sign(Lm.y) == sign(Vm.y);
	float light = (same_side ? 0.35 + 0.65 * abs(Lm.y) : 0.25 * (1.0 - d * 0.6)) + 0.08;
	// the planet's shadow: the sun's ray from here meets the unit sphere
	vec3 o = vec3(mpos.x, 0.0, mpos.z);
	float b = dot(o, Lm);
	float c = dot(o, o) - 1.0;
	float disc = b * b - c;
	if (disc > 0.0 && -b - sqrt(disc) > 0.0) { light *= 0.04; }
	ALBEDO = col * light * sun_color * sun_energy;
	ALPHA = d * ring_opacity;
}
