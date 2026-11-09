"""Fleet Command: screenshots of Blender building one Vhorr Brood piece, stage by stage, with its interface on screen.

Owns showing the owner how the Brood's organic kit is made (owner, 2026-11-01, relayed by the cloud session: "Can you
order the Mac to take some screenshots of the blender process for making UI stuff in the brood style?"). It sculpts
nothing of its own: it runs the kit's own code (tools/blender/build_ui_vhorr_organic.py and the sculptors in
tools/blender/organic_components.py) on one slot, the weapons panel (a frame in style "wrap", two larva keys and three
wart tabs), keeps each layer of the height field as its own mesh, and screenshots Blender's window after each stage:
the blockout, bone, muscle, skin, veins and sacs, the material masks, the node tree, the lights and camera, the Cycles
viewport and the final render. With --nodes it shows instead what each node of the anatomy material does: each step's
output wired to an Emission viewer, seen through the camera, above that node framed close in the Shader Editor (owner,
2026-11-01: "I want to see what nodes do"). Run with Blender's interface (not --background):

    /Applications/Blender.app/Contents/MacOS/Blender --factory-startup -p 0 0 1728 1080 \\
        --python tools/blender/brood_process_shots.py -- --out docs/screenshots/blender/brood-process
    ... -- --nodes --out docs/screenshots/blender/brood-nodes

Units: 1 mm is 1 CSS px of the 1920 x 1080 console (as in the kit's generator); Blender's y runs up.
"""

import math
import os
import sys

import bpy
from mathutils import Euler, Vector

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
import numpy as np  # noqa: E402
import build_ui_vhorr_organic as bo  # noqa: E402  (the kit's generator: layout, sculpt, scene, materials, render)
import organic_components as oc  # noqa: E402
import organic_study as st  # noqa: E402

ARGV = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
OUT = os.path.join(bo.ROOT, ARGV[ARGV.index("--out") + 1] if "--out" in ARGV else "docs/screenshots/blender/brood-process")
SLOT = "panel.weapons"
SAMPLES = 24
MM = st.MM


def gui_reset(samples):
    """organic_study.reset without read_factory_settings (which would also reset the open interface): the same render
    settings, world and orthographic camera, on the current scene."""
    for ob in list(bpy.data.objects):
        bpy.data.objects.remove(ob)
    sc = bpy.context.scene
    sc.render.engine = "CYCLES"
    sc.cycles.samples = samples
    sc.cycles.seed = 0
    sc.cycles.use_denoising = True
    sc.cycles.max_bounces = 10
    sc.view_settings.view_transform = "AgX"
    sc.view_settings.look = "AgX - Medium High Contrast"
    sc.render.image_settings.file_format = "PNG"
    w = bpy.data.worlds.new("w")
    w.use_nodes = True
    w.node_tree.nodes["Background"].inputs["Color"].default_value = (0.004, 0.003, 0.004, 1)
    sc.world = w
    cam = bpy.data.cameras.new("cam")
    cam.type = "ORTHO"
    cam.clip_start, cam.clip_end = 0.001, 2.0
    co = st.link(bpy.data.objects.new("cam", cam))
    sc.camera = co
    return sc


st.reset = gui_reset

# The layers: Field.finish stretches the skin over bone and muscle; keep both as they stood just before it.
LAYERS = {}
_finish = oc.Field.finish
VEINS = [True]


def finish(self, veins=True):
    LAYERS["B"] = self.B.copy()
    LAYERS["M"] = np.maximum(np.maximum(self.M, self.B - 0.5), self.B)
    return _finish(self, VEINS[0])


oc.Field.finish = finish

bo.load_styles(bo.STYLES_FILE)
SLOTS, _size = bo.load_layout(os.path.join(bo.OUT, "blockout.json"), "desktop")
TOP = SLOTS[SLOT]
OWN = {SLOT, *TOP["children"]}
BL = bo.SLOT_BLEED.get(SLOT, bo.BLEED)
X, Y, W, H = TOP["rect"]
BLED = [X - BL, Y - BL, W + 2 * BL, H + 2 * BL]
CX, CY = (X + W / 2) * MM, -(Y + H / 2) * MM   # the slot's centre in Blender (m)
STATE = {}


def win():
    return bpy.context.window_manager.windows[0]


def areas(kind):
    return [a for a in win().screen.areas if a.type == kind]


