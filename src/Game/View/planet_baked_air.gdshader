// planet_baked_air: a planet's air in the low-spec mode: a rim of its colour round the sphere, strongest edge-on and on the
// sunlit side, added over the planet (the full single-scattering shell is too costly for a small GPU).
shader_type spatial;
render_mode unshaded, cull_back, blend_add, depth_draw_never;
uniform vec3 color : source_color = vec3(0.4, 0.7, 1.0);
uniform float strength = 1.0;
uniform vec3 sun_dir = vec3(0.0, 1.0, 0.0);
uniform float sun_energy = 1.0;
varying vec3 wn;
void vertex() { wn = (MODEL_MATRIX * vec4(NORMAL, 0.0)).xyz; }
void fragment() {
	float rim = pow(1.0 - abs(dot(normalize(NORMAL), normalize(VIEW))), 3.0);
	float lit = smoothstep(-0.2, 0.6, dot(normalize(wn), normalize(sun_dir)));
	ALBEDO = color * rim * lit * strength * min(sun_energy, 1.5);
}
