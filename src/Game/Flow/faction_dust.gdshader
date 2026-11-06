// Dust in a lamp's beam on the faction screen (FactionStage): motes drifting on slow currents, each glinting as it turns (a
// sharp flicker on its own beat), lit only inside the cone and only while the lamp is on. A port of the page's dustMat and
// its drift (docs/mockups/game-flow.html stepFaction); the mote's seed is its UV.x.
shader_type spatial;
render_mode unshaded, blend_add, depth_draw_never, cull_disabled, shadows_disabled;

uniform float level = 0.0;
uniform float t = 0.0;
uniform vec3 tip;
uniform vec3 axis = vec3(0.0, -1.0, 0.0);
uniform float cos_cone = 0.95;
uniform float px = 1.0;

varying float v_lit;

void vertex() {
	float seed = UV.x, q = seed * 100.0;
	VERTEX += vec3(sin(t * 0.21 + q) * 0.12, sin(t * 0.13 + q * 1.7) * 0.18, cos(t * 0.17 + q) * 0.12);
	vec3 w = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
	float c = dot(normalize(w - tip), axis);
	float in_cone = smoothstep(cos_cone, cos_cone + 0.012, c);
	float glint = pow(0.5 + 0.5 * sin(t * (1.5 + 3.0 * fract(seed * 7.3)) + seed * 40.0), 10.0);
	v_lit = in_cone * (0.25 + 1.6 * glint);
	vec4 mv = MODELVIEW_MATRIX * vec4(VERTEX, 1.0);
	POINT_SIZE = px * (1.6 + 3.2 * glint) * (8.0 / max(0.1, -mv.z));
}

void fragment() {
	vec2 q = POINT_COORD * 2.0 - 1.0;
	float a = exp(-dot(q, q) * 3.0);
	ALBEDO = vec3(1.0, 0.86, 0.66) * a * v_lit * level * 1.2;
}
