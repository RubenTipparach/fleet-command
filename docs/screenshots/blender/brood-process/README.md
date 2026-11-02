# How a Vhorr Brood piece is made, in Blender

The owner asked on 2026-11-01, through the cloud session: "Can you order the Mac to take some screenshots of the blender process for making UI stuff in the brood style?"

These are screenshots of Blender 4.5.4 with its interface open, on the Mac, building one Brood piece: the weapons panel (`panel.weapons`, 460 x 174 px). It is a frame in style "wrap", with two larva keys (FIRE, MULTIPLE) and three low wart tabs (OFFLINE, NORMAL, OVERLOAD), from survey W.

`tools/blender/brood_process_shots.py` makes them. It runs the kit's own code: the layout, the sculptors, the materials, the lights and the render settings in `tools/blender/build_ui_vhorr_organic.py` and `tools/blender/organic_components.py`. It keeps each layer of the sculpt as its own mesh, so the outliner shows the history, and it captures Blender's window after each stage. Only one part is added for display: the node tree is laid out in columns, because the generator leaves every node at the origin. The material itself is unchanged.

```sh
/Applications/Blender.app/Contents/MacOS/Blender --factory-startup -p 0 0 1728 1080 <an empty .blend> \
    --python tools/blender/brood_process_shots.py -- --out docs/screenshots/blender/brood-process
```

Passing a saved empty .blend skips Blender's splash screen. The shots are saved at half the Retina size (1669 x 1051) and reduced to 256 colours by `tools/mockups/shrink_png.py`.

Note: the Brood is not built with modifiers or Geometry Nodes. Each piece is a height field computed in Python (NumPy) and made into one mesh by `organic_study.field_mesh`. So the modifier stack is empty, and the only node tree is the material's.

| Shot | What it shows | Made by |
|---|---|---|
| `01-blockout.png` | The placeholder blockout at slot size, seen through the render camera. Grey: the panel. Orange: the two keys. Green: the three tabs. Red: protected rectangles (labels and key faces) that no growth may cover. | `build_ui_vhorr_organic.load_layout` (rectangles pinned to whole pixels) from `assets/ui/vhorr/organic/blockout.json`; `bo.scene` sets the camera and lights round the slot and its 34 mm bleed |
| `02-bone.png` | Bone: the armature raised first, a rib floor, spines and the larva keys' plateaus (the field's `B` layer). | `organic_components.panel`, `key`, `tentacle_styles` via `build_ui_vhorr_organic.sculpt_slot` |
| `03-muscle.png` | Muscle over the bone: body plateaus, the tentacles of style "wrap" and the larva bodies (`max(B, M)`). | the same sculptors (`Field.body`, `ridge`, `ring`, `tentacle`) |
| `04-skin.png` | One leather skin stretched over both: drape, blur, dense anatomy, creases, and the openings for glass and keys, before veins. | `Field.finish(veins=False)` |
| `05-veins-and-sacs.png` | Veins raised under the skin (red) and the sacs of light (in their own light colour; this panel has only one small sac). | `Field.finish` (veins), `Field.pust`, `Field.glowc` |
| `06-material-masks.png` | The masks the sculpt writes for the material, colour attribute `m1`: red bone, green muscle, blue vein (`m2` carries crease, web, pustule and depth). | `organic_study.field_mesh` |
| `07-node-tree.png` | The final sculpt in Material Preview with the kit's own materials and instruments (smoked glass, bronze rims, key caps with lit rims), with the anatomy material's node tree below. | `build_ui_vhorr_organic.materials` (`organic_study.anatomy`, step 26), `mesh`, `place` |
| `08-node-tree-close-up.png` | The whole node tree (top) and a close-up of its output end (bottom): a wave texture and the math that shapes it, the Mix into the Principled BSDF, and the Material Output. | `organic_study.anatomy` |
| `09-lights-and-camera.png` | The light rig from outside (top): a warm area key from above, two grazing rims and a dim cool fill. Below: the view through the orthographic camera. The fill light's settings are in the properties panel. | `organic_study.light_rig` via `build_ui_vhorr_organic.scene` |
| `10-cycles-viewport.png` | Cycles on the Metal GPU through the camera (bottom), 24 samples, the kit's materials under the kit's lights. | render settings from `organic_study.reset` |
| `11-final-render.png` | The final render (2 px per mm, transparent film) in the Image Editor, above the approved Brood console (`docs/screenshots/mockups/console-skins-vhorr-desktop.png`). The weapons panel sits at the console's lower left. | `bpy.ops.render.render` with the kit's settings |
| `render-panel.weapons.png` | The render itself, 1056 x 484 px (the slot and its bleed). | the same render |
