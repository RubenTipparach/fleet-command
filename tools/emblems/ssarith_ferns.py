#!/usr/bin/env python3
"""Grow the fern bed in the Ssarith badge's recess (owner, 2026-11-05, with a photograph of dense ferns in deep shadow:
"redo the fern, render a fern forrest sccene. Make THAT exact scene!", then "no hologram, just make a scene that looks like
that as background for the emblem, and it should be 3d"). The badge's field is sunk 9 mm below its rim and filled with
ferns in layers, the top ones in the light and the deep ones falling into shadow, as in the photograph (our own scene, not
the photograph). One mesh, read by both the Blender render (tools/blender/emblem_ssarith.py) and the three.js badge
(docs/mockups/ssarith-emblem-3d.html), so they match.

Each frond is bipinnate, as the photograph's: a stem (rachis) with leaflets (pinnae) along both sides, longest a third of
the way up and shrinking to the tip, and each leaflet itself a midrib with small lobed leaflets (pinnules) along both its
sides. The frond arches up from its root and droops toward its tip; leaflets droop a little and angle forward.

Seeded, inside the field (assets/emblems/ssarith/sd.parts.json). Grid units are mm, y down. Writes
assets/emblems/ssarith/sd.ferns.json:
    ferns   {v: [x, y, z, ...] in hundredths of a mm, f: [a, b, c, ...] triangles, s: [light 0-100 per vertex]}

    python3 tools/emblems/ssarith_ferns.py
"""
import json
import math
import os
import random

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
PARTS = os.path.join(ROOT, "assets", "emblems", "ssarith", "sd.parts.json")
OUT = os.path.join(ROOT, "assets", "emblems", "ssarith", "sd.ferns.json")
SEED, FRONDS, MARGIN_MM = 23, 40, 0.6
FLOOR_MM, TOP_MM = 0.6, 6.4          # the recess's floor (the field's face) and the highest a frond may reach
LENGTH_MM = (22.0, 40.0)


def inside(pt, poly):
    x, y = pt
    c = False
    for (x1, y1), (x2, y2) in zip(poly, poly[1:] + poly[:1]):
        if (y1 > y) != (y2 > y) and x < x1 + (y - y1) * (x2 - x1) / (y2 - y1):
            c = not c
    return c


