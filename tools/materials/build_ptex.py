#!/usr/bin/env python3
"""Author Fleet Command's Material Maker (.ptex) graphs, one function per material (material-maker skill).

Each graph is Material Maker's own noise nodes feeding one inline shader node whose GLSL draws the surface at true size
(one texture tile covers the material's `tile_m` metres, the ships' UVs being metres), then Material Maker's normal map
and occlusion nodes and its material node (`ptex_graph.finish`). `data/materials/materials.json` lists every material;
`export_materials.sh` renders the graphs and `postprocess.py` writes the maps and the contact sheet.

Conventions every graph keeps (from Crab Breeze's build_house_ptex.py):
- **Every map tiles.** Every pattern repeats a whole number of times per tile and every noise is sampled at
  whole-number scales, so the 2 x 2 tiling on the contact sheet shows no seam.
- **Normals at true depth.** `depth_m` is the height range in metres; `finish` sets the normal strength from it.

`hull_plate` is a starter graph that proves the pipeline end to end. It is not an approved look: ship materials are
designed in an OpenSpec change and shown on a mockup first (CLAUDE.md section 4). Each .ptex opens in Material Maker for
hand tweaking; once one is edited there, remove it from GRAPHS below or it will be overwritten.

Run:  python3 tools/materials/build_ptex.py [name ...]
Then: tools/materials/export_materials.sh
"""
import json
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from ptex_graph import OUT_DIR, Graph, fbm, finish  # noqa: E402


def hull_plate():
    """Painted starship hull plating: rows of 0.5 m plates of mixed widths, 6 mm seams, raised access plates, faint
    tonal variation and grime gathered at the seams. One tile is 2 m."""
    g = Graph("Hull Plate")
    ins = [("n", "f", fbm(g, 8, iterations=5, offset=0.1)),
           ("streak", "f", g.node("fbm2", noise=2, scale_x=4, scale_y=32, folds=0, iterations=4, persistence=0.5,
                                  offset=0.3))]
    body = """
// Per texel: x, y the distance to the nearest seam in metres, z the plate's hash, w the raised access plate mask.
vec4 plate_$name(vec2 uv) {
	float rows = 4.0;
	float r = floor(uv.y * rows);
	float n = 2.0 + floor(hs_$name(vec2(r, 3.0)) * 3.0);
	float x = fract(uv.x + hs_$name(vec2(r, 7.0)));
	float c = floor(x * n);
	vec2 f = vec2(fract(x * n), fract(uv.y * rows));
	vec2 d = vec2(min(f.x, 1.0 - f.x) * 2.0 / n, min(f.y, 1.0 - f.y) * 2.0 / rows);
	float id = hs_$name(vec2(c + 17.0 * r, r + 1.0));
	float inset = step(0.72, id) * smoothstep(0.08, 0.085, d.x) * smoothstep(0.06, 0.065, d.y);
	return vec4(d, id, inset);
}
float seam_$name(vec4 p) { return smoothstep(0.003, 0.006, min(p.x, p.y)); }
float height_$name(vec2 uv) {
	vec4 p = plate_$name(uv);
	return 0.3 + 0.5 * seam_$name(p) + 0.15 * p.w + 0.03 * ($n(uv) - 0.5);
}
vec3 albedo_$name(vec2 uv) {
	vec4 p = plate_$name(uv);
	float v = 0.62 + 0.06 * (p.z - 0.5) + 0.03 * ($n(uv) - 0.5);
	float grime = (1.0 - smoothstep(0.0, 0.03, min(p.x, p.y))) * 0.18 + 0.05 * smoothstep(0.6, 0.9, $streak(uv));
	v *= 1.0 - grime;
	return vec3(v * 0.93, v * 0.96, v) * (1.0 - 0.6 * (1.0 - seam_$name(p)));
}
float rough_$name(vec2 uv) {
	vec4 p = plate_$name(uv);
	return 0.42 + 0.12 * (p.z - 0.5) + 0.1 * $n(uv) + 0.25 * (1.0 - seam_$name(p));
}
float metal_$name(vec2 uv) { return 0.1; }
"""
    return finish(g, "hull plate", ins, body, tile_m=2.0, depth_m=0.006, ao_radius=8, ao_strength=0.8)


