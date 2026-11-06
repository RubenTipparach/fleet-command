// beads.gdshader: a discrete trail, the game's port of the weapons range's beads (docs/mockups/lib/weapon-fx.js, beads();
// owner, 2026-11-04: "torpedo should have like discrete trail instead of a long streak", "large spheres to small
// spehres", "half the length of tail ... larger (2x)", "torpedo tail should fade out"). Each bead is one MultiMesh
// instance, a camera-facing quad left where the shot was; INSTANCE_CUSTOM.x is the battle time it was dropped. It shrinks
// from size at the head to an eighth of it and fades out over life_s, with a little twinkle. Never white.
shader_type spatial;
render_mode unshaded, blend_add, depth_draw_never, cull_disabled, shadows_disabled, fog_disabled;

uniform vec4 color : source_color = vec4(0.56, 0.17, 1.0, 1.0);
uniform float size = 1.0;
uniform float life_s = 0.4;
uniform float time_s = 0.0;

varying vec2 v_p;
varying float v_k;
varying float v_seed;

vec3 hot_of(vec3 c) { return c / max(max(c.r, c.g), max(c.b, 0.001)); }

void vertex() {
	float k = clamp((time_s - INSTANCE_CUSTOM.x) / life_s, 0.0, 1.0);
	v_k = INSTANCE_CUSTOM.x < -1.0 ? 1.0 : k;
	v_seed = INSTANCE_CUSTOM.x * 37.0;
	v_p = VERTEX.xy * 2.0;
	MODELVIEW_MATRIX = VIEW_MATRIX * mat4(INV_VIEW_MATRIX[0], INV_VIEW_MATRIX[1], INV_VIEW_MATRIX[2], MODEL_MATRIX[3]);
	VERTEX *= size * (1.0 - 0.88 * v_k) * (v_k >= 1.0 ? 0.0 : 1.0);
}

void fragment() {
	vec2 q = v_p * 0.5;
	float r = length(q);
	float dot_ = exp(-r * r * 40.0), heart = exp(-r * r * 260.0);
	float cross_ = (exp(-abs(q.x) * 60.0) + exp(-abs(q.y) * 60.0)) * exp(-r * 9.0) * 0.5;
	float tw = 0.8 + 0.2 * sin(time_s * 30.0 + v_seed);
	float a = pow(1.0 - v_k, 3.0) * tw;
	ALBEDO = (color.rgb * (dot_ * 2.2 + cross_ * 1.4) + hot_of(color.rgb) * heart * 2.2) * a * (1.0 - smoothstep(0.4, 0.5, r));
}
