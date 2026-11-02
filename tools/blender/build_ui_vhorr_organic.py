"""Fleet Command: the Vhorr Brood console skin, sculpted on the Organic Panel Bible (organic, no nine-slice).

Owns turning a layout into the Brood's kit images. The owner, 2026-10-31: "due to the nature of this ui, nine slice is not
possible and the UI will have to be bocked out using placeholders then a full detail agent will go in and sculpt
components manually", "If you structured your sculpting tools well. Then we should be able to sculpt panels, buttons and
elements of any size", and of the blockout: "Go". This script holds no sculpting of its own: it reads a layout (the
blockout, assets/ui/vhorr/organic/blockout.json, or any list of slots with a rectangle, a kit piece and its protected
rectangles), pins every rectangle to whole pixels, hands each slot to the parametric sculptor its kit piece calls for
(tools/blender/organic_components.py), and renders. So a new slot size, a new panel or a phone layout is a re-run.

How it renders (the bible's rules 1-5): each top-level slot (a panel, a strip, a hero piece) is a region: the layout is
sculpted over the region and a margin round it (neighbours' edges included, so light and shadow match), meshed by the
study's own field_mesh (tools/blender/organic_study.py, the floor cut away: an overlay, not a scene), its instruments
placed (smoked glass with a bronze rim, key caps with lit rims, the crystal heart), lit by the bible's warm top key, two
grazing rims and a dim cool fill as suns (so every region is lit alike), and rendered face on at 2 px per mm on a
transparent film. A slot inside another (a key in a panel, a bar's track in a strip, the alert lamp) is cut from its
parent's render; its other states (a key hovered or pressed, a tab active, the lamp green or red) are rendered again over
the slot alone with only that state changed. Writes assets/ui/vhorr/organic/<slot>.<state>.png, manifest.json (kind
"organic": every slot's pinned rectangle and its state images, for docs/mockups/console-skins.html) and fit.json (rule
G1, measured on the sculpted field).

    /opt/blender/blender --background --factory-startup --python tools/blender/build_ui_vhorr_organic.py -- \\
        [--layout desktop] [--only nameplate,key.fire] [--samples 24] [--out assets/ui/vhorr/organic]
    ... -- --sheet docs/design/organic-bible/any-size.png     # the "any size" proof sheet

Units: 1 mm is 1 CSS px of the 1920 x 1080 console; the layout's y runs down the page, Blender's y up (y_b = -y).
"""

import argparse
import json
import math
import os
import shutil
import sys
import time

import bpy

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
import numpy as np  # noqa: E402
import organic_components as oc  # noqa: E402  (the parametric sculptors)
import organic_study as st  # noqa: E402  (the bible's materials, mesh builder and crystal: one implementation)
import ui_render  # noqa: E402  (the shared suns)

ROOT = os.path.normpath(os.path.join(HERE, "..", ".."))
MM = st.MM
PX = 2                  # pixels per mm in the kit images (the kit's 2x)
RES = 0.5               # sculpt samples per mm
MARGIN = 8.0            # mm of the layout sculpted round a region, so its edges are lit and shadowed as in place
LIGHT_GAIN = 1.0        # the rig's power against the study's (the same light)
KIT_DETAIL = 2.2        # the anatomy material's surface detail drawn this many times larger than the study's: the kit is
                        # seen at about 0.65 px per mm on the page, the study at 2 (measured: at page scale v25's body had
                        # a lightness spread of 12.8 against the widget's 21.9, its fine detail averaged into flat brown)
BLEED = 16.0            # mm a top-level slot's body may grow past its rectangle (horns, crests, ribs, claws), so a feature
                        # too big for the slot bleeds over the gutter and the game instead of being dropped (the owner, of
                        # the accepted widget: "this thing just looks sooo good!"); never over a neighbour's protected
                        # rectangles, which are cut back to the floor and measured by rule G1 like the slot's own
OUT = os.path.join(ROOT, "assets", "ui", "vhorr", "organic")
ISOLATE = False

STATES = {"button": ["idle", "hover", "pressed"], "tab": ["idle", "active"], "lamp": ["green", "yellow", "red"]}


# ----------------------------------------------------------------------------------------------------------- layout
def pin(r):
    """A rectangle pinned to whole pixels (CLAUDE.md 7: panels are a fixed size; no slot may change with its content)."""
    x, y, w, h = r
    x0, y0 = int(math.floor(x + 0.5)), int(math.floor(y + 0.5))
    return [x0, y0, int(math.floor(x + w + 0.5)) - x0, int(math.floor(y + h + 0.5)) - y0]


def family(piece):
    return piece.split(".")[0] if not piece.startswith("lamp.") else "lamp"


