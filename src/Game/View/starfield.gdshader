// The stand-in sky: hashed stars over a warm dust nebula, after the battle console mockup's backdrop (mockup 3, look
// dev, sets the real one).
shader_type sky;

uniform float density = 0.012;
uniform float scale = 260.0;

float hash(vec3 p) {
	p = fract(p * 0.3183099 + vec3(0.1, 0.2, 0.3));
	p *= 17.0;
	return fract(p.x * p.y * p.z * (p.x + p.y + p.z));
}

void sky() {
	vec3 d = normalize(EYEDIR);
	vec3 col = vec3(0.004, 0.006, 0.014) + vec3(0.01, 0.014, 0.03) * pow(1.0 - abs(d.y), 3.0);
	// a warm dust cloud: a few layers of smooth waves over the sphere, brightest toward the horizon
	float n = 0.0;
	n += 0.5 + 0.5 * sin(d.x * 3.1 + d.z * 2.3 + sin(d.y * 4.0 + d.x * 2.0));
	n += 0.5 * (0.5 + 0.5 * sin(d.z * 7.3 - d.x * 5.1 + sin(d.y * 9.0) * 1.5));
	n += 0.25 * (0.5 + 0.5 * sin(d.x * 15.0 + d.y * 13.0 - d.z * 11.0));
	n = smoothstep(0.55, 1.6, n) * (0.35 + 0.65 * pow(1.0 - abs(d.y), 2.0));
	col += vec3(0.20, 0.13, 0.08) * n;
	vec3 p = d * scale;
	vec3 c = floor(p);
	vec3 f = fract(p);
	float h = hash(c);
	if (h > 1.0 - density) {
		vec3 sp = vec3(hash(c + 1.3), hash(c + 2.7), hash(c + 5.1)) * 0.6 + 0.2;
		float dist = length(f - sp);
		float b = smoothstep(0.16, 0.0, dist) * (0.35 + 0.65 * (h - (1.0 - density)) / density);
		vec3 tint = mix(vec3(0.72, 0.82, 1.0), vec3(1.0, 0.9, 0.78), hash(c + 9.0));
		col += b * tint * 1.6;
	}
	COLOR = col;
}
