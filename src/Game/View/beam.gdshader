// beam.gdshader: a shot drawn as a camera-facing strip between two points, the game's port of the weapons range's
// ribbon (docs/mockups/lib/weapon-fx.js, RIBBON_FS: lance, bolt, tracer; change weapon-fx). The strip's mesh is a flat
// grid (UV.x across 0..1, UV.y along 0..1); the vertex function lays it from point_a to point_b, turned about its axis
// to face the camera, so it reads as a round column from any side. Both points are set every frame by ShotView from the
// simulation's Battle.Ends, so a beam's ends stay on their ships (owner, 2026-11-04). No weapon is drawn white (owner:
// "please avoid the color white for weapons"): a hot core is the weapon's own colour at full strength (hot_of).
// Modes: 0 lance (a core in the weapon's colour, flowing light, pulses racing to the target), 1 bolt (a disruptor bolt:
// narrow and dim behind, a hot head), 2 tracer (point defence cannon rounds racing from the mount), 3 chorus (the chorus
// lance: a core in Sky with flowing light and Lilac strands winding round it, two plus one for each ship that joined).
shader_type spatial;
render_mode unshaded, blend_add, depth_draw_never, cull_disabled, shadows_disabled, fog_disabled;

uniform vec3 point_a;
uniform vec3 point_b;
uniform float width = 0.3;
uniform float intensity = 0.0;
uniform vec4 color : source_color = vec4(0.44, 0.77, 1.0, 1.0);
uniform int mode = 0;
uniform float time_s = 0.0;
uniform vec4 color2 : source_color = vec4(0.72, 0.65, 0.95, 1.0);
uniform float strands = 2.0;

varying float v_x;
varying float v_s;
varying float v_len;

float h3(vec3 p) { p = fract(p * 0.3183099 + 0.1); p *= 17.0; return fract(p.x * p.y * p.z * (p.x + p.y + p.z)); }
float n3(vec3 x) {
	vec3 i = floor(x), f = fract(x); f = f * f * (3.0 - 2.0 * f);
	return mix(mix(mix(h3(i), h3(i + vec3(1, 0, 0)), f.x), mix(h3(i + vec3(0, 1, 0)), h3(i + vec3(1, 1, 0)), f.x), f.y),
		mix(mix(h3(i + vec3(0, 0, 1)), h3(i + vec3(1, 0, 1)), f.x), mix(h3(i + vec3(0, 1, 1)), h3(i + vec3(1, 1, 1)), f.x), f.y), f.z);
}
float fbm(vec3 p) { float s = 0.0, a = 0.5; for (int i = 0; i < 4; i++) { s += a * n3(p); p = p * 2.03 + vec3(1.7, 9.2, 3.1); a *= 0.5; } return s; }
vec3 hot_of(vec3 c) { return c / max(max(c.r, c.g), max(c.b, 0.001)); }

void vertex() {
	float k = UV.y;
	vec3 p = mix(point_a, point_b, k);
	vec3 ax = point_b - point_a;
	ax = length(ax) > 1e-5 ? normalize(ax) : vec3(0.0, 0.0, 1.0);
	vec3 cam = INV_VIEW_MATRIX[3].xyz;
	vec3 side = cross(ax, normalize(cam - p));
	side = length(side) > 1e-5 ? normalize(side) : vec3(1.0, 0.0, 0.0);
	p += side * (UV.x - 0.5) * width;
	v_x = (UV.x - 0.5) * 2.0;
	v_len = length(point_b - point_a);
	v_s = k * v_len;
	POSITION = PROJECTION_MATRIX * VIEW_MATRIX * vec4(p, 1.0);
}

void fragment() {
	float x = v_x, s = v_s;
	vec3 c = vec3(0.0);
	if (mode == 0) {
		float core = exp(-x * x * 80.0), glow = exp(-abs(x) * 4.0) * 0.6 + exp(-abs(x) * 1.6) * 0.12;
		float flow = fbm(vec3(s * 2.4 - time_s * 14.0, x * 3.0, 1.7));
		float pulse = pow(0.5 + 0.5 * sin(s * 5.0 - time_s * 70.0), 6.0);
		c = color.rgb * glow * (1.1 + 1.3 * flow + 0.8 * pulse) + hot_of(color.rgb) * core * (1.6 + 0.8 * flow);
	} else if (mode == 1) {
		float k = clamp(s / max(v_len, 1e-4), 0.0, 1.0);
		float w = mix(0.15, 1.0, k * k), xx = x / max(w, 0.05);
		float core = exp(-xx * xx * 18.0) * k * k * k, glow = exp(-abs(xx) * 3.0) * k * k;
		float flick = 0.8 + 0.4 * n3(vec3(s * 12.0 - time_s * 40.0, x * 2.0, 4.0));
		c = color.rgb * glow * 1.6 * flick + hot_of(color.rgb) * core * 2.6;
	} else if (mode == 3) {
		float core = exp(-x * x * 90.0), glow = exp(-abs(x) * 4.2) * 0.55 + exp(-abs(x) * 1.4) * 0.12;
		float flow = fbm(vec3(s * 1.8 - time_s * 9.0, x * 3.0, time_s * 0.7));
		c = color.rgb * glow * (1.3 + 1.4 * flow) + hot_of(color.rgb) * core * (1.5 + flow);
		float st = 0.0;
		for (int i = 0; i < 5; i++) {
			if (float(i) >= strands) break;
			float ph = float(i) * 6.2831853 / strands, w = s * 3.0 - time_s * 11.0 + ph;
			st += exp(-pow((x - 0.42 * sin(w)) / 0.07, 2.0)) * (0.55 + 0.45 * cos(w));
		}
		c += color2.rgb * st * 2.4 * (0.7 + 0.6 * flow);
	} else {
		float per = width * 9.0, u = fract(s / per - time_s * 6.0);
		float dash = smoothstep(0.0, 0.06, u) * (1.0 - smoothstep(0.22, 0.32, u));
		float core = exp(-x * x * 28.0), glow = exp(-abs(x) * 3.0) * 0.5;
		c = (hot_of(color.rgb) * core * 2.2 + color.rgb * glow) * dash;
	}
	float ends = smoothstep(0.0, width * 0.6, s) * (1.0 - smoothstep(v_len - width * 0.3, v_len, s));
	ALBEDO = c * intensity * ends * (1.0 - smoothstep(0.82, 1.0, abs(x)));
}
