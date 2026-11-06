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
BADGE = os.path.join(ROOT, "assets", "emblems", "kelvane")             # the 3D badge's orbital and its grid mask
BADGE_R_MM = 40.0             # the plot's unit circle on the badge, mm (inside the oval's opening)
# The coin (owner, 2026-11-06: "green hydrogen icon, irrdescent and red satin soft fabric background on an oval emblem
# coin"): an upright oval, as tall as the plot, its rim between the outer and inner ellipses (semi-axes x, y in mm); the
# satin draped inside it from SATIN_Z up through its folds, and the orbital floating above the folds' crests.
OVAL_OUT = (39.0, 49.0)
OVAL_IN = (35.0, 45.0)
SATIN_Z = (0.6, 2.0)          # the satin's floor at the rim and its highest crest, mm
RELIEF = (2.4, 5.4)           # the orbital's relief, its farthest point clear of the highest crest, under the 6.2 mm rim
RIM_Z = 6.2
# The emblem's colours, KID's (the Emblem Design Guide; owner, 2026-11-06: "on the emblem, background red, shape green",
# "the red background should be like a satin texture", "green hydrogen icon, irrdescent and red satin soft fabric
# background on an oval emblem coin"): one palette for the 2D logo, the console's live emblem, the hull logo and the badge.
PALETTE = {"frame": "#e6d8b8", "field": "#8c1a1f", "sheen": "#d4434a", "shade": "#4f0b0f",
           "lit": "#2fd36a", "dark": "#137a3c", "wire": "#a6ffbe", "outline": "#0c4a24"}

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


def two_tone(scale, n_theta=40, n_phi=80):
    """The two-tone render (owner, 2026-11-06: "in 2d logo, I also would like a 2 -tone render with wireframe and shape as
    well"): the surface's faces that look toward the viewer, painted far to near so the ring and the lobes cover each other
    as they do in 3D, each lit or in shadow by the cosine of LIGHT on the surface's outward normal. The light is taken at
    every vertex and a face it crosses is split along the threshold (LIT), so the edge between the tones is a smooth line,
    not the mesh's steps. Consecutive pieces of one tone are merged into one run (a list of polygons), so a page fills each
    run in one pass, with no seams."""
    th = np.linspace(1e-4, math.pi - 1e-4, n_theta + 1)
    ph = np.linspace(0.0, 2 * math.pi, n_phi + 1)
    T, F = np.meshgrid(th, ph, indexing="ij")
    P = view_point(T, F)
    # the outward normal at every vertex, from the surface's two tangents (central differences), and its light
    h = 1e-4
    dT = view_point(T + h, F) - view_point(T - h, F)
    dF = view_point(T, F + h) - view_point(T, F - h)
    N = np.cross(dT, dF)
    N /= np.maximum(np.linalg.norm(N, axis=-1, keepdims=True), 1e-12)
    N *= np.where(np.sum(N * P, axis=-1, keepdims=True) < 0, -1.0, 1.0)
    L = N @ LIGHT - LIT
    pieces = []
    for i in range(n_theta):
        for j in range(n_phi):
            for a, b, c in (((i, j), (i + 1, j), (i + 1, j + 1)), ((i, j), (i + 1, j + 1), (i, j + 1))):
                v = np.array([P[a], P[b], P[c]])
                n = np.cross(v[1] - v[0], v[2] - v[0])
                if np.linalg.norm(n) < 1e-12:
                    continue
                cen = v.mean(axis=0)
                if np.dot(n, cen) < 0:
                    n = -n
                if n[2] <= 0.0:                 # faces turned away from the viewer are never seen
                    continue
                d = float(cen[2])
                lv = [L[a], L[b], L[c]]
                for tone, poly in split(v[:, :2], lv):
                    pieces.append((d, tone, [round(float(q * scale), 4) for xy in poly for q in xy]))
    pieces.sort(key=lambda t: t[0])
    runs = []
    for _d, tone, poly in pieces:
        if runs and runs[-1]["tone"] == tone:
            runs[-1]["polys"].append(poly)
        else:
            runs.append({"tone": tone, "polys": [poly]})
    return runs


