// The transporter's column of light (the battle console mockup's BEAM_SHADER; owner, 2026-11-03: "there should be some
// shader + vfx for transporting stuff"): a cylinder open at both ends, filled with twinkling motes that climb it (hashed
// cells blinking in time, scrolled upward), a faint body, and a bright band sweeping up as it materialises; t is the
// seconds since the beam, k its age as a share of its life. Additive, so it reads as light.
shader_type spatial;
render_mode unshaded, blend_add, depth_draw_never, cull_disabled, shadows_disabled;

uniform float t = 0.0;
uniform float k = 0.0;
uniform vec3 col : source_color = vec3(0.45, 0.75, 1.0);

float h(vec2 p) { return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453); }

void fragment() {
	// the mesh's v runs down from the top; the mockup's up from the plane
	float v = 1.0 - UV.y;
	vec2 g = vec2(UV.x * 48.0, v * 22.0 - t * 6.0), c = floor(g), f = fract(g) - 0.5;
	float r = h(c), tw = step(0.8, r) * (0.5 + 0.5 * sin(t * 18.0 + r * 40.0));
	float spark = tw * smoothstep(0.34, 0.0, length(f));
	float body = smoothstep(0.0, 0.12, v) * (1.0 - v);
	float life = sin(3.14159 * clamp(k, 0.0, 1.0));
	float sw = k * 1.3 - 0.15, sweep = smoothstep(sw - 0.25, sw, v) * (1.0 - smoothstep(sw, sw + 0.12, v));
	ALBEDO = col * (1.0 + spark * 1.5);
	ALPHA = clamp((spark * 2.2 + body * 0.3 + sweep * 0.7) * life, 0.0, 1.0);
}
