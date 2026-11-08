#!/usr/bin/env python3
"""The low-poly hull a ship's texture is baked onto, from the game's own model (change hull-texture-designs).

The Concord and Krai lines are exported from their mockups by tools/mockups/export_ships.mjs (CLAUDE.md section 14); their
glbs are the one copy of each hull. painted_bake.py --ship bakes a ship's detail onto a low-poly hull read from
tools/materials/raw/painted/ships/<slug>.obj, as the Ssarith Hunt's blockouts are written by export_blockouts.mjs. This
writes that OBJ from a glb: the meshes whose material is named in --mats, each node's transform applied, turned into
mockup ship space (+x port, +y up, +z bow, metres: the glb's half turn about Y undone, its re-centring added back from the
export manifest), one object a piece. Plain Python, no Blender; the OBJ is a build intermediate (gitignored).

    python3 tools/mockups/glb_hull_obj.py assets/ships/concord/petrel_level.glb petrel paint_hull_2,paint_plain,paint_nacelle 1

With --parts in place of the slug, it prints where each piece of the named materials sits instead (JSON: name, material
and its box in the same ship space), so the bake's designed pieces can be placed where the model's flat parts are:

    python3 tools/mockups/glb_hull_obj.py assets/ships/concord/swift_level.glb --parts glow_dish,glow_torpedo,glow_grille_2
"""
import json
import math
import pathlib
import struct
import sys

ROOT = pathlib.Path(__file__).resolve().parents[2]
OUT = ROOT / "tools" / "materials" / "raw" / "painted" / "ships"
COMP = {5120: ("b", 1), 5121: ("B", 1), 5122: ("h", 2), 5123: ("H", 2), 5125: ("I", 4), 5126: ("f", 4)}
NCOMP = {"SCALAR": 1, "VEC2": 2, "VEC3": 3, "VEC4": 4}


def read_glb(path):
    b = path.read_bytes()
    jl = struct.unpack("<I", b[12:16])[0]
    g = json.loads(b[20:20 + jl])
    off = 20 + jl
    bl = struct.unpack("<I", b[off:off + 4])[0]
    return g, b[off + 8:off + 8 + bl]


def accessor(g, bin_, i):
    a = g["accessors"][i]
    bv = g["bufferViews"][a["bufferView"]]
    fmt, size = COMP[a["componentType"]]
    n = NCOMP[a["type"]]
    stride = bv.get("byteStride", size * n)
    base = bv.get("byteOffset", 0) + a.get("byteOffset", 0)
    return [struct.unpack_from("<" + fmt * n, bin_, base + k * stride) for k in range(a["count"])]


def mat4(nd):
    """A node's local matrix (row-major 4 x 4 as nested lists)."""
    if "matrix" in nd:
        m = nd["matrix"]
        return [[m[c * 4 + r] for c in range(4)] for r in range(4)]
    t = nd.get("translation", [0, 0, 0]); s = nd.get("scale", [1, 1, 1]); x, y, z, w = nd.get("rotation", [0, 0, 0, 1])
    R = [[1 - 2 * (y * y + z * z), 2 * (x * y - z * w), 2 * (x * z + y * w)],
         [2 * (x * y + z * w), 1 - 2 * (x * x + z * z), 2 * (y * z - x * w)],
         [2 * (x * z - y * w), 2 * (y * z + x * w), 1 - 2 * (x * x + y * y)]]
    return [[R[r][0] * s[0], R[r][1] * s[1], R[r][2] * s[2], t[r]] for r in range(3)] + [[0, 0, 0, 1]]


def mul(A, B):
    return [[sum(A[r][k] * B[k][c] for k in range(4)) for c in range(4)] for r in range(4)]


def main():
    glb, slug, mats = pathlib.Path(sys.argv[1]), sys.argv[2], set(sys.argv[3].split(","))
    if not glb.is_absolute():
        glb = ROOT / glb
    g, bin_ = read_glb(glb)
    names = [m.get("name", "") for m in g["materials"]]
    rel = str(glb.relative_to(ROOT / "assets" / "ships"))
    entry = next(s for s in json.loads((ROOT / "assets/ships/manifest.json").read_text())["ships"] if s["file"] == rel)
    off = entry["origin_offset_m"]
    parent = {c: i for i, nd in enumerate(g["nodes"]) for c in nd.get("children", [])}

    def world(i):
        M = mat4(g["nodes"][i])
        while i in parent:
            i = parent[i]
            M = mul(mat4(g["nodes"][i]), M)
        return M
    n = sys.argv[4] if len(sys.argv) > 4 else "0"   # painted_bake.py reads the first line's number as the page's ship number
    if slug == "--parts":   # where the named materials' pieces sit, not an OBJ
        out = []
        for i, nd in enumerate(g["nodes"]):
            if "mesh" not in nd:
                continue
            M = world(i)
            for pr in g["meshes"][nd["mesh"]]["primitives"]:
                if names[pr.get("material", 0)] not in mats:
                    continue
                pts = [[sum(M[r][c] * (list(p) + [1])[c] for c in range(4)) for r in range(3)] for p in accessor(g, bin_, pr["attributes"]["POSITION"])]
                pts = [(-x + off[0], y + off[1], -z + off[2]) for x, y, z in pts]
                out.append({"name": nd.get("name"), "material": names[pr.get("material", 0)],
                            "min": [round(min(p[k] for p in pts), 3) for k in range(3)], "max": [round(max(p[k] for p in pts), 3) for k in range(3)]})
        print(json.dumps(out))
        return
    lines = [f"# {n} {entry.get('label') or slug}, ship space (+x port, +y up, +z bow), from {rel} (tools/mockups/glb_hull_obj.py)"]
    base, tris = 1, 0
    for i, nd in enumerate(g["nodes"]):
        if "mesh" not in nd:
            continue
        M = world(i)
        for k, pr in enumerate(g["meshes"][nd["mesh"]]["primitives"]):
            if names[pr.get("material", 0)] not in mats:
                continue
            pos = accessor(g, bin_, pr["attributes"]["POSITION"])
            idx = [v[0] for v in accessor(g, bin_, pr["indices"])] if "indices" in pr else list(range(len(pos)))
            lines.append(f"o {nd.get('name', 'part')}_{k}")
            for p in pos:
                x, y, z = (sum(M[r][c] * (list(p) + [1])[c] for c in range(4)) for r in range(3))
                lines.append(f"v {-x + off[0]:.4f} {y + off[1]:.4f} {-z + off[2]:.4f}")   # the glb's half turn undone
            for t in range(0, len(idx), 3):
                a, b2, c = (base + idx[t + j] for j in range(3))
                lines.append(f"f {a} {b2} {c}")   # a half turn keeps handedness, so the winding is the glb's
                tris += 1
            base += len(pos)
    OUT.mkdir(parents=True, exist_ok=True)
    (OUT / f"{slug}.obj").write_text("\n".join(lines) + "\n")
    print(f"wrote {OUT / (slug + '.obj')}: {tris} triangles")


if __name__ == "__main__":
    main()