def load_layout(path, name):
    """The slots of one layout: id, kit piece, pinned rectangle, protected rectangles, children and states."""
    data = json.load(open(path))
    raw = data["layouts"][name]["slots"]
    slots = {}
    for s in raw:
        piece = s["piece"]
        fam = family(piece)
        slots[s["id"]] = {"id": s["id"], "piece": piece, "family": fam, "rect": pin(s["rect"]),
                          "protected": [p for p in s["protected"] if p["kind"] != "child-slot"],
                          "children": [p["what"] for p in s["protected"] if p["kind"] == "child-slot"],
                          "states": STATES.get(fam, ["idle"]),
                          "rest": {"button": piece.split(".")[-1] if fam == "button" else None,
                                   "tab": "active" if piece == "tab.active" else "idle",
                                   "lamp": piece.split(".")[-1]}.get(fam)}
    for s in slots.values():   # children are named by their slot ids (the blockout records them so)
        s["children"] = [c for c in s["children"] if c in slots]
    parents = {c: s["id"] for s in slots.values() for c in s["children"]}
    for s in slots.values():
        s["parent"] = parents.get(s["id"])
    return slots, data["layouts"][name]["size"]


def prot(s, kinds=None):
    return [p["rect"] for p in s["protected"] if kinds is None or p["kind"] in kinds]


# The owner's picks, one style per panel and a body per key (organic_components.STYLES and KEY_BODIES), are data: the
# styles file beside the blockout (assets/ui/vhorr/organic/styles.json; survey section W, 2026-11-01), loaded by
# load_styles(). A slot missing from it takes DEFAULT_STYLE; an unknown style or key body is an error.
STYLES_FILE = os.path.join(OUT, "styles.json")
DEFAULT_STYLE = "ridge"
STYLE = {}
KEY_BODY = {}
BACKING = None   # the column's backing (organic_components.BACKINGS), from the styles file or --backing


def load_styles(path):
    """The per-slot styles, key bodies and backing from a styles file: {"styles": {slot: style}, "key_body": {slot:
    body}, "backing": mode}. Validated on load: an unknown key, style, body or mode stops the build naming it."""
    d = json.load(open(path))
    extra = set(d) - {"source", "styles", "key_body", "backing"}
    if extra:
        raise SystemExit(f"{path}: unknown keys {sorted(extra)}")
    for k, v in d.get("styles", {}).items():
        if v not in oc.STYLES:
            raise SystemExit(f"{path}: styles.{k} = {v!r} is not one of {oc.STYLES}")
    for k, v in d.get("key_body", {}).items():
        if v not in oc.KEY_BODIES:
            raise SystemExit(f"{path}: key_body.{k} = {v!r} is not one of {oc.KEY_BODIES}")
    if d.get("backing") is not None and d["backing"] not in oc.BACKINGS:
        raise SystemExit(f"{path}: backing = {d['backing']!r} is not one of {oc.BACKINGS}")
    STYLE.clear()
    STYLE.update(d.get("styles", {}))
    KEY_BODY.clear()
    KEY_BODY.update(d.get("key_body", {}))
    return d.get("backing")


SLOT_BLEED = {"panel.weapons": 34.0, "strip.top": 44.0, "strip.bottom": 36.0, "dial": 24.0, "inset.contacts": 24.0,
              "crest": 24.0}


