#!/usr/bin/env python3
"""Compose many nebula sky recipes from themed palettes, for tools/space/nebula_skies.py to paint.

The owner, 2026-11-04: "make sure to generate lots of cool nebulas to be used for our demos, I'm getting sick of the
same one being used all the time". Star Crew's four recipes (bloom, ember, quiet, verdigris) were painted by hand; this
writes more of the same kind from a palette and a seed each, so a new sky is one line in THEMES. Each is built as the
painted ones are: one main cloud of overlapping strokes along a long path, dark lanes down its spine and its core on it,
one or two smaller clouds on the far side, and a gently tilted galaxy band. They keep to the band of sky the battle camera sees (it looks down at the battle plane from
about 20 degrees above it), latitudes about -35 to +45, spread round every longitude so any heading shows nebula.

The composed recipes are written into data/space/skies/recipes.json after the hand-painted ones (which this leaves
alone), each marked "composed": true; then run tools/space/nebula_skies.py to paint them. Deterministic: a recipe is a
function of its theme and seed only.

Usage: python3 tools/space/nebula_recipes.py
"""

import json
import math
import os
import random

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
RECIPES = os.path.join(ROOT, "data", "space", "skies", "recipes.json")

# The mesh's vertex budget: the painters' own (more did not soften anything: hard edges came from folded paths, above).
VERTEX_BUDGET = 8000

