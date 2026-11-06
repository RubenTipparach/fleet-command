// baked_hull.gdshader: a hull that carries its own baked maps, the Ssarith Hunt's (change ssarith-ships-engine; mockup 12,
// docs/mockups/ssarith-ship-concepts.html, hullLooks() and hullShader(), its PBR look). Each ship is unwrapped on its own and
// its maps baked onto its own UVs by tools/blender/painted_bake.py --ship (CLAUDE.md section 10: no repeating tile as the
// main colour): base colour, ORM (occlusion, roughness, metalness), a tangent-space normal map, emission (lamps, the light
// strips), and a light map of the hull lights falling on the plating, stored with a gamma curve this shader undoes. The
// maps are named in the export manifest's shader_params.maps and set by ShipSkin. Battle damage is hull.gdshader's
// (damage.gdshaderinc): holes, scorch marks, a broken piece's own cell; a piece that breaks away loses its lights (lit).
shader_type spatial;

uniform sampler2D base_map : source_color, filter_linear_mipmap_anisotropic;
uniform sampler2D orm_map : filter_linear_mipmap_anisotropic;
uniform sampler2D normal_map : hint_normal, filter_linear_mipmap_anisotropic;
uniform sampler2D emit_map : source_color, filter_linear_mipmap_anisotropic;
uniform sampler2D light_map : filter_linear_mipmap;
// the light map's stored curve (the manifest's light_gamma): light = stored ^ light_gamma, times light_ref in the bake's units
uniform float light_gamma = 1.6;
// how strongly the baked hull lights show in the battle's light (the page: 0.3 in its studio, 4.5 at night). The battle has
// a sun and a dim ambient, nearer the page's night on the shadow side, so the lit pools read there.
uniform float light_k = 1.4;
// 1 while the ship or its piece is whole, 0 once a piece has broken away: its lamps, strips and hull lights go out
uniform float lit = 1.0;
// the export's re-centring: mockup frame = (-x, y, -z) + mockup_offset (damage is placed in mockup ship space)
uniform vec3 mockup_offset = vec3(0.0);

#include "res://src/Game/View/damage.gdshaderinc"

varying vec3 v_obj;
varying vec3 v_cam;

void vertex() {
	v_obj = vec3(-VERTEX.x, VERTEX.y, -VERTEX.z) + mockup_offset;
	vec3 c = (inverse(MODEL_MATRIX) * vec4(CAMERA_POSITION_WORLD, 1.0)).xyz;
	v_cam = vec3(-c.x, c.y, -c.z) + mockup_offset;
}

void fragment() {
	// battle damage (damage.gdshaderinc), as hull.gdshader
	float d_char = 0.0, d_ember = 0.0; int hole_idx = -1; float nz = dfbm(v_obj * 0.45);
	dmg_holes(v_obj, nz, d_char, d_ember, hole_idx);
	float nz2 = dfbm(v_obj * 0.9 + 7.0);
	dmg_scorch(v_obj, nz2, d_char, d_ember);
	if (dmg_break(v_obj, nz, d_char, d_ember)) discard;
	d_char = clamp(d_char, 0.0, 1.0);
	float soot = smoothstep(0.0, 0.7, d_char);

	vec3 base = texture(base_map, UV).rgb;
	vec3 orm = texture(orm_map, UV).rgb;
	vec3 col = mix(base, vec3(0.03, 0.026, 0.024) * (0.6 + 0.8 * nz2), soot);   // soot, as the page
	ALBEDO = col;
	AO = orm.r;
	ROUGHNESS = mix(orm.g, 1.0, soot);
	METALLIC = orm.b * (1.0 - 0.85 * d_char * d_char);
	NORMAL_MAP = texture(normal_map, UV).rgb;

	// the lamps and strips, then the hull lights on the plating; both die where it is charred and when the piece breaks away
	vec3 glow = texture(emit_map, UV).rgb * (1.0 - smoothstep(0.0, 0.6, d_char)) * lit;
	glow += col * pow(texture(light_map, UV).rgb, vec3(light_gamma)) * light_k * lit;
	glow += vec3(1.0, 0.36, 0.07) * d_ember * 1.8 * (0.75 + 0.25 * sin(TIME * 21.0 + nz * 30.0));
	if (hole_idx >= 0) {
		glow = interior_room(v_obj, normalize(v_obj - v_cam), holes[hole_idx], hole_n[hole_idx]);
		ALBEDO = vec3(0.0); ROUGHNESS = 1.0; METALLIC = 0.0; AO = 1.0;
	}
	EMISSION = glow;
}
