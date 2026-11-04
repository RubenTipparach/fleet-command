// damage.gdshaderinc: battle damage on a hull, the rules of the ship mockups' DAMAGE_GLSL and CELL_GLSL
// (docs/mockups/lib/ship-fx.js, lifted from mockup 6 where the owner asked for each piece), ported to Godot's shading
// language for hull.gdshader. Everything is in mockup ship space (+X port, +Y up, +Z bow, metres), the hull shader's v_obj.
// Scorch marks (a charred centre, an ember ring that cools), holes (charred round, the decks behind them by interior
// mapping), and a broken piece's cell (only the skin nearest its own seed, charred and glowing toward the break).
// What colours mean is the Ship Design Bible's: orange fire, red emergency light in a hole.

// holes: xyz centre, w radius; hole_n: xyz the hull's normal there, w the hole's kind (0 fire, 1 plasma, 2 coolant, 3 plain)
uniform vec4 holes[24];
uniform vec4 hole_n[24];
uniform int hole_count = 0;
// scorch: xyz centre, w radius; scorch_heat: 1 fresh to 0 cold (the ember ring)
uniform vec4 scorch[24];
uniform float scorch_heat[24];
uniform int scorch_count = 0;
// a piece cut off at up to four planes (n.p = d): its stump or its broken end is charred toward the cut
uniform vec4 cuts[4];
uniform vec4 cut_c[4];
uniform int cut_count = 0;
uniform float cut_heat = 0.0;
// a fracture cell: -1 the whole body; otherwise this copy keeps only the skin of cell `cell`
uniform int cell = -1;
uniform vec3 seeds[16];
uniform int seed_n = 0;
// 1 where a cell has gone, so its neighbours' edges toward it char and glow
uniform float open_cells[16];
// which chunk each cell left the ship in (0 still on it): cells that went together stay whole between them
uniform float cell_group[16];

float dh3(vec3 p) { p = fract(p * 0.3183099 + 0.1); p *= 17.0; return fract(p.x * p.y * p.z * (p.x + p.y + p.z)); }
float dn3(vec3 x) {
	vec3 i = floor(x), f = fract(x); f = f * f * (3.0 - 2.0 * f);
	return mix(mix(mix(dh3(i), dh3(i + vec3(1, 0, 0)), f.x), mix(dh3(i + vec3(0, 1, 0)), dh3(i + vec3(1, 1, 0)), f.x), f.y),
		mix(mix(dh3(i + vec3(0, 0, 1)), dh3(i + vec3(1, 0, 1)), f.x), mix(dh3(i + vec3(0, 1, 1)), dh3(i + vec3(1, 1, 1)), f.x), f.y), f.z);
}
float cell_dist(vec3 p, vec3 s, int k) { return length(p - s) + 1.1 * (dn3(p * 0.32 + float(k) * 7.13) - 0.5); }
float dfbm(vec3 p) { return 0.5 * dn3(p) + 0.3 * dn3(p * 2.03) + 0.2 * dn3(p * 4.1); }
vec3 hole_light(float kind) { return kind < 0.5 ? vec3(1.0, 0.16, 0.06) : (kind < 1.5 ? vec3(0.55, 0.32, 1.0) : (kind < 2.5 ? vec3(0.35, 0.85, 1.0) : vec3(0.85, 0.72, 0.55))); }

