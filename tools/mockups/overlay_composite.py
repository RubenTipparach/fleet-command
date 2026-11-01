#!/usr/bin/env python3
"""Composite a console render made on a transparent film over a stand-in game view, so an overlay is judged where it
will be seen: over the live battle (owner, 2026-10-31, of the organic study: "The one thing about UI is it's overlayed
on top of gameplay so that background wouldn't make sense here"; the Organic Panel Bible's rule 5).

The game view is our own and deterministic: a dark space scene drawn here (a seeded starfield in three sizes, a faint
violet and teal nebula, and a distant ship's wedge silhouette with its engine glow), the same look as the console
page's 3D view in docs/mockups/console-skins.html. It is only a backdrop for judging; it never ships in a kit piece.

    python3 tools/mockups/overlay_composite.py docs/design/organic-bible/26-overlay.png \\
        docs/design/organic-bible/26-overlay-over-game.jpg

The transparent PNG stays beside it as the piece itself.
"""
import math
import sys

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

SEED = 26


def game_view(w, h, seed=SEED):
    """A w x h RGB stand-in battle view: deep space, a soft nebula, stars, and a distant ship. Same seed, same image."""
    rng = np.random.default_rng(seed)
    yy, xx = np.mgrid[0:h, 0:w].astype(np.float32)
    img = np.zeros((h, w, 3), np.float32)
    img[:] = (4, 5, 12)
    # the nebula: two soft blobs of light, violet and teal, broken by a coarse noise
    noise = Image.fromarray((rng.random((h // 24 + 2, w // 24 + 2)) * 255).astype(np.uint8)).resize((w, h), Image.BICUBIC)
    n = np.asarray(noise.filter(ImageFilter.GaussianBlur(18)), np.float32) / 255.0
    for cx, cy, r, col in ((0.72 * w, 0.30 * h, 0.55 * w, (60, 28, 90)), (0.25 * w, 0.85 * h, 0.45 * w, (14, 48, 60))):
        d = np.hypot(xx - cx, yy - cy) / r
        a = np.clip(1.0 - d, 0.0, 1.0) ** 2 * (0.45 + 0.9 * n)
        img += a[..., None] * np.array(col, np.float32)
    # stars in three sizes, some tinted
    for count, size, bright in ((int(w * h / 900), 0.6, 150), (int(w * h / 6000), 1.1, 220), (int(w * h / 40000), 1.8, 255)):
        for _ in range(count):
            x, y = rng.uniform(0, w), rng.uniform(0, h)
            tint = rng.choice([(1.0, 1.0, 1.0), (0.8, 0.9, 1.0), (1.0, 0.9, 0.75)])
            b = bright * rng.uniform(0.5, 1.0)
            x0, x1 = int(max(x - 2, 0)), int(min(x + 3, w))
            y0, y1 = int(max(y - 2, 0)), int(min(y + 3, h))
            g = np.exp(-((xx[y0:y1, x0:x1] - x) ** 2 + (yy[y0:y1, x0:x1] - y) ** 2) / (2 * size * size))
            img[y0:y1, x0:x1] += g[..., None] * b * np.array(tint, np.float32)
    out = Image.fromarray(np.clip(img, 0, 255).astype(np.uint8))
    # a distant ship: a pale wedge with an engine glow, small, below the console's middle
    d = ImageDraw.Draw(out)
    sx, sy, s = 0.755 * w, 0.80 * h, 0.035 * w
    a = math.radians(-12)
    pts = [(sx + s * math.cos(a), sy + s * math.sin(a)), (sx - 0.8 * s * math.cos(a) - 0.3 * s * math.sin(a), sy - 0.8 * s * math.sin(a) + 0.3 * s * math.cos(a)),
           (sx - 0.8 * s * math.cos(a) + 0.3 * s * math.sin(a), sy - 0.8 * s * math.sin(a) - 0.3 * s * math.cos(a))]
    glow = Image.new("RGB", out.size, (0, 0, 0))
    gd = ImageDraw.Draw(glow)
    ex, ey = sx - 0.85 * s * math.cos(a), sy - 0.85 * s * math.sin(a)
    gd.ellipse((ex - 6, ey - 6, ex + 6, ey + 6), fill=(90, 150, 255))
    out = Image.fromarray(np.clip(np.asarray(out, np.int32) + np.asarray(glow.filter(ImageFilter.GaussianBlur(5)), np.int32) * 2, 0, 255).astype(np.uint8))
    ImageDraw.Draw(out).polygon(pts, fill=(150, 160, 178))
    return out


def composite(png, out_path, quality=86):
    """Write out_path: the transparent render png composited over the game view of its size."""
    piece = Image.open(png).convert("RGBA")
    view = game_view(*piece.size).convert("RGBA")
    view.alpha_composite(piece)
    view.convert("RGB").save(out_path, quality=quality)


if __name__ == "__main__":
    if len(sys.argv) != 3:
        raise SystemExit("usage: overlay_composite.py <render.png> <out.jpg>")
    composite(sys.argv[1], sys.argv[2])
    print(f"wrote {sys.argv[2]}")
