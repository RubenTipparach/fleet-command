// planet_craters.gdshader: a cratered world's craters, a shell a hair above its ground (the planet-shaders skill). Ported
// from godot-sandbox (RubenTipparach/godot-sandbox at d5d8e461, resources/shaders/comic_crater_overlay.gdshader): the
// craters are points on a Fibonacci lattice, jittered, in three sizes stamped big to small (young over old), each a
// height profile whose exact slope bends the normal; between craters the shell is clear and the ground shows. Changes for
// Fleet Command: drawn as a multiple of the ground under it (blend_mul), never a colour of its own, so the ground's own
// texture runs through every crater and only the light on the slopes (from the battle's sun, sun_dir) and a slight tint
// change; each planet's seed lays its own craters; PCG noise.
// Shaped like a real impact crater, not a bowl (owner, 2026-11-09: "Craters didn't look natural close up"): a flat floor
// (wider in the big ones) with a central peak in the biggest, a wall steepest just under the rim, a sharp rim crest, and
// outside it the ejecta blanket falling away and fading into the ground; the rim wanders off the circle by a low noise;
// the floor takes the ground's own colours and a fine rubble relief, finer as the camera comes down; the walls are shaded
// by their turn from the ground's own light, never black on a lit face. A crater is shallower the bigger it is (owner,
// 2026-11-10: "make planets more realistic please"): a real basin's floor is wide and lies a small share of its radius
// down, so its far wall is a band of shade and never a dark disc; only the small bowls are deep for their size.
shader_type spatial;
render_mode unshaded, shadows_disabled, depth_draw_never, blend_mul, cull_back, fog_disabled;

#include "res://src/Game/View/planet_noise.gdshaderinc"

uniform float floor_tint = 0.9;   // the floor's and the crest's colour, as multiples of the ground's own
uniform float rim_tint = 1.12;
uniform float coverage = 0.45;
uniform float dir_jitter = 0.45;
uniform vec2 big_size = vec2(0.14, 0.24);
uniform vec2 med_size = vec2(0.07, 0.11);
uniform vec2 small_size = vec2(0.03, 0.055);
uniform float crater_depth = 0.22;       // the floor under the plain, in the crater's radii
uniform float crater_rim_height = 0.085;  // the rim crest over the plain, in its radii
uniform float rim_wander = 0.08;         // how far the rim strays off the circle, a share of the radius
uniform float ejecta_reach = 1.7;        // where the blanket outside the rim has faded out, in radii
uniform float crater_relief = 1.0;
uniform float relief_max_slope = 2.5;
uniform float rubble = 0.3;              // the fine relief on floors and walls
uniform float seed = 0.0;
uniform float air = 0.0;          // 1 where the world has air (a soft terminator), 0 where it has none (PlanetBody)
uniform vec3 sun_dir = vec3(0.4, 0.7, 0.6);
uniform float sun_energy = 1.0;   // set with the other layers'; the multiple needs only the sun's direction

const int BIG_COUNT = 14;
const int MED_COUNT = 48;
const int SMALL_COUNT = 140;

varying vec3 mpos;

void vertex() {
	mpos = VERTEX;
#ifdef PLANET_BAKE
	POSITION = pn_bake_position(UV);
#endif
}

// the profile's slope at x radii out (height in radii): a flat floor to floor_x with a peak of peak_h at the centre, a
// wall rising as the square of the way across it (gentle at the foot, steepest under the crest), and outside the crest
// the blanket falling off; deep scales the whole profile (1 a small bowl, less for a basin)
float crater_dhdx(float x, float floor_x, float peak_h, float deep) {
	float total = (crater_depth + crater_rim_height) * deep;
	if (x < floor_x) {
		const float pw = 0.16;
		return peak_h * exp(-(x * x) / (pw * pw)) * (-2.0 * x / (pw * pw));
	}
	if (x < 1.0) {
		float t = (x - floor_x) / (1.0 - floor_x);
		return total * 2.0 * t / (1.0 - floor_x);
	}
	const float k = 4.5;
	return -crater_rim_height * deep * k * exp(-(x - 1.0) * k);
}

