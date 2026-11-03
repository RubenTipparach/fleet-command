#!/usr/bin/env python3
"""Make an emblem render's console and list sizes, and a sheet to judge them (the Emblem Render Guide, "Check it
small"). From assets/emblems/<faction>/emblem.png (tools/blender/emblem_<faction>.py) writes, beside it:
crest.png (the kit's 96 x 96 crest slot, at 2x: 192 px, as every kit piece is rendered), mark-40.png and mark-20.png
(at 2x: 80 and 40 px), and check.png: the render, then 96, 40 and 20 px at 1x on a dark console and a light page.
Lanczos throughout, with the drop shadow kept: it is part of the badge.

    python3 tools/emblems/sizes.py concord
"""
import os
import sys

from PIL import Image, ImageDraw

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))


def main():
    faction = sys.argv[1]
    d = os.path.join(ROOT, "assets", "emblems", faction)
    src = Image.open(os.path.join(d, "emblem.png")).convert("RGBA")
    for name, px in (("crest", 192), ("mark-40", 80), ("mark-20", 40)):
        src.resize((px, px), Image.LANCZOS).save(os.path.join(d, f"{name}.png"), optimize=True)
    grounds = ((10, 12, 18), (228, 232, 240))
    sizes = (96, 40, 20)
    w = 360 + 24 + sum(s + 24 for s in sizes) + 24
    sheet = Image.new("RGB", (w, 2 * (360 + 24) + 24), (40, 42, 48))
    for row, g in enumerate(grounds):
        y = 24 + row * (360 + 24)
        panel = Image.new("RGBA", (w - 48, 360), g + (255,))
        big = src.resize((360, 360), Image.LANCZOS)
        panel.alpha_composite(big, (0, 0))
        x = 360 + 24
        for s in sizes:
            panel.alpha_composite(src.resize((s, s), Image.LANCZOS), (x, 180 - s // 2))
            x += s + 24
        sheet.paste(panel.convert("RGB"), (24, y))
    ImageDraw.Draw(sheet).text((24, 6), f"{faction}: render, 96 px, 40 px, 20 px (1x) on console black and page light",
                                fill=(220, 220, 220))
    sheet.save(os.path.join(d, "check.png"), optimize=True)
    print("wrote", os.path.relpath(d, ROOT), "crest.png mark-40.png mark-20.png check.png")


if __name__ == "__main__":
    main()
