#!/usr/bin/env python3
"""Lay out the holographic leaves over the Ssarith badge's field (owner, 2026-11-05: "In the 3d version I mean with like
holographic leaves", "Should be 3d leaves shapes"): a dry forest's canopy projected as light above the enamel forest,
leaves hanging at three depths so they shift against each other as the badge turns. One layout, read by both the Blender
render (tools/blender/emblem_ssarith.py) and the three.js badge (docs/mockups/ssarith-emblem-3d.html), so they match.

Seeded (the same leaves every run), inside the field's polygon (assets/emblems/ssarith/sd.parts.json) with a margin.
Grid units are mm, y down, as the parts. Writes assets/emblems/ssarith/sd.leaves.json:
    outline   one leaf, unit length along +x from its stalk, its half-widths from a pointed oval (a dry leaf, not round)
    leaves    [x, y, z_mm, length_mm, angle_deg, bend, tint] each; tint 0 dry green, 1 olive, 2 straw

    python3 tools/emblems/ssarith_leaves.py
"""
import json
import math
import os
import random

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
PARTS = os.path.join(ROOT, "assets", "emblems", "ssarith", "sd.parts.json")
OUT = os.path.join(ROOT, "assets", "emblems", "ssarith", "sd.leaves.json")
SEED, COUNT, MARGIN_MM = 7, 70, 4.0
DEPTHS_MM = (1.4, 2.1, 2.8)          # three layers above the field's face (1.0 mm)
LENGTH_MM = (4.0, 8.5)


def inside(pt, poly):
    x, y = pt
    c = False
    for (x1, y1), (x2, y2) in zip(poly, poly[1:] + poly[:1]):
        if (y1 > y) != (y2 > y) and x < x1 + (y - y1) * (x2 - x1) / (y2 - y1):
            c = not c
    return c


def dist_to_edge(pt, poly):
    best = 1e9
    for (x1, y1), (x2, y2) in zip(poly, poly[1:] + poly[:1]):
        dx, dy = x2 - x1, y2 - y1
        t = max(0.0, min(1.0, ((pt[0] - x1) * dx + (pt[1] - y1) * dy) / (dx * dx + dy * dy)))
        best = min(best, math.hypot(pt[0] - x1 - t * dx, pt[1] - y1 - t * dy))
    return best


def main():
    field = json.load(open(PARTS))["field"][0]
    rng = random.Random(SEED)
    # One leaf: 13 points down each side, pointed at both ends, widest a third of the way from the stalk.
    side = [(t / 12, 0.3 * math.sin(math.pi * (t / 12) ** 0.8) ** 0.9) for t in range(13)]
    outline = [[round(u, 4), round(w, 4)] for u, w in side] + [[round(u, 4), round(-w, 4)] for u, w in reversed(side[1:-1])]
    leaves, tries = [], 0
    while len(leaves) < COUNT and tries < 20000:
        tries += 1
        x, y = rng.uniform(0, 100), rng.uniform(0, 100)
        length = rng.uniform(*LENGTH_MM)
        if not inside((x, y), field) or dist_to_edge((x, y), field) < MARGIN_MM + length:
            continue
        z = DEPTHS_MM[len(leaves) % 3]
        leaves.append([round(x, 2), round(y, 2), z, round(length, 2), round(rng.uniform(0, 360), 1),
                       round(rng.uniform(-0.25, 0.25), 3), rng.choice((0, 0, 1, 2))])
    json.dump({"note": __doc__.split("\n\n")[0], "seed": SEED, "outline": outline, "leaves": leaves}, open(OUT, "w"))
    open(OUT, "a").write("\n")
    print(f"wrote {os.path.relpath(OUT, ROOT)}: {len(leaves)} leaves")


if __name__ == "__main__":
    main()