def sculpt_slot(f, s, state):
    """Hand one slot to the sculptor its kit piece calls for. Every size and position comes from the slot."""
    fam, r, sid = s["family"], s["rect"], s["id"]
    if fam == "strip":
        outer = "top" if s["piece"] == "strip.top" else "bottom"
        wins = prot(s, {"label", "bar-cells"}) + [c["rect"] for c in s.get("child_rects", []) if c["family"] == "bar"]
        sty = STYLE.get(sid, DEFAULT_STYLE)
        oc.strip(f, r, windows=wins, seed=sid, outer=outer, spine=sty in ("ridge", "mixed"))
        allw = wins + [c["rect"] for c in s.get("child_rects", [])]
        if sty in ("crown", "mixed"):
            oc.strip_crown(f, r, allw, outer, sid, scale=1.0 if outer == "top" else 0.8, half=sty == "mixed")
        elif sty == "hang":       # longer, straighter tentacles dripping off the strip's inner edge
            oc.strip_crown(f, r, allw, outer, sid, scale=1.5, bend=(0.3, 0.8))
        elif sty == "web":        # tentacles strung along the strip between its readouts, holding them
            rng = np.random.default_rng(oc.sid(sid) + 5)
            ws = sorted(oc.grow(oc.xyxy(q), 3.0) for q in allw)
            x0, y0, x1, y1 = oc.xyxy(r)
            yy = (y0 + y1) / 2
            for a, b in zip([(x0 + 6, 0, x0 + 6, 0)] + ws, ws + [(x1 - 6, 0, x1 - 6, 0)]):
                if b[0] - a[2] > 12:
                    m = ((a[2] + b[0]) / 2, yy + rng.uniform(2, 6))
                    f.tentacle([(a[2], yy - 4), m, (b[0], yy - 4)], 6.0, 2.0, height=9.0)
        elif sty in ("wrap", "feelers"):
            oc.tentacle_styles(f, sty, oc.xyxy(r), [], [], np.random.default_rng(oc.sid(sid)))
    elif fam == "lamp":
        oc.lamp(f, r, state=state or "yellow", seed=sid)
    elif fam == "nameplate":
        oc.panel(f, r, windows=prot(s, {"portrait-window"}), labels=prot(s, {"label"}), seed=sid, deep=2.0,
                 style=STYLE.get(sid, DEFAULT_STYLE))
    elif fam == "helm":
        oc.helm(f, r, faces=prot(s, {"key-face"}), seed=sid, style=STYLE.get(sid, DEFAULT_STYLE))
    elif fam in ("frame", "ssd") and s["piece"] == "ssd.bezel":
        oc.bezel(f, r, window=prot(s, {"live-drawing"})[0], seed=sid, style=STYLE.get(sid, DEFAULT_STYLE))
    elif fam == "frame":
        oc.panel(f, r, windows=prot(s, {"icon", "live-drawing"}), labels=prot(s, {"label"}), seed=sid, style=STYLE.get(sid, DEFAULT_STYLE),
                 avoid=[c["rect"] for c in s.get("child_rects", [])])
    elif fam == "ssd":
        oc.bezel(f, r, window=prot(s, {"live-drawing"})[0], seed=sid, style=STYLE.get(sid, DEFAULT_STYLE))
    elif fam == "divider":
        oc.cord(f, r, seed=sid)
    elif fam == "button":
        lab = prot(s, {"key-label"})
        oc.key(f, r, seed=sid, state=state or "idle", label=lab[0] if lab else None, body=KEY_BODY.get(sid, "larva"))
        top = f.objects[-1]["top"]   # the label is printed on the metal button: measured over the button's face
        for p in prot(s, {"key-label"}):
            f.protected.append((f"{sid} label",) + oc.xyxy(p) + (top,))
    elif fam == "tab":
        oc.tab(f, r, seed=sid, state=state or "idle")
    elif fam == "dial":
        oc.round_pocket(f, r, window=prot(s, {"live-drawing"})[0], seed=sid)
        if STYLE.get(sid) in ("crown", "hang", "feelers", "wrap", "mixed", "web"):
            oc.tentacle_styles(f, {"web": "wrap"}.get(STYLE[sid], STYLE[sid]), oc.xyxy(r), [], [], np.random.default_rng(oc.sid(sid)))
    elif fam == "crest":
        oc.crest(f, r, seed=sid)
        if STYLE.get(sid) in ("crown", "hang", "feelers", "wrap", "mixed", "web"):
            oc.tentacle_styles(f, {"web": "wrap"}.get(STYLE[sid], STYLE[sid]), oc.xyxy(r), [], [], np.random.default_rng(oc.sid(sid)))
    elif fam == "bar":
        pass   # a bar's track is a window of its strip, cut from the strip's render
    else:
        raise SystemExit(f"slot {sid}: no sculptor for kit piece {s['piece']}")


def grow_field(slots, region, states, own, bleed=BLEED):
    """Sculpt one slot's family (own: its id and its children's) over the region grown by its bleed and MARGIN, parents
    before children, each in its given state. Every other slot reaching into those bounds only keeps its protected
    rectangles clear (cut back to the floor, measured by rule G1): a neighbour is drawn by its own image, never twice."""
    x, y, w, h = region
    pad = MARGIN + bleed
    bounds = (x - pad, y - pad, x + w + pad, y + h + pad)
    f = oc.Field(bounds, RES, seed=oc.sid("vhorr" + str(region)))
    f.bleed = bleed

    def touches(s):
        sx, sy, sw, sh = s["rect"]
        return sx < bounds[2] and sx + sw > bounds[0] and sy < bounds[3] and sy + sh > bounds[1]
    order = sorted((s for s in slots.values() if touches(s)), key=lambda s: (s["parent"] is not None, s["id"]))
    for s in order:
        if s["id"] not in own:
            if ISOLATE:   # survey pictures: the component alone, its style whole (no neighbour to keep clear of)
                continue
            for p in s["protected"]:
                f.keepout(oc.xyxy(p["rect"]), f"{s['id']} {p['kind']} (neighbour)")
            if s["family"] in ("button", "tab", "lamp") or s["family"] == "bar":
                f.keepout(oc.xyxy(s["rect"]), f"{s['id']} (neighbour)")
            continue
        s["child_rects"] = [slots[c] | {"rect": slots[c]["rect"]} for c in s["children"]]
        sculpt_slot(f, s, states.get(s["id"], s["rest"]))
    f.finish()
    return f


