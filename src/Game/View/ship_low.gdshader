// ship_low.gdshader: a ship part's base in the low-spec mode (LowShips): one surface for all its plating, metal and
// markings, its colour from the baked base map and its hull lights from the baked emission layer, lit by the battle's sun;
// its roughness and metalness ride in the two maps' alpha.
// Battle damage is the live hull's (damage.gdshaderinc): holes, scorch marks, a broken piece's own cell, and the lights
// going out where the plating is charred or the piece has broken away (lit).
shader_type spatial;
render_mode cull_disabled;
// both maps hold linear values (the bake's viewport stores what it draws as it is), so neither is read as sRGB
uniform sampler2D base_map : filter_linear_mipmap;
uniform sampler2D emit_map : filter_linear_mipmap;
uniform float emit_scale = 4.0;
uniform float lit = 1.0;
uniform vec3 mockup_offset = vec3(0.0);
#include "res://src/Game/View/damage.gdshaderinc"
varying vec3 v_obj;
varying vec3 v_cam;
varying vec3 v_view;
void vertex() {
	v_obj = vec3(-VERTEX.x, VERTEX.y, -VERTEX.z) + mockup_offset;
	vec3 c = (inverse(MODEL_MATRIX) * vec4(CAMERA_POSITION_WORLD, 1.0)).xyz;
	v_cam = vec3(-c.x, c.y, -c.z) + mockup_offset;
	v_view = (MODELVIEW_MATRIX * vec4(VERTEX, 1.0)).xyz;
}
void fragment() {
	float d_char = 0.0, d_ember = 0.0; int hole_idx = -1; float nz = dfbm(v_obj * 0.45);
	dmg_holes(v_obj, nz, d_char, d_ember, hole_idx);
	float nz2 = dfbm(v_obj * 0.9 + 7.0);
	dmg_scorch(v_obj, nz2, d_char, d_ember);
	if (dmg_break(v_obj, nz, d_char, d_ember)) discard;
	d_char = clamp(d_char, 0.0, 1.0);
	float soot = smoothstep(0.0, 0.7, d_char);
	vec3 col = mix(texture(base_map, UV).rgb, vec3(0.035, 0.03, 0.027) * (0.8 + 0.4 * nz2), soot);
	ALBEDO = col;
	// metalness in the emission map's alpha, roughness in the base map's (the bake's third pass)
	METALLIC = texture(emit_map, UV).a * (1.0 - 0.85 * d_char * d_char);
	ROUGHNESS = mix(texture(base_map, UV).a, 1.0, soot);
	vec3 glow = texture(emit_map, UV).rgb * emit_scale * (1.0 - smoothstep(0.0, 0.6, d_char)) * lit;
	glow += vec3(1.0, 0.36, 0.07) * d_ember * 1.8 * (0.75 + 0.25 * sin(TIME * 21.0 + nz * 30.0));
	if (hole_idx >= 0) {
		glow = interior_room(v_obj, normalize(v_obj - v_cam), holes[hole_idx], hole_n[hole_idx]);
		ALBEDO = vec3(0.0); ROUGHNESS = 1.0; METALLIC = 0.0;
	}
	EMISSION = glow;
	// each facet one normal, as the live hull (hull.gdshader flat_shading): the mockups draw their hulls flat shaded
	vec3 f = normalize(cross(dFdx(v_view), dFdy(v_view)));
	NORMAL = dot(f, NORMAL) < 0.0 ? -f : f;
}
