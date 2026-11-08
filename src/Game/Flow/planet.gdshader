// A planet on the paper chart, drawn live: a port of docs/mockups/lib/planet-shader.js (its FRAG and VERT), which is the one
// design; change that file first and this to match (CLAUDE.md section 4, "The game stays in step with its mockups"). Our
// own code with the features of Francisco Cidade's CC0 Pixel Planet Godot shader: six kinds of world spinning slowly, its
// light in flat bands, clouds at twice the ground's speed throwing shadows, polar caps, an axis tilt, a ring in the holder's
// colour, a rim of atmosphere; finer cells than the original (220 across the disc) and the night side cross-hatched in sepia
// ink band by band instead of dithered, coastlines and limb inked. The noise is value noise over a lattice of Mulberry32
// numbers (ChartRoom.Lattice), read with the two-channel trick. The page's colours are sRGB and so are these: the result is
// turned to linear light once at the end.
shader_type spatial;
render_mode unshaded, cull_disabled, skip_vertex_transform, depth_draw_opaque;

uniform sampler2D lat : filter_linear, repeat_enable;
uniform float time_s = 0.0;
uniform float pixels = 220.0;
uniform float steps = 5.0;
uniform float seed = 0.0;
uniform float tilt = 0.0;
uniform float spin = 0.12;
uniform float kind = 0.0;   // 0 terran, 1 desert, 2 ice, 3 lava, 4 gas giant, 5 moon
uniform float ring = 0.0;
uniform float clouds = 0.0;
uniform float land = 0.5;
uniform float caps = 0.0;
uniform float width = 1.0;  // the quad's width, map units: its foot (bottom edge) at the node's origin
uniform vec3 g0; uniform vec3 g1; uniform vec3 g2; uniform vec3 g3;
uniform vec3 s0; uniform vec3 s1; uniform vec3 s2; uniform vec3 s3;
uniform vec3 b0; uniform vec3 b1; uniform vec3 b2; uniform vec3 b3; uniform vec3 b4; uniform vec3 b5;
uniform vec3 cloud_col = vec3(0.957, 0.937, 0.886);
uniform vec3 atmo = vec3(0.0);
uniform vec3 ring_col = vec3(0.5);
uniform vec3 glow_col = vec3(0.0);
uniform vec3 ink_col = vec3(0.165, 0.106, 0.063);

void vertex() {
	// a billboard: the quad laid in the view plane at the node's origin, its foot there
	vec4 mv = MODELVIEW_MATRIX * vec4(0.0, 0.0, 0.0, 1.0);
	mv.xy += vec2(VERTEX.x, VERTEX.y + 0.5) * width;
	VERTEX = mv.xyz;
	NORMAL = vec3(0.0, 0.0, 1.0);
}

float vn(vec3 x) {
	vec3 p = floor(x), f = fract(x); f = f * f * (3.0 - 2.0 * f);
	vec2 uv = (p.xy + vec2(37.0, 17.0) * p.z) + f.xy;
	vec2 rg = texture(lat, (uv + 0.5) / 256.0).yx;
	return mix(rg.x, rg.y, f.z);
}
float fbm(vec3 p) { float v = 0.0, a = 0.5; for (int i = 0; i < 5; i++) { v += a * vn(p); p = p * 2.03 + vec3(1.7, 9.2, 4.1); a *= 0.5; } return v; }
vec3 surf(vec2 q, float turn) {
	float z = sqrt(max(0.0, 1.0 - dot(q, q)));
	vec3 n = vec3(q, z);
	float c = cos(turn), s = sin(turn);
	return vec3(c * n.x + s * n.z, n.y, -s * n.x + c * n.z);
}
float lines(vec2 px, float a, float k, float lw) { float d = dot(px, vec2(cos(a), sin(a))); return step(abs(fract(d / k) - 0.5) * k, lw); }
vec3 pick4(float t, vec3 c0, vec3 c1, vec3 c2, vec3 c3) { return t < 0.25 ? c0 : t < 0.5 ? c1 : t < 0.75 ? c2 : c3; }

