#!/usr/bin/env python3
"""Top-down ship schematics: two white layers per ship (a shaded hull and a quad wireframe) for every console skin.

What it owns: reading the exported ship glbs (assets/ships/<faction>/<id>.glb, written by
tools/mockups/export_ships.mjs), turning their triangles back into quads, and rendering each ship from directly
above, nose up, into assets/ships/schematics/<faction>/<id>.hull.png and <id>.wire.png, with
assets/ships/schematics/manifest.json (sizes, metres per pixel, the origin's pixel, counts and the source glb's
sha256) and a contact sheet, docs/screenshots/schematics/sheet.png.

Why here: the schematics are drawn from the glbs, which are themselves exported from the approved ship mockups
(CLAUDE.md section 14), so there is one copy of every hull and this is only a view of it. Both layers are pure white
RGB with the drawing in alpha, so a skin tints the hull with a dark colour and the wire with a bright one and lays
one over the other (owner, 2026-11-05: "use 2 differnt layers both white so we can recombine them for each ui
skin"). The game and the store use them to preview a ship (the same owner note).

The run is deterministic: no clocks, no randomness, fixed ordering everywhere; a second run gives byte-identical
files. Needs only numpy and Pillow.

    python3 tools/mockups/ship_schematics.py
"""
from __future__ import annotations

import hashlib
import json
import math
import pathlib
import struct
import sys

import numpy as np
from PIL import Image, ImageDraw

ROOT = pathlib.Path(__file__).resolve().parents[2]
SHIPS = ROOT / "assets" / "ships"
OUT = SHIPS / "schematics"
SHEET = ROOT / "docs" / "screenshots" / "schematics" / "sheet.png"
FACTIONS = ("concord", "krai")

SIZE_PX = 1024          # the square canvas of each layer
MARGIN = 0.06           # fraction of the canvas left clear on each side of the longest extent
SS = 4                  # supersampling factor for both layers
WIRE_PX = 1.6           # wire line width in final pixels
OUTLINE_PX = 2.2        # outline width in final pixels
HULL_ALPHA = (0.15, 0.55)  # hull alpha from fully shadowed to fully lit
LIGHT = np.array([-0.35, 1.0, -0.45])  # top light, a little from the bow and port side (glb frame)
COPLANAR_DEG = 8.0      # two triangles pair into a quad only if their normals are within this angle
UP_FACING = 0.2         # an edge is drawn only if a face beside it has normal.y at least this (facing up)
FEATURE_DEG = 2.0       # two faces bending less than this across an edge lie in one plane
COPLANAR_GAP_PX = 36.0  # parallel tessellation lines in one plane are thinned to at least this far apart
MIN_EDGE_PX = 3.0       # edges shorter than this on screen are left out (dense tessellation reads as noise)
SKIP_MATERIALS = ("registry",)  # registry decals are flat cards over the hull, not hull shape

# ---------------------------------------------------------------------------------------------------------------
# glb reading


COMPONENT = {5120: np.int8, 5121: np.uint8, 5122: np.int16, 5123: np.uint16, 5125: np.uint32, 5126: np.float32}
WIDTH = {"SCALAR": 1, "VEC2": 2, "VEC3": 3, "VEC4": 4, "MAT4": 16}


def read_glb(path: pathlib.Path) -> tuple[dict, bytes]:
    """Return the glTF JSON and the BIN chunk of a binary glTF file."""
    data = path.read_bytes()
    magic, version, _length = struct.unpack_from("<III", data, 0)
    if magic != 0x46546C67 or version != 2:
        raise ValueError(f"{path}: not a glTF 2 binary")
    off = 12
    gltf, binary = None, b""
    while off < len(data):
        clen, ctype = struct.unpack_from("<II", data, off)
        chunk = data[off + 8: off + 8 + clen]
        if ctype == 0x4E4F534A:
            gltf = json.loads(chunk)
        elif ctype == 0x004E4942:
            binary = chunk
        off += 8 + clen
    if gltf is None:
        raise ValueError(f"{path}: no JSON chunk")
    return gltf, binary


