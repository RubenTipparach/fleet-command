// novastar.gdshader: the heart of a ship's end, a star of light facing the camera (owner, 2026-11-04: "center is white
// with pink edges, and a blue ring glowing outward. this explosion was so iconic to me as a child"). A white-hot core
// whose light falls off into a pink halo, with a few soft spikes; it flares in, then the white burns down and the pink
// lingers. Added to the scene (the bloom picks up the core). progress runs 0 to 1 over its life.
shader_type spatial;
render_mode unshaded, blend_add, depth_draw_never, depth_test_disabled, cull_disabled, shadows_disabled, fog_disabled;

uniform float progress = 0.0;
uniform float spin = 0.0;
uniform vec3 core_col : source_color = vec3(1.0, 1.0, 1.0);
uniform vec3 edge_col : source_color = vec3(1.0, 0.36, 0.78);

void vertex() {
	// face the camera at the node's own size
	float s = length(MODEL_MATRIX[0].xyz);
	MODELVIEW_MATRIX = VIEW_MATRIX * mat4(INV_VIEW_MATRIX[0] * s, INV_VIEW_MATRIX[1] * s, INV_VIEW_MATRIX[2] * s, MODEL_MATRIX[3]);
}

void fragment() {
	vec2 p = UV * 2.0 - 1.0;
	float r = length(p), a = atan(p.y, p.x) + spin;
	float t = progress;
	float flare = smoothstep(0.0, 0.06, t) * (1.0 - smoothstep(0.06, 1.0, t));
	float white = exp(-r * r * mix(30.0, 140.0, t)) * (1.0 - smoothstep(0.1, 0.6, t)) * 3.0;
	float halo = exp(-r * mix(3.2, 4.4, t)) * (1.0 - smoothstep(0.55, 1.0, t));   // the pink holds, then goes
	float spikes = (pow(abs(cos(a * 2.0)), 80.0) + 0.6 * pow(abs(cos(a * 2.0 + 0.785)), 120.0)) * exp(-r * 4.0) * flare * 1.6;
	vec3 col = edge_col * halo * 1.6 + core_col * (white + spikes);
	ALBEDO = col;
	ALPHA = clamp(halo + white + spikes, 0.0, 1.0) * (1.0 - smoothstep(0.85, 1.0, r));
}
