// nebula_cloud.gdshaderinc: how a nebula's cloud is made from the shared 3D noise and how a sample of it is lit, the one
// implementation of both (CLAUDE.md 6.1) for every nebula shader: a plume's column (nebula_plume.gdshader) and the decks
// it rises between (nebula_deck.gdshader). After the real-time cloud renderers (Schneider and Vos, "The Real-time
// Volumetric Cloudscapes of Horizon Zero Dawn", SIGGRAPH 2015): the billows are the noise's Perlin-Worley channel lifted
// by its Worley octaves and cut by a cover (how far inside the shape a point is), their edges eaten by the Worley channels
// at a finer scale; a sample is lit by one sun through the cloud between (Beer's law, with a slower fall for light
// scattered many times), by the cloud's own dim light and by the glow of its thin edges. The noise is
// assets/space/plume_noise.bin (tools/space/plume_noise.py).

// The cloud's body at a point, 0 to 1, from the noise there and the cover: solid in the heart, billows at the edge.
float nb_body(vec4 n, float cover) {
	float low = n.g * 0.625 + n.b * 0.25 + n.a * 0.125;
	float body = clamp((n.r - (low - 1.0)) / (2.0 - low), 0.0, 1.0);
	return clamp((body - (1.0 - cover)) / max(cover, 1.0e-3), 0.0, 1.0);
}

// The body's thin edge eaten by the wisps (m: the noise at the finer scale; the two coarser channels only, since wisps
// finer than a step of the march show as grain).
float nb_erode(float d, vec4 m, float erosion) {
	float e = erosion * (1.0 - (m.g * 0.75 + m.b * 0.25)) * (1.0 - smoothstep(0.0, 0.6, d));
	return clamp((d - e) / (1.0 - e), 0.0, 1.0);
}

// How much of the cloud's density survives at a point, by a broader noise (v): dense cores, thin veils and holes, so the
// cloud is not one even fill behind its billowed skin (owner, 2026-11-10: "the nebula cloud column has uniform density, so
// its not something convincing"). contrast 0 is an even fill, 1 cuts holes right through.
float nb_vary(vec4 v, float contrast) {
	return mix(1.0, smoothstep(0.25, 0.72, v.r * 0.65 + v.g * 0.35), contrast);
}

// The sun's strength on a cloud: never quite dark on the night side of a battle.
float nb_sun_k(float sun_energy) {
	return 0.35 + min(sun_energy, 1.5);
}

// A silver lining: the cloud between the camera and the sun scatters forward.
float nb_phase(vec3 rd, vec3 sun_l) {
	return 0.8 + 1.1 * pow(max(dot(rd, sun_l), 0.0), 6.0);
}

// How much sunlight reaches a sample through shade (the cloud toward the sun, density x distance), and less out of a thin
// edge (soft, the body before its edge was drawn crisp) than out of a full billow, so creases between billows read.
float nb_sun_through(float shade, float absorption, float soft) {
	float t = max(exp(-absorption * shade), 0.24 * exp(-absorption * 0.3 * shade));
	return t * mix(1.0, smoothstep(0.0, 0.7, soft), 0.45);
}

// A sample's light: its own dim light (deep, brighter toward its sunward side: height 0 to 1), the sun's on it (lit, drifting
// toward glow by tint), and the glow of its thin edges; open dims the bays that lie deeper in the shape.
vec3 nb_light(vec3 deep, vec3 lit, vec3 glow, float glow_strength, float height, float open, float tint, float sun_k,
		float phase, float sun_t, float soft) {
	float thin = 1.0 - smoothstep(0.0, 0.8, soft);
	return deep * (0.4 + 0.8 * height) * open
		+ mix(lit, glow, 0.45 * smoothstep(0.35, 0.75, tint)) * sun_k * phase * sun_t * open * (0.7 + 0.4 * height)
		+ glow * glow_strength * thin * thin;
}

// The distance along the camera's ray to the nearest solid at this pixel (the depth texture), in view units; a march stops
// there.
float nb_solid(sampler2D depth_tex, vec2 screen_uv, mat4 inv_projection) {
	float depth = texture(depth_tex, screen_uv).x;
#if CURRENT_RENDERER == RENDERER_COMPATIBILITY
	vec3 ndc = vec3(screen_uv, depth) * 2.0 - 1.0;
#else
	vec3 ndc = vec3(screen_uv * 2.0 - 1.0, depth);
#endif
	vec4 view = inv_projection * vec4(ndc, 1.0);
	return abs(view.w) > 1.0e-9 ? length(view.xyz / view.w) : 1.0e9;
}
