// lamp_glow.gdshader: the glow round a ship's running light, strobe or beacon (change ssarith-ships-engine 2.4), as mockup 12
// draws it over each lamp: a soft round sprite facing the camera, white at its heart and gone at its edge (the page's
// glowSprite gradient: 1 at the centre, 0.55 a quarter out, 0 at the rim), added over the scene. Its size is the quad's (the
// lamp's glow_m, set by ShipSkin). A strobe's glow flashes with its light (ShipSkin.Flash shows and hides both), a beacon's
// pulses (the page's 0.25 + 0.75 max(0, sin(pi t))^3), a running light's burns steady at 0.8. A floodlight has none (owner,
// 2026-11-06: "I dont think we need the light source dots for hul lights, just nav lights are good enough").
shader_type spatial;
render_mode blend_add, unshaded, cull_disabled, depth_draw_never, shadows_disabled;

uniform vec3 colour : source_color = vec3(1.0);
// 0 steady, 1 a beacon's pulse
uniform int pulse = 0;
// 1 while its piece is on the ship, 0 once it has broken away
uniform float lit = 1.0;

void vertex() {
	// a billboard: the quad laid out in the camera's plane about the lamp, at the lamp's own scale (the ship's)
	vec3 centre = MODEL_MATRIX[3].xyz; float sc = length(MODEL_MATRIX[0].xyz);
	vec3 world = centre + (INV_VIEW_MATRIX[0].xyz * VERTEX.x + INV_VIEW_MATRIX[1].xyz * VERTEX.y) * sc;
	POSITION = PROJECTION_MATRIX * (VIEW_MATRIX * vec4(world, 1.0));
}

void fragment() {
	float r = length(UV - 0.5) * 2.0;
	float g = r < 0.25 ? mix(1.0, 0.55, r / 0.25) : mix(0.55, 0.0, clamp((r - 0.25) / 0.75, 0.0, 1.0));
	float on = pulse == 1 ? 0.25 + 0.75 * pow(max(0.0, sin(TIME * PI)), 3.0) : 0.8;
	// unshaded: what is drawn, added over the scene. The lamp's colour is scaled to at most 1, as the page's sprite colours
	// are: the battle blooms only what is brighter than 1, and a lamp's glow is a soft dot, not a flare.
	vec3 c = colour / max(1.0, max(colour.r, max(colour.g, colour.b)));
	ALBEDO = c * g * on * lit;
}