def split(xy, lv):
    """A triangle cut along the light's threshold: its lit part (tone 1, where lv > 0) and its shadow part (tone 0), each a
    polygon, the cut placed where the light crosses the threshold along each edge."""
    lit, shade = [], []
    for k in range(3):
        p, q, a, b = xy[k], xy[(k + 1) % 3], lv[k], lv[(k + 1) % 3]
        (lit if a > 0 else shade).append(p)
        if (a > 0) != (b > 0):
            t = a / (a - b)
            m = p + (q - p) * t
            lit.append(m)
            shade.append(m)
    return [(1, lit)] * (len(lit) >= 3) + [(0, shade)] * (len(shade) >= 3)


def svg(d, px=200.0):
    """The 2D logo as an SVG (owner, 2026-11-06: "the 2d version the green wireframe and oval background can be used for
    ship hull logo"): the oval coin, its champagne rim round a red satin field (soft bright and dark bands across it, as
    satin catches the light), and on it the plot: its shape in two greens, lit and in shadow, the wireframe in light green
    and the silhouette in a deeper green. px is the size of the plot's unit circle; the oval is the badge's, in its units."""
    k = PALETTE
    u = 1.0 / BADGE_R_MM * 0.96 / 0.96          # badge mm to plot units (the plot's unit circle is BADGE_R_MM)
    ao, bo = OVAL_OUT[0] * u * px, OVAL_OUT[1] * u * px
    ai, bi = OVAL_IN[0] * u * px, OVAL_IN[1] * u * px
    w, h = 2 * ao + 8, 2 * bo + 8
    cx, cy = w / 2, h / 2
    P = lambda x, y: f"{cx + x * px:.2f},{cy - y * px:.2f}"
    stops = [(0, k["shade"]), (0.18, k["field"]), (0.34, k["sheen"]), (0.46, k["field"]), (0.6, k["shade"]), (0.74, k["field"]),
             (0.86, k["sheen"]), (1, k["field"])]
    out = [f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 {w:.0f} {h:.0f}" width="{w:.0f}" height="{h:.0f}">',
           '<title>Kelvane Institute: the hydrogen 3d z2 eigen plot</title>',
           '<defs><linearGradient id="satin" x1="0.1" y1="0" x2="0.9" y2="1">'
           + "".join(f'<stop offset="{o}" stop-color="{c}"/>' for o, c in stops) + '</linearGradient></defs>',
           f'<ellipse cx="{cx:.2f}" cy="{cy:.2f}" rx="{ao:.2f}" ry="{bo:.2f}" fill="{k["frame"]}"/>',
           f'<ellipse cx="{cx:.2f}" cy="{cy:.2f}" rx="{ai:.2f}" ry="{bi:.2f}" fill="url(#satin)"/>']
    for r in d["tones"]:
        path = "".join("M" + "L".join(P(t[q], t[q + 1]) for q in range(0, len(t), 2)) + "Z" for t in r["polys"])
        col = k["lit"] if r["tone"] else k["dark"]
        out.append(f'<path d="{path}" fill="{col}" stroke="{col}" stroke-width="0.6" stroke-linejoin="round"/>')
    for l in d["fine"]:
        out.append(f'<polyline points="{" ".join(P(x, y) for x, y in l)}" fill="none" stroke="{k["wire"]}" stroke-width="{px / 200 * 1.3:.2f}" stroke-linecap="round" stroke-linejoin="round"/>')
    out.append(f'<polyline points="{" ".join(P(x, y) for x, y in d["outline"])}" fill="none" stroke="{k["outline"]}" stroke-width="{px / 200 * 3.4:.2f}" stroke-linejoin="round"/>')
    out.append("</svg>")
    return "\n".join(out) + "\n"


def parallels():
    """The fine level's parallels (theta, radians): spread evenly through each lobe and the ring, as wires() spreads them."""
    out = []
    for a, b in [(0.0, NODE), (NODE, math.pi - NODE), (math.pi - NODE, math.pi)]:
        m = 12 if b - a < 1.2 else 13
        out += [float(t) for t in np.linspace(a, b, m + 2)[1:-1]]
    return out


