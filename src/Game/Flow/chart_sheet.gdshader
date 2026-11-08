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

uniform sampler2D base_tex : filter_linear_mipmap_anisotropic;
uniform sampler2D screen_tex : filter_linear_mipmap_anisotropic;
uniform sampler2D over_tex : filter_linear_mipmap_anisotropic;
uniform sampler2D mul_tex : filter_linear_mipmap_anisotropic;
// per sector, one texel each (COLS x ROWS): rgb the holder's ink (sRGB), a 1 when the holder is a faction, 0 unclaimed
uniform sampler2D owner_tex : filter_nearest, repeat_disable;
// per sector: r the fog, 0 uncharted, 0.5 seen before, 1 seen now
uniform sampler2D fog_tex : filter_nearest, repeat_disable;
// the fog's mask over the whole sheet, already blurred (ChartRoom.FogMask): 1 black over the uncharted, 0.45 the veil over
// the revealed, 0 what is seen now; read with linear filtering, so its border is soft
uniform sampler2D fog_mask : filter_linear, repeat_disable;
uniform vec4 sheet_bounds;   // x0, z0, x1, z1 of the sheet in map units
uniform vec4 print_bounds;   // the print's frame
uniform vec2 grid_size;      // columns, rows

const float SQ3 = 1.7320508;

vec3 to_lin(vec3 c) { return pow(c, vec3(2.2)); }

// the sector a map point falls in: the nearest centre (the hexes are the centres' Voronoi cells)
ivec2 hex_at(vec2 p) {
	int c0 = int(round(p.x / 1.5));
	ivec2 best = ivec2(0);
	float bd = 1e9;
	for (int dc = -1; dc <= 1; dc++) {
		int c = c0 + dc;
		float off = float(c & 1) * 0.5;
		int r0 = int(round(p.y / SQ3 - off));
		for (int dr = -1; dr <= 1; dr++) {
			int r = r0 + dr;
			vec2 ctr = vec2(1.5 * float(c), SQ3 * (float(r) + off));
			float d = dot(p - ctr, p - ctr);
			if (d < bd) { bd = d; best = ivec2(c, r); }
		}
	}
	return best;
}
vec2 centre(ivec2 h) { return vec2(1.5 * float(h.x), SQ3 * (float(h.y) + float(h.x & 1) * 0.5)); }
bool on_map(ivec2 h) { return h.x >= 0 && h.y >= 0 && float(h.x) < grid_size.x && float(h.y) < grid_size.y; }
vec4 owner_of(ivec2 h) { return on_map(h) ? texelFetch(owner_tex, h, 0) : vec4(0.0); }
float fog_of(ivec2 h) { return on_map(h) ? texelFetch(fog_tex, h, 0).r : 0.0; }
void fragment() {
	// the glb's uvs run up the sheet; the layers' rows run down it (the page's canvas)
	vec2 uv = vec2(UV.x, 1.0 - UV.y);
	vec2 p = vec2(mix(sheet_bounds.x, sheet_bounds.z, uv.x), mix(sheet_bounds.y, sheet_bounds.w, uv.y));
	vec4 base = texture(base_tex, uv);
	vec3 col = base.rgb;
	bool in_print = p.x > print_bounds.x && p.x < print_bounds.z && p.y > print_bounds.y && p.y < print_bounds.w;
	if (in_print) {
		ivec2 h = hex_at(p);
		vec2 d = p - centre(h);
		float fog = fog_of(h);
		if (on_map(h) && fog > 0.25) {
			vec4 own = owner_of(h);
			float dim = fog < 0.75 ? 0.55 : 1.0;
			// the edge this point is nearest: its outward normal and how far along it (k from 0 at +x, a sixth of a turn each)
			float ang = atan(d.y, d.x);
			float k = floor(mod(ang, TAU) / (PI / 3.0));   // the edge between corners k and k + 1
			float na = PI / 3.0 * k + PI / 6.0;
			vec2 n = vec2(cos(na), sin(na)), t = vec2(-n.y, n.x);
			float along = dot(d, n), side = dot(d, t);
			// the holder's wash (screen, a sixteenth), inside the hex at 0.98
			if (own.a > 0.5 && along < 0.98 * SQ3 * 0.5) col = 1.0 - (1.0 - col) * (1.0 - own.rgb * 0.06);
			// the outline at 0.97: a faction's ink lightened a fifth, 0.032 wide; unclaimed space pale, 0.02 wide
			float edge = abs(along - 0.97 * SQ3 * 0.5);
			if (own.a > 0.5) {
				vec3 ink = mix(own.rgb, vec3(1.0), 0.2);
				col = mix(col, ink, 0.75 * dim * (1.0 - smoothstep(0.012, 0.018, edge)));
			} else {
				col = mix(col, vec3(214.0, 206.0, 188.0) / 255.0, 0.3 * dim * (1.0 - smoothstep(0.007, 0.012, edge)));
			}
			// the heavier rule along an edge where the holder changes: at 0.88 of the corner radius, 0.09 wide, round ends
			if (own.a > 0.5) {
				ivec2 nb = hex_at(centre(h) + n * SQ3);
				vec4 o2 = owner_of(nb);
				bool same = on_map(nb) && fog_of(nb) > 0.25 && distance(o2.rgb, own.rgb) < 0.01 && o2.a > 0.5;
				if (!same) {
					float half_len = 0.88 * 0.5;
					vec2 q = vec2(clamp(side, -half_len, half_len), 0.88 * SQ3 * 0.5);
					float dist = distance(vec2(side, along), q);
					vec3 ink2 = mix(own.rgb, vec3(1.0), 0.12);
					col = mix(col, ink2, 0.95 * dim * (1.0 - smoothstep(0.035, 0.045, dist)));
				}
			}
		}
		// the fog: flat black ink over the uncharted, a veil over the revealed, its border soft
		col = mix(col, vec3(7.0, 8.0, 11.0) / 255.0, texture(fog_mask, uv).r);
	}
	// the still layers over it: the worn ink (screen), the navigator's furniture (over), the years (multiply)
	vec4 s = texture(screen_tex, uv);
	col = 1.0 - (1.0 - col) * (1.0 - s.rgb * s.a);
	vec4 o = texture(over_tex, uv);
	col = mix(col, o.rgb, o.a);
	vec4 mu = texture(mul_tex, uv);
	col *= mix(vec3(1.0), mu.rgb, mu.a);
	ALBEDO = to_lin(col);   // composited in sRGB, as the page's canvas composites
	ROUGHNESS = 0.92;
	ALPHA = base.a;
	ALPHA_SCISSOR_THRESHOLD = 0.5;
}
