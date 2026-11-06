"""Fleet Command: the Kelvane Institute's emblem KA, the hydrogen eigen plot, rendered as a 3D badge
(openspec/changes/faction-emblems; openspec/changes/kelvane-battle-console D9; the Emblem Design Guide). The owner,
2026-11-06, with a book cover's hydrogen-atom plot: "I would like this hydrogen atom eigen plot used as the logo/emblem",
then "shade the 3d plot in the 3d render of the emblem, use our emblem designer for this please".

The emblem is computed, not traced: tools/mockups/kelvane_emblem.py writes the orbital (hydrogen's 3d z-squared state,
the surface at |3 cos^2 theta - 1|) as this badge's relief, assets/emblems/kelvane/ka.orbital.json (vertices in badge
mm, the true surface's normals, UVs on longitude and colatitude), and its wireframe as a mask over those UVs,
ka.grid.png: the same lines the 2D logo and the console crest draw. The shared rig (lights, studio, shadow catcher,
Material Maker map loader, rings and slabs) is tools/blender/emblem_render.py; this file owns the parts, their heights and
materials.

The coin (owner, 2026-11-06: "green hydrogen icon, irrdescent and red satin soft fabric background on an oval emblem
coin"): an upright oval in a champagne rim; inside it red satin draped in soft folds (assets/emblems/kelvane/ka.satin.json,
the same generator's height field: a few broad folds and smaller ones between, gathered toward the bottom, tucked flat
under the rim); over the satin the orbital in iridescent green, a thin film whose colour runs with the viewing angle,
its wireframe in light green lines (KID's colours, the Emblem Design Guide). The relief keeps the full orbital's normals, so it is lit and shaded as the 3D orbital is,
and it floats above the satin's crests, shadowing the cloth.

Materials:
    champagne     the rim: the platinum graph tinted the Institute's champagne (its instruments' plate)
    red satin     the field: red, a soft sheen at grazing angles and a highlight drawn out along the weave (anisotropic)
    green film    the orbital: green under a hard coat with a thin film (iridescent), its wireframe light green, faintly lit

Heights (mm), no two faces sharing a plane (the generator's numbers):
    satin       0.6 to 2.0    its floor where it is tucked under the rim, its highest crest
    orbital     2.4 to 5.4    the relief, its farthest point 0.4 above the highest crest
    rim         0.0 to 6.2    the frame, highest

    /opt/blender/blender -b -P tools/blender/emblem_kelvane.py -- [--out DIR] [--samples N] [--px N] [--steps]

Writes <out>/kelvane/ka-face.png (face on, orthographic) and ka-angle.png (three-quarter, perspective); with --steps, the
clay blockout ka-clay.png too.
"""
import json
import math
import os
import sys

import bpy

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import emblem_render as R  # noqa: E402

HERE = os.path.join(R.ROOT, "assets", "emblems", "kelvane")
ORBITAL = os.path.join(HERE, "ka.orbital.json")
SATIN = os.path.join(HERE, "ka.satin.json")
GRID = os.path.join(HERE, "ka.grid.png")
# The colours are the emblem's one palette (tools/mockups/kelvane_emblem.py's PALETTE, KID's on the Emblem Design Guide;
# owner: "on the emblem, background red, shape green"), carried in the satin's file.
PAL = json.load(open(SATIN))["palette"]
CHAMPAGNE = PAL["frame"]
SATIN_RED = PAL["field"]
SHEEN = PAL["sheen"]
GREEN = PAL["lit"]
LINE = PAL["wire"]      # the wireframe, light green on the green, as KID's wires


def node_mesh(path, name):
    """A mesh from one of the generator's files (positions in badge mm, normals, UVs, faces), smooth shaded on the
    normals the generator computed (custom normals)."""
    d = json.load(open(path))
    P, N, U, F = d["positions"], d["normals"], d["uvs"], d["faces"]
    verts = [(P[i] * R.MM, P[i + 1] * R.MM, P[i + 2] * R.MM) for i in range(0, len(P), 3)]
    me = bpy.data.meshes.new(name)
    me.from_pydata(verts, [], [tuple(F[i:i + 3]) for i in range(0, len(F), 3)])
    me.validate(clean_customdata=False)
    uvl = me.uv_layers.new(name="UVMap")
    for poly in me.polygons:
        poly.use_smooth = True
        for li in poly.loop_indices:
            v = me.loops[li].vertex_index
            uvl.data[li].uv = (U[2 * v], 1.0 - U[2 * v + 1])
    me.normals_split_custom_set_from_vertices([(N[i], N[i + 1], N[i + 2]) for i in range(0, len(N), 3)])
    ob = bpy.data.objects.new(name, me)
    bpy.context.scene.collection.objects.link(ob)
    return ob, d


