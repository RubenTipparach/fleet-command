// The Concord badge's holographic field (the faction screen, FactionStage): a port of the field shader in
// docs/mockups/lib/emblem-builds.js concord() (holo), the badge pages' one copy. The bell's night: stars on three layers set at
// different depths below the surface and two nebula layers, each offset by the view's slant (parallax), so the sky reads as
// deep space behind glass; a thin-film sheen whose hue runs with the viewing angle; a slow drift in time. Positions are the
// badge's own millimetres (the glb's vertices), as in the page.
shader_type spatial;
render_mode unshaded;

uniform float time = 4.0;

varying vec3 v_world;
varying vec3 v_normal_w;
varying vec2 v_xy;

void vertex() {
	v_world = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
	v_xy = VERTEX.xy;
	v_normal_w = normalize(mat3(MODEL_MATRIX) * NORMAL);
}

float hash(vec2 p) { vec3 p3 = fract(vec3(p.xyx) * 0.1031); p3 += dot(p3, p3.yzx + 33.33); return fract((p3.x + p3.y) * p3.z); }
float noise(vec2 p) { vec2 i = floor(p), f = fract(p); vec2 u = f * f * (3.0 - 2.0 * f);
	return mix(mix(hash(i), hash(i + vec2(1, 0)), u.x), mix(hash(i + vec2(0, 1)), hash(i + vec2(1, 1)), u.x), u.y); }
float fbm(vec2 p) { float v = 0.0, a = 0.5; for (int i = 0; i < 6; i++) { v += a * noise(p); p *= 2.03; a *= 0.5; } return v; }
// one layer of stars: a cell grid, a few cells lit with a soft point, twinkling slowly
float stars(vec2 p, float density, float size) {
	vec2 c = floor(p), f = fract(p); float h = hash(c);
	vec2 at = vec2(hash(c + 17.0), hash(c + 31.0)) * 0.7 + 0.15;
	float lit = step(1.0 - density, h);
	float tw = 0.75 + 0.25 * sin(time * (1.0 + 3.0 * hash(c + 5.0)) + h * 40.0);
	return lit * tw * smoothstep(size, 0.0, length(f - at));
}

void fragment() {
	vec3 V = normalize(CAMERA_POSITION_WORLD - v_world);
	vec3 N = normalize(v_normal_w);
	// the view's slant across the face (the badge's own axes), for parallax: a layer at depth d mm is seen offset by d * slant
	vec3 Vl = normalize((inverse(MODEL_MATRIX) * vec4(V, 0.0)).xyz);
	vec2 slant = Vl.xy / max(Vl.z, 0.3);
	vec2 p = v_xy;
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
	// thin-film sheen: a hue that runs round with the viewing angle and across the face
	float fres = pow(1.0 - max(dot(N, V), 0.0), 2.0);
	vec3 film = 0.5 + 0.5 * cos(6.2832 * (vec3(0.0, 0.33, 0.67) + fres * 1.6 + (p.x - p.y) * 0.006));
	col += film * (0.03 + 0.25 * fres);
	// linear, as in the page (three converts it to sRGB on output, as Godot does)
	ALBEDO = col;
}
