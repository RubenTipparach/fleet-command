// planet.gdshader: a planet's ground in the battle, drawn procedurally (owner, 2026-11-08: "In game Planets: pull in planets
// from godot-sandbox and falchion ... Also just zooming down to planets is supper cool"). One shader for every kind of world;
// a kind's look is uniforms set from data/space/planets.json (PlanetLooks.Apply), never written here.
//
// Ported from Falchion (RubenTipparach/falchion at 5d128af8): src/render/planets/PlanetMaterial.ts (the fragment shader:
// rocky, gas and lava surfaces, seas, caps, clouds, night lights and the relief bump), noise.glsl.ts (Ashima Arts / Stefan
// Gustavson's 3D simplex noise, MIT, with fbm and ridged sums), relief.glsl.ts (craters, canyons and volcanoes) and
// atmosphere.glsl.ts (cloud cover). Changes for Fleet Command: Godot's shading language (no #defines: each feature is a
// uniform, off at 0); the night lights are procedural on the land (Falchion bakes a light map from its settlements); the
// noise gains octaves as the camera comes down (octaves, close, set by BattleView's planet view), so the ground stays
// sharp right down to the surface; and the installations a scan has found glow on the ground in their state's colour
// (site_dir, site_col), each a pool of light with a scatter of lit buildings round it, the close-up's settlements in
// small. Unshaded and lit here by the battle's sun (sun_dir), the same in Forward+ and Compatibility.
shader_type spatial;
render_mode unshaded, shadows_disabled, cull_back;

uniform int surface = 0;   // 0 rocky, 1 gas, 2 lava (PlanetLooks.Surfaces)
uniform vec3 c0 : source_color = vec3(0.04, 0.15, 0.32);
uniform vec3 c1 : source_color = vec3(0.12, 0.44, 0.65);
uniform vec3 c2 : source_color = vec3(0.25, 0.49, 0.22);
uniform vec3 c3 : source_color = vec3(0.55, 0.48, 0.35);
uniform float sea = 0.02;
uniform float ice_lat = 0.8;
uniform float clouds = 0.5;
uniform vec3 cloud_color : source_color = vec3(1.0);
uniform vec3 atmo : source_color = vec3(0.44, 0.71, 1.0);
uniform float atmo_strength = 0.9;
uniform float detail = 1.6;
uniform float craters = 0.0;
uniform float canyons = 0.0;
uniform vec3 glow : source_color = vec3(1.0, 0.8, 0.48);
uniform vec3 city_glow : source_color = vec3(1.0, 0.8, 0.48);
uniform float cities = 0.0;
uniform float volcanoes = 0.0;
uniform vec3 seed = vec3(0.0);
// the battle's sun: the direction toward it (world space), its colour and strength
uniform vec3 sun_dir = vec3(0.4, 0.7, 0.6);
uniform vec3 sun_color : source_color = vec3(1.0, 0.96, 0.9);
uniform float sun_energy = 1.0;
// the planet view: octaves of the terrain noise (6 from afar, more coming down) and how close the camera is (0 to 1)
uniform float octaves = 6.0;
uniform float close = 0.0;
// the installations shown: a unit direction in the planet's frame and the pool's angular radius (w, radians), and the
// colour with its strength (a; 0 for a slot not in use)
uniform vec4 site_dir[8];
uniform vec4 site_col[8];

varying vec3 v_obj;