# (id, name, look, seed, space, band colour, glows [(heart, edge)], dust redden, cores [(heart, edge)], exposure)
# Colours sRGB 0-1. A theme's glows are its clouds' colours, heart and edge; cores its bright hearts.
THEMES = [
    ("crimson_reach", "Crimson Reach", "A wide blood-red cloud with ember edges, black lanes across it and a white-hot core.",
     2101, [0.022, 0.01, 0.014], [0.9, 0.6, 0.55], [((1.0, 0.25, 0.2), (0.35, 0.02, 0.06)), ((1.0, 0.55, 0.3), (0.5, 0.08, 0.06))],
     [0.7, 1.0, 1.3], [((1.0, 0.9, 0.8), (1.0, 0.4, 0.3))], 1.2),
    ("abyssal_teal", "Abyssal Teal", "Deep sea-green veils over near black, a cold cyan core and thin dark lanes.",
     2102, [0.008, 0.02, 0.026], [0.55, 0.8, 0.85], [((0.2, 0.85, 0.8), (0.02, 0.2, 0.26)), ((0.4, 0.95, 0.7), (0.04, 0.25, 0.2))],
     [0.8, 1.0, 1.1], [((0.8, 1.0, 1.0), (0.3, 0.8, 0.9))], 1.1),
    ("violet_storm", "Violet Storm", "Storm-purple clouds piled high, magenta at their hearts, a pale lilac core.",
     2103, [0.018, 0.01, 0.034], [0.7, 0.6, 0.95], [((0.75, 0.35, 1.0), (0.18, 0.04, 0.36)), ((1.0, 0.35, 0.8), (0.3, 0.04, 0.3))],
     [0.7, 0.9, 1.2], [((0.95, 0.85, 1.0), (0.7, 0.45, 1.0))], 1.25),
    ("golden_veil", "Golden Veil", "A thin gold veil across a dark sky, warm amber knots and a bright sun-coloured core.",
     2104, [0.02, 0.016, 0.012], [0.95, 0.82, 0.6], [((1.0, 0.78, 0.35), (0.4, 0.22, 0.06)), ((1.0, 0.62, 0.25), (0.34, 0.12, 0.04))],
     [0.8, 1.0, 1.1], [((1.0, 0.96, 0.82), (1.0, 0.7, 0.35))], 1.1),
    ("emerald_rift", "Emerald Rift", "Two green clouds torn apart by a dark rift, a yellow-green core between them.",
     2105, [0.01, 0.022, 0.014], [0.6, 0.9, 0.6], [((0.35, 1.0, 0.45), (0.04, 0.26, 0.08)), ((0.7, 1.0, 0.4), (0.12, 0.3, 0.04))],
     [0.8, 1.0, 1.1], [((0.95, 1.0, 0.8), (0.5, 0.9, 0.4))], 1.15),
    ("ice_halo", "Ice Halo", "Pale blue-white wisps round a brilliant blue star, cold and clean, full of stars.",
     2106, [0.012, 0.016, 0.032], [0.75, 0.85, 1.0], [((0.6, 0.8, 1.0), (0.1, 0.2, 0.45)), ((0.85, 0.92, 1.0), (0.25, 0.35, 0.6))],
     [0.9, 1.0, 1.0], [((0.95, 0.98, 1.0), (0.5, 0.7, 1.0))], 1.0),
    ("rose_quartz", "Rose Quartz", "Soft pink clouds with peach edges and a gentle white core: a calm, bright sky.",
     2107, [0.026, 0.016, 0.026], [0.95, 0.75, 0.85], [((1.0, 0.55, 0.7), (0.4, 0.12, 0.26)), ((1.0, 0.75, 0.6), (0.4, 0.2, 0.18))],
     [0.8, 0.9, 1.1], [((1.0, 0.95, 0.95), (1.0, 0.6, 0.7))], 1.15),
    ("rust_lanes", "Rust Lanes", "Brown and rust dust thick with dark lanes, a dim orange glow behind: a gritty sky.",
     2108, [0.016, 0.011, 0.009], [0.8, 0.65, 0.5], [((0.75, 0.38, 0.18), (0.22, 0.08, 0.03)), ((0.6, 0.42, 0.28), (0.18, 0.1, 0.06))],
     [0.6, 0.85, 1.3], [((1.0, 0.75, 0.5), (0.8, 0.35, 0.15))], 1.0),
    ("twin_lights", "Twin Lights", "A cyan cloud and a magenta cloud meeting, each round its own core.",
     2109, [0.014, 0.012, 0.03], [0.7, 0.7, 0.95], [((0.2, 0.9, 1.0), (0.02, 0.2, 0.4)), ((1.0, 0.3, 0.75), (0.34, 0.03, 0.26))],
     [0.8, 0.95, 1.1], [((0.85, 1.0, 1.0), (0.3, 0.85, 1.0)), ((1.0, 0.85, 0.95), (1.0, 0.4, 0.8))], 1.2),
    ("ashen_void", "Ashen Void", "Almost empty: grey-blue dust drifts and a single faint smudge, stars everywhere.",
     2110, [0.006, 0.007, 0.012], [0.6, 0.66, 0.8], [((0.5, 0.58, 0.72), (0.1, 0.12, 0.2))],
     [0.9, 1.0, 1.1], [((0.85, 0.9, 1.0), (0.5, 0.55, 0.7))], 0.9),
    ("aurora", "Aurora", "Green and violet ribbons like an aurora, draped along the horizon, a teal core.",
     2111, [0.01, 0.014, 0.026], [0.6, 0.85, 0.85], [((0.3, 1.0, 0.6), (0.04, 0.26, 0.16)), ((0.6, 0.35, 1.0), (0.12, 0.04, 0.32))],
     [0.8, 0.9, 1.1], [((0.8, 1.0, 0.95), (0.3, 0.9, 0.8))], 1.2),
    ("solar_furnace", "Solar Furnace", "A blazing yellow-white heart in orange fire, the brightest sky: battle under a star.",
     2112, [0.024, 0.014, 0.008], [1.0, 0.8, 0.55], [((1.0, 0.7, 0.2), (0.45, 0.14, 0.02)), ((1.0, 0.88, 0.45), (0.5, 0.3, 0.06))],
     [0.7, 0.95, 1.2], [((1.0, 1.0, 0.92), (1.0, 0.75, 0.3))], 1.3),
    ("deep_indigo", "Deep Indigo", "Indigo clouds lit blue from within, a white-blue core and crossing black lanes.",
     2113, [0.008, 0.008, 0.03], [0.6, 0.65, 1.0], [((0.3, 0.4, 1.0), (0.04, 0.06, 0.32)), ((0.5, 0.65, 1.0), (0.08, 0.12, 0.36))],
     [0.8, 0.9, 1.0], [((0.9, 0.95, 1.0), (0.45, 0.55, 1.0))], 1.15),
    ("coral_drift", "Coral Drift", "Coral and turquoise side by side, like a reef seen from above, a warm core.",
     2114, [0.016, 0.016, 0.026], [0.85, 0.8, 0.8], [((1.0, 0.45, 0.35), (0.38, 0.08, 0.08)), ((0.3, 0.85, 0.8), (0.03, 0.22, 0.24))],
     [0.8, 0.9, 1.1], [((1.0, 0.92, 0.8), (1.0, 0.55, 0.4))], 1.15),
]


