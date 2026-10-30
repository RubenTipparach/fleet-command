#!/usr/bin/env python3
"""The Material Maker (.ptex) graph kit every Fleet Command material generator builds with (material-maker skill).

A graph is the one implementation of how a material looks (CLAUDE.md section 10). Generators (`build_ptex.py`) write
graphs with this kit; Material Maker renders them headless (`export_materials.sh`, the material-maker-headless skill);
`postprocess.py` turns the renders into the game's maps and the contact sheet.

Provenance: `Graph`, `rgb`, `hexc`, `grad`, `curve`, the op and blend constants, `shader`, `fbm` and `HASH` are Crab
Breeze's `tools/materials/build_ptex.py` at 69ce196 (the `Graph` helper itself from Star Crew Bridge Simulator 8b0bbf1,
from fps-game-demo f6cd25c), unchanged; `PRELUDE`, `vor` and `finish` are Crab Breeze's
`tools/materials/build_house_ptex.py` at 69ce196, unchanged but for the docstrings.
"""
import os

HERE = os.path.dirname(os.path.abspath(__file__))
OUT_DIR = os.path.join(HERE, "ptex")

# Material inputs on the "material" node (Material Maker's static PBR output).
ALBEDO, METALLIC, ROUGHNESS, EMISSION, NORMAL, AO, DEPTH, OPACITY = range(8)


def rgb(r, g, b, a=1.0):
    return {"r": r, "g": g, "b": b, "a": a, "type": "Color"}


def hexc(h):
    h = h.lstrip("#")
    return tuple(int(h[i:i + 2], 16) / 255.0 for i in (0, 2, 4))


def grad(*stops, interpolation=1):
    """stops: (pos, '#rrggbb') or (pos, (r, g, b)) or (pos, grey_float)."""
    pts = []
    for pos, c in stops:
        if isinstance(c, str):
            c = hexc(c)
        elif isinstance(c, (int, float)):
            c = (c, c, c)
        pts.append({"pos": pos, "r": c[0], "g": c[1], "b": c[2], "a": 1})
    return {"interpolation": interpolation, "points": pts, "type": "Gradient"}


def curve(*pts):
    """Tonality curve points: (x, y) pairs, linear tangents."""
    out = []
    for i, (x, y) in enumerate(pts):
        out.append({"x": x, "y": y, "ls": 0.0, "rs": 0.0})
    # compute simple slopes
    for i in range(len(out)):
        if i > 0:
            dx = out[i]["x"] - out[i - 1]["x"]
            out[i]["ls"] = (out[i]["y"] - out[i - 1]["y"]) / dx if dx else 0
        if i < len(out) - 1:
            dx = out[i + 1]["x"] - out[i]["x"]
            out[i]["rs"] = (out[i + 1]["y"] - out[i]["y"]) / dx if dx else 0
    return {"points": out, "type": "Curve"}


