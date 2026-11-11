// planet_noise.gdshaderinc: the noise and the relief every planet shell and asteroid shares (the planet-shaders skill).
// After godot-sandbox (RubenTipparach/godot-sandbox at d5d8e461, resources/shaders/comic_dirt_ground.gdshader,
// comic_cloud_layer.gdshader, comic_crater_overlay.gdshader): every pattern lives on the unit-sphere direction in model
// space, so there are no UVs, no seams and no pinched poles, and a relief is a height field over that direction,
// differenced across a tangent frame into a normal, its step widened to the fragment's own footprint and its slope
// capped. One change: the sandbox's fract-multiply hash is replaced by PCG3D on the float's bits (Jarzynski and Olano,
// "Hash Functions for GPU Rendering", 2020), which CLAUDE.md section 10 asks of every scatter: full avalanche, no lines.

uvec3 pn_pcg3d(uvec3 v) {
	v = v * 1664525u + 1013904223u;
	v.x += v.y * v.z; v.y += v.z * v.x; v.z += v.x * v.y;
	v ^= v >> uvec3(16u);
	v.x += v.y * v.z; v.y += v.z * v.x; v.z += v.x * v.y;
	return v;
}

// a value in [0, 1) for any point (the float's own bits are hashed, so 1.7 and 1.8 differ as much as 1 and 900)
float pn_hash31(vec3 p) { return float(pn_pcg3d(floatBitsToUint(p)).x) * (1.0 / 4294967296.0); }
vec3 pn_hash33(vec3 p) { return vec3(pn_pcg3d(floatBitsToUint(p))) * (1.0 / 4294967296.0); }

// value noise on the integer lattice, smoothstepped
float pn_vnoise3(vec3 p) {
	vec3 i = floor(p);
	vec3 f = fract(p);
	vec3 u = f * f * (3.0 - 2.0 * f);
	float n000 = pn_hash31(i);
	float n100 = pn_hash31(i + vec3(1.0, 0.0, 0.0));
	float n010 = pn_hash31(i + vec3(0.0, 1.0, 0.0));
	float n110 = pn_hash31(i + vec3(1.0, 1.0, 0.0));
	float n001 = pn_hash31(i + vec3(0.0, 0.0, 1.0));
	float n101 = pn_hash31(i + vec3(1.0, 0.0, 1.0));
	float n011 = pn_hash31(i + vec3(0.0, 1.0, 1.0));
	float n111 = pn_hash31(i + vec3(1.0, 1.0, 1.0));
	return mix(mix(mix(n000, n100, u.x), mix(n010, n110, u.x), u.y), mix(mix(n001, n101, u.x), mix(n011, n111, u.x), u.y), u.z);
}

// four octaves, about 0 to 1 (the sandbox's fbm3)
float pn_fbm3(vec3 p) {
	float v = 0.0, amp = 0.55;
	for (int i = 0; i < 4; i++) {
		v += pn_vnoise3(p) * amp;
		p = p * 2.05 + vec3(17.3, 9.1, 4.7);
		amp *= 0.5;
	}
	return v;
}

// n octaves (n up to 9), for the detail that grows as the camera comes down (Falchion's octaves by distance)
float pn_fbm_n(vec3 p, float n) {
	float v = 0.0, amp = 0.55, norm = 0.0;
	for (int i = 0; i < 9; i++) {
		float w = clamp(n - float(i), 0.0, 1.0);
		if (w <= 0.0) { break; }
		v += pn_vnoise3(p) * amp * w;
		norm += amp * w;
		p = p * 2.03 + vec3(17.3, 9.1, 4.7);
		amp *= 0.5;
	}
	return v / max(norm, 1e-4) * 0.9;
}

// Rodrigues: p turned by a radians about the unit axis ax
vec3 pn_rot(vec3 p, vec3 ax, float a) {
	return p * cos(a) + cross(ax, p) * sin(a) + ax * dot(ax, p) * (1.0 - cos(a));
}

// two directions across the sphere at nd (discontinuous at its own poles, which is fine: the gradient they build is the
// same vector whichever perpendicular pair is used)
void pn_frame(vec3 nd, out vec3 ta, out vec3 tb) {
	vec3 pick = abs(nd.y) < 0.999 ? vec3(0.0, 1.0, 0.0) : vec3(1.0, 0.0, 0.0);
	ta = normalize(cross(pick, nd));
	tb = cross(nd, ta);
}

// a relief normal (model space) from a height field's four samples taken eps radians either side along ta and tb,
// its slope capped so no fine feature tips the normal past the horizon
vec3 pn_relief(vec3 nd, vec3 ta, vec3 tb, float ha, float hb, float eps, float strength, float max_slope) {
	vec2 grad = vec2(ha, hb) / (2.0 * eps);
	float g = length(grad);
	if (g > max_slope) { grad *= max_slope / g; }
	return normalize(nd - strength * (grad.x * ta + grad.y * tb));
}

