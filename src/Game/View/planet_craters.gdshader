// planet_craters.gdshader: a cratered world's craters, a shell a hair above its ground (the planet-shaders skill). Ported
// from godot-sandbox (RubenTipparach/godot-sandbox at d5d8e461, resources/shaders/comic_crater_overlay.gdshader): the
// craters are points on a Fibonacci lattice, jittered, in three sizes stamped big to small (young over old), each a
// round bowl with a raised rim as a height profile whose exact slope bends the normal; between craters the shell is
// clear and the ground shows. Changes for Fleet Command: unshaded and lit here by the battle's sun (sun_dir), as the
// ground is; the floor and rim colours come from the kind's palette; each planet's seed lays its own craters; PCG noise.
shader_type spatial;
render_mode unshaded, shadows_disabled, depth_draw_never, blend_mix, cull_back;

#include "res://src/Game/View/planet_noise.gdshaderinc"

uniform vec3 color_floor : source_color = vec3(0.35, 0.33, 0.31);
uniform vec3 color_shadow : source_color = vec3(0.16, 0.15, 0.15);
uniform vec3 color_rim : source_color = vec3(0.78, 0.75, 0.70);
uniform float coverage = 0.45;
uniform float dir_jitter = 0.45;
uniform vec2 big_size = vec2(0.14, 0.24);
uniform vec2 med_size = vec2(0.07, 0.11);
uniform vec2 small_size = vec2(0.03, 0.055);
uniform float crater_depth = 0.09;
uniform float crater_rim_height = 0.045;
uniform float crater_relief = 1.0;
uniform float relief_max_slope = 4.0;
uniform float seed = 0.0;
uniform vec3 sun_dir = vec3(0.4, 0.7, 0.6);
uniform vec3 sun_color : source_color = vec3(1.0, 0.96, 0.9);
uniform float sun_energy = 1.0;

const int BIG_COUNT = 14;
const int MED_COUNT = 48;
const int SMALL_COUNT = 140;

varying vec3 mpos;

void vertex() {
	mpos = VERTEX;
}

float crater_dh(float x) {
	float d_bowl = 0.0;
	if (x < 0.80) {
		float u = x / 0.80;
		d_bowl = 6.0 * u * (1.0 - u) / 0.80;
	}
	float e = (x - 0.92) * 7.0;
	return d_bowl * crater_depth + (-14.0 * e * exp(-e * e)) * crater_rim_height;
}

vec4 crater_layer(vec3 nd, int count, vec2 size_range, float layer, out vec3 grad_out) {
	float fcount = float(count);
	float best_d = 9.0, best_r = 1.0, best_id = -1.0;
	vec3 best_c = vec3(0.0, 1.0, 0.0);
	grad_out = vec3(0.0);
	for (int i = 0; i < count; i++) {
		float fi = float(i);
		if (pn_hash31(vec3(fi, layer + seed, 1.7)) > coverage) { continue; }
		vec3 c = normalize(pn_fib_dir(fi, fcount) + (pn_hash33(vec3(fi, layer + seed, 9.2)) - 0.5) * dir_jitter);
		float radius = mix(size_range.x, size_range.y, pn_hash31(vec3(fi, layer + seed, 4.4)));
		float d = length(nd - c) / radius;
		if (d < best_d) { best_d = d; best_c = c; best_id = fi; best_r = radius; }
	}
	float d = best_d;
	float aa = fwidth(d) * 1.5;
	float inside = 1.0 - smoothstep(1.0 - aa, 1.0 + aa, d);
	if (best_id < 0.0 || inside <= 0.001) { return vec4(0.0); }
	vec3 away = nd - best_c;
	float al = length(away);
	if (al > 1e-6) {
		vec3 dirn = away / al;
		vec3 t = dirn - nd * dot(dirn, nd);
		grad_out = t * (crater_dh(al / best_r) / best_r);
	}
	vec3 col = mix(color_floor, color_rim, smoothstep(0.62, 0.98, d));
	col = mix(col, color_shadow, (1.0 - smoothstep(0.0, 0.45, d)) * 0.35);
	return vec4(col, inside);
}

void fragment() {
	vec3 nd = normalize(mpos);
	vec3 g0, g1, g2;
	vec4 l0 = crater_layer(nd, BIG_COUNT, big_size, 3.0, g0);
	vec4 l1 = crater_layer(nd, MED_COUNT, med_size, 57.0, g1);
	vec4 l2 = crater_layer(nd, SMALL_COUNT, small_size, 131.0, g2);
	float alpha = max(l0.a, max(l1.a, l2.a));
	if (alpha <= 0.001) { discard; }
	vec3 col = vec3(0.0);
	col = mix(col, l0.rgb, l0.a);
	col = mix(col, l1.rgb, l1.a);
	col = mix(col, l2.rgb, l2.a);
	vec3 grad = g0 * l0.a + g1 * l1.a + g2 * l2.a;
	float g = length(grad);
	if (g > relief_max_slope) { grad *= relief_max_slope / g; }
	vec3 n_model = normalize(nd - crater_relief * grad);
	vec3 L = normalize(sun_dir);
	float ndl = dot(normalize(MODEL_NORMAL_MATRIX * n_model), L);
	float ndl_g = dot(normalize(MODEL_NORMAL_MATRIX * nd), L);
	// a bowl's wall is lit or shaded by its slope, but past the terminator the whole crater is night with its ground
	float day = pn_day(ndl) * smoothstep(-0.2, 0.05, ndl_g);
	ALBEDO = col * (0.03 + 0.97 * day) * sun_color * sun_energy;
	ALPHA = alpha;
}