# ----------------------------------------------------------------------------------------------------------- render
def B(r):
    """A layout rectangle (x0, y0, x1, y1), y down, as Blender's (x0, y0, x1, y1), y up."""
    return (r[0], -r[3], r[2], -r[1])


LIGHTS = [   # the bible's light as suns, so every region of any size is lit alike: (name, direction, W/m2, colour, angle)
    ("key", (0.08, -0.85, -0.52), 4.2, (1.0, 0.82, 0.62), 20.0),       # warm, from above
    ("rimL", (0.97, -0.2, -0.12), 4.6, (1.0, 0.72, 0.55), 12.0),        # grazing the wet ridges from the left
    ("rimR", (-0.97, -0.15, -0.1), 2.8, (0.85, 0.75, 1.0), 12.0),       # and, cooler, from the right
    ("fill", (-0.19, 0.8, -0.57), 0.3, (0.45, 0.55, 1.0), 60.0),        # dim and cool from below: the hollows stay deep
]


def scene(region, samples):
    """A fresh scene whose camera frames the region (x, y, w, h) face on at PX pixels per mm, film transparent."""
    sc = st.reset(samples)
    x, y, w, h = region
    sc.render.resolution_x, sc.render.resolution_y = int(round(w * PX)), int(round(h * PX))
    sc.render.film_transparent = True
    sc.render.image_settings.color_mode = "RGBA"
    cam = sc.camera
    cam.data.ortho_scale = max(w, h) * MM
    cam.data.sensor_fit = "AUTO"
    cam.location = ((x + w / 2) * MM, -(y + h / 2) * MM, 0.5)
    # the study's own light rig, laid round the region at its size (the accepted widget's light, not an approximation of
    # it: version 25 lit the kit with suns, which give no broad wet highlight on the rolled lips)
    st.light_rig(st.KEY_W * 0.7 * LIGHT_GAIN, centre=(x + w / 2, -(y + h / 2)), scale=max(w, h, 120.0) / 545.0)
    return sc


def materials():
    """The skin's materials: the bible's accepted anatomy (step 26), its sacs' light coloured per vertex (attribute gc,
    so the alert lamp can glow amber or red while every other sac glows life green), glass, bronze, key caps."""
    m = st.anatomy("anatomy", 26, detail=KIT_DETAIL, bone_gain=1.6)
    n, l = st.nt(m)
    a = n.new("ShaderNodeAttribute")
    a.attribute_type = "GEOMETRY"
    a.attribute_name = "gc"
    l.new(a.outputs["Color"], n["Principled BSDF"].inputs["Emission Color"])
    return {"anatomy": m, "glass": st.smoked("glass", veins=False),
            "bronze": st.principled("bronze", st.PAL["bronze"], rough=0.3, metal=1.0),
            "cap": st.principled("keycap", "#3a332b", rough=0.3, metal=1.0, coat=0.6, coat_rough=0.08),   # a metal button
            "pocket": st.principled("pocket", "#020101", rough=0.9)}