// the evenly spread direction i of n on the sphere (a Fibonacci lattice)
vec3 pn_fib_dir(float i, float n) {
	float k = i + 0.5;
	float phi = acos(clamp(1.0 - 2.0 * k / n, -1.0, 1.0));
	float th = 10.166407 * k;   // pi * (1 + sqrt 5)
	return vec3(cos(th) * sin(phi), cos(phi), sin(th) * sin(phi));
}

// the battle's light on a surface whose normal makes ndl with the sun. Under air (air 1) the terminator is a short
// twilight; on a world with none (air 0) it is a hard edge. Inside it the light falls off with the sun's height, so a
// disc is brightest under the sun and shades toward its night, not one flat lit face. Every layer of a planet calls
// this, so they all turn to night together. It is steepest at the terminator, so a layer with relief gives it the
// slope's own ndl (its relief normal against the sun): there a slope facing the sun is lit and one turned away is in
// night, which is what picks the relief out along the terminator (owner, 2026-11-10, with a photograph of a crescent
// Earth: "terminator line, normal maps? do we have those?"). The line is strong (owner, 2026-11-10: "Stronger
// terminator lines. i would prefer it be done in the shader"): the band is a few degrees wide under air and under one on
// a world with none, and the ground just inside it is already at two fifths of full day, so the fall into night is a
// step, not a long fade.
float pn_day_air(float ndl, float air) {
	float edge = smoothstep(mix(-0.004, -0.025, air), mix(0.02, 0.07, air), ndl);
	return edge * (0.42 + 0.58 * pow(clamp(ndl, 0.0, 1.0), 0.6));
}
float pn_day(float ndl) { return pn_day_air(ndl, 1.0); }

// the sun's colour through the air at the terminator: reddened in the narrow band where its light comes in low (nothing
// on an airless world)
vec3 pn_twilight(float ndl, float air) {
	return mix(vec3(1.0), vec3(1.0, 0.8, 0.62), air * (1.0 - smoothstep(-0.03, 0.06, ndl)));
}

// the night side's fill: a minimal cold light, enough to make out the land, the seas and the weather in the dark and no
// more (owner, 2026-11-10: "keep fill.lights please. Just minimal. But I'd like to somewhat be able to make out
// features"). Linear light: this is a two hundredth of the day and shows as a few percent grey.
const vec3 PN_NIGHT_FILL = vec3(0.0036, 0.0044, 0.006);

// a surface of colour col under a sun: day is its share of the sun's light (pn_day_air, times any shade of its own), over
// the night side's fill
vec3 pn_lit(vec3 col, float day, float ndl, float air, vec3 sun) {
	return col * (PN_NIGHT_FILL + day * pn_twilight(ndl, air)) * sun;
}

// the air's rim seen from afar, on the ground, the cloud tops and a gas giant alike (ndv: the sphere's normal against the
// view; from inside the air, close 1, the shell scatters instead). It follows the day: bright on the lit limb, fading
// through the terminator, and only a trace round the night side (owner, 2026-11-10: "The fresnel on dark sides of
// planets is a little too strong").
vec3 pn_rim(vec3 atmo, float strength, float ndv, float ndl, float close) {
	float limb = pow(1.0 - max(ndv, 0.0), 2.5) * (1.0 - close);
	return atmo * (strength * limb * (0.05 + 0.95 * smoothstep(-0.08, 0.4, ndl)));
}

// A ring system's density at radius r (the planet's radius is 1) between inner and outer: ringlets at three scales, a
// main gap two thirds out and a fainter one, soft edges. Shared by the rings and by the shadow they cast on the planet.
float pn_ring_density(float r, float inner, float outer, float seed) {
	if (r < inner || r > outer) { return 0.0; }
	float t = (r - inner) / (outer - inner);
	float d = 0.55 + 0.45 * pn_vnoise3(vec3(t * 9.0, seed, 0.5));
	d *= 0.6 + 0.4 * pn_vnoise3(vec3(t * 41.0, seed, 3.5));
	d *= 0.75 + 0.25 * pn_vnoise3(vec3(t * 160.0, seed, 7.5));
	d *= 1.0 - 0.92 * exp(-pow((t - 0.66) / 0.022, 2.0));   // the main gap
	d *= 1.0 - 0.6 * exp(-pow((t - 0.28) / 0.012, 2.0));    // a fainter one
	d *= smoothstep(0.0, 0.05, t) * (1.0 - smoothstep(0.9, 1.0, t));
	d *= mix(0.55, 1.0, smoothstep(0.1, 0.5, t));            // thinner toward the planet
	return clamp(d, 0.0, 1.0);
}
