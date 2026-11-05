#!/usr/bin/env python3
"""Write the Emblem Gallery's pictures (docs/mockups/emblem-gallery.html): each faction's finished badge, its Blender
renders face on and at an angle, copied down to 640 px JPEGs on black so the gallery page stays light. A build product of
the renders in assets/emblems/<faction>/ (tools/blender/emblem_<faction>.py); re-run after a badge is re-rendered, then
re-inline the page (tools/mockups/inline_data.py).

    python3 tools/emblems/gallery_images.py
"""
import json
import os

from PIL import Image

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT = os.path.join(ROOT, "assets", "emblems", "gallery")
BADGES = [("concord", "b-e27"), ("krai", "kc"), ("ssarith", "sd")]
PX = 640


def main():
    os.makedirs(OUT, exist_ok=True)
    pieces = {}
    for faction, key in BADGES:
        for view in ("face", "angle"):
            src = os.path.join(ROOT, "assets", "emblems", faction, f"{key}-{view}.png")
            im = Image.open(src).convert("RGBA")
            ground = Image.new("RGBA", im.size, (5, 7, 13, 255))
            ground.alpha_composite(im)
            name = f"{faction}-{view}.jpg"
            ground.convert("RGB").resize((PX, PX), Image.LANCZOS).save(os.path.join(OUT, name), quality=86, optimize=True)
            pieces[f"{faction}/{view}"] = {"size": [PX, PX], "file": name}
    json.dump({"note": __doc__.split("\n\n")[0], "pieces": pieces}, open(os.path.join(OUT, "gallery.json"), "w"), indent=1)
    print(f"wrote {len(pieces)} pictures to {os.path.relpath(OUT, ROOT)}")


if __name__ == "__main__":
    main()
