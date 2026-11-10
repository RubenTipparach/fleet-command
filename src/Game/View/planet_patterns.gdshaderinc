// planet_patterns.gdshaderinc: what was built, or died, on a world's ground (owner, 2026-11-10: "theres some world types in
// falchion, city planet, archipelego, fossil planet (lots of bones). and an old alien ruins planet"). Included by
// planet.gdshader after its noise (it uses snoise and ridged, and the uniforms seed, detail, c0 to c3, glow, pattern and
// pattern_color), and drawn over the ground's colour where a kind has a pattern (PlanetLooks.Patterns):
//   1 city    a world built over, all of it: districts on avenues that wander off the grid, blocks split into streets
//             again and again, round plaza districts, diagonal boulevards, every plot a building of its own height,
//             islands of far taller towers, the shadows they throw, and its lights over the night side, blazing in the
//             islands and thinner between them
//   2 ruins   what an old people left: great ring works, the footprints of towns, broken causeways, most of it under the
//             dust, a few lamps still lit
//   3 bones   a graveyard: skeletons from the size of a province down to a hill, half buried, over dead forests and salt
//
// After Falchion (RubenTipparach/falchion at 5d128af8, src/render/planets/patterns.glsl.ts and the cubeUV of
// atmosphere.glsl.ts): the city began as a port of its PATTERN 1 (the ecumenopolis: the bent grid, the streets split again and again, the plazas
// and boulevards are its; the heights, islands and shadows are new here); the bones' ground (dead forests, salt flats,
// ash) is its PATTERN 4; the ruins take its dead machine world's dying lamps (PATTERN 2) and its choir world's pulse
// (PATTERN 3). New here: the skeletons (Falchion's bones are props on its ground), the ring works, towns and causeways,
// and these changes to the method:
//  - the cube's faces are even in angle (pp_cube), so a block at a face's corner is the size of one at its middle;
//  - every cell count is a whole number to the cube unit, so cells meet the faces' edges and nothing is cut by one;
//  - a pixel's size is passed in (pxc, cube units, taken by the caller outside any branch), never taken by fwidth in
//    here: a derivative across a face's edge or inside a loop is wrong or undefined;
//  - a line finer than a pixel fades to its share of the ground and a cell finer than a pixel to its mean, so nothing
//    sparkles from afar and the pattern's brightness holds at every distance;
//  - the hash is PCG on the float's bits (pn_hash31), not Falchion's fract-multiply hash13 (CLAUDE.md section 10).

const float PP_PER_RAD = 1.2732395;   // cube units to a radian at a face's middle (4 / pi)

// the cube face a direction falls on: its place on the face (x, y: -1 to 1, even in angle) and the face (z: 0 to 5)
vec3 pp_cube(vec3 d) {
	vec3 a = abs(d);
	vec3 cu;
	if (a.x >= a.y && a.x >= a.z) { cu = vec3(d.zy / a.x, d.x > 0.0 ? 0.0 : 1.0); }
	else if (a.y >= a.z) { cu = vec3(d.xz / a.y, d.y > 0.0 ? 2.0 : 3.0); }
	else { cu = vec3(d.xy / a.z, d.z > 0.0 ? 4.0 : 5.0); }
	cu.xy = atan(cu.xy) * PP_PER_RAD;
	return cu;
}
// a cell's own number, 0 to 1 (salt picks which of its numbers), different on every face and every planet
float pp_hash(vec2 cell, float face, float salt) { return pn_hash31(vec3(cell, face + salt * 8.0) + floor(seed)); }
vec3 pp_hash3(vec2 cell, float face, float salt) { return pn_hash33(vec3(cell, face + salt * 8.0) + floor(seed)); }
// how much of a layer of cells can still be drawn (fw: a pixel's size in cells): 1 while a cell is wider than about
// three pixels, 0 once it is narrower than one
float pp_fade(float fw) { return 1.0 - smoothstep(0.35, 0.9, fw); }
// a cell's number, drawn toward the mean as its cell shrinks under a pixel
float pp_soft(float v, float fw) { return mix(v, 0.5, smoothstep(0.35, 0.9, fw)); }
// a band of half width hw about dist 0 (fw: a pixel in the same units): never drawn narrower than a pixel, dimmed by
// its share of that pixel instead
float pp_band(float dist, float hw, float fw) {
	float w = max(hw, fw * 0.6);
	return (1.0 - smoothstep(w - fw * 0.5, w + fw * 0.5, dist)) * hw / w;
}
// the lines of a grid, at the whole numbers of g; once its cells are finer than a pixel, the lines' share of the ground
float pp_line(float g, float hw, float fw) {
	return mix(pp_band(abs(g - floor(g + 0.5)), hw, fw), 2.0 * hw, smoothstep(0.3, 0.8, fw));
}
float pp_seg(vec2 q, vec2 a, vec2 b) {
	vec2 ab = b - a;
	return length(q - a - ab * clamp(dot(q - a, ab) / dot(ab, ab), 0.0, 1.0));
}