def place(f, M, only_rect=None):
    """The instruments the sculptors listed: glass with a bronze rim in every window, a dark cap with a lit rim in every
    key and tab (pressed: sunk 3 mm and dropped 2 mm; the flesh stays), the crystal heart in the crest."""
    for k, o in enumerate(f.objects):
        kind = o["kind"]
        if kind == "glass":
            r = B(o["rect"])
            st.assign(st.box(f"glass{k}", r[0] - 2, r[1] - 2, r[2] + 2, r[3] + 2, o["z"] - 2, o["z"], bevel=0), M["glass"])
            st.rect_ring(f"rim{k}", r, o["z"] + 1.2, 0.8, M["bronze"], inset=0.6, corner=3.0)
        elif kind == "glass_round":
            cx, cy = o["c"]
            rr = o["r"]
            bpy.ops.mesh.primitive_circle_add(vertices=64, radius=(rr + 2) * MM, fill_type="NGON",
                                              location=(cx * MM, -cy * MM, o["z"] * MM))
            st.assign(bpy.context.active_object, M["glass"])
            st.rect_ring(f"rim{k}", (cx - rr, -cy - rr, cx + rr, -cy + rr), o["z"] + 1.2, 0.8, M["bronze"], inset=0.6, corner=rr)
        elif kind in ("key", "tab"):
            r = B(o["rect"])
            state = o["state"]
            sink, drop = (3.0, 2.0) if state == "pressed" else (0.0, 0.0)
            top = o.get("top", oc.KEY_TOP_Z if kind == "key" else oc.KEY_TOP_Z - 1.5)
            st.assign(st.box(f"cap{k}", r[0] + 0.8, r[1] + 0.8 - drop, r[2] - 0.8, r[3] - 0.8 - drop, top - 9.0 - sink,
                             top - sink, bevel=1.6, segs=4), M["cap"])
            st.assign(st.box(f"pit{k}", r[0] - 3, r[1] - 3, r[2] + 3, r[3] + 3, oc.GLASS_Z - 12.0, oc.GLASS_Z - 10.0, bevel=0),
                      M["pocket"])
            if kind == "key":
                col, es = (st.PAL["accent_sat"], {"idle": 0.9, "hover": 2.6, "pressed": 3.6}[state])
            else:
                col, es = (st.PAL["readout"], 1.8) if state == "active" else (st.PAL["bronze"], 0.25)
            rim = st.glow(f"rimg{k}", col, es)
            st.rect_ring(f"caprim{k}", (r[0] + 2.2, r[1] + 2.2 - drop, r[2] - 2.2, r[3] - 2.2 - drop), top + 0.15 - sink, 0.6,
                         rim, corner=2.6)
        elif kind == "crystal":
            cx, cy = o["c"]
            if not (f.bounds[0] <= cx <= f.bounds[2] and f.bounds[1] <= cy <= f.bounds[3]):
                continue   # a neighbour's crystal, outside this region's sculpt
            cz = height_at(f, cx, cy) - 3.0
            st.crystal_heart(cx, -cy, cz, o["scale"], frosted=True)


def height_at(f, x, y):
    j = int(round((y - f.bounds[1]) * f.px))
    i = int(round((x - f.bounds[0]) * f.px))
    return float(f.height[j, i])


def mesh(f, M, sub=None):
    """The field as the one sculpted mesh (y flipped, rows reversed so its faces look at the camera), optionally only
    the samples within sub (x0, y0, x1, y1) of the layout."""
    s = f.win(sub, 0) if sub else (slice(None), slice(None))
    X, Y, S = f.X[s][::-1], -f.Y[s][::-1], f.height[s][::-1]
    masks = {k: v[s][::-1] for k, v in f.masks.items()}
    ob = st.field_mesh("sculpt", X, Y, S, masks, cut_mm=oc.FLOOR_MM + 3.0, glow=f.glowc[s][::-1])
    st.assign(ob, M["anatomy"])
    return ob


def render(region, f, path, samples, sub_pad=MARGIN):
    """Render the region of the field to path (RGBA PNG at PX per mm)."""
    x, y, w, h = region
    sc = scene(region, samples)
    M = materials()
    mesh(f, M, (x - sub_pad, y - sub_pad, x + w + sub_pad, y + h + sub_pad))
    place(f, M)
    sc.render.filepath = path
    t0 = time.time()
    bpy.ops.render.render(write_still=True)
    ui_render.strip_png_metadata(path)
    return time.time() - t0


def crop(src, region, rect, dst):
    """Cut rect (x, y, w, h) out of a render of region, at PX per mm, into dst."""
    img = bpy.data.images.load(src)
    W, H = img.size
    px = np.array(img.pixels[:], dtype=np.float32).reshape(H, W, 4)[::-1]     # rows top down
    x0 = int(round((rect[0] - region[0]) * PX))
    y0 = int(round((rect[1] - region[1]) * PX))
    w, h = int(round(rect[2] * PX)), int(round(rect[3] * PX))
    out = px[y0:y0 + h, x0:x0 + w]
    o = bpy.data.images.new("crop", w, h, alpha=True)
    o.pixels.foreach_set(out[::-1].ravel())
    o.filepath_raw = dst
    o.file_format = "PNG"
    o.save()
    bpy.data.images.remove(o)
    bpy.data.images.remove(img)


