// pane.gdshader: the Ssarith Hunt's windows and launch bays (change ssarith-ships-engine 2.1), mockup 12's PANE_GLSL and
// paneMesh (docs/mockups/ssarith-ship-concepts.html). Every window and bay is a quad of its own, laid on the hull where the
// bake cut for it (tools/blender/hull_detail.py pane_box, exported as the glb's window_* and bay_* empties; ShipSkin.Panes
// builds the quads): the pane plus its frame, a bevelled rim of dark metal with no glow of its own (owner, 2026-11-06:
// "window borders should also not glow"), lit by the battle's light; behind the glass, the room or the hangar
// (ship_rooms.gdshaderinc), standing level with the decks whatever the face's angle (owner, 2026-11-05: "Need to account for
// normal angles on windows, and the hangar bay too"), the hangar painted for its parallax ("I actually like the paralax so
// I'd keep it"). Battle damage is the hull's (damage.gdshaderinc): a hole or a burn takes its panes with it, a broken
// piece keeps the panes of its own cell, and a piece that breaks away goes dark (lit).
//
// Vertex data, all in the glb frame of the node the quads hang on: NORMAL the face's normal; CUSTOM0 (x, y) the point on
// the pane in metres from its centre and (z, w) the glass's half width and height; CUSTOM1 xyz the pane's across axis and w
// its kind (0 quarters, 1 mess, 2 workshop, 3 hold, 4 bay; +8 when lit); CUSTOM2 xyz its up axis; CUSTOM3 xyz its centre.
shader_type spatial;
render_mode cull_disabled;

// how strongly the rooms show (the page's roomK, 1 in both its lights)
uniform float room_k = 1.0;
// 1 while the ship or its piece is whole, 0 once a piece has broken away
uniform float lit = 1.0;
// the export's re-centring: mockup frame = (-x, y, -z) + mockup_offset (damage is placed in mockup ship space)
uniform vec3 mockup_offset = vec3(0.0);

#include "res://src/Game/View/damage.gdshaderinc"
#include "res://src/Game/View/ship_rooms.gdshaderinc"

varying vec4 v_qh;
varying vec3 v_u;
varying vec3 v_v;
varying vec3 v_n;
varying flat vec3 v_c;
varying flat float v_kind;
varying vec3 v_pm;     // the point, model frame
varying vec3 v_cm;     // the camera, model frame
varying vec3 v_obj;    // the point, mockup ship space (for the damage rules)

void vertex() {
	v_qh = CUSTOM0; v_u = CUSTOM1.xyz; v_kind = CUSTOM1.w; v_v = CUSTOM2.xyz; v_c = CUSTOM3.xyz; v_n = NORMAL;
	v_pm = VERTEX;
	v_cm = (inverse(MODEL_MATRIX) * vec4(CAMERA_POSITION_WORLD, 1.0)).xyz;
	v_obj = vec3(-VERTEX.x, VERTEX.y, -VERTEX.z) + mockup_offset;
}

void fragment() {
	float d_char = 0.0, d_ember = 0.0; int hole_idx = -1; float nz = dfbm(v_obj * 0.45), nz2 = dfbm(v_obj * 0.9 + 7.0);
	dmg_holes(v_obj, nz, d_char, d_ember, hole_idx);
	dmg_scorch(v_obj, nz2, d_char, d_ember);
	if (dmg_break(v_obj, nz, d_char, d_ember)) discard;   // a broken piece: only its own cell's panes
	if (hole_idx >= 0 || d_char > 0.5) discard;           // a hole or a burn takes the pane with it

	vec3 eu = normalize(v_u), ev = normalize(v_v), en = normalize(v_n), V = normalize(v_pm - v_cm);
	float kk = floor(v_kind + 0.5); bool on = kk >= 8.0; int k = int(on ? kk - 8.0 : kk);
	vec2 hs = v_qh.zw, qp = v_qh.xy, d2 = qp - clamp(qp, -hs, hs); float out_ = length(d2);
	mat3 to_view = mat3(VIEW_MATRIX * MODEL_MATRIX);
	if (out_ > 0.0) {   // the frame: a bevel rising from the glass, dark metal under the battle's light
		vec2 dir = d2 / out_;
		vec3 nf = normalize(en * 0.75 + (eu * dir.x + ev * dir.y) * 0.65);
		ALBEDO = vec3(0.105, 0.11, 0.095) * (1.0 - 0.7 * smoothstep(0.0, 0.5, d_char));
		METALLIC = 0.55; ROUGHNESS = 0.5;
		NORMAL = normalize(to_view * nf);
	} else {
		vec3 L = !on ? vec3(0.015, 0.018, 0.016) : (k == 2 ? vec3(1.0, 0.66, 0.3) : (k == 3 ? vec3(0.85, 0.62, 0.36) : vec3(1.0, 0.82, 0.52)));   // lib/baked-ship.js PANE_LIGHT
		// the room's frame: up the ship's up, in the face's normal turned level; the pane cuts its front at a slant
		vec3 rv = vec3(0.0, 1.0, 0.0), ri = vec3(-en.x, 0.0, -en.z), ru, rn; float hl = length(ri);
		if (hl < 0.3) { ru = eu; rv = ev; rn = -en; } else { rn = ri / hl; ru = normalize(cross(rv, rn)); }
		vec3 Pp = eu * qp.x + ev * qp.y; float zext = abs(dot(eu, rn)) * hs.x + abs(dot(ev, rn)) * hs.y;
		vec3 D = normalize(vec3(dot(V, ru), dot(V, rv), dot(V, rn)) + vec3(1e-4)); if (D.z <= 0.0) D.z = 1e-3;
		vec3 q = vec3(dot(Pp, ru), dot(Pp, rv), dot(Pp, rn) + zext);
		vec2 mouth = 2.0 * vec2(abs(dot(eu, ru)) * hs.x + abs(dot(ev, ru)) * hs.y, abs(dot(eu, rv)) * hs.x + abs(dot(ev, rv)) * hs.y);
		vec3 room = k == 4 ? hangar_behind(q, D, floor(v_c * 0.3), mouth, on ? vec3(1.0, 0.84, 0.58) : L)
			: room_behind(q, D, floor(v_c * 0.6), k == 1 ? 1 : (k == 2 ? 2 : (k == 3 ? 1 : 0)), L);
		float fres = pow(1.0 - clamp(-dot(V, en), 0.0, 1.0), 4.0);   // a faint sky in the glass at a glancing look
		ALBEDO = vec3(0.0); METALLIC = 0.0; ROUGHNESS = 0.08; SPECULAR = 0.5;
		NORMAL = normalize(to_view * en);
		EMISSION = (room * room_k * lit * (1.0 - 0.5 * fres) + vec3(0.05, 0.07, 0.06) * fres) * (1.0 - smoothstep(0.0, 0.5, d_char));
	}
}
