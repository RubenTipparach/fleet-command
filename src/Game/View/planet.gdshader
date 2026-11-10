// planet.gdshader: a planet's ground in the battle, drawn procedurally (owner, 2026-11-08: "In game Planets: pull in planets
// from godot-sandbox and falchion ... Also just zooming down to planets is supper cool"). One shader for every rocky and
// lava world (a gas giant's tops are planet_gas.gdshader); a kind's look is uniforms set from data/space/planets.json
// (PlanetLooks.Apply), never written here.
//
// Ported from Falchion (RubenTipparach/falchion at 5d128af8): src/render/planets/PlanetMaterial.ts (the fragment shader:
// rocky and lava surfaces, seas, caps, clouds, night lights and the relief bump), noise.glsl.ts (Ashima Arts / Stefan
// Gustavson's 3D simplex noise, MIT, with fbm and ridged sums), relief.glsl.ts (craters and volcanoes) and
// atmosphere.glsl.ts (cloud cover). Changes for Fleet Command: Godot's shading language (no #defines: each feature is a
// uniform, off at 0); the noise gains octaves as the camera comes down (octaves, close, set by BattleView's planet view),
// so the ground stays sharp right down to the surface; and the installations a scan has found glow on the ground in their
// state's colour (site_dir, site_col), each a pool of light with a scatter of lit buildings round it. Unshaded and lit
// here by the battle's sun (sun_dir), the same in Forward+ and Compatibility.
//
// Made to read as a real world, not as noise in a palette (owner, 2026-11-10: "make planets more realistic please"):
//  - the land is coloured by its climate where the kind has one (climate): forest where it rains, dry land in the belts a
//    third of the way to the poles and in the rain's gaps, tundra toward the caps, bare rock high up; a world with no sea
//    runs all four of its colours by height and by broad regions (a moon's dark plains and bright highlands);
//  - mountain ranges (mountains) are ridged peaks in belts, and the terrain shades by its slope from afar, not only close;
//  - the sea is dark and deep, pale only on the shelves by a coast, with a tight glint of the sun inside a broad one;
//  - canyons are a few great rifts, each an arc that wanders, widest in its middle and pinched out at its ends (Falchion's
//    iso-lines of noise drew closed worms); an ice shell's long cracks (lineae) are the same arcs, longer and thinner;
//  - the night side's lights are clustered where people would live (lowlands and coasts, in a few regions), small
//    patches from afar that break into points as the camera comes down;
//  - a lava world is a dark crust of plates whose joints glow where the ground is hot, round molten lowlands with
//    floes of crust on them, and its volcanoes run a few broad flows, not rays;
//  - the light falls off toward the terminator, soft and reddened under air, hard on a world with none (pn_day_air),
//    and the land's relief moves its own terminator: a slope toward the sun is lit a little past it, one turned away is
//    in night a little before (terminator_relief), so low light picks the ranges out; the night side is black but for
//    its lights.
shader_type spatial;
render_mode unshaded, shadows_disabled, cull_back;

#include "res://src/Game/View/planet_noise.gdshaderinc"

