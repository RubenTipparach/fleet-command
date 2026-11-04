// fireball.gdshader: a ball of fire for the battle's blasts (owner, 2026-11-04: "aww theres no big super nova", "we had a
// big explosion earlier, but it needs better shaders and particles"). A sphere whose surface billows out on 3D noise and
// rolls as it grows; its colour is a temperature: white-hot where it faces the camera and early on, cooling through
// yellow and orange to a dull red at its limb and as it ages, its edge eaten away by the noise until it is gone. Added to
// the scene (it glows; the environment's bloom picks up the hottest of it). progress runs 0 to 1 over its life.
shader_type spatial;
render_mode unshaded, blend_add, depth_draw_never, cull_back, shadows_disabled, fog_disabled;

uniform float progress = 0.0;
uniform float seed = 0.0;
uniform float intensity = 1.0;
// how far the surface billows, as a share of the radius
uniform float billow = 0.35;

varying vec3 v_dir;

float h3(vec3 p) { p = fract(p * 0.3183099 + 0.1); p *= 17.0; return fract(p.x * p.y * p.z * (p.x + p.y + p.z)); }
float n3(vec3 x) {
	vec3 i = floor(x), f = fract(x); f = f * f * (3.0 - 2.0 * f);
	return mix(mix(mix(h3(i), h3(i + vec3(1, 0, 0)), f.x), mix(h3(i + vec3(0, 1, 0)), h3(i + vec3(1, 1, 0)), f.x), f.y),
		mix(mix(h3(i + vec3(0, 0, 1)), h3(i + vec3(1, 0, 1)), f.x), mix(h3(i + vec3(0, 1, 1)), h3(i + vec3(1, 1, 1)), f.x), f.y), f.z);
}
float fbm(vec3 p) { float a = 0.5, s = 0.0; for (int i = 0; i < 5; i++) { s += a * n3(p); p = p * 2.03 + 11.7; a *= 0.5; } return s; }

// temperature (0 cold to 1.2 white-hot) to colour, brighter than 1 at the top so the bloom takes it
vec3 heat_col(float t) {
	vec3 c = mix(vec3(0.08, 0.01, 0.0), vec3(0.75, 0.12, 0.02), smoothstep(0.0, 0.3, t));
	c = mix(c, vec3(1.0, 0.42, 0.06), smoothstep(0.25, 0.55, t));
	c = mix(c, vec3(1.0, 0.78, 0.3), smoothstep(0.5, 0.8, t));
	c = mix(c, vec3(1.6, 1.5, 1.3), smoothstep(0.8, 1.15, t));
	return c;
}

void vertex() {
	v_dir = normalize(VERTEX);
	vec3 q = v_dir * 2.2 + vec3(seed, seed * 0.7, -seed) + vec3(0.0, -progress * 1.5, progress * 0.8);
	float b = fbm(q);
	VERTEX += v_dir * (b - 0.45) * billow * 2.0;
}

void fragment() {
	float t = progress;
	vec3 q = v_dir * 3.0 + vec3(seed) + vec3(0.0, -t * 2.2, t * 1.2);
	float f = fbm(q), g = fbm(v_dir * 7.5 - vec3(seed) + t * 3.0);
	float facing = clamp(dot(normalize(NORMAL), normalize(VIEW)), 0.0, 1.0);
	// hottest early and face-on; the billows' tops stay hotter than their valleys
	float temp = 1.25 - t * 1.25 + facing * 0.45 + (f - 0.5) * 0.9 + (g - 0.5) * 0.35 - (1.0 - facing) * 0.35;
	// the edge burns away: what survives is where the noise beats a rising threshold, softest at the limb
	float alive = smoothstep(t * 0.95 - 0.15, t * 0.95 + 0.12, f * 0.9 + facing * 0.35);
	ALBEDO = heat_col(temp) * intensity;
	ALPHA = clamp(alive * (0.35 + 0.65 * facing) * (1.0 - t * t), 0.0, 1.0);
}
