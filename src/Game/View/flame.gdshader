// flame.gdshader: one puff of fire, for the battle's particle flames (owner, 2026-11-04: "make flame look better"): a
// camera-facing tongue of flame, its outline torn by noise that rolls upward through it, its heat white-yellow at the
// heart cooling to orange and deep red at the edges and as the puff ages, flickering. Drawn by the CPU particles of Fx
// (wreck trails, debris trails, blasts): each particle's age is INSTANCE_CUSTOM.y (0 to 1), its rotation INSTANCE_CUSTOM.x,
// and its COLOR's alpha scales it. Added to the scene.
shader_type spatial;
render_mode unshaded, blend_add, depth_draw_never, cull_disabled, shadows_disabled, fog_disabled;

// 1 a flame; lower for a cooler, redder fire
uniform float heat = 1.0;

varying float v_age;
varying float v_alpha;
varying float v_seed;

float h2(vec2 p) { return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453); }
float n2(vec2 x) { vec2 i = floor(x), f = fract(x); f = f * f * (3.0 - 2.0 * f);
	return mix(mix(h2(i), h2(i + vec2(1, 0)), f.x), mix(h2(i + vec2(0, 1)), h2(i + vec2(1, 1)), f.x), f.y); }
float fbm(vec2 p) { float a = 0.5, s = 0.0; for (int i = 0; i < 4; i++) { s += a * n2(p); p = p * 2.07 + 5.3; a *= 0.5; } return s; }

void vertex() {
	// face the camera, turned by the particle's own angle, at the particle's own scale (Godot's particle billboard)
	mat4 w = mat4(normalize(INV_VIEW_MATRIX[0]), normalize(INV_VIEW_MATRIX[1]), normalize(INV_VIEW_MATRIX[2]), MODEL_MATRIX[3]);
	float a = INSTANCE_CUSTOM.x;
	w = w * mat4(vec4(cos(a), -sin(a), 0.0, 0.0), vec4(sin(a), cos(a), 0.0, 0.0), vec4(0.0, 0.0, 1.0, 0.0), vec4(0.0, 0.0, 0.0, 1.0));
	MODELVIEW_MATRIX = VIEW_MATRIX * w;
	MODELVIEW_MATRIX = MODELVIEW_MATRIX * mat4(vec4(length(MODEL_MATRIX[0].xyz), 0.0, 0.0, 0.0), vec4(0.0, length(MODEL_MATRIX[1].xyz), 0.0, 0.0),
		vec4(0.0, 0.0, length(MODEL_MATRIX[2].xyz), 0.0), vec4(0.0, 0.0, 0.0, 1.0));
	v_age = INSTANCE_CUSTOM.y;
	v_alpha = COLOR.a;
	v_seed = float(INSTANCE_ID) * 1.618;
}

void fragment() {
	vec2 p = UV * 2.0 - 1.0;
	float t = v_age;
	// noise rolling up through the puff tears its outline; a little lift makes it a tongue, not a disc
	float n = fbm(p * 2.2 + vec2(v_seed, v_seed * 0.7) + vec2(0.0, TIME * 2.4 + t * 3.0));
	float d = length(p * vec2(1.0, 0.85 + 0.25 * p.y)) + (n - 0.5) * (0.65 + 0.5 * t);
	float body = 1.0 - smoothstep(0.35 + 0.25 * (1.0 - t), 0.95, d);
	float temp = (1.0 - d) * 1.3 * (1.0 - t * 0.85) * heat + (n - 0.5) * 0.3;
	vec3 c = mix(vec3(0.35, 0.03, 0.0), vec3(0.95, 0.22, 0.03), smoothstep(0.0, 0.35, temp));
	c = mix(c, vec3(1.0, 0.55, 0.12), smoothstep(0.3, 0.6, temp));
	c = mix(c, vec3(1.0, 0.88, 0.55), smoothstep(0.55, 0.85, temp));
	c = mix(c, vec3(1.3, 1.25, 1.1), smoothstep(0.85, 1.1, temp));
	float flicker = 0.85 + 0.15 * sin(TIME * 23.0 + v_seed * 7.0);
	ALBEDO = c * flicker;
	ALPHA = clamp(body * (1.0 - t * t) * v_alpha, 0.0, 1.0);
}