# ------------------------------------------------------------------------------------------------------------- main
def build(args):
    slots, size = load_layout(args.blockout, args.layout)
    os.makedirs(args.out, exist_ok=True)
    tops = [s for s in slots.values() if s["parent"] is None]
    want = set(args.only.split(",")) if args.only else None
    pieces, fit, times = {}, {}, {}
    for t in sorted(tops, key=lambda s: s["id"]):
        fam_ids = [t["id"]] + t["children"]
        if want and not (want & set(fam_ids)):
            continue
        region = t["rect"]
        own = set(fam_ids)
        bl = SLOT_BLEED.get(t["id"], BLEED)
        f = grow_field(slots, region, {}, own, bleed=bl)
        x, y, w, h = region
        bled = [x - bl, y - bl, w + 2 * bl, h + 2 * bl]
        for label, d in f.clearance().items():
            fit[label] = min(d, fit.get(label, 99.0))
        if args.fit_only:
            continue
        full = os.path.join(args.out, f"{t['id']}.{t['rest'] or 'idle'}.png")
        times[t["id"]] = round(render(bled, f, full, args.samples), 1)
        print(f"VHORR {t['id']} {region} {times[t['id']]} s", flush=True)
        for c in t["children"]:   # each child at rest, cut from its parent
            cs = slots[c]
            crop(full, bled, cs["rect"], os.path.join(args.out, f"{c}.{cs['rest'] or 'idle'}.png"))
            for state in cs["states"]:   # each other state: only that slot re-rendered with the state changed
                if state == (cs["rest"] or "idle") or args.rest_only:
                    continue
                fs = grow_field(slots, region, {c: state}, own, bleed=bl)
                p = os.path.join(args.out, f"{c}.{state}.png")
                times[f"{c}.{state}"] = round(render(cs["rect"], fs, p, args.samples), 1)
                print(f"VHORR {c}.{state} {times[f'{c}.{state}']} s", flush=True)
    if BACKING and not want and not args.fit_only:   # the column's backing (survey W8), drawn under every panel
        build_backing(args, BACKING)
    # the manifest of every image present
    for s in slots.values():
        for state in s["states"]:
            fn = f"{s['id']}.{state}.png"
            if os.path.exists(os.path.join(args.out, fn)):
                pieces[f"{s['id']}.{state}"] = {"file": fn, "slot": s["id"], "state": state, "piece": s["piece"], "rect": s["rect"]}
    portrait = os.path.join(ROOT, "docs", "design", "organic-bible", "portrait-pixels.png")
    shutil.copyfile(portrait, os.path.join(args.out, "portrait.png"))
    pieces["portrait"] = {"file": "portrait.png", "slot": "nameplate", "state": "live"}
    slot_list = {s["id"]: {"piece": s["piece"], "rect": s["rect"], "states": s["states"], "rest": s["rest"] or "idle",
                           "parent": s["parent"], "bleed": SLOT_BLEED.get(s["id"], BLEED) if s["parent"] is None else 0.0,
                           # the glass a live drawing fills (the ship systems display, the radar), so the page draws to it
                           "live": next((pin(p["rect"]) for p in s["protected"] if p["kind"] == "live-drawing"), None)}
                  for s in slots.values()}
    portrait_rect = next((p["rect"] for p in slots["nameplate"]["protected"] if p["kind"] == "portrait-window"), None)
    man = {"skin": "vhorr", "kind": "organic", "scale": PX, "layout": args.layout, "size": size,
           "method": "every slot sculpted at its exact pinned size by tools/blender/organic_components.py (no nine-slice); "
                     "a top-level slot's image covers its rectangle grown by its bleed (mm) on every side",
           "portrait": {"rect": pin(portrait_rect) if portrait_rect else None}, "hex": st.hx.HEX,
           "slots": slot_list, "pieces": pieces}
    bk = os.path.join(args.out, f"backing.{BACKING}.json") if BACKING else ""
    if bk and os.path.exists(bk):   # the backing: its image and where it goes (its rectangle grown by its bleed)
        b = json.load(open(bk))
        pieces["backing"] = {"file": f"backing.{BACKING}.png", "slot": COLUMN, "state": BACKING, "rect": b["image_rect"]}
        man["backing"] = {"mode": BACKING, "rect": b["rect"], "image_rect": b["image_rect"]}
    if args.fit_only:
        json.dump({"rule": "G1: growth at least 2 mm clear of every protected rectangle",
                   "clearance_mm": dict(sorted(fit.items())), "min_mm": min(fit.values()) if fit else None,
                   "pass": all(v >= oc.G1_MM for v in fit.values())}, open(os.path.join(args.out, "fit.json"), "w"), indent=1)
        print("VHORR fit", json.dumps(fit), flush=True)
        return
    if not want:
        json.dump(man, open(os.path.join(args.out, "manifest.json"), "w"), indent=1)
        json.dump({"rule": "G1: growth at least 2 mm clear of every protected rectangle",
                   "clearance_mm": dict(sorted(fit.items())), "min_mm": min(fit.values()) if fit else None,
                   "pass": all(v >= oc.G1_MM for v in fit.values())}, open(os.path.join(args.out, "fit.json"), "w"), indent=1)
        json.dump(times, open(os.path.join(args.out, "render-times.json"), "w"), indent=1)
    print("VHORR fit", json.dumps(fit), flush=True)


BACKING_GAIN = 0.55   # the backing's colour values against the panels' (owner, 2026-11-01, approving backing A, which was
                      # darkened a step so the panels stand off it: the same light gave its flesh a median L* of 21.9 against
                      # the panels' 19.6-20.6; graded down after rendering, highlights included, it measures 14.8)