# ---- faction emblem materials (openspec/changes/faction-emblems; the Emblem Render Guide, docs/design/emblem-guide.html)
# An emblem is a badge about 0.1 m across; these tile every 0.05 m and are projected flat onto the badge's face by
# tools/blender/emblem_render.py. The clear coat of enamel and the glass of a cabochon are Blender's, on top.

def emblem_platinum():
    """Polished platinum, the Concord's metal (its pearl-white hulls): a mirror finish with the faint hairlines of
    polishing in two directions and a soft cloud of wear. One tile is 0.05 m."""
    g = Graph("Emblem Platinum")
    ins = [("n", "f", fbm(g, 6, iterations=5, offset=0.0)),
           ("hx", "f", g.node("fbm2", noise=2, scale_x=2, scale_y=96, folds=0, iterations=3, persistence=0.5, offset=0.0)),
           ("hy", "f", g.node("fbm2", noise=2, scale_x=128, scale_y=3, folds=0, iterations=3, persistence=0.5, offset=0.0))]
    body = """
float lines_$name(vec2 uv) { return max(smoothstep(0.78, 0.86, $hx(uv)), 0.6 * smoothstep(0.8, 0.88, $hy(uv))); }
float height_$name(vec2 uv) { return 0.5 - 0.15 * lines_$name(uv) + 0.02 * ($n(uv) - 0.5); }
vec3 albedo_$name(vec2 uv) { float v = 0.86 + 0.03 * ($n(uv) - 0.5) - 0.04 * lines_$name(uv); return vec3(v * 0.97, v * 0.985, v); }
float rough_$name(vec2 uv) { return 0.2 + 0.06 * $n(uv) + 0.12 * lines_$name(uv); }
float metal_$name(vec2 uv) { return 1.0; }
"""
    return finish(g, "emblem platinum", ins, body, tile_m=0.05, depth_m=0.00003, ao_radius=4, ao_strength=0.3)


def emblem_enamel_signal():
    """Fired enamel in the Concord's Signal blue (#2f62e0, its flag): the faint orange peel of a fired glaze, a slow
    cloud of deeper and lighter colour, and a fine speckle of grains in the glass. One tile is 0.05 m."""
    g = Graph("Emblem Enamel Signal")
    ins = [("peel", "f", fbm(g, 40, iterations=3, persistence=0.5)), ("cloud", "f", fbm(g, 3, iterations=4)),
           ("grain", "f", fbm(g, 160, iterations=2, persistence=0.5))]
    body = """
float height_$name(vec2 uv) { return 0.5 + 0.12 * ($peel(uv) - 0.5); }
vec3 albedo_$name(vec2 uv) {
	vec3 base = vec3(0.184, 0.384, 0.878);
	vec3 c = base * (0.9 + 0.2 * $cloud(uv));
	return mix(c, vec3(0.42, 0.62, 1.0), 0.5 * smoothstep(0.72, 0.8, $grain(uv)));
}
float rough_$name(vec2 uv) { return 0.22 + 0.08 * $peel(uv); }
float metal_$name(vec2 uv) { return 0.0; }
"""
    return finish(g, "emblem enamel signal", ins, body, tile_m=0.05, depth_m=0.00005, ao_radius=4, ao_strength=0.2)


def emblem_enamel_night():
    """Enamel in the Concord's Deep blue (#1b2a6b) scattered with fine lit flecks: the night sky every homeworld
    shares (console skins design, the Concord palette). The flecks also go to the emission map. One tile is 0.05 m."""
    g = Graph("Emblem Enamel Night")
    ins = [("peel", "f", fbm(g, 40, iterations=3, persistence=0.5)), ("cloud", "f", fbm(g, 2, iterations=5))]
    body = """
// Stars: one candidate per cell of a 48 x 48 grid (tiles), lit where the cell's hash is high, jittered in its cell.
float star_$name(vec2 uv) {
	vec2 q = uv * 48.0; vec2 c = floor(q); vec2 f = fract(q);
	float h = hs_$name(c + 3.0);
	vec2 p = vec2(hs_$name(c + 11.0), hs_$name(c + 29.0)) * 0.6 + 0.2;
	float r = mix(0.05, 0.13, hs_$name(c + 47.0));
	return step(0.86, h) * (1.0 - smoothstep(r * 0.4, r, length(f - p)));
}
float height_$name(vec2 uv) { return 0.5 + 0.12 * ($peel(uv) - 0.5); }
vec3 albedo_$name(vec2 uv) {
	vec3 c = vec3(0.106, 0.165, 0.42) * (0.75 + 0.4 * $cloud(uv));
	return mix(c, vec3(0.85, 0.9, 1.0), star_$name(uv));
}
vec3 emit_$name(vec2 uv) { return vec3(0.8, 0.88, 1.0) * star_$name(uv); }
float rough_$name(vec2 uv) { return 0.2 + 0.08 * $peel(uv); }
float metal_$name(vec2 uv) { return 0.0; }
"""
    return finish(g, "emblem enamel night", ins, body, tile_m=0.05, depth_m=0.00005, ao_radius=4, ao_strength=0.2,
                  emission=True)


