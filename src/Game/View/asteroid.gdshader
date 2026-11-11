// asteroid.gdshader: a procedural asteroid (owner, 2026-11-09: "We also need shaders for proc gen asteroids too"; the
// planet-shaders skill). One ball mesh serves every rock: the shape is grown in the vertex stage from the rock's seed, as
// a height over the sphere's direction (planet_noise.gdshaderinc, so no seams): a lumpy body of low noise, a few broad
// facets where it has been split off something bigger (ridged), and craters stamped into it as bowls with rims (the crater
// shell's profile, godot-sandbox's comic_crater_overlay). The vertex normal is that height differenced across the
// sphere; the fragment adds the fine grain and pebbles of godot-sandbox's comic_dirt_ground as relief, and the colour:
// two rock colours by a low noise, paler dust on the high ground, darker crater floors. Lit by the scene's lights like a
// ship (not unshaded), so a rock sits in the same light as the ships beside it.
shader_type spatial;
render_mode cull_back;

#include "res://src/Game/View/planet_noise.gdshaderinc"

uniform vec3 seed = vec3(0.0);
uniform float lumpiness = 0.32;   // the body's swing, a share of the radius
uniform float facets = 0.18;      // broad split faces
uniform float craters = 0.5;      // 0 to 1: how many of the lattice's craters are stamped
uniform float crater_depth = 0.12;
uniform vec3 color_a : source_color = vec3(0.42, 0.39, 0.36);
uniform vec3 color_b : source_color = vec3(0.30, 0.27, 0.25);
uniform vec3 color_dust : source_color = vec3(0.58, 0.54, 0.48);
uniform float grain = 0.6;
uniform float roughness = 0.92;

const int CRATERS = 24;

varying vec3 v_dir;
varying float v_h;
varying float v_crater;

// the crater bowls at a direction: (height, how deep in a floor 0 to 1)
vec2 crater_h(vec3 d) {
	float h = 0.0, floor_k = 0.0;
	for (int i = 0; i < CRATERS; i++) {
		float fi = float(i);
		if (pn_hash31(vec3(fi, seed.x, 3.3)) > craters) { continue; }
		vec3 c = normalize(pn_fib_dir(fi, float(CRATERS)) + (pn_hash33(vec3(fi, seed.y, 7.7)) - 0.5) * 0.7);
		float r = mix(0.14, 0.42, pow(pn_hash31(vec3(fi, seed.z, 1.1)), 2.0));
		float x = length(d - c) / r;
		if (x > 1.3) { continue; }
		float bowl = -(1.0 - smoothstep(0.0, 0.8, x));
		float e = (x - 0.92) * 7.0;
		h += (bowl + exp(-e * e) * 0.35) * crater_depth * r * 2.6;
		floor_k = max(floor_k, 1.0 - smoothstep(0.3, 0.85, x));
	}
	return vec2(h, floor_k);
}

// the rock's radius along d: a lumpy body (low noise at two scales), cut by a few flat planes where it split from
// something bigger (the facets: each plane's distance from the centre, the ball meets it and stops), craters in it
float body_h(vec3 d) {
	float lump = (pn_fbm3(d * 0.9 + seed) - 0.5) * 2.0 * lumpiness + (pn_fbm3(d * 2.4 + seed.zxy) - 0.5) * lumpiness * 0.5;
	float r = 1.0 + lump;
	for (int k = 0; k < 4; k++) {
		vec3 n = normalize(pn_hash33(vec3(float(k), seed.y, 6.6)) - 0.5);
		float off = 1.0 - facets * (0.4 + 0.8 * pn_hash31(vec3(float(k), seed.z, 2.9)));
		float c = dot(d, n);
		if (c > 0.05) { r = min(r, mix(r, off / c, smoothstep(0.05, 0.25, c))); }
	}
	return r - 1.0 + crater_h(d).x;
}

void vertex() {
	vec3 d = normalize(VERTEX);
	vec3 ta, tb;
	pn_frame(d, ta, tb);
	const float e = 0.025;
	float h = body_h(d);
	vec3 p = d * (1.0 + h);
	vec3 pa = normalize(d + ta * e); pa *= 1.0 + body_h(pa);
	vec3 pb = normalize(d + tb * e); pb *= 1.0 + body_h(pb);
	VERTEX = p;
	NORMAL = normalize(cross(pa - p, pb - p));
	if (dot(NORMAL, d) < 0.0) { NORMAL = -NORMAL; }
	v_dir = d;
	v_h = h;
	v_crater = crater_h(d).y;
}

float fine_h(vec3 d) {
	vec3 pp = d * 14.0 + seed;
	vec3 pid = floor(pp), pf = fract(pp);
	float keep = step(0.65, pn_hash31(pid + 91.0));
	float pebble = keep * (1.0 - smoothstep(0.07, 0.18, length(pf - (pn_hash33(pid + 3.0) * 0.5 + 0.25))));
	return pn_vnoise3(d * 40.0 + seed) * 0.6 * grain + pn_vnoise3(d * 110.0 + seed) * 0.25 * grain + pebble * 0.3;
}

void fragment() {
	vec3 d = normalize(v_dir);
	vec3 ta, tb;
	pn_frame(d, ta, tb);
	float footprint = max(length(dFdx(d)), length(dFdy(d)));
	float eps = max(0.004, footprint * 1.5);
	float ha = fine_h(normalize(d + ta * eps)) - fine_h(normalize(d - ta * eps));
	float hb = fine_h(normalize(d + tb * eps)) - fine_h(normalize(d - tb * eps));
	// the fine relief as a turn of the mesh's own normal, toward the grain's slope
	vec3 bump = pn_relief(d, ta, tb, ha, hb, eps, 0.02, 3.0) - d;
	vec3 bump_view = (VIEW_MATRIX * vec4(MODEL_NORMAL_MATRIX * bump, 0.0)).xyz;
	// the true facet from the screen's own slopes, half and half with the smooth one, so a split face reads flat and
	// clean where the vertex normals, taken across a cut, cannot agree
	vec3 ng = normalize(cross(dFdx(VERTEX), dFdy(VERTEX)));
	if (dot(ng, NORMAL) < 0.0) { ng = -ng; }
	NORMAL = normalize(mix(normalize(NORMAL), ng, 0.5) + bump_view);
	float k = pn_fbm3(d * 2.5 + seed.yzx);
	vec3 col = mix(color_b, color_a, smoothstep(0.35, 0.65, k));
	col = mix(col, color_dust, smoothstep(0.02, 0.2, v_h) * 0.55);
	col *= mix(1.0, 0.72, v_crater);
	col *= 0.9 + 0.2 * pn_vnoise3(d * 60.0 + seed);
	ALBEDO = col;
	ROUGHNESS = roughness;
	METALLIC = 0.0;
	SPECULAR = 0.25;
}
