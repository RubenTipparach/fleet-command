// planet_atmosphere.gdshader: a planet's air, a shell round its ground that scatters the battle's sun (single scattering:
// the chord through the shell intersected per fragment, in-scatter marched along it, the sun's ray shadowed by the planet,
// so the shell has a real terminator and a warm limb at twilight). Its tint and strength are the kind's look
// (data/space/planets.json, PlanetLooks.Apply).
//
// Ported from godot-sandbox (RubenTipparach/godot-sandbox at 63f463f7), resources/shaders/comic_atmosphere_shell.gdshader,
// whose notes explain each choice. Changes for Fleet Command: the sun's strength multiplies the light (sun_energy), and the
// shell draws from inside as well as out (the planet view comes down through the air): it is drawn double sided and keeps
// the faces toward the camera from outside, the inner faces from inside, so it is never drawn twice.
shader_type spatial;
render_mode unshaded, shadows_disabled, depth_draw_never, blend_mix, cull_disabled;

uniform vec3 sun_dir = vec3(0.4, 0.7, 0.6);   // toward the sun, world space
uniform float sun_energy = 1.0;
uniform vec3 beta : source_color = vec3(0.85, 0.9, 1.0);
uniform vec3 scatter_tint : source_color = vec3(0.5, 0.7, 1.0);
uniform float scatter_strength = 6.0;
uniform float density_falloff = 3.5;
// the ground and the shell's top, in the mesh's own units
uniform float planet_radius = 1.0;
uniform float atmo_radius = 1.05;

const int VIEW_STEPS = 10;
const int LIGHT_STEPS = 4;

varying vec3 v_view_pos;
varying vec3 v_centre;
varying float v_scale;

void vertex() {
	v_view_pos = (MODELVIEW_MATRIX * vec4(VERTEX, 1.0)).xyz;
	v_centre = (MODELVIEW_MATRIX * vec4(0.0, 0.0, 0.0, 1.0)).xyz;
	v_scale = length(MODELVIEW_MATRIX[0].xyz);
}

vec2 ray_sphere(vec3 ro, vec3 rd, vec3 c, float r) {
	vec3 oc = ro - c;
	float b = dot(oc, rd);
	float q = dot(oc, oc) - r * r;
	float disc = b * b - q;
	if (disc < 0.0) { return vec2(1.0, -1.0); }
	float s = sqrt(disc);
	return vec2(-b - s, -b + s);
}

float view_optical_depth(vec3 rd, vec3 centre, float rg, float ra) {
	vec2 ha = ray_sphere(vec3(0.0), rd, centre, ra);
	if (ha.y < ha.x) { return 0.0; }
	float t0 = max(ha.x, 0.0);
	float t1 = ha.y;
	vec2 hg = ray_sphere(vec3(0.0), rd, centre, rg);
	if (hg.y > hg.x && hg.x > 0.0) { t1 = min(t1, hg.x); }
	float dt = max(t1 - t0, 0.0) / float(VIEW_STEPS);
	float od = 0.0;
	for (int i = 0; i < VIEW_STEPS; i++) {
		vec3 p = rd * (t0 + (float(i) + 0.5) * dt);
		float h = clamp((length(p - centre) - rg) / max(ra - rg, 1e-4), 0.0, 1.0);
		od += exp(-h * density_falloff) * dt;
	}
	return od;
}

vec3 in_scatter(vec3 rd, vec3 l, vec3 centre, float rg, float ra) {
	vec2 ha = ray_sphere(vec3(0.0), rd, centre, ra);
	if (ha.y < ha.x) { return vec3(0.0); }
	float t0 = max(ha.x, 0.0);
	float t1 = ha.y;
	vec2 hg = ray_sphere(vec3(0.0), rd, centre, rg);
	if (hg.y > hg.x && hg.x > 0.0) { t1 = min(t1, hg.x); }
	if (t1 <= t0) { return vec3(0.0); }
	float dt = (t1 - t0) / float(VIEW_STEPS);
	vec3 od_view = vec3(0.0);
	vec3 acc = vec3(0.0);
	for (int i = 0; i < VIEW_STEPS; i++) {
		vec3 p = rd * (t0 + (float(i) + 0.5) * dt);
		float h = clamp((length(p - centre) - rg) / max(ra - rg, 1e-4), 0.0, 1.0);
		float dens = exp(-h * density_falloff) * dt;
		vec3 od_here = od_view + beta * dens * 0.5;
		od_view += beta * dens;
		vec2 sg = ray_sphere(p, l, centre, rg);
		if (sg.y > sg.x && sg.x > 0.0) { continue; }
		vec2 sa = ray_sphere(p, l, centre, ra);
		float sdt = max(sa.y, 0.0) / float(LIGHT_STEPS);
		vec3 od_sun = vec3(0.0);
		for (int j = 0; j < LIGHT_STEPS; j++) {
			vec3 q = p + l * ((float(j) + 0.5) * sdt);
			float hq = clamp((length(q - centre) - rg) / max(ra - rg, 1e-4), 0.0, 1.0);
			od_sun += beta * exp(-hq * density_falloff) * sdt;
		}
		acc += dens * exp(-(od_sun + od_here));
	}
	float mu = dot(rd, l);
	float phase = (3.0 / (16.0 * PI)) * (1.0 + mu * mu);
	return acc * beta * scatter_tint * phase * scatter_strength;
}

void fragment() {
	// the march runs in the mesh's own units (the scene scaled down by the planet's size about the camera), so a planet of
	// any size scatters alike
	vec3 centre = v_centre / v_scale;
	float rg = planet_radius;
	float ra = atmo_radius;
	// one side only: the outer faces seen from outside, the inner ones from inside the air
	bool inside = length(centre) < ra;
	if (FRONT_FACING == inside) { discard; }
	vec3 rd = normalize(v_view_pos);
	vec3 l = normalize((VIEW_MATRIX * vec4(normalize(sun_dir), 0.0)).xyz);
	float od = view_optical_depth(rd, centre, rg, ra);
	float a = clamp(1.0 - exp(-od * dot(beta, vec3(0.3333))), 0.0, 1.0);
	vec3 s = in_scatter(rd, l, centre, rg, ra);
	ALBEDO = s * sun_energy / max(a, 1e-3);
	ALPHA = a;
}
