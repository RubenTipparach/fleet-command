// nebula_plume.gdshader: a nebula plume, a standing column of cloud marched through a 3D noise texture (owner, 2026-11-10:
// "something like a 3d noise texture would be cool? something like how Ace Combat 7 does clouds, entering a column of
// nebula plume can hide you from enemies", "some nebulas are ion storms, which will have cool lightning"). One mesh a plume
// (NebulaPlume), a bounding column drawn by its far faces so it draws from inside as well as out; every fragment casts the
// camera's ray through the column and marches the cloud along it, front to back:
// - the shape is a column of towers (NebulaPlume.Cover: a stem that narrows below the battle plane, a waist, a head that
//   spreads, towers of different heights that round over, a foot that hangs to different depths, leaning and lobed so no
//   two plumes stand alike), baked into a small 3D texture a plume, and exactly the rule's circle on the plane (radius 1
//   in the mesh's units is NebulaDef.RM: a ship the picture shows inside is inside);
// - the cloud is the noise's Perlin-Worley channel cut by that shape (solid in the heart, billows at the edge), its edges
//   eaten by the Worley channels at a finer scale, the way real-time cloud renderers do (Schneider and Vos, "The Real-time
//   Volumetric Cloudscapes of Horizon Zero Dawn", SIGGRAPH 2015; the texture is tools/space/plume_noise.py's);
// - each sample is lit by the battle's one sun through the cloud between (a few samples toward it, each twice as far as
//   the last; Beer's law with a slower fall for light scattered many times), by the cloud's own dim light, by the glow
//   of its thin edges, and in an ion storm by up to four lightning flashes inside it;
// - the march strides through clear space and walks the cloud in fine steps, its skin in half steps, from a start
//   jittered a step by a PCG hash of the pixel; the cloud thins toward the camera (clear), so a captain inside a plume
//   still sees their own ship;
// - the march stops at whatever solid stands in the cloud (the depth texture), so a ship deep in a plume fades into it and
//   a ship near the camera shows through the little cloud between.
// Unshaded and lit by the same sun_dir and sun_energy as the planets (planet-shaders skill, rule 6), so Forward+ and
// Compatibility agree; Compatibility takes a third of the steps and no wisps (survey T3: a 2022 phone at 60 frames a second).
// It draws after the planets' layers and before every glow, beam and shield (render priority, NebulaPlume), so those show
// in the cloud and it shows over a planet behind it. The look's numbers are data/space/plumes.json (PlumeLooks).
shader_type spatial;
render_mode unshaded, shadows_disabled, depth_draw_never, depth_test_disabled, blend_mix, cull_front;

#include "planet_noise.gdshaderinc"

#if CURRENT_RENDERER == RENDERER_COMPATIBILITY
const int STEPS = 44;
const int LIGHT_STEPS = 2;
const float LIGHT_REACH = 0.1;  // radii: the first step toward the sun; each one after is twice the last
const float FINE = 0.07;        // radii a step inside the cloud
#else
const int STEPS = 128;
const int LIGHT_STEPS = 3;
const float LIGHT_REACH = 0.07;
const float FINE = 0.028;
#endif
const float COARSE = 4.0;       // steps in clear space are this many fine ones
const int FLASHES = 4;

uniform sampler3D noise_tex : repeat_enable, filter_linear;
uniform sampler3D cover_tex : repeat_disable, filter_linear;
uniform sampler2D depth_texture : hint_depth_texture, repeat_disable, filter_nearest;

uniform vec3 sun_dir = vec3(0.4, 0.7, 0.6);   // toward the sun, world space
uniform float sun_energy = 0.65;
uniform vec3 color_lit : source_color = vec3(0.72, 0.63, 0.95);
uniform vec3 color_deep : source_color = vec3(0.13, 0.07, 0.27);
uniform vec3 color_glow : source_color = vec3(0.48, 0.27, 0.94);
uniform float glow_strength = 0.5;
uniform float density = 1.0;        // the plume's own (NebulaDef.Density)
uniform float top = 1.2;            // the column's height over the plane, radii
uniform float bottom = -0.45;       // how far it hangs below the plane, radii (NebulaPlume.Bottom)
uniform float bound = 2.1;          // the bounding column's radius, radii (NebulaPlume.Bound)
uniform float clear = 0.8;          // the cloud thins from here in to the camera, radii
uniform float absorption = 2.4;
uniform float base_scale = 0.85;
uniform float detail_scale = 3.4;
uniform float erosion = 0.38;
uniform float drift = 0.006;
uniform vec3 seed = vec3(0.0);
uniform float plume_time = 0.0;     // battle seconds: the cloud holds still while the battle is paused
uniform vec4 flash[4];              // lightning: where (the mesh's units) and how bright now
uniform vec3 flash_color : source_color = vec3(0.8, 0.88, 1.0);