// ---- 1: the city world ------------------------------------------------------------------------------------------------
// The whole world is built over (owner, 2026-11-10: "City planet should just be covered in cities... Some islands of
// really tall buildings otherwise it's cities with buildings of various heights"): no open ground anywhere, every plot a
// building of its own height, and here and there an island of towers many times taller than the city round it.

// how deep in an island of towers a place is, 0 to 1 (a low noise, so an island is a few blocks to a district across)
float pp_isle(vec3 d) {
	return smoothstep(0.6, 0.72, pn_vnoise3(d * 10.0 + seed.zxy + 11.0) * 0.7 + pn_vnoise3(d * 27.0 + seed + 5.0) * 0.3);
}
// the same for one block (gb: the block grid): in an island's middle every block is towers, toward its edge fewer and
// fewer, so an island ends in single blocks of towers standing in the low city, never along a line
float pp_isle_block(vec2 gb, float face, float isle) {
	return isle * step(pp_hash(floor(gb), face, 31.0), isle * 1.5);
}
// the height of the building on a plot, in plot widths (g2: the plot grid, eight plots to a block's side): low and
// middling over most of the city, each block with its own habit, and in an island every plot a tower, a few far above
// the rest
float pp_city_height(vec2 g2, float face, float isle) {
	float t = pp_hash(floor(g2), face, 13.0);
	float habit = pp_hash(floor(g2 / 8.0), face, 0.0);
	return mix(0.15, 1.3, t * t) * (0.5 + habit) + pp_isle_block(g2 / 8.0, face, isle) * mix(1.5, 8.0, t * t * t);
}
// the same, with the alleys between plots and the streets between blocks open to the ground: what a shadow's ray meets
float pp_city_roof(vec2 g2, float face, float isle) {
	vec2 a = abs(g2 - floor(g2 + 0.5));
	vec2 gb = g2 / 8.0;
	vec2 b = abs(gb - floor(gb + 0.5));
	return pp_city_height(g2, face, isle) * step(0.08, min(a.x, a.y)) * step(0.045, min(b.x, b.y));
}

