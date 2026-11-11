// lens.gdshader: a glowing mouth on a ship's hull (a gun's mouth, a tractor emitter, a drive), as mockup 37 draws it
// (docs/mockups/lib/baked-ship.js lenses; Ship Design Bible T14; owner, 2026-11-11: "If you're gonna have glowy bits use
// baked lighting or shaders"). The hull's maps hold a dark lens with its light baked onto the plating round it; this draws
// the glow over the lens: a disc facing out of the mouth, a hot core fading to a darker lip, slow rings drawn inward, a rim
// that lights as the view grazes it, and a slow pulse, added over the scene. Its size is the quad's (the lens_* empty's
// radius_m, set by ShipSkin), its colour and pulse phase the empty's own.
shader_type spatial;
render_mode blend_add, unshaded, cull_disabled, depth_draw_never, shadows_disabled;

uniform vec3 colour : source_color = vec3(0.25, 0.72, 0.64);
uniform float phase = 0.0;
// 1 while its piece is on the ship, 0 once it has broken away
uniform float lit = 1.0;

void fragment() {
	float r = length(UV - 0.5) * 2.0;
	if (r > 1.0) discard;
	float pulse = 0.82 + 0.18 * sin(TIME * 1.7 + phase);
	float core = exp(-r * r * 7.0);                                                     // the hot centre
	float body = smoothstep(1.0, 0.15, r) * 0.45;                                       // the glow falling off toward the lip
	float rings = 0.10 * (0.5 + 0.5 * sin(r * 22.0 + TIME * 2.6 + phase)) * smoothstep(1.0, 0.35, r);   // drawn inward
	float rim = smoothstep(0.84, 0.95, r) * smoothstep(1.0, 0.95, r);
	float graze = pow(1.0 - abs(dot(normalize(NORMAL), normalize(VIEW))), 2.0);        // the rim lights as the view grazes the mouth
	vec3 c = colour * (body + rings) * pulse + mix(colour, vec3(1.0), 0.7) * core * pulse + colour * rim * (0.25 + 0.9 * graze);
	ALBEDO = c * lit;
}
