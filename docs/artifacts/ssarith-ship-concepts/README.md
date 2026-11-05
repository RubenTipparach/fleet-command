# Ssarith Hunt Ships (mockup 12)

- Link: https://claude.ai/artifact/UbsDVv6EvKbt3wkCDb6Ris
- Source: `docs/mockups/ssarith-ship-concepts.html` (one copy, not duplicated here); the Concord line yardstick and the loft
  geometry are inlined from `docs/mockups/lib/concord-line.js`, and the painted tile from
  `assets/textures/painted/manifest.json`, by `tools/mockups/inline_data.py`.
- OpenSpec change: `openspec/changes/ssarith-ship-concepts/`.
- Shots: `docs/screenshots/mockups/ssarith-ship-concepts/` (`tools/mockups/shoot.mjs`), at desktop, phone landscape and
  portrait.

## Versions

- 2026-11-04, cloud session (owner: "using current bibles and mockups on ship design, lets consider the Ssarith Hunt
  culture and how their ships will look"): version 1, page written and shot. The frigate's rooms on three decks, the hull
  fitted round them with sloped transitions and a shovel prow, directions A Grasp, B Jaw and C Crouch, Stalk and Strike,
  deck plans, 40 px silhouettes, the Concord Petrel yardstick, the page's checks (P1, H1, broad prow, squat, short,
  nacelle daylight, no kill red in Stalk). Published from the cloud session, version id `1793798110-ab57`.
- To do from the desktop session: add it to the mockup tracker (game mockups, stage: version 1 withdrawn), and republish the survey
  (`docs/design/survey.html`, section H added in this commit; the cloud session's account cannot open the survey artifact).
- 2026-11-04, cloud session (owner: "you are over stereotyping an alien species which is extremely insensitive"):
  version 2, version id `1793800832-dbd2`. Version 1 marked withdrawn on the page (subtitle and Notes); its readings kept
  as the record only. The survey's section H is now one question, H1 (how the Hunt's culture is set), still to publish
  from the desktop session.
