// hull_look.gdshaderinc: how an approved ship's plating looks (hull.gdshader's look, one implementation): the faction's
// detail tile projected from three sides in ship space, its seams, the Krai livery, and the floodlight pools baked into
// the hull. hull.gdshader draws it live; ship_bake.gdshader paints it once onto a ship's own unwrap for the low-spec
// mode (LowShips; the owner, 2026-11-12: "bake hull lights for pi into 1 layer, the mesh should be two materials: base
// and emission"). Positions and normals are in the mockup's ship frame.

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

// The plating's colour at p with normal n (the mockup's frame) and the vertex colour vcol: the tile, its seams darkened
// and tinted, the livery painted on; a the tile's value there, seam how much is seam, paint how much is livery.
vec3 hull_paint(vec3 p, vec3 n, vec3 vcol, out float a, out float seam, out float paint) {
	a = detail_a(p, n);
	seam = 1.0 - smoothstep(0.45, 0.6, a);
	vec3 col = base_color.rgb * (use_vertex_color ? vcol : vec3(1.0));
	col *= mix(1.0, a, detail_strength);
	col = mix(col, seam_tint * 0.45, seam * 0.5);

	paint = 0.0;
	vec3 paint_c = vec3(0.0);
	if (livery) {
		vec3 n_a = abs(n);
		float t = (p.z - liv.x) / (liv.y - liv.x), hgt = liv.w - liv.z, sc = liv2.z;
		if (abs(p.x) < liv2.x + 0.3 && n_a.x > 0.3) {
			float sw = smoothstep(0.62, 0.93, t), yc = liv.z + hgt * (0.28 + 0.3 * sw * sw);
			float w = hgt * (0.03 + 0.07 * smoothstep(0.1, 0.6, t)) * (1.0 - smoothstep(0.8, 0.94, t));
			float dy = abs(p.y - yc), in_t = step(0.12, t) * step(t, 0.94);
			float band = (1.0 - smoothstep(w - 0.04, w + 0.04, dy)) * in_t;
			float pin = (1.0 - smoothstep(0.05 * sc, 0.1 * sc, abs(dy - w - 0.32 * sc))) * in_t * step(t, 0.86) * step(0.14, t);
			float tz = p.z - (liv.x + 0.14 * (liv.y - liv.x)), pitch = 0.75 * sc, k = floor(tz / pitch), in_g = mod(k, 6.0);
			float slots = liv2.y + floor((liv2.y - 1.0) / 5.0), ty = p.y - (yc + w + 0.9 * sc);
			float tally = step(0.0, tz) * step(k, slots - 1.0) * step(in_g, 4.5) * step(fract(tz / pitch), 0.42) * step(0.0, ty) * step(ty, 1.5 * sc);
			paint = max(band, max(pin, tally));
			paint_c = band > 0.5 ? liv_crimson : liv_bronze;
		}
	}
	return mix(col, paint_c * (0.55 + 0.45 * a) * mix(1.0, 0.85, seam), paint);
}
