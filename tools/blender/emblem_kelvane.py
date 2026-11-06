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

The relief is pressed into the guide's device band, but every vertex keeps the full orbital's normal, so the plot is lit
and shaded as the 3D orbital is: lobes and ring rounding away from the key light, the ring's shadow side dark.

Materials:
    champagne     the rim: the platinum graph tinted the Institute's champagne (its instruments' plate)
    scope glass   the field: the pearl enamel graph tinted the console's smoked glass, under a hard clear coat
    brass         the graticule on the field and the wireframe on the orbital (the console's bezels and crest wire)
    amber enamel  the orbital's surface: the reading lit amber, under a soft clear coat, with a faint glow of its own

Heights (mm), no two faces sharing a plane:
    field       0.3 to 1.0
    graticule   1.0 to 1.15   two rings and a cross, engraved standing proud by a hair
    orbital     1.05 to 4.0   the relief (the guide's device band); its farthest point clears the field by 0.05
    rim         0.0 to 5.0    the frame, highest

    /opt/blender/blender -b -P tools/blender/emblem_kelvane.py -- [--out DIR] [--samples N] [--px N] [--steps]

Writes <out>/kelvane/ka-face.png (face on, orthographic) and ka-angle.png (three-quarter, perspective); with --steps, the
clay blockout ka-clay.png too.
"""
import json
import os
import sys

import bpy

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import emblem_render as R  # noqa: E402

HERE = os.path.join(R.ROOT, "assets", "emblems", "kelvane")
ORBITAL = os.path.join(HERE, "ka.orbital.json")
GRID = os.path.join(HERE, "ka.grid.png")
CHAMPAGNE = "#d8cbab"   # the instruments' plate, on the platinum graph (white), so a little lighter than #c9bfa9
GLASS = "#1b150d"       # the console's smoked glass
BRASS = "#b0915a"
AMBER = "#f2d08c"       # the 2D logo's lit tone, a step lighter: enamel under a coat reads darker than flat ink
RIM_IN, RIM_OUT = 44.0, 48.0


def orbital_material():
    """Amber enamel with the wireframe inlaid in brass: one surface, the grid mask choosing between the two."""
    m = bpy.data.materials.new("ka.orbital")
    m.use_nodes = True
    nt = m.node_tree
    nt.nodes.clear()
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    enamel = nt.nodes.new("ShaderNodeBsdfPrincipled")
    enamel.inputs["Base Color"].default_value = R.hex_rgb(AMBER)
    enamel.inputs["Roughness"].default_value = 0.34
    # a faint glow of its own, a reading lit from behind as on the console's windows, so the shadow side reads amber
    enamel.inputs["Emission Color"].default_value = R.hex_rgb(AMBER)
    enamel.inputs["Emission Strength"].default_value = 0.12
    enamel.inputs["Coat Weight"].default_value = 0.35
    enamel.inputs["Coat Roughness"].default_value = 0.08
    brass = nt.nodes.new("ShaderNodeBsdfPrincipled")
    brass.inputs["Base Color"].default_value = R.hex_rgb(BRASS)
    brass.inputs["Metallic"].default_value = 1.0
    brass.inputs["Roughness"].default_value = 0.28
    uv = nt.nodes.new("ShaderNodeUVMap")
    mask = nt.nodes.new("ShaderNodeTexImage")
    mask.image = bpy.data.images.load(GRID)
    mask.image.colorspace_settings.name = "Non-Color"
    mask.interpolation = "Cubic"
    mix = nt.nodes.new("ShaderNodeMixShader")
    nt.links.new(uv.outputs["UV"], mask.inputs["Vector"])
    nt.links.new(mask.outputs["Color"], mix.inputs["Fac"])
    nt.links.new(enamel.outputs["BSDF"], mix.inputs[1])
    nt.links.new(brass.outputs["BSDF"], mix.inputs[2])
    nt.links.new(mix.outputs["Shader"], out.inputs["Surface"])
    return m


def orbital(material):
    """The relief from the generator's mesh, smooth shaded on the full orbital's normals (custom normals)."""
    d = json.load(open(ORBITAL))
    P, N, U, F = d["positions"], d["normals"], d["uvs"], d["faces"]
    verts = [(P[i] * R.MM, P[i + 1] * R.MM, P[i + 2] * R.MM) for i in range(0, len(P), 3)]
    faces = [tuple(F[i:i + 3]) for i in range(0, len(F), 3)]
    me = bpy.data.meshes.new("ka.orbital")
    me.from_pydata(verts, [], faces)
    me.validate(clean_customdata=False)
    uvl = me.uv_layers.new(name="UVMap")
    for poly in me.polygons:
        poly.use_smooth = True
        for li in poly.loop_indices:
            v = me.loops[li].vertex_index
            uvl.data[li].uv = (U[2 * v], 1.0 - U[2 * v + 1])
    me.normals_split_custom_set_from_vertices([(N[i], N[i + 1], N[i + 2]) for i in range(0, len(N), 3)])
    ob = bpy.data.objects.new("ka.orbital", me)
    bpy.context.scene.collection.objects.link(ob)
    me.materials.append(material)
    return ob


def build():
    champagne = R.mm_material("emblem_platinum", tint=CHAMPAGNE)
    glass = R.mm_material("emblem_enamel_pearl", coat=1.0, tint=GLASS)
    brass = R.mm_material("emblem_platinum", tint=BRASS)
    R.ring("rim", 0.0, 0.0, RIM_IN, RIM_OUT, 0.0, 5.0, 0.8, champagne)
    R.disc("field", 0.0, 0.0, RIM_IN + 0.4, 0.3, 1.0, 0.15, glass)
    # the graticule: two rings and a cross, a scope's measuring lines, on the glass under the plot
    for k, r in enumerate((22.0, 36.0)):
        R.ring(f"graticule.ring.{k}", 0.0, 0.0, r - 0.25, r + 0.25, 1.0, 1.15, 0.05, brass)
    for k, rect in enumerate((((50 - 41, 49.8), (50 + 41, 49.8), (50 + 41, 50.2), (50 - 41, 50.2)),
                              ((49.8, 50 - 41), (50.2, 50 - 41), (50.2, 50 + 41), (49.8, 50 + 41)))):
        R.poly_slab(f"graticule.cross.{k}", [list(rect)], 1.0, 1.15, 0.05, brass)
    orbital(orbital_material())


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
