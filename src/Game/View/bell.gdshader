// bell.gdshader: an impulse bell in the battle (owner, 2026-11-04, of the Albatross: "more thrusters"). A port of the
// ship mockups' thruster material (docs/mockups/lib/ship-skin.js thrusterMat, recorded in assets/ships/manifest.json as
// the bell's heat_rgb): the shell outside is dark metal with ribs and rings cut in it (the lathe's own UVs: u round the
// bell, v along it); inside (the back faces) it glows with heat, from the throat colour toward the lip colour by depth,
// times (0.15 + 1.9 d^2), dimmed in the grooves.
shader_type spatial;
render_mode cull_disabled;

uniform vec3 shell : source_color = vec3(0.290, 0.306, 0.341);
uniform vec3 heat_a : source_color = vec3(0.85, 0.12, 0.03);
uniform vec3 heat_b : source_color = vec3(1.0, 0.78, 0.45);
uniform float heat = 1.0;
uniform float ribs = 32.0;

void fragment() {
	float rib = step(0.9, fract(UV.x * ribs)), ring = step(0.86, fract(UV.y * 7.0)), groove = max(rib, ring);
	ALBEDO = shell * (FRONT_FACING ? 1.0 - 0.5 * groove : 0.25);
	METALLIC = 0.8;
	ROUGHNESS = 0.34;
	if (!FRONT_FACING) {
		float d = UV.y;
		vec3 hot = mix(heat_a, heat_b, smoothstep(0.25, 1.0, d));
		EMISSION = hot * (0.15 + 1.9 * d * d) * heat * (1.0 - 0.55 * groove);
		NORMAL = -NORMAL;
	}
}
