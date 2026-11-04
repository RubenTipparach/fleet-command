// cutface.gdshader: the face that closes a broken piece of hull (owner, 2026-11-02: "interiors are hollowed, this is not
// what I want"), a port of the ship mockups' cut face (docs/mockups/lib/ship-break.js and docs/mockups/concord-hull-concepts.html,
// cutFace() and CUTFACE_GLSL). A quad on the plane between two fracture cells, clipped here to the lofted body and to the
// border the two cells share; it shows the skin cut edge on, deck plates every 3.4 m, bulkheads, and between them rooms
// with depth by interior mapping, some lit red by emergency light, soot toward the break, flame licking up the walls and
// an ember line along the broken edge that cools with heat. Everything in mockup ship space, as hull.gdshader.
shader_type spatial;
render_mode cull_disabled;

uniform vec3 mockup_offset = vec3(0.0);
uniform vec3 seeds[16];
uniform int seed_n = 0;
uniform int cell_a = 0;
uniform int cell_b = 0;
// the body's stations, bow first: x z, y half beam, z top, w bottom; ch the chamfer at each
uniform vec4 st[48];
uniform float ch[48];
uniform int st_n = 0;
uniform float ox = 0.0;
uniform float heat = 1.0;
uniform vec3 across = vec3(1.0, 0.0, 0.0);
uniform vec3 depth_dir = vec3(0.0, 0.0, 1.0);
uniform vec3 mid = vec3(0.0);

varying vec3 v_obj;
varying vec3 v_cam;

void vertex() {
	v_obj = vec3(-VERTEX.x, VERTEX.y, -VERTEX.z) + mockup_offset;
	vec3 c = (inverse(MODEL_MATRIX) * vec4(CAMERA_POSITION_WORLD, 1.0)).xyz;
	v_cam = vec3(-c.x, c.y, -c.z) + mockup_offset;
}

float fh(vec3 p) { p = fract(p * 0.3183099 + 0.1); p *= 17.0; return fract(p.x * p.y * p.z * (p.x + p.y + p.z)); }
float fn(vec3 x) {
	vec3 i = floor(x), f = fract(x); f = f * f * (3.0 - 2.0 * f);
	return mix(mix(mix(fh(i), fh(i + vec3(1, 0, 0)), f.x), mix(fh(i + vec3(0, 1, 0)), fh(i + vec3(1, 1, 0)), f.x), f.y),
		mix(mix(fh(i + vec3(0, 0, 1)), fh(i + vec3(1, 0, 1)), f.x), mix(fh(i + vec3(0, 1, 1)), fh(i + vec3(1, 1, 1)), f.x), f.y), f.z);
}
// Signed distance into the lofted body (positive inside): the octagon at z.
float loft_d(vec3 p) {
	p.x -= ox;
	if (p.z > st[0].x || p.z < st[st_n - 1].x) return -1.0;
	vec4 s = st[0]; float c = ch[0];
	for (int i = 0; i < 47; i++) {
		if (i >= st_n - 1) break;
		vec4 a = st[i], b = st[i + 1];
		if (p.z <= a.x && p.z >= b.x) { float t = (a.x - p.z) / max(a.x - b.x, 1e-4); s = mix(a, b, t); c = mix(ch[i], ch[i + 1], t); break; }
	}
	float ax = abs(p.x);
	float d = min(min(s.y - ax, s.z - p.y), p.y - s.w);
	d = min(d, min((s.y + s.z - c - ax - p.y) * 0.70710678, (s.y - s.w - c - ax + p.y) * 0.70710678));
	return min(d, min(st[0].x - p.z, p.z - st[st_n - 1].x));
}
float band(float v, float period, float w) { float m = mod(v + 1000.0 * period, period); return 1.0 - smoothstep(w * 0.5, w, min(m, period - m)); }
vec3 to_f(vec3 p) { vec3 d = p - mid; return vec3(dot(d, across), p.y, dot(d, depth_dir)); }
vec3 dir_f(vec3 v) { return vec3(dot(v, across), v.y, dot(v, depth_dir)); }
vec3 deck_cell(float deck_n) { return vec3(6.0 + 3.0 * floor(fh(vec3(deck_n, 4.1, 0.7)) * 3.0), 3.4, 5.0 + 3.0 * fh(vec3(deck_n, 8.3, 1.9))); }
float deck_shift(float deck_n) { return fh(vec3(deck_n, 2.0, 5.0)) * 12.0; }
float cut_frame_w(vec3 f, float k) { float deck_n = floor(f.y / 3.4); vec3 cl = deck_cell(deck_n); return max(band(f.y, 3.4, 0.32 * k), band(f.x + deck_shift(deck_n), cl.x, 0.26 * k)); }
float cut_frame(vec3 f) { return cut_frame_w(f, 1.0); }
vec3 interior_cut(vec3 f, vec3 V, out float solid) {
	float deck_n = floor((f.y + V.y * 0.03) / 3.4); vec3 cl = deck_cell(deck_n);
	vec3 q = f + vec3(deck_shift(deck_n), 0.0, 0.0) + V * 0.03; q.z = max(q.z, 0.001);
	vec3 room = floor(q / cl); room.z = 0.0; float rh = fh(room + vec3(0.37, 1.3, 2.9) + deck_n * 0.11);
	solid = 0.0;
	vec3 t3;
	for (int a = 0; a < 3; a++) {
		float va = V[a];
		if (abs(va) < 1e-4) { t3[a] = 1e5; continue; }
		float b = va > 0.0 ? (room[a] + 1.0) * cl[a] : room[a] * cl[a];
		t3[a] = (b - q[a]) / va;
	}
	if (V.z <= 0.0) t3.z = 1e5;
	float t = min(t3.x, min(t3.y, t3.z)); vec3 hit = q + V * t; vec3 local = hit - room * cl; vec3 base;
	if (t == t3.y) base = V.y < 0.0 ? vec3(0.32, 0.30, 0.27) * (0.85 + 0.15 * step(0.5, fract(local.x * 0.5 + 0.5 * step(0.5, fract(local.z * 0.5)))))
			* (1.0 - 0.7 * step(0.72, fh(floor(local * 0.9) + room)))
		: vec3(0.05) + vec3(0.7, 0.66, 0.58) * step(0.8, fract(local.x / cl.x * 2.0 + 0.1)) * step(0.5, rh);
	else if (t == t3.z) base = vec3(0.13, 0.125, 0.12) * (0.7 + 0.3 * fh(floor(local * 0.5) + room)) * (1.0 - 0.55 * step(local.y, 0.25));
	else base = vec3(0.19, 0.185, 0.18) * (0.75 + 0.25 * fh(floor(local * 0.5) + room)) * (1.0 - 0.55 * step(local.y, 0.25));
	float lamp = 0.35 + 0.65 * clamp(local.y / 3.4, 0.0, 1.0);
	vec3 L;
	if (rh < 0.62) L = vec3(0.03, 0.035, 0.05);
	else if (rh < 0.86) L = vec3(0.9, 0.07, 0.04);
	else L = vec3(0.55, 0.65, 0.85);
	float soot_p = smoothstep(0.42, 0.7, fn(hit * 0.7 + room * 3.1)) * (1.0 - smoothstep(0.0, 4.5, local.z));
	base *= 1.0 - 0.8 * soot_p;
	float lick = (1.0 - smoothstep(0.0, 2.4, local.z)) * smoothstep(0.5, 0.8, fn(vec3(hit.x * 0.9, hit.y * 1.7 - TIME * 1.6, hit.z * 0.9 + room.x)));
	vec3 flame = vec3(1.0, 0.3, 0.07) * lick * (1.0 - step(t3.y, t) * step(V.y, 0.0) * 0.5);
	return base * L * 2.0 * lamp * exp(-t * 0.13) + flame * (0.35 + 0.35 * heat) * (0.85 + 0.15 * sin(TIME * 14.0 + hit.y * 3.0));
}