def badge_mesh(n_theta=48, n_phi=96):
    """The orbital as the 3D badge's relief (owner, 2026-11-06: "shade the 3d plot in the 3d render of the emblem, use our
    emblem designer for this please"): the surface seen as the 2D logo sees it (TILT, TURN), laid in the badge's plane at
    BADGE_R_MM, its depth pressed into the guide's device band (RELIEF), so it stands as a low relief like every badge's
    device. Each vertex keeps the true surface's outward normal, so the relief is lit and shaded as the full orbital is.
    UVs: u the longitude (0-1 round the axis), v the colatitude (0 at the top pole, 1 at the bottom), which the grid
    mask's lines follow. Vertices in badge mm (x right, y up, z out of the badge), faces as vertex index triples."""
    th = np.linspace(1e-4, math.pi - 1e-4, n_theta + 1)
    ph = np.linspace(0.0, 2 * math.pi, n_phi + 1)
    T, F = np.meshgrid(th, ph, indexing="ij")
    P = view_point(T, F)
    h = 1e-4
    N = np.cross(view_point(T + h, F) - view_point(T - h, F), view_point(T, F + h) - view_point(T, F - h))
    N /= np.maximum(np.linalg.norm(N, axis=-1, keepdims=True), 1e-12)
    N *= np.where(np.sum(N * P, axis=-1, keepdims=True) < 0, -1.0, 1.0)
    th2, ph2 = np.meshgrid(np.linspace(0, math.pi, 200), np.linspace(0, 2 * math.pi, 400), indexing="ij")
    x2, y2, _ = point(th2, ph2)
    scale = 0.96 / float(np.max(np.hypot(x2, y2))) * BADGE_R_MM
    d0, d1 = float(P[..., 2].min()), float(P[..., 2].max())
    V = np.stack([P[..., 0] * scale, P[..., 1] * scale, RELIEF[0] + (P[..., 2] - d0) / (d1 - d0) * (RELIEF[1] - RELIEF[0])], axis=-1)
    U = np.stack([F / (2 * math.pi), T / math.pi], axis=-1)
    W = n_phi + 1
    faces = []
    for i in range(n_theta):
        for j in range(n_phi):
            a, b, c, d = i * W + j, (i + 1) * W + j, (i + 1) * W + j + 1, i * W + j + 1
            faces += [[a, b, c], [a, c, d]]
    r4 = lambda arr: [round(float(v), 4) for v in arr.reshape(-1)]
    return {"units": "badge mm: x right, y up, z out of the badge", "relief_mm": list(RELIEF), "radius_mm": BADGE_R_MM,
            "positions": r4(V), "normals": r4(N), "uvs": r4(U), "faces": [k for f in faces for k in f]}


def grid_mask(w=2048, h=1024, line_px=5.0, meridian_px=16.0):
    """The wireframe as a mask over the badge mesh's UVs (white on the lines): meridians every 15 degrees of longitude
    and the fine level's parallels, the same lines the 2D logo and the crest draw. A meridian is drawn wider in the map
    (meridian_px of its 2048 px round the axis) than a parallel (line_px of its 1024 px pole to pole), because the map is
    stretched far more round a lobe's narrow girth than along its length; on the badge they come out near one width."""
    from PIL import Image
    u = np.arange(w)[None, :] / w
    v = np.arange(h)[:, None] / h
    m = np.zeros((h, w))
    for k in range(24):
        du = np.abs(((u - k / 24 + 0.5) % 1.0) - 0.5) * w
        m = np.maximum(m, np.clip(meridian_px / 2 + 0.5 - du, 0, 1))
    for t in parallels():
        dv = np.abs(v - t / math.pi) * h
        m = np.maximum(m, np.clip(line_px / 2 + 0.5 - dv, 0, 1))
    return Image.fromarray((np.broadcast_to(m, (h, w)) * 255).astype(np.uint8), "L")


