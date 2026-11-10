// nebula_deck.gdshader: a deck of nebula cloud above or below the battle, the larger structure a battle's plumes rise
// between (owner, 2026-11-10: "make a cloud mesh for top and bottom of nebula and fade out the edges, so it looks like the
// nebula columns are connected to a larger structure above and below the map"). One mesh a deck (NebulaDeck), a box drawn
// by its far faces so it draws from inside as well as out; every fragment casts the camera's ray through the deck's slab and
// marches its cloud along it, front to back, in the scene's own units:
// - the shape is a slab: its face toward the battle stands underside off the plane, hangs and lifts by a slow field of the
//   noise (hang) and reaches further down round the root of each column that runs into it (join), so each column runs up
//   into a funnel of the deck; it is whole over the field and thins away to nothing by fade_r (its edge), and is clear
//   round every planet so none is buried in it: the funnels and the edge are DeckField's, baked into field_shape;
// - the cloud is the noise cut by that shape, and each sample lit, by nebula_cloud.gdshaderinc (the plumes' too), in the
//   colours the columns placed in it set (DeckField, baked into field_lit, field_deep and field_glow);
// - the sun reaches a sample through the cloud toward it (a few samples, each twice as far as the last) and the rest of the
//   deck's depth on that side, so the top deck's face toward the battle lies in its own shadow and the bottom deck's is lit;
//   a face in shadow takes a fill of the light the battle's clouds throw back (fill), the shade the columns climb into;
// - far off it fades into the sky behind it (far_from to far_to);
// - a deck thins away as the camera climbs (or sinks) toward it, so a captain who zooms out over the battle looks down
//   through where it was.
// Unshaded and lit by the same sun_dir and sun_energy as the planets and plumes, so Forward+ and Compatibility agree;
// Compatibility takes a third of the steps and no wisps (survey T3). It draws after the planets and before the plumes
// (render priority, NebulaDeck), so a column shows in front of the deck it runs into. The numbers are
// data/space/nebula_decks.json (NebulaDeck).
shader_type spatial;
render_mode unshaded, shadows_disabled, depth_draw_never, depth_test_disabled, blend_mix, cull_front, fog_disabled;

#include "planet_noise.gdshaderinc"
#include "nebula_cloud.gdshaderinc"

#if CURRENT_RENDERER == RENDERER_COMPATIBILITY
const int STEPS = 14;
const int LIGHT_STEPS = 1;
#else
const int STEPS = 40;
const int LIGHT_STEPS = 2;
#endif
uniform sampler3D noise_tex : repeat_enable, filter_linear;
// the field the columns set, baked when they are placed (DeckField): red how far the face reaches down round the root of
// a column that runs into this deck, green how whole the deck is (its edge, clear round the planets); and its colours
uniform sampler2D field_shape : filter_linear, repeat_disable;
uniform sampler2D field_lit : source_color, filter_linear, repeat_disable;
uniform sampler2D field_deep : source_color, filter_linear, repeat_disable;
uniform sampler2D field_glow : source_color, filter_linear, repeat_disable;
uniform sampler2D depth_texture : hint_depth_texture, repeat_disable, filter_nearest;

