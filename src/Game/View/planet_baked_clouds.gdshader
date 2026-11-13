// planet_baked_clouds: a planet's cloud deck in the low-spec mode, its baked map (RGBA) on a sphere just above the ground,
// lit by the battle's sun (planet_baked.gdshader's light).
shader_type spatial;
render_mode unshaded, cull_back, blend_mix, depth_draw_never;
uniform sampler2D map : source_color, filter_linear_mipmap, repeat_enable;
uniform vec3 sun_dir = vec3(0.0, 1.0, 0.0);
uniform float sun_energy = 1.0;
uniform float ambient = 0.05;
varying vec3 wn;
void vertex() { wn = (MODEL_MATRIX * vec4(NORMAL, 0.0)).xyz; }
void fragment() {
	vec4 c = texture(map, UV);
	float d = clamp(dot(normalize(wn), normalize(sun_dir)) * 1.4 + 0.1, 0.0, 1.0);   // a soft terminator, as the full shader's air gives
	ALBEDO = c.rgb * (ambient + d * min(sun_energy, 1.5));
	ALPHA = c.a;
}
