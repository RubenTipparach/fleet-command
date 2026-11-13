// The paper chart's sheet (openspec/changes/paper-map; mockup 1's paperSheet, docs/mockups/game-flow.html): the still layers
// the page drew and tools/mockups/export_chart.mjs wrote (base: paper, print, stars, numbers, suns; then the worn ink by
// screen, the rhumb lines, compass, title and folds over, the frame, stains, foxing and browned edge by multiply), and
// between them what changes in play, drawn here as the page draws it: each charted hex washed faintly in its holder's
// colour, its outline in the holder's ink (pale for unclaimed space), a heavier rule inside it along each edge where the
// holder changes, dimmer where the sector was seen before but is not seen now; and the fog of war, flat black over the
// uncharted and a lighter veil over the revealed, blurred so its border is soft. The sheet is lit by the room's lamp.
// The hexes are flat-topped, a corner at radius 1 (HexGrid.Pos: x = 1.5 c, z = sqrt(3) (r + c/2 odd)). Everything is
// composited in sRGB, as the canvas does, and turned to linear light once at the end.
shader_type spatial;
render_mode cull_disabled;

#include "res://src/Game/Flow/chart_sheet.gdshaderinc"

void fragment() {
	// the glb's uvs run up the sheet; the layers' rows run down it (the page's canvas)
	vec4 c = sheet_colour(vec2(UV.x, 1.0 - UV.y));
	ALBEDO = to_lin(c.rgb);   // composited in sRGB, as the page's canvas composites
	ROUGHNESS = 0.92;
	ALPHA = c.a;
	ALPHA_SCISSOR_THRESHOLD = 0.5;
}
