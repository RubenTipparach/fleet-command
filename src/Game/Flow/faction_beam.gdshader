// The lamp's light falling through dust on the faction screen (FactionStage): a cone from the lamp to the table, brightest
// down its middle and toward the lamp, fading to nothing by the table, added to the scene. A port of the page's beamMat
// (docs/mockups/game-flow.html). Two things the port must undo (owner, 2026-11-06: "that light cone is wrong", "the cone
// shading is wrong"):
//  * three.js's cone has uv.y 1 at the tip (the lamp) and 0 at the base; Godot's CylinderMesh has UV.y 0 at the top (the
//    tip) and 1 at the base, so the page's uv.y is 1 - UV.y here;
//  * the page writes its value straight into the display (sRGB) buffer, Godot adds in linear light and encodes after, so
//    the page's number is turned to linear before it is added, or the beam reads several times brighter.
shader_type spatial;
render_mode unshaded, blend_add, cull_disabled, depth_draw_never, shadows_disabled;

uniform float level = 0.0;
// the page's colour as it writes it (display values, not linear: no source_color hint)
uniform vec3 colour = vec3(1.0, 0.9, 0.74);

vec3 to_linear(vec3 c) {
	return mix(c / 12.92, pow((c + 0.055) / 1.055, vec3(2.4)), step(vec3(0.04045), c));
}

void fragment() {
	float edge = pow(abs(dot(normalize(NORMAL), normalize(VIEW))), 1.6);
	float along = pow(1.0 - UV.y, 0.7);   // the page's uv.y: 1 at the lamp, 0 at the base
	ALBEDO = to_linear(colour * edge * along * 0.16 * level);
}
