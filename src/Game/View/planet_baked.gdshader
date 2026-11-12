// planet_baked: a planet's ground in the low-spec mode (LowSpec): its baked map (assets/space/planets_baked, PlanetBaker)
// on a plain sphere, lit by the battle's sun. The full ground shader stalls a small GPU's compiler (a Raspberry Pi 5,
// docs/playtests/pi/README.md), so this one only samples and lights. A volcanic world (lava) keeps its brightest
// colours lit on the night side.
shader_type spatial;
render_mode unshaded, cull_back;
uniform sampler2D map : source_color, filter_linear_mipmap, repeat_enable;
uniform vec3 sun_dir = vec3(0.0, 1.0, 0.0);
uniform float sun_energy = 1.0;
uniform float ambient = 0.05;
uniform vec3 glow = vec3(0.0);
varying vec3 wn;
void vertex() { wn = (MODEL_MATRIX * vec4(NORMAL, 0.0)).xyz; }
void fragment() {
	vec3 c = texture(map, UV).rgb;
	float d = clamp(dot(normalize(wn), normalize(sun_dir)) * 1.4 + 0.1, 0.0, 1.0);   // a soft terminator, as the full shader's air gives
	float hot = smoothstep(0.55, 0.9, max(c.r, max(c.g, c.b))) * step(0.02, dot(glow, vec3(1.0)));
	ALBEDO = c * (ambient + d * min(sun_energy, 1.5)) + c * hot * (1.0 - d);
}
