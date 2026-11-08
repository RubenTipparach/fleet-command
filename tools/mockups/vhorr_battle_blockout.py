#!/usr/bin/env python3
"""Fleet Command: the Vhorr Brood's blockout for the battle console (openspec/changes/vhorr-battle-console).

Owns the slots the parametric sculptors (tools/blender/organic_components.py, run by
tools/blender/build_ui_vhorr_organic.py) sculpt for the battle console, at the battle console's own rectangles: the
Organic Panel Bible's rule 4 (no nine-slice; owner, 2026-10-31: "If you structured your sculpting tools well. Then we
should be able to sculpt panels, buttons and elements of any size"). The console skins page's blockout
(assets/ui/vhorr/organic/blockout.json) was measured from that page's DOM; the battle console draws its panels on a
canvas at fixed reference rectangles (docs/mockups/battle-console.html, rects() and the ui* functions), so its slots are
written here from those numbers, one place.

Three layouts, each sculpted on its own so no layout's slots carve keep-outs into another's flesh:

  battle     the column (the name plate, the station, ship and weapons panels, the radar's pocket and the crest), each
             panel a body of flesh round one smoked-glass screen the console draws on, its title and officer slot kept
             clear on its rim; and the two strips, each a spine round one glass channel for its readouts
  expanded   the expanded panel's frame (owner, survey U1: a panel takes the left two thirds), the same body round one
             screen
  tray       the console's own controls, which it draws at many sizes: a key (idle, hover, pressed), a square key, a tab
             (idle, active), an inset with its screen, a divider cord and the alert lamp (green, yellow, red), sculpted
             once each, held by a tray the console never shows

    python3 tools/mockups/vhorr_battle_blockout.py        # writes assets/ui/vhorr/organic/battle/blockout.json

Units: CSS px of the 1920 x 1080 console, as the build script's mm.
"""
import json
import os

ROOT = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", ".."))
OUT = os.path.join(ROOT, "assets", "ui", "vhorr", "organic", "battle", "blockout.json")

# The battle console's rectangles (battle-console.html rects(): the column 480 wide, panels at x 6, 468 wide; strips
# 72 px tall, as the skin binds them; the plate drawn shorter, 356 wide, so the Fleet key stands beside it).
PLATE = [10, 6, 356, 56]
STATION, SHIP, WEAPONS = [6, 68, 468, 420], [6, 494, 468, 276], [6, 776, 468, 160]
DIAL, CREST = [6, 948, 120, 120], [400, 972, 72, 72]
TOP, BOTTOM = [480, 0, 1440, 72], [480, 1008, 1440, 72]
EXPANDED = [6, 34, 1268, 1040]
SCREEN = (20, 22, 20, 20)   # a panel's screen inside its rectangle: left, top, right, bottom (the skin's pad less 6 px)


def screen(r, m=SCREEN):
    x, y, w, h = r
    return [x + m[0], y + m[1], w - m[0] - m[2], h - m[1] - m[3]]


def prot(kind, what, rect):
    return {"kind": kind, "what": what, "rect": rect}


def slot(sid, piece, kind, rect, protected=()):
    return {"id": sid, "piece": piece, "kind": kind, "rect": rect, "protected": list(protected)}


def panel(sid, r, titles):
    """A panel: one screen for everything the console draws in it, its titles (placards on the rim) kept clear."""
    return slot(sid, "frame.panel", "nine", r, [prot("live-drawing", "screen", screen(r))] + [prot("label", w, q) for w, q in titles])


def layouts():
    x0 = STATION[0] + STATION[2]
    battle = [
        slot("nameplate", "nameplate", "hero", PLATE, [prot("label", "name", [PLATE[0] + 14, PLATE[1] + 12, PLATE[2] - 30, PLATE[3] - 24])]),
        # the station's officer slot (left) and title (right) on its top rim; the ship's and the weapons' titles on the
        # gap above them, centred on it (the console's titles under a column)
        panel("panel.station", STATION, [("officer", [56, 66, 250, 26]), ("title", [x0 - 190, 66, 190, 26])]),
        panel("panel.ship", SHIP, [("title", [x0 - 250, SHIP[1] - 16, 250, 26])]),
        panel("panel.weapons", WEAPONS, [("title", [x0 - 190, WEAPONS[1] - 16, 190, 26])]),
        slot("dial", "dial", "hero", DIAL, [prot("live-drawing", "radar", [DIAL[0] + 10, DIAL[1] + 10, 100, 100])]),
        slot("crest", "crest", "hero", CREST),
        # each strip's readouts sit in one glass channel 96 px in from each end (the console's strip content)
        slot("strip.top", "strip.top", "nine", TOP, [prot("bar-cells", "readouts", [TOP[0] + 92, 12, TOP[2] - 184, 48])]),
        slot("strip.bottom", "strip.bottom", "nine", BOTTOM, [prot("bar-cells", "readouts", [BOTTOM[0] + 92, BOTTOM[1] + 12, BOTTOM[2] - 184, 48])]),
    ]
    expanded = [panel("panel.expanded", EXPANDED, [("title", [EXPANDED[0] + EXPANDED[2] - 280, EXPANDED[1] - 2, 270, 26])])]
    # the tray, far from the console's rectangles, and the controls it holds
    tx, ty = 2400, 40
    key = [tx + 20, ty + 20, 150, 48]
    sq = [tx + 190, ty + 20, 48, 48]
    tab = [tx + 260, ty + 24, 120, 40]
    inset = [tx + 20, ty + 92, 180, 100]
    cord = [tx + 220, ty + 100, 160, 10]
    lamp = [tx + 400, ty + 24, 40, 40]
    held = [
        slot("key", "button.idle", "nine", key, [prot("key-label", "label", [key[0] + 40, key[1] + 14, 70, 20])]),
        slot("key.square", "button.idle", "nine", sq),
        slot("tab", "tab.idle", "nine", tab, [prot("key-label", "label", [tab[0] + 30, tab[1] + 11, 60, 18])]),
        slot("inset", "frame.inset", "nine", inset, [prot("live-drawing", "screen", [inset[0] + 12, inset[1] + 12, inset[2] - 24, inset[3] - 24])]),
        slot("divider", "divider", "nine", cord),
        slot("lamp.alert.yellow", "lamp.alert.yellow", "hero", lamp),
    ]
    tray = [slot("tray", "frame.panel", "nine", [tx, ty, 460, 212], [prot("child-slot", c["id"], c["rect"]) for c in held])] + held
    return {"battle": {"size": [1920, 1080], "slots": battle}, "expanded": {"size": [1920, 1080], "slots": expanded},
            "tray": {"size": [1920, 1080], "slots": tray}}


def main():
    out = {"skin": "vhorr", "method": "organic: no nine-slice; every slot sculpted at its exact rect (Organic Panel Bible rule 4)",
           "units": "CSS px of the 1920 x 1080 console", "written_by": "tools/mockups/vhorr_battle_blockout.py, from "
           "docs/mockups/battle-console.html's own rectangles", "layouts": layouts()}
    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    with open(OUT, "w") as f:
        json.dump(out, f, indent=1)
        f.write("\n")
    print("wrote", os.path.relpath(OUT, ROOT))


if __name__ == "__main__":
    main()