void fragment() {
	vec2 vuv = vec2(UV.x, 1.0 - UV.y);   // the page's uvs run up
	vec2 grid = floor(vuv * pixels * 2.0) / (pixels * 2.0) + 0.5 / (pixels * 2.0);
	vec2 p = (grid - 0.5) * 4.0;
	vec2 px = grid * pixels * 2.0;
	float ct = cos(tilt), st = sin(tilt);
	vec2 q = vec2(ct * p.x + st * p.y, -st * p.x + ct * p.y);
	vec3 L = normalize(vec3(-0.55, 0.5, 0.67));
	float r = length(p);
	vec4 col = vec4(0.0);
	float ringR = length(vec2(q.x, q.y / 0.32));
	bool ringHit = ring > 0.5 && ringR > 1.42 && ringR < 1.82;
	bool behind = q.y > 0.0;
	if (r < 1.0) {
		vec3 n = vec3(p, sqrt(1.0 - r * r));
		float lit = clamp(dot(n, L), 0.0, 1.0);
		vec3 sp = surf(q, time_s * spin + seed);
		vec3 sc = surf(q, time_s * spin * 2.0 + seed * 1.7);
		float h = fbm(sp * 2.2 + seed);
		vec3 base; float coast = 0.0, glow = 0.0;
		float latd = q.y;
		if (kind < 0.5) {
			float t = clamp((h - land) / 0.25, 0.0, 1.0);
			base = h > land ? pick4(t * 0.99, g0, g1, g2, g3) : pick4(clamp((land - h) / 0.2, 0.0, 0.99), s0, s1, s2, s3);
			coast = 1.0 - smoothstep(0.0, 0.012, abs(h - land));
		} else if (kind < 1.5) {
			float d = fbm(sp * vec3(1.0, 6.0, 1.0) * 2.0 + seed) * 0.6 + h * 0.4;
			base = pick4(clamp(1.0 - d, 0.0, 0.99), g0, g1, g2, g3);
		} else if (kind < 2.5) {
			base = pick4(clamp(1.0 - h * 1.1, 0.0, 0.99), g0, g1, g2, g3);
			coast = 1.0 - smoothstep(0.0, 0.01, abs(fbm(sp * 4.0 + 3.0) - 0.5));
		} else if (kind < 3.5) {
			base = pick4(clamp(1.0 - h, 0.0, 0.99), g0, g1, g2, g3);
			glow = 1.0 - smoothstep(0.0, 0.03, abs(fbm(sp * 3.0 + 7.0) - 0.5));
		} else if (kind < 4.5) {
			float b = sp.y * 3.0 + fbm(sp * vec3(2.0, 0.5, 2.0) * 2.0 + seed) * 1.4;
			float i = mod(floor(b * 2.0), 6.0);
			base = i < 0.5 ? b0 : i < 1.5 ? b1 : i < 2.5 ? b2 : i < 3.5 ? b3 : i < 4.5 ? b4 : b5;
			coast = 1.0 - smoothstep(0.0, 0.04, 0.5 - abs(fract(b * 2.0) - 0.5));
		} else {
			base = pick4(clamp(1.0 - h, 0.0, 0.99), g0, g1, g2, g3);
			vec3 cp = sp * 4.0; vec3 cf = fract(cp) - 0.5; float cr = vn(floor(cp) * 0.37 + 11.0);
			float cd = length(cf) / (0.15 + cr * 0.3);
			if (cr > 0.45 && cd < 1.0) { base *= 0.78; coast = step(0.8, cd); }
		}
		if (caps > 0.5 && abs(latd) > 0.78 - 0.12 * fbm(sp * 5.0)) { base = mix(base, vec3(0.95, 0.97, 1.0), 0.9); }
		float cl = 0.0, csh = 0.0;
		if (clouds > 0.0) {
			cl = smoothstep(0.62 - clouds * 0.2, 0.7 - clouds * 0.2, fbm(sc * 3.0 + 5.0));
			vec3 so = surf(q + L.xy * -0.05, time_s * spin * 2.0 + seed * 1.7);
			csh = smoothstep(0.62 - clouds * 0.2, 0.7 - clouds * 0.2, fbm(so * 3.0 + 5.0)) * (1.0 - cl);
		}
		float band = floor(lit * steps) / steps;
		float shade = 0.55 + 0.45 * band;
		vec3 c = base * shade * (1.0 - 0.35 * csh);
		c = mix(c, cloud_col * (0.6 + 0.4 * band), cl);
		c += glow_col * glow * 1.2;
		float dark = 1.0 - band;
		float ink = 0.0;
		if (dark > 0.35) ink = max(ink, lines(px, 0.785, 4.0, 0.55));
		if (dark > 0.55) ink = max(ink, lines(px, -0.785, 4.0, 0.55));
		if (dark > 0.8) ink = max(ink, lines(px, 0.0, 3.0, 0.5));
		ink *= 1.0 - glow;
		c = mix(c, ink_col, ink * 0.62);
		c = mix(c, ink_col, coast * (1.0 - cl) * 0.8);
		float pen = 0.06 + 0.02 * (vn(vec3(atan(p.y, p.x) * 3.0, seed, 0.0)) - 0.5);
		c = mix(c, ink_col, smoothstep(1.0 - pen, 1.0 - pen * 0.5, r));
		col = vec4(c, 1.0);
	} else if (r < 1.12 && atmo.r + atmo.g + atmo.b > 0.0) {
		vec2 d = p / r; float side = clamp(dot(vec3(d, 0.2), L) * 1.5 + 0.2, 0.0, 1.0);
		float a = (1.0 - (r - 1.0) / 0.12) * side * 0.65;
		col = vec4(atmo, a * (1.0 - 0.5 * lines(px, 0.785, 3.5, 0.55)));
	}
	if (ringHit && (!behind || r >= 1.0)) {
		float e = min(ringR - 1.42, 1.82 - ringR);
		vec3 rc = mix(ring_col, vec3(0.93, 0.87, 0.74), 0.3) * (behind ? 0.72 : 1.0);
		float shadowed = (!behind && q.x > 0.0 && abs(q.x) < 1.0) ? 1.0 : 0.0;
		rc = mix(rc, ink_col, lines(px, -0.785, 3.5, 0.55) * (0.2 + 0.55 * shadowed));
		rc = mix(rc, ink_col, 1.0 - smoothstep(0.0, 0.05, e));
		rc = mix(rc, ink_col, (1.0 - smoothstep(0.0, 0.018, abs(ringR - 1.6))) * 0.7);
		col = vec4(rc, 1.0);
	}
	if (col.a <= 0.01) discard;
	ALBEDO = pow(col.rgb, vec3(2.2));
	ALPHA = col.a;
}