// (tint, 0, 0, cover) of the nearest crater of a layer, its slope (model space, along the sphere) in grad_out, and in
// floor_out how far inside its wall the point is (1 on the floor, 0 at the crest and outside)
vec4 crater_layer(vec3 nd, int count, vec2 size_range, float layer, float floor_x, float peak_h, float deep, out vec3 grad_out, out float floor_out) {
	float fcount = float(count);
	float best_d = 1e9, best_r = 1.0, best_id = -1.0;
	vec3 best_c = vec3(0.0, 1.0, 0.0);
	grad_out = vec3(0.0);
	floor_out = 0.0;
	for (int i = 0; i < count; i++) {
		float fi = float(i);
		if (pn_hash31(vec3(fi, layer + seed, 1.7)) > coverage) { continue; }
		vec3 c = normalize(pn_fib_dir(fi, fcount) + (pn_hash33(vec3(fi, layer + seed, 9.2)) - 0.5) * dir_jitter);
		float radius = mix(size_range.x, size_range.y, pn_hash31(vec3(fi, layer + seed, 4.4)));
		float d = length(nd - c) / radius;
		if (d < best_d) { best_d = d; best_c = c; best_id = fi; best_r = radius; }
	}
	if (best_id < 0.0 || best_d > ejecta_reach * 1.15) { return vec4(0.0); }
	// the rim wanders: the distance stretched by a low noise round the crater, its own for every crater
	float wander = (pn_fbm3(nd * (2.2 / best_r) + vec3(best_id, layer, seed)) - 0.5) * 2.0 * rim_wander;
	float x = best_d * (1.0 + wander);
	float aa = fwidth(x) * 1.5;
	float cover = 1.0 - smoothstep(1.05, ejecta_reach, x);
	if (cover <= 0.001) { return vec4(0.0); }
	vec3 away = nd - best_c;
	float al = length(away);
	if (al > 1e-6) {
		vec3 dirn = away / al;
		vec3 t = dirn - nd * dot(dirn, nd);
		grad_out = t * crater_dhdx(x, floor_x, peak_h, deep);
	}
	float inside = 1.0 - smoothstep(1.0 - aa, 1.0 + aa, x);
	floor_out = 1.0 - smoothstep(floor_x, 1.0, x);
	// the floor a shade under the plain, the wall paler as it climbs to the crest (fresh rock), the blanket a little
	// paler still and thinning out; varied by a broad noise so no two read the same. A multiple of the ground's colour.
	float mottle = pn_fbm3(nd * (9.0 / best_r) + best_id);
	float tint = mix(floor_tint, rim_tint, smoothstep(floor_x, 1.0, x));
	tint = mix(tint, rim_tint, 1.0 - inside);
	tint *= 0.93 + 0.14 * mottle;
	// the blanket fades from the crest to nothing at its reach, so it melts into the plain
	return vec4(tint, 0.0, 0.0, mix(1.0, cover, 1.0 - inside));
}

// the rubble on floors and walls: pebbles and grit as a height (in planet radii, each octave's swing its own size, so
// every octave's slopes are alike), down to the scale the camera comes to
float rubble_h(vec3 d) {
	return pn_vnoise3(d * 420.0 + seed) / 420.0 + pn_vnoise3(d * 1200.0 + seed) / 1200.0 * 0.8 + pn_vnoise3(d * 3400.0 + seed) / 3400.0 * 0.6;
}

void fragment() {
	vec3 nd = normalize(mpos);
	vec3 g0, g1, g2;
	float f0, f1, f2;
	vec4 l0 = crater_layer(nd, BIG_COUNT, big_size, 3.0, 0.56, 0.05, 0.36, g0, f0);
	vec4 l1 = crater_layer(nd, MED_COUNT, med_size, 57.0, 0.34, 0.0, 0.62, g1, f1);
	vec4 l2 = crater_layer(nd, SMALL_COUNT, small_size, 131.0, 0.12, 0.0, 1.0, g2, f2);
	float cover = max(l0.a, max(l1.a, l2.a));
	if (cover <= 0.001) { discard; }
	float tint = mix(1.0, l0.r, l0.a);
	tint = mix(tint, l1.r, l1.a);
	tint = mix(tint, l2.r, l2.a);
	vec3 grad = g0 * l0.a + g1 * l1.a + g2 * l2.a;
	float g = length(grad);
	if (g > relief_max_slope) { grad *= relief_max_slope / g; }
	vec3 n_model = normalize(nd - crater_relief * grad);

	// the rubble, as a relief on that normal, its step widened to the fragment's footprint (no shimmer far off)
	vec3 ta, tb;
	pn_frame(nd, ta, tb);
	float footprint = max(length(dFdx(nd)), length(dFdy(nd)));
	float eps = max(0.0008, footprint * 1.5);
	float ha = rubble_h(normalize(nd + ta * eps)) - rubble_h(normalize(nd - ta * eps));
	float hb = rubble_h(normalize(nd + tb * eps)) - rubble_h(normalize(nd - tb * eps));
	float rough = rubble * (0.4 + 0.6 * max(max(f0 * l0.a, f1 * l1.a), f2 * l2.a)) * cover;
	vec2 gr = vec2(ha, hb) / (2.0 * eps);
	float gl = length(gr);
	if (gl > 1.0) { gr /= gl; }
	n_model = normalize(n_model - rough * 0.6 * (gr.x * ta + gr.y * tb));

	// the ground's light turned by the slope, as a multiple of what the plain already has: the same day term the ground
	// uses (pn_day_air), taken on the slope's normal over the plain's, so a wall facing the sun is brighter, one turned
	// from it in shade, a flat floor exactly as lit as the plain round it; past the terminator the plain is night and the
	// multiple does nothing to it
	vec3 L = pn_sun(normalize(sun_dir), normalize(MODEL_NORMAL_MATRIX * nd));
	float ndl = dot(normalize(MODEL_NORMAL_MATRIX * n_model), L);
	float ndl_g = dot(normalize(MODEL_NORMAL_MATRIX * nd), L);
	float turn = clamp(pn_day_air(ndl, air) / max(pn_day_air(ndl_g, air), 0.03), 0.06, 2.4);
	ALBEDO = vec3(mix(1.0, turn * tint, cover));
}