def emblem_enamel_pearl():
    """Pearl enamel, the Concord's hull colour (#eef3ff) as the ship on its emblem: a white glaze with the faint orange
    peel of firing, a slow pearly drift between a cool and a warm white, and fine bright grains in the glass. One tile is
    0.05 m."""
    g = Graph("Emblem Enamel Pearl")
    ins = [("peel", "f", fbm(g, 40, iterations=3, persistence=0.5)), ("cloud", "f", fbm(g, 3, iterations=4)),
           ("grain", "f", fbm(g, 160, iterations=2, persistence=0.5))]
    body = """
float height_$name(vec2 uv) { return 0.5 + 0.12 * ($peel(uv) - 0.5); }
vec3 albedo_$name(vec2 uv) {
	vec3 cool = vec3(0.92, 0.95, 1.0), warm = vec3(0.97, 0.95, 0.92);
	vec3 c = mix(cool, warm, smoothstep(0.35, 0.75, $cloud(uv))) * (0.97 + 0.03 * $peel(uv));
	return mix(c, vec3(1.0), 0.6 * smoothstep(0.74, 0.82, $grain(uv)));
}
float rough_$name(vec2 uv) { return 0.2 + 0.08 * $peel(uv); }
float metal_$name(vec2 uv) { return 0.0; }
"""
    return finish(g, "emblem enamel pearl", ins, body, tile_m=0.05, depth_m=0.00005, ao_radius=4, ao_strength=0.2)


# ---- the Ssarith Hunt's badge, the Bow Rites (owner, 2026-11-05: "shade the bow parts with like wood", "Arrow made of
# wood"). The bow is the Hunt's own,
# laminated from cut plate (the guide's SD), so the wood shows its laminations as well as its grain.

def emblem_wood_laminate():
    """A bow's laminated wood: thin plies of a warm and a darker wood glued edge to edge (the Hunt's makers build their
    bows from cut plate), long grain running along the plies with fine pores, a slow drift of tone between boards, and an
    oiled, satin finish. The plies and grain run along the tile's v (the bows' length). One tile is 0.05 m."""
    g = Graph("Emblem Wood Laminate")
    ins = [("grain", "f", g.node("fbm2", noise=2, scale_x=64, scale_y=2, folds=0, iterations=5, persistence=0.55, offset=0.0)),
           ("pore", "f", g.node("fbm2", noise=2, scale_x=256, scale_y=16, folds=0, iterations=2, persistence=0.5, offset=0.0)),
           ("board", "f", fbm(g, 3, iterations=4))]
    body = """
// The plies: 40 per tile across u (1.25 mm each, so a 3 mm limb shows two or three), alternating a warm and a dark
// wood, a hairline of glue between them.
vec2 ply_$name(vec2 uv) { float x = uv.x * 40.0; return vec2(mod(floor(x), 2.0), min(fract(x), 1.0 - fract(x))); }
float height_$name(vec2 uv) {
	vec2 p = ply_$name(uv);
	return 0.5 + 0.1 * ($grain(uv) - 0.5) - 0.12 * smoothstep(0.75, 0.9, $pore(uv)) - 0.08 * (1.0 - smoothstep(0.0, 0.025, p.y));
}
vec3 albedo_$name(vec2 uv) {
	vec2 p = ply_$name(uv);
	vec3 warm = vec3(0.56, 0.35, 0.17), dark = vec3(0.47, 0.28, 0.13);
	vec3 c = mix(warm, dark, p.x) * (0.85 + 0.3 * $board(uv));
	c *= 0.78 + 0.34 * smoothstep(0.25, 0.75, $grain(uv));
	c *= 1.0 - 0.35 * smoothstep(0.75, 0.9, $pore(uv));
	return mix(c * 0.7, c, smoothstep(0.0, 0.025, p.y));
}
float rough_$name(vec2 uv) { return 0.42 + 0.12 * $grain(uv) + 0.2 * smoothstep(0.75, 0.9, $pore(uv)); }
float metal_$name(vec2 uv) { return 0.0; }
"""
    return finish(g, "emblem wood laminate", ins, body, tile_m=0.05, depth_m=0.00008, ao_radius=4, ao_strength=0.4)