def grade(path, gain):
    """Scale an RGBA PNG's colour values by gain (alpha kept), in place."""
    img = bpy.data.images.load(path)
    px = np.array(img.pixels[:], dtype=np.float32).reshape(-1, 4)
    px[:, :3] *= gain
    img.pixels.foreach_set(px.ravel())
    img.filepath_raw = path
    img.file_format = "PNG"
    img.save()
    bpy.data.images.remove(img)
    ui_render.strip_png_metadata(path)


COLUMN = "column"   # the backing's slot id: the left column (layout x 0 to the strips' left end, full height)


def build_backing(args, mode):
    """The column's backing (organic_components.backing) in mode, sculpted over the column and rendered as its own
    image (backing.<mode>.png) with its rectangle and bleed, drawn under every panel."""
    slots, size = load_layout(args.blockout, args.layout)
    tops = [s for s in slots.values() if s["parent"] is None]
    cw = min(s["rect"][0] for s in tops if s["family"] == "strip")
    rect = (0.0, 0.0, float(cw), float(size[1]))
    panels = [oc.xyxy(s["rect"]) for s in tops if s["rect"][0] < cw or s["family"] == "strip"]
    bl = 14.0
    region = [0, 0, cw + bl, size[1]]
    pad = MARGIN + bl
    f = oc.Field((-pad, -pad, region[2] + pad, size[1] + pad), RES, seed=oc.sid("vhorr.backing." + mode))
    f.bleed = bl
    oc.backing(f, rect, panels, mode=mode, seed="vhorr.column")
    f.finish()
    os.makedirs(args.out, exist_ok=True)
    path = os.path.join(args.out, f"backing.{mode}.png")
    t = render(region, f, path, args.samples)
    grade(path, BACKING_GAIN)
    json.dump({"slot": COLUMN, "mode": mode, "rect": [0, 0, cw, size[1]], "image_rect": region, "px_per_mm": PX,
               "render_s": round(t, 1)}, open(os.path.splitext(path)[0] + ".json", "w"), indent=1)
    print("VHORR backing", mode, path, round(t, 1), flush=True)


def sheet(args):
    """The any-size proof: the console's own slots, in the owner's approved styles (survey W, 2026-11-01), sculpted at
    several sizes by the same dispatch the console uses (sculpt_slot), laid out on one transparent sheet."""
    def slot(sid, fam, piece, r, style=None, body=None, label=None, window=None):
        pr = []
        if label:
            pr.append({"kind": "key-label" if fam == "button" else "label", "rect": label})
        if window:
            pr.append({"kind": "bar-cells" if fam == "strip" else "live-drawing", "rect": window})
        if style:
            STYLE[sid] = style
        if body:
            KEY_BODY[sid] = body
        return {"id": sid, "family": fam, "piece": piece, "rect": r, "protected": pr, "children": [], "child_rects": []}
    items = [  # rect (x, y, w, h) in mm: one sculptor at several sizes, in the console's approved styles
        slot("sheet.key0", "button", "button.idle", (14, 14, 90, 48), body="wart", label=(39, 28, 40, 20)),
        slot("sheet.key1", "button", "button.idle", (118, 14, 160, 48), body="larva", label=(168, 28, 60, 20)),
        slot("sheet.key2", "button", "button.idle", (292, 14, 260, 48), body="larva", label=(392, 28, 60, 20)),
        slot("sheet.panel0", "frame", "frame.panel", (24, 96, 160, 160), "wrap", label=(54, 120, 60, 16),
             window=(54, 144, 100, 84)),
        slot("sheet.panel1", "frame", "frame.panel", (214, 96, 330, 150), "hang", label=(244, 120, 80, 16),
             window=(244, 144, 270, 72)),
        slot("sheet.panel2", "frame", "frame.panel", (574, 96, 120, 270), "plain", label=(594, 120, 60, 16),
             window=(594, 144, 80, 196)),
        slot("sheet.strip0", "strip", "strip.top", (24, 380, 300, 60), "mixed", label=(74, 392, 90, 16),
             window=(74, 412, 200, 14)),
        slot("sheet.strip1", "strip", "strip.top", (24, 460, 670, 60), "mixed", label=(74, 472, 90, 16),
             window=(74, 492, 560, 14)),
    ]
    region = (0, 0, 720, 560)
    f = oc.Field((-MARGIN, -MARGIN, region[2] + MARGIN, region[3] + MARGIN), RES, seed=11)
    f.bleed = 10.0
    for it in items:
        sculpt_slot(f, it, None)
    f.finish()
    t = render(region, f, args.sheet, args.samples)
    json.dump({"items": [[it["id"], STYLE.get(it["id"]) or KEY_BODY.get(it["id"]), list(it["rect"])] for it in items],
               "region": region, "px_per_mm": PX, "render_s": round(t, 1), "clearance_mm": f.clearance()},
              open(os.path.splitext(args.sheet)[0] + ".json", "w"), indent=1)
    print("VHORR sheet", args.sheet, round(t, 1), flush=True)


