// The lamp's light falling through dust on the faction screen (FactionStage): a cone from the lamp to the table, brightest
// down its middle and toward its base, added to the scene. A port of the page's beamMat (docs/mockups/game-flow.html); its
// numbers are the exported manifest's beam (assets/scenes/faction/manifest.json).
shader_type spatial;
render_mode unshaded, blend_add, cull_disabled, depth_draw_never, shadows_disabled;

uniform float level = 0.0;
uniform vec3 colour : source_color = vec3(1.0, 0.9, 0.74);

void fragment() {
	float edge = pow(abs(dot(normalize(NORMAL), normalize(VIEW))), 1.6);
	float along = pow(UV.y, 0.7);   // 0 at the lamp, 1 at the base
	ALBEDO = colour * edge * along * 0.16 * level;
}