def emblem_fern():
    """A fern's leaf, for the fronds over the Ssarith badge's field (owner, 2026-11-05: "3d model Fern leaves", then "it should
    be 3d", with a photograph of ferns in deep shadow): a deep, saturated green with fine parallel veins, lighter and yellower where the
    leaf is thin, a waxy satin sheen. The veins run along u, the shader's own shading does the rest. One tile is 0.05 m."""
    g = Graph("Emblem Fern")
    ins = [("vein", "f", g.node("fbm2", noise=2, scale_x=4, scale_y=160, folds=0, iterations=3, persistence=0.5, offset=0.0)),
           ("cloud", "f", fbm(g, 6, iterations=4)), ("speck", "f", fbm(g, 96, iterations=2, persistence=0.5))]
    body = """
float height_$name(vec2 uv) { return 0.5 + 0.18 * smoothstep(0.6, 0.85, $vein(uv)) + 0.05 * ($cloud(uv) - 0.5); }
vec3 albedo_$name(vec2 uv) {
	vec3 deep = vec3(0.03, 0.2, 0.04), bright = vec3(0.16, 0.5, 0.09), yellow = vec3(0.36, 0.6, 0.12);
	vec3 c = mix(deep, bright, 0.3 + 0.6 * $cloud(uv));
	c = mix(c, yellow, 0.35 * smoothstep(0.6, 0.85, $vein(uv)));
	return c * (0.92 + 0.12 * $speck(uv));
}
float rough_$name(vec2 uv) { return 0.42 + 0.12 * $cloud(uv); }
float metal_$name(vec2 uv) { return 0.0; }
"""
    return finish(g, "emblem fern", ins, body, tile_m=0.05, depth_m=0.00005, ao_radius=4, ao_strength=0.3)