// h: the ground's height (the old terrain still shows as a shade under the city); near: how close the camera is; night: 1
// on the night side; sun_m: toward the sun, model space. Adds its lights to emit, gives the roofs' height in rise and
// the glass's glint in spec.
vec3 pp_city(vec3 d, vec3 cu, float pxc, float h, float near, float night, vec3 sun_m, inout vec3 emit, out float rise, inout float spec) {
	float face = cu.z;
	// the avenues wander, as roads laid over old country do (straight again at a face's edge, where the grids meet)
	vec2 e = cu.xy * cu.xy;
	vec2 bend = (vec2(pn_fbm3(d * 2.6 + seed + 3.0), pn_fbm3(d * 2.6 + seed.yzx + 9.0)) - 0.5) * 0.11 * (1.0 - e.x * e.x) * (1.0 - e.y * e.y);
	vec2 uv = cu.xy + bend;
	vec2 gA = uv * 6.0;    // districts
	vec2 g = uv * 24.0;    // blocks, four to a district's side
	float fA = pxc * 6.0, fB = pxc * 24.0;
	vec2 dA = floor(gA);
	float dh = pp_hash(dA, face, 19.0);
	float bh = pp_soft(pp_hash(floor(g), face, 0.0), fB);
	float isle = pp_isle(d);
	// (from afar, where a block is finer than a pixel, the share of an island's blocks that are towers)
	float isle_b = mix(pp_isle_block(g, face, isle), isle * min(isle * 1.5, 1.0), smoothstep(0.35, 0.9, fB));
	// the old cores: denser, paler, brighter at night
	float dense = pn_fbm3(d * 3.0 + seed);
	float core = smoothstep(0.42, 0.7, dense);
	vec3 block = mix(c1, c3, bh) * (0.8 + 0.35 * pp_soft(pp_hash(floor(g * 3.0), face, 7.0), fB * 3.0));
	block = mix(block, mix(c2, vec3(0.62, 0.6, 0.55), 0.4), core * 0.5);
	// every district its own age and trade, and whole regions darker or paler
	block *= mix(0.93, 1.05, pp_hash(dA, face, 29.0)) * (0.78 + 0.44 * pn_fbm3(d * 1.7 + seed.yzx));
	block *= 0.82 + 0.3 * smoothstep(-0.5, 0.5, h);
	float street = max(pp_line(g.x, 0.045, fB), pp_line(g.y, 0.045, fB));
	float avenue = max(pp_line(gA.x, 0.016, fA), pp_line(gA.y, 0.016, fA));
	// streets within streets: at each finer level a block whose number says so is split by its midlines, down to the
	// lanes a camera over the roofs can see
	float split_lines = 0.0, lvl = 1.0;
	for (int i = 1; i <= 7; i++) {
		lvl *= 2.0;
		float fL = fB * lvl;
		float fade = pp_fade(fL);
		if (fade <= 0.0) { break; }
		vec2 gL = g * lvl;
		float split = step(0.32, pp_hash(floor(gL * 0.5), face, float(i) * 7.0 + 1.0));
		vec2 odd = mod(floor(gL + 0.5), 2.0);
		float mid = max(pp_band(abs(gL.x - floor(gL.x + 0.5)), 0.09, fL) * odd.x, pp_band(abs(gL.y - floor(gL.y + 0.5)), 0.09, fL) * odd.y);
		split_lines = max(split_lines, mid * split * fade * (1.0 - 0.1 * float(i)));
	}
	// a district laid out round a plaza (rings and spokes), another cut by diagonal boulevards
	vec2 q = fract(gA) - 0.5;
	float rA = length(q);
	float circ = step(0.9, dh);
	float ring_roads = max(pp_band(abs(rA - 0.18), 0.007, fA), pp_band(abs(rA - 0.34), 0.007, fA));
	float spokes = pp_band(abs(sin(atan(q.y, q.x) * 4.0)) * rA * 0.25, 0.005, fA) * step(0.08, rA) * step(rA, 0.46);
	float plaza = 1.0 - smoothstep(0.08 - fA, 0.08 + fA, rA);
	float radial = circ * max(ring_roads, spokes);
	float diag = step(dh, 0.06) * max(pp_line(gA.x + gA.y, 0.014, fA * 1.414), pp_line(gA.x - gA.y, 0.014, fA * 1.414));
	// (inside a round district the plain grid gives way to the rings)
	float inner = circ * (1.0 - smoothstep(0.3, 0.42, rA));
	street *= 1.0 - inner;
	split_lines *= 1.0 - inner;
	// close up, every block is plots and alleys, every plot a building of its own height, and every roof its own
	float alley = 0.0, tower = 0.5, roof = 1.0;
	// (from afar a plot is finer than a pixel: the ground is as tall as its plots' mean)
	float tall = 0.6 + isle_b * 3.1;
	float fine = pp_fade(fB * 8.0);
	vec2 g2 = g * 8.0;
	if (fine > 0.0) {
		alley = max(pp_line(g2.x, 0.08, fB * 8.0), pp_line(g2.y, 0.08, fB * 8.0)) * fine;
		tower = pp_hash(floor(g2), face, 13.0);
		tall = mix(tall, pp_city_height(g2, face, isle), fine);
		float finer = pp_fade(fB * 64.0);
		if (finer > 0.0) {
			vec2 g3 = g * 64.0;
			roof *= mix(1.0, 0.86 + 0.28 * pp_hash(floor(g3), face, 17.0), finer);
			alley = max(alley, max(pp_line(g3.x, 0.06, fB * 64.0), pp_line(g3.y, 0.06, fB * 64.0)) * finer * 0.6 * step(0.4, tower));
		}
	}
	// a tall roof stands in clearer light than a low one, and the towers are glass
	block *= roof * (0.74 + 0.13 * min(tall, 5.0));
	block = mix(block, pattern_color * (0.55 + 0.08 * min(tall, 5.0)), smoothstep(1.3, 3.2, tall) * 0.6);
	block = mix(block, mix(c3, vec3(0.7, 0.68, 0.62), 0.5), circ * plaza * 0.6);
	float roads = max(max(max(street * 0.8, avenue), max(radial, diag)), max(alley * 0.7, split_lines));
	// (a road from orbit is a darker seam in the roofs, not a black line: only close up is its bed seen)
	vec3 col = mix(block, mix(block * 0.5, c0, 0.35 + 0.65 * fine), roads);
	// shadows, which are how a height reads from above. An island's skyline throws one long shadow over the low city on
	// its far side (the island's own depth looked up a shadow's length toward the sun) ...
	float sun_up = dot(d, sun_m);
	float tan_e = max(sun_up, 0.0) / sqrt(max(1.0 - sun_up * sun_up, 1e-4));
	vec3 lt = sun_m - d * sun_up;
	float low = length(lt);
	float reach = min(0.016 / max(tan_e, 0.15), 0.08);
	float shadow = clamp((pp_isle(normalize(d + lt / max(low, 1e-4) * reach)) - isle) * 1.1, 0.0, 1.0) * smoothstep(0.02, 0.2, low);
	// ... and close up every building throws its own over the roofs and streets behind it (four looks toward the sun
	// along the plots; none within a hair of a face's edge, where the plots' grid turns)
	float own = tall * (1.0 - roads);
	float sunlit = 0.0;
	if (fine > 0.0 && sun_up > 0.0) {
		vec3 cs = pp_cube(normalize(d + lt * 0.002));
		vec2 sd = cs.xy - cu.xy;
		float sl = length(sd);
		if (cs.z == face && sl > 1e-6) {
			sd /= sl;
			// (the sun never taken higher than about thirty degrees here, nor lower than seven: a shadow always has a
			// length to be read by, and never runs off past the looks)
			float tan_s = clamp(tan_e, 0.12, 0.55);
			float cast = 0.0;
			for (int i = 0; i < 6; i++) {
				float s = i == 0 ? 0.6 : (i == 1 ? 1.3 : (i == 2 ? 2.4 : (i == 3 ? 4.0 : (i == 4 ? 6.5 : 10.0))));
				cast = max(cast, smoothstep(0.0, 0.4, pp_city_roof(g2 + sd * s, face, isle) - own - s * tan_s));
			}
			shadow = max(shadow, cast * fine);
			// the sunward wall of a building that stands above what is in front of it, seen a little from the side
			sunlit = smoothstep(0.15, 1.2, own - pp_city_roof(g2 + sd * 0.2, face, isle)) * fine;
		}
	}
	col *= (1.0 - 0.78 * shadow) * (1.0 + 0.4 * sunlit * (1.0 - shadow));
	spec = 0.25 + 0.35 * isle_b;
	// (the islands stand as low hills of roofs, so the relief lights their sunward side from afar)
	rise = (1.0 - roads) * ((bh - 0.5) * 0.09 + (tall - 0.6 - isle_b * 3.1) * 0.02 * fine) + isle * 0.12;
	// the lights. From afar the whole night glows (lit windows, finer than a pixel, and the roads); close in, the glow
	// draws in to the streets, and a wide road is dark with threads of light down it, not a glowing plain
	// bright where the city is oldest and thickest and blazing in the islands, a thinner web between: never one even
	// sheet of light
	float lit = min(mix(0.05, 0.9, smoothstep(0.36, 0.7, dense)) * (0.4 + 1.2 * pn_fbm3(d * 12.0 + seed.yzx)) + isle_b * 0.9, 1.0);
	float windows = mix(0.3 * lit + 0.7 * step(1.0 - 0.6 * lit, pp_hash(floor(g2), face, 3.0)), 0.72 * lit, smoothstep(0.2, 0.7, fB * 8.0));
	float away = 1.0 - near * 0.8;
	float threads = max(pp_line(g.x, 0.004, fB), pp_line(g.y, 0.004, fB)) * (1.0 - inner);
	float av_threads = max(pp_line(gA.x, 0.003, fA), pp_line(gA.y, 0.003, fA));
	float built = windows * (0.26 + 0.2 * core + 0.7 * isle_b) * away + step(0.8, tower) * fine * 0.35 * away * lit + circ * plaza * 0.5
		+ (mix(street * 0.2 + (radial + diag) * 0.3, threads * 1.2 + (radial + diag) * 0.25, near) * mix(1.0, 0.5, near)
		+ split_lines * 0.4 + alley * 0.2) * (0.25 + 0.75 * lit);
	float highways = mix(avenue * 0.35, av_threads * 1.2, near) * mix(1.0, 0.5, near);
	emit += glow * (built + highways * max(lit, 0.25)) * (night * 0.5 + 0.01);
	return col;
}

