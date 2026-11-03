// The stand-in sky: hashed stars over a faint blue haze (mockup 3, look dev, sets the real one).
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