uniform int surface = 0;   // 0 rocky, 2 lava (PlanetLooks.Surfaces; 1 is a gas giant, planet_gas.gdshader)
uniform vec3 c0 : source_color = vec3(0.04, 0.15, 0.32);
uniform vec3 c1 : source_color = vec3(0.12, 0.44, 0.65);
uniform vec3 c2 : source_color = vec3(0.25, 0.49, 0.22);
uniform vec3 c3 : source_color = vec3(0.55, 0.48, 0.35);
uniform float sea = 0.02;
uniform float ice_lat = 0.8;
uniform float terminator_relief = 0.7;   // how far a slope moves its own terminator (times its turn from the sphere)
uniform float clouds = 0.5;
uniform vec3 cloud_color : source_color = vec3(1.0);
uniform vec3 atmo : source_color = vec3(0.44, 0.71, 1.0);
uniform float atmo_strength = 0.9;
uniform float detail = 1.6;
uniform float craters = 0.0;
uniform float canyons = 0.0;
uniform float mountains = 0.0;
uniform float climate = 0.0;
uniform vec3 arid : source_color = vec3(0.72, 0.58, 0.38);
uniform vec3 tundra : source_color = vec3(0.42, 0.40, 0.33);
uniform float lineae = 0.0;
uniform vec3 lineae_color : source_color = vec3(0.45, 0.30, 0.24);
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
// ridges: 1 on a crest, falling away either side, three octaves (0 to about 0.875)
float ridged(vec3 p) {
	float v = 0.0, a = 0.5;
	for (int i = 0; i < 3; i++) { v += a * (1.0 - abs(snoise(p))); p = p * 2.1 + vec3(4.1, 1.3, 7.7); a *= 0.5; }
	return v;
}

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
				// a lake of lava in the caldera, the ground warm round it, and one or two tongues of lava down the flanks:
				// lobes with ragged edges, each to its own reach, brightest at the vent and cooling to a dull red at the toe
				float lake = 1.0 - smoothstep(0.07, 0.13, d);
				float warm = 0.1 * pow(1.0 - smoothstep(0.08, 0.5, d), 2.0);
				float way = snoise(u * 0.9 + vec3(a)) + 0.22 * snoise(q * 7.0 + vec3(a)) + 0.1 * snoise(q * 19.0 + vec3(a));
				float reach = 0.35 + 0.5 * (snoise(u * 1.7 + vec3(a + 5.0)) * 0.5 + 0.5);
				float t = clamp(d / reach, 0.0, 1.0);
				float flow = smoothstep(0.25, 0.4, way) * smoothstep(0.08, 0.14, d) * (1.0 - smoothstep(0.75, 1.0, t)) * mix(0.9, 0.3, t);
				lava = max(lava, max(lake, max(flow, warm)));
			}
		}
	}
	return vec2(h, lava);
}