// ---- 2: the ruins -----------------------------------------------------------------------------------------------------
// one ring work in the unit cell (q: -0.5 to 0.5; fw: a pixel in the cell's units; r: the cell's numbers): rings a tenth
// of the cell apart, twelve spokes between the first and the last, some chambers between them still roofed, a shaft at the
// hub. x: its walls, y: its roofs, z: the hub
vec3 pp_ring_work(vec2 q, float fw, vec3 r) {
	float rad = length(q);
	if (rad > 0.47) { return vec3(0.0); }
	float count = 2.0 + floor(r.x * 2.99);   // two to four rings
	float ri = clamp(floor((rad - 0.12) / 0.1 + 0.5), 0.0, count - 1.0);
	float last = 0.12 + 0.1 * (count - 1.0);
	float ang = mod(atan(q.y, q.x) + r.y * 6.2832, 6.2832);
	float between = step(0.12, rad) * step(rad, last);
	float sector = floor(ang * 1.9098593);   // twelve to the turn
	float ring_in = floor((rad - 0.12) / 0.1);
	float salt = r.z * 977.0;
	// each stretch of a ring between two spokes stands or has fallen, and each stretch of a spoke between two rings
	float wide = 0.009 * (0.7 + 0.6 * pn_hash31(vec3(sector, ri, salt + 3.0)));
	float walls = pp_band(abs(rad - (0.12 + 0.1 * ri)), wide, fw) * step(0.3, pn_hash31(vec3(sector, ri, salt + 1.0)));
	float spoke = floor(ang * 1.9098593 + 0.5);
	walls = max(walls, pp_band(abs(sin(ang * 6.0)) * rad / 6.0, 0.005, fw) * between * step(0.45, pn_hash31(vec3(spoke, ring_in, salt + 2.0))));
	// the chambers: each between two rings and two spokes, one in three still roofed
	float roofs = step(0.66, pn_hash31(vec3(sector, ring_in, salt))) * between * pp_fade(fw * 12.0);
	float hub = 1.0 - smoothstep(0.018, 0.045 + fw, rad);
	return vec3(walls, roofs, hub);
}

