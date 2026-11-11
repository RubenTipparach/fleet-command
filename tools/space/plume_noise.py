#!/usr/bin/env python3
"""Write the 3D noise the nebula plumes are marched through (openspec/changes/nebula-plumes).

The owner, 2026-11-10: "something like a 3d noise texture would be cool? something like how Ace Combat 7 does
clouds". The plume shader (src/Game/View/nebula_plume.gdshader) is a volume march through one small tiling 3D
texture, the way real-time cloud renderers do it (Schneider and Vos, "The Real-time Volumetric Cloudscapes of
Horizon Zero Dawn", SIGGRAPH 2015: a low-frequency Perlin-Worley shape eroded by Worley noise at rising
frequencies). This writes that texture, assets/space/plume_noise.bin: SIZE x SIZE x SIZE voxels of four bytes,
x fastest, then y, then z:

  R  Perlin-Worley: Perlin fbm (the billows' broad shape) remapped by an inverted Worley fbm (round cells), so
     the shape has the cauliflower lumps of a cloud and not the smooth hills of plain Perlin
  G  inverted Worley fbm at 3, 6 and 12 cells
  B  the same at 6, 12 and 24
  A  the same at 12 and 24

Every octave tiles at the texture's edge (lattices and cells wrap), so the shader can repeat it at any scale.
Points and gradients come from numpy's seeded PCG64 (CLAUDE.md 10: a real generator, never a hash), so the file
is the same on every run. Run from the repository root:

    python3 tools/space/plume_noise.py            # writes assets/space/plume_noise.bin
    python3 tools/space/plume_noise.py --check    # fails if the committed file differs
    python3 tools/space/plume_noise.py --sheet out.png   # slices of each channel, to look at (needs Pillow)
"""
import argparse
import pathlib
import sys

import numpy as np

SIZE = 64
SEED = 20261110
OUT = pathlib.Path(__file__).resolve().parents[2] / "assets" / "space" / "plume_noise.bin"


def grid():
    """Voxel centres in 0..1 on each axis, as three broadcastable arrays (z, y, x order of the file)."""
    c = (np.arange(SIZE, dtype=np.float64) + 0.5) / SIZE
    return c[None, None, :], c[None, :, None], c[:, None, None]


def perlin(rng, freq):
    """Tiling gradient noise with `freq` lattice cells an axis, about -1..1."""
    g = rng.normal(size=(freq, freq, freq, 3))
    g /= np.linalg.norm(g, axis=-1, keepdims=True)
    x, y, z = (a * freq for a in grid())
    xi, yi, zi = np.floor(x).astype(int), np.floor(y).astype(int), np.floor(z).astype(int)
    xf, yf, zf = x - xi, y - yi, z - zi
    fade = lambda t: t * t * t * (t * (t * 6 - 15) + 10)
    u, v, w = fade(xf), fade(yf), fade(zf)
    total = 0.0
    for dz in (0, 1):
        for dy in (0, 1):
            for dx in (0, 1):
                gr = g[(zi + dz) % freq, (yi + dy) % freq, (xi + dx) % freq]
                d = gr[..., 0] * (xf - dx) + gr[..., 1] * (yf - dy) + gr[..., 2] * (zf - dz)
                total = total + d * (u if dx else 1 - u) * (v if dy else 1 - v) * (w if dz else 1 - w)
    return total * 1.5


def worley(rng, cells):
    """Tiling cellular noise with `cells` cells an axis: 1 at a feature point, falling to 0 a cell away."""
    pts = rng.random(size=(cells, cells, cells, 3))
    x, y, z = (np.broadcast_to(a * cells, (SIZE, SIZE, SIZE)) for a in grid())
    xi, yi, zi = np.floor(x).astype(int), np.floor(y).astype(int), np.floor(z).astype(int)
    best = np.full((SIZE, SIZE, SIZE), 9.0)
    for dz in (-1, 0, 1):
        for dy in (-1, 0, 1):
            for dx in (-1, 0, 1):
                p = pts[(zi + dz) % cells, (yi + dy) % cells, (xi + dx) % cells]
                d2 = (xi + dx + p[..., 0] - x) ** 2 + (yi + dy + p[..., 1] - y) ** 2 + (zi + dz + p[..., 2] - z) ** 2
                best = np.minimum(best, d2)
    return 1.0 - np.clip(np.sqrt(best), 0.0, 1.0)


def remap(v, lo, hi, a, b):
    return a + (v - lo) / (hi - lo) * (b - a)


def build():
    rng = np.random.default_rng(SEED)
    pf = sum(perlin(rng, f) * a for f, a in ((3, 1.0), (6, 0.5), (12, 0.25), (24, 0.125))) / 1.875
    pf = np.clip(pf * 0.5 + 0.5, 0.0, 1.0)
    w = {c: worley(rng, c) for c in (3, 6, 12, 24)}
    g = w[3] * 0.625 + w[6] * 0.25 + w[12] * 0.125
    b = w[6] * 0.625 + w[12] * 0.25 + w[24] * 0.125
    a = w[12] * 0.7 + w[24] * 0.3
    r = np.clip(remap(pf, 0.0, 1.0, g * 0.6, 1.0) * (0.55 + 0.45 * g) * 1.25, 0.0, 1.0)
    vol = np.stack([r, g, b, a], axis=-1)
    return np.round(np.clip(vol, 0.0, 1.0) * 255.0).astype(np.uint8)


def sheet(vol, path):
    from PIL import Image
    tiles = []
    for ch in range(4):
        tiles.append(np.concatenate([vol[z, :, :, ch] for z in (0, 16, 32, 48)], axis=1))
    img = np.concatenate(tiles, axis=0)
    Image.fromarray(img, "L").resize((img.shape[1] * 4, img.shape[0] * 4), Image.NEAREST).save(path)


def main():
    ap = argparse.ArgumentParser(description=__doc__.split("\n")[0])
    ap.add_argument("--check", action="store_true", help="fail if the committed file differs from what this writes")
    ap.add_argument("--sheet", help="also write slices of each channel as an image, to look at")
    args = ap.parse_args()
    vol = build()
    data = vol.tobytes()
    assert len(data) == SIZE ** 3 * 4
    if args.sheet:
        sheet(vol, args.sheet)
    if args.check:
        if not OUT.exists() or OUT.read_bytes() != data:
            sys.exit(f"{OUT} is not what tools/space/plume_noise.py writes: run it and commit the file")
        print(f"{OUT.name}: up to date")
        return
    OUT.parent.mkdir(parents=True, exist_ok=True)
    OUT.write_bytes(data)
    means = ", ".join(f"{n} {vol[..., i].mean() / 255:.3f}" for i, n in enumerate("RGBA"))
    print(f"wrote {OUT} ({len(data)} bytes; channel means {means})")


if __name__ == "__main__":
    main()