def frond(rng, root, ang, length, z0, verts, faces, light):
    """Append one bipinnate frond's triangles. Light rises with height in the recess (the deep ones in shadow), brighter
    toward each leaflet's tip, and each frond has its own tone."""
    curl = rng.uniform(-0.5, 0.5)
    rise = rng.uniform(1.2, 2.6)                 # mm the stem climbs before it droops
    tone = rng.uniform(0.8, 1.0)

    def stem(t):
        x, y = root
        steps = max(1, int(t * 48))
        for k in range(steps):
            a = ang + curl * (k + 0.5) / 48
            x += math.cos(a) * length * t / steps
            y += math.sin(a) * length * t / steps
        z = z0 + rise * math.sin(math.pi * min(1.0, t * 1.3)) - 0.8 * t * t
        return x, y, min(TOP_MM, max(FLOOR_MM + 0.15, z)), ang + curl * t

    def lit(z, u):
        depth = (z - FLOOR_MM) / (TOP_MM - FLOOR_MM)
        return max(0.05, min(1.0, tone * (0.12 + 0.88 * depth ** 1.4) * (0.75 + 0.25 * u)))

    def add(points):
        """A small leaf as triangles fanned from its first point; points are (x, y, z, u)."""
        base = len(verts) // 3
        for x, y, z, u in points:
            verts.extend((x, y, z))
            light.append(lit(z, u))
        for i in range(1, len(points) - 1):
            faces.extend((base, base + i, base + i + 1))

    # the stem, as a thin ribbon
    prev = None
    for i in range(25):
        x, y, z, a = stem(i / 24)
        w = 0.28 * (1 - 0.7 * i / 24)
        nx, ny = -math.sin(a) * w, math.cos(a) * w
        cur = ((x + nx, y + ny, z + 0.04, 0.3), (x - nx, y - ny, z + 0.04, 0.3))
        if prev:
            add([prev[0], cur[0], cur[1], prev[1]])
        prev = cur
    # the leaflets (pinnae), each with its own small leaflets (pinnules)
    n = int(length / 1.6)
    for k in range(n):
        for side in (-1, 1):
            t = 0.08 + 0.9 * (k + (0.5 if side > 0 else 0.0)) / n
            if t > 0.98:
                continue
            x, y, z, a = stem(t)
            plen = length * 0.3 * (1 - t) ** 0.8 * min(1.0, (t + 0.04) / 0.28)
            if plen < 0.9:
                continue
            pa = a + side * math.radians(rng.uniform(58, 72))
            ca, sa = math.cos(pa), math.sin(pa)
            droop = 0.1 * plen
            m = max(3, int(plen / 0.65))
            for j in range(m):
                u = (j + 0.5) / m
                px, py = x + ca * plen * u, y + sa * plen * u
                pz = max(FLOOR_MM + 0.1, z - droop * u * u)
                ql = plen * 0.2 * (1 - 0.75 * u) + 0.25           # the pinnule's length, shrinking to the leaflet's tip
                qw = ql * 0.36
                for s2 in (-1, 1):
                    qa = pa + s2 * math.radians(55)
                    qc, qs = math.cos(qa), math.sin(qa)
                    nx, ny = -qs, qc
                    pts = [(px, py, pz, u)]
                    # a pinnule: a small pointed leaf out along qa, widest at its middle
                    pts.append((px + qc * ql * 0.45 + nx * qw, py + qs * ql * 0.45 + ny * qw, pz - 0.02, u))
                    pts.append((px + qc * ql, py + qs * ql, pz - 0.05, u))
                    pts.append((px + qc * ql * 0.45 - nx * qw, py + qs * ql * 0.45 - ny * qw, pz - 0.02, u))
                    add(pts)
            # the leaflet's midrib
            add([(x, y, z + 0.03, 0.2), (x + ca * plen, y + sa * plen, max(FLOOR_MM + 0.1, z - droop) + 0.03, 0.9),
                 (x + ca * plen - sa * 0.1, y + sa * plen + ca * 0.1, max(FLOOR_MM + 0.1, z - droop) + 0.03, 0.9),
                 (x - sa * 0.14, y + ca * 0.14, z + 0.03, 0.2)])


def main():
    field = json.load(open(PARTS))["field"][0]
    rng = random.Random(SEED)
    xs, ys = [p[0] for p in field], [p[1] for p in field]
    verts, faces, light = [], [], []
    for i in range(FRONDS):
        # layered: the first fronds lie deepest, each later one a little higher, so the top of the bed is in the light
        z0 = FLOOR_MM + 0.2 + (TOP_MM - FLOOR_MM - 2.4) * i / FRONDS
        for _ in range(200):
            root = (rng.uniform(min(xs) - 8, max(xs) + 8), rng.uniform(min(ys) - 8, max(ys) + 8))
            mid = (sum(xs) / len(xs), sum(ys) / len(ys))
            ang = math.atan2(mid[1] - root[1], mid[0] - root[0]) + rng.uniform(-1.2, 1.2)
            length = rng.uniform(*LENGTH_MM)
            tip = (root[0] + math.cos(ang) * length * 0.6, root[1] + math.sin(ang) * length * 0.6)
            if inside(tip, field):
                break
        frond(rng, root, ang, length, z0, verts, faces, light)
    # The recess's wall clips the bed: drop every triangle with a corner outside the field (less a margin).
    def ok(i):
        return inside((verts[3 * i], verts[3 * i + 1]), field)
    keep = [t for t in range(0, len(faces), 3) if ok(faces[t]) and ok(faces[t + 1]) and ok(faces[t + 2])]
    used = sorted({faces[t + k] for t in keep for k in range(3)})
    remap = {o: n for n, o in enumerate(used)}
    V = [round(verts[3 * o + c] * 100) for o in used for c in range(3)]
    F = [remap[faces[t + k]] for t in keep for k in range(3)]
    S = [round(light[o] * 100) for o in used]
    with open(OUT, "w") as fh:
        json.dump({"note": __doc__.split("\n\n")[0], "seed": SEED, "ferns": {"v": V, "f": F, "s": S}}, fh, separators=(",", ":"))
        fh.write("\n")
    print(f"wrote {os.path.relpath(OUT, ROOT)}: {FRONDS} fronds, {len(used)} vertices, {len(F) // 3} triangles; "
          f"{os.path.getsize(OUT) // 1024} KB")


if __name__ == "__main__":
    main()
