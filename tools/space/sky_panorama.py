#!/usr/bin/env python3
"""Bake each nebula sky (data/space/skies/<id>.json) to a small equirectangular panorama, assets/skies/<id>.jpg, for the
game's environment: its sky then lights and reflects on the hulls (owner, 2026-11-04: "are you using reflection probes
for all those spacey backgrounds? that would be nice too, but dont crank it up super high"). Only the nebula is baked
(no stars: a reflection blurs them away); the background the player sees is still the sky file's own mesh and stars
(src/Game/View/NebulaSky.cs). Small on purpose: 1024 x 512, since reflections and ambient light are blurred anyway.

The mapping is the sky file's own longitude (round +y from +z toward +x, -180 degrees at the left edge) and latitude
(+90 at the top), and the game samples it with the same formula (src/Game/View/nebula_sky.gdshader), never Godot's own
panorama mapping, so the reflections line up with the sky by construction.

Usage: python3 tools/space/sky_panorama.py [--only id,id]
"""
import argparse
import glob
import json
import os

import numpy as np
from PIL import Image
from scipy.interpolate import griddata

from sky_sheet import octa_decode, panorama

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT = os.path.join(ROOT, "assets", "skies")
W, H = 1024, 512


def bake(path, lon_offset):
    sky = json.load(open(path))
    rgb, u, v = panorama(sky)
    d = octa_decode(u.astype(float), v.astype(float))
    lon = np.degrees(np.arctan2(d[:, 0], d[:, 2]))
    lat = np.degrees(np.arcsin(np.clip(d[:, 1], -1, 1)))
    pts = np.stack([np.concatenate([lon, lon - 360, lon + 360]), np.concatenate([lat] * 3)], 1)
    col = np.concatenate([rgb] * 3)
    gx, gy = np.meshgrid(np.linspace(-180, 180, W, endpoint=False) + lon_offset, np.linspace(90, -90, H))
    gx = ((gx + 180) % 360) - 180
    img = np.stack([griddata(pts, col[:, k], (gx, gy), method="linear") for k in range(3)], 2)
    return Image.fromarray(np.nan_to_num(img).clip(0, 255).astype(np.uint8))


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--only", default="")
    a = ap.parse_args()
    os.makedirs(OUT, exist_ok=True)
    for p in sorted(glob.glob(os.path.join(ROOT, "data", "space", "skies", "*.json"))):
        sid = os.path.basename(p)[:-5]
        if sid == "recipes" or (a.only and sid not in a.only.split(",")):
            continue
        out = os.path.join(OUT, sid + ".jpg")
        bake(p, 0.0).save(out, quality=88)
        print(f"panorama {os.path.relpath(out, ROOT)} {W}x{H} {os.path.getsize(out) // 1024} KB")


if __name__ == "__main__":
    main()
