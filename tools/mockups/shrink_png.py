#!/usr/bin/env python3
"""Re-save mockup screenshots as dithered 256-colour PNGs, about a quarter of the size, before they are committed.
Leaves a shot already shrunk alone. After Crab Breeze and Star Crew's tools/mockups/shrink_png.py (same job).

    python3 tools/mockups/shrink_png.py docs/screenshots/mockups
"""
import pathlib
import sys

from PIL import Image

for root in sys.argv[1:] or ["docs/screenshots/mockups"]:
    for p in sorted(pathlib.Path(root).rglob("*.png")):
        im = Image.open(p)
        if im.mode == "P":
            continue
        before = p.stat().st_size
        im.convert("RGB").quantize(colors=256, method=Image.Quantize.MEDIANCUT, dither=Image.Dither.FLOYDSTEINBERG).save(p, optimize=True)
        print(f"{p}: {before // 1024} -> {p.stat().st_size // 1024} KiB")
