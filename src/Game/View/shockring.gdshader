// shockring.gdshader: a blast's shock front, a flat ring racing out (owner, 2026-11-04: "it needs better shaders and
// particles"). A plane whose ring of light sits at radius progress (0 to 1 of the plane's half-size), a sharp leading
// edge and a soft glowing wake behind it, ragged by noise round the ring, hot white-orange early and bluer as it thins.
// Added to the scene; progress runs 0 to 1 over its life.
shader_type spatial;
render_mode unshaded, blend_add, depth_draw_never, cull_disabled, shadows_disabled, fog_disabled;

uniform float progress = 0.0;
uniform float seed = 0.0;
uniform float intensity = 1.0;

float h2(vec2 p) { return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453); }
float n2(vec2 x) { vec2 i = floor(x), f = fract(x); f = f * f * (3.0 - 2.0 * f);
	return mix(mix(h2(i), h2(i + vec2(1, 0)), f.x), mix(h2(i + vec2(0, 1)), h2(i + vec2(1, 1)), f.x), f.y); }

void fragment() {
	vec2 p = UV * 2.0 - 1.0;
	float r = length(p), a = atan(p.y, p.x);
	float e = progress;
	float rag = n2(vec2(a * 9.0 + seed, e * 6.0)) * 0.6 + n2(vec2(a * 23.0 - seed, e * 11.0)) * 0.4;
	float front = e + (rag - 0.5) * 0.03;
	float lead = 1.0 - smoothstep(front, front + 0.012, r);                  // sharp outer edge
	float wake = smoothstep(front - (0.08 + 0.25 * e), front, r);            // soft glow behind it
	float band = lead * wake;
	vec3 hot = mix(vec3(1.6, 1.2, 0.8), vec3(0.6, 0.75, 1.4), smoothstep(0.2, 0.9, e));
	ALBEDO = hot * intensity * (0.7 + 0.6 * rag);
	ALPHA = clamp(band * (1.0 - e) * (1.0 - e) * (0.55 + 0.6 * rag), 0.0, 1.0);
}
