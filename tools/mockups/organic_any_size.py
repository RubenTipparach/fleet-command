#!/usr/bin/env python3
"""Compose the Organic Panel Bible's "any size" sheet: the parametric sculptors' render (tools/blender/
build_ui_vhorr_organic.py --sheet) over the game's dark, each piece labelled with its sculptor and size (owner, 2026-10-31:
"If you structured your sculpting tools well. Then we should be able to sculpt panels, buttons and elements of any size").

    python3 tools/mockups/organic_any_size.py /tmp/any-size.png docs/design/organic-bible/any-size.jpg
"""
import json
import os
import sys

from PIL import Image, ImageDraw, ImageFont

FONT = "/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf"


def main(src, dst):
    meta = json.load(open(os.path.splitext(src)[0] + ".json"))
    k = meta["px_per_mm"]
    im = Image.open(src).convert("RGBA")
    bg = Image.new("RGBA", (im.width, im.height + 16 * k), (8, 9, 16, 255))   # room under the last row for its label
    bg.alpha_composite(im)
    d = ImageDraw.Draw(bg)
    f = ImageFont.truetype(FONT, 18)
    for kind, (x, y, w, h) in meta["items"]:
        d.text(((x + 2) * k, (y + h + 1.5) * k), f"{kind} {w} x {h}", fill=(230, 214, 190, 255), font=f)
    bg.convert("RGB").save(dst, quality=86)
    print("wrote", dst)


if __name__ == "__main__":
    main(sys.argv[1], sys.argv[2])
