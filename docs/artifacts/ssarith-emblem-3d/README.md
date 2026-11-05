# Ssarith Badge 3D (mockup 19)

- Link: https://claude.ai/artifact/NLspB15dVMS7dhnbM8PvCB
- Shows: the Ssarith Hunt's emblem SD (the Bow Rites) as a badge 100 mm across, orbitable: three laminated wooden bows,
  overlapping and aimed right, each a step prouder than the one behind it; arrows in Watch green lacquer, each head its own
  shape (the learner's leaf, the hunter's barb, the elder's tally) under dried blood; a dry forest of beige, dull brown and
  dry green in the field with its canopy hanging over it as holographic leaves at three depths; a bone-pale metal rim; the
  Blender renders beside it.
- Made by (since v2: ferns and blood from `tools/emblems/ssarith_ferns.py`, see Publishes): `docs/mockups/ssarith-emblem-3d.html` on the shared badge library `docs/mockups/lib/emblem-badge.js`, with the
  parts from `assets/emblems/ssarith/sd.parts.json` (`tools/emblems/export_parts.mjs ssarith`), the leaves from
  `assets/emblems/ssarith/sd.leaves.json` (`tools/emblems/ssarith_leaves.py`), the Material Maker maps listed in
  `docs/mockups/lib/emblem-materials-ssarith.json` (new graphs `emblem_wood_laminate`, `emblem_lacquer_watch`,
  `emblem_blood_watch`, `emblem_enamel_forest` in `tools/materials/build_ptex.py`), and the Blender renders listed in
  `assets/emblems/ssarith/sd.renders.json` (`tools/blender/emblem_ssarith.py`, 96 samples, 1200 px), inlined by
  `tools/mockups/inline_data.py`.

## Publishes

- 2026-11-05, version 1 (`1793839261-17d1`): first publish (owner: "shade the bow parts with like wood and the arrows green
  with blood", "Background should be like a beige, dull brown and dry green forest", "make each arrowhead unique", "with
  like holographic leaves", "Should be 3d leaves shapes").
- 2026-11-05, version 2 (`1793841959-133b`): to the owner's photograph of ferns ("3d model Fern leaves. Arrow made of wood.
  Arrow head made of steal. Feathera red with orange tip. Arrows dripping with bright glowing green blood", "Fern leaves are
  the holographic background jungle"): 60 modelled fern fronds drawn as a hologram over the darkened forest
  (`tools/emblems/ssarith_ferns.py`, `assets/emblems/ssarith/sd.ferns.json`, replacing the leaves), wooden shafts, steel
  heads, red feathers with orange tips, and glowing green blood pooled on each head and dripping down the face. The green
  lacquer, blood and fern graphs are gone; the full Blender render beside it.
- 2026-11-05, version 3 (`1793846054-a212`): owner: "z fighting, the blood doesnt look right, remove blood, and just make
  arrow hard green and arrow body metal", "redo the fern, render a fern forrest sccene. Make THAT exact scene!", "no
  hologram, just make a scene that looks like that as background for the emblem, and it should be 3d". The badge is a deep
  frame: the rim to 10.4 mm, the field a dark floor 9 mm below it, filled with 40 modelled bipinnate ferns in layers, the deep
  ones in shadow (`tools/emblems/ssarith_ferns.py`, our own scene after the owner's photograph, which is not committed); the
  bows float over them as thin parts; steel shafts, hard green heads, no blood; the feathers' body and tip meet edge to edge
  (the z-fighting was the tip laid over the body). The fern-leaf graph is back; the holographic jungle is gone.