// clear: 0 where a crater or a rift has since cut the ground. Adds the lamps to emit and the walls' height to rise.
vec3 pp_ruins(vec3 d, vec3 cu, float pxc, vec3 col, float night, float clear, inout vec3 emit, out float rise) {
	float face = cu.z;
	rise = 0.0;
	// what still shows: a few broad regions where the dust has not covered it, and within them the drifts
	float stand = smoothstep(0.4, 0.6, pn_fbm3(d * 2.3 + seed.zxy)) * clear;
	float drift = pn_fbm3(d * 13.0 + seed);
	// (and every wall crumbled along its length, the gaps finer than the drifts)
	float crumble = mix(pn_vnoise3(d * 47.0 + seed.yzx) * 0.6 + pn_vnoise3(d * 131.0 + seed) * 0.4, 0.5, smoothstep(0.15, 0.5, pxc * 131.0));
	float show = smoothstep(0.42, 0.56, drift + 0.2 * stand) * smoothstep(0.3, 0.44, crumble + 0.08 * stand) * clear;
	float walls = 0.0, floors = 0.0, lamps = 0.0, pulse = 0.0;
	// the ring works: great ones half a face across, and lesser ones among them down to the size of a hill (drawn once
	// the camera is near enough to tell one from a speck)
	for (int i = 0; i < 4; i++) {
		float k = i == 0 ? 2.0 : (i == 1 ? 7.0 : (i == 2 ? 23.0 : 71.0));
		if (pxc * k > 0.25) { break; }
		vec2 gW = cu.xy * k;
		vec2 cell = floor(gW);
		vec3 r = pp_hash3(cell, face, 31.0 + float(i) * 3.0);
		if (pp_hash(cell, face, 32.0 + float(i) * 3.0) > (i == 0 ? 0.22 : 0.26)) { continue; }
		float size = mix(0.7, 1.0, r.z);
		vec2 q = (gW - cell - 0.5 - (r.xy - 0.5) * 0.08) / size;
		float fw = pxc * k / size;
		vec3 w = pp_ring_work(q, fw, r);
		walls = max(walls, w.x);
		floors = max(floors, w.y);
		rise += w.x * 0.3 / k;
		// the shaft at its hub is dark by day and still lit at night; one work in two still breathes, a slow pulse
		// running out along its rings
		lamps += step(0.4, r.x) * exp(-dot(q, q) / 0.0004) * (0.6 + 0.4 * sin(TIME * 0.9 + r.x * 40.0));
		pulse += w.x * step(r.y, 0.5) * pow(0.5 + 0.5 * sin(length(q) * 60.0 - TIME * 0.8 + r.z * 30.0), 8.0);
		col = mix(col, c0 * 0.4, w.z * clear * 0.8);
	}
	// the towns: a footprint with a ragged edge in about half the cells, its own grid turned its own way; plots with the
	// walls of a foundation standing, and inside the walls, close up, the rooms and then the stones
	vec2 gT = cu.xy * 5.0;
	vec2 tc = floor(gT);
	if (pp_hash(tc, face, 43.0) < 0.5) {
		vec3 r = pp_hash3(tc, face, 44.0);
		vec2 q = gT - tc - 0.5 - (r.xy - 0.5) * 0.1;
		float foot = 1.0 - smoothstep(0.28, 0.4, length(q) + 0.16 * (drift - 0.5));
		if (foot > 0.0) {
			float ang = floor(r.z * 4.0) * 0.3927;
			float ca = cos(ang), sa = sin(ang);
			vec2 g8 = vec2(ca * q.x + sa * q.y, ca * q.y - sa * q.x) * 9.0;
			float f8 = pxc * 45.0;
			float town = 0.0, plates = 0.0;
			float scale = 1.0;
			for (int i = 0; i < 3; i++) {
				float fL = f8 * scale;
				float fade = pp_fade(fL * 0.6);
				if (fade <= 0.0) { break; }
				vec2 gL = g8 * scale;
				vec2 pc = floor(gL);
				vec3 pr = pn_hash33(vec3(pc, r.z * 613.0 + float(i) * 5.0));
				// a plot in two holds a foundation: a box of walls of its own size, a gap where its door was
				vec2 f = gL - pc - 0.5;
				vec2 half_size = vec2(0.2 + 0.2 * pr.x, 0.2 + 0.2 * pr.y);
				vec2 bd2 = abs(f) - half_size;
				float bd = max(bd2.x, bd2.y);
				float has = step(pr.z, 0.5);
				float box = pp_band(abs(bd), 0.035, fL) * has;
				float street = max(pp_line(gL.x, 0.03, fL), pp_line(gL.y, 0.03, fL)) * 0.45;
				float weight = fade * (i == 0 ? 1.0 : 0.7);
				town = max(town, max(box, street) * weight);
				plates = max(plates, has * step(bd, 0.0) * step(0.6, pr.x + pr.y) * weight);
				// one foundation in thirty still has a lamp
				if (i == 0) { lamps += step(0.97, pn_hash31(vec3(pc, r.z * 211.0))) * has * exp(-dot(f, f) / 0.02) * foot; }
				rise += box * weight * 0.25 / (45.0 * scale);
				scale *= 8.0;
			}
			walls = max(walls, town * foot);
			floors = max(floors, plates * foot * 0.7);
		}
	}
	// the causeways between them: the lines of a coarse grid, each stretch standing or gone
	vec2 gC = cu.xy * 5.0;
	float fC = pxc * 5.0;
	float cx = pp_line(gC.x, 0.011, fC) * step(0.62, pp_hash(vec2(floor(gC.x + 0.5), floor(gC.y)), face, 37.0));
	float cy = pp_line(gC.y, 0.011, fC) * step(0.62, pp_hash(vec2(floor(gC.x), floor(gC.y + 0.5)), face, 41.0));
	float cause = max(cx, cy);
	walls = max(walls, cause * 0.8);
	rise += cause * 0.05;
	rise *= show;
	// the walls in the ground's own stone, dark with their own shade, stained by the metal still in them; the roofed
	// chambers and the floors a darker pavement
	vec3 metal = pattern_color * (0.7 + 0.6 * pn_vnoise3(d * 60.0 + seed.yzx));
	col = mix(col, col * 0.68 + metal * 0.08, floors * show);
	col = mix(col, mix(col * 0.5, metal, 0.4), walls * show);
	float flicker = 0.75 + 0.25 * sin(TIME * 11.0 + d.x * 90.0);
	emit += glow * (lamps * flicker * (night * 1.5 + 0.02) + pulse * show * (night * 0.45 + 0.01)) * clear;
	return col;
}