def emblem_enamel_forest():
    """The badge's field, the Hunt's hunting ground (owner, 2026-11-05: "Background should be like a beige, dull brown and
    dry green forest"): a dry forest painted in enamel. A beige haze, dull brown trunks standing in it at uneven spacing and
    widths, nearer trunks darker, and dappled dry-green and olive canopy over them, with light falling through. Every
    trunk and blot repeats whole across the tile, so the field tiles. One tile is 0.05 m."""
    g = Graph("Emblem Enamel Forest")
    ins = [("leaf", "f", fbm(g, 4, iterations=5, persistence=0.55)), ("leaf2", "f", fbm(g, 12, iterations=4, persistence=0.5)),
           ("bark", "f", g.node("fbm2", noise=2, scale_x=96, scale_y=4, folds=0, iterations=3, persistence=0.5, offset=0.0)),
           ("haze", "f", fbm(g, 3, iterations=4)), ("peel", "f", fbm(g, 40, iterations=3, persistence=0.5))]
    body = """
// Trunks: 5 columns across the tile, each with a trunk at a hashed offset and width, near (dark, wide) or far (pale, thin).
// Returns x the trunk's cover (0-1), y its depth (0 far, 1 near).
vec2 trunk_$name(vec2 uv) {
	float cols = 5.0; float c = floor(uv.x * cols); float f = fract(uv.x * cols);
	float near = step(0.55, hs_$name(vec2(c, 5.0)));
	float w = mix(0.1, 0.18, hs_$name(vec2(c, 9.0))) * mix(0.8, 1.8, near);
	float x0 = mix(0.2, 0.8, hs_$name(vec2(c, 13.0)));
	float lean = 0.03 * (hs_$name(vec2(c, 21.0)) - 0.5) * sin(6.2831853 * uv.y);
	float t = 1.0 - smoothstep(w * 0.4, w * 0.5, abs(f - x0 - lean));
	return vec2(t * step(0.15, hs_$name(vec2(c, 31.0))), near);
}
// The canopy: dry leaves where the leaf noise is high, thicker toward the top of the tile's repeat, broken by light.
float canopy_$name(vec2 uv) {
	float l = 0.65 * $leaf(uv) + 0.35 * $leaf2(uv);
	return smoothstep(0.47, 0.57, l);
}
float height_$name(vec2 uv) {
	vec2 t = trunk_$name(uv);
	return 0.5 + 0.12 * ($peel(uv) - 0.5) + 0.12 * t.x * (0.5 + t.y) + 0.08 * canopy_$name(uv);
}
vec3 albedo_$name(vec2 uv) {
	vec3 beige = vec3(0.64, 0.57, 0.43), haze = vec3(0.55, 0.5, 0.38);
	vec3 c = mix(beige, haze, $haze(uv));
	vec2 t = trunk_$name(uv);
	vec3 farT = vec3(0.44, 0.35, 0.24), nearT = vec3(0.26, 0.18, 0.11);
	vec3 trunkC = mix(farT, nearT, t.y) * (0.85 + 0.3 * $bark(uv));
	c = mix(c, trunkC, t.x);
	float can = canopy_$name(uv);
	vec3 dry = vec3(0.42, 0.44, 0.25), olive = vec3(0.3, 0.33, 0.18), straw = vec3(0.56, 0.53, 0.33);
	vec3 leafC = mix(olive, dry, smoothstep(0.3, 0.7, $leaf2(uv)));
	leafC = mix(leafC, straw, 0.5 * smoothstep(0.72, 0.85, $leaf2(uv)));
	return mix(c, leafC, can * 0.92);
}
float rough_$name(vec2 uv) { return 0.3 + 0.1 * $peel(uv); }
float metal_$name(vec2 uv) { return 0.0; }
"""
    return finish(g, "emblem enamel forest", ins, body, tile_m=0.05, depth_m=0.00006, ao_radius=4, ao_strength=0.3)


# ---- the faction screen's table (owner, 2026-11-06: "a wooden table", "generate the best wooden textture you can with
# material maker").

