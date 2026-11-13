// planet_baked_rings: a ringed planet's rings in the low-spec mode: the baked strip (inner radius at its left, outer at its
// right) read by each point's distance from the planet's centre on a flat square in the equator plane.
shader_type spatial;
render_mode unshaded, cull_disabled, blend_mix, depth_draw_never;
uniform sampler2D map : source_color, filter_linear, repeat_disable;
uniform float ring_inner = 1.5;
uniform float ring_outer = 2.5;
uniform vec3 sun_dir = vec3(0.0, 1.0, 0.0);
uniform float sun_energy = 1.0;
uniform float ambient = 0.15;
void fragment() {
	float r = length((UV - 0.5) * 2.0) * ring_outer;
	float u = (r - ring_inner) / (ring_outer - ring_inner);
	if (u < 0.0 || u > 1.0) discard;
	vec4 c = texture(map, vec2(u, 0.5));
	ALBEDO = c.rgb * (ambient + 0.85 * min(sun_energy, 1.5));
	ALPHA = c.a;
}
