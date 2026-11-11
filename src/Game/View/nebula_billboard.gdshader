// nebula_billboard.gdshader: a nebula puff as one camera-facing quad (NebulaPuffs, the "billboards" style; owner,
// 2026-11-10: "third technique to try out: crab-breezes cloud system", "cheap 3d billboards with lighting"). Crab
// Breeze's cloud billboards (RubenTipparach/crab-breeze at 5ad3de22, assets/shaders/cloud.wesl), ported: each quad faces
// the eye's position and stays upright, so turning the camera leaves the cloud where it is and only moving past a puff
// turns it; straight above or below a puff its right is east laid onto its plane. Its edge is eaten by two scales of the
// shared nebula noise, it is lit as a sphere (nebula_puff.gdshaderinc), and it fades where it meets a ship or a planet
// behind it (the depth texture: soft particles) and near the eye. One MultiMesh of every puff, sorted far to near on the
// CPU, one draw. The engine's fog hazes far ones, as it does the ships.
shader_type spatial;
render_mode unshaded, blend_mix, depth_draw_never, cull_disabled, shadows_disabled;

#include "nebula_puff.gdshaderinc"

uniform sampler3D noise_tex : repeat_enable, filter_linear;
uniform sampler2D depth_texture : hint_depth_texture, repeat_disable, filter_nearest;
uniform vec3 sun_dir = vec3(0.4, 0.7, 0.6);
uniform float sun_energy = 0.65;
uniform float underside = 18.0;   // the decks' face off the plane, units (puff_seen)
uniform float erode = 0.55;       // how deep the noise bites into a puff's edge
uniform float soft = 1.5;         // the depth over which a puff fades in front of a ship or planet, units
uniform float near_fade = 3.0;    // a puff fades out within this of the eye, units
uniform float puff_time = 0.0;

varying vec2 v_uv;
varying vec3 v_centre;
varying vec3 v_right;
varying vec3 v_up;
varying vec3 v_back;
varying vec3 v_deep;
varying float v_side;

// The puff's frame: facing the eye's position, upright. Columns: right, up, back toward the eye.
mat3 facing(vec3 c, vec3 eye) {
	vec3 back = normalize(eye - c + vec3(0.0, 1.0e-3, 0.0));
	vec3 flat_right = cross(vec3(0.0, 1.0, 0.0), back);
	vec3 east = vec3(1.0, 0.0, 0.0) - back * back.x;
	if (dot(east, flat_right) < 0.0) east = -east;
	vec3 right = normalize(mix(east, flat_right, smoothstep(0.02, 0.2, length(flat_right))));
	return mat3(right, cross(back, right), back);
}

void vertex() {
	vec3 c = (MODEL_MATRIX * vec4(0.0, 0.0, 0.0, 1.0)).xyz;
	float r = length(MODEL_MATRIX[0].xyz);
	vec3 eye = CAMERA_POSITION_WORLD;
	mat3 f = facing(c, eye);
	vec3 wp = c + f[0] * VERTEX.x * r + f[1] * VERTEX.y * r;
	POSITION = PROJECTION_MATRIX * VIEW_MATRIX * vec4(wp, 1.0);
	v_uv = VERTEX.xy;
	v_centre = c;
	v_right = f[0];
	v_up = f[1];
	v_back = f[2];
	v_deep = INSTANCE_CUSTOM.rgb;
	v_side = INSTANCE_CUSTOM.a;
}

void fragment() {
	vec2 p = v_uv;
	float d = length(p);
	if (d >= 1.0) discard;
	// two scales of the noise in the quad's plane, each puff cut from its own place
	vec3 at = v_centre * 0.031;
	vec4 n4 = textureLod(noise_tex, vec3(p * 0.42, 0.0) + at + vec3(puff_time * 0.002, 0.0, 0.0), 0.0);
	float n = n4.g * 0.6 + n4.b * 0.4;
	float n2 = textureLod(noise_tex, vec3(p * 1.3, 0.5) + at.zxy, 0.0).g;
	float dens = smoothstep(1.0, 0.45, d + (n - 0.5) * erode + (n2 - 0.5) * 0.2) * smoothstep(1.0, 0.8, d);
	float z = sqrt(max(0.0, 1.0 - d * d));
	vec3 nrm = normalize(v_right * p.x + v_up * p.y + v_back * (z + 0.15) + 0.5 * (vec3(n, n2, n) - 0.5));
	vec3 look = normalize(v_centre - CAMERA_POSITION_WORLD);
	float under = smoothstep(-0.95, 0.55, p.y + (n - 0.5) * 0.4);
	vec3 col = puff_light(nrm, normalize(sun_dir), sun_energy, under, 1.0 - z, look, COLOR.rgb, v_deep);
	// soft against whatever stands behind it, faded near the eye
	float depth = texture(depth_texture, SCREEN_UV).x;
#if CURRENT_RENDERER == RENDERER_COMPATIBILITY
	vec3 ndc = vec3(SCREEN_UV, depth) * 2.0 - 1.0;
#else
	vec3 ndc = vec3(SCREEN_UV * 2.0 - 1.0, depth);
#endif
	vec4 view = INV_PROJECTION_MATRIX * vec4(ndc, 1.0);
	float scene = length(view.xyz / view.w);
	float here = length(VERTEX);
	float a = dens * COLOR.a * smoothstep(0.0, soft, scene - here) * smoothstep(near_fade * 0.3, near_fade, here);
	a *= puff_seen(v_side, CAMERA_POSITION_WORLD.y, underside);
	if (a < 0.003) discard;
	ALBEDO = col;
	ALPHA = a;
}