// ---- Noise (Falchion's noise.glsl.ts: Ashima Arts / Stefan Gustavson, MIT) -------------------------------------------
float m289f(float x) { return x - floor((x + 0.5) * (1.0 / 289.0)) * 289.0; }
vec4 m289v4(vec4 x) { return x - floor((x + 0.5) * (1.0 / 289.0)) * 289.0; }
vec3 m289v3(vec3 x) { return x - floor((x + 0.5) * (1.0 / 289.0)) * 289.0; }
vec4 permute(vec4 x) { return m289v4(m289v4(x * 34.0 + 1.0) * x); }
vec4 taylor_inv_sqrt(vec4 r) { return 1.79284291400159 - 0.85373472095314 * r; }
float snoise(vec3 v) {
	vec2 C = vec2(1.0 / 6.0, 1.0 / 3.0);
	vec3 i = floor(v + dot(v, C.yyy));
	vec3 x0 = v - i + dot(i, C.xxx);
	vec3 g = step(x0.yzx, x0.xyz);
	vec3 l = 1.0 - g;
	vec3 i1 = min(g.xyz, l.zxy);
	vec3 i2 = max(g.xyz, l.zxy);
	vec3 x1 = x0 - i1 + C.xxx;
	vec3 x2 = x0 - i2 + C.yyy;
	vec3 x3 = x0 - 0.5;
	i = m289v3(i);
	vec4 p = permute(permute(permute(i.z + vec4(0.0, i1.z, i2.z, 1.0)) + i.y + vec4(0.0, i1.y, i2.y, 1.0)) + i.x + vec4(0.0, i1.x, i2.x, 1.0));
	vec4 j = p - 49.0 * floor((p + 0.5) * (1.0 / 49.0));
	vec4 x_ = floor((j + 0.5) * (1.0 / 7.0));
	vec4 y_ = floor(j - 7.0 * x_);
	vec4 x = x_ * (2.0 / 7.0) + (0.5 / 7.0 - 1.0);
	vec4 y = y_ * (2.0 / 7.0) + (0.5 / 7.0 - 1.0);
	vec4 h = 1.0 - abs(x) - abs(y);
	vec4 b0 = vec4(x.xy, y.xy);
	vec4 b1 = vec4(x.zw, y.zw);
	vec4 s0 = floor(b0) * 2.0 + 1.0;
	vec4 s1 = floor(b1) * 2.0 + 1.0;
	vec4 sh = -step(h, vec4(0.05));
	vec4 a0 = b0.xzyw + s0.xzyw * sh.xxyy;
	vec4 a1 = b1.xzyw + s1.xzyw * sh.zzww;
	vec3 p0 = vec3(a0.xy, h.x);
	vec3 p1 = vec3(a0.zw, h.y);
	vec3 p2 = vec3(a1.xy, h.z);
	vec3 p3 = vec3(a1.zw, h.w);
	vec4 norm = taylor_inv_sqrt(vec4(dot(p0, p0), dot(p1, p1), dot(p2, p2), dot(p3, p3)));
	p0 *= norm.x; p1 *= norm.y; p2 *= norm.z; p3 *= norm.w;
	vec4 m = max(0.6 - vec4(dot(x0, x0), dot(x1, x1), dot(x2, x2), dot(x3, x3)), 0.0);
	m = m * m;
	return 42.0 * dot(m * m, vec4(dot(p0, x0), dot(p1, x1), dot(p2, x2), dot(p3, x3)));
}
float fbm(vec3 p) {
	float v = 0.0, a = 0.5;
	for (int i = 0; i < 6; i++) { v += a * snoise(p); p = p * 2.02 + vec3(1.7, 9.2, 3.1); a *= 0.5; }
	return v;
}
// fbm with a fractional octave count (the close-up adds octaves as it comes down)
float fbm_n(vec3 p, float oct) {
	float v = 0.0, a = 0.5;
	for (int i = 0; i < 11; i++) {
		float w = clamp(oct - float(i), 0.0, 1.0);
		if (w <= 0.0) { break; }
		v += a * w * snoise(p);
		p = p * 2.02 + vec3(1.7, 9.2, 3.1);
		a *= i < 6 ? 0.5 : 0.62;
	}
	return v;
}
float ridged(vec3 p) {
	float v = 0.0, a = 0.5;
	for (int i = 0; i < 4; i++) { v += a * (1.0 - abs(snoise(p))); p *= 2.1; a *= 0.5; }
	return v;
}
float hash13(vec3 p) { p = fract(p * 0.1031); p += dot(p, p.zyx + 31.32); return fract((p.x + p.y) * p.z); }

