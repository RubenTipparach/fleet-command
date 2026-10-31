"""Fleet Command: renders the analog panel bible's 3D tiles: the formed-metal catalogue and the lighting options.

Owns the Blender side of docs/design/ui-bible.html chapters 8 and 10. Each tile is a 320 x 220 mm plate built from the
outlines tools/mockups/formed_metal.py writes (tiles.json, already fit-checked), with the same plate, bezel, backlit
film, lamps, knobs, materials and light linking as the panel study, so a tile looks exactly as a panel built that way
would.

It is a tail, not a script of its own: it runs on the study's helper section (everything before the study's first
`panel_kit.reset_scene()`), appended to it, until those helpers move into tools/blender/ui_shapes.py (the follow-up
task in the blender-ui-panels skill). The study is not committed (CLAUDE.md sections 1 and 10), so until the move
this renders only where the study is present.

    python3 tools/mockups/formed_metal.py --out <dir>
    cat <study helpers> tools/blender/ui_tiles_tail.py > <dir>/tile_render.py
    TILE_DIR=<dir> TILES=formed|lighting [ONLY=a,b] blender --background --factory-startup --python <dir>/tile_render.py -- x 32

Units: 1 px of a tile = 1 mm; renders at 2x (640 x 440 px).
"""
import json
from mathutils import Matrix

HERE = os.environ["TILE_DIR"]
SPECS = json.load(open(os.path.join(HERE, "tiles.json")))
W, H = 320, 220
PT, ROLL = 8.0, 3.8


def edge_lamp(x0, y0, x1, y1, nx, ny, watts_per_mm, spread=120.0, tilt=90.0, depth=None):
    """A strip lamp along the edge (x0, y0)-(x1, y1) px, tucked under the lip, aimed inward along the film (nx, ny
    is the inward normal on screen, y down): a strip along one side, several make all-sides or rim light."""
    ln = math.hypot(x1 - x0, y1 - y0)
    ld = bpy.data.lights.new("lip", "AREA")
    ld.shape, ld.size, ld.size_y = "RECTANGLE", ln * MM, 6.0 * MM
    ld.energy, ld.color, ld.spread = watts_per_mm * ln * LAMPS, (1.0, 0.88, 0.6), math.radians(spread)
    ob = bpy.data.objects.new("lip", ld)
    cx, cy = (x0 + x1) / 2 + nx * 1.0, (y0 + y1) / 2 + ny * 1.0
    ob.location = ((cx - W / 2) * MM, (H / 2 - cy) * MM, (DZ + 6.0 if depth is None else depth) * MM)
    t = math.radians(tilt)
    d = Vector((nx * math.sin(t), -ny * math.sin(t), -math.cos(t))).normalized()   # light direction, world
    ex = Vector(((x1 - x0) / ln, -(y1 - y0) / ln, 0.0))                             # along the edge, world
    z = -d
    xx = (ex - z * ex.dot(z)).normalized()
    yy = z.cross(xx)
    ob.matrix_world = Matrix.Translation(ob.location) @ Matrix((xx, yy, z)).transposed().to_4x4()
    ob.visible_camera = False
    bpy.context.scene.collection.objects.link(ob)


def scene_base(name):
    panel_kit.reset_scene()
    ui_render.setup(W * 2, H * 2, SAMPLES, [("Room", (0.3, -1.0, -1.6), 0.7, (1.0, 0.97, 0.92), 8.0)], world=(0.05, 0.045, 0.04),
                    bounces=dict(max_bounces=8, glossy_bounces=4, transmission_bounces=4, diffuse_bounces=3))
    g = globals()
    g["K"] = Shaper(name, W, H, True)
    g["plate_m"], g["back"] = K.mat("satin", lambda: satin("satin")), K.mat("display", lambda: display("display"))
    g["ink"] = K.mat("ink", lambda: film_ink("ink", "#9a7f4f"))
    g["dark"] = K.mat("well", lambda: film_ink("well", "#7d6845"))
    g["groove"] = K.mat("groove", lambda: printed("groove", "#2e271c"))
    g["knobmetal"] = K.mat("knobmetal", lambda: kv.metal("knobmetal", "#d6d0c4", 0.16))
    g["red"] = K.mat("red", lambda: printed("red", "#a83020"))
    g["redink"] = K.mat("redink", lambda: film_ink("redink", "#a83020"))
    g["brass"] = K.mat("brass", lambda: kv.metal("brass", "#b0915a", 0.28))
    K.slab([rrect(0, 0, W, H, 12)], DZ - 6.0, DZ - 4.0, groove, bevel=0.5)


def build_plate(sp):
    for w in sp["windows"]:
        film(w)
    K.slab([sp["plate_off"]] + sp["holes_off"], 0.0, PT, plate_m, bevel=ROLL)      # rolls grow back to the drawn line
    for outer, hole in sp["bezel"]:
        K.slab([outer, hole], PT - ROLL - 1.8, PT - ROLL + 0.2, brass, bevel=0.6)
    for x, y, r, p in sp["knobs"]:
        knob(x, y, r, PT, pointer=p)
    for x, y, r in sp["gauges"]:
        flat([circle(x, y, r + 0.7, 64), circle(x, y, r - 0.7, 64)], ink)
        for i in range(11):
            a = math.radians(200 + 14 * i)
            stroke([(x + math.cos(a) * r * 0.86, y + math.sin(a) * r * 0.86), (x + math.cos(a) * r * 0.97, y + math.sin(a) * r * 0.97)], 1.0, ink)
        a = math.radians(250)
        stroke([(x, y), (x + math.cos(a) * r * 0.9, y + math.sin(a) * r * 0.9)], 1.2, redink)


