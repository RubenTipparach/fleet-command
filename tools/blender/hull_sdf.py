"""Hull SDF: a ship's signed distance field, the inside test its breakup cuts by (lib/ship-break.js).

The shared breakup (docs/mockups/lib/ship-break.js) cuts a hull into seeded cells and closes every break with a cut face
clipped to the hull's inside. The Concord and Krai hulls are lofts, so "inside" is a distance into their stations; the
Ssarith Hunt's hulls are one grown skin (Ship Design Bible chapter 14), with no stations, so their inside comes from this
field instead (owner, 2026-11-05: "ship piece breaking should be built next"). One field per ship, sampled the same way by
the page's script (insideBody) and its cut-face shader (bodyD), so the cut and the face that closes it agree.

The field: a grid of cell_m cubes over the hull's bounds and a margin, each cell centre's distance to the nearest triangle,
positive inside (a generalised winding number over 0.5, so overlapping closed parts count as one solid), stored as one
byte a cell: 128 + 10 x metres, clamped (so 0.1 m steps, +-12.7 m). Plain numpy, no Blender: painted_bake.py calls it after
a ship's bake, and run alone it fills a manifest's ships from their stored meshes:

    python3 tools/blender/hull_sdf.py assets/ships/ssarith/manifest.json [ship ...]
"""
import base64
import json
import sys

import numpy as np

CELL_M = 1.0        # grid cell, metres: the thinnest Hunt branch is about 3 m through
MARGIN_M = 2.0      # grid beyond the hull's bounds
SCALE = 10.0        # bytes per metre (0.1 m steps)


def _tri_dist(P, A, B, C):
    """Distance from each point P (n, 3) to each triangle (A, B, C each (t, 3)): (n, t). Ericson's closest point, by region."""
    P = P[:, None, :]
    ab, ac, ap = B - A, C - A, P - A
    d1, d2 = (ab * ap).sum(-1), (ac * ap).sum(-1)
    bp = P - B
    d3, d4 = (ab * bp).sum(-1), (ac * bp).sum(-1)
    cp = P - C
    d5, d6 = (ab * cp).sum(-1), (ac * cp).sum(-1)
    va, vb, vc = d3 * d6 - d5 * d4, d5 * d2 - d1 * d6, d1 * d4 - d3 * d2
    den = np.where(np.abs(va + vb + vc) < 1e-12, 1e-12, va + vb + vc)
    v, w = vb / den, vc / den
    Q = A + ab * v[..., None] + ac * w[..., None]                                   # inside the face
    def pick(cond, R):
        nonlocal Q
        Q = np.where(cond[..., None], R, Q)
    t_ab = d1 / np.where(np.abs(d1 - d3) < 1e-12, 1e-12, d1 - d3)
    pick((vc <= 0) & (d1 >= 0) & (d3 <= 0), A + ab * t_ab[..., None])             # edge ab
    t_ac = d2 / np.where(np.abs(d2 - d6) < 1e-12, 1e-12, d2 - d6)
    pick((vb <= 0) & (d2 >= 0) & (d6 <= 0), A + ac * t_ac[..., None])             # edge ac
    e = (d4 - d3) + (d5 - d6)
    t_bc = (d4 - d3) / np.where(np.abs(e) < 1e-12, 1e-12, e)
    pick((va <= 0) & ((d4 - d3) >= 0) & ((d5 - d6) >= 0), B + (C - B) * t_bc[..., None])  # edge bc
    pick((d1 <= 0) & (d2 <= 0), np.broadcast_to(A, Q.shape))                      # vertex a
    pick((d3 >= 0) & (d4 <= d3), np.broadcast_to(B, Q.shape))                     # vertex b
    pick((d6 >= 0) & (d5 <= d6), np.broadcast_to(C, Q.shape))                     # vertex c
    return np.linalg.norm(P - Q, axis=-1)


def _winding(P, A, B, C):
    """The generalised winding number of the triangles about each point (n,): about 1 inside a closed skin, 0 outside."""
    a, b, c = A[None] - P[:, None], B[None] - P[:, None], C[None] - P[:, None]
    la, lb, lc = (np.linalg.norm(x, axis=-1) for x in (a, b, c))
    det = (a * np.cross(b, c)).sum(-1)
    den = la * lb * lc + (a * b).sum(-1) * lc + (b * c).sum(-1) * la + (c * a).sum(-1) * lb
    return (2.0 * np.arctan2(det, den)).sum(-1) / (4.0 * np.pi)


def hull_sdf(pos, cell_m=CELL_M, margin_m=MARGIN_M):
    """The field for a hull given as a flat triangle list in ship space (x port, y up, z bow, metres): a dict of lo, hi
    (the grid's corners, metres), dims (nx, ny, nz), cell_m, scale (bytes per metre) and data (base64 bytes, x fastest)."""
    T = np.asarray(pos, dtype=np.float64).reshape(-1, 3, 3)
    A, B, C = T[:, 0], T[:, 1], T[:, 2]
    lo = T.reshape(-1, 3).min(0) - margin_m
    dims = np.ceil((T.reshape(-1, 3).max(0) + margin_m - lo) / cell_m).astype(int)
    hi = lo + dims * cell_m
    xs, ys, zs = (lo[k] + (np.arange(dims[k]) + 0.5) * cell_m for k in range(3))
    Z, Y, X = np.meshgrid(zs, ys, xs, indexing="ij")             # (nz, ny, nx): x runs fastest when flattened
    pts = np.stack([X.ravel(), Y.ravel(), Z.ravel()], 1)
    out = np.empty(len(pts))
    step = max(64, 400000 // max(1, len(T)))
    for s in range(0, len(pts), step):
        p = pts[s:s + step]
        d = _tri_dist(p, A[None], B[None], C[None]).min(1)
        out[s:s + step] = np.where(np.abs(_winding(p, A, B, C)) > 0.5, d, -d)
    q = np.clip(np.round(128 + out * SCALE), 0, 255).astype(np.uint8)
    return {"lo": [round(float(v), 3) for v in lo], "hi": [round(float(v), 3) for v in hi], "dims": [int(v) for v in dims],
            "cell_m": cell_m, "scale": SCALE, "inside_share": round(float((out > 0).mean()), 4),
            "data": base64.b64encode(q.tobytes()).decode("ascii")}


def main():
    path, only = sys.argv[1], sys.argv[2:]
    man = json.load(open(path))
    for slug, sh in man["ships"].items():
        if only and slug not in only:
            continue
        sh["sdf"] = hull_sdf(sh["mesh"]["pos"])
        print(f"sdf {slug}: {sh['sdf']['dims']} cells, {sh['sdf']['inside_share'] * 100:.1f} % inside", flush=True)
    with open(path, "w") as fh:
        json.dump(man, fh, separators=(",", ":"), sort_keys=True)
        fh.write("\n")


if __name__ == "__main__":
    main()