// ---- 3: the bones -----------------------------------------------------------------------------------------------------
void pp_bone(float dist, float w, float fw, inout float cov, inout float shade) {
	cov = max(cov, pp_band(dist, w, fw));
	shade = max(shade, 1.0 - smoothstep(w, w * 2.2 + 0.006 + fw, dist));
}
// one skeleton in the unit cell (q: -0.5 to 0.5; fw: a pixel in the cell's units; ra, rb: the cell's numbers), lying at
// its own place, heading, size and bend, and kept inside its cell: a spine of vertebrae thinning to the tail, nine pairs
// of ribs swept back and longest mid-chest, a long skull with its eye sockets, and on most a pair of fore and hind limbs.
// x: the bone's cover, y: the hollow of shade it lies in
vec2 pp_skeleton(vec2 q, float fw, vec3 ra, vec3 rb) {
	float size = mix(0.62, 1.0, ra.z);
	q -= (ra.xy - 0.5) * 0.14;
	float ang = rb.x * 6.2832;
	float ca = cos(ang), sa = sin(ang);
	q = vec2(ca * q.x + sa * q.y, ca * q.y - sa * q.x) / size;
	fw /= size;
	if (dot(q, q) > 0.2) { return vec2(0.0); }
	q.y -= (rb.y - 0.5) * 1.5 * (q.x * q.x - 0.06);
	float ay = abs(q.y);
	float cov = 0.0, shade = 0.0;
	// the spine, tail to neck
	float xs = clamp(q.x, -0.42, 0.24);
	float ws = mix(0.0025, 0.011, smoothstep(-0.42, 0.02, q.x));
	ws *= mix(1.0, 0.7 + 0.3 * cos(q.x * 282.74), pp_fade(fw * 90.0));
	pp_bone(length(vec2(q.x - xs, q.y)), ws, fw, cov, shade);
	// the ribs
	float sweep = 0.3 * ay + 1.1 * ay * ay;
	float ri = clamp(floor((q.x + sweep - 0.01) / 0.025 + 0.5), 0.0, 8.0);
	float rl = 0.16 * (0.4 + 0.6 * sin(3.1416 * (ri + 0.6) / 9.6));
	float slope = 0.3 + 2.2 * ay;
	float dx = abs(q.x + sweep - 0.01 - ri * 0.025) / sqrt(1.0 + slope * slope);
	pp_bone(length(vec2(dx, max(ay - rl, 0.0))), 0.0042 * (1.0 - 0.45 * min(ay / rl, 1.0)), fw, cov, shade);
	// the limbs
	if (rb.z > 0.3) {
		vec2 qa = vec2(q.x, ay);
		pp_bone(min(pp_seg(qa, vec2(0.2, 0.02), vec2(0.16, 0.19)), pp_seg(qa, vec2(0.16, 0.19), vec2(0.03, 0.27))), 0.0075, fw, cov, shade);
		pp_bone(min(pp_seg(qa, vec2(-0.1, 0.0), vec2(-0.15, 0.1)), pp_seg(qa, vec2(-0.15, 0.1), vec2(-0.26, 0.14))), 0.006, fw, cov, shade);
	}
	// (one in three carried tusks)
	if (ra.x > 0.66) {
		vec2 qa = vec2(q.x, ay);
		pp_bone(min(pp_seg(qa, vec2(0.35, 0.03), vec2(0.42, 0.075)), pp_seg(qa, vec2(0.42, 0.075), vec2(0.47, 0.06))), 0.005, fw, cov, shade);
	}
	// the skull, narrowing to the snout, its sockets dark
	vec2 sk = vec2(q.x - 0.325, ay);
	float ry = 0.05 * (1.0 - 0.45 * smoothstep(-0.03, 0.09, sk.x));
	float dk = (length(sk / vec2(0.09, ry)) - 1.0) * ry;
	float skull = 0.0;
	pp_bone(dk + 0.02, 0.02, fw, skull, shade);
	float socket = 1.0 - smoothstep(0.011 - fw, 0.011 + fw, length(sk - vec2(-0.03, 0.024)));
	cov = max(cov, skull * (1.0 - 0.85 * socket));
	return vec2(cov, shade);
}