def board(args):
    """A style board for the owner to choose from (owner, 2026-10-31: "Get some different styles to see what i like.
    Some panels don't need the boney ridge. Some should have like a tentacle kind of thing. I want to see more
    tentacles"): one panel (a framed readout and three larva keys with metal buttons, over the rib floor) in every
    style the panel sculptor knows (organic_components.STYLES), each with a tall thin panel and a wide strip beside it,
    all from the same sculptors; labelled afterwards by the caller."""
    global PX
    PX = 1.5
    cw, ch, cols = 380, 380, 4
    rows = (len(oc.STYLES) + cols - 1) // cols
    region = (0, 0, cw * cols, ch * rows)
    f = oc.Field((-MARGIN, -MARGIN, region[2] + MARGIN, region[3] + MARGIN), RES, seed=13)
    f.bleed = 14.0
    cells = []
    for k, style in enumerate(oc.STYLES):
        x, y = (k % cols) * cw, (k // cols) * ch
        keys = [(x + 36 + 100 * j, y + 140, 88, 40) for j in range(3)]
        oc.panel(f, (x + 20, y + 40, 330, 180), labels=[(x + 40, y + 62, 90, 14)], windows=[(x + 40, y + 80, 260, 44)],
                 seed=f"board.{style}", avoid=keys, style=style)
        for j, kr in enumerate(keys):
            oc.key(f, kr, seed=f"board.{style}.key{j}", state="pressed" if j == 2 else "idle",
                   label=(kr[0] + 30, kr[1] + 13, 28, 14))
        oc.panel(f, (x + 20, y + 236, 64, 96), windows=[(x + 34, y + 258, 36, 52)], seed=f"board.{style}.tall", style=style)
        oc.panel(f, (x + 100, y + 262, 250, 46), labels=[(x + 124, y + 276, 56, 14)], windows=[(x + 190, y + 278, 140, 12)],
                 seed=f"board.{style}.strip", style=style)
        cells.append({"style": style, "x": x, "y": y})
    f.finish()
    t = render(region, f, args.board, args.samples)
    json.dump({"cells": cells, "cell_mm": [cw, ch], "px_per_mm": PX, "render_s": round(t, 1), "clearance_mm": f.clearance()},
              open(os.path.splitext(args.board)[0] + ".json", "w"), indent=1)
    print("VHORR board", args.board, round(t, 1), flush=True)


def main():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    ap = argparse.ArgumentParser()
    ap.add_argument("--blockout", default=os.path.join(OUT, "blockout.json"))
    ap.add_argument("--layout", default="desktop")
    ap.add_argument("--only", default="")
    ap.add_argument("--samples", type=int, default=24)
    ap.add_argument("--out", default=OUT)
    ap.add_argument("--sheet", default="")
    ap.add_argument("--board", default="")
    ap.add_argument("--style", default="", help="every slot in this one style (organic_components.STYLES): survey options")
    ap.add_argument("--rest-only", action="store_true", help="only each slot's resting state")
    ap.add_argument("--isolate", action="store_true", help="each slot sculpted alone, no neighbour kept clear (survey pictures)")
    ap.add_argument("--fit-only", action="store_true", help="sculpt every slot and write fit.json (rule G1); no renders")
    ap.add_argument("--styles", default=STYLES_FILE, help="the per-slot styles, key bodies and backing (JSON)")
    ap.add_argument("--backing", default="", help="render only the column's backing in these modes (comma separated: "
                    f"{', '.join(oc.BACKINGS)})")
    a = ap.parse_args(argv)
    global ISOLATE, BACKING
    ISOLATE = a.isolate
    BACKING = load_styles(a.styles)
    if a.backing:
        for mode in a.backing.split(","):
            if mode not in oc.BACKINGS:
                raise SystemExit(f"--backing {mode}: not one of {oc.BACKINGS}")
            build_backing(a, mode)
        return
    if a.style:
        if a.style not in oc.STYLES:
            raise SystemExit(f"--style {a.style}: not one of {oc.STYLES}")
        for k in list(STYLE) + ["panel.weapons", "ssd.bezel", "nameplate", "helm.pad", "inset.helm-readout", "dial",
                                "inset.contacts", "crest", "strip.top", "strip.bottom"]:
            STYLE[k] = a.style
    if a.board:
        board(a)
    elif a.sheet:
        sheet(a)
    else:
        build(a)


if __name__ == "__main__":
    main()
