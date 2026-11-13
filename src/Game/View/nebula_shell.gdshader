// nebula_shell: one shell of a nebula drawn as geometry (NebulaShells; the owner, 2026-11-12: "Nebula shells. The cube
// marched, generate once static mesh"; mockup 40). The mesh was polygonised once at load; here it is only coloured: its
// vertex colour (the cloud's colour in light, alpha its share of the shell's opacity) dimmed toward its colour in shadow
// on the side away from the sun, a glow at the rims, faded as it turns edge-on so no hard outline shows, and faded away
// near the camera, so the shells a ship flies through do not fill the screen. Blended over what is behind it (never
// brighter than the cloud's own colour, however many shells stack), with no depth written. No noise per pixel, so a small GPU's compiler has nothing to chew on.
shader_type spatial;
render_mode unshaded, blend_mix, cull_back, depth_draw_never;   // the far side of a shell is the next shell's to show: half the fill
uniform vec3 sun_dir = vec3(0.0, 1.0, 0.0);
uniform float sun_energy = 1.0;
uniform float alpha = 0.16;
uniform float soft = 1.0;
uniform float shadow = 0.3;
uniform vec3 glow : source_color = vec3(0.0);
uniform float near_fade = 1.0;   // scene units: clear at this distance from the camera, whole at three times it
varying vec3 wn;
void vertex() { wn = (MODEL_MATRIX * vec4(NORMAL, 0.0)).xyz; }
void fragment() {
	float facing = abs(dot(normalize(NORMAL), normalize(VIEW)));
	float lit = clamp(dot(normalize(wn), normalize(sun_dir)) * 0.5 + 0.5, 0.0, 1.0);
	vec3 c = COLOR.rgb * mix(shadow, 1.0, lit) * min(sun_energy, 1.5) + glow * pow(1.0 - facing, 2.0) * 0.6;
	float near = smoothstep(near_fade, near_fade * 3.0, length(VERTEX));
	ALBEDO = c;
	ALPHA = clamp(alpha * COLOR.a * pow(facing, soft) * near, 0.0, 1.0);
}
