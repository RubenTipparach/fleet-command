#!/usr/bin/env python3
"""Grow the ferns and spill the blood on the Ssarith badge (owner, 2026-11-05, with a photograph of dense ferns in shadow:
"3d model Fern leaves", "Arrows dripping with bright glowing green blood"). One layout, read by both the Blender render
(tools/blender/emblem_ssarith.py) and the three.js badge (docs/mockups/ssarith-emblem-3d.html), so they match.

Ferns: fronds rooted around the field, each a stem (rachis) arching up off the enamel and curving to one side, with
leaflets (pinnae) along both sides, longest a third of the way up and shrinking to the tip, each leaflet lobed along its
edges and drooping a little. Built here as finished meshes, so both consumers draw the same triangles.
Blood: on each arrowhead, pools over the steel and drips running down the badge's face (grid +y, as it hangs), each drip
a stream ending in a drop.

Seeded, inside the field (assets/emblems/ssarith/sd.parts.json). Grid units are mm, y down. Writes
assets/emblems/ssarith/sd.ferns.json:
    ferns   {v: [x, y, z, ...] in hundredths of a mm, f: [a, b, c, ...] triangles, s: [shade 0-100 per vertex]}
    blood   [{head: i, rings: [[[x, y], ...], ...]}] per arrowhead: the pools and drips as polygons

    python3 tools/emblems/ssarith_ferns.py
"""
import json
import math
import os
import random

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
PARTS = os.path.join(ROOT, "assets", "emblems", "ssarith", "sd.parts.json")
OUT = os.path.join(ROOT, "assets", "emblems", "ssarith", "sd.ferns.json")
SEED, FRONDS, MARGIN_MM = 11, 60, 1.5
Z_BASE_MM, Z_TOP_MM = (1.15, 1.9), 2.7   # a frond's root height, and no fern above this (the bows stand over them)


def inside(pt, poly):
    x, y = pt
    c = False
    for (x1, y1), (x2, y2) in zip(poly, poly[1:] + poly[:1]):
        if (y1 > y) != (y2 > y) and x < x1 + (y - y1) * (x2 - x1) / (y2 - y1):
            c = not c
    return c


def edge_dist(pt, poly):
    best = 1e9
    for (x1, y1), (x2, y2) in zip(poly, poly[1:] + poly[:1]):
        dx, dy = x2 - x1, y2 - y1
        t = max(0.0, min(1.0, ((pt[0] - x1) * dx + (pt[1] - y1) * dy) / (dx * dx + dy * dy)))
        best = min(best, math.hypot(pt[0] - x1 - t * dx, pt[1] - y1 - t * dy))
    return best


def frond(rng, root, ang, length, z0):
    """One frond's triangles: (verts [(x, y, z)], faces [(a, b, c)], shades [0-1])."""
    verts, faces, shade = [], [], []
    curl = rng.uniform(-0.6, 0.6)            # radians the stem turns over its length
    arch = rng.uniform(0.35, 0.7)            # mm it rises at its middle
    tone = rng.uniform(0.75, 1.0)            # this frond's light: some sit in shadow

    def stem(t):
        a = ang + curl * t
        # integrate the turning stem in small steps
        x, y, steps = root[0], root[1], max(1, int(t * 40))
        for k in range(steps):
            aa = ang + curl * (k + 0.5) / 40
            x += math.cos(aa) * length / 40
            y += math.sin(aa) * length / 40
        z = min(Z_TOP_MM, z0 + arch * math.sin(math.pi * t) - 0.25 * t * t)
        return x, y, z, a

    def strip(points, widths, z_of, light):
        """A ribbon down `points` with half-widths `widths`: three vertices across (edge, middle, edge)."""
        base = len(verts)
        n = len(points)
        for i, ((x, y, a), w) in enumerate(zip(points, widths)):
            nx, ny = -math.sin(a), math.cos(a)
            for side in (-1, 0, 1):
                verts.append((x + nx * w * side, y + ny * w * side, z_of(i, side)))
                shade.append(light(i, side))
        for i in range(n - 1):
            for j in range(2):
                p0 = base + i * 3 + j
                faces.append((p0, p0 + 3, p0 + 1))
                faces.append((p0 + 1, p0 + 3, p0 + 4))

    # the stem
    pts = [stem(i / 24) for i in range(25)]
    strip([(p[0], p[1], p[3]) for p in pts], [0.22 * (1 - 0.6 * i / 24) for i in range(25)],
          lambda i, s: pts[i][2] + 0.05, lambda i, s: 0.55 * tone)
    # the leaflets, both sides, alternating a little
    n = max(8, int(length / 1.3))
    for k in range(n):
        for side in (-1, 1):
            t = 0.1 + 0.88 * (k + (0.5 if side > 0 else 0.0)) / n
            if t > 0.97:
                continue
            x, y, z, a = stem(t)
            plen = length * 0.27 * (1 - t) ** 0.75 * min(1.0, (t + 0.05) / 0.3)
            if plen < 0.6:
                continue
            pa = a + side * math.radians(rng.uniform(55, 68))
            wmax = plen * 0.17
            S = 7
            ppts, widths = [], []
            for i in range(S + 1):
                u = i / S
                ppts.append((x + math.cos(pa) * plen * u, y + math.sin(pa) * plen * u, pa))
                lobe = 0.72 + 0.28 * math.cos(2 * math.pi * u * 3.5)          # the pinnules: a lobed edge
                widths.append(wmax * math.sin(math.pi * min(1.0, u * 0.9 + 0.05)) ** 0.7 * lobe)
            droop = 0.12 * plen
            strip(ppts, widths, lambda i, s, z=z: max(1.08, z - droop * (i / S) ** 2 + 0.03 * abs(s)),
                  lambda i, s: tone * (0.6 + 0.4 * i / S) * (0.85 + 0.15 * abs(s)))
    return verts, faces, shade


