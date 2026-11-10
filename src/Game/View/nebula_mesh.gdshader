// nebula_mesh.gdshader: a nebula puff as a mesh (NebulaPuffs, the "meshes" style; owner, 2026-11-10: "either the deck
// would have to be clouds too or everything is geometry. lets try both"). One shared ball, its surface pushed in and out
// by the shared nebula noise where it sits (so no two puffs are alike and neighbours' lumps do not line up), lit as a
// cloud puff (nebula_puff.gdshaderinc, the billboards' light) on its own lumpy normal, its silhouette feathered so it
// reads soft. One MultiMesh of every puff, sorted far to near on the CPU, drawn with a depth prepass so a puff's near
// side hides its far side. The engine's fog hazes far ones.
shader_type spatial;
render_mode unshaded, blend_mix, depth_prepass_alpha, cull_back, shadows_disabled;

#include "nebula_puff.gdshaderinc"

uniform sampler3D noise_tex : repeat_enable, filter_linear;
uniform vec3 sun_dir = vec3(0.4, 0.7, 0.6);
uniform float sun_energy = 0.65;
uniform float underside = 18.0;
uniform float lump = 0.45;        // how far the noise pushes the surface in and out, a share of the radius
uniform float feather = 0.5;      // how soft the silhouette is (0 a hard ball edge)
uniform float near_fade = 3.0;

varying vec3 v_local;
varying vec3 v_wnrm;
varying vec3 v_deep;
varying float v_side;

float lump_at(vec3 dir, vec3 c) {
	vec4 n = textureLod(noise_tex, dir * 0.35 + c * 0.031, 0.0);
	return (n.r * 0.6 + n.g * 0.4 - 0.5) * lump;
}

void vertex() {
	vec3 c = (MODEL_MATRIX * vec4(0.0, 0.0, 0.0, 1.0)).xyz;
	vec3 dir = normalize(VERTEX);
	float h = lump_at(dir, c);
	// the normal from the lumps' slope: the height a little way along two tangents
	vec3 t1 = normalize(cross(dir, abs(dir.y) < 0.9 ? vec3(0.0, 1.0, 0.0) : vec3(1.0, 0.0, 0.0)));
	vec3 t2 = cross(dir, t1);
	float e = 0.12;
	vec3 pa = normalize(dir + t1 * e) * (1.0 + lump_at(normalize(dir + t1 * e), c));
	vec3 pb = normalize(dir + t2 * e) * (1.0 + lump_at(normalize(dir + t2 * e), c));
	vec3 p0 = dir * (1.0 + h);
	vec3 nrm = normalize(cross(pa - p0, pb - p0));
	if (dot(nrm, dir) < 0.0) nrm = -nrm;
	VERTEX = p0;
	v_local = p0;
	v_wnrm = normalize((MODEL_MATRIX * vec4(nrm, 0.0)).xyz);
	NORMAL = nrm;
	v_deep = INSTANCE_CUSTOM.rgb;
	v_side = INSTANCE_CUSTOM.a;
}

void fragment() {
	vec3 wpos = (INV_VIEW_MATRIX * vec4(VERTEX, 1.0)).xyz;
	vec3 look = normalize(wpos - CAMERA_POSITION_WORLD);
	float facing_eye = clamp(dot(v_wnrm, -look), 0.0, 1.0);
	float under = smoothstep(-0.95, 0.6, v_local.y);
	vec3 col = puff_light(v_wnrm, normalize(sun_dir), sun_energy, under, 1.0 - facing_eye, look, COLOR.rgb, v_deep);
	float here = length(VERTEX);
	float a = COLOR.a * mix(1.0, smoothstep(0.0, 0.55, facing_eye), feather) * smoothstep(near_fade * 0.3, near_fade, here);
	a *= puff_seen(v_side, CAMERA_POSITION_WORLD.y, underside);
	ALBEDO = col;
	ALPHA = a;
}
