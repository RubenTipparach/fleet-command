// flare.gdshader: a camera-facing glow at a point, the game's port of the weapons range's flares
// (docs/mockups/lib/weapon-fx.js, flare(); change weapon-fx). Mode 0 soft (a plain glow), 1 six (the pulse torpedo's
// six-armed star: three thin beaded spikes at 60 degrees that turn slowly and flicker in length, a warm and a cyan fringe,
// a faint strut and a streaked hot core; owner, 2026-11-04: "pulse torpedo needs that iconic 6 armed flare", "the flare
// arms should be moving around"), 2 ball (a missile's plume: a big boiling ball round a hot heart; "a big ball plume").
// The quad is a 1 x 1 QuadMesh scaled by size (scene units, its full width). Never white: the core is the colour at full
// strength (owner: "please avoid the color white for weapons").
shader_type spatial;
render_mode unshaded, blend_add, depth_draw_never, depth_test_disabled, cull_disabled, shadows_disabled, fog_disabled;

uniform vec4 color : source_color = vec4(0.56, 0.17, 1.0, 1.0);
uniform float intensity = 1.0;
uniform float size = 1.0;
uniform int mode = 0;
uniform float time_s = 0.0;
uniform float seed = 0.0;

varying vec2 v_p;

float h3(vec3 p) { p = fract(p * 0.3183099 + 0.1); p *= 17.0; return fract(p.x * p.y * p.z * (p.x + p.y + p.z)); }
float n3(vec3 x) {
	vec3 i = floor(x), f = fract(x); f = f * f * (3.0 - 2.0 * f);
	return mix(mix(mix(h3(i), h3(i + vec3(1, 0, 0)), f.x), mix(h3(i + vec3(0, 1, 0)), h3(i + vec3(1, 1, 0)), f.x), f.y),
		mix(mix(h3(i + vec3(0, 0, 1)), h3(i + vec3(1, 0, 1)), f.x), mix(h3(i + vec3(0, 1, 1)), h3(i + vec3(1, 1, 1)), f.x), f.y), f.z);
}
vec3 hot_of(vec3 c) { return c / max(max(c.r, c.g), max(c.b, 0.001)); }

void vertex() {
	v_p = VERTEX.xy * 2.0;
	MODELVIEW_MATRIX = VIEW_MATRIX * mat4(INV_VIEW_MATRIX[0], INV_VIEW_MATRIX[1], INV_VIEW_MATRIX[2], MODEL_MATRIX[3]);
	VERTEX *= size;
}

void fragment() {
	vec2 p = v_p;
	float r = length(p), t = time_s + seed;
	vec3 c = vec3(0.0);
	if (mode == 1) {
		float core = exp(-r * r * 70.0) * 3.2 + exp(-r * 10.0) * 0.9, spikes = 0.0;
		vec3 fringe = vec3(0.0);
		float spin = t * 0.9 + 0.15 * sin(t * 2.3);
		for (int i = 0; i < 3; i++) {
			float a = 1.5707963 + float(i) * 1.0471976 + spin;
			vec2 d = vec2(cos(a), sin(a));
			float along = abs(dot(p, d)), perp = abs(p.x * d.y - p.y * d.x);
			float beads = 0.7 + 0.3 * cos(along * 110.0 - t * 9.0);
			float reach = 2.0 / (0.75 + 0.35 * sin(t * 7.0 + float(i) * 2.1) + 0.15 * sin(t * 17.0 + float(i)));
			spikes += exp(-perp * perp * 12000.0 - perp * 70.0) * exp(-along * reach) * beads;
			fringe += vec3(1.0, 0.62, 0.32) * exp(-perp * 30.0) * exp(-along * 3.4) * 0.22 + vec3(0.3, 0.85, 0.8) * exp(-pow((perp - 0.012) * 260.0, 2.0)) * exp(-along * 2.6) * 0.25;
		}
		vec2 ps = vec2(cos(spin) * p.x + sin(spin) * p.y, -sin(spin) * p.x + cos(spin) * p.y);
		float strut = exp(-abs(ps.y) * 420.0) * exp(-abs(ps.x) * 5.0) * 0.45;
		float fine = pow(n3(vec3(atan(p.y, p.x + 1e-5) * 36.0, 1.0, t * 1.5)), 5.0) * exp(-r * 6.5) * 2.2;
		c = color.rgb * (spikes * 2.4 + strut + exp(-r * 4.0) * 0.6) + fringe * 0.6 + hot_of(color.rgb) * (core + fine);
	} else if (mode == 2) {
		float body = exp(-r * r * 7.0), heart = exp(-r * r * 55.0), rim = exp(-pow((r - 0.42) / 0.12, 2.0)) * 0.25;
		float boil = 0.85 + 0.3 * n3(vec3(p * 5.0, t * 9.0));
		c = color.rgb * (body * 1.6 * boil + rim + exp(-r * 3.0) * 0.35) + hot_of(color.rgb) * heart * 2.0;
	} else {
		c = color.rgb * (exp(-r * r * 12.0) * 1.5 + exp(-r * 4.0) * 0.4);
	}
	ALBEDO = c * intensity * (1.0 - smoothstep(0.8, 1.0, r));
}