// ---- Landforms (Falchion's relief.glsl.ts) ---------------------------------------------------------------------------
float r_perm(float x) { return m289f(m289f(x * 34.0 + 1.0) * x); }
float r_cell(vec3 c, float salt) { return r_perm(m289f(c.x + salt) + r_perm(m289f(c.y) + r_perm(m289f(c.z)))); }
bool r_feature(vec3 c, float salt, float density, out vec4 f, out float h) {
	float a = r_cell(c, salt);
	h = a;
	f = vec4(0.0);
	if (a / 289.0 > density) { return false; }
	float b = r_perm(a + 1.0), cc = r_perm(b + 1.0), d = r_perm(cc + 1.0), e = r_perm(d + 1.0);
	f = vec4(c + 0.2 + 0.6 * vec3(b, cc, d) / 289.0, 0.16 + 0.28 * e / 289.0);
	return true;
}
float crater_layer(vec3 q, float salt, float density) {
	vec3 fq = floor(q);
	vec3 t = q - fq;
	ivec3 lo = ivec3(mix(vec3(-1.0), vec3(0.0), step(0.55, t)));
	ivec3 hi = ivec3(mix(vec3(0.0), vec3(1.0), step(0.45, t)));
	float sum = 0.0;
	for (int k = lo.z; k <= hi.z; k++) {
		for (int j = lo.y; j <= hi.y; j++) {
			for (int i = lo.x; i <= hi.x; i++) {
				vec4 f; float hh;
				if (!r_feature(fq + vec3(float(i), float(j), float(k)), salt, density, f, hh)) { continue; }
				float d = length(q - f.xyz) / f.w;
				if (d > 1.7) { continue; }
				float bowl = d < 1.0 ? d * d - 1.0 : 0.0;
				float rim = exp(-pow((d - 1.0) / 0.24, 2.0)) * 0.32;
				sum += f.w * (bowl + rim);
			}
		}
	}
	return sum;
}
float crater_sum(vec3 p, float oct) {
	float h = 0.75 * crater_layer(p * 1.3, 11.0, 0.5);
	h += 0.45 * crater_layer(p * 3.1, 37.0, 0.65);
	h += 0.26 * crater_layer(p * 7.3, 71.0, 0.8);
	float w4 = clamp(oct - 8.0, 0.0, 1.0);
	if (w4 > 0.0) { h += w4 * 0.12 * crater_layer(p * 17.0, 113.0, 0.85); }
	float w5 = clamp(oct - 10.0, 0.0, 1.0);
	if (w5 > 0.0) { h += w5 * 0.05 * crater_layer(p * 41.0, 151.0, 0.9); }
	return h;
}
float canyon_cut(vec3 p, float strength) {
	float wide = clamp(strength - 1.0, 0.0, 1.0);
	vec3 w = vec3(snoise(p * 0.7 + vec3(11.0)), snoise(p * 0.7 + vec3(23.0)), snoise(p * 0.7 + vec3(37.0)));
	float valley = 1.0 - smoothstep(0.0, 0.2 + 0.1 * wide, abs(snoise(p * 0.9 + w * 0.6)));
	float tribs = 1.0 - smoothstep(0.0, 0.11, abs(snoise(p * 2.3 + w * 0.8 + vec3(5.0))));
	float mask = smoothstep(-0.25 - wide, 0.3 - wide, snoise(p * 0.45 + vec3(71.0)));
	return max(valley, tribs * 0.55) * mask;
}
float canyon_depth(float cut, float strength) { return strength * (0.55 * cut + 0.35 * smoothstep(0.45, 0.9, cut)); }
vec2 volcano_field(vec3 p, vec3 c, float rad) {
	vec3 q = p * 0.9;
	vec3 fq = floor(q);
	vec3 cq = c * 0.9;
	float R = rad * 0.9;
	float h = 0.0, lava = 0.0;
	for (int k = -1; k <= 1; k++) {
		for (int j = -1; j <= 1; j++) {
			for (int i = -1; i <= 1; i++) {
				vec4 f; float a;
				if (!r_feature(fq + vec3(float(i), float(j), float(k)), 197.0, 0.6, f, a)) { continue; }
				vec3 o = f.xyz - cq;
				float ol = length(o);
				if (abs(ol - R) > 0.5) { continue; }
				float r = f.w * 1.8;
				vec3 dv = q - (cq + o * (R / ol));
				float len = length(dv);
				float d = len / r;
				if (d >= 1.0) { continue; }
				float cone = pow(1.0 - d, 1.4);
				float caldera = 1.0 - smoothstep(0.1, 0.2, d);
				h += r * (1.4 * cone - 0.45 * caldera);
				vec3 u = dv / max(len, 1e-6);
				float ring = max(exp(-pow((d - 0.17) / 0.06, 2.0)), 0.8 * (1.0 - smoothstep(0.08, 0.12, d)));
				float river = (1.0 - smoothstep(0.0, 0.07, abs(snoise(vec3(u.x * 4.0 + a, u.y * 4.0, u.z * 4.0))))) * smoothstep(0.12, 0.2, d) * (1.0 - smoothstep(0.35, 0.9, d));
				lava = max(lava, max(ring, river * 0.85));
			}
		}
	}
	return vec2(h, lava);
}

