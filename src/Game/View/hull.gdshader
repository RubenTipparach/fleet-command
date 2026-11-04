// hull.gdshader: an approved ship's hull plating in the battle (change ship-models-engine). A port of the ship mockups'
// hull shader (docs/mockups/lib/ship-skin.js GLSL.detail, and the Krai hull material in docs/mockups/krai-ship-concepts.html
// hullMat): the faction's detail tile (assets/ships/<faction>/detail_albedo.png and detail_normal.png, drawn by the same
// seeded generator the mockup uses) projected from three sides in ship space, its seams darkened and tinted, and the Krai
// livery (a crimson war stripe low on each flank that sweeps up toward the bow, bronze pin lines, battle tallies in fives)
// painted from the same formula. Positions are taken back into the mockup's ship frame (+X port, +Z bow, metres, before
// the export's re-centring) so the tile and the livery land exactly where the mockup puts them.
shader_type spatial;
render_mode cull_disabled;

uniform sampler2D detail_albedo : filter_linear_mipmap, repeat_enable;
uniform sampler2D detail_normal : filter_linear_mipmap, repeat_enable;
// the tile's size along and round the hull, metres
uniform vec2 tile_m = vec2(8.0, 8.0);
// the export's re-centring: mockup frame = (-x, y, -z) + mockup_offset
uniform vec3 mockup_offset = vec3(0.0);
uniform vec4 base_color : source_color = vec4(1.0);
uniform bool use_vertex_color = true;
// the mockups draw their hulls flat shaded (each facet one normal), whatever normals the mesh carries
uniform bool flat_shading = true;
uniform float metallic = 0.72;
uniform float detail_strength = 0.8;
uniform vec3 seam_tint : source_color = vec3(0.725, 0.541, 0.306);
uniform vec3 back_color : source_color = vec3(0.05, 0.045, 0.042);
// the Krai livery (the mockup's uLiv and uLiv2): x, y the hull's stern and bow z; z, w its keel and deck y. liv2: x the
// hull's half beam (the nacelles stay bare), y the tally count, z the scale of the marks
uniform bool livery = false;
uniform vec4 liv = vec4(0.0);
uniform vec4 liv2 = vec4(0.0);
uniform vec3 liv_crimson : source_color = vec3(0.478, 0.118, 0.173);
uniform vec3 liv_bronze : source_color = vec3(0.725, 0.541, 0.306);

varying vec3 v_obj;
varying vec3 v_nobj;

void vertex() {
	v_obj = vec3(-VERTEX.x, VERTEX.y, -VERTEX.z) + mockup_offset;
	v_nobj = vec3(-NORMAL.x, NORMAL.y, -NORMAL.z);
}

vec3 det_w(vec3 n) { vec3 w = pow(abs(n), vec3(6.0)); return w / (w.x + w.y + w.z); }

float detail_a(vec3 p, vec3 n) {
	vec3 w = det_w(n);
	return texture(detail_albedo, vec2(p.z / tile_m.x, p.y / tile_m.y)).r * w.x
		+ texture(detail_albedo, vec2(p.z / tile_m.x, p.x / tile_m.y)).r * w.y
		+ texture(detail_albedo, vec2(p.x / tile_m.x, p.y / tile_m.y)).r * w.z;
}

// tri-planar normal map, whiteout blend in ship space (the mockup's detailN)
vec3 detail_n(vec3 p, vec3 n, float k) {
	vec3 w = det_w(n);
	vec2 tx = (texture(detail_normal, vec2(p.z / tile_m.x, p.y / tile_m.y)).xy * 2.0 - 1.0) * k;
	vec2 ty = (texture(detail_normal, vec2(p.z / tile_m.x, p.x / tile_m.y)).xy * 2.0 - 1.0) * k;
	vec2 tz = (texture(detail_normal, vec2(p.x / tile_m.x, p.y / tile_m.y)).xy * 2.0 - 1.0) * k;
	vec3 nx = vec3(n.x, n.y + tx.y, n.z + tx.x * sign(n.x));
	vec3 ny = vec3(n.x + ty.y * sign(n.y), n.y, n.z + ty.x);
	vec3 nz = vec3(n.x + tz.x * sign(n.z), n.y + tz.y, n.z);
	return normalize(nx * w.x + ny * w.y + nz * w.z);
}

void fragment() {
	vec3 n = normalize(v_nobj);
	if (flat_shading) {
		vec3 f = normalize(cross(dFdx(v_obj), dFdy(v_obj)));
		n = dot(f, n) < 0.0 ? -f : f;
	}
	float a = detail_a(v_obj, n);
	float seam = 1.0 - smoothstep(0.45, 0.6, a);
	vec3 col = base_color.rgb * (use_vertex_color ? COLOR.rgb : vec3(1.0));
	col *= mix(1.0, a, detail_strength);
	col = mix(col, seam_tint * 0.45, seam * 0.5);

	float paint = 0.0;
	vec3 paint_c = vec3(0.0);
	if (livery) {
		vec3 n_a = abs(n);
		float t = (v_obj.z - liv.x) / (liv.y - liv.x), hgt = liv.w - liv.z, sc = liv2.z;
		if (abs(v_obj.x) < liv2.x + 0.3 && n_a.x > 0.3) {
			float sw = smoothstep(0.62, 0.93, t), yc = liv.z + hgt * (0.28 + 0.3 * sw * sw);
			float w = hgt * (0.03 + 0.07 * smoothstep(0.1, 0.6, t)) * (1.0 - smoothstep(0.8, 0.94, t));
			float dy = abs(v_obj.y - yc), in_t = step(0.12, t) * step(t, 0.94);
			float band = (1.0 - smoothstep(w - 0.04, w + 0.04, dy)) * in_t;
			float pin = (1.0 - smoothstep(0.05 * sc, 0.1 * sc, abs(dy - w - 0.32 * sc))) * in_t * step(t, 0.86) * step(0.14, t);
			float tz = v_obj.z - (liv.x + 0.14 * (liv.y - liv.x)), pitch = 0.75 * sc, k = floor(tz / pitch), in_g = mod(k, 6.0);
			float slots = liv2.y + floor((liv2.y - 1.0) / 5.0), ty = v_obj.y - (yc + w + 0.9 * sc);
			float tally = step(0.0, tz) * step(k, slots - 1.0) * step(in_g, 4.5) * step(fract(tz / pitch), 0.42) * step(0.0, ty) * step(ty, 1.5 * sc);
			paint = max(band, max(pin, tally));
			paint_c = band > 0.5 ? liv_crimson : liv_bronze;
		}
	}
	col = mix(col, paint_c * (0.55 + 0.45 * a) * mix(1.0, 0.85, seam), paint);
	if (!FRONT_FACING) col = back_color;

	ALBEDO = col;
	METALLIC = metallic * (1.0 - 0.6 * paint);
	ROUGHNESS = mix(0.3 + 0.3 * (1.0 - a) + 0.15 * seam, 0.55, paint);
	// fine detail fades as it shrinks below a few pixels, so seams and bolts never sparkle at a distance
	float px = max(length(dFdx(v_obj)), length(dFdy(v_obj)));
	float fine = 1.0 - smoothstep(0.012, 0.05, px);
	vec3 nd = detail_n(v_obj, n, fine);
	if (!FRONT_FACING) nd = -nd;
	vec3 n_model = vec3(-nd.x, nd.y, -nd.z);
	NORMAL = normalize((VIEW_MATRIX * MODEL_MATRIX * vec4(n_model, 0.0)).xyz);
}
