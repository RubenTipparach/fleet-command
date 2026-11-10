// planet_clouds.gdshader: a planet's weather, a shell just above the ground (the planet-shaders skill). Ported from
// godot-sandbox (RubenTipparach/godot-sandbox at d5d8e461, resources/shaders/comic_cloud_layer.gdshader), whose notes
// explain each choice: the cover is warped noise on the sphere's direction, two layers turning against each other about
// a wind axis so shapes churn rather than slide; the edge thins out over a soft fringe; the tops have their own height
// map (the square root of the density above the edge, so a cloud climbs fast from its rim and rounds over its top, with
// a finer billow on it), differenced into a normal. Changes for Fleet Command: unshaded and lit here by the battle's sun
// (sun_dir), as the ground is, the same in Forward+ and Compatibility; the sun through thin tops at the terminator
// (backlight) is that light's own term here; each planet's seed moves its weather; PCG noise (planet_noise.gdshaderinc).
shader_type spatial;
render_mode unshaded, shadows_disabled, depth_draw_never, blend_mix, cull_back;

#include "res://src/Game/View/planet_noise.gdshaderinc"

uniform vec3 color_lit : source_color = vec3(1.0, 0.97, 0.92);
uniform vec3 color_base : source_color = vec3(0.84, 0.86, 0.92);
uniform float pattern_scale = 3.0;
uniform float coverage = 0.5;
uniform vec3 wind_axis = vec3(0.15, 1.0, 0.0);
uniform float wind_speed = 0.01;   // radians a second
uniform float relief_step = 0.06;
uniform float cloud_relief = 0.9;
uniform float relief_max_slope = 2.5;
uniform float backlight_amount = 0.35;
uniform float cloud_opacity = 0.9;
uniform float fringe_opacity = 0.5;
uniform float dome_height = 1.4;
uniform float billow_scale = 6.0;
uniform float billow_strength = 0.4;
uniform float edge_softness = 0.09;
uniform vec3 seed = vec3(0.0);
uniform vec3 sun_dir = vec3(0.4, 0.7, 0.6);   // toward the sun, world space
uniform vec3 sun_color : source_color = vec3(1.0, 0.96, 0.9);
uniform float sun_energy = 1.0;

varying vec3 mpos;

void vertex() {
	mpos = VERTEX;
}

float cloud_density(vec3 nd) {
	vec3 ax = normalize(wind_axis);
	vec3 p1 = pn_rot(nd, ax, TIME * wind_speed) * pattern_scale + seed;
	vec3 p2 = pn_rot(nd, ax, -TIME * wind_speed * 0.6) * pattern_scale * 1.9 + seed.zxy + 37.0;
	vec3 warp = vec3(pn_fbm3(p1 * 0.5 + 7.7), pn_fbm3(p1 * 0.5 - 3.1), pn_fbm3(p1 * 0.5 + 2.4)) - 0.5;
	return pn_fbm3(p1 + warp * 1.6) * 0.72 + pn_fbm3(p2) * 0.28;
}

float cloud_height(vec3 d, float t_edge) {
	float body = max(cloud_density(d) - t_edge, 0.0);
	float dome = sqrt(body) * dome_height;
	float billow = (pn_fbm3(d * pattern_scale * billow_scale + seed) - 0.5) * billow_strength;
	return dome + billow * smoothstep(0.0, 0.06, body);
}

void fragment() {
	vec3 nd = normalize(mpos);
	float n = cloud_density(nd);
	float t_edge = 1.0 - coverage * 0.72;
	float t_core = t_edge + edge_softness * 0.5;
	float edge_mask = smoothstep(t_edge, t_edge + edge_softness, n);
	float core_mask = smoothstep(t_core, t_core + edge_softness * 0.6, n);
	if (edge_mask <= 0.001) { discard; }

	vec3 ta, tb;
	pn_frame(nd, ta, tb);
	float footprint = max(length(dFdx(nd)), length(dFdy(nd)));
	float eps = max(relief_step, footprint * 1.5);
	float ha = cloud_height(normalize(nd + ta * eps), t_edge) - cloud_height(normalize(nd - ta * eps), t_edge);
	float hb = cloud_height(normalize(nd + tb * eps), t_edge) - cloud_height(normalize(nd - tb * eps), t_edge);
	vec3 n_model = pn_relief(nd, ta, tb, ha, hb, eps, cloud_relief, relief_max_slope);
	n_model = normalize(mix(nd, n_model, edge_mask));

	vec3 L = normalize(sun_dir);
	vec3 n_world = normalize(MODEL_NORMAL_MATRIX * n_model);
	vec3 g_world = normalize(MODEL_NORMAL_MATRIX * nd);
	// the sphere's own light, which the relief shades and lights by its turn from it (a cloud is never black by its slope
	// alone: the relief tipping a top away from the sun only shades it)
	float ndl_g = dot(g_world, L);
	float day = pn_day(ndl_g) * clamp(0.8 + 0.9 * (dot(n_world, L) - ndl_g), 0.45, 1.25);
	// thin tops glow where the sun comes through them, just past the terminator
	float back = backlight_amount * (1.0 - core_mask * 0.6) * smoothstep(-0.25, 0.0, ndl_g) * (1.0 - smoothstep(0.0, 0.3, ndl_g));
	vec3 col = mix(color_base, color_lit, core_mask);
	vec3 sun = sun_color * sun_energy;
	ALBEDO = col * (0.02 + 0.98 * day + back) * sun;
	ALPHA = mix(edge_mask * fringe_opacity, cloud_opacity, core_mask);
}