def satin_material():
    """Red satin: a soft sheen at grazing angles and a highlight drawn out along the weave (anisotropic, its tangent
    round the oval, the generator's u)."""
    m = bpy.data.materials.new("ka.satin")
    m.use_nodes = True
    nt = m.node_tree
    p = nt.nodes["Principled BSDF"]
    p.inputs["Base Color"].default_value = R.hex_rgb(SATIN_RED)
    p.inputs["Roughness"].default_value = 0.3
    p.inputs["Anisotropic"].default_value = 0.85
    p.inputs["Sheen Weight"].default_value = 0.9
    p.inputs["Sheen Roughness"].default_value = 0.35
    p.inputs["Sheen Tint"].default_value = R.hex_rgb(SHEEN)
    p.inputs["Specular IOR Level"].default_value = 0.6
    tg = nt.nodes.new("ShaderNodeTangent")
    tg.direction_type = "UV_MAP"
    tg.uv_map = "UVMap"
    nt.links.new(tg.outputs["Tangent"], p.inputs["Tangent"])
    return m


def orbital_material():
    """Iridescent green under a hard coat, a thin film whose colour runs with the viewing angle, with the wireframe in a
    dark green-black: one surface, the grid mask choosing between the two."""
    m = bpy.data.materials.new("ka.orbital")
    m.use_nodes = True
    nt = m.node_tree
    nt.nodes.clear()
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    film = nt.nodes.new("ShaderNodeBsdfPrincipled")
    film.inputs["Base Color"].default_value = R.hex_rgb(GREEN)
    film.inputs["Metallic"].default_value = 0.25
    film.inputs["Roughness"].default_value = 0.22
    film.inputs["Coat Weight"].default_value = 1.0
    film.inputs["Coat Roughness"].default_value = 0.04
    film.inputs["Thin Film Thickness"].default_value = 520.0
    film.inputs["Thin Film IOR"].default_value = 1.7
    line = nt.nodes.new("ShaderNodeBsdfPrincipled")
    line.inputs["Base Color"].default_value = R.hex_rgb(LINE)
    line.inputs["Roughness"].default_value = 0.35
    line.inputs["Emission Color"].default_value = R.hex_rgb(LINE)   # a little light of its own, a trace on a scope
    line.inputs["Emission Strength"].default_value = 0.25
    line.inputs["Coat Weight"].default_value = 1.0
    line.inputs["Coat Roughness"].default_value = 0.04
    uv = nt.nodes.new("ShaderNodeUVMap")
    mask = nt.nodes.new("ShaderNodeTexImage")
    mask.image = bpy.data.images.load(GRID)
    mask.image.colorspace_settings.name = "Non-Color"
    mask.interpolation = "Cubic"
    mix = nt.nodes.new("ShaderNodeMixShader")
    nt.links.new(uv.outputs["UV"], mask.inputs["Vector"])
    nt.links.new(mask.outputs["Color"], mix.inputs["Fac"])
    nt.links.new(film.outputs["BSDF"], mix.inputs[1])
    nt.links.new(line.outputs["BSDF"], mix.inputs[2])
    nt.links.new(mix.outputs["Shader"], out.inputs["Surface"])
    return m


def ellipse(a, b, n=180):
    """An ellipse of semi-axes a, b mm as a polygon on the guide's 100 unit grid (y down)."""
    return [(50 + a * math.cos(2 * math.pi * k / n), 50 - b * math.sin(2 * math.pi * k / n)) for k in range(n)]


def build():
    champagne = R.mm_material("emblem_platinum", tint=CHAMPAGNE)
    satin, d = node_mesh(SATIN, "ka.satin")
    satin.data.materials.append(satin_material())
    (ao, bo), (ai, bi) = d["oval_out"], d["oval_in"]
    R.poly_slab("rim", [ellipse(ao, bo), ellipse(ai, bi)], 0.0, d["rim_z"], 0.8, champagne)
    orbital, _ = node_mesh(ORBITAL, "ka.orbital")
    orbital.data.materials.append(orbital_material())


def main():
    a = R.args()
    out = os.path.join(a["out"], "kelvane")
    R.reset()
    R.setup(px=a["px"], samples=a["samples"])
    build()
    if a["steps"]:
        restore = R.with_material(R.clay())
        R.render(os.path.join(out, "ka-clay.png"))
        restore()
    R.render(os.path.join(out, "ka-face.png"))
    bpy.context.scene.camera = R.angle_camera()
    R.render(os.path.join(out, "ka-angle.png"))


main()
