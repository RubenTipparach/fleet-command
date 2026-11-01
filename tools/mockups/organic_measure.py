#!/usr/bin/env python3
"""Measure an organic render's look, so the kit can be compared with the study's accepted widget in numbers (the
Winamp exercise: compare, measure, fix, compare again; the blender-ui-panels skill, section 8). A measurement
instrument (CLAUDE.md section 4): it only produces the numbers the Organic Panel Bible's "From widget to kit" lesson
needs.

For each RGBA render (2 px per mm, transparent film), over the body only (alpha above 0.9, and not the black glass,
L* below 4): CIE L*C*h statistics, and the share of the body that reads as bone or ivory (L* 50 or more, C* 30 or
less, hue 50-110), as red-maroon flesh (hue 0-45 or 330-360, C* 12 or more), as a specular highlight (L* 65 or more),
and as deep shadow (L* 12 or less); the local contrast (standard deviation of L* in 6 px tiles, a measure of how much
relief reads); and the share of the slot the body covers (silhouette fill).

    python3 tools/mockups/organic_measure.py a.png [b.png ...] [--json out.json]
"""
import json
import sys

import numpy as np
from PIL import Image


def lab(rgb):
    """sRGB (0-1) to CIE L*a*b* (D65)."""
    c = np.where(rgb <= 0.04045, rgb / 12.92, ((rgb + 0.055) / 1.055) ** 2.4)
    m = np.array([[0.4124, 0.3576, 0.1805], [0.2126, 0.7152, 0.0722], [0.0193, 0.1192, 0.9505]])
    xyz = c @ m.T / np.array([0.95047, 1.0, 1.08883])
    f = np.where(xyz > 0.008856, np.cbrt(xyz), 7.787 * xyz + 16 / 116)
    return np.stack([116 * f[..., 1] - 16, 500 * (f[..., 0] - f[..., 1]), 200 * (f[..., 1] - f[..., 2])], -1)


def measure(path):
    im = np.asarray(Image.open(path).convert("RGBA"), dtype=np.float64) / 255.0
    a = im[..., 3]
    L = lab(im[..., :3])
    Ls, C = L[..., 0], np.hypot(L[..., 1], L[..., 2])
    hue = (np.degrees(np.arctan2(L[..., 2], L[..., 1])) + 360) % 360
    body = (a > 0.9) & (Ls >= 4.0)
    n = max(int(body.sum()), 1)
    lb, cb, hb = Ls[body], C[body], hue[body]
    bone = ((lb >= 50) & (cb <= 30) & (hb >= 50) & (hb <= 110)).sum() / n
    red = (((hb <= 45) | (hb >= 330)) & (cb >= 12)).sum() / n
    hi = (lb >= 65).sum() / n
    deep = (lb <= 12).sum() / n
    t = 6
    H, W = Ls.shape
    sd = []
    for y in range(0, H - t, t):
        for x in range(0, W - t, t):
            if body[y:y + t, x:x + t].all():
                sd.append(Ls[y:y + t, x:x + t].std())
    return {"file": path, "L_mean": round(float(lb.mean()), 1), "L_sd": round(float(lb.std()), 1),
            "C_mean": round(float(cb.mean()), 1), "hue_mean": round(float(np.degrees(np.arctan2(
                np.sin(np.radians(hb)).mean(), np.cos(np.radians(hb)).mean())) % 360), 1),
            "bone_share": round(float(bone), 3), "red_share": round(float(red), 3), "highlight_share": round(float(hi), 3),
            "deep_share": round(float(deep), 3), "relief_sd": round(float(np.mean(sd)) if sd else 0.0, 2),
            "fill": round(float((a > 0.5).mean()), 3)}


def blur(a, s):
    """Gaussian blur (sigma s px) by separable convolution."""
    r = int(3 * s) + 1
    k = np.exp(-0.5 * (np.arange(-r, r + 1) / s) ** 2)
    k /= k.sum()
    a = np.apply_along_axis(lambda v: np.convolve(np.pad(v, r, mode="edge"), k, "valid"), 0, a)
    return np.apply_along_axis(lambda v: np.convolve(np.pad(v, r, mode="edge"), k, "valid"), 1, a)


def detail(path, mask=None):
    """How detailed and how structured a body is (the owner, 2026-10-31: "The body looks soft, and has very little
    details", "And detail that look like pores or goosebumps?"), over the body pixels (alpha above 0.9 and not the black
    glass; or every pixel of an opaque crop, e.g. a reference frame): hf, the mean magnitude of the lightness Laplacian
    (high-frequency energy, L* per px^2); mid, the mean band-pass lightness (blur 1.5 px minus blur 6 px: the form
    detail, bundles, ribs, folds, that reads at a glance); edges, the share of pixels whose lightness gradient exceeds 4 L* per px; smooth,
    the share whose local (5 px) lightness spread is under 2 L*; coherence, the structure tensor's mean coherence
    ((l1 - l2) / (l1 + l2), over 4 px windows, weighted by gradient energy): 1 for detail that runs one way (folds,
    fibres, ribs), 0 for isotropic noise (pores, goosebumps)."""
    im = np.asarray(Image.open(path).convert("RGBA"), dtype=np.float64) / 255.0
    L = lab(im[..., :3])[..., 0]
    body = (im[..., 3] > 0.9) & (L >= 4.0) if mask is None else mask
    body[:2, :] = body[-2:, :] = False
    body[:, :2] = body[:, -2:] = False
    gy, gx = np.gradient(L)
    lap = np.abs(np.gradient(gx, axis=1) + np.gradient(gy, axis=0))
    gm = np.hypot(gx, gy)
    mu = blur(L, 2.0)
    sd = np.sqrt(np.maximum(blur(L * L, 2.0) - mu * mu, 0.0))
    jxx, jyy, jxy = blur(gx * gx, 4.0), blur(gy * gy, 4.0), blur(gx * gy, 4.0)
    tr = jxx + jyy
    root = np.sqrt((jxx - jyy) ** 2 + 4 * jxy * jxy)
    coh = root / np.maximum(tr, 1e-9)
    w = tr[body]
    mid = np.abs(blur(L, 1.5) - blur(L, 6.0))      # form detail: bundles, ribs, knuckles, folds (2-12 px across)
    return {"file": path, "hf": round(float(lap[body].mean()), 2), "mid": round(float(mid[body].mean()), 2), "edges": round(float((gm[body] > 4.0).mean()), 3),
            "smooth": round(float((sd[body] < 2.0).mean()), 3),
            "coherence": round(float((coh[body] * w).sum() / max(w.sum(), 1e-9)), 3)}


if __name__ == "__main__":
    args = [a for a in sys.argv[1:] if not a.startswith("--")]
    out = [measure(p) for p in args if p != (sys.argv[sys.argv.index("--json") + 1] if "--json" in sys.argv else None)]
    for r in out:
        print(json.dumps(r))
    if "--json" in sys.argv:
        json.dump(out, open(sys.argv[sys.argv.index("--json") + 1], "w"), indent=1)