def accessor(gltf: dict, binary: bytes, index: int) -> np.ndarray:
    """Read one accessor as an (count, width) array, honouring the buffer view's stride."""
    acc = gltf["accessors"][index]
    view = gltf["bufferViews"][acc["bufferView"]]
    dtype = np.dtype(COMPONENT[acc["componentType"]])
    width = WIDTH[acc["type"]]
    count = acc["count"]
    start = view.get("byteOffset", 0) + acc.get("byteOffset", 0)
    stride = view.get("byteStride", dtype.itemsize * width)
    raw = np.frombuffer(binary, dtype=np.uint8, count=stride * (count - 1) + dtype.itemsize * width, offset=start)
    rows = np.lib.stride_tricks.as_strided(raw, shape=(count, dtype.itemsize * width), strides=(stride, 1))
    return np.ascontiguousarray(rows).view(dtype).reshape(count, width).astype(np.float64)


def local_matrix(node: dict) -> np.ndarray:
    """A node's local transform as a 4 x 4 matrix (column vectors)."""
    if "matrix" in node:
        return np.array(node["matrix"], dtype=np.float64).reshape(4, 4).T
    t = np.array(node.get("translation", [0, 0, 0]), dtype=np.float64)
    x, y, z, w = node.get("rotation", [0, 0, 0, 1])
    s = np.array(node.get("scale", [1, 1, 1]), dtype=np.float64)
    r = np.array([
        [1 - 2 * (y * y + z * z), 2 * (x * y - z * w), 2 * (x * z + y * w)],
        [2 * (x * y + z * w), 1 - 2 * (x * x + z * z), 2 * (y * z - x * w)],
        [2 * (x * z - y * w), 2 * (y * z + x * w), 1 - 2 * (x * x + y * y)],
    ])
    m = np.eye(4)
    m[:3, :3] = r * s
    m[:3, 3] = t
    return m


def world_nodes(gltf: dict) -> list[tuple[int, np.ndarray]]:
    """Every node of the default scene with its world matrix, in depth-first order."""
    scene = gltf["scenes"][gltf.get("scene", 0)]
    out: list[tuple[int, np.ndarray]] = []

    def walk(i: int, parent: np.ndarray) -> None:
        m = parent @ local_matrix(gltf["nodes"][i])
        out.append((i, m))
        for c in gltf["nodes"][i].get("children", []):
            walk(c, m)

    for root in scene["nodes"]:
        walk(root, np.eye(4))
    return out


def load_ship(path: pathlib.Path) -> tuple[list[tuple[str, np.ndarray, np.ndarray]], dict[str, np.ndarray]]:
    """The ship's triangle meshes in world space, as (name, positions, triangles), and its empties' positions."""
    gltf, binary = read_glb(path)
    meshes, empties = [], {}
    for i, m in world_nodes(gltf):
        node = gltf["nodes"][i]
        name = node.get("name", f"node_{i}")
        if "mesh" not in node:
            empties[name] = m[:3, 3].copy()
            continue
        for p, prim in enumerate(gltf["meshes"][node["mesh"]]["primitives"]):
            if prim.get("mode", 4) != 4:
                continue
            mat = gltf["materials"][prim["material"]]["name"] if "material" in prim else ""
            if mat.startswith(SKIP_MATERIALS):
                continue
            pos = accessor(gltf, binary, prim["attributes"]["POSITION"])
            with np.errstate(all="ignore"):  # macOS Accelerate raises spurious matmul warnings; checked below
                pos = pos @ m[:3, :3].T + m[:3, 3]
            if not np.isfinite(pos).all():
                raise SystemExit(f"ship_schematics: {path}: non-finite positions in {name}")
            if "indices" in prim:
                idx = accessor(gltf, binary, prim["indices"]).astype(np.int64).reshape(-1, 3)
            else:
                idx = np.arange(len(pos), dtype=np.int64).reshape(-1, 3)
            if np.linalg.det(m[:3, :3]) < 0:
                idx = idx[:, ::-1]
            meshes.append((f"{name}/{p}", pos, idx))
    return meshes, empties


