// The paper chart's sheet in the low-spec mode (ChartRoom): its colour baked by chart_sheet_bake.gdshader (sRGB, read as
// it is stored), lit by the room as the live sheet is (chart_sheet.gdshader).
shader_type spatial;
render_mode cull_disabled;

uniform sampler2D baked : filter_linear_mipmap;

void fragment() {
	vec4 c = texture(baked, vec2(UV.x, 1.0 - UV.y));
	ALBEDO = pow(c.rgb, vec3(2.2));   // the live sheet's to_lin
	ROUGHNESS = 0.92;
	ALPHA = c.a;
	ALPHA_SCISSOR_THRESHOLD = 0.5;
}
