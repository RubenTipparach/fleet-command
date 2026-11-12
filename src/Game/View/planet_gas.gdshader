// planet_gas.gdshader: a gas giant's cloud tops (owner, 2026-11-09: "We need some really good in game planet shaders",
// "gas giants with rings"; the planet-shaders skill). The method is godot-sandbox's (every pattern on the sphere's
// direction in model space, warped value noise, no UVs or seams; planet_noise.gdshaderinc); the bands are new here:
//  - belts and zones by latitude (bands of them), each edge torn by two passes of domain warp so the boundaries curl into
//    eddies, and the warp sheared along the latitude so it reads as flow, not as stripes of noise;
//  - differential rotation: each latitude turns at its own rate (alternating jets), so the bands slide past each other;
//  - storms: one great oval (its colour the palette's fourth) at storm_lat, and a scatter of small white ovals on the
//    lattice, each a vortex that twists the bands round it;
//  - the poles hazier and darker, the limb darkened, and the shadow of the rings (pn_ring_density along the sun's ray to
//    the ring plane, the same density the rings draw), so a ringed giant wears its rings' shadow as a band.
// Unshaded and lit by the battle's sun (sun_dir), as the other grounds are, the same in Forward+ and Compatibility.
shader_type spatial;
render_mode unshaded, shadows_disabled, cull_back;

#include "res://src/Game/View/planet_noise.gdshaderinc"

uniform vec3 c0 : source_color = vec3(0.85, 0.70, 0.55);   // the zones (pale)
uniform vec3 c1 : source_color = vec3(0.66, 0.46, 0.31);   // the belts (dark)
uniform vec3 c2 : source_color = vec3(0.95, 0.88, 0.78);   // the brightest bands and the small ovals
uniform vec3 c3 : source_color = vec3(0.75, 0.34, 0.23);   // the great storm
uniform float bands = 7.0;          // belt and zone pairs from pole to pole
uniform float turbulence = 1.0;     // how torn the band edges are
uniform float flow_speed = 0.004;   // the jets' rate, radians a second
uniform float storm_lat = -0.35;    // the great storm's latitude (sine), its size (radians) and longitude
uniform float storm_size = 0.16;
uniform float storm_lon = 0.6;
uniform float ovals = 0.6;          // how many small ovals, 0 to 1
uniform vec3 seed = vec3(0.0);
// the rings, whose shadow falls on the tops (ring_outer 0: no rings), in the planet's radii, in its own equator plane
uniform float ring_inner = 0.0;
uniform float ring_outer = 0.0;
uniform float ring_opacity = 0.0;
uniform float ring_seed = 0.0;
uniform vec3 atmo : source_color = vec3(0.94, 0.82, 0.63);
uniform float atmo_strength = 0.8;
uniform vec3 sun_dir = vec3(0.4, 0.7, 0.6);
uniform vec3 sun_color : source_color = vec3(1.0, 0.96, 0.9);
uniform float sun_energy = 1.0;
uniform float close = 0.0;
uniform float octaves = 6.0;   // the fine flow's octaves, more as the camera comes down (PlanetBody.Sync)

varying vec3 v_obj;

void vertex() {
	v_obj = normalize(VERTEX);
#ifdef PLANET_BAKE
	POSITION = pn_bake_position(UV);
#endif
}

// a vortex at c of radius r: the point turned about c's axis, most at the centre, nothing past its edge
vec3 swirl(vec3 p, vec3 c, float r, float turns) {
	float d = length(p - c) / r;
	float k = turns * exp(-d * d * 2.2);
	return normalize(pn_rot(p, c, k));
}