varying vec3 v_local;
varying vec3 v_cam;
varying float v_scale;

void vertex() {
	v_local = VERTEX;
	v_cam = (inverse(MODEL_MATRIX) * vec4(CAMERA_POSITION_WORLD, 1.0)).xyz;
	v_scale = length(MODEL_MATRIX[0].xyz);
}

// The ray's stretch inside the bounding column (its radius, and bottom to top); false when it misses.
bool plume_bounds(vec3 ro, vec3 rd, out float t0, out float t1) {
	t0 = -1.0e9;
	t1 = 1.0e9;
	float a = dot(rd.xz, rd.xz);
	float b = dot(ro.xz, rd.xz);
	float c = dot(ro.xz, ro.xz) - bound * bound;
	if (a > 1.0e-8) {
		float h = b * b - a * c;
		if (h < 0.0) return false;
		h = sqrt(h);
		t0 = (-b - h) / a;
		t1 = (-b + h) / a;
	} else if (c > 0.0) {
		return false;
	}
	if (abs(rd.y) > 1.0e-6) {
		float ya = (bottom - ro.y) / rd.y, yb = (top - ro.y) / rd.y;
		t0 = max(t0, min(ya, yb));
		t1 = min(t1, max(ya, yb));
	} else if (ro.y < bottom || ro.y > top) {
		return false;
	}
	return t1 > max(t0, 0.0);
}

// How far inside the column a point is: 0 outside, 1 in the heart, soft between over half a radius, so the billows stand
// out of the column and bays cut into it. The column's shape (NebulaPlume.Cover, the one implementation of it) is baked
// into cover_tex over the bounding box, so a sample of the march costs a fetch and no trigonometry.
float plume_cover(vec3 p) {
	return textureLod(cover_tex, vec3(p.x / (2.0 * bound) + 0.5, (p.y - bottom) / (top - bottom), p.z / (2.0 * bound) + 0.5), 0.0).r;
}

// The cloud at a point, 0 to the plume's density: the billows cut by the cover and drawn up to a firm body with a crisp
// edge, that edge eaten by the wisps. tint is a slow drift of the noise, 0 to 1, for the cloud's change of colour; soft
// is the cloud before its edge is drawn crisp, 0 to 1, a band several steps of the march wide (what the light's edge
// terms read: a term read off the crisp edge, thinner than a step, shows as grain).
float plume_cloud(vec3 p, float cover, bool wisps, out float tint, out float soft) {
	// sampled a little askew, so the texture's repeat never lines up with the column
	vec3 q = vec3(p.x * 0.94 + p.z * 0.34, p.y, p.z * 0.94 - p.x * 0.34) * base_scale + seed;
	q.y -= plume_time * drift;
	vec4 n = textureLod(noise_tex, q, 0.0);
	tint = n.g;
	soft = 0.0;
	float low = n.g * 0.625 + n.b * 0.25 + n.a * 0.125;
	float body = clamp((n.r - (low - 1.0)) / (2.0 - low), 0.0, 1.0);
	float d = clamp((body - (1.0 - cover)) / max(cover, 1.0e-3), 0.0, 1.0);
	if (d <= 0.0) return 0.0;
#if CURRENT_RENDERER != RENDERER_COMPATIBILITY
	if (wisps) {
		vec3 q2 = p * detail_scale + seed.zxy;
		q2.y -= plume_time * drift * 2.5;
		vec4 m = textureLod(noise_tex, q2, 0.0);
		// (the two coarser channels only: wisps finer than a step of the march would show as grain)
		float e = erosion * (1.0 - (m.g * 0.75 + m.b * 0.25)) * (1.0 - smoothstep(0.0, 0.6, d));
		d = clamp((d - e) / (1.0 - e), 0.0, 1.0);
	}
#endif
	soft = d;
	return smoothstep(0.0, 0.28, d) * density;
}