def table_wood():
    """A polished walnut tabletop. Four boards across a 1 m tile (0.25 m each), each a different log, so each its own
    tone and its own figure, butt-jointed once a tile at a staggered place, a hairline seam between boards. Flat-sawn
    figure: the growth rings of a log cut lengthways, so where the board's plane rises and falls through the rings they
    sweep up into cathedral arches, drawn from a ring field round each board's own pith (the board's depth in the log
    drifting along its length). Earlywood a warm mid brown, latewood a dark chocolate band at each ring's end; open
    pores streaking along the grain; the medullary rays as short bright flecks; a few dark mineral streaks. Under a deep
    polished finish: low roughness, the pores and seams rougher, a faint burnish. The grain runs along the tile's u.
    One tile is 1 m; every pattern repeats a whole number of times per tile."""
    g = Graph("Table Wood")
    ins = [("warp", "f", g.node("fbm2", noise=2, scale_x=2, scale_y=8, folds=0, iterations=2, persistence=0.5, offset=0.0)),
           ("fine", "f", g.node("fbm2", noise=2, scale_x=2, scale_y=96, folds=0, iterations=5, persistence=0.55, offset=0.0)),
           ("pore", "f", g.node("fbm2", noise=2, scale_x=12, scale_y=640, folds=0, iterations=2, persistence=0.5, offset=0.0)),
           ("fleck", "f", g.node("fbm2", noise=2, scale_x=96, scale_y=384, folds=0, iterations=2, persistence=0.5, offset=0.0)),
           ("mineral", "f", g.node("fbm2", noise=2, scale_x=1, scale_y=24, folds=0, iterations=3, persistence=0.5, offset=0.0)),
           ("burnish", "f", fbm(g, 24, iterations=3, persistence=0.5))]
    body = """
// A board: t across it (0-1), du along it from its butt joint (0-1 a tile), its log's hash, and the distance (tile units,
// metres) to the nearest seam or joint.
vec4 board_$name(vec2 uv) {
	float rows = 4.0;
	float r = floor(uv.y * rows);
	float t = fract(uv.y * rows);
	float du = fract(uv.x - hs_$name(vec2(r, 5.0)));
	float d = min(min(t, 1.0 - t) / rows, min(du, 1.0 - du));
	return vec4(t, du, hs_$name(vec2(r * 7.0 + 1.0, 3.0)), d);
}
// The ring field: where the board's face cuts its log's growth rings, in metres. The pith sits beside or under the board
// (a flat-sawn board is cut from one side of the log), 0.09-0.15 m below its face; the face's depth in the log rises and
// falls a few centimetres along the board, so the rings sweep up into arches where the pith is under the board and run
// straight where it is beside it. Rings 4.5-7 mm apart, as a walnut grows. The warp, stretched along the grain, waves
// the rings without blotting them. Returns the ring's phase, 0-1 within a year.
float ring_$name(vec2 uv, vec4 b) {
	float pith = mix(-0.4, 1.4, hs_$name(vec2(b.z, 11.0)));
	float f = 145.0 + 75.0 * hs_$name(vec2(b.z, 13.0));
	float ph = hs_$name(vec2(b.z, 17.0));
	float dx = (b.x - pith) * 0.25;
	float depth = 0.09 + 0.06 * hs_$name(vec2(b.z, 19.0)) + 0.028 * sin(6.2831853 * (b.y + ph)) + 0.012 * sin(6.2831853 * (2.0 * b.y + ph * 3.0))
		+ 0.008 * ($warp(uv) - 0.5);
	float r = sqrt(dx * dx + depth * depth);
	return fract(r * f + 0.55 * ($warp(uv) - 0.5) + 0.025 * ($fine(uv) - 0.5));
}
// a year's ring: light earlywood darkening slowly into the latewood, then the next year's earlywood starts at once
float late_$name(float g) { return smoothstep(0.12, 0.93, g) * (1.0 - smoothstep(0.965, 1.0, g)); }
float seam_$name(vec4 b) { return smoothstep(0.0004, 0.0013, b.w); }
float pore_$name(vec2 uv) { return smoothstep(0.72, 0.9, $pore(uv)); }
float height_$name(vec2 uv) {
	vec4 b = board_$name(uv);
	float g = ring_$name(uv, b);
	return 0.62 + 0.05 * late_$name(g) - 0.12 * pore_$name(uv) + 0.02 * ($fine(uv) - 0.5) - 0.5 * (1.0 - seam_$name(b));
}
vec3 albedo_$name(vec2 uv) {
	vec4 b = board_$name(uv);
	float g = ring_$name(uv, b);
	vec3 early = vec3(0.39, 0.24, 0.13), late = vec3(0.21, 0.115, 0.06);
	// each board its own log: a little lighter or darker, a little redder or cooler
	float tone = 0.86 + 0.28 * hs_$name(vec2(b.z, 23.0));
	vec3 tint = mix(vec3(1.04, 0.98, 0.92), vec3(0.96, 1.0, 1.04), hs_$name(vec2(b.z, 29.0)));
	vec3 c = mix(early, late, pow(late_$name(g), 1.6)) * tone * tint;
	c *= 0.9 + 0.2 * $fine(uv);                                                    // the grain's long streaks
	c = mix(c, c * 1.4, 0.45 * smoothstep(0.82, 0.96, $fleck(uv)));                 // ray flecks, catching the light
	c *= 1.0 - 0.45 * pore_$name(uv);                                               // open pores, darker
	c *= 1.0 - 0.3 * smoothstep(0.68, 0.9, $mineral(uv)) * (0.5 + 0.5 * hs_$name(vec2(b.z, 31.0)));   // mineral streaks
	c = pow(c, vec3(1.06)) * vec3(1.05, 1.0, 0.92);                                 // the finish deepens and warms it
	return mix(c * 0.35, c, seam_$name(b));
}
float rough_$name(vec2 uv) {
	vec4 b = board_$name(uv);
	return 0.16 + 0.07 * $burnish(uv) + 0.24 * pore_$name(uv) + 0.35 * (1.0 - seam_$name(b));
}
float metal_$name(vec2 uv) { return 0.0; }
"""
    return finish(g, "table wood", ins, body, tile_m=1.0, depth_m=0.0004, ao_radius=6, ao_strength=0.6)


# ---- the paper map's room (owner, 2026-11-07: "make the space background a crusty print on old piece of paper with hexagon
# grid drawn on it", "the big map poster sits on a large wooden table, of which sits in a room with red carpeting"; then "The
# walls in that room should be like a rustic dark green wall with wood trimmings"). The map's own print is drawn live by the
# page over this paper.