# ---------------------------------------------------------------------------------------------------------------
# axes: worked out from the data, then checked against the manifest's convention


def axes_from_data(meshes, empties) -> dict:
    """Find the nose and up axes: engines sit aft of the bow weapons and deflector, the bridge sits on top.

    Returns the axis convention used for the picture. Stops if the data disagrees with assets/ships/manifest.json.
    """
    allp = np.concatenate([p for _, p, _ in meshes])
    ext = allp.max(0) - allp.min(0)
    long_axis = int(np.argmax(ext))
    engines = [v for k, v in empties.items() if k.startswith("engine_")]
    fore = [v for k, v in empties.items() if k in ("deflector", "bridge") or k.startswith("mount_")]
    if long_axis != 2 or not engines or not fore:
        raise SystemExit("ship_schematics: cannot find the nose from the data (expected the long axis on Z)")
    # The bow is the end away from the engines: engines' mean Z is greater than the fore fittings' -> bow is -Z.
    bow_sign = -1 if np.mean([e[2] for e in engines]) > np.mean([f[2] for f in fore]) else 1
    # Up: the beacons say it directly (dorsal above ventral); else the bridge is above the centre.
    if "light_beacon_dorsal" in empties and "light_beacon_ventral" in empties:
        up_sign = 1 if empties["light_beacon_dorsal"][1] > empties["light_beacon_ventral"][1] else -1
    else:
        up_sign = 1 if empties.get("bridge", np.zeros(3))[1] >= 0 else -1
    conv = json.loads((SHIPS / "manifest.json").read_text())["conventions"]
    if (bow_sign, up_sign) != (-1, 1) or conv["bow"] != "-Z" or conv["up"] != "+Y":
        raise SystemExit(f"ship_schematics: data says bow {bow_sign:+d}Z up {up_sign:+d}Y, manifest says "
                         f"bow {conv['bow']} up {conv['up']}")
    return {"bow": "-Z", "up": "+Y", "port": conv["port"]}


# ---------------------------------------------------------------------------------------------------------------
# quads


def weld(pos: np.ndarray, tris: np.ndarray, tol: float) -> tuple[np.ndarray, np.ndarray]:
    """Merge vertices closer than tol (the glb splits them at UV and normal seams); drop degenerate triangles."""
    keys = np.round(pos / tol).astype(np.int64)
    uniq, first, inv = np.unique(keys, axis=0, return_index=True, return_inverse=True)
    inv = inv.reshape(-1)
    order = np.argsort(first, kind="stable")          # keep vertices in first-seen order (deterministic)
    remap = np.empty(len(order), dtype=np.int64)
    remap[order] = np.arange(len(order))
    new_pos = pos[first[order]]
    t = remap[inv[tris]]
    ok = (t[:, 0] != t[:, 1]) & (t[:, 1] != t[:, 2]) & (t[:, 0] != t[:, 2])
    t = t[ok]
    a, b, c = new_pos[t[:, 0]], new_pos[t[:, 1]], new_pos[t[:, 2]]
    area = np.linalg.norm(np.cross(b - a, c - a), axis=1)
    return new_pos, t[area > tol * tol]