// the gap between a point's nearest two cell centres (0 on the joint between two cells): the plates of a cooling crust
float plate_gap(vec3 q) {
	vec3 c = floor(q), f = q - c;
	float d1 = 8.0, d2 = 8.0;
	for (int k = -1; k <= 1; k++) {
		for (int j = -1; j <= 1; j++) {
			for (int i = -1; i <= 1; i++) {
				vec3 o = vec3(float(i), float(j), float(k));
				vec3 r = o + pn_hash33(c + o) - f;
				float d = dot(r, r);
				if (d < d1) { d2 = d1; d1 = d; } else if (d < d2) { d2 = d; }
			}
		}
	}
	return sqrt(d2) - sqrt(d1);
}
// long cracks across the world: count arcs of great circles, each about its own axis and centred on its own point, half
// as long as half_len (radians, drawn per arc between its two ends), as wide at its middle as width (radians) and pinched
// out at its ends; wander moves every arc off its circle (the caller's noise), px is a pixel's size on the sphere (the
// edge is widened by it and the arc thinned to match, so a crack finer than a pixel fades and does not sparkle). 1 in
// the crack's floor, 0 outside it.
float arcs(vec3 d, float wander, float px, int count, float salt, vec2 half_len, vec2 width) {
	float best = 0.0;
	for (int i = 0; i < 16; i++) {
		if (i >= count) { break; }
		vec3 ha = pn_hash33(vec3(float(i), salt, seed.x));
		vec3 hb = pn_hash33(vec3(float(i), salt + 3.0, seed.y));
		vec3 axis = normalize(ha - 0.5);
		vec3 centre = normalize(cross(axis, normalize(hb - 0.5)));
		float t = (1.0 - dot(d, centre)) / (1.0 - cos(mix(half_len.x, half_len.y, hb.x)));
		if (t >= 1.0) { continue; }
		float wd = mix(width.x, width.y, ha.x) * (1.0 - t * t);
		float dist = abs(dot(d, axis) + wander);
		best = max(best, (1.0 - smoothstep(wd * 0.5 - px, wd + px, dist)) * wd / (wd + px));
	}
	return best;
}
// the lit points of a town in one size of cell: a point in about half the cells, 1 at its middle
float town_points(vec3 q) {
	vec3 c = floor(q);
	vec3 r = pn_hash33(c + vec3(17.0));
	vec3 dv = q - c - (0.3 + 0.4 * r);
	float rad = 0.06 + 0.12 * r.z;
	return step(0.5, pn_hash31(c + vec3(3.0))) * exp(-dot(dv, dv) / (rad * rad));
}
// a town's grain: 1 from afar (its points finer than a pixel, so the patch glows evenly), and as the camera comes down
// the points themselves, at three sizes, each size showing once a pixel is finer than its cells
// (px is the fragment's footprint, taken by the caller outside any branch: a derivative taken inside the land's branch
// is undefined along the coast, and drew the coast as a line of lights)
float town_grain(vec3 d, float px) {
	float g = smoothstep(0.25, 0.9, px * 220.0);
	g += (1.0 - smoothstep(0.25, 0.9, px * 220.0)) * town_points(d * 220.0 + seed) * 2.2;
	g += (1.0 - smoothstep(0.25, 0.9, px * 700.0)) * town_points(d * 700.0 + seed.zxy) * 1.8;
	g += (1.0 - smoothstep(0.25, 0.9, px * 2300.0)) * town_points(d * 2300.0 + seed.yzx) * 1.4;
	return g;
}
vec3 ramp4(vec3 a, vec3 b, vec3 c, vec3 d, float t) {
	return t < 0.3333 ? mix(a, b, t * 3.0) : (t < 0.6667 ? mix(b, c, t * 3.0 - 1.0) : mix(c, d, t * 3.0 - 2.0));
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
	float air = step(0.001, atmo_strength);
	float day = pn_day_air(ndl, air);
	float px = length(fwidth(v_obj));
	bool dry = sea < -1.5;   // a world with no sea
	vec3 emissive = vec3(0.0);
	float spec = 0.0;

	// ---- the ground's height: continents (their coasts drawn out by a broad warp), at more octaves as the camera comes
	// down; ranges of ridged peaks in belts on the land; craters, rifts and volcanoes
	vec3 warp = vec3(snoise(p * 0.55 + vec3(31.0)), snoise(p * 0.55 + vec3(57.0)), snoise(p * 0.55 + vec3(83.0)));
	vec3 pw = p + warp * 0.22;
	float h = fbm_n(pw, octaves);
	float h_base = h;
	float mtn = 0.0;
	if (mountains > 0.0) {
		float belt = smoothstep(0.0, 0.45, snoise(pw * 0.7 + vec3(41.0)));
		float shore = dry ? 1.0 : smoothstep(sea - 0.04, sea + 0.14, h);
		mtn = belt * shore * pow(smoothstep(0.48, 0.84, ridged(pw * 2.4 + vec3(13.0))), 1.6);
		h += mountains * 0.3 * mtn;
	}
	float crater = craters > 0.5 ? crater_sum(p, octaves) : 0.0;
	h += crater * CRATER_DEPTH;
	float cut = 0.0, lin = 0.0;
	if (canyons > 0.0 || lineae > 0.0) {
		float wander = 0.035 * snoise(v_obj * 3.1 + seed) + 0.009 * snoise(v_obj * 13.0 + seed.zxy);
		if (canyons > 0.0) {
			// the walls ragged: side canyons bite into the rim
			float rag = 0.006 * snoise(v_obj * 47.0 + seed.yzx);
			cut = arcs(v_obj, wander + rag, px, 3 + int(canyons * 6.0), 23.0, vec2(0.22, 0.6), vec2(0.012, 0.03));
		}
		if (lineae > 0.0) {
			lin = arcs(v_obj, wander * 0.5, px, int(lineae * 16.0), 61.0, vec2(0.5, 1.5), vec2(0.0035, 0.009));
		}
	}
	float cut_depth = canyons * 0.55 * cut;
	h -= cut_depth;
	vec2 volc = volcanoes > 0.5 ? volcano_field(p, seed, detail) : vec2(0.0);
	h += volc.x * VOLCANO_HEIGHT;

	// ---- its colour
	vec3 col;
	float land = 1.0, molten = 0.0;
	float e = dry ? 0.0 : smoothstep(sea, sea + 0.7, h);   // the way up from the shore
	float cold = 0.0;
	if (surface == 2) {
		// a lava world: a crust in the palette by height, darkest in the low ground
		col = ramp4(c0, c1, c2, c3, smoothstep(-0.6, 0.7, h));
		col *= 0.8 + 0.4 * (snoise(p * 5.0) * 0.5 + 0.5);
		// the crust is plates, great ones broken into small, their edges bent off the straight: the joints glow only
		// where the ground is hot (broad regions, and the low ground), wider the hotter; the lowest ground is molten,
		// dark floes of crust on it with bright gaps between. A joint finer than a pixel fades and does not sparkle.
		vec3 bend = vec3(snoise(p * 2.3 + vec3(7.0)), snoise(p * 2.3 + vec3(29.0)), snoise(p * 2.3 + vec3(53.0)));
		vec3 pp = p + warp * 0.3 + bend * 0.12;
		float gap = plate_gap(pp * 3.1);
		float gap_s = plate_gap(pp * 9.7 + vec3(11.0));
		float heat = smoothstep(0.05, 0.6, snoise(p * 0.8 + vec3(5.0)) * 0.7 + 0.3 * warp.z - h * 0.9);
		molten = dry ? 0.0 : 1.0 - smoothstep(sea - 0.03, sea + 0.04, h);
		float pxc = px * detail * 3.1, pxs = px * detail * 9.7;
		float wj = 0.012 + 0.05 * heat * heat;
		float joint = (1.0 - smoothstep(wj * 0.4, wj + pxc, gap)) * smoothstep(0.0, 0.3, heat) * wj / (wj + pxc);
		float fine = (1.0 - smoothstep(0.0, 0.03 + pxs, gap_s)) * heat * heat * 0.6 * 0.03 / (0.03 + pxs);
		float melt = 1.0 - smoothstep(0.02, 0.16 + pxs, gap_s + 0.08 * snoise(p * 9.0));
		float hot = max(max(joint, fine) * (1.0 - molten), molten * mix(0.1, 1.0, melt));
		hot = max(hot, volc.y);
		col = mix(col, c0 * 0.6, molten * (1.0 - melt));
		col = mix(col, c3 * 1.2 + vec3(0.03), clamp(volc.x * 1.2, 0.0, 0.5));
		col = mix(col, glow * 0.35, hot * 0.7);
		// hottest in the middle of a flow: the glow's own colour, toward yellow white at its core
		emissive += glow * hot * 1.4 + vec3(1.0, 0.72, 0.3) * pow(hot, 4.0) * 0.7;
		land = 1.0 - molten;
	} else if (dry) {
		// no sea: all four colours by height and by broad regions (dark plains, bright highlands)
		float region = snoise(p * 0.75 + vec3(47.0));
		col = ramp4(c0, c1, c2, c3, smoothstep(-0.75, 0.75, h_base * 0.85 + region * 0.55 + mtn * 0.15));
		col *= 0.88 + 0.24 * (snoise(p * 6.0) * 0.5 + 0.5);
	} else {
		// land and sea, the coast between them never sharper than a pixel
		float coast = h - sea;
		float aw = fwidth(h) * 0.75 + 0.003;
		land = smoothstep(-aw, aw, coast);
		vec3 ground = mix(c2, c3, e);
		float mottle = snoise(p * 6.0) * 0.5 + 0.5;
		if (climate > 0.0) {
			// rain falls on the equator and the middle latitudes and misses the belts between; it thins inland and uphill,
			// and the biomes' edges are ragged (two finer fields move them)
			float rain = snoise(p * 1.3 + vec3(19.0)) * 0.5 + 0.5;
			float belts = cos(lat * 9.0) * (1.0 - smoothstep(0.7, 0.92, lat));
			float wet = clamp(rain * 0.85 + 0.3 * belts + 0.12 - e * 0.3 + 0.16 * (mottle - 0.5)
				+ 0.09 * snoise(p * 17.0 + vec3(71.0)), 0.0, 1.0);
			vec3 green = mix(c2, c2 * vec3(0.5, 0.68, 0.5), smoothstep(0.5, 0.82, wet));
			vec3 low = mix(arid, green, smoothstep(0.26, 0.5, wet));
			cold = smoothstep(ice_lat - 0.34, ice_lat - 0.03, lat + e * 0.22 + 0.06 * warp.x);
			low = mix(low, tundra, cold * 0.9);
			vec3 clim = mix(low, c3, smoothstep(0.45, 0.9, e + mtn * 0.6));
			ground = mix(ground, clim, climate);
			// a pale strand along the shore
			ground = mix(ground, arid * 1.08, climate * 0.35 * (1.0 - smoothstep(0.0, 0.016, coast)));
		}
		ground *= 0.86 + 0.28 * mottle;
		// the sea: pale only on the shelves by a coast, dark out in the deep, its colour drifting a little
		float depth = max(-coast, 0.0);
		vec3 water = mix(c0, c1, 0.8 * exp(-depth / 0.05));
		water = mix(water, c0 * 0.55, smoothstep(0.15, 0.8, depth));
		water *= 0.9 + 0.2 * mottle;
		col = mix(water, ground, land);
		spec = 1.0 - land;
	}
	if (surface != 2) {
		// craters by their relief, a little by their albedo: a fresh rim is paler, a floor a shade darker
		col *= 1.0 + clamp(crater * 0.9, -0.16, 0.3);
		col = mix(col, c3 * 1.25 + vec3(0.04), clamp(volc.x * 1.2, 0.0, 0.6));
		emissive += glow * volc.y * 1.8;
		col = mix(col, glow * 0.5, volc.y * 0.6);
	}
	// the ground's own grain, once the camera is close enough to see it
	if (close > 0.0 && surface != 2) {
		col *= 1.0 + close * land * 0.2 * (0.6 * snoise(p * 43.0 + vec3(3.0)) + 0.4 * snoise(p * 131.0 + vec3(9.0)));
	}
	col *= 1.0 - cut * 0.32;
	col = mix(col, lineae_color, lin * 0.75);
	// the caps, their edge broken by the terrain; snow on the peaks where it is cold enough
	float caps = smoothstep(ice_lat, ice_lat + 0.05, lat + h_base * 0.1 + 0.05 * warp.y);
	float peaks = ice_lat > 1.0 ? 0.0 : smoothstep(0.55, 0.9, mtn) * smoothstep(ice_lat - 0.75, ice_lat - 0.2, lat + mtn * 0.35);
	float snow = max(caps, peaks);
	col = mix(col, vec3(0.93, 0.96, 1.0), snow * (1.0 - molten));
	spec *= 1.0 - caps * 0.85;
	// the night side's lights: where people would live (a few regions, the low ground, the coasts, never the ice), in
	// patches that are small from afar and break into points as the camera comes down
	if (cities > 0.0 && land > 0.5) {
		float night = 1.0 - smoothstep(-0.2, 0.02, ndl);
		if (night > 0.0) {
			float region = smoothstep(0.3, 0.72, snoise(p * 1.9 + vec3(3.0)) * 0.5 + 0.5);
			float low = 1.0 - smoothstep(0.04, 0.5, h - (dry ? -0.3 : sea));
			float metro = smoothstep(0.5, 0.82, snoise(p * 6.5 + vec3(9.0)) * 0.5 + 0.5);
			float urban = 0.45 * snoise(p * 23.0 + vec3(5.0)) + 0.55 * snoise(p * 71.0 + vec3(8.0));
			float lived = (0.2 + 0.8 * region) * low * (0.1 + 0.9 * metro) * (1.0 - snow) * (1.0 - cold * 0.8);
			float patch_k = smoothstep(0.72 - 0.34 * min(1.0, cities * 2.5) * lived, 0.86, urban * 0.5 + 0.5) * lived;
			emissive += city_glow * patch_k * town_grain(v_obj, px) * smoothstep(0.5, 1.0, land) * night * 1.9 * min(1.0, cities * 4.0);
		}
	}
	// painted clouds (a kind with no cloud layer of its own): they drift slowly
	float cloud = 0.0;
	if (clouds > 0.0) {
		cloud = smoothstep(0.05, 0.55, fbm(v_obj * (detail * 1.4) + seed.zxy + vec3(TIME * 0.015, 0.0, TIME * 0.008))) * clouds;
		col = mix(col, cloud_color, cloud);
		spec *= 1.0 - cloud;
		emissive *= 1.0 - cloud * 0.5;
	}

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
		float lit = step(0.9, pn_hash31(floor(q) + vec3(float(i) * 7.0))) * exp(-ang2 / (rad * rad * 6.0));
		lit *= 1.0 - smoothstep(0.3, 1.0, length(fwidth(q)));
		emissive += sc.rgb * sc.a * (pool * 0.9 + lit * 0.8) * (1.0 - cloud * 0.6);
	}

	// relief: the height shades the ground by its screen-space slope (Falchion's bump). The landforms at their full
	// height, the broad terrain at a share of it (more up close), so ranges and basins read from afar
	float h_rel = crater * CRATER_DEPTH - cut_depth + volc.x * VOLCANO_HEIGHT + mountains * 0.3 * mtn + lin * 0.05
		+ (0.2 + 0.3 * close) * h_base;
	float relief = 1.0;
	float day_land = day;
	vec3 dpx = dFdx(VERTEX), dpy = dFdy(VERTEX);
	float R = length(dpx) / max(length(dFdx(v_obj)), 1e-7);
	float bh = h_rel * 0.045 * R;
	vec3 r1 = cross(dpy, n), r2 = cross(n, dpx);
	float det = dot(dpx, r1);
	vec3 grad = sign(det) * (dFdx(bh) * r1 + dFdy(bh) * r2);
	float sharp = 1.0 - smoothstep(0.15, 0.4, fwidth(h_rel));
	if (land > 0.0) {
		// (eased in over the coast with the land itself, so the shore is not a line where the relief switches on)
		vec3 nb = normalize(abs(det) * n - grad);
		float turn = (dot(nb, L) - ndl) * sharp * land;
		relief = clamp(1.0 + turn * 1.8, 0.35, 1.5);
		// and the day itself taken on the slope: along the terminator a face toward the sun is lit, one turned away is night
		// (by no more than a slope can: a ridge catches the sun a little past the terminator, never deep in the night)
		day_land = pn_day_air(ndl + clamp(turn * terminator_relief, -0.1, 0.08), air);
	}
	// the sun on the sea: a tight glint inside a broad sheen
	float ndh = max(dot(n, normalize(L + V)), 0.0);
	float sp = (pow(ndh, 200.0) * 0.45 + pow(ndh, 24.0) * 0.09) * spec * day;
	vec3 sun = sun_color * sun_energy;
	vec3 lit_col = pn_lit(col, day_land * relief, ndl, air, sun) + sp * sun * pn_twilight(ndl, air) + emissive;
	// the air's rim, seen from afar (from inside the air, coming down, the shell scatters instead)
	float rim = pow(1.0 - max(dot(n, V), 0.0), 2.5) * (1.0 - close);
	lit_col += atmo * rim * atmo_strength * (0.15 + 0.85 * smoothstep(-0.3, 0.6, ndl)) * sun_energy;
	ALBEDO = lit_col;
}