void fragment() {
	vec3 p = v_obj;
	// the great storm and the small ovals twist the flow round them
	float horiz = sqrt(max(0.0, 1.0 - storm_lat * storm_lat));
	vec3 sc = vec3(horiz * sin(storm_lon), storm_lat, horiz * cos(storm_lon));
	p = swirl(p, sc, storm_size * 1.4, 2.6);
	float oval = 0.0;
	for (int i = 0; i < 18; i++) {
		float fi = float(i);
		if (pn_hash31(vec3(fi, seed.x, 2.2)) > ovals) { continue; }
		vec3 oc = normalize(pn_fib_dir(fi, 18.0) + (pn_hash33(vec3(fi, seed.y, 5.1)) - 0.5) * 0.5);
		oc.y *= 0.8;
		oc = normalize(oc);
		float r = mix(0.025, 0.06, pn_hash31(vec3(fi, seed.z, 8.3)));
		p = swirl(p, oc, r * 1.6, 1.8);
		float d = length(v_obj - oc) / r;
		oval = max(oval, 1.0 - smoothstep(0.55, 1.0, d));
	}
	// each latitude turns at its own rate: the jets alternate with the bands
	float lat = p.y;
	float jet = sin(lat * bands * 3.14159) * flow_speed;
	p = pn_rot(p, vec3(0.0, 1.0, 0.0), TIME * jet);
	// two passes of warp, sheared along the latitude: eddies on the band edges
	vec3 q = vec3(p.x * 2.2, p.y * bands * 1.4, p.z * 2.2) + seed;
	vec3 w1 = vec3(pn_fbm3(q + 1.7), pn_fbm3(q - 4.3), pn_fbm3(q + 8.1)) - 0.5;
	vec3 w2 = vec3(pn_fbm3(q * 2.1 + w1 * 2.0 + 3.3), pn_fbm3(q * 2.1 + w1 * 2.0 - 6.6), 0.0) - 0.5;
	float b = lat * bands + (w1.x * 0.35 + w2.x * 0.22) * turbulence;
	float zone = 0.5 + 0.5 * sin(b * 3.14159);
	float fine = 0.5 + 0.5 * sin(b * 3.14159 * 3.7 + w2.y * 3.0 * turbulence);
	// every band its own shade: a band's place in the palette is drawn from its own number, so no two pairs repeat
	float band_id = floor(b * 2.0 + 0.5);
	float pick = pn_hash31(vec3(band_id, seed.x, 4.0));
	vec3 belt = mix(c1, mix(c1, c3, 0.35), step(0.75, pick));
	vec3 zone_c = mix(c0, c2, smoothstep(0.3, 1.0, pick));
	vec3 col = mix(belt, zone_c, smoothstep(0.2, 0.8, zone));
	col = mix(col, c2, smoothstep(0.7, 1.0, zone) * smoothstep(0.4, 0.9, fine) * 0.55);
	col = mix(col, c1, (1.0 - smoothstep(0.0, 0.6, fine)) * smoothstep(0.3, 0.6, zone) * 0.25);
	// the fine flow: streaks drawn out along the latitude, finer and more of them as the camera comes down
	float streak = pn_fbm_n(vec3(p.x * 6.0, p.y * bands * 9.0, p.z * 6.0) + w2 * 2.5 + seed, octaves);
	col *= 0.82 + 0.36 * streak;
	col *= 0.92 + 0.16 * pn_fbm3(q * 4.0 + w2 * 3.0);
	// the great storm: its own colour in a ring of paler cloud
	float sd = length(v_obj - sc) / storm_size;
	float storm = 1.0 - smoothstep(0.6, 1.0, sd);
	col = mix(col, c2, (1.0 - smoothstep(0.9, 1.25, sd)) * (1.0 - storm) * 0.5);
	col = mix(col, c3 * (0.85 + 0.3 * pn_fbm3(p * 30.0 + seed)), storm);
	col = mix(col, c2, oval * 0.85);
	// hazier, darker poles
	col = mix(col, c1 * 0.75, smoothstep(0.62, 0.95, abs(v_obj.y)) * 0.6);

	vec3 n = normalize(NORMAL);
	vec3 L = pn_sun(normalize((VIEW_MATRIX * vec4(normalize(sun_dir), 0.0)).xyz), n);
	vec3 V = pn_sun(normalize(-VERTEX), n);
	float ndl = dot(n, L);
	float day = pn_day(ndl);
	// the rings' shadow: along the sun's ray from this point to the ring plane (the planet's own equator)
	float ring_shade = 1.0;
	if (ring_outer > 0.0) {
		vec3 Lm = pn_sun(normalize(inverse(mat3(MODEL_MATRIX)) * normalize(sun_dir)), v_obj);
		if (abs(Lm.y) > 1e-3) {
			float t = -v_obj.y / Lm.y;
			if (t > 0.0) {
				vec3 hit = v_obj + Lm * t;
				ring_shade = 1.0 - 0.85 * ring_opacity * pn_ring_density(length(hit.xz), ring_inner, ring_outer, ring_seed);
			}
		}
	}
	float limb = mix(0.55, 1.0, pow(max(dot(n, V), 0.0), 0.45));
	vec3 sun = sun_color * sun_energy;
	vec3 lit = pn_lit(col, day * ring_shade, ndl, 1.0, sun) * limb;
	lit += pn_rim(atmo, atmo_strength, dot(n, V), ndl, close) * sun_energy;
	ALBEDO = lit;
}
