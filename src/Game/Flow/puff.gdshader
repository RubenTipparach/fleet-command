// puff.gdshader: the campaign map's nebula puffs (game-flow mockup v15, its puffMat; Crab Breeze's cloud billboards):
// one camera-facing billboard per instance of a MultiMesh, its edge eaten by noise, lit as a sphere from uLight and
// glowing from its core like gas. Instance colour is the puff's tint (linear) and alpha; INSTANCE_CUSTOM is (rx, ry, seed).
// Drawn back to front by CampaignMap, which sorts the instances when the camera moves.
shader_type spatial;
render_mode unshaded, blend_mix, depth_draw_never, cull_disabled, shadows_disabled;

uniform vec3 light_dir = vec3(-0.4, 0.8, 0.45);
varying vec2 v_uv;
varying vec4 v_tint;
varying float v_seed;

// the value noise's lattice hash: PCG on the integer cell (CLAUDE.md section 10: never fract(sin) or a fract product,
// which loses its precision far from the origin and drew the clouds in blocks; owner, 2026-11-07: "need to fix pixely
// clouds")
uint pcg(uint v) { uint s = v * 747796405u + 2891336453u; uint w = ((s >> ((s >> 28u) + 4u)) ^ s) * 277803737u; return (w >> 22u) ^ w; }
float h21(ivec2 c) { return float(pcg(uint(c.x) * 1597334677u ^ pcg(uint(c.y)))) * (1.0 / 4294967295.0); }
float vn(vec2 p) {
	ivec2 i = ivec2(floor(p)); vec2 f = fract(p);
	f = f * f * (3.0 - 2.0 * f);
	return mix(mix(h21(i), h21(i + ivec2(1, 0)), f.x), mix(h21(i + ivec2(0, 1)), h21(i + ivec2(1, 1)), f.x), f.y);
}
float fbm2(vec2 p) { return 0.5 * vn(p) + 0.3 * vn(p * 2.1 + 3.7) + 0.2 * vn(p * 4.3 + 8.1); }

void vertex() {
	v_uv = VERTEX.xy;
	v_tint = COLOR;
	v_seed = INSTANCE_CUSTOM.z;
	vec3 c = MODEL_MATRIX[3].xyz;
	vec3 right = INV_VIEW_MATRIX[0].xyz, up = INV_VIEW_MATRIX[1].xyz;
	float k = abs(normalize(CAMERA_POSITION_WORLD - c).y);
	vec3 wp = c + right * VERTEX.x * INSTANCE_CUSTOM.x + up * VERTEX.y * mix(INSTANCE_CUSTOM.y, INSTANCE_CUSTOM.x, k * k);
	POSITION = PROJECTION_MATRIX * VIEW_MATRIX * vec4(wp, 1.0);
}

void fragment() {
	vec2 p = v_uv;
	float d = length(p);
	if (d > 1.0) discard;
	// each puff its own patch of the noise, the offset kept small so the noise keeps its precision
	vec2 off = vec2(fract(v_seed * 0.6180339), fract(v_seed * 0.7548777)) * 97.0;
	float n = fbm2(p * 2.2 + off), n2 = vn(p * 7.0 + off.yx);
	float dens = smoothstep(1.0, 0.4, d + (n - 0.5) * 0.7 + (n2 - 0.5) * 0.22);
	if (dens < 0.01) discard;
	vec3 right = INV_VIEW_MATRIX[0].xyz, up = INV_VIEW_MATRIX[1].xyz, back = INV_VIEW_MATRIX[2].xyz;
	float z = sqrt(max(0.0, 1.0 - d * d));
	vec3 nrm = normalize(right * p.x + up * p.y + back * (z + 0.15) + 0.5 * (vec3(n, n2, n) - 0.5));
	float diffuse = clamp((dot(nrm, normalize(light_dir)) + 0.6) / 1.6, 0.0, 1.0);
	vec3 col = v_tint.rgb * (0.18 + 0.62 * diffuse) + v_tint.rgb * 0.55 * z * z * (0.6 + 0.4 * n);
	ALBEDO = col;
	ALPHA = dens * v_tint.a;
}