// h: the ground's height. Gives the bones' height in rise.
vec3 pp_bones(vec3 d, vec3 cu, float pxc, float h, vec3 col, float clear, out float rise) {
	rise = 0.0;
	// the ground of a dead world, from orbit (Falchion's): grey dead forests, pale ash, white salt where the seas dried
	vec3 p = d * detail + seed;
	float forest = smoothstep(0.5, 0.68, pn_fbm3(d * 5.0 + seed.yzx));
	float salt = 1.0 - smoothstep(-0.42, -0.24, h);
	col = mix(col, c1 * 0.75, forest * 0.5);
	col = mix(col, vec3(0.86, 0.84, 0.78), salt * 0.7);
	col *= 0.9 + 0.15 * smoothstep(0.55, 0.95, ridged(p * 6.0));
	// where they lie thick: the bone fields, whole herds dead together
	float field = smoothstep(0.34, 0.62, pn_fbm3(d * 2.6 + seed.zxy));
	// seven sizes, each 3.4 times finer than the last (2, 7, 23, 79, 267, 909 and 3090 cells to the cube unit), the great
	// ones first and the small laid over them; a size is drawn once its cells are wider than about five pixels, and under
	// that its bones only pale the ground
	float meal = 0.0;
	for (int i = 0; i < 7; i++) {
		float k = floor(2.0 * pow(3.4, float(i)) + 0.5);
		float fw = pxc * k;
		float weight = (1.0 - smoothstep(0.1, 0.22, fw)) * clear;
		if (weight <= 0.0) { meal = 1.0; break; }
		vec2 g = cu.xy * k;
		vec2 cell = floor(g);
		float salt_i = 50.0 + float(i) * 3.0;
		if (pp_hash(cell, cu.z, salt_i) > mix(i == 0 ? 0.2 : 0.3, i == 0 ? 0.55 : 0.8, field)) { continue; }
		vec3 rb = pp_hash3(cell, cu.z, salt_i + 2.0);
		vec2 s = pp_skeleton(g - cell - 0.5, fw, pp_hash3(cell, cu.z, salt_i + 1.0), rb);
		if (s.y <= 0.0) { continue; }
		// half buried: drifts of ash lie over parts of every one
		float bare = mix(0.2, 1.0, smoothstep(0.3, 0.5, pn_vnoise3(d * k * 4.0 + seed)));
		float cov = s.x * weight * bare;
		// (old bone is browner; the freshest is the bone colour itself)
		vec3 bone = mix(pattern_color, c2, 0.4 * rb.y * rb.y) * (0.86 + 0.2 * pn_vnoise3(d * k * 40.0 + seed.yzx));
		col *= 1.0 - 0.32 * s.y * weight * (1.0 - cov);
		col = mix(col, bone, cov);
		rise += cov * 0.22 / k;
	}
	col = mix(col, pattern_color, 0.07 * field * meal * clear);
	return col;
}

// The pattern over the ground's colour. d: the unit direction; px: a pixel's size on the sphere (radians, the caller's,
// taken outside any branch); h: the ground's height; near: how close the camera is (0 to 1); night: 1 on the night side;
// clear: 0 where a crater or a rift has cut the ground since. Adds the pattern's own lights to emit, gives its height
// (the ground's units) in rise, and may set the glint (spec).
vec3 pp_surface(vec3 d, float px, float h, vec3 col, float near, float night, float clear, vec3 sun_m, inout vec3 emit, out float rise, inout float spec) {
	vec3 cu = pp_cube(d);
	float pxc = px * PP_PER_RAD;
	if (pattern == 1) { return pp_city(d, cu, pxc, h, near, night, sun_m, emit, rise, spec); }
	if (pattern == 2) { return pp_ruins(d, cu, pxc, col, night, clear, emit, rise); }
	return pp_bones(d, cu, pxc, h, col, clear, rise);
}