class Graph:
    def __init__(self, label):
        self.label = label
        self.nodes = []
        self.conns = []
        self.counter = {}
        self.col = 0

    def node(self, type_, *inputs, name=None, **params):
        """Create a node. inputs: sequence of (node_name, port) or node_name (port 0) or None."""
        idx = self.counter.get(type_, 0)
        self.counter[type_] = idx + 1
        name = name or f"{type_}_{idx}"
        self.col += 1
        n = {
            "name": name,
            "type": type_,
            "node_position": {"x": 220 * (self.col % 8), "y": 160 * (self.col // 8)},
            "parameters": params,
        }
        self.nodes.append(n)
        for port, src in enumerate(inputs):
            if src is None:
                continue
            self.link(src, name, port)
        return name

    def link(self, src, dst, dst_port):
        if isinstance(src, tuple):
            s, sp = src
        else:
            s, sp = src, 0
        self.conns.append({"from": s, "from_port": sp, "to": dst, "to_port": dst_port})

    def material(self, albedo=None, metallic=None, roughness=None, emission=None,
                 normal=None, ao=None, depth=None, metallic_v=1.0, roughness_v=1.0,
                 emission_energy=1.0, normal_v=1.0, ao_v=1.0, transparent=False, opacity=None):
        name = self.node(
            "material", name="Material",
            albedo_color=rgb(1, 1, 1), metallic=metallic_v, roughness=roughness_v,
            emission_energy=emission_energy, normal=normal_v, ao=ao_v,
            depth_scale=0.0, flags_transparent=transparent, sss=0.0, size=11)
        self.nodes[-1]["export_paths"] = {}
        for port, src in ((ALBEDO, albedo), (METALLIC, metallic), (ROUGHNESS, roughness),
                          (EMISSION, emission), (NORMAL, normal), (AO, ao), (DEPTH, depth),
                          (OPACITY, opacity)):
            if src is not None:
                self.link(src, name, port)
        return name

    # --- convenience wrappers -------------------------------------------------
    def colorize(self, src, gradient):
        return self.node("colorize", src, gradient=gradient)

    def blend(self, top, bottom, mask=None, mode=0, amount=1.0):
        # Material Maker blend: s1 (top) over s2 (bottom) using mask * amount.
        return self.node("blend", top, bottom, mask, blend_type=mode, amount=amount)

    def math(self, a, b=None, op=0, in1=0.0, in2=0.0, clamp=False):
        return self.node("math", a, b, op=op, default_in1=in1, default_in2=in2, clamp=clamp)

    def normal_map(self, height, strength=1.0):
        return self.node("normal_map", height, param0=11, param1=strength, param2=0, param4=1)

    def occlusion(self, height, strength=1.5, radius=20):
        return self.node("occlusion2", height, param0=11, param1=radius, param2=strength, param3=1)

    def tone(self, src, *pts):
        return self.node("tonality", src, curve=curve(*pts))

    def grey(self, v):
        return self.node("uniform_greyscale", color=v)

    def color(self, c):
        if isinstance(c, str):
            c = hexc(c)
        return self.node("uniform", color=rgb(*c))

    def to_json(self):
        return {
            "name": "Material", "label": self.label, "type": "graph",
            "node_position": {"x": 0, "y": 0},
            "parameters": {}, "connections": self.conns, "nodes": self.nodes,
        }


# Math op indices (see Material Maker math node)
ADD, SUB, MUL, DIV = 0, 1, 2, 3
POW, ABS = 6, 7
MIN, MAX = 13, 14
LT = 15
SMOOTH = 20

# Blend modes
NORMAL_B, DISSOLVE, MULTIPLY, SCREEN, OVERLAY, HARD_LIGHT, SOFT_LIGHT = range(7)
BURN, DODGE, LIGHTEN, DARKEN, DIFFERENCE, ADDITIVE = range(7, 13)



def shader(g, label, inputs, instance, outputs):
    """An inline shader node (Material Maker's `shader` type, as a custom node saved in a graph).

    inputs: (name, type, source) with type "f", "rgb" or "rgba" and source a node name or (node, port); the GLSL calls
    `$name(uv)`. instance: Godot shading language with `$name` suffixes on its functions. outputs: (type, expression)
    in `$uv`. Returns the node's name (port i is outputs[i])."""
    # Every input is declared a function ("function": true), so the shader's own functions can call it anywhere (at
    # offsets, for the lighting and the rays); otherwise Material Maker hoists `$input(...)` into the output's code.
    name = f"shader_{len(g.nodes)}"
    g.col += 1
    g.nodes.append({
        "name": name, "type": "shader", "node_position": {"x": 220 * (g.col % 8), "y": 160 * (g.col // 8)},
        "parameters": {}, "seed_int": 0,
        "shader_model": {
            "code": "", "global": "", "instance": instance, "name": label, "shortdesc": label, "longdesc": label,
            "parameters": [],
            "inputs": [{"name": n, "type": t, "default": "0.0" if t == "f" else "vec4(0.0)" if t == "rgba" else "vec3(0.0)",
                        "function": True, "label": n, "shortdesc": n, "longdesc": n} for n, t, _ in inputs],
            "outputs": [{"type": t, t: e, "shortdesc": f"out{i}", "longdesc": f"out{i}"} for i, (t, e) in enumerate(outputs)],
        },
    })
    for port, (_, _, src) in enumerate(inputs):
        g.link(src, name, port)
    return name


def fbm(g, scale, iterations=5, persistence=0.55, offset=0.0, noise=2, sy=None):
    return g.node("fbm2", noise=noise, scale_x=scale, scale_y=sy or scale, folds=0, iterations=iterations,
                  persistence=persistence, offset=offset)


HASH = "float h_$name(float x) { return fract(sin(x * 12.9898 + 0.731) * 43758.5453); }\n"


# The normal maps are drawn at true depth and then lifted by this much, so relief reads at a distance.
EXAGGERATION = 1.5
# Material Maker's occlusion node is gentle at these radii; this deepens it so seams and gaps read in the ORM red.
AO_GAIN = 2.2

# Helpers every finished shader shares: a hash of a cell id (Dave Hoskins' hash without sine), so per-panel
# variation repeats with the tile, and a clamp.
PRELUDE = """
float hs_$name(vec2 p) { vec3 p3 = fract(vec3(p.xyx) * 0.1031); p3 += dot(p3, p3.yzx + 33.33); return fract((p3.x + p3.y) * p3.z); }
float sat_$name(float x) { return clamp(x, 0.0, 1.0); }
"""


def vor(g, scale, randomness=1.0, intensity=1.0):
    """Material Maker's voronoi node, whole-number scale so it tiles. Ports: 0 nodes, 1 borders, 2 random colour."""
    return g.node("voronoi", scale_x=scale, scale_y=scale, stretch_x=1, stretch_y=1, intensity=intensity,
                  randomness=randomness)


def finish(g, label, inputs, body, tile_m, depth_m, ao_radius=16, ao_strength=1.2):
    """Wire the shader node (height, albedo, roughness, metal) to the normal, occlusion and material nodes.

    body must define height_$name, albedo_$name (vec3, sRGB), rough_$name and metal_$name, each taking the uv."""
    s = shader(g, label, inputs, PRELUDE + body,
               [("f", "sat_$name(height_$name($uv))"), ("rgba", "vec4(clamp(albedo_$name($uv), vec3(0.0), vec3(1.0)), 1.0)"),
                ("f", "sat_$name(rough_$name($uv))"), ("f", "sat_$name(metal_$name($uv))")])
    normal = g.normal_map((s, 0), strength=round(EXAGGERATION * 16.0 * depth_m / tile_m, 4))
    ao = g.occlusion((s, 0), strength=round(AO_GAIN * ao_strength, 3), radius=ao_radius)
    g.material(albedo=(s, 1), roughness=(s, 2), metallic=(s, 3), normal=normal, ao=ao, depth=(s, 0))
    return g
