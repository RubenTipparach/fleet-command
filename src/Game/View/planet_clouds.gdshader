// planet_clouds.gdshader: a planet's weather, a shell just above the ground (the planet-shaders skill). Ported from
// godot-sandbox (RubenTipparach/godot-sandbox at d5d8e461, resources/shaders/comic_cloud_layer.gdshader), whose notes
// explain each choice: the cover is warped noise on the sphere's direction, two layers turning against each other about
// a wind axis so shapes churn rather than slide; the edge thins out over a soft fringe; the tops have their own height
// map (the square root of the density above the edge, so a cloud climbs fast from its rim and rounds over its top, with
// a finer billow on it), differenced into a normal. Changes for Fleet Command: unshaded and lit here by the battle's sun
// (sun_dir), as the ground is, the same in Forward+ and Compatibility; the sun through thin tops at the terminator
// (backlight) is that light's own term here; each planet's seed moves its weather; PCG noise (planet_noise.gdshaderinc).
// Made to read as weather, not as blobs (owner, 2026-11-10: "make planets more realistic please"): great systems whose
// edges a finer layer erodes into streets and wisps; more cloud along the equator and in the middle latitudes, clear
// skies in the dry belts between and thin toward the poles; a few storms in the middle latitudes that wind the cover
// round them into spirals; and the low sun reddening the tops at the terminator (pn_twilight).
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
uniform float cloud_relief = 0.6;
uniform float relief_max_slope = 2.5;
uniform float backlight_amount = 0.12;
uniform float terminator_relief = 0.8;   // how far a top's slope moves its own terminator
uniform float cloud_opacity = 0.9;
uniform float fringe_opacity = 0.5;
uniform float dome_height = 0.55;
uniform float billow_scale = 6.0;
uniform float billow_strength = 0.12;
uniform float edge_softness = 0.09;
uniform float erosion = 0.52;       // how far the fine layer eats the systems' edges
uniform float belts = 0.045;        // the cover's lean by latitude
uniform float storm_twist = 2.4;    // radians a storm winds its middle
uniform vec3 seed = vec3(0.0);
uniform vec3 sun_dir = vec3(0.4, 0.7, 0.6);   // toward the sun, world space
uniform vec3 sun_color : source_color = vec3(1.0, 0.96, 0.9);
uniform float sun_energy = 1.0;
uniform vec3 atmo : source_color = vec3(0.44, 0.71, 1.0);   // the air's rim over the tops, as on the ground under them
uniform float atmo_strength = 0.0;
uniform float close = 0.0;        // 0 from afar, 1 down in the air (PlanetBody.Sync)

const int STORMS = 6;

varying vec3 mpos;

void vertex() {
	mpos = VERTEX;
#ifdef PLANET_BAKE
	POSITION = pn_bake_position(UV);
#endif
}

// two octaves, about 0 to 1: the broad field that bends the weather
float fbm2(vec3 p) { return pn_vnoise3(p) * 0.66 + pn_vnoise3(p * 2.05 + vec3(17.3, 9.1, 4.7)) * 0.34; }

// the storms: the direction turned about a few centres in the middle latitudes (north one way, south the other), most
// at each centre and dying away from it, so the cover there winds into a spiral
vec3 swirl(vec3 d) {
	for (int i = 0; i < STORMS; i++) {
		vec3 h = pn_hash33(vec3(float(i), 71.0, seed.x));
		float side = (i % 2 == 0) ? 1.0 : -1.0;
		float sl = mix(0.38, 0.78, h.x) * side;
		float lon = h.y * 6.2832;
		float cl = sqrt(1.0 - sl * sl);
		vec3 ctr = vec3(cl * cos(lon), sl, cl * sin(lon));
		float r = mix(0.14, 0.26, h.z);
		float k = exp(-2.0 * (1.0 - dot(d, ctr)) / (r * r));
		if (k > 0.01) { d = pn_rot(d, ctr, k * storm_twist * side); }
	}
	return d;
}