def map_paper():
    """Old poster paper: a warm cream gone uneven with age, mottled in broad soft patches, its fibres showing as faint
    crossed streaks, a fine tooth, and scattered foxing (small rust-brown spots, a few with a darker core). Matte. The
    page lays the printed map over it and adds what belongs to this one sheet (its folds, stains and torn edge). One tile is
    0.25 m."""
    g = Graph("Map Paper")
    ins = [("mott", "f", fbm(g, 4, iterations=5, persistence=0.6)),
           ("blot", "f", fbm(g, 12, iterations=4, persistence=0.55)),
           ("fibx", "f", g.node("fbm2", noise=2, scale_x=96, scale_y=6, folds=0, iterations=3, persistence=0.5, offset=0.0)),
           ("fiby", "f", g.node("fbm2", noise=2, scale_x=6, scale_y=96, folds=0, iterations=3, persistence=0.5, offset=0.0)),
           ("tooth", "f", fbm(g, 256, iterations=3, persistence=0.5)),
           ("fox", "f", fbm(g, 40, iterations=4, persistence=0.5))]
    body = """
float foxing_$name(vec2 uv) { return smoothstep(0.80, 0.86, $fox(uv)); }
float height_$name(vec2 uv) {
	return 0.5 + 0.25 * ($tooth(uv) - 0.5) + 0.12 * ($fibx(uv) + $fiby(uv) - 1.0) + 0.1 * ($mott(uv) - 0.5);
}
vec3 albedo_$name(vec2 uv) {
	vec3 cream = vec3(0.86, 0.79, 0.64), aged = vec3(0.74, 0.62, 0.43);
	float age = smoothstep(0.35, 0.85, $mott(uv)) * 0.32 + smoothstep(0.6, 0.9, $blot(uv)) * 0.18;
	vec3 c = mix(cream, aged, age);
	c *= 0.96 + 0.06 * ($fibx(uv) + $fiby(uv) - 1.0) + 0.05 * ($tooth(uv) - 0.5);
	float f = foxing_$name(uv);
	c = mix(c, vec3(0.55, 0.36, 0.20), 0.55 * f);
	c = mix(c, vec3(0.38, 0.22, 0.12), 0.5 * smoothstep(0.87, 0.91, $fox(uv)));
	return c;
}
float rough_$name(vec2 uv) { return 0.88 + 0.08 * ($tooth(uv) - 0.5); }
float metal_$name(vec2 uv) { return 0.0; }
"""
    return finish(g, "map paper", ins, body, tile_m=0.25, depth_m=0.00008, ao_radius=3, ao_strength=0.2)


def carpet_red():
    """A deep red cut-pile carpet: the pile's tufts as fine bright and dark specks, the nap brushed in broad patches that
    catch the light differently, a little wear. Matte. One tile is 0.5 m."""
    g = Graph("Carpet Red")
    ins = [("pile", "f", fbm(g, 512, iterations=2, persistence=0.5)),
           ("tuft", "f", fbm(g, 128, iterations=3, persistence=0.5)),
           ("nap", "f", fbm(g, 6, iterations=4, persistence=0.55)),
           ("wear", "f", fbm(g, 3, iterations=4, persistence=0.5))]
    body = """
float height_$name(vec2 uv) { return 0.5 + 0.35 * ($pile(uv) - 0.5) + 0.25 * ($tuft(uv) - 0.5); }
vec3 albedo_$name(vec2 uv) {
	vec3 red = vec3(0.42, 0.045, 0.055), deep = vec3(0.22, 0.02, 0.03);
	vec3 c = mix(deep, red, 0.55 + 0.8 * ($pile(uv) - 0.5) + 0.5 * ($tuft(uv) - 0.5));
	c *= 0.86 + 0.28 * $nap(uv);
	c = mix(c, c * vec3(1.18, 1.05, 1.05), 0.4 * smoothstep(0.62, 0.8, $wear(uv)));
	return c;
}
float rough_$name(vec2 uv) { return 0.97; }
float metal_$name(vec2 uv) { return 0.0; }
"""
    return finish(g, "carpet red", ins, body, tile_m=0.5, depth_m=0.002, ao_radius=4, ao_strength=0.5)