- 2026-11-04, cloud session (owner: "now update the damn ships!", "Ssarith ships", "not anyother ships thoo lol, SSarith
  needs work"): version 3. The claws, jaw, scales and crouch are gone; three traditions drafted inside the Hunt on the same
  rooms, A Old Lodge, B Trackers and C Course, each with its own plating, colours and moving parts, every choice naming its
  line in the culture or marked ours, and the stereotype test (C5) shown on the Notes tab. Waiting and Hunting replace
  Stalk and Strike. Shots in `docs/screenshots/mockups/ssarith-ship-concepts/`. Published, version id `1793802161-a15d`; survey version 2 (H2 added), version id `1793802162-bb01`.
- 2026-11-04, cloud session (owner: "sillhoutte seems a bit rough along the spine. I would prefer something smooth. No go
  for C, A and B are good options I will think about that more. Add new C that replaces nacelles with a secondary hull
  instead, adding some assuymetry or lower or upper secondary hull"): version 4. Smooth chamfers on every direction
  (`fairChamfers`); C Course withdrawn; C Field Hull, a secondary hull with its own rooms in place of nacelles, in three
  layouts (below and to port, below, above). The field hull's plan is inlined from
  `data/ships/ssarith/frigate_field_hull_plan.json`. Published, version id `1793803895-9a25`; survey version 3 (H2, H3), version id `1793803896-cbea`.
- 2026-11-04, cloud session (owner: "I'd like to see these contours smoothed out top down and sides stay flat", "but
  without adding more polygons", "remove the wobble", "yea if this could be smoothed thatd be nice"): version 5, a fair
  hull (`fairHull`): smooth outlines from above and in profile, one chamfer along the body, the bows added after the
  fairing; no polygons added. Published, version id `1793804179-2dae`.
- 2026-11-04, cloud session (owner: "oof now theres uv issues"): version 6, version id `1793805620-1059`. The plating is
  laid on each face itself (along the ship, and across the face in metres on its own surface), so plates keep their size
  on the 45 degree chamfers and line up along the length; no geometry changed.
- 2026-11-04, cloud session (owner: "ships arent very complex"): version 7, version id `1793829581-24aa`. Working detail on
  every direction from the plan and the bible (gun houses over the forward battery and under the keel, bay doors where
  the bays meet the hull, attitude thruster blocks, the shared thruster bells, a forward emitter) and each tradition's own
  (A: rafters, a belt strake, patch plates, a mast; B: tube clamps, a sensor head, radiator fins, vent hoods; C: field
  hoops, neck conduits, a dish). `docs/mockups/lib/ship-skin.js` is now inlined for the bells. 5,100-6,200 triangles a frigate.
- 2026-11-04, cloud session (owner: "Take a look at the originals textures. They packed it with 3d depth and details. Use
  you skills with star crew bridge command to pack in those details by rendering a high poly model into textures", "Then
  applying metal detail texture on top"): version 8, version id `1793831193-f961`. Each tradition's plating is a tile
  modelled in Blender and rendered face-on into albedo, normal and ORM maps (`tools/blender/build_hull_tiles_ssarith.py`,
  `assets/textures/ssarith/`, inlined from its manifest as WebP, 646 KB), with a 2 m fine metal tile over every face; a
  Close view. Contact sheet `docs/screenshots/mockups/ssarith-ship-concepts/hull-tiles-contact-sheet.png`.
- 2026-11-05, cloud session (owner: "Delete this design. It sucks, look at the inspiration. Block out more stuff. We need a
  variety of designs. Not just 3 lame ones that look like coffins"): version 9, the page replaced by a blockout board of
  twelve frigate-sized massings in grey clay. Versions 1 to 8 deleted with their plans, tiles, generator and shots (git
  keeps them). Published, version id `1793837617-6c69`; survey version 4 (H4), version id `1793837618-10b4`.
- 2026-11-05, cloud session (owner, with three images of Gorn ships, not committed: "they looked menacing!", "make ships
  like this, with this kind of texture", "might need a new skill to make high -poly AO baked texture that looks drawn on"):
  version 10, version id `1793842420-9cbe`. Twelve blockouts in the examples' shape language, painted by the new
  `painted-bake` skill (`tools/blender/painted_bake.py`, `assets/textures/painted/`), a Painted switch over the clay. Survey
  version 5 (H4 for the new twelve), version id `1793842421-9921`.
- 2026-11-05, cloud session: version 11, version id `1793842658-f5f8` (shots of the board from above and ahead painted; subtitle says painted).
- 2026-11-05, cloud session (owner: the picks "headedf lance: great! / forked prow, great! ... / trident: awesome! / harpoon
  yes!", Spearhead withdrawn; "every ship needs custom uv Unwraped textures ... tile textures are for DETAIL only ... dont
  forget to bake AO as well"; "these shapes need work, they arent connect together well, and violate our "no deformed
  primitive rules", shapes must be made with purpose and flow into the grater structures"): version 12, version id
  `1793849478-14e9`. The four picks rebuilt as one grown skin each (`Skin`, `hsec` in the page), exported as OBJ
  (`tools/mockups/export_blockouts.mjs`) and baked onto their own UVs with AO, plating, chevrons and lit ports
  (`tools/blender/painted_bake.py --ship`, `assets/ships/ssarith/`, inlined as WebP; page 3.7 MB). Survey version 6 (H4
  decided), version id `1793849483-356d`.
- 2026-11-05, cloud session (owner: "discard ships that I didnt want, only keep the ramining ones. I'd like more features
  onthe hulls ... vents, lights, and sub armor pieces, and primary armor peices", "notice stuff like liveries, smooth
  sphereical portions of armor, and specifically shaped vents", "no repeating elemnts, single axis symmetry, and lots of
  shading to give the armor depth"): version 13, version id `1793873084-ad12`. Only the four picks. Each one's hull detail
  is modelled over it (`tools/blender/hull_detail.py`, `data/ships/ssarith/hull_detail.json`: primary plates each its own
  outline and height with overlays, sub-armour, machinery, domes, fans, louvres, lamps, liveries; mirrored) and baked
  onto its own UVs at 2048 px with a low sun's shadows and AO (`tools/blender/painted_bake.py --ship`), shown with its
  object-space normal map; the tile is gone. Page 4.6 MB.
- 2026-11-05, cloud session (owner: "lighting pass, windows, pbr, normal maps, logo, emissive texture"): version 14,
  version id `1793882426-b4b5`. Each pick's PBR set (base colour, ORM, tangent normals, emission) and a light map of its
  hull lights (floods, belly pair, position lights, beacons, strobes, windows), the Hunt emblem from
  `docs/mockups/lib/emblems.js` on the top armour (`tools/mockups/emblem_masks.mjs`), Look (Painted, PBR, Clay) and Light
  (Studio, Night) switches, strobes and beacons blinking. Page 9.8 MB. The method is the `ship-detailing` skill.
- 2026-11-05, cloud session (owner: "we need some more lights pointed forward to paint more of the hull", "we need a
  carrier", "we need to show interior of ships with windows", "we need damage systems", "I wish it were more green", "the
  hull emission"): version 15, version id `1793901988-73f2`. The four picks and a new carrier (blockout 13: a flight
  deck over a hangar block, launch bays forward and on the flanks, a keel blade), rows of floods aimed forward, the strips
  and green lamps greener, rooms behind every window and hangars behind the bays (`docs/mockups/lib/ship-rooms.js`,
  shared with the Concord page), battle damage on the shared `docs/mockups/lib/ship-fx.js` (Fire: Phaser, Torpedo,
  Repair), the carrier on the board beside the picks. Greebles are kept out of every opening. Page 13.1 MB.
- 2026-11-05, cloud session (owner: "windows and hangar bay are kinda messed up, mmake them clean, make the border
  clean"): version 16, version id `1793908285-29c4`. Every window and bay is its own quad on the page, placed from the
  panes `tools/blender/hull_detail.py` writes into the manifest (`painted_bake.py --ship ... --panes-only` writes them
  without a bake), with a bevelled dark-metal frame and a fine lit line round the glass, the room or hangar behind drawn
  from exact coordinates. The baked pane maps are gone. Page 13.1 MB.
- 2026-11-05, cloud session (owner: "Need to account for normal angles on windows, and the hangar bay too", "I actually
  like the paralax so I'd keep it", "the windows and hangar on the side on sloped angles need work though", "hanger window
  taller than side of the ship"): version 17, version id `1793915201-ef18`, an interim publish while the five ships
  rebake. Rooms and parallax hangars drawn level with the decks whatever the face's angle; bow bays turned so their width
  runs level; every window and bay checked to lie flat on its face, frame and all (a window over a bend is left out, the
  flank bays moved onto the middle of their face at 4 m tall). The Hooded lance's maps are rebaked; the other four still
  carry their v16 maps, so a faint glow of the old bay openings shows round the carrier's bays until the rebake lands.
- 2026-11-05, cloud session: version 18, version id `1793916208-29de`. Version 17 with all five ships rebaked (the old
  bay openings' glow gone). The page's subtitle still read v17 in this publish.
