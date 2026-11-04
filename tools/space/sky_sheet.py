#!/usr/bin/env python3
"""A contact sheet of every nebula sky (data/space/skies/<id>.json), unwrapped to a panorama: longitude across, latitude
-60 to +60 degrees up (the band the battle camera sees), each sky's vertex colours interpolated over its mesh and its
brightest stars dotted on. For looking at the skies before a page or the game uses them (CLAUDE.md section 12).

Usage: python3 tools/space/sky_sheet.py [out.png]   (default docs/screenshots/skies/contact.png)
"""
import base64
import glob
import json
import os
import sys

import numpy as np
from PIL import Image, ImageDraw, ImageFont
from scipy.interpolate import griddata

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
W, H = 720, 240


def octa_decode(u, v):
    """Two 16-bit octahedral coordinates to a unit direction (the sky file's encoding)."""
    x, y = u / 65535.0 * 2 - 1, v / 65535.0 * 2 - 1
    z = 1 - np.abs(x) - np.abs(y)
    t = np.clip(-z, 0, None)
    x = x + np.where(x >= 0, -t, t)
    y = y + np.where(y >= 0, -t, t)
    d = np.stack([x, y, z], 1)
    return d / np.linalg.norm(d, axis=1, keepdims=True)


def panorama(sky):
    raw = base64.b64decode("".join(sky["mesh"]["vertices_b64"]) if isinstance(sky["mesh"]["vertices_b64"], list) else sky["mesh"]["vertices_b64"])
    a = np.frombuffer(raw, dtype=np.uint8).reshape(-1, 8)
    u = a[:, 0].astype(np.uint32) | (a[:, 1].astype(np.uint32) << 8)
    v = a[:, 2].astype(np.uint32) | (a[:, 3].astype(np.uint32) << 8)
    return a[:, 4:7].astype(float), u, v


def render(path):
    sky = json.load(open(path))
    rgb, u, v = panorama(sky)
    best = None
    for perm in ((0, 1, 2), (0, 2, 1)):   # the file's frame: +y north; try the octahedral axis order that gives it
        d = octa_decode(u.astype(float), v.astype(float))[:, perm]
        best = d if best is None else best
        break
    d = best
    lon = np.degrees(np.arctan2(d[:, 0], d[:, 2]))
    lat = np.degrees(np.arcsin(np.clip(d[:, 1], -1, 1)))
    pts = np.stack([np.concatenate([lon, lon - 360, lon + 360]), np.concatenate([lat] * 3)], 1)
    col = np.concatenate([rgb] * 3)
    gx, gy = np.meshgrid(np.linspace(-180, 180, W), np.linspace(60, -60, H))
    img = np.stack([griddata(pts, col[:, k], (gx, gy), method="linear") for k in range(3)], 2)
    img = np.nan_to_num(img).clip(0, 255).astype(np.uint8)
    im = Image.fromarray(img)
    dr = ImageDraw.Draw(im)
    for s in sky["stars"]["list"][:400]:
        x, y, z, b = s[0], s[1], s[2], s[3]
        lo, la = np.degrees(np.arctan2(x, z)), np.degrees(np.arcsin(max(-1, min(1, y))))
        if -60 <= la <= 60:
            px, py = (lo + 180) / 360 * (W - 1), (60 - la) / 120 * (H - 1)
            g = int(120 + 135 * b)
            dr.point((px, py), fill=(g, g, g))
    return sky["id"], sky["name"], im


def main():
    out = sys.argv[1] if len(sys.argv) > 1 else os.path.join(ROOT, "docs", "screenshots", "skies", "contact.png")
    files = sorted(p for p in glob.glob(os.path.join(ROOT, "data", "space", "skies", "*.json")) if not p.endswith("recipes.json"))
    tiles = [render(p) for p in files]
    cols = 3
    rows = (len(tiles) + cols - 1) // cols
    sheet = Image.new("RGB", (cols * W, rows * (H + 28)), (8, 10, 16))
    dr = ImageDraw.Draw(sheet)
    font = ImageFont.truetype(os.path.join(ROOT, "assets", "fonts", "Antonio-VariableFont_wght.ttf"), 20)
    for i, (sid, name, im) in enumerate(tiles):
        x, y = (i % cols) * W, (i // cols) * (H + 28)
        dr.text((x + 8, y + 3), f"{name}  ({sid})", fill=(200, 210, 230), font=font)
        sheet.paste(im, (x, y + 28))
    os.makedirs(os.path.dirname(out), exist_ok=True)
    sheet.save(out)
    print(f"sheet {out}: {len(tiles)} skies")


if __name__ == "__main__":
    main()
