// nebula_sky.gdshader: the environment's sky, for light and reflections only (the background the player sees is the sky
// file's own mesh, NebulaSky.cs, drawn over it). It samples a scenario's baked panorama (assets/skies/<id>.jpg,
// tools/space/sky_panorama.py) with the bake's own mapping: u from the longitude round +y from +z toward +x, -180
// degrees at the left; v from the latitude, +90 at the top. So the hulls reflect and are lit by the nebula they fight in.
shader_type sky;

uniform sampler2D panorama : source_color, filter_linear, repeat_enable;
uniform float energy = 1.0;

void sky() {
	vec3 d = normalize(EYEDIR);
	float lon = atan(d.x, d.z);
	float lat = asin(clamp(d.y, -1.0, 1.0));
	vec2 uv = vec2(lon / (2.0 * PI) + 0.5, 0.5 - lat / PI);
	COLOR = texture(panorama, uv).rgb * energy;
}