// Falchion's CRATER_DEPTH and VOLCANO_HEIGHT (src/core/surface/Terrain.ts)
const float CRATER_DEPTH = 0.32;
const float VOLCANO_HEIGHT = 0.6;

void vertex() {
	v_obj = normalize(VERTEX);
}

void fragment() {
	vec3 n = normalize(NORMAL);
	vec3 L = normalize((VIEW_MATRIX * vec4(normalize(sun_dir), 0.0)).xyz);
	vec3 V = normalize(-VERTEX);
	vec3 p = v_obj * detail + seed;
	float lat = abs(v_obj.y);
	float ndl = dot(n, L);
	float day = smoothstep(-0.12, 0.35, ndl);
	vec3 col = vec3(0.0);
	vec3 emissive = vec3(0.0);
	float spec = 0.0;
	float h_rel = 0.0;
	float land = 0.0;
	float h = 0.0;

	if (surface == 1) {
		// a gas giant: warped latitude bands with a storm
		float warp = fbm(vec3(v_obj.x * 2.0, v_obj.y * 9.0, v_obj.z * 2.0) + seed + vec3(TIME * 0.01, 0.0, 0.0));
		float b = v_obj.y * 7.0 + warp * 1.6;
		float t1 = 0.5 + 0.5 * sin(b * 3.1);
		float t2 = 0.5 + 0.5 * sin(b * 7.3 + 1.7);
		col = mix(c0, c1, t1);
		col = mix(col, c2, smoothstep(0.55, 0.95, t2) * 0.7);
		vec3 storm_c = normalize(vec3(0.7, -0.35, 0.6));
		float storm = smoothstep(0.22, 0.0, distance(v_obj, storm_c) + snoise(p * 4.0) * 0.04);
		col = mix(col, c3, storm * 0.85);
	} else {
		// the ground's height, at more octaves as the camera comes down
		h = fbm_n(p, octaves);
		float crater = craters > 0.5 ? crater_sum(p, octaves) : 0.0;
		h += crater * CRATER_DEPTH;
		float cut = canyons > 0.0 ? canyon_cut(p, canyons) : 0.0;
		h -= canyon_depth(cut, canyons);
		vec2 volc = volcanoes > 0.5 ? volcano_field(p, seed, detail) : vec2(0.0);
		h += volc.x * VOLCANO_HEIGHT;
		h_rel = crater * CRATER_DEPTH - canyon_depth(cut, canyons) + volc.x * VOLCANO_HEIGHT;
		if (h < sea) {
			col = mix(c0, c1, smoothstep(sea - 0.45, sea, h));
			spec = 1.0;
		} else {
			land = 1.0;
			float e = smoothstep(sea, sea + 0.7, h);
			col = mix(c2, c3, e);
			col *= 0.85 + 0.3 * snoise(p * 6.0) * 0.5;
		}
		// craters read by their albedo: dark floors, bright rims
		col *= 1.0 + clamp(crater * 1.6, -0.4, 0.35);
		col = mix(col, c3 * 1.25 + vec3(0.04), clamp(volc.x * 1.2, 0.0, 0.6));
		col *= 1.0 - cut * (0.3 + 0.12 * sin(h * 40.0));
		emissive += glow * volc.y * 1.8;
		col = mix(col, glow * 0.5, volc.y * 0.6);
		if (surface == 2) {
			float cracks = smoothstep(0.82, 0.97, ridged(p * 1.3 + vec3(0.0, TIME * 0.01, 0.0)));
			emissive += glow * cracks * 2.2;
			col = mix(col, glow * 0.4, cracks * 0.5);
		}
		float caps = smoothstep(ice_lat, ice_lat + 0.06, lat + h * 0.12);
		col = mix(col, vec3(0.93, 0.96, 1.0), caps);
		spec *= 1.0 - caps;
		// the night side's lights: towns on the land, in clusters (Falchion bakes these from its settlements)
		float region = smoothstep(0.1, 0.55, snoise(p * 2.3 + vec3(3.0)) * 0.5 + 0.5);
		float towns = smoothstep(0.62, 0.9, snoise(p * 38.0 + vec3(9.0)) * 0.5 + 0.5);
		float lights = cities * region * towns * land * (1.0 - caps);
		col = mix(col, vec3(0.42, 0.42, 0.4), lights * 0.35);
		emissive += city_glow * lights * (1.0 - smoothstep(-0.18, 0.08, ndl)) * 1.4;
		// up close the fine terrain shades too
		h_rel += close * h * 0.5;
	}
	// clouds drift slowly
	float cloud = surface == 1 ? 0.0 : smoothstep(0.05, 0.55, fbm(v_obj * (detail * 1.4) + seed.zxy + vec3(TIME * 0.015, 0.0, TIME * 0.008))) * clouds;
	col = mix(col, cloud_color, cloud);
	spec *= 1.0 - cloud;
	emissive *= 1.0 - cloud * 0.5;

	// the installations a scan has found: a pool of light in the state's colour, and lit buildings scattered round it
	for (int i = 0; i < 8; i++) {
		vec4 sc = site_col[i];
		if (sc.a <= 0.0) { continue; }
		vec4 sd = site_dir[i];
		float ang2 = max(0.0, 2.0 * (1.0 - dot(v_obj, sd.xyz)));
		float rad = sd.w;
		float pool = exp(-ang2 / (rad * rad));
		// the buildings: small lit cells, faded where they would be finer than a pixel
		vec3 q = v_obj / (rad * 0.05);
		float lit = step(0.9, hash13(floor(q) + vec3(float(i) * 7.0))) * exp(-ang2 / (rad * rad * 6.0));
		lit *= 1.0 - smoothstep(0.3, 1.0, length(fwidth(q)));
		emissive += sc.rgb * sc.a * (pool * 0.9 + lit * 0.8) * (1.0 - cloud * 0.6);
	}

	// relief: the height shades the ground by its screen-space slope (Falchion's bump)
	float relief = 1.0;
	vec3 dpx = dFdx(VERTEX), dpy = dFdy(VERTEX);
	float R = length(dpx) / max(length(dFdx(v_obj)), 1e-7);
	float bh = h_rel * 0.045 * R;
	vec3 r1 = cross(dpy, n), r2 = cross(n, dpx);
	float det = dot(dpx, r1);
	vec3 grad = sign(det) * (dFdx(bh) * r1 + dFdy(bh) * r2);
	float sharp = 1.0 - smoothstep(0.15, 0.4, fwidth(h_rel));
	if (surface != 1 && spec < 0.5) {
		vec3 nb = normalize(abs(det) * n - grad);
		relief = mix(1.0, clamp(1.0 + (dot(nb, L) - ndl) * 1.8, 0.35, 1.5), sharp);
	}
	vec3 H = normalize(L + V);
	float sp = pow(max(dot(n, H), 0.0), 40.0) * spec * day;
	vec3 sun = sun_color * sun_energy;
	vec3 lit_col = col * (0.03 + 0.97 * day * relief) * sun + sp * 0.6 * sun + emissive;
	// the air's rim, seen from afar (from inside the air, coming down, the shell scatters instead)
	float rim = pow(1.0 - max(dot(n, V), 0.0), 2.5) * (1.0 - close);
	lit_col += atmo * rim * atmo_strength * (0.15 + 0.85 * smoothstep(-0.3, 0.6, ndl)) * sun_energy;
	ALBEDO = lit_col;
}