void fragment() {
	float sd = loft_d(v_obj);
	if (sd < 0.0) discard;
	vec3 s_a = seeds[cell_a]; float d_a = length(v_obj - s_a); float edge_c = 1e9;
	for (int k = 0; k < 16; k++) {
		if (k >= seed_n) break;
		if (k == cell_a || k == cell_b) continue;
		vec3 sk = seeds[k]; float dk = length(v_obj - sk);
		if (dk < d_a - 0.02) discard;
		edge_c = min(edge_c, (dk * dk - d_a * d_a) / (2.0 * length(sk - s_a)));
	}
	float ed = min(sd, edge_c); float nz = 0.6 * fn(v_obj * 0.35) + 0.4 * fn(v_obj * 1.3 + 5.0);
	vec3 fp = to_f(v_obj); float frame = cut_frame(fp);
	float skin = 1.0 - smoothstep(0.45, 1.1, sd);
	float solid; vec3 room_col = interior_cut(fp, dir_f(normalize(v_obj - v_cam)), solid);
	float soot = clamp(1.0 - smoothstep(0.0, 12.0, ed + nz * 5.0), 0.0, 1.0);
	float is_struct = max(max(step(0.5, frame), step(0.5, skin)), solid);
	vec3 metal = mix(vec3(0.33, 0.32, 0.30), vec3(0.58, 0.57, 0.54), skin);
	metal = mix(metal, vec3(0.018, 0.016, 0.015), clamp(0.35 + 0.6 * soot + 0.25 * (nz - 0.5), 0.0, 1.0));
	vec3 col = is_struct > 0.5 ? metal : vec3(0.0);
	vec3 emit = is_struct > 0.5 ? vec3(0.0) : room_col * (1.0 - 0.8 * soot);
	float ember = (1.0 - smoothstep(0.0, 1.1, ed + nz * 1.3)) * (0.45 + 0.55 * heat) * (0.8 + 0.2 * sin(TIME * 17.0 + nz * 30.0));
	float rim = clamp(cut_frame_w(fp, 1.9 + 0.8 * nz) - cut_frame(fp), 0.0, 1.0) * smoothstep(0.3, 0.55, fn(v_obj * 0.9 + 2.0)) * (1.0 - solid);
	emit += vec3(1.0, 0.36, 0.07) * rim * (0.35 + 0.8 * heat);
	col = mix(col, vec3(0.02, 0.016, 0.014), rim);
	ALBEDO = col;
	METALLIC = 0.3;
	ROUGHNESS = 0.85;
	EMISSION = emit + vec3(1.0, 0.36, 0.07) * ember * 2.8;
}
