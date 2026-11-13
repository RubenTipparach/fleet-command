// hull.gdshader: an approved ship's hull plating in the battle (change ship-models-engine). A port of the ship mockups'
// hull shader (docs/mockups/lib/ship-skin.js GLSL.detail, and the Krai hull material in docs/mockups/krai-ship-concepts.html
// hullMat): the faction's detail tile (assets/ships/<faction>/detail_albedo.png and detail_normal.png, drawn by the same
// seeded generator the mockup uses) projected from three sides in ship space, its seams darkened and tinted, and the Krai
// livery (a crimson war stripe low on each flank that sweeps up toward the bow, bronze pin lines, battle tallies in fives)
// painted from the same formula. Positions are taken back into the mockup's ship frame (+X port, +Z bow, metres, before
// the export's re-centring) so the tile and the livery land exactly where the mockup puts them.
shader_type spatial;
render_mode cull_disabled;

#include "res://src/Game/View/hull_look.gdshaderinc"
#include "res://src/Game/View/damage.gdshaderinc"

varying vec3 v_obj;
varying vec3 v_nobj;
varying vec3 v_cam;

void vertex() {
	v_obj = vec3(-VERTEX.x, VERTEX.y, -VERTEX.z) + mockup_offset;
	v_nobj = vec3(-NORMAL.x, NORMAL.y, -NORMAL.z);
	vec3 c = (inverse(MODEL_MATRIX) * vec4(CAMERA_POSITION_WORLD, 1.0)).xyz;
	v_cam = vec3(-c.x, c.y, -c.z) + mockup_offset;
}

void fragment() {
	// battle damage (damage.gdshaderinc): holes, scorch marks, and a broken piece's own cell
	float d_char = 0.0, d_ember = 0.0; int hole_idx = -1; float nz = dfbm(v_obj * 0.45);
	dmg_holes(v_obj, nz, d_char, d_ember, hole_idx);
	float nz2 = dfbm(v_obj * 0.9 + 7.0);
	dmg_scorch(v_obj, nz2, d_char, d_ember);
	if (dmg_break(v_obj, nz, d_char, d_ember)) discard;
	d_char = clamp(d_char, 0.0, 1.0);
	vec3 n = normalize(v_nobj);
	if (flat_shading) {
		vec3 f = normalize(cross(dFdx(v_obj), dFdy(v_obj)));
		n = dot(f, n) < 0.0 ? -f : f;
	}
	float a, seam, paint;
	vec3 col = hull_paint(v_obj, n, COLOR.rgb, a, seam, paint);
	col = mix(col, vec3(0.035, 0.03, 0.027) * (0.8 + 0.4 * nz2), smoothstep(0.0, 0.7, d_char));   // soot, smooth rather than speckled
	if (!FRONT_FACING) col = back_color;

	ALBEDO = col;
	METALLIC = metallic * (1.0 - 0.6 * paint) * (1.0 - 0.85 * d_char * d_char);
	ROUGHNESS = mix(mix(0.3 + 0.3 * (1.0 - a) + 0.15 * seam, 0.55, paint), 1.0, smoothstep(0.0, 0.7, d_char));
	vec3 glow = vec3(1.0, 0.36, 0.07) * d_ember * 1.8 * (0.75 + 0.25 * sin(TIME * 21.0 + nz * 30.0));
	if (!FRONT_FACING) glow = vec3(0.08, 0.008, 0.0);
	if (hole_idx >= 0) {
		glow = interior_room(v_obj, normalize(v_obj - v_cam), holes[hole_idx], hole_n[hole_idx]);
		ALBEDO = vec3(0.0); ROUGHNESS = 1.0; METALLIC = 0.0;
	}
	EMISSION = glow;
	// fine detail fades as it shrinks below a few pixels, so seams and bolts never sparkle at a distance
	float px = max(length(dFdx(v_obj)), length(dFdy(v_obj)));
	float fine = 1.0 - smoothstep(0.012, 0.05, px);
	vec3 nd = detail_n(v_obj, n, fine);
	// the baked floodlight pools, with their glints, dimmed where the plating is charred
	if (FRONT_FACING && hole_idx < 0 && pool_n > 0) {
		vec3 spec = vec3(0.0);
		vec3 lit = pool_light(v_obj, nd, normalize(v_cam - v_obj), pool_spec * (1.0 - ROUGHNESS * 0.7), spec);
		EMISSION += (col * lit + spec) * (1.0 - clamp(d_char * 1.4, 0.0, 1.0));
	}
	if (!FRONT_FACING) nd = -nd;
	vec3 n_model = vec3(-nd.x, nd.y, -nd.z);
	NORMAL = normalize((VIEW_MATRIX * MODEL_MATRIX * vec4(n_model, 0.0)).xyz);
}