def view3d():
    a = max(areas("VIEW_3D"), key=lambda a: (a.width * a.height // 1000, a.y))   # the main viewport; of two halves, the top
    return a, a.spaces.active, next(r for r in a.regions if r.type == "WINDOW")


def ctx():
    a, _s, r = view3d()
    return bpy.context.temp_override(window=win(), screen=win().screen, area=a, region=r)


def look(top=True, dist=0.9, rot=(38.0, 0.0, 14.0), at=None):
    """Point the viewport: straight down through the camera (top) or obliquely at the slot."""
    _a, s, _r = view3d()
    s.clip_start, s.clip_end = 0.0005, 20.0
    r3 = s.region_3d
    if top:
        r3.view_perspective = "CAMERA"
        with ctx():
            bpy.ops.view3d.view_center_camera()
        return
    r3.view_perspective = "PERSP"
    r3.view_rotation = Euler([math.radians(v) for v in rot]).to_quaternion()
    if at is None:   # frame what is selected, then step back to dist times that framing
        with ctx():
            bpy.ops.view3d.view_selected()
        r3.view_distance *= dist
    else:
        r3.view_location = Vector(at)
        r3.view_distance = dist


def shading(kind="SOLID", color="MATERIAL", cavity=True, light="STUDIO"):
    _a, s, _r = view3d()
    s.shading.type = kind
    if kind == "SOLID":
        s.shading.light = light
        s.shading.color_type = color
        s.shading.show_cavity = cavity
        s.shading.cavity_type = "BOTH"
        s.shading.show_shadows = False
    s.overlay.show_floor = False
    s.overlay.show_axis_x = s.overlay.show_axis_y = False
    s.overlay.show_extras = STATE.get("extras", False)   # lights and camera outlines: only from the lights stage on


def only(*names):
    """Show these objects (and select the first); hide every other mesh, viewport and render alike."""
    for ob in bpy.data.objects:
        if ob.type == "MESH":
            show = ob.name in names or any(ob.name.startswith(n + ".") or ob.get("stage") == n for n in names)
            ob.hide_set(not show)
            ob.hide_render = not show
    for ob in bpy.context.view_layer.objects:
        ob.select_set(False)
    first = bpy.data.objects.get(names[0])
    if first:
        first.select_set(True)
        bpy.context.view_layer.objects.active = first
    props("OBJECT")


def props(tab):
    for a in areas("PROPERTIES"):
        try:
            a.spaces.active.context = tab
        except TypeError:
            pass


def flat_mat(name, rgb):
    m = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    m.diffuse_color = (*rgb, 1.0)
    return m


def layer_mesh(name, S, colour, attr=None):
    """One layer of the height field as its own mesh, coloured for the solid viewport."""
    f = STATE["f"]
    sub = f.win((BLED[0] - bo.MARGIN, BLED[1] - bo.MARGIN, BLED[0] + BLED[2] + bo.MARGIN, BLED[1] + BLED[3] + bo.MARGIN), 0)
    masks = {k: v[sub][::-1] for k, v in f.masks.items()}
    glow = attr[sub][::-1] if attr is not None else f.glowc[sub][::-1]   # a colour layer for the viewport rides as gc
    ob = st.field_mesh(name, f.X[sub][::-1], -f.Y[sub][::-1], S[sub][::-1], masks, cut_mm=oc.FLOOR_MM + 3.0, glow=glow)
    if attr is not None:
        ob.data.color_attributes.active_color = ob.data.color_attributes["gc"]
    ob.data.materials.append(flat_mat(name + " look", colour))
    return ob


# ------------------------------------------------------------------------------------------------------------- stages
def s01_blockout():
    """The placeholder blockout at slot size: the panel, its keys and tabs, and the protected rectangles, from the
    layout (assets/ui/vhorr/organic/blockout.json), pinned to whole pixels by build_ui_vhorr_organic.load_layout."""
    with ctx():
        bo.scene(BLED, SAMPLES)          # the kit's own camera and light rig round the bled region
        sc = bpy.context.scene
        for o in bpy.data.objects:
            if o.type == "LIGHT":
                o["stage"] = "lights"
        colours = {"frame": (0.32, 0.30, 0.28), "button": (0.75, 0.45, 0.12), "tab": (0.55, 0.62, 0.25)}
        for sid in sorted(OWN, key=lambda i: (i != SLOT, i)):
            s = SLOTS[sid]
            r = bo.B(oc.xyxy(s["rect"]))
            z0 = 0.0 if sid == SLOT else 1.0
            ob = st.box(f"blockout {sid}", r[0], r[1], r[2], r[3], z0, z0 + 1.0, bevel=0)
            ob.data.materials.append(flat_mat("block " + s["family"], colours.get(s["family"], (0.4, 0.4, 0.4))))
            ob["stage"] = "blockout"
            for p in s["protected"]:
                q = bo.B(oc.xyxy(p["rect"]))
                pb = st.box(f"protected {sid} {p['kind']}", q[0], q[1], q[2], q[3], z0 + 1.0, z0 + 1.6, bevel=0)
                pb.data.materials.append(flat_mat("protected", (0.85, 0.12, 0.1)))
                pb["stage"] = "blockout"
        sc.camera.data.show_name = True
    only("blockout " + SLOT, "blockout")
    shading("SOLID", "MATERIAL", cavity=False, light="FLAT")
    look(top=True)


def sculpt():
    VEINS[0] = False
    f0 = bo.grow_field(SLOTS, TOP["rect"], {}, OWN, bleed=BL)     # the skin before veins (same seeds)
    STATE["skin"] = f0.height.copy()
    VEINS[0] = True
    STATE["f"] = bo.grow_field(SLOTS, TOP["rect"], {}, OWN, bleed=BL)


def s02_bone():
    """Bone: the armature the sculptors raise first (Field.B: rib floor, spines, rings), organic_components.panel/key."""
    sculpt()
    with ctx():
        layer_mesh("2 bone", LAYERS["B"], (0.86, 0.80, 0.66))
    only("2 bone")
    shading("SOLID", "MATERIAL")
    look(top=False)


def s03_muscle():
    """Muscle over the bone (Field.M: body plateaus, bundles, the tentacles of style wrap, the larva bodies)."""
    with ctx():
        layer_mesh("3 muscle", LAYERS["M"], (0.62, 0.22, 0.2))
    only("3 muscle")
    look(top=False)


def s04_skin():
    """One leather skin stretched over both (Field.finish: drape, blur, dense anatomy, creases, openings), no veins."""
    with ctx():
        layer_mesh("4 skin", STATE["skin"], (0.36, 0.25, 0.2))
    only("4 skin")
    look(top=False)


def s05_veins():
    """Veins raised under the skin and the sacs of light (Field.finish veins, Field.pust and the sac colours glowc),
    shown as a colour layer: veins red, sacs in their own light colour, skin dark."""
    f = STATE["f"]
    g = f.glowc / max(float(f.glowc.max()), 1e-6)
    vis = 0.08 + 0.85 * f.masks["vein"][..., None] * np.array([0.9, 0.15, 0.1]) + g * np.clip(f.pust, 0, 1)[..., None]
    with ctx():
        layer_mesh("5 veins and sacs", f.height, (0.3, 0.3, 0.3), attr=np.clip(vis, 0, 1))
    only("5 veins and sacs")
    shading("SOLID", "VERTEX")
    look(top=False, dist=0.62, rot=(40.0, 0.0, 10.0))


def s06_masks():
    """The material masks the sculpt writes (field_mesh attribute m1: bone red, muscle green, vein blue), which the
    anatomy material blends its layers by."""
    ob = bpy.data.objects["5 veins and sacs"]
    ob.data.color_attributes.active_color = ob.data.color_attributes["m1"]
    only("5 veins and sacs")
    shading("SOLID", "VERTEX", cavity=False, light="FLAT")
    look(top=True)


def s07_nodes():
    """The final sculpt with the kit's own materials (build_ui_vhorr_organic.materials: organic_study.anatomy step 26),
    its instruments placed (glass, bronze rims, key caps with lit rims), and the anatomy node tree in the Shader Editor."""
    f = STATE["f"]
    with ctx():
        M = bo.materials()
        ob = bo.mesh(f, M, (BLED[0] - bo.MARGIN, BLED[1] - bo.MARGIN, BLED[0] + BLED[2] + bo.MARGIN, BLED[1] + BLED[3] + bo.MARGIN))
        ob.name = "6 sculpt"
        n0 = set(bpy.data.objects.keys())
        bo.place(f, M)
        for nm in set(bpy.data.objects.keys()) - n0:
            bpy.data.objects[nm]["stage"] = "instruments"
    only("6 sculpt", "instruments")
    main, _s, _r = view3d()
    before = set(win().screen.areas[:])
    with ctx():                              # split the main viewport: its lower part becomes the Shader Editor
        bpy.ops.screen.area_split(direction="HORIZONTAL", factor=0.5)
    new = [a for a in win().screen.areas if a not in before]
    low = min([main] + new, key=lambda a: a.y) if new else None   # area.y counts up from the window's bottom
    if low is not None:
        low.type = "NODE_EDITOR"
        low.ui_type = "ShaderNodeTree"
        STATE["nodes"] = low
    props("MATERIAL")
    shading("MATERIAL")
    look(top=False, dist=0.85)
    STATE["frame_nodes"] = True


def arrange(nt):
    """Lay a node tree out in columns by its distance from the output, for reading only (the generator leaves every node
    at the origin; the links, and so the material, are unchanged)."""
    out = next(n for n in nt.nodes if n.type == "OUTPUT_MATERIAL")
    depth = {out: 0}
    todo = [out]
    while todo:
        n = todo.pop()
        for i in n.inputs:
            for lk in i.links:
                m = lk.from_node
                if depth.get(m, -1) < depth[n] + 1:
                    depth[m] = depth[n] + 1
                    todo.append(m)
    STATE["depth"] = {n.name: d for n, d in depth.items()}
    cols = {}
    for n, d in depth.items():
        cols.setdefault(d, []).append(n)
    for d, ns in cols.items():
        y = 0.0
        for n in ns:
            n.location = (-d * 260.0, y)
            y -= max(n.dimensions.y, 120.0) + 30.0
    for n in nt.nodes:
        if n not in depth:
            n.location = (300.0, 0.0)


def frame_nodes():
    tl = STATE.get("nodes")
    if tl is None:
        return
    ob = bpy.data.objects.get("6 sculpt")
    if ob and ob.active_material:
        arrange(ob.active_material.node_tree)
    reg = next(r for r in tl.regions if r.type == "WINDOW")
    with bpy.context.temp_override(window=win(), screen=win().screen, area=tl, region=reg):
        bpy.ops.node.view_all()


def frame_nodes_view():
    tl = STATE.get("nodes")
    if tl is None:
        return
    reg = next(r for r in tl.regions if r.type == "WINDOW")
    with bpy.context.temp_override(window=win(), screen=win().screen, area=tl, region=reg):
        bpy.ops.node.view_all()


def s07b_nodes_close():
    """A close-up of the anatomy material's node tree: the whole tree above, and below, its output end (the Principled
    BSDF, the mixes and the attribute nodes reading the sculpt's masks m1 and m2 and the sac colour gc) near enough to read."""
    top = view3d()[0]
    STATE["top"] = top
    top.type = "NODE_EDITOR"
    top.ui_type = "ShaderNodeTree"
    STATE["frame_close"] = True


def frame_close():
    top, low = STATE.get("top"), STATE.get("nodes")
    ob = bpy.data.objects.get("6 sculpt")
    if top is not None:
        reg = next(r for r in top.regions if r.type == "WINDOW")
        with bpy.context.temp_override(window=win(), screen=win().screen, area=top, region=reg):
            bpy.ops.node.view_all()
    if low is not None and ob and ob.active_material:
        nt = ob.active_material.node_tree
        near = {k for k, d in STATE.get("depth", {}).items() if d <= 2}
        for n in nt.nodes:   # the output end's first rows only: its columns run tall, and all of them would not read
            n.select = n.name in near and n.location.y > -520.0
        reg = next(r for r in low.regions if r.type == "WINDOW")
        with bpy.context.temp_override(window=win(), screen=win().screen, area=low, region=reg):
            bpy.ops.node.view_selected()
        for n in nt.nodes:
            n.select = False


def s08_lights():
    """The light rig (organic_study.light_rig: a warm key from above, two grazing rims, a dim cool fill) and the
    orthographic camera framing the slot and its bleed: from outside above, and through the camera below."""
    nodes = STATE.get("nodes")
    if nodes is not None:
        nodes.type = "VIEW_3D"
    if STATE.get("top") is not None:
        STATE["top"].type = "VIEW_3D"
    only("6 sculpt", "instruments")
    for o in bpy.data.objects:
        if o.type == "LIGHT":
            o.hide_set(False)
    props("DATA")
    sun = next((o for o in bpy.data.objects if o.type == "LIGHT"), None)
    if sun:
        for o in bpy.context.view_layer.objects:
            o.select_set(False)
        sun.select_set(True)
        bpy.context.view_layer.objects.active = sun
    STATE["extras"] = True
    shading("SOLID", "MATERIAL")
    big = view3d()[0]
    look(top=False, dist=1.4, rot=(58.0, 0.0, 30.0), at=(CX, CY + 0.12, 0.05))
    for a in areas("VIEW_3D"):
        if a.as_pointer() != big.as_pointer():                    # the other half looks through the camera
            s = a.spaces.active
            s.region_3d.view_perspective = "CAMERA"
            s.shading.type = "SOLID"
            s.shading.color_type = "MATERIAL"
            s.overlay.show_floor = False
            s.clip_start, s.clip_end = 0.0005, 20.0
            reg = next(r for r in a.regions if r.type == "WINDOW")
            with bpy.context.temp_override(window=win(), screen=win().screen, area=a, region=reg):
                bpy.ops.view3d.view_center_camera()


def s09_cycles():
    """The Cycles viewport through the camera: the kit's materials under the kit's lights, on the Metal GPU, beside
    the solid view from outside."""
    prefs = bpy.context.preferences.addons["cycles"].preferences
    try:
        prefs.compute_device_type = "METAL"
        prefs.get_devices()
        for d in prefs.devices:
            d.use = True
        bpy.context.scene.cycles.device = "GPU"
    except (TypeError, AttributeError):
        pass
    bpy.context.scene.cycles.preview_samples = SAMPLES
    only("6 sculpt", "instruments")
    props("RENDER")
    big = view3d()[0]
    for a in areas("VIEW_3D"):
        if a.as_pointer() != big.as_pointer():
            a.spaces.active.shading.type = "RENDERED"
    STATE["extras"] = False
    shading("SOLID", "MATERIAL")
    look(top=False, dist=0.85)


def s10_render():
    """The final render (the kit's settings: 2 px per mm, transparent film) in the Image Editor, beside the approved
    Brood console (docs/screenshots/mockups/console-skins-vhorr-desktop.png), where this slot sits at lower left."""
    sc = bpy.context.scene
    path = os.path.join(OUT, "render-panel.weapons.png")
    sc.render.filepath = path
    for a in areas("VIEW_3D"):
        a.spaces.active.shading.type = "SOLID"
    with ctx():
        bpy.ops.render.render(write_still=True)
    img = bpy.data.images.load(path, check_existing=True)
    console = bpy.data.images.load(os.path.join(bo.ROOT, "docs", "screenshots", "mockups", "console-skins-vhorr-desktop.png"))
    vs = sorted(areas("VIEW_3D"), key=lambda a: -a.y)    # top first
    for a, im in zip(vs, (img, console)):
        a.type = "IMAGE_EDITOR"
        a.spaces.active.image = im
        reg = next(r for r in a.regions if r.type == "WINDOW")
        with bpy.context.temp_override(window=win(), screen=win().screen, area=a, region=reg):
            bpy.ops.image.view_all(fit_view=True)


STAGES = [("01-blockout", s01_blockout, 2.0), ("02-bone", s02_bone, 2.5), ("03-muscle", s03_muscle, 2.0),
          ("04-skin", s04_skin, 2.0), ("05-veins-and-sacs", s05_veins, 2.0), ("06-material-masks", s06_masks, 2.0),
          ("07-node-tree", s07_nodes, 3.0), ("08-node-tree-close-up", s07b_nodes_close, 1.5),
          ("09-lights-and-camera", s08_lights, 2.0), ("10-cycles-viewport", s09_cycles, 45.0),
          ("11-final-render", s10_render, 2.0)]


# ------------------------------------------------------------------------------------------------- nodes mode (--nodes)
# What each node of the anatomy material does: each step's output wired to an Emission viewer (the panel above shows
# exactly that node's result through the camera) with the node and its inputs framed close in the Shader Editor below.
def mat_tree():
    return bpy.data.objects["6 sculpt"].active_material.node_tree


def src(sock):
    """The output socket feeding an input socket."""
    return sock.links[0].from_socket


def chain():
    """The base colour's chain of mixes, from the Principled BSDF back to the leather ramp (organic_study.anatomy adds
    them in the order muscle, veins, bone, crease grime, dried fluid, light sacs, depth shadow)."""
    nt = mat_tree()
    b = next(n for n in nt.nodes if n.type == "BSDF_PRINCIPLED")
    out, n = [], src(b.inputs["Base Color"]).node
    while n.type == "MIX":
        out.append(n)
        n = src(n.inputs[6]).node
    return out, n   # mixes (last added first) and the ramp


def view_socket(sock):
    """Wire sock into an Emission viewer feeding the Material Output (None: the Principled BSDF again)."""
    nt = mat_tree()
    out = next(n for n in nt.nodes if n.type == "OUTPUT_MATERIAL")
    v = nt.nodes.get("viewer") or nt.nodes.new("ShaderNodeEmission")
    v.name = v.label = "viewer"
    v.location = (out.location.x, out.location.y + 220.0)
    for lk in list(out.inputs["Surface"].links):
        nt.links.remove(lk)
    if sock is None:
        b = next(n for n in nt.nodes if n.type == "BSDF_PRINCIPLED")
        nt.links.new(b.outputs["BSDF"], out.inputs["Surface"])
        return
    for lk in list(v.inputs["Color"].links):
        nt.links.remove(lk)
    nt.links.new(sock, v.inputs["Color"])
    nt.links.new(v.outputs["Emission"], out.inputs["Surface"])


def focus_nodes():
    names = STATE.get("focus_names", [])
    low = STATE.get("nodes")
    if low is None or not names:
        return
    nt = mat_tree()
    for n in nt.nodes:   # frame the node itself, then step back so its neighbours show: near enough to read
        n.select = n.name == names[0]
    nt.nodes.active = nt.nodes.get(names[0])
    low.spaces.active.show_region_ui = False
    reg = next(r for r in low.regions if r.type == "WINDOW")
    with bpy.context.temp_override(window=win(), screen=win().screen, area=low, region=reg):
        bpy.ops.node.view_selected()
        for _ in range(STATE.get("zoom_out", 2)):
            bpy.ops.view2d.zoom_out()


def node_stage(get):
    """A stage showing one node: get() returns (output socket to view or None, the node to frame)."""
    def fn():
        sock, node = get()
        view_socket(sock)
        names = [node.name] + [lk.from_node.name for i in node.inputs for lk in i.links]
        STATE["focus_names"] = names
        STATE["focus"] = True
        shading("MATERIAL")
        look(top=True)
    return fn


def n_setup():
    s01_blockout()
    sculpt()
    s07_nodes()
    look(top=True)


def find(kind, pick=min, key=None):
    ns = [n for n in mat_tree().nodes if n.type == kind]
    return pick(ns, key=key) if key else ns[0]


def sep_m1():
    return next(n for n in mat_tree().nodes if n.type == "SEPARATE_COLOR"
                and src(n.inputs[0]).node.attribute_name == "m1")


def bsdf():
    return find("BSDF_PRINCIPLED")


NODE_STAGES = [
    ("n00-overview", n_setup, 3.0),
    ("n01-mask-bone", node_stage(lambda: (sep_m1().outputs[0], sep_m1())), 6.0),
    ("n02-mask-muscle", node_stage(lambda: (sep_m1().outputs[1], sep_m1())), 6.0),
    ("n03-mask-vein", node_stage(lambda: (sep_m1().outputs[2], sep_m1())), 6.0),
    ("n04-ambient-occlusion", node_stage(lambda: (find("AMBIENT_OCCLUSION").outputs["AO"], find("AMBIENT_OCCLUSION"))), 3.0),
    ("n05-blotch-noise", node_stage(lambda: (find("TEX_NOISE", key=lambda n: n.inputs["Scale"].default_value).outputs["Fac"],
                                             find("TEX_NOISE", key=lambda n: n.inputs["Scale"].default_value))), 6.0),
    ("n06-leather-ramp", node_stage(lambda: (chain()[1].outputs["Color"], chain()[1])), 6.0),
    ("n07-muscle-mix", node_stage(lambda: (chain()[0][-1].outputs[2], chain()[0][-1])), 6.0),
    ("n08-vein-mix", node_stage(lambda: (chain()[0][-2].outputs[2], chain()[0][-2])), 6.0),
    ("n09-bone-mix", node_stage(lambda: (chain()[0][-3].outputs[2], chain()[0][-3])), 6.0),
    ("n10-crease-grime-mix", node_stage(lambda: (chain()[0][-4].outputs[2], chain()[0][-4])), 6.0),
    ("n11-dried-fluid-mix", node_stage(lambda: (chain()[0][-5].outputs[2], chain()[0][-5])), 6.0),
    ("n12-light-sac-mix", node_stage(lambda: (chain()[0][-6].outputs[2], chain()[0][-6])), 6.0),
    ("n13-depth-shadow-mix", node_stage(lambda: (chain()[0][0].outputs[2], chain()[0][0])), 6.0),
    ("n14-wetness", node_stage(lambda: (src(bsdf().inputs["Coat Weight"]), src(bsdf().inputs["Coat Weight"]).node)), 6.0),
    ("n15-bump-height", node_stage(lambda: (src(find("BUMP").inputs["Height"]), find("BUMP"))), 6.0),
    ("n16-sac-glow", node_stage(lambda: (src(bsdf().inputs["Emission Strength"]), src(bsdf().inputs["Emission Strength"]).node)), 6.0),
    ("n17-final", node_stage(lambda: (None, bsdf())), 3.0),
]
if "--nodes" in ARGV:
    STAGES = NODE_STAGES


def shoot(name, keep=True):
    p = os.path.join(OUT, f"{name}.png") if keep else os.path.join(bpy.app.tempdir, "drop.png")
    with bpy.context.temp_override(window=win(), screen=win().screen):
        bpy.ops.wm.redraw_timer(type="DRAW_WIN_SWAP", iterations=1)   # the window as it stands now, not a stale frame
        bpy.ops.screen.screenshot(filepath=p, check_existing=False)
    if keep:
        print(f"BROOD_SHOT {p}", flush=True)


def driver():
    """Step the stages on timers so the interface redraws between a stage's setup and its screenshot."""
    os.makedirs(OUT, exist_ok=True)
    k = STATE.setdefault("k", 0)
    if STATE.get("pending"):
        name = STATE.pop("pending")
        if STATE.pop("frame_nodes", False):
            frame_nodes()
            STATE["pending"] = name
            STATE["reframe"] = True
            return 1.0
        if STATE.pop("focus", False):
            focus_nodes()
            STATE["pending"] = name
            STATE["refocus"] = True
            return 1.0
        if STATE.pop("refocus", False):
            focus_nodes()   # again, once the editor has its size (each pass frames afresh)
            STATE["pending"] = name
            return 1.0
        if STATE.pop("frame_close", False):
            frame_close()
            STATE["pending"] = name
            STATE["reclose"] = True
            return 1.0
        if STATE.pop("reclose", False):   # once more, now the editors have their sizes
            frame_close()
            STATE["pending"] = name
            return 1.0
        if STATE.pop("reframe", False):   # the arranged nodes have their sizes now: frame them again
            frame_nodes_view()
            STATE["pending"] = name
            return 1.0
        if not STATE.pop("tagged", False):   # mark every area for redraw and give it a beat before the capture
            for a in win().screen.areas:
                a.tag_redraw()
            STATE["pending"] = name
            STATE["tagged"] = True
            return 1.5
        if not STATE.pop("warmed", False):   # screen.screenshot returns the frame before the latest: take one to drop
            shoot(name, keep=False)
            for a in win().screen.areas:
                a.tag_redraw()
            STATE["pending"] = name
            STATE["tagged"] = STATE["warmed"] = True
            return 1.5
        shoot(name)
        STATE["k"] = k + 1
        if STATE["k"] >= len(STAGES):
            print("BROOD_DONE", flush=True)
            bpy.ops.wm.quit_blender()
            return None
        return 0.5
    name, fn, wait = STAGES[k]
    print(f"BROOD_STAGE {name}", flush=True)
    fn()
    STATE["pending"] = name
    return wait


def safe():
    try:
        return driver()
    except Exception:   # a failed stage is reported and Blender closed, never left open on the owner's screen
        import traceback
        traceback.print_exc()
        print("BROOD_FAIL", flush=True)
        bpy.ops.wm.quit_blender()
        return None


bpy.app.timers.register(safe, first_interval=3.0)
