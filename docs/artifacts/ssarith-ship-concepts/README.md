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