def rnd_path(r, n, lon0, lat_lo, lat_hi, step):
    """A path of n points: a smooth walk round the sky from lon0, always onward and turning gently, kept inside the
    camera's band of latitudes. (A path that doubles back folds its stroke over itself, and the painter, which shades a
    point by the nearest point of the path, then draws a hard straight seam where the two folds meet.)"""
    lon, lat = lon0, r.uniform(lat_lo, lat_hi)
    slope = r.uniform(-0.25, 0.25)
    pts = [[round(((lon + 180) % 360) - 180, 1), round(lat, 1)]]
    for _ in range(n - 1):
        dl = step * r.uniform(0.8, 1.2)
        slope = max(-0.45, min(0.45, slope + r.uniform(-0.18, 0.18)))
        lon += dl
        lat = max(lat_lo, min(lat_hi, lat + slope * dl))
        pts.append([round(((lon + 180) % 360) - 180, 1), round(lat, 1)])
    return pts


def c3(c):
    return [round(float(x), 3) for x in c]


def stroke(r, path, width, heart, edge, intensity):
    return {"kind": "glow", "path_deg": path, "width_deg": round(width, 1),
            "taper": [round(r.uniform(0.2, 0.32), 2), round(r.uniform(0.25, 0.38), 2)],
            "colour_srgb": c3(heart), "edge_srgb": c3(edge), "intensity": round(intensity, 2),
            "warp": round(r.uniform(0.12, 0.2), 2), "warp_scale": round(r.uniform(1.4, 2.0), 1),
            "grain": round(r.uniform(0.4, 0.55), 2), "grain_scale": round(r.uniform(2.0, 3.0), 1),
            "hides_stars": round(r.uniform(0.35, 0.5), 2), "filaments": round(r.uniform(0.35, 0.55), 2),
            "filament_scale": round(r.uniform(3.8, 4.8), 1)}


def shifted(path, dlon, dlat):
    return [[round(((p[0] + dlon + 180) % 360) - 180, 1), round(max(-35, min(45, p[1] + dlat)), 1)] for p in path]