def satin_mesh(n_r=40, n_a=192):
    """The red satin the orbital floats over: a cloth draped inside the oval in soft rounded folds that run out from the
    middle, a few broad ones and some smaller between them, gathered a little toward the bottom, lying flat where it is
    tucked under the rim. A height field over the oval (polar rings, the centre one point), with its normals and UVs (u
    round the oval, v from the middle out: the direction its folds run, which the satin's sheen follows)."""
    a, b = OVAL_IN[0] + 0.6, OVAL_IN[1] + 0.6   # tucked a hair under the rim's inner edge
    def height(r, t):
        fold = (0.55 * np.sin(7 * t + 0.9 * np.sin(2 * t) + 0.4) + 0.3 * np.sin(13 * t + 1.7 * np.cos(3 * t)) + 0.15 * np.sin(23 * t + 0.5))
        gather = 1.0 + 0.35 * np.sin(t - math.pi / 2)        # deeper folds toward the bottom, where the cloth gathers
        rise = np.clip(r / 0.25, 0, 1) ** 0.7 * (1 - np.clip((r - 0.86) / 0.14, 0, 1)) ** 1.5   # flat at the middle and the tuck
        z = 0.5 + 0.5 * fold * gather * rise
        return SATIN_Z[0] + (SATIN_Z[1] - SATIN_Z[0]) * np.clip(z * (0.45 + 0.55 * rise), 0, 1)
    R = np.linspace(0.0, 1.0, n_r + 1)
    T = np.linspace(0.0, 2 * math.pi, n_a, endpoint=False)
    rr, tt = np.meshgrid(R, T, indexing="ij")
    X, Y = a * rr * np.cos(tt), b * rr * np.sin(tt)
    Z = height(rr, tt)
    e = 1e-3
    def hz(x, y):
        r = np.sqrt((x / a) ** 2 + (y / b) ** 2)
        return height(np.clip(r, 0, 1), np.arctan2(y / b, x / a))
    nx = -(hz(X + e, Y) - hz(X - e, Y)) / (2 * e)
    ny = -(hz(X, Y + e) - hz(X, Y - e)) / (2 * e)
    N = np.stack([nx, ny, np.ones_like(nx)], axis=-1)
    N /= np.linalg.norm(N, axis=-1, keepdims=True)
    V = np.stack([X, Y, Z], axis=-1)
    U = np.stack([tt / (2 * math.pi), rr], axis=-1)
    faces = []
    idx = lambda i, j: i * n_a + (j % n_a)
    for i in range(n_r):
        for j in range(n_a):
            p, q, r_, s_ = idx(i, j), idx(i + 1, j), idx(i + 1, j + 1), idx(i, j + 1)
            faces += [[p, q, r_], [p, r_, s_]] if i else [[p, q, r_]]   # the centre ring's quads fold to triangles
    r4 = lambda arr: [round(float(v), 4) for v in arr.reshape(-1)]
    return {"units": "badge mm: x right, y up, z out of the badge", "oval_out": list(OVAL_OUT), "oval_in": list(OVAL_IN),
            "rim_z": RIM_Z, "satin_z": list(SATIN_Z), "palette": PALETTE, "positions": r4(V), "normals": r4(N), "uvs": r4(U),
            "faces": [k for f in faces for k in f]}


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
    levels["palette"] = PALETTE
    levels["oval"] = {"out": [v / BADGE_R_MM for v in OVAL_OUT], "in": [v / BADGE_R_MM for v in OVAL_IN], "units": "the plot's unit circle"}
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
    os.makedirs(BADGE, exist_ok=True)
    with open(os.path.join(BADGE, "ka.orbital.json"), "w") as f:
        json.dump(badge_mesh(), f, separators=(",", ":"))
        f.write("\n")
    with open(os.path.join(BADGE, "ka.satin.json"), "w") as f:
        json.dump(satin_mesh(), f, separators=(",", ":"))
        f.write("\n")
    grid_mask().save(os.path.join(BADGE, "ka.grid.png"), optimize=True)
    print(f"wrote {os.path.relpath(OUT, ROOT)}: fine {len(d['fine'])} lines, coarse {len(d['coarse'])} lines, "
          f"{sum(len(r['polys']) for r in d['tones'])} pieces in {len(d['tones'])} tone runs; and {os.path.relpath(SVG, ROOT)}")


if __name__ == "__main__":
    main()
