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
// the export's re-centring and the piece's pivot: mockup frame = (-x, y, -z) + mockup_offset
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

#include "res://src/Game/View/damage.gdshaderinc"

// The floodlights, baked into the hull as light pools (the mockups' pools(), mockup 6's "baked hull lights"; owner,
// 2026-11-04: "I thought we were using emissive baked lights + shader based multi specular? for ship hull detail
// lighting"): each lamp a position and range (pools), an aim (pool_d), a strength (pool_a) and a colour (pool_c), in
// mockup ship space, up to 16. Its light is taken against the detail normal, so it grazes the plating and every seam
// catches it; on top, each lamp puts its own specular glint on the plating, moving with the view.
uniform vec4 pools[16];
uniform vec4 pool_d[16];
uniform float pool_a[16];
uniform vec4 pool_c[16];
uniform int pool_n = 0;
// how strong the lamps' glints are, and how tight
uniform float pool_spec = 0.55;
uniform float pool_shine = 40.0;

// The mockups' pools(): a cone with a soft edge, falling off with distance, only on faces turned toward the lamp. Returns
// the light (diffuse, in the lamps' colours) and adds each lamp's glint to spec.
vec3 pool_light(vec3 p, vec3 n, vec3 v, float gloss, inout vec3 spec) {
	vec3 sum = vec3(0.0);
	for (int i = 0; i < 16; i++) {
		if (i >= pool_n) break;
		vec4 P = pools[i]; vec3 d = p - P.xyz; float dist = length(d); vec3 dl = d / max(dist, 1e-3);
		float cone = smoothstep(0.64, 0.76, dot(dl, pool_d[i].xyz));
		float fall = (1.0 - smoothstep(P.w * 0.55, P.w, dist)) / (1.0 + pow(dist / P.w * 2.4, 2.0));
		float ndl = dot(n, -dl), face = smoothstep(-0.02, 0.12, ndl) * (0.45 + 1.6 * max(ndl, 0.0));
		float k = pool_a[i] * cone * fall;
		sum += pool_c[i].rgb * k * face;
		vec3 h = normalize(-dl + v);
		spec += pool_c[i].rgb * k * step(0.0, ndl) * pow(max(dot(n, h), 0.0), pool_shine) * gloss;
	}
	return sum;
}

varying vec3 v_obj;
varying vec3 v_nobj;
varying vec3 v_cam;

void vertex() {
	v_obj = vec3(-VERTEX.x, VERTEX.y, -VERTEX.z) + mockup_offset;
	v_nobj = vec3(-NORMAL.x, NORMAL.y, -NORMAL.z);
	vec3 c = (inverse(MODEL_MATRIX) * vec4(CAMERA_POSITION_WORLD, 1.0)).xyz;
	v_cam = vec3(-c.x, c.y, -c.z) + mockup_offset;
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
	col = mix(col, vec3(0.03, 0.026, 0.024) * (0.6 + 0.8 * nz2), smoothstep(0.0, 0.7, d_char));
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
