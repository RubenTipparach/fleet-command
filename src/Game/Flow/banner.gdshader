// banner.gdshader: cloth banners on the campaign map (game-flow mockup v15, bannerMesh and waveBanners; owner, 2026-11-05:
// "Ithink mission flags sould just be 3d"): a subdivided plane hung from its top edge (a mission's gonfalon) or its left
// edge (a fleet's strength flag), rippling most at its free edge and not at all where it hangs. The shape (a notched
// tail), its dark outline and a strength flag's pips are cut from the UVs here, so no texture is painted.
shader_type spatial;
render_mode cull_disabled;

uniform vec4 ink : source_color = vec4(1.0, 0.7, 0.28, 1.0);
uniform vec4 line_ink : source_color = vec4(0.1, 0.063, 0.024, 1.0);
uniform float seed = 0.0;
// 0: hung from the top (the free edge is the bottom); 1: hung from the left (the free edge is the right)
uniform int hang = 0;
// where the tail ends (0 to 1 down the cloth) and how deep its notch is (a fraction of the cloth's height)
uniform float tail = 1.0;
uniform float notch = 0.18;
// the cloth's size in canvas pixels, for an even outline and round pips
uniform vec2 px = vec2(128.0, 176.0);
uniform int pips = 0;

void vertex() {
	float free = hang == 1 ? UV.x : UV.y;
	VERTEX.z += sin(TIME * 2.2 + seed + free * 4.5) * 0.018 * free;
}

void fragment() {
	vec2 uv = UV;
	// the notched tail: the bottom edge rises to a point at the middle
	float bottom = tail - notch * (1.0 - abs(uv.x - 0.5) * 2.0);
	if (uv.y > bottom) discard;
	vec2 q = uv * px;
	float edge = min(min(q.x, px.x - q.x), min(q.y, (bottom - uv.y) * px.y));
	vec3 c = ink.rgb;
	if (edge < 3.5) c = line_ink.rgb;
	if (q.y < 12.0 && hang == 1) c *= 0.72;
	for (int i = 0; i < 5; i++) {
		if (i >= pips) break;
		vec2 pc = vec2(px.x * 0.5, 34.0 + float(i) * 34.0);
		if (distance(q, pc) < 10.0) c = vec3(0.957, 0.945, 0.91);
	}
	ALBEDO = c;
	EMISSION = c * 0.35;
	ROUGHNESS = 0.85;
}