def compose(theme):
    """One sky: a main cloud (two or three overlapping strokes along one long path, as the painted skies are built),
    dark lanes down its spine, its core on it, one or two smaller clouds elsewhere and a gently tilted galaxy band."""
    tid, name, look, seed, space, band, glows, redden, cores, exposure = theme
    r = random.Random(seed)
    layers = []
    lon0 = r.uniform(-180, 180)
    spine = rnd_path(r, r.randint(5, 7), lon0, -18, 28, r.uniform(24, 34))     # 100-200 degrees of sky
    (h0, e0), (h1, e1) = glows[0], glows[min(1, len(glows) - 1)]
    layers.append(stroke(r, spine, r.uniform(17, 24), h0, e0, r.uniform(0.55, 0.85)))
    layers.append(stroke(r, shifted(spine, r.uniform(-12, 12), r.uniform(-8, 8)), r.uniform(9, 14), h1, e1, r.uniform(0.35, 0.6)))
    if r.random() < 0.7:
        mid = spine[1:-1] or spine
        layers.append(stroke(r, shifted(mid, r.uniform(-8, 8), r.uniform(-6, 6)), r.uniform(5, 8), h0, e1, r.uniform(0.4, 0.6)))
    for _ in range(r.randint(1, 2)):   # dark lanes down the main cloud's spine
        lane = shifted(spine[r.randint(0, 1):len(spine) - r.randint(0, 1)], r.uniform(-6, 6), r.uniform(-5, 5))
        layers.append({"kind": "dust", "path_deg": lane, "width_deg": round(r.uniform(2.6, 4.2), 1), "taper": [0.2, 0.25],
                       "density": round(r.uniform(2.2, 3.4), 1), "redden": c3(redden), "warp": round(r.uniform(0.08, 0.12), 2),
                       "warp_scale": 2.8, "grain": 0.5, "grain_scale": 4.0})
    far = spine[0][0] + 180   # smaller clouds on the far side, so no heading is bare
    for i in range(r.randint(1, 2)):
        heart, edge = glows[(i + 1) % len(glows)]
        layers.append(stroke(r, rnd_path(r, r.randint(3, 4), far + r.uniform(-50, 50), -20, 30, r.uniform(14, 22)),
                             r.uniform(8, 13), heart, edge, r.uniform(0.2, 0.4)))
    for i, (heart, edge) in enumerate(cores[: r.randint(1, len(cores))]):
        at = spine[len(spine) // 2 + i * 2 - (1 if i else 0)] if i < 2 else spine[0]
        layers.append({"kind": "core", "at_deg": [at[0], round(at[1] + r.uniform(-3, 3), 1)],
                       "radius_deg": round(r.uniform(2.2, 3.6), 1), "halo": round(r.uniform(0.55, 0.75), 2),
                       "colour_srgb": c3(heart), "edge_srgb": c3(edge), "intensity": round(r.uniform(1.0, 1.7), 2),
                       "warp": 0.03, "warp_scale": 3.0, "hides_stars": 0.75})
    core0 = next(l for l in layers if l["kind"] == "core")["at_deg"]
    return {"id": tid, "label": "", "name": name, "for": "the test scenes and demos (composed)", "look": look,
            "composed": True, "seed": seed, "space_srgb": c3(space), "exposure": exposure,
            "vertex_budget": VERTEX_BUDGET,
            "band": {"through_deg": [round(r.uniform(-180, 180)), round(r.uniform(-12, 12))], "tilt_deg": round(r.uniform(12, 32)),
                     "width_deg": round(r.uniform(6, 10), 1), "colour_srgb": c3(band), "intensity": round(r.uniform(0.04, 0.07), 3),
                     "bulge": 1.4, "bulge_width_deg": 30, "warp": 0.06, "warp_scale": 2.5, "grain": 0.35, "grain_scale": 4.0},
            "stars": {"count": r.choice([1400, 1600, 1900]), "band_bias": 2.0,
                      "cluster": {"at_deg": core0, "radius_deg": 2.5, "share": 0.03}},
            "dust_stars": {"density": 0.06, "brightness": 0.75}, "ambient": {"strength": 0.06},
            "layers": layers}


def main():
    with open(RECIPES, encoding="utf-8") as f:
        R = json.load(f)
    hand = [s for s in R["skies"] if not s.get("composed")]
    R["skies"] = hand + [compose(t) for t in THEMES]
    with open(RECIPES, "w", encoding="utf-8") as f:
        f.write(json.dumps(R, indent=1, ensure_ascii=False) + "\n")
    print(f"{RECIPES}: {len(hand)} painted and {len(THEMES)} composed recipes")


if __name__ == "__main__":
    main()