def finish_scene(name):
    DISPLAY = {"display", "ink", "well", "redink"}
    metal_layer = bpy.data.collections.new("metal_layer")
    display_layer = bpy.data.collections.new("display_layer")
    for c in (metal_layer, display_layer):
        bpy.context.scene.collection.children.link(c)
    for ob in list(bpy.context.scene.objects):
        if ob.type in ("MESH", "CURVE", "FONT") and ob.active_material:
            m = ob.active_material.name
            (display_layer if m in DISPLAY else metal_layer).objects.link(ob)
            if m in DISPLAY and m != "display":
                ob.visible_shadow = False
    sb = bpy.data.lights.new("Softbox", "AREA")
    sb.shape, sb.size, sb.energy, sb.color = "DISK", 0.6, 1.2, (1.0, 0.94, 0.85)
    sbo = bpy.data.objects.new("Softbox", sb)
    sbo.location, sbo.rotation_euler = (-0.25, 0.35, 0.6), (math.radians(-35), math.radians(-20), 0.0)
    bpy.context.scene.collection.objects.link(sbo)
    ui_render.sun("Main", (0.3, -1.0, -1.6), MAIN, (1.0, 0.97, 0.92), MAIN_SOFT)
    cl = bpy.data.lights.new("Ceiling", "AREA")
    cl.shape, cl.size, cl.energy, cl.color = "SQUARE", 1.2, 1.6, (1.0, 0.97, 0.92)
    clo = bpy.data.objects.new("Ceiling", cl)
    clo.location = (0.0, 0.1, 0.7)
    bpy.context.scene.collection.objects.link(clo)
    for ob in bpy.context.scene.objects:
        if ob.type == "LIGHT":
            ob.light_linking.receiver_collection = display_layer if ob.name.startswith(("lip", "Main")) else metal_layer
        elif ob.active_material and ob.active_material.name in DISPLAY:
            ob.light_linking.receiver_collection = display_layer
    panel_kit.camera(W * MM, H * MM)
    sc = bpy.context.scene
    sc.render.filepath = os.path.join(HERE, f"tile_{name}.png")
    bpy.ops.render.render(write_still=True)
    print("TILE done", name, flush=True)


def lighting_tile(mode):
    """One plain display with printed text and a ruler, lit one way, to compare lighting options for legibility."""
    scene_base("light_" + mode)
    win = rrect(22, 22, W - 22, H - 52, 8)
    film(win)
    K.slab([rrect(6 + ROLL, 6 + ROLL, W - 6 - ROLL, H - 6 - ROLL, 14 - ROLL), rrect(22 - ROLL, 22 - ROLL, W - 22 + ROLL, H - 52 + ROLL, 8 + ROLL)],
           0.0, PT, plate_m, bevel=ROLL)
    K.slab([rrect(21.2, 21.2, W - 21.2, H - 51.2, 8.8), rrect(23.5, 23.5, W - 23.5, H - 53.5, 6.5)], PT - ROLL - 1.8, PT - ROLL + 0.2, brass, bevel=0.6)
    for i, t in enumerate(("THE LONG WATCH", "CONCORD FLEET ORCHESTRA", "SUITES FROM THE DEEP")):
        label(t, 42, 52 + i * 26, 13, ink, DZ + FZ, font="Tektur-Medium.ttf", align="LEFT", track=0.1)
    stroke([(42, 140), (W - 42, 140)], 1.0, ink)
    for i in range(25):
        x = 42 + i * (W - 84) / 24
        stroke([(x, 140.5), (x, 134 if i % 4 else 130)], 0.8, ink)
    knob(W / 2, H - 29, 9, PT)
    x0, y0, x1, y1 = 22, 22, W - 22, H - 52
    e = {"top": (x0 + 8, y0, x1 - 8, y0, 0, 1), "bottom": (x0 + 8, y1, x1 - 8, y1, 0, -1),
         "left": (x0, y0 + 8, x0, y1 - 8, 1, 0), "right": (x1, y0 + 8, x1, y1 - 8, -1, 0)}
    if mode == "spots":
        for x in (W * 0.2, W * 0.5, W * 0.8):
            lip_light(x, y0, 0.3)
    elif mode == "strip":
        edge_lamp(*e["top"], 0.0004)
    elif mode == "all_sides":
        for k in e:
            edge_lamp(*e[k], 0.00018)
    elif mode == "bottom":
        edge_lamp(*e["bottom"], 0.0004)
    elif mode == "rim":
        for k in e:
            edge_lamp(*e[k], 0.0004, spread=30.0, depth=DZ + 1.5)
    elif mode == "side":
        edge_lamp(*e["left"], 0.0006)
    finish_scene("light_" + mode)


which = os.environ.get("TILES", "formed")
names = [n for n in os.environ.get("ONLY", "").split(",") if n]
if which == "formed":
    for name in names or list(SPECS):
        scene_base(name)
        build_plate(SPECS[name])
        for x, y in SPECS[name]["lamps"]:
            lip_light(x, y, 0.3)
        finish_scene(name)
else:
    for mode in names or ["spots", "strip", "all_sides", "bottom", "rim", "side", "backlight"]:
        lighting_tile(mode)
