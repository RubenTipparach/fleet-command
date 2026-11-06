"""Fleet Command: the Kelvane Institute's emblem, a hydrogen eigenstate drawn as a wireframe eigen plot.

Owner, 2026-11-06, with a book cover's hydrogen-atom plot: "I would like this hydrogen atom eigen plot used as the
logo/emblem". The cover's art is its publisher's; what we take is the mathematics it plots (CLAUDE.md section 1: take
the shape, not the art). The state is hydrogen's 3d z-squared orbital (n 3, l 2, m 0), whose angular part is
Y20 = (3 cos^2 theta - 1) up to a constant. Its polar plot, the surface at radius |3 cos^2 theta - 1| from the nucleus,
is two lobes along the axis and a ring round the waist, meeting at the nodal cones (theta 54.7 and 125.3 degrees) where the
wavefunction is zero. The Institute's tradition behind it: its founding measurement, the simplest atom's states computed
and then checked against its spectrum, the first truth measured (console-skins design.md, "truth is measured").

This file is the one implementation of the emblem's geometry (CLAUDE.md section 6.1). It draws the surface's wireframe
(meridians and parallels) tilted toward the viewer, removes the hidden lines with a depth buffer made from the surface
itself, and writes the visible lines, in two levels of detail, to assets/ui/kelvane/emblem.json:
  - "fine": every 15 degrees of longitude and 12 parallels a lobe, for an emblem drawn large (a portrait, a page);
  - "coarse": every 30 degrees and 5 parallels a lobe, for the crest, read at 96 px;
  - "mark": every 90 degrees and 1 parallel a lobe, for the name plate's engraved mark, read at about 26 px;
and "outline", the plot's silhouette, drawn heavier round either. It also writes the two-tone render (the shape filled lit and in shadow, far to near) and, from all of these, the 2D
logo as assets/ui/kelvane/emblem.svg. Coordinates are in a unit circle about the nucleus, y up. tools/mockups/kelvane_shapes.py places them on the crest and the
name plate (rendered by tools/blender/build_ui_kelvane.py); docs/mockups/lib/kelvane.js draws them live.

    python3 tools/mockups/kelvane_emblem.py      # writes assets/ui/kelvane/emblem.json
"""
import json
import math
import os

import numpy as np

ROOT = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", ".."))
OUT = os.path.join(ROOT, "assets", "ui", "kelvane", "emblem.json")
SVG = os.path.join(ROOT, "assets", "ui", "kelvane", "emblem.svg")   # the 2D logo: two tones, wireframe, outline

TILT = math.radians(30.0)     # the view looks down on the ring by this much, so the ring reads as a ring
TURN = math.radians(15.0)     # meridians turned off the view's axis, so no two lie on top of each other
GRID = 700                    # depth buffer cells across the unit square
NODE = math.acos(1.0 / math.sqrt(3.0))   # the nodal cone, where 3 cos^2 theta - 1 = 0
GAP = 6                       # hidden samples a wire may skip and still read as one line (depth-test noise)
CRUMB = 0.035                 # visible runs shorter than this (unit circle) are dropped


def radius(theta):
    """The plotted surface's distance from the nucleus: |Y20| up to its constant."""
    return np.abs(3.0 * np.cos(theta) ** 2 - 1.0)


def point(theta, phi):
    """A surface point seen by the viewer: x right, y up, depth toward the viewer (larger is nearer)."""
    r = radius(theta)
    x, y, z = r * np.sin(theta) * np.cos(phi + TURN), r * np.sin(theta) * np.sin(phi + TURN), r * np.cos(theta)
    # tilt about the x axis: the top of the axis leans away, the ring's near side comes down
    return x, z * math.cos(TILT) - y * math.sin(TILT), z * math.sin(TILT) + y * math.cos(TILT)


def depth_buffer(scale):
    """The nearest surface depth in each cell, from a dense sampling of the surface splatted into the grid."""
    th, ph = np.meshgrid(np.linspace(0.0, math.pi, 900), np.linspace(0.0, 2 * math.pi, 1800, endpoint=False), indexing="ij")
    x, y, d = point(th, ph)
    i, j = cell(x, y, scale)
    buf = np.full((GRID, GRID), -np.inf)
    np.maximum.at(buf, (j, i), d)
    # close the pinholes the sampling leaves: a cell takes the nearest of its neighbours where it is empty
    for _ in range(2):
        pad = np.pad(buf, 1, constant_values=-np.inf)
        nb = np.max([pad[1 + dy:GRID + 1 + dy, 1 + dx:GRID + 1 + dx] for dy in (-1, 0, 1) for dx in (-1, 0, 1)], axis=0)
        buf = np.where(np.isfinite(buf), buf, nb)
    return buf


