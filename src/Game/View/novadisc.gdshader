// novadisc.gdshader: a ship's end spreading through the battle plane (owner, 2026-11-04: "a blue ring glowing outward").
// A flat disc of blue light whose front races out to the plane's edge: brightest at the rim, a deep glowing blue inside,
// streaked from the centre and churned by noise, white where the core still burns, thinning and fading as it spreads.
// Added to the scene; progress runs 0 to 1 over its life.
shader_type spatial;
render_mode unshaded, blend_add, depth_draw_never, cull_disabled, shadows_disabled, fog_disabled;

uniform float progress = 0.0;
uniform float seed = 0.0;
uniform vec3 deep_col : source_color = vec3(0.08, 0.22, 1.0);
uniform vec3 rim_col : source_color = vec3(0.45, 0.75, 1.0);

float h2(vec2 p) { return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453); }
float n2(vec2 x) { vec2 i = floor(x), f = fract(x); f = f * f * (3.0 - 2.0 * f);
	return mix(mix(h2(i), h2(i + vec2(1, 0)), f.x), mix(h2(i + vec2(0, 1)), h2(i + vec2(1, 1)), f.x), f.y); }

void fragment() {
	vec2 p = UV * 2.0 - 1.0;
	float t = progress;
	float front = sqrt(t) * 0.97 + 0.02;                 // fast at first, easing out
	float r = length(p) / front, a = atan(p.y, p.x);
	if (r > 1.06) discard;
	float rag = n2(vec2(a * 7.0 + seed, t * 3.0)) * 0.6 + n2(vec2(a * 19.0 - seed, 2.0)) * 0.4;
	float edge = 1.0 + (rag - 0.5) * 0.04;
	float inside = 1.0 - smoothstep(edge - 0.01, edge + 0.02, r);
	float rim = smoothstep(edge - 0.22, edge, r) * inside;
	float streak = 0.55 + 0.45 * n2(vec2(a * 40.0 + seed, r * 1.5 - t * 4.0));
	float churn = n2(p / front * 4.0 + seed + t * 2.0);
	float glow = (0.14 + 0.4 * churn) * streak * inside + rim * 1.7;
	float core = exp(-r * r * 18.0) * (1.0 - smoothstep(0.0, 0.5, t)) * 2.0;
	vec3 col = mix(deep_col, rim_col, clamp(rim + 0.3 * churn, 0.0, 1.0)) * glow * 1.4 + vec3(1.0, 0.85, 1.0) * core;
	float fade = (1.0 - t) * (1.0 - t * 0.5);
	ALBEDO = col;
	ALPHA = clamp((glow + core) * fade, 0.0, 1.0);
}
