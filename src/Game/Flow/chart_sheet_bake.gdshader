// The low-spec mode's bake of the paper chart's sheet (ChartRoom): its colour drawn once into a texture each time the chart
// changes, by the same code the live sheet runs (chart_sheet.gdshaderinc). The sheet's work per pixel (the nearest hex
// found among nine, its edge and its neighbour's holder, five layers) then runs once a turn, not in every frame and every
// light's pass. Written as it is: sRGB colour and the paper's alpha.
shader_type canvas_item;
render_mode blend_disabled;

#include "res://src/Game/Flow/chart_sheet.gdshaderinc"

void fragment() {
	COLOR = sheet_colour(UV);
}