void fragment() {
	vec3 ro = v_cam;
	vec3 rd = normalize(v_local - v_cam);
	float t0, t1;
	if (!plume_bounds(ro, rd, t0, t1)) discard;

	// the nearest solid along this ray, in radii: the march stops there
	float depth = texture(depth_texture, SCREEN_UV).x;
#if CURRENT_RENDERER == RENDERER_COMPATIBILITY
	vec3 ndc = vec3(SCREEN_UV, depth) * 2.0 - 1.0;
#else
	vec3 ndc = vec3(SCREEN_UV * 2.0 - 1.0, depth);
#endif
	vec4 view = INV_PROJECTION_MATRIX * vec4(ndc, 1.0);
	float solid = abs(view.w) > 1.0e-9 ? length(view.xyz / view.w) / v_scale : 1.0e9;
	t0 = max(t0, 0.0);
	t1 = min(t1, solid);
	if (t1 <= t0) discard;

	float t = t0 + FINE * pn_hash31(vec3(FRAGCOORD.xy, 1.0));
	vec3 sun_l = normalize(sun_dir);
	float sun_k = 0.35 + 1.0 * min(sun_energy, 1.5);
	// a silver lining: the cloud between the camera and the sun scatters forward
	float phase = 0.8 + 1.1 * pow(max(dot(rd, sun_l), 0.0), 6.0);
	vec3 lit = vec3(0.0);
	float through = 1.0;
	int empty = 8;   // fine samples since the last one in the column: past a few, the march strides
	bool skin = false;   // the last sample was in cloud
	bool back = false;   // the last sample went back half a step
	for (int i = 0; i < STEPS; i++) {
		if (t >= t1) break;
		vec3 p = ro + rd * t;
		float cover = plume_cover(p);
		if (cover <= 0.002) {
			empty++;
			t += empty > 3 ? FINE * COARSE : FINE;
			continue;
		}
		if (empty > 3) {
			// a stride landed in the column: back to where it left clear space, and on from there in fine steps
			empty = 0;
			t = max(t - FINE * (COARSE - 1.0), t0);
			continue;
		}
		empty = 0;
		float tint, soft;
		// the cloud thins round the camera, so a captain inside a plume still sees their own ship
		float d = plume_cloud(p, cover, true, tint, soft) * smoothstep(0.0, clear, t);
		// the cloud's skin is thinner than a step: a step that lands deep in it out of clear air goes back half way,
		// and the skin is walked in half steps
		if (!skin && d > 0.5 * density) {
			skin = true;
			back = true;
			t -= 0.5 * FINE;
			continue;
		}
		skin = back || d > 0.002;   // (one step back only: the sample after it is taken where it stands)
		back = false;
		float dt = FINE * (soft < 0.45 ? 0.5 : 1.0);
		if (d > 0.002) {
			// the cloud between this point and the sun
			float shade = 0.0;
			float reach = LIGHT_REACH;
			vec3 lp = p;
			for (int j = 0; j < LIGHT_STEPS; j++) {
				lp += sun_l * reach;
				float lc = plume_cover(lp);
				float lt, ls;
				if (lc > 0.002) shade += plume_cloud(lp, lc, false, lt, ls) * reach;
				reach *= 2.0;
			}
			// Beer's law, with a second, slower fall for the light scattered many times on its way in
			float sun_t = max(exp(-absorption * shade), 0.24 * exp(-absorption * 0.3 * shade));
			// and less light comes back out of a thin edge than out of a full billow, so creases between billows read
			sun_t *= mix(1.0, smoothstep(0.0, 0.7, soft), 0.45);
			float height = clamp((p.y - bottom) / (top - bottom), 0.0, 1.0);
			float thin = 1.0 - smoothstep(0.0, 0.8, soft);
			// the bays between billows lie deeper in the column than their tips, and take less of the sky's light
			float open = mix(1.0, 0.55, smoothstep(0.15, 0.7, cover));
			vec3 s = color_deep * (0.4 + 0.8 * height) * open
				+ mix(color_lit, color_glow, 0.45 * smoothstep(0.35, 0.75, tint)) * sun_k * phase * sun_t * open * (0.7 + 0.4 * height)
				+ color_glow * glow_strength * thin * thin;
			for (int f = 0; f < FLASHES; f++) {
				if (flash[f].w <= 0.0) continue;
				vec3 dv = p - flash[f].xyz;
				// (capped: a strike beside the camera lights the cloud, it does not white the screen out)
				s += flash_color * min(flash[f].w / (1.0 + dot(dv, dv) * 90.0), 1.6);
			}
			float a = 1.0 - exp(-absorption * d * dt);
			lit += through * a * s;
			through *= 1.0 - a;
			if (through < 0.02) {
				through = 0.0;
				break;
			}
		}
		t += dt;
	}
	float alpha = 1.0 - through;
	if (alpha < 0.003) discard;
	ALBEDO = lit / alpha;
	ALPHA = alpha;
}