def make_quads(pos: np.ndarray, tris: np.ndarray) -> list[tuple[int, ...]]:
    """Pair triangles across shared edges into convex, near-coplanar quads, best pairs first; the rest stay."""
    a, b, c = pos[tris[:, 0]], pos[tris[:, 1]], pos[tris[:, 2]]
    n = np.cross(b - a, c - a)
    n /= np.linalg.norm(n, axis=1, keepdims=True)
    edges: dict[tuple[int, int], list[int]] = {}
    for t, tri in enumerate(tris.tolist()):
        for k in range(3):
            u, v = tri[k], tri[(k + 1) % 3]
            edges.setdefault((min(u, v), max(u, v)), []).append(t)
    cos_lim = math.cos(math.radians(COPLANAR_DEG))
    cands = []
    for (u, v), ts in edges.items():
        if len(ts) != 2:
            continue
        t0, t1 = ts
        d = float(n[t0] @ n[t1])
        if d < cos_lim:
            continue
        w0 = next(x for x in tris[t0] if x != u and x != v)
        w1 = next(x for x in tris[t1] if x != u and x != v)
        if w0 == w1:
            continue
        # quad u, w1, v, w0 (around the shared diagonal u-v): convex if the diagonals cross
        P = pos[[u, w1, v, w0]]
        nn = n[t0] + n[t1]
        turns, corners = [], []
        for k in range(4):
            e1 = P[(k + 1) % 4] - P[k]
            e0 = P[k] - P[k - 1]
            turns.append(float(np.cross(e0, e1) @ nn))
            corners.append(abs(float(e0 @ e1)) / (np.linalg.norm(e0) * np.linalg.norm(e1) + 1e-12))
        # convex when every corner turns the same way (the ring may run either way round the normal)
        scale = float(np.linalg.norm(P[2] - P[0]) ** 2) * 1e-9
        if not (all(t > scale for t in turns) or all(t < -scale for t in turns)):
            continue
        # score: flatter and more rectangular is better; the shared edge should be the long diagonal
        diag = np.linalg.norm(pos[u] - pos[v])
        other = np.linalg.norm(pos[w0] - pos[w1])
        score = (1 - d) * 4 + sum(corners) / 4 + 0.25 * abs(diag - other) / max(diag, other)
        cands.append((round(score, 9), u, v, t0, t1))
    cands.sort()
    used = np.zeros(len(tris), dtype=bool)
    faces: list[tuple[int, ...]] = []
    for _s, u, v, t0, t1 in cands:
        if used[t0] or used[t1]:
            continue
        used[t0] = used[t1] = True
        tri0 = tris[t0].tolist()
        k = tri0.index(u)
        # walk triangle t0 from u in its winding, inserting t1's apex on the shared edge
        w1 = next(x for x in tris[t1] if x != u and x != v)
        ring = [tri0[k], tri0[(k + 1) % 3], tri0[(k + 2) % 3]]
        if ring[1] == v:
            faces.append((ring[0], w1, ring[1], ring[2]))
        else:
            faces.append((ring[0], ring[1], ring[2], w1))
    for t in np.flatnonzero(~used).tolist():
        faces.append(tuple(tris[t].tolist()))
    return faces


# ---------------------------------------------------------------------------------------------------------------
# rendering


class View:
    """Top-down orthographic view: image x = +X (starboard right), image y = +Z (bow -Z at the top)."""

    def __init__(self, lo: np.ndarray, hi: np.ndarray):
        self.centre = (lo + hi) / 2
        extent = max(hi[0] - lo[0], hi[2] - lo[2])
        self.m_per_px = extent / (SIZE_PX * (1 - 2 * MARGIN))

    def px(self, p: np.ndarray, scale: int = 1) -> np.ndarray:
        """Pixel (x, y) of glb points at the given supersampling (pixel centres at +0.5)."""
        x = (p[..., 0] - self.centre[0]) / self.m_per_px + SIZE_PX / 2
        y = (p[..., 2] - self.centre[2]) / self.m_per_px + SIZE_PX / 2
        return np.stack([x, y], -1) * scale


