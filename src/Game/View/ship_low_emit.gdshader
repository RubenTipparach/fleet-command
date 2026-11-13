// ship_low_emit.gdshader: a ship part's emission in the low-spec mode (LowShips): every glow, engine throat, window and
// lens of the part merged into one surface, each vertex its own glowing colour, unlit; out when the part breaks away (lit).
shader_type spatial;
render_mode unshaded, cull_disabled;
uniform float lit = 1.0;
void fragment() {
	ALBEDO = COLOR.rgb * lit;
}