def main():
    parts = json.load(open(PARTS))
    field = parts["field"][0]
    rng = random.Random(SEED)
    V, F, Sh = [], [], []
    made, tries = 0, 0
    xs = [p[0] for p in field]
    ys = [p[1] for p in field]
    while made < FRONDS and tries < 4000:
        tries += 1
        root = (rng.uniform(min(xs), max(xs)), rng.uniform(min(ys), max(ys)))
        if not inside(root, field):
            continue
        length = rng.uniform(14, 28)
        ang = rng.uniform(0, 2 * math.pi)
        z0 = rng.uniform(*Z_BASE_MM)
        v, f, s = frond(rng, root, ang, length, z0)
        if any(not inside((p[0], p[1]), field) or edge_dist((p[0], p[1]), field) < MARGIN_MM for p in v):
            continue
        base = len(V)
        V += v
        F += [(a + base, b + base, c + base) for a, b, c in f]
        Sh += s
        made += 1
    blood = []
    for i, bow in enumerate(parts["bows"]):
        head = bow["head"]
        tip = max(head, key=lambda p: p[0])
        cx = sum(p[0] for p in head) / len(head)
        cy = sum(p[1] for p in head) / len(head)
        rings = []
        # pools over the steel: a few rounded blots along the head
        for k in range(3):
            px = tip[0] - 1.5 - k * rng.uniform(1.6, 2.4)
            r = rng.uniform(0.6, 0.95)
            py = cy + rng.uniform(-0.6, 0.6)
            rings.append([[round(px + r * math.cos(a) * (1.3 if j % 2 else 1.0), 2), round(py + r * math.sin(a), 2)]
                          for j, a in enumerate(x * 2 * math.pi / 16 for x in range(16))])
        # drips from the head's lower edge, down the face (grid +y): a stream that thins, then a drop
        low = sorted(head, key=lambda p: -p[1])[:3]
        for k in range(2 + (i == 2)):
            px = low[k % len(low)][0] + rng.uniform(-0.6, 0.6)
            top = max(p[1] for p in head) - 0.6
            ln = rng.uniform(3.0, 7.5)
            w0, w1, rd = rng.uniform(0.45, 0.6), rng.uniform(0.22, 0.3), rng.uniform(0.75, 1.05)
            left = [[px - w0 + (w0 - w1) * u, top + ln * u] for u in (0, 0.5, 1)]
            right = [[px + w0 - (w0 - w1) * u, top + ln * u] for u in (1, 0.5, 0)]
            drop = [[px + rd * math.cos(a), top + ln + rd + rd * math.sin(a)] for a in
                    (math.pi * (1.25 + 1.5 * j / 10) for j in range(11))]
            # the drop's arc runs from upper right round the bottom to upper left; order: left down, drop, right up
            ring = left + [[px - w1, top + ln + 0.2]] + list(reversed(drop)) + [[px + w1, top + ln + 0.2]] + right
            rings.append([[round(a, 2), round(b, 2)] for a, b in ring])
        blood.append({"head": i, "rings": rings})
    out = {"note": __doc__.split("\n\n")[0], "seed": SEED,
           "ferns": {"v": [round(c * 100) for p in V for c in p], "f": [i for t in F for i in t],
                     "s": [round(x * 100) for x in Sh]},
           "blood": blood}
    with open(OUT, "w") as fh:
        json.dump(out, fh, separators=(",", ":"))
        fh.write("\n")
    print(f"wrote {os.path.relpath(OUT, ROOT)}: {made} fronds, {len(V)} vertices, {len(F)} triangles, "
          f"{sum(len(b['rings']) for b in blood)} blood shapes; {os.path.getsize(OUT) // 1024} KB")


if __name__ == "__main__":
    main()