uniform vec3 sun_dir = vec3(0.4, 0.7, 0.6);   // toward the sun, world space
uniform float sun_energy = 0.65;
uniform float side = 1.0;          // +1 the deck above the plane, -1 the one below
uniform float underside = 30.0;    // the face's mean distance off the plane, units
uniform float thickness = 26.0;    // the deck's depth past it, units
uniform float hang = 7.0;          // how far the face hangs and lifts, units
uniform float join = 12.0;         // how much further it reaches toward the plane round a column's root, units
uniform vec2 centre = vec2(0.0);   // the field's centre on the plane, units
uniform float fade_r = 240.0;      // gone by here, units
uniform float reach = 600.0;       // the farthest a ray marches, units
uniform float coverage = 0.8;
uniform float absorption = 0.45;   // per unit of thick cloud
uniform float base_scale = 0.018;  // repeats of the noise a unit (billows)
uniform float detail_scale = 0.07; // the same for the wisps
uniform float erosion = 0.5;
uniform float contrast = 0.7;      // how far the density varies inside the cloud (nb_vary)
uniform float drift = 0.0015;
uniform float glow_strength = 0.22;
uniform float fill = 0.3;
uniform float column_shade = 0.4;
uniform float far_from = 120.0;    // the deck fades into the sky behind it from here, units from the camera
uniform float far_to = 260.0;      // and is gone by here  // the share of the sun a column's own cloud lets through to its face, 0 to 1          // the light the battle's clouds throw back onto the shadowed face, 0 to 1 of its lit colour
uniform float deck_time = 0.0;     // battle seconds: the cloud holds still while the battle is paused
uniform vec3 seed = vec3(0.0);
uniform vec3 fog_color = vec3(0.0);  // the battle's haze (NebulaDeck.Fog), laid on at the cloud's own depth
uniform float fog_density = 0.0;     // a scene unit

varying vec3 v_world;

void vertex() {
	v_world = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
}

// the baked field at q (DeckField)
vec2 field_uv(vec2 q) {
	return (q - centre) / (2.0 * fade_r) + 0.5;
}

// where the face toward the battle stands off the plane at q: its hang by the noise, and the funnel round the root of each
// column that runs into this deck
float deck_face(vec2 q) {
	float n = textureLod(noise_tex, vec3(q * base_scale * 0.4, 0.31) + seed.yzx, 0.0).r;
	return underside - hang * (2.0 * n - 0.7) - join * textureLod(field_shape, field_uv(q), 0.0).r;
}

// How far inside the deck a point is, 0 to coverage; h is its depth into the deck from the face, 0 to 1.
float deck_cover(vec3 p, out float h) {
	float yd = side * p.y;
	float face = deck_face(p.xz), outer = underside + thickness;
	h = clamp((yd - face) / max(outer - face, 1.0e-3), 0.0, 1.0);
	float v = smoothstep(face, face + thickness * 0.3, yd) * (1.0 - smoothstep(outer - thickness * 0.4, outer, yd));
	return v * textureLod(field_shape, field_uv(p.xz), 0.0).g * coverage;
}

// the noise's coordinates at p: sampled a little askew, so its repeat never lines up with the field, drifting slowly
vec3 deck_q(vec3 p) {
	return vec3(p.x * 0.94 + p.z * 0.34 + deck_time * drift / base_scale, p.y, p.z * 0.94 - p.x * 0.34) * base_scale + seed;
}