// Interior mapping: a room grid (decks every 3.4 m, walls every 6 m) and a back wall, seen through the skin.
vec3 interior_room(vec3 p, vec3 V, vec4 h, vec4 hn) {
	vec3 cl = vec3(6.0, 3.4, 6.0); vec3 q = p + V * 0.01; vec3 t3;
	for (int a = 0; a < 3; a++) {
		float va = V[a];
		if (abs(va) < 1e-4) { t3[a] = 1e5; continue; }
		float b = va > 0.0 ? (floor(q[a] / cl[a]) + 1.0) * cl[a] : floor(q[a] / cl[a]) * cl[a];
		t3[a] = (b - q[a]) / va;
	}
	float depth = max(h.w * 1.4, 5.0); float vn = dot(V, hn.xyz);
	float tb = vn < -1e-3 ? max((-depth - dot(q - h.xyz, hn.xyz)) / vn, 0.0) : 1e5;
	float t = min(min(t3.x, t3.y), min(t3.z, tb)); vec3 hit = q + V * t; vec3 col; vec3 room = floor(q / cl); vec3 local = hit - room * cl;
	float rh = dh3(room + 0.37);
	if (t == tb) col = vec3(0.14, 0.13, 0.12) * (0.7 + 0.3 * dh3(floor(hit * 0.8)));
	else if (t == t3.y) col = V.y < 0.0 ? vec3(0.30, 0.28, 0.25) * (0.85 + 0.15 * step(0.5, fract(local.x * 0.5 + 0.5 * step(0.5, fract(local.z * 0.5)))))
			* (1.0 - 0.7 * step(0.7, dh3(floor(local * 0.9) + room)))
		: vec3(0.05) + vec3(0.7, 0.66, 0.58) * step(0.82, fract(local.x / 6.0 * 2.0 + 0.1));
	else col = vec3(0.18, 0.17, 0.16) * (0.75 + 0.25 * dh3(floor(local * 0.5) + room)) * (1.0 - 0.55 * step(local.y, 0.25));
	float lamp = 0.4 + 0.6 * clamp(local.y / 3.4, 0.0, 1.0);
	vec3 L = rh < 0.7 || hn.w > 0.5 ? hole_light(hn.w) * 1.4 : vec3(0.55, 0.65, 0.85);
	float soot_in = 1.0 - 0.6 * (1.0 - smoothstep(h.w * 0.2, h.w * 0.9, length(p - h.xyz)));
	return col * L * 2.4 * lamp * soot_in * exp(-t * 0.08);
}

void dmg_holes(vec3 p, float nz, inout float d_char, inout float d_ember, inout int hole_idx) {
	for (int i = 0; i < 24; i++) {
		if (i >= hole_count) break;
		vec4 h = holes[i]; float d = length(p - h.xyz); float r = h.w * (0.72 + 0.56 * nz);
		if (d < r) hole_idx = i;
		d_char = max(d_char, 1.0 - smoothstep(r, r * 2.4, d));
		d_ember = max(d_ember, step(hole_n[i].w, 2.5) * step(r, d) * (1.0 - smoothstep(r, r * 1.2, d)));
	}
}

void dmg_scorch(vec3 p, float nz2, inout float d_char, inout float d_ember) {
	for (int i = 0; i < 24; i++) {
		if (i >= scorch_count) break;
		vec4 s = scorch[i]; float d = length(p - s.xyz); float r = s.w * (0.55 + 0.9 * nz2);
		d_char = max(d_char, 0.92 * (1.0 - smoothstep(r * 0.25, r, d)));
		d_ember = max(d_ember, scorch_heat[i] * smoothstep(r * 0.45, r * 0.75, d) * (1.0 - smoothstep(r * 0.75, r, d)));
	}
}

// True where the fragment is not this cell's: discard it.
bool dmg_break(vec3 p, float nz, inout float d_char, inout float d_ember) {
	for (int i = 0; i < 4; i++) {
		if (i >= cut_count) break;
		float d = abs(dot(p, cuts[i].xyz) - cuts[i].w) + nz * 2.5 - 0.8;
		float near = 1.0 - smoothstep(cut_c[i].w * 0.85, cut_c[i].w, length(p - cut_c[i].xyz));
		d_char = max(d_char, near * (1.0 - smoothstep(0.0, 7.0, d)));
		d_ember = max(d_ember, near * cut_heat * (1.0 - smoothstep(0.0, 1.6, d)));
	}
	if (cell >= 0) {
		vec3 si = seeds[cell]; float di = cell_dist(p, si, cell); float edge = 1e9;
		for (int k = 0; k < 16; k++) {
			if (k >= seed_n) break;
			if (k == cell) continue;
			vec3 sk = seeds[k];
			float dk = cell_dist(p, sk, k);
			if (dk < di) return true;
			if (max(open_cells[cell], open_cells[k]) > 0.5 && abs(cell_group[cell] - cell_group[k]) > 0.5) edge = min(edge, (dot(p - sk, p - sk) - dot(p - si, p - si)) / (2.0 * length(sk - si)));
		}
		d_char = max(d_char, 1.0 - smoothstep(0.0, 11.0, edge + nz * 4.0));
		d_ember = max(d_ember, (1.0 - smoothstep(0.0, 1.6, edge + nz * 1.6)) * (0.45 + 0.55 * cut_heat) * 1.6);
	}
	return false;
}