def raster_heights(view: View, tris_world: np.ndarray, shade: np.ndarray) -> tuple[np.ndarray, np.ndarray]:
    """Z-buffer the triangles from above at SS x: returns the height buffer and the shade of the top surface."""
    n = SIZE_PX * SS
    zbuf = np.full((n, n), -np.inf)
    sbuf = np.zeros((n, n))
    P = view.px(tris_world, SS)                  # (T, 3, 2)
    H = tris_world[..., 1]
    for t in range(len(P)):
        (x0, y0), (x1, y1), (x2, y2) = P[t]
        den = (y1 - y2) * (x0 - x2) + (x2 - x1) * (y0 - y2)
        if abs(den) < 1e-9:
            continue
        xa = max(int(math.floor(min(x0, x1, x2))), 0)
        xb = min(int(math.ceil(max(x0, x1, x2))), n - 1)
        ya = max(int(math.floor(min(y0, y1, y2))), 0)
        yb = min(int(math.ceil(max(y0, y1, y2))), n - 1)
        if xa > xb or ya > yb:
            continue
        gx, gy = np.meshgrid(np.arange(xa, xb + 1) + 0.5, np.arange(ya, yb + 1) + 0.5)
        l0 = ((y1 - y2) * (gx - x2) + (x2 - x1) * (gy - y2)) / den
        l1 = ((y2 - y0) * (gx - x2) + (x0 - x2) * (gy - y2)) / den
        l2 = 1 - l0 - l1
        inside = (l0 >= -1e-9) & (l1 >= -1e-9) & (l2 >= -1e-9)
        if not inside.any():
            continue
        h = l0 * H[t, 0] + l1 * H[t, 1] + l2 * H[t, 2]
        zs = zbuf[ya:yb + 1, xa:xb + 1]
        win = inside & (h > zs)
        zs[win] = h[win]
        sbuf[ya:yb + 1, xa:xb + 1][win] = shade[t]
    return zbuf, sbuf


def disk(radius: float) -> list[tuple[int, int]]:
    """Integer offsets inside a disk of the given radius (supersampled pixels)."""
    r = int(math.ceil(radius))
    return [(dx, dy) for dy in range(-r, r + 1) for dx in range(-r, r + 1) if dx * dx + dy * dy <= radius * radius]


def dilate(mask: np.ndarray, radius: float) -> np.ndarray:
    out = np.zeros_like(mask)
    n = mask.shape[0]
    for dx, dy in disk(radius):
        xs, xd = (slice(0, n - dx), slice(dx, n)) if dx >= 0 else (slice(-dx, n), slice(0, n + dx))
        ys, yd = (slice(0, n - dy), slice(dy, n)) if dy >= 0 else (slice(-dy, n), slice(0, n + dy))
        out[yd, xd] |= mask[ys, xs]
    return out


def downsample(a: np.ndarray) -> np.ndarray:
    n = a.shape[0] // SS
    return a.reshape(n, SS, n, SS).mean(axis=(1, 3))


def white_layer(alpha: np.ndarray) -> Image.Image:
    a = np.clip(np.round(alpha * 255), 0, 255).astype(np.uint8)
    rgba = np.empty(a.shape + (4,), dtype=np.uint8)
    rgba[..., :3] = 255
    rgba[..., 3] = a
    return Image.fromarray(rgba)


def pick_edges(wp: np.ndarray, edge_faces: dict, span: float) -> list[tuple[np.ndarray, np.ndarray]]:
    """The edges of one mesh worth drawing, seen from above.

    A crease (the faces beside it bend) or an open border is always drawn. An edge between two faces in one plane is
    only a tessellation line: such lines are thinned so parallel ones in the same plane are at least COPLANAR_GAP_PX
    apart on screen, which keeps the quad grid without letting a finely cut plate fill in solid. Edges with no
    up-facing face beside them, and edges shorter than MIN_EDGE_PX on screen, are left out.
    """
    cos_feat = math.cos(math.radians(FEATURE_DEG))
    keep, flat = [], {}
    for (u, v) in sorted(edge_faces):
        fns = edge_faces[(u, v)]
        if max(abs(fn[1]) for fn in fns) < UP_FACING:
            continue
        d2 = (wp[v] - wp[u])[[0, 2]] * span
        length = float(np.hypot(*d2))
        if length < MIN_EDGE_PX:
            continue
        if len(fns) == 2 and float(fns[0] @ fns[1]) > cos_feat:
            fn = fns[0] if fns[0][1] >= 0 else -fns[0]
            plane = (tuple(np.round(fn * 40).astype(int).tolist()), int(round(float(fn @ wp[u]) * span / 4)))
            ang = math.atan2(d2[1], d2[0]) % math.pi
            bucket = int(round(math.degrees(ang) / 10)) % 18
            perp = np.array([-math.sin(ang), math.cos(ang)])
            mid = (wp[u] + wp[v])[[0, 2]] / 2 * span
            flat.setdefault((plane, bucket), []).append((round(float(mid @ perp), 3), u, v))
            continue
        keep.append((u, v))
    for key in sorted(flat):
        last = None
        for off, u, v in sorted(flat[key]):
            if last is None or off - last >= COPLANAR_GAP_PX:
                keep.append((u, v))
                last = off
    return [(wp[u], wp[v]) for u, v in sorted(keep)]