def cell(x, y, scale):
    i = np.clip(((x * scale + 1.0) / 2.0 * (GRID - 1)).round().astype(int), 0, GRID - 1)
    j = np.clip(((1.0 - y * scale) / 2.0 * (GRID - 1)).round().astype(int), 0, GRID - 1)
    return i, j


def visible_runs(theta, phi, buf, scale, eps):
    """A wire's visible stretches: its points that are the nearest surface in their cell, as polylines."""
    x, y, d = point(theta, phi)
    i, j = cell(x, y, scale)
    # a point is seen when nothing in its neighbourhood of cells lies nearer by more than eps
    pad = np.pad(buf, 1, constant_values=-np.inf)
    near = np.max([pad[j + 1 + dy, i + 1 + dx] for dy in (-1, 0, 1) for dx in (-1, 0, 1)], axis=0)
    seen = d >= near - eps
    seen &= radius(theta) > 1e-3
    # bridge hidden gaps of a few samples (the depth test's noise along an edge), then drop crumbs
    k = 0
    while k < len(seen):
        if not seen[k]:
            e = k
            while e < len(seen) and not seen[e]:
                e += 1
            if 0 < k and e < len(seen) and e - k <= GAP:
                seen[k:e] = True
            k = e
        else:
            k += 1
    runs, cur = [], []
    for k in range(len(x)):
        if seen[k]:
            cur.append([round(float(x[k] * scale), 4), round(float(y[k] * scale), 4)])
        else:
            runs.append(cur)
            cur = []
    runs.append(cur)
    return [r for r in runs if len(r) > 1 and sum(math.hypot(a[0] - b[0], a[1] - b[1]) for a, b in zip(r, r[1:])) >= CRUMB]


def silhouette(buf):
    """The plot's outline: for each direction from the nucleus, the furthest cell the surface covers, smoothed."""
    c = (GRID - 1) / 2.0
    pts = []
    for a in np.linspace(0.0, 2 * math.pi, 720, endpoint=False):
        rr = np.arange(0.0, c, 0.5)
        i = np.clip((c + np.cos(a) * rr).round().astype(int), 0, GRID - 1)
        j = np.clip((c - np.sin(a) * rr).round().astype(int), 0, GRID - 1)
        hit = np.nonzero(np.isfinite(buf[j, i]))[0]
        pts.append(rr[hit[-1]] / c if len(hit) else 0.0)
    r = np.array(pts)
    r = np.convolve(np.r_[r[-3:], r, r[:3]], np.ones(7) / 7.0, mode="valid")
    ang = np.linspace(0.0, 2 * math.pi, 720, endpoint=False)
    line = [[round(float(math.cos(a) * v), 4), round(float(math.sin(a) * v), 4)] for a, v in zip(ang, r)]
    return line + [line[0]]


def wires(step_deg, parallels):
    """The wireframe: meridians every step_deg of longitude (each from pole to pole), and parallels spread evenly
    through each lobe and through the ring, clear of the nodal cones where the surface shrinks to the nucleus."""
    out = []
    n = 400
    th = np.linspace(0.0, math.pi, n)
    for k in range(int(round(360 / step_deg))):
        out.append((th, np.full(n, math.radians(k * step_deg))))
    ph = np.linspace(0.0, 2 * math.pi, n)
    bands = [(0.0, NODE), (NODE, math.pi - NODE), (math.pi - NODE, math.pi)]
    for a, b in bands:
        m = parallels if b - a < 1.2 else parallels + 1
        for t in np.linspace(a, b, m + 2)[1:-1]:
            out.append((np.full(n, t), ph))
    return out


LIGHT = np.array([-0.45, 0.62, 0.64]) / np.linalg.norm([-0.45, 0.62, 0.64])  # from the upper left, toward the viewer
LIT = 0.38                    # a face whose light (cosine) is above this takes the lit tone, below it the shadow tone


def view_point(theta, phi):
    """A surface point in the view's frame as one array (x right, y up, z toward the viewer)."""
    x, y, d = point(theta, phi)
    return np.stack([x, y, d], axis=-1)