void fragment() {
	vec3 ro = CAMERA_POSITION_WORLD;
	vec3 rd = normalize(v_world - ro);
	float seen = 1.0 - smoothstep(0.45, 0.85, side * ro.y / underside);
	if (seen <= 0.0) discard;

	// the ray's stretch inside the slab (the lowest the face hangs to the far side) and the deck's round edge
	float y_in = underside - hang * 1.3 - join, y_out = underside + thickness;
	float oy = side * ro.y, dy = side * rd.y;
	float t0 = 0.0, t1 = reach;
	if (abs(dy) < 1.0e-5) {
		if (oy < y_in || oy > y_out) discard;
	} else {
		float a = (y_in - oy) / dy, b = (y_out - oy) / dy;
		t0 = max(t0, min(a, b));
		t1 = min(t1, max(a, b));
	}
	vec2 o2 = ro.xz - centre;
	float A = dot(rd.xz, rd.xz), B = dot(o2, rd.xz), C = dot(o2, o2) - fade_r * fade_r;
	if (A > 1.0e-8) {
		float h = B * B - A * C;
		if (h < 0.0) discard;
		h = sqrt(h);
		t0 = max(t0, (-B - h) / A);
		t1 = min(t1, (-B + h) / A);
	} else if (C > 0.0) {
		discard;
	}
	t1 = min(t1, nb_solid(depth_texture, SCREEN_UV, INV_PROJECTION_MATRIX));
	if (t1 <= t0) discard;

	float dt = (t1 - t0) / float(STEPS);
	float t = t0 + dt * pn_hash31(vec3(FRAGCOORD.xy, 2.0));
	vec3 sun_l = normalize(sun_dir);
	float sun_k = nb_sun_k(sun_energy);
	float phase = nb_phase(rd, sun_l);
	// toward the sun, in the deck's own sense: + through the deck's far side, - out through its face
	float sun_dy = side * sun_l.y;
	vec3 lit = vec3(0.0);
	float through = 1.0;
	float first = -1.0;   // how far the cloud begins along the ray
	for (int i = 0; i < STEPS; i++) {
		if (t >= t1) break;
		vec3 p = ro + rd * t;
		float h;
		float cover = deck_cover(p, h);
		if (cover > 0.002) {
			vec4 n = textureLod(noise_tex, deck_q(p), 0.0);
			float d = nb_body(n, cover) * nb_vary(textureLod(noise_tex, deck_q(p) * 0.43 + seed.yzx, 0.0), contrast);
#if CURRENT_RENDERER != RENDERER_COMPATIBILITY
			if (d > 0.0) {
				vec3 q2 = p * detail_scale + seed.zxy;
				q2.x += deck_time * drift * 2.5;
				d = nb_erode(d, textureLod(noise_tex, q2, 0.0), erosion);
			}
#endif
			float dens = smoothstep(0.0, 0.28, d);
			if (dens > 0.002) {
				if (first < 0.0) first = t;
				// the cloud between this point and the sun: a few samples near, then the rest of the deck on that side
				float shade = 0.0;
				float step_l = thickness * 0.06;
				vec3 lp = p;
				for (int j = 0; j < LIGHT_STEPS; j++) {
					lp += sun_l * step_l;
					float lh;
					float lc = deck_cover(lp, lh);
					if (lc > 0.002) shade += smoothstep(0.0, 0.28, nb_body(textureLod(noise_tex, deck_q(lp), 0.0), lc)) * step_l;
					step_l *= 2.0;
				}
				float rest = sun_dy > 0.05 ? (1.0 - h) * thickness / sun_dy : (sun_dy < -0.05 ? h * thickness * 0.5 / -sun_dy : thickness);
				shade += 0.35 * coverage * min(rest, thickness * 3.0);
				// the soft edge and the tint as a column reads them (its body before the edge is drawn crisp, and the noise's
				// slow drift), so deck and column take the same colour from the same look
				float soft = smoothstep(0.0, 0.5, d);
				// and the light a column's own cloud keeps from it: a column shades itself through its own depth, a deck's
				// face sees the sun across open space, so the deck takes the column's share (column_shade)
				float sun_t = nb_sun_through(shade, absorption, soft) * column_shade;
				// the colours the columns set here, with their patches (DeckField, baked)
				vec2 fuv = field_uv(p.xz);
				vec3 c_lit = textureLod(field_lit, fuv, 0.0).rgb;
				vec3 c_deep = textureLod(field_deep, fuv, 0.0).rgb;
				vec3 c_glow = textureLod(field_glow, fuv, 0.0).rgb;
				float sunward = sun_dy >= 0.0 ? h : 1.0 - h;
				vec3 s = nb_light(c_deep, c_lit, c_glow, glow_strength, sunward, 1.0, n.g, sun_k, phase, sun_t, soft);
				// where the sun does not reach, the light the battle's clouds throw back
				s += c_lit * fill * sun_k * (1.0 - sun_t) * (1.0 - h);
				float a = 1.0 - exp(-absorption * dens * dt);
				lit += through * a * s;
				through *= 1.0 - a;
				if (through < 0.02) {
					through = 0.0;
					break;
				}
			}
		}
		t += dt;
	}
	// far off, the deck fades into the sky behind it
	float alpha = (1.0 - through) * seen * (1.0 - smoothstep(far_from, far_to, first));
	if (alpha < 0.003) discard;
	ALBEDO = mix(lit / (1.0 - through), fog_color, 1.0 - exp(-max(first, 0.0) * fog_density));
	ALPHA = alpha;
}
