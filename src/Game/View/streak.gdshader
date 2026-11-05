// streak.gdshader: a spark drawn as a streak along its own flight (owner, 2026-11-04: "add in some smaller debris like
// sparks", "shouldnt the sparks be coming from the center?"). Each particle's Y axis is its velocity (Fx.Streaks aligns it),
// so the quad is laid along that axis and turned about it to face the camera: seen from anywhere, the sparks radiate from
// the blast. Bright at the head, tapering to the tail, soft across; its colour and alpha are the particle's. Added.
shader_type spatial;
render_mode unshaded, blend_add, depth_draw_never, cull_disabled, shadows_disabled, fog_disabled, skip_vertex_transform;

void vertex() {
	vec3 along = MODEL_MATRIX[1].xyz;
	float len = length(along);
	vec3 y = along / max(len, 1e-5);
	vec3 pos = MODEL_MATRIX[3].xyz;
	vec3 to_cam = normalize(INV_VIEW_MATRIX[3].xyz - pos);
	vec3 x = cross(y, to_cam);
	x = length(x) > 1e-4 ? normalize(x) : normalize(INV_VIEW_MATRIX[0].xyz);
	float wide = length(MODEL_MATRIX[0].xyz);
	vec3 world = pos + x * VERTEX.x * wide + y * VERTEX.y * len;
	VERTEX = (VIEW_MATRIX * vec4(world, 1.0)).xyz;
}

void fragment() {
	float across = 1.0 - abs(UV.x * 2.0 - 1.0);
	float head = 1.0 - UV.y;            // the quad's top (its +Y, the way it flies) is UV.y 0
	ALBEDO = COLOR.rgb * (1.0 + head);
	ALPHA = clamp(across * across * (0.15 + 0.85 * head) * COLOR.a, 0.0, 1.0);
}