def render_ship(meshes, view: View) -> tuple[Image.Image, Image.Image, dict]:
    """Both layers of one ship, and its quad and triangle counts."""
    span = 1 / view.m_per_px
    world_tris, face_up, edges_world = [], [], []
    n_quads = n_tris = n_src = 0
    L = LIGHT / np.linalg.norm(LIGHT)
    for _name, pos, idx in meshes:
        n_src += len(idx)
        wp, wt = weld(pos, idx, 1e-4)
        if not len(wt):
            continue
        faces = make_quads(wp, wt)
        a, b, c = wp[wt[:, 0]], wp[wt[:, 1]], wp[wt[:, 2]]
        nrm = np.cross(b - a, c - a)
        nrm /= np.linalg.norm(nrm, axis=1, keepdims=True)
        nrm[nrm[:, 1] < 0] *= -1             # double-sided hulls: shade the face as seen from above
        world_tris.append(np.stack([a, b, c], 1))
        face_up.append(nrm)
        # face normals for the edge rules (Newell's method for quads)
        edge_faces: dict[tuple[int, int], list[np.ndarray]] = {}
        for f in faces:
            if len(f) == 4:
                n_quads += 1
            else:
                n_tris += 1
            P = wp[list(f)]
            fn = np.zeros(3)
            for k in range(len(f)):
                p0, p1 = P[k], P[(k + 1) % len(f)]
                fn += np.array([(p0[1] - p1[1]) * (p0[2] + p1[2]), (p0[2] - p1[2]) * (p0[0] + p1[0]),
                                (p0[0] - p1[0]) * (p0[1] + p1[1])])
            fn /= np.linalg.norm(fn) + 1e-12
            for k in range(len(f)):
                u, v = f[k], f[(k + 1) % len(f)]
                edge_faces.setdefault((min(u, v), max(u, v)), []).append(fn)
        edges_world.extend(pick_edges(wp, edge_faces, span))
    T = np.concatenate(world_tris)
    N = np.concatenate(face_up)
    shade = np.clip((N * L).sum(1), 0, 1) * 0.75 + np.clip(N[:, 1], 0, 1) * 0.25
    zbuf, sbuf = raster_heights(view, T, shade)
    covered = np.isfinite(zbuf)

    # hull layer: alpha between the two limits by the top surface's light
    lo, hi = HULL_ALPHA
    hull_alpha = downsample(np.where(covered, lo + (hi - lo) * sbuf, 0.0))

    # wire layer: visible edges (sampled against the height buffer) plus the outline of the coverage
    n = SIZE_PX * SS
    tol = 2.5 * view.m_per_px
    line = np.zeros((n, n), dtype=bool)
    if edges_world:
        E0 = np.array([e[0] for e in edges_world])
        E1 = np.array([e[1] for e in edges_world])
        p0, p1 = view.px(E0, SS), view.px(E1, SS)
        lens = np.linalg.norm(p1 - p0, axis=1)
        steps = np.maximum(np.ceil(lens / 0.5).astype(np.int64), 1)
        eid = np.repeat(np.arange(len(steps)), steps + 1)
        tpar = np.concatenate([np.arange(s + 1) / s for s in steps.tolist()])
        pts = p0[eid] + (p1[eid] - p0[eid]) * tpar[:, None]
        hts = E0[eid, 1] + (E1[eid, 1] - E0[eid, 1]) * tpar
        ix = np.clip(np.floor(pts[:, 0]).astype(np.int64), 0, n - 1)
        iy = np.clip(np.floor(pts[:, 1]).astype(np.int64), 0, n - 1)
        vis = hts >= zbuf[iy, ix] - tol
        line[iy[vis], ix[vis]] = True
    wire = dilate(line, WIRE_PX * SS / 2)
    edge_of_cover = covered & ~(np.roll(covered, 1, 0) & np.roll(covered, -1, 0) &
                                np.roll(covered, 1, 1) & np.roll(covered, -1, 1))
    wire |= dilate(edge_of_cover, OUTLINE_PX * SS / 2)
    wire_alpha = downsample(wire.astype(np.float64))
    if covered[:SS * 2].any() or covered[-SS * 2:].any() or covered[:, :SS * 2].any() or covered[:, -SS * 2:].any():
        raise SystemExit("ship_schematics: the hull touches the canvas edge")
    counts = {"source_triangles": int(n_src), "quads": n_quads, "triangles": n_tris, "edges_drawn": len(edges_world)}
    return white_layer(hull_alpha), white_layer(wire_alpha), counts