def two_tone(scale, n_theta=48, n_phi=96):
    """The two-tone render (owner, 2026-11-06: "in 2d logo, I also would like a 2 -tone render with wireframe and shape as
    well"): the surface's faces that look toward the viewer, each lit (1) or in shadow (0) by the cosine of LIGHT on its
    outward normal, painted far to near so the ring and the lobes cover each other as they do in 3D. Consecutive faces of
    one tone are merged into one run, so a page fills each run in one pass, with no seams between its triangles."""
    th = np.linspace(0.0, math.pi, n_theta + 1)
    ph = np.linspace(0.0, 2 * math.pi, n_phi + 1)
    T, F = np.meshgrid(th, ph, indexing="ij")
    P = view_point(T, F)
    tris = []
    for i in range(n_theta):
        for j in range(n_phi):
            for a, b, c in (((i, j), (i + 1, j), (i + 1, j + 1)), ((i, j), (i + 1, j + 1), (i, j + 1))):
                v = np.array([P[a], P[b], P[c]])
                n = np.cross(v[1] - v[0], v[2] - v[0])
                if np.linalg.norm(n) < 1e-9:
                    continue
                cen = v.mean(axis=0)
                if np.dot(n, cen) < 0:          # outward: away from the nucleus
                    n = -n
                n = n / np.linalg.norm(n)
                if n[2] <= 0.0:                 # faces turned away from the viewer are never seen
                    continue
                tone = 1 if float(np.dot(n, LIGHT)) > LIT else 0
                tris.append((float(cen[2]), tone, [round(float(q * scale), 4) for q in v[:, :2].ravel()]))
    tris.sort(key=lambda t: t[0])
    runs = []
    for _d, tone, t in tris:
        if runs and runs[-1]["tone"] == tone:
            runs[-1]["tris"].append(t)
        else:
            runs.append({"tone": tone, "tris": [t]})
    return runs


def svg(d, size=512, lit="#e9c47a", shade="#8a6528", ink="#2a1a06"):
    """The 2D logo as an SVG: the two-tone fill, the fine wireframe over it and the outline, in ink."""
    c = size / 2.0
    k = c * 0.98
    P = lambda x, y: f"{c + x * k:.2f},{c - y * k:.2f}"
    out = [f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 {size} {size}" width="{size}" height="{size}">',
           '<title>Kelvane Institute: the hydrogen 3d z2 eigen plot</title>']
    for r in d["tones"]:
        path = "".join(f"M{P(t[0], t[1])}L{P(t[2], t[3])}L{P(t[4], t[5])}Z" for t in r["tris"])
        col = lit if r["tone"] else shade
        out.append(f'<path d="{path}" fill="{col}" stroke="{col}" stroke-width="0.6" stroke-linejoin="round"/>')
    for l in d["fine"]:
        out.append(f'<polyline points="{" ".join(P(x, y) for x, y in l)}" fill="none" stroke="{ink}" stroke-width="{size / 512 * 1.6:.2f}" stroke-linecap="round" stroke-linejoin="round"/>')
    out.append(f'<polyline points="{" ".join(P(x, y) for x, y in d["outline"])}" fill="none" stroke="{ink}" stroke-width="{size / 512 * 4.2:.2f}" stroke-linejoin="round"/>')
    out.append("</svg>")
    return "\n".join(out) + "\n"


def build():
    th, ph = np.meshgrid(np.linspace(0, math.pi, 200), np.linspace(0, 2 * math.pi, 400), indexing="ij")
    x, y, _d = point(th, ph)
    scale = 0.96 / float(np.max(np.hypot(x, y)))   # the whole plot inside the unit circle, 4 % clear
    buf = depth_buffer(scale)
    eps = 4.0 * 2.0 * 2.0 / GRID / scale             # depth slack: a few cells' worth of surface slope
    levels = {}
    for name, step, par in (("fine", 15, 12), ("coarse", 30, 5), ("mark", 90, 1)):
        lines = []
        for t, p in wires(step, par):
            lines += visible_runs(t, p, buf, scale, eps)
        levels[name] = lines
    levels["outline"] = silhouette(buf)
    levels["tones"] = two_tone(scale)
    return {"state": "hydrogen 3d z^2 (n 3, l 2, m 0): surface at |3 cos^2 theta - 1|", "tilt_deg": math.degrees(TILT),
            "turn_deg": math.degrees(TURN), "units": "unit circle about the nucleus, y up", **levels}


def main():
    d = build()
    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    with open(OUT, "w") as f:
        json.dump(d, f, separators=(",", ":"))
        f.write("\n")
    with open(SVG, "w") as f:
        f.write(svg(d))
    print(f"wrote {os.path.relpath(OUT, ROOT)}: fine {len(d['fine'])} lines, coarse {len(d['coarse'])} lines, "
          f"{sum(len(r['tris']) for r in d['tones'])} faces in {len(d['tones'])} tone runs; and {os.path.relpath(SVG, ROOT)}")


if __name__ == "__main__":
    main()
