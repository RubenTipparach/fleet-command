// ship_bake.gdshader: paints a ship's surfaces onto its own unwrap for the low-spec mode (LowShips; the owner, 2026-11-12:
// "bake hull lights for pi into 1 layer, the mesh should be two materials: base and emission"). Each triangle is drawn at
// its place in the unwrap (UV2), not as seen, and coloured as the battle draws it, without the sun: pass 0 its base
// colour, pass 1 its hull lights (the floodlight pools and their glints on a plated hull; the lamps, strips and light map of
// a hull with its own maps; the running lights' light on the plating), one layer, pass 2 its metalness and roughness, stored at a quarter strength so bright lights fit in a byte (EmitScale in LowShips).
// kind 0 a plated hull (hull_look.gdshaderinc, the live hull's own look), 1 a hull with its own baked maps
// (baked_hull.gdshader's), 2 a plain surface (its albedo colour and texture).
shader_type spatial;
render_mode unshaded, cull_disabled, depth_test_disabled;

#include "res://src/Game/View/hull_look.gdshaderinc"

uniform int kind = 0;
// the ship's frame back into this surface's part's own (the hull's look and its pools are placed in the part's frame;
// the unwrap is the whole ship's)
uniform mat4 to_part = mat4(1.0);
uniform int bake_pass = 0;
uniform float emit_scale = 4.0;
uniform sampler2D base_map : source_color, filter_linear;
uniform sampler2D emit_map : source_color, filter_linear;
uniform sampler2D light_map : filter_linear;
uniform float light_gamma = 1.6;
uniform float light_k = 1.4;
uniform vec4 albedo : source_color = vec4(1.0);
uniform float std_metallic = 0.0;
uniform float std_roughness = 1.0;
uniform sampler2D orm_map : filter_linear, hint_default_white;
uniform sampler2D albedo_tex : source_color, filter_linear, hint_default_white;

// the ship's running lights, strobes and beacons (its lamps' omni lights, which the low-spec mode does not keep): their
// light on the plating baked into the emission layer with the rest of the hull lights; positions in the ship's frame,
// ranges in its metres
uniform vec4 lamp_pos[24];
uniform vec4 lamp_col[24];
uniform int lamp_n = 0;

varying vec3 v_obj;
varying vec3 v_nobj;
varying vec3 v_ship;
varying vec3 v_nship;

void vertex() {
	vec3 lp = (to_part * vec4(VERTEX, 1.0)).xyz, ln = (to_part * vec4(NORMAL, 0.0)).xyz;
	v_obj = vec3(-lp.x, lp.y, -lp.z) + mockup_offset;
	v_nobj = vec3(-ln.x, ln.y, -ln.z);
	v_ship = VERTEX;
	v_nship = NORMAL;
	POSITION = vec4(UV2.x * 2.0 - 1.0, 1.0 - UV2.y * 2.0, 0.0, 1.0);
}

void fragment() {
	vec3 n = normalize(v_nobj);
	vec3 base = vec3(0.0), emit = vec3(0.0);
	float metal = 0.0, rough = 1.0;
	if (kind == 0) {
		float a, seam, paint;
		base = hull_paint(v_obj, n, COLOR.rgb, a, seam, paint);
		metal = metallic * (1.0 - 0.6 * paint);
		rough = mix(0.3 + 0.3 * (1.0 - a) + 0.15 * seam, 0.55, paint);
		if (pool_n > 0) {
			vec3 spec = vec3(0.0);
			// the glints taken as if seen along the plating's own normal, their brightest
			vec3 lit = pool_light(v_obj, n, n, pool_spec * (1.0 - rough * 0.7), spec);
			emit = base * lit + spec;
		}
	} else if (kind == 1) {
		base = texture(base_map, UV).rgb;
		vec3 orm = texture(orm_map, UV).rgb;
		base *= mix(1.0, orm.r, 0.5);   // half its baked occlusion, the ambient's share in the battle
		metal = orm.b;
		rough = orm.g;
		emit = texture(emit_map, UV).rgb + base * pow(texture(light_map, UV).rgb, vec3(light_gamma)) * light_k;
	} else {
		base = albedo.rgb * texture(albedo_tex, UV).rgb;
		metal = std_metallic;
		rough = std_roughness;
	}
	// the lamps' light, as Godot's omni light falls off: by distance to its range, on faces turned to it
	vec3 ns = normalize(v_nship);
	for (int i = 0; i < 24; i++) {
		if (i >= lamp_n) break;
		vec3 d = lamp_pos[i].xyz - v_ship;
		float dist = length(d), range = lamp_pos[i].w;
		float att = pow(max(1.0 - dist / range, 0.0), 2.0);
		emit += base * lamp_col[i].rgb * lamp_col[i].a * att * max(dot(ns, d / max(dist, 1e-3)), 0.0);
	}
	// pass 2: the surface's metalness and roughness (stored in the two maps' alpha by LowShips)
	ALBEDO = bake_pass == 0 ? base : bake_pass == 1 ? emit / emit_scale : vec3(metal, rough, 0.0);
}