// near (0 to 1) adds the small puffs a camera coming down can see
float cloud_density(vec3 nd, float near) {
	vec3 ax = normalize(wind_axis);
	vec3 q = swirl(pn_rot(nd, ax, PN_TIME * wind_speed));
	vec3 p1 = q * pattern_scale + seed;
	vec3 p2 = pn_rot(q, ax, -PN_TIME * wind_speed * 1.6) * pattern_scale * 3.4 + seed.zxy + 37.0;
	vec3 warp = vec3(fbm2(p1 * 0.5 + 7.7), fbm2(p1 * 0.5 - 3.1), fbm2(p1 * 0.5 + 2.4)) - 0.5;
	float big = pn_fbm3(p1 + warp * 1.6);
	// the fine layer, bent by the same field, eats into the systems' edges
	float fine = pn_fbm3(p2 + warp * 2.6);
	float lat = abs(nd.y);
	float belt = cos(lat * 9.0) * (1.0 - smoothstep(0.62, 0.95, lat)) - 0.6 * smoothstep(0.8, 1.0, lat);
	float dens = big * 0.74 + 0.134 + (fine - 0.515) * erosion + belt * belts;
	if (near > 0.0) { dens += (pn_fbm3(q * pattern_scale * 14.0 + seed.yzx) - 0.515) * 0.14 * near; }
	return dens;
}

float cloud_height(vec3 d, float t_edge, float near) {
	float body = max(cloud_density(d, near) - t_edge, 0.0);
	float dome = sqrt(body) * dome_height;
	float billow = (pn_fbm3(d * pattern_scale * billow_scale + seed) - 0.5) * billow_strength;
	return dome + billow * smoothstep(0.0, 0.06, body);
}

void fragment() {
	vec3 nd = normalize(mpos);
	float footprint = max(length(dFdx(nd)), length(dFdy(nd)));
	float near = 1.0 - smoothstep(0.0006, 0.0025, footprint);
	float n = cloud_density(nd, near);
	float t_edge = 1.0 - coverage * 0.72;
	float t_core = t_edge + edge_softness * 0.5;
	float edge_mask = smoothstep(t_edge, t_edge + edge_softness, n);
	float core_mask = smoothstep(t_core, t_core + edge_softness * 0.6, n);
	if (edge_mask <= 0.001) { discard; }

	vec3 ta, tb;
	pn_frame(nd, ta, tb);
	// (the relief's step finer as the camera comes down, so the small puffs have tops too)
	float eps = max(relief_step * mix(1.0, 0.45, near), footprint * 1.5);
	float ha = cloud_height(normalize(nd + ta * eps), t_edge, near) - cloud_height(normalize(nd - ta * eps), t_edge, near);
	float hb = cloud_height(normalize(nd + tb * eps), t_edge, near) - cloud_height(normalize(nd - tb * eps), t_edge, near);
	// the slope eased toward its cap, never cut at it: a cut leaves every steep top the same flat tone, hard edged
	float ease = 1.0 / (1.0 + length(vec2(ha, hb)) / (2.0 * eps * relief_max_slope));
	vec3 n_model = pn_relief(nd, ta, tb, ha * ease, hb * ease, eps, cloud_relief, relief_max_slope);
	n_model = normalize(mix(nd, n_model, edge_mask));

	vec3 n_world = normalize(MODEL_NORMAL_MATRIX * n_model);
	vec3 g_world = normalize(MODEL_NORMAL_MATRIX * nd);
	vec3 L = pn_sun(normalize(sun_dir), g_world);
	// the sphere's own light, which the relief shades and lights by its turn from it (a cloud is never black by its slope
	// alone: the relief tipping a top away from the sun only shades it)
	float ndl_g = dot(g_world, L);
	// (eased at both ends, never clamped: a clamp leaves flat grey patches with hard edges)
	float turn = dot(n_world, L) - ndl_g;
	// (the day taken on the top's own slope, so along the terminator the sunward side of a cloud is lit and its far side
	// is in night: the relief the low light picks out)
	float day = pn_day(ndl_g + clamp(turn * terminator_relief, -0.06, 0.03)) * (0.5 + 0.75 * smoothstep(-0.7, 0.55, turn));
	// thin tops glow where the sun comes through them, just past the terminator
	float back = backlight_amount * (1.0 - core_mask * 0.6) * smoothstep(-0.05, 0.0, ndl_g) * (1.0 - smoothstep(0.0, 0.1, ndl_g));
	// thin cloud in the base colour, thick in the lit one, the tallest towers a little brighter still
	vec3 col = mix(color_base, color_lit, core_mask) * (0.92 + 0.1 * smoothstep(t_core, t_core + 0.25, n));
	vec3 sun = sun_color * sun_energy;
	ALBEDO = pn_lit(col, day + back, ndl_g, 1.0, sun)
		+ pn_rim(atmo, atmo_strength, dot(normalize(NORMAL), normalize(-VERTEX)), ndl_g, close) * sun_energy;
	ALPHA = mix(edge_mask * fringe_opacity, cloud_opacity, core_mask);
}