def wall_green_rustic():
    """A rustic wall in dark green: limewash over old lime plaster, the colour uneven where coats dried at different
    rates, the brush's strokes running every way across it, the plaster's own lumps and dips under the paint, hairline
    cracks, and the pale plaster showing through where the paint has worn thin. Matte. One tile is 1 m."""
    g = Graph("Wall Green Rustic")
    ins = [("tone", "f", fbm(g, 3, iterations=5, persistence=0.6)),
           ("cloud", "f", fbm(g, 9, iterations=4, persistence=0.55)),
           ("strokea", "f", g.node("fbm2", noise=2, scale_x=4, scale_y=40, folds=0, iterations=3, persistence=0.5, offset=0.0)),
           ("strokeb", "f", g.node("fbm2", noise=2, scale_x=40, scale_y=5, folds=0, iterations=3, persistence=0.5, offset=0.0)),
           ("lump", "f", fbm(g, 16, iterations=4, persistence=0.5)),
           ("grain", "f", fbm(g, 192, iterations=3, persistence=0.5)),
           ("crack", "f", fbm(g, 6, iterations=6, persistence=0.6)),
           ("wear", "f", fbm(g, 7, iterations=5, persistence=0.55))]
    body = """
float crack_$name(vec2 uv) { float r = 1.0 - abs(2.0 * $crack(uv) - 1.0); return smoothstep(0.965, 0.995, r); }
// worn through in a few places, where an old knock or rub took the paint: patches gathered where a broad mask is high
float worn_$name(vec2 uv) { return smoothstep(0.70, 0.76, $wear(uv) + 0.12 * ($grain(uv) - 0.5)) * smoothstep(0.55, 0.75, $tone(uv)); }
float height_$name(vec2 uv) {
	float h = 0.5 + 0.22 * ($lump(uv) - 0.5) + 0.08 * ($strokea(uv) + $strokeb(uv) - 1.0) + 0.06 * ($grain(uv) - 0.5);
	return h - 0.18 * crack_$name(uv) - 0.05 * worn_$name(uv);
}
vec3 albedo_$name(vec2 uv) {
	vec3 deep = vec3(0.075, 0.15, 0.10), mid = vec3(0.13, 0.24, 0.16), olive = vec3(0.17, 0.25, 0.14);
	vec3 c = mix(deep, mid, smoothstep(0.25, 0.75, $tone(uv)));
	c = mix(c, olive, 0.35 * smoothstep(0.5, 0.8, $cloud(uv)));
	c *= 0.9 + 0.12 * ($strokea(uv) + $strokeb(uv) - 1.0) + 0.06 * ($grain(uv) - 0.5);
	c = mix(c, vec3(0.50, 0.47, 0.38) * (0.9 + 0.2 * $grain(uv)), 0.85 * worn_$name(uv));
	c = mix(c, c * 0.35, crack_$name(uv));
	return c;
}
float rough_$name(vec2 uv) { return 0.86 + 0.06 * worn_$name(uv); }
float metal_$name(vec2 uv) { return 0.0; }
"""
    return finish(g, "wall green rustic", ins, body, tile_m=1.0, depth_m=0.0015, ao_radius=5, ao_strength=0.5)


GRAPHS = {
    "hull_plate": hull_plate,
    "emblem_platinum": emblem_platinum,
    "emblem_enamel_signal": emblem_enamel_signal,
    "emblem_enamel_night": emblem_enamel_night,
    "emblem_enamel_pearl": emblem_enamel_pearl,
    "emblem_wood_laminate": emblem_wood_laminate,
    "emblem_enamel_forest": emblem_enamel_forest,
    "emblem_fern": emblem_fern,
    "table_wood": table_wood,
    "map_paper": map_paper,
    "carpet_red": carpet_red,
    "wall_green_rustic": wall_green_rustic,
}


def main():
    os.makedirs(OUT_DIR, exist_ok=True)
    only = set(sys.argv[1:])
    for name, fn in GRAPHS.items():
        if only and name not in only:
            continue
        path = os.path.join(OUT_DIR, f"{name}.ptex")
        with open(path, "w") as f:
            json.dump(fn().to_json(), f, indent=1, sort_keys=True)
            f.write("\n")
        print("wrote", os.path.relpath(path))


if __name__ == "__main__":
    main()
