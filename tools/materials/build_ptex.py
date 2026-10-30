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


GRAPHS = {
    "hull_plate": hull_plate,
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