# ---------------------------------------------------------------------------------------------------------------
# contact sheet


TINTS = (
    ("concord", (0x12, 0x30, 0x5a), (0x6f, 0xb4, 0xff)),
    ("krai", (0x4a, 0x2a, 0x08), (0xff, 0xb0, 0x40)),
    ("green", (0x10, 0x3a, 0x22), (0x9c, 0xf5, 0xb8)),
)
GROUND = (8, 10, 14)


def tinted(layer: Image.Image, rgb: tuple[int, int, int]) -> Image.Image:
    solid = Image.new("RGBA", layer.size, rgb + (255,))
    solid.putalpha(layer.getchannel("A"))
    return solid


def combine(hull: Image.Image, wire: Image.Image, hull_rgb, wire_rgb, ground=GROUND) -> Image.Image:
    """How a skin recombines the two layers: the tinted hull over the ground, then the tinted wire over it."""
    out = Image.new("RGBA", hull.size, ground + (255,))
    out.alpha_composite(tinted(hull, hull_rgb))
    out.alpha_composite(tinted(wire, wire_rgb))
    return out.convert("RGB")


def contact_sheet(rows: list[tuple[str, Image.Image, Image.Image]]) -> Image.Image:
    cell, small = 300, 200
    cols = 2 + len(TINTS) + 1
    pad, label_h = 8, 18
    w = pad + cols * (cell + pad)
    h = pad + len(rows) * (cell + label_h + pad)
    sheet = Image.new("RGB", (w, h), (24, 26, 30))
    d = ImageDraw.Draw(sheet)
    for r, (name, hull, wire) in enumerate(rows):
        y = pad + r * (cell + label_h + pad)
        d.text((pad, y + 2), name, fill=(220, 220, 220))
        y += label_h
        cells = [combine(hull, Image.new("RGBA", hull.size), (255, 255, 255), (0, 0, 0), (60, 60, 70)),
                 combine(Image.new("RGBA", hull.size), wire, (0, 0, 0), (255, 255, 255), (60, 60, 70))]
        cells += [combine(hull, wire, hc, wc) for _, hc, wc in TINTS]
        for c, im in enumerate(cells):
            sheet.paste(im.resize((cell, cell), Image.Resampling.LANCZOS), (pad + c * (cell + pad), y))
        sm = combine(hull, wire, TINTS[0][1], TINTS[0][2]).resize((small, small), Image.Resampling.LANCZOS)
        x = pad + (cols - 1) * (cell + pad)
        sheet.paste(Image.new("RGB", (cell, cell), GROUND), (x, y))
        sheet.paste(sm, (x + (cell - small) // 2, y + (cell - small) // 2))
    return sheet


# ---------------------------------------------------------------------------------------------------------------


def main() -> int:
    ships_manifest = json.loads((SHIPS / "manifest.json").read_text())
    order = [(s["faction"], s["id"]) for s in ships_manifest["ships"]]
    entries, rows, convention = [], [], None
    for faction, sid in order:
        if faction not in FACTIONS:
            continue
        glb = SHIPS / faction / f"{sid}.glb"
        if not glb.exists():
            continue
        meshes, empties = load_ship(glb)
        conv = axes_from_data(meshes, empties)
        convention = convention or conv
        allp = np.concatenate([p for _, p, _ in meshes])
        view = View(allp.min(0), allp.max(0))
        hull, wire, counts = render_ship(meshes, view)
        out_dir = OUT / faction
        out_dir.mkdir(parents=True, exist_ok=True)
        hull.save(out_dir / f"{sid}.hull.png", optimize=True)
        wire.save(out_dir / f"{sid}.wire.png", optimize=True)
        origin = view.px(np.zeros(3))
        entries.append({
            "faction": faction,
            "id": sid,
            "hull": f"{faction}/{sid}.hull.png",
            "wire": f"{faction}/{sid}.wire.png",
            "size_px": [SIZE_PX, SIZE_PX],
            "m_per_px": round(float(view.m_per_px), 6),
            "origin_px": [round(float(origin[0]), 3), round(float(origin[1]), 3)],
            "extent_m": {"beam_x": round(float(np.ptp(allp[:, 0])), 3), "length_z": round(float(np.ptp(allp[:, 2])), 3)},
            **counts,
            "source": {"glb": f"assets/ships/{faction}/{sid}.glb",
                       "sha256": hashlib.sha256(glb.read_bytes()).hexdigest()},
        })
        rows.append((f"{faction}/{sid}  quads {counts['quads']}  tris {counts['triangles']}", hull, wire))
        print(f"{faction}/{sid}: {counts['source_triangles']} source triangles -> {counts['quads']} quads + "
              f"{counts['triangles']} triangles, {counts['edges_drawn']} edges")
    manifest = {
        "_doc": "Written by tools/mockups/ship_schematics.py from the ship glbs; do not edit, re-run it. Each ship has "
                "two layers, pure white RGB with the drawing in alpha: hull (filled, top-lit, low alpha) and wire "
                "(quad edges seen from above plus the outline, full alpha). Tint the hull with a dark skin colour, "
                "the wire with a bright one, and lay the wire over the hull. A glb point (x, y, z) in metres falls at "
                "pixel (origin_px[0] + x / m_per_px, origin_px[1] + z / m_per_px); y (height) is not drawn. A "
                "schematic is stale when its source glb's sha256 differs.",
        "conventions": {
            "view": "orthographic, looking straight down -Y (from above)",
            "image_x": "+X (starboard to the right)",
            "image_y": "+Z (the bow, -Z, at the top)",
            "glb_axes": convention,
            "pixel_origin": "top-left corner of the image; pixel centres at +0.5",
            "centre": "the centre of the hull's top-down bounding box is the centre of the canvas",
            "margin": MARGIN,
            "layers": "white RGB, alpha carries the drawing; hull alpha %.2f-%.2f, wire alpha up to 1" % HULL_ALPHA,
        },
        "ships": entries,
    }
    (OUT / "manifest.json").write_text(json.dumps(manifest, indent=1) + "\n")
    SHEET.parent.mkdir(parents=True, exist_ok=True)
    # saved already shrunk, the way tools/mockups/shrink_png.py does it, so that tool leaves it alone
    contact_sheet(rows).quantize(colors=256, method=Image.Quantize.MEDIANCUT,
                                 dither=Image.Dither.FLOYDSTEINBERG).save(SHEET, optimize=True)
    print(f"wrote {OUT / 'manifest.json'} and {SHEET}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
