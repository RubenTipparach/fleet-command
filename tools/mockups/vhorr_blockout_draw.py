#!/usr/bin/env python3
"""Draw the Vhorr Brood console's blockout: phase 1 of the organic rebuild (Organic Panel Bible rule 4: block out with
placeholders, then sculpt each component in place).

Reads assets/ui/vhorr/organic/blockout.json and game-view.png, both written by tools/mockups/vhorr_blockout.mjs from
the live page, and draws the desktop console at 1920 x 1080: the page's own game view fills the screen (rule 5: the
console is an overlay, so the game shows in every gutter), every slot is a flat, labelled placeholder (its id, kit piece
and size), and every rectangle growth must never cover is hatched and outlined in a second colour (labels, readouts,
live drawings, icons, bar cells, key faces, the portrait window). Child slots are drawn over their parents.

    python3 tools/mockups/vhorr_blockout_draw.py [--out docs/screenshots/mockups/vhorr-blockout-desktop.png]
"""
import json
import pathlib
import sys

from PIL import Image, ImageDraw, ImageFont

ROOT = pathlib.Path(__file__).resolve().parents[2]
SRC = ROOT / "assets/ui/vhorr/organic"
OUT = ROOT / "docs/screenshots/mockups/vhorr-blockout-desktop.png"
W, H = 1920, 1080
SLOT_FILL = (74, 30, 26, 205)      # placeholder flesh, mostly opaque so the layout reads
SLOT_EDGE = (226, 214, 187, 255)   # bone
CHILD_FILL = (40, 18, 16, 225)
GUARD = (184, 242, 122, 255)       # protected rectangles: the readout green
GUARD_HATCH = (184, 242, 122, 70)
FONT = "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf"
FONT_B = "/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf"


def font(path, size):
    try:
        return ImageFont.truetype(path, size)
    except OSError:
        return ImageFont.load_default()


def box(r):
    x, y, w, h = r
    return [x, y, x + w - 1, y + h - 1]


def hatch(d, r, step=6):
    """Diagonal hatching clipped to rect r (drawn as short segments, so it stays inside)."""
    x0, y0, w, h = r
    for k in range(-int(h), int(w), step):
        a = (x0 + max(k, 0), y0 + max(-k, 0))
        n = min(w - max(k, 0), h - max(-k, 0))
        if n > 0:
            d.line([a, (a[0] + n, a[1] + n)], fill=GUARD_HATCH, width=1)


def draw(out=OUT):
    data = json.loads((SRC / "blockout.json").read_text())
    slots = data["layouts"]["desktop"]["slots"]
    view = Image.open(SRC / "game-view.png").convert("RGB")
    k = max(W / view.width, H / view.height)   # cover the screen: the game is behind everything
    view = view.resize((round(view.width * k), round(view.height * k)), Image.LANCZOS)
    img = Image.new("RGBA", (W, H))
    img.paste(view.crop(((view.width - W) // 2, (view.height - H) // 2, (view.width - W) // 2 + W, (view.height - H) // 2 + H)))
    layer = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)
    small, tiny = font(FONT_B, 13), font(FONT, 11)
    ids = {s["id"] for s in slots}
    children = {p["what"] for s in slots for p in s["protected"] if p["kind"] == "child-slot"}
    for s in sorted(slots, key=lambda s: -s["rect"][2] * s["rect"][3]):   # parents first, children on top
        child = s["id"] in children
        d.rectangle(box(s["rect"]), fill=CHILD_FILL if child else SLOT_FILL, outline=SLOT_EDGE, width=2)
        for p in s["protected"]:
            if p["kind"] in ("child-slot",):
                continue
            hatch(d, p["rect"])
            d.rectangle(box(p["rect"]), outline=GUARD, width=1)
        x, y, w, h = s["rect"]
        f = small if (w >= 150 and h >= 30) else tiny
        for label in (f"{s['id']}  ·  {s['piece']}  ·  {w:g} x {h:g}", f"{s['id']}  {w:g}x{h:g}", s["id"], f"{w:g}x{h:g}"):
            if d.textlength(label, font=f) <= w - 8:
                break
        else:
            label = ""
        if label:   # inside the slot, along its foot (a thin slot: its top)
            ty = y + h - (17 if f is small else 14) if h >= 30 else y + 3
            d.text((x + 5, ty), label, font=f, fill=SLOT_EDGE)
    assert ids, "no slots"
    # A legend in the game view's empty middle.
    lg = ["Vhorr Brood: blockout, desktop 1920 x 1080 (phase 1 of the organic rebuild)",
          f"{len(slots)} slots in {len(data['sculpt_groups'])} sculpt groups; helm pad carries {data['helm_keys']['count']} key faces",
          "bone outline: a slot, sculpted whole at this exact size (no nine-slice)",
          "green hatch: never covered by growth (labels, readouts, live drawings, icons, bar cells, key faces, portrait window)",
          "the game shows in every gutter: an overlay, not a scene (rule 5)"]
    lx, ly = 560, 120
    d.rectangle([lx - 12, ly - 10, lx + 820, ly + 22 * len(lg) + 4], fill=(6, 6, 10, 200), outline=SLOT_EDGE)
    for i, t in enumerate(lg):
        d.text((lx, ly + i * 22), t, font=small if i == 0 else font(FONT, 13), fill=SLOT_EDGE if i == 0 else (220, 220, 210, 255))
    img.alpha_composite(layer)
    out.parent.mkdir(parents=True, exist_ok=True)
    img.convert("RGB").save(out, optimize=True)
    return out


if __name__ == "__main__":
    o = pathlib.Path(sys.argv[sys.argv.index("--out") + 1]) if "--out" in sys.argv else OUT
    print("wrote", draw(o))
