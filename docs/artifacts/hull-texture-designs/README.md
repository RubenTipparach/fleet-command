# Hull Texture Designs (mockup 24)

- Link: https://claude.ai/artifact/GyVMZ9s6hxwbDhmeBC9Fqu
- Source: `docs/mockups/hull-texture-designs.html` (one copy, not duplicated here); the Petrel and the Krai frigate are
  inlined from `assets/ships/concord/petrel_level.glb` and `assets/ships/krai/frigate.glb` by `tools/mockups/inline_data.py`.
- OpenSpec change: `openspec/changes/hull-texture-designs/`.
- Shots: `docs/screenshots/mockups/hull-texture-designs/` (`tools/mockups/shoot.mjs`), at desktop, phone landscape and
  portrait.

## Versions

- 2026-11-06, cloud session (owner: "Pitch a few different texture designs. Start with frigate of each and propose4-5
  different texturing approaches for each. Goal is to follow culture and ideology"): version 1, version id
  `1793959368-3126`. Five Concord designs (C1 Survey white, C2 Many yards, C3 Envoy sash, C4 Lightwork, C5 Home Guard)
  and five Krai designs (K1 Tempered blade, K2 House banner, K3 Blood record, K4 Blackened war plate, K5 Kept plate),
  each drawn live in the shader over the game's own model, one at a time or all five side by side, studio and night.
- 2026-11-06, cloud session: version 2, version id `1793960466-acf6`. In the all-five view the first row's labels sit
  below the header (on a phone in portrait they overlapped it). Every shot taken at desktop, phone landscape and portrait.
- 2026-11-06, cloud session (owner, of v2: "None of this look good, they don't bring the same level as the ssarith ships.
  We need visual story telling no repetitive patterns"): version 3, version id `1793988972-414a`. Every design modelled and
  baked as the Hunt's ships are: the game's own hull unwrapped, a detail layer of armour plates cut by the faction's own
  style (the Concord's strakes, the Krai's split-and-split-again kit with cut corners), the ship's story placed from its
  room plan (windows where people live, sensors, vents, bays) and worn into the paint (soot, scorch, scuffs), each design
  its own plating, palette, liveries and wear (`data/ships/<faction>/hull_detail.json`). Page 11.7 MB.
- 2026-11-06, cloud session: version 4, version id `1793989317-abb4`. Renumbered mockup 24 (another session's Kelvane
  Institute ships took mockup 23 first).
- 2026-11-07, cloud session: version 5, version id `1794022545-6d92`. The Concord's C1 rebuilt on the Kelvane Institute's
  hull method (owner: "exampline how the ionstitute does its hull, and replicate taht", "lets try a new dirtection"): grid
  plating with diagonal corners, painted plate marks, light seams, framed hatches, the Concord's colours and teal bands,
  2048 px; the Krai still on version 4 until K1 lands; the other designs shown as to come. Page 7.8 MB.
- 2026-11-07, cloud session: version 6, version id `1794023316-8d53`. The Krai's K1 on the Institute's method too (plates
  square to the hull with their corners cut, the Warhost's steels, crimson and bone, light seams, the KC emblem), 2048 px;
  the old K2-K5 maps out of the manifest. Page 4.1 MB.
- 2026-11-07, cloud session: version 7, version id `1794028003-dbe9`. K1 in staggered rows of plates (about twice as many;
  owner: "a different panneling design on the hull too, with 2x number of panels"), its darks lighter, no inner lines on
  the plates (both ships; C1 repainted).
- 2026-11-07, cloud session: version 8, version id `1794029978-4709`. C1 in staggered rows of plates too; no windows on
  the shuttle bay's mouth or the nacelles; the cooling fans clear of the game model's radiators; both ships' seams and
  rim shadows lighter (the paint's cavity floor, `ao_floor`, now the faction's own).
- 2026-11-07, cloud session: version 9, version id `1794032682-b2d0`. C1 with half the panels (the Krai keeps the dense
  rows), thin plates and narrow light grey seams, plates in eight tones, and its windows from the room plan again (44
  windows and bays, as the original).
- 2026-11-07, cloud session: version 10, version id `1794037122-34b0`. C1 with its bow and stern plated, a broad bake sun
  (no hard cast shadows), more flank floods, the fine detail layer on the hull and the game model's own parts, and room
  boxes that share a face no longer speckling.
- 2026-11-07, cloud session: version 11, version id `1794040501-8e1b`. K1 with its bow and stern plated (each face grouped
  by the way it faces most), soft bake light, the model's painted-on marking hidden; both ships with two detail tiles by
  hull area (an area mask from the bake: smooth on the plating, coarse on the sub-hull) and the roughness map in the
  painted look.
- 2026-11-07, cloud session: version 12, version id `1794051858-24a0`. C1's deflector, torpedo tube and nacelle noses as
  designed pieces (lit emitter cells in a grey frame, a ribbed launcher with a lit throat, intake rings round a lit core)
  in place of the model's flat glowing parts; K1's windows from its room plan in place of the model's strips; a class
  picker for the rest of both lines (each class shows once its bake lands).
- 2026-11-07, cloud session: version 13, version id `1794053991-357f`. The Swift, the Albatross and the Krai corvette on
  the line's first designs (the rest of both lines baking). The maps are published as files beside the page,
  `docs/mockups/hull-texture-designs-maps/<faction>/`, written by `tools/mockups/inline_data.py` (the manifests'
  `maps_dir`); the page is 7.7 MB, the maps 5.5 MB.
- 2026-11-07, cloud session: version 14, version id `1794057593-9f56`. The Condor, the Pelican and the Krai cruiser; fewer
  crimson accent plates on the Krai (the corvette, frigate and cruiser repainted); the dreadnought still baking.
- 2026-11-07, cloud session: version 15, version id `1794060545-e826`. The Krai dreadnought: the whole of both lines on the
  new hull method (nine ships). Page 12 MB, maps 11 MB beside it.
- 2026-11-07, cloud session: version 16, version id `1794091931-65fc`. The Concord ships back on the original light
  arrangement (a flood every 30 m; the owner: "too many lights ... go back to the way lights were arramged originally"),
  their textures unchanged; all five rebaked.
- 2026-11-08, cloud session: version 17, version id `1794098228-274b`. Windows and hangars lit warm yellow from inside (the
  owner: "Windows/interiors need to be a warm emissive yellow on the interior so they stand out more"): the rooms' light
  warm yellow, brighter, and a warm glow filling each lit pane (`docs/mockups/lib/baked-hull.js`).
- 2026-11-08, cloud session: version 18, version id `1794102257-2ee3`. The window light softened to a warm white (the
  owner: "it looks strongly tinted rather than just yellowish lighting"): the rooms keep their own colours, a smaller glow.
- 2026-11-08, cloud session: version 19, version id `1794112570-ed25`. The hangar bays lit near-neutral, with no warm glow
  over their mouths (the owner: "hanger bay doesnt ned that much tint"); the windows keep their soft warm light.
- 2026-11-08, cloud session: renumbered mockup 35 on merging main (the freighters took mockup 24, the battle consoles 25-33
  and the space chart 34); the link is unchanged. The lines above say mockup 24 as it was then.
- 2026-11-08, cloud session: version 20, version id `1794119731-c3e5`. The page reads mockup 35.
- 2026-11-08, cloud session: version 21, version id `1794134765-6d64`. Windows on the Swift (12), Albatross (86), Condor
  (288) and Pelican (50) by the Concord line's own rule and numbers (the owner: "missing windows on albatross"); the Krai
  models' own nacelle studs, rails and ribs hidden ("what the hell are these random cubes on krai ships?"); the rooms'
  fine patterns fading with distance ("z fighting in windows here": aliasing).
- 2026-11-08, cloud session: version 22, version id `1794139084-f64b`. The Condor down to three decks of windows, 160 (the
  owner: "way too many windows on the condor", "temove top and bottom rows").
