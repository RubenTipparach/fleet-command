# Design: ssarith-ship-concepts

## Version 15: the carrier, rooms behind the windows, forward floods, battle damage (owner, 2026-11-05)

With two night shots: "we need some more lights pointed forward to paint more of the hull", "we need a carrier", "we need
to show interior of ships with windows", "we need damage systems", and, of the hull emission, "also this light, I wish it
were more green".

| Ask | How | Numbers |
|---|---|---|
| Forward floods | `auto_floods` (defaults, a ship's own keys over them): rows of floods aimed forward along the port flank (mirrored), the top and the belly, from the stern to the bow's split | every 22 m, 1.8 kW, 80 degree cones, tilted 12 degrees into the plating |
| Greener emission | The strips' and green lamps' light | (0.1, 1.0, 0.24), was (0.27, 1.0, 0.54) |
| Rooms behind windows | Every window and bay is a quad of its own on the page, laid on the hull where the bake cut for it (`hull_detail.pane_box` writes each pane's centre, axes, size and room into the manifest), so its frame stays exact at any distance (owner: "windows and hangar bay are kinda messed up, mmake them clean, make the border clean"; a baked pane map was a few texels a window). A bevelled dark-metal rim with a fine lit line round the glass; behind it the room from `roomBehind`, lifted from the Concord page into `docs/mockups/lib/ship-rooms.js` (the Concord page now calls the same code, its shots unchanged to the pixel) | quarters, mess, workshop (green-lit), hold; one window in five dark; frames 0.1 m (windows), 0.35 m (bays) |
| The carrier | Blockout 13, grown like the picks: a flight deck (the hood) over a hangar block whose forward face holds two launch bays, two more on each flank, a keel blade below; hangars drawn behind the bays by `hangarBehind` (a deck, its stripe, three parked craft, ceiling light rows); on the board it lies beside the picks, to starboard. No greeble is kept with a vertex inside an opening's cutter, so nothing stands in front of a pane | 168 m by 68 m by 24 m, 326 triangles; bays 8-12 m by 5-6 m |
| Level rooms, geometric hangars (owner: "Need to account for normal angles on windows, and the hangar bay too", "Hangar bay is large enough for me to just recommend using geometry. Not sure if this illusion holds up well with battlescars") | A window's room is drawn in a frame level with the decks (the face's normal turned level), the pane cutting its front at a slant. A bay's width runs level (bow bays turn a quarter); its hangar is geometry (`hangarMesh`): deck, ribbed walls, ceiling lights, back wall, stripe, parked craft, self-lit, cut at the face, scorched with the ship; the hull opens over the mouth | bow bays 8 m by 6 m, 26 m deep; flank bays 12 and 10 m by 5 m, 11 m deep |
| Damage | `docs/mockups/lib/ship-fx.js` inlined: the hull shader runs its `dmgHoles`, `dmgScorch` and `interiorRoom`; Fire (Phaser, Torpedo, Repair) and a click on a ship | phaser: a 2.2 m scorch; torpedo: a 6 m scorch and a 2.4-4.0 m hole with fire, smoke and embers |

- Not yet: the Hunt hulls breaking apart (`lib/ship-break.js` reads a Concord-style loft; the Hunt's grown skin needs its
  own inside test), systems and their states, damaged lights flickering (L3). Each is a task.
- Provisional: the carrier's rooms and bays (until it is planned rooms first), the window and room light colours (H5).

## Version 14: the lighting pass, windows, PBR, normal maps, the emblem, emission (owner, 2026-11-05)

"Now those my friend are gorgeous looking star ships. Next up, lighting pass, windows, pbr, normal maps, logo, emissive
texture (you have some features that look like glowy bits)". The method is written up as the `ship-detailing` skill (owner:
"write up a skill for detailing space ships with custom texturing", "Include image samples in your documentation").

| Map or light | How | Numbers |
|---|---|---|
| PBR base colour | The same flat paint as the painted map (one function, `flat_paint`), with no light in it: cavities dark from the AO, plates darker toward their gaps, worn edges lighter, grain | 2048 px |
| ORM | AO; roughness and metalness by kind (painted armour 0.58 / 0.45, sub-armour 0.62 / 0.55, under layer 0.8 / 0.6, greebles 0.55 / 0.7, domes 0.12 / 0, slats 0.6 / 0.7, vents 0.9 / 0.3); paint 0.5 / 0.1; worn edges 0.22 smoother, 0.35 barer | 2048 px |
| Normal map | Baked in tangent space (OpenGL), the one map for the page and the game | 2048 px |
| Emission | Lamps, windows, the strips' lit line, the domes' faint inner light (0.35 of their green), a soft halo | 2048 px |
| Hull lights | The fixtures alone (sun off) and the lamps' and windows' own faces, baked as direct light on white plating, over `light_ref` 0.9, stored with a 1/1.6 curve | 1024 px |
| Fixtures | Three hull floods a ship from data (mirrored: five), a logo flood, a belly pair, position lights (port red, starboard green at the widest frame, white aft), red beacons top and bottom mid-ship, white strobes at the bow's tips (one to three, mirrored) | floods 2.2-3.2 kW, 60-90 degree cones, 0.9 m off the hull |
| Windows | 1.1 by 0.6 m, groups of 4-12 at 1.6 m pitch, rows 3.5 m apart on the port flank (mirrored), one in five dark, pale green-white (survey H5) | provisional until the rooms (W1) |
| Emblem | `assets/emblems/ssarith/hull_mask.png`, rendered from `docs/mockups/lib/emblems.js` by `tools/mockups/emblem_masks.mjs`, painted from above on the top armour over a patch of its plate colour, in its own hull colours | 5-7 m across |

- **The page**: Look (Painted, PBR, Clay) and Light (Studio, Night). PBR uses the base colour, ORM, tangent normals and a
  soft room environment; both looks add the light map and the emission. At night the live lights drop to almost nothing
  and the ships are lit by their own floods, lamps and windows; strobes flash, beacons pulse, position lights burn steady
  (sprites at the fixtures, written to the manifest).
- **Cost**: two more bakes a ship (tangent normals, hull lights), about 12 minutes a ship at 2048 px and 32 samples; the
  page carries six maps a ship.
- Provisional: the window and flood colours (H5), the windows' places (until the rooms are planned).

## Version 13: the picks only, their hull detail modelled and baked (owner, 2026-11-05)

With the reference game's orthographic sheet of a ship (not committed): "discard ships that I didnt want, only keep the
ramining ones. I'd like more features onthe hulls, the gorn ships have things like vents, lights, and sub armor pieces,
and primary armor peices. Armor pieces are carefully craftyer to not look the same, in your mockup armor pieces are
square, and there are no distinct features. like under armor area with greebles"; then, with three crops of it, "notice
stuff like liveries, smooth sphereical portions of armor, and specifically shaped vents"; and "no repeating elemnts,
single axis symmetry, and lots of shading to give the armor depth". What is taken is how such a hull is layered and
shaded, never its art.

- **The board** keeps only the four picks, two by two. The other blockouts, their builders and shots are deleted (git
  keeps them), and the triplanar tile with them: nothing on the picks repeats.
- **The detail is modelled** (`tools/blender/hull_detail.py`, laid out in `data/ships/ssarith/hull_detail.json`) over
  each low-poly hull and baked onto its own UVs, selected to active (Ship Design Bible T5-T8):

| Layer or feature | How it is built | Numbers |
|---|---|---|
| Primary armour | The hull's faces by facing, thickened outward, cut apart by gap slabs: a kinked spine and swept chevrons on top and bottom (every station's spacing, sweep and spine width drawn from the ship's seed), raked cuts on the flanks; each plate raised by its own amount; plates over 22 m2 get a second plate over part of them | 0.5 m thick; stations 6-13 m (top), 7-14 m (flanks); sweep 0.35-1.1; gaps 0.5-1.0 m, 40 % of cuts trenches of 2.6-4.6 m; lift up to 0.35 m; overlays drawn in to 0.86 and lifted 0.3 m |
| Sub-armour | A thinner shell with its own cuts, 35 % of them wide | 0.22 m |
| Under layer | The hull itself, near black, with machinery only where both armour layers leave it bare: blocks, sub-armour slabs, pipe runs, ribs | 1.6 per m2 sampled |
| Domes | A sphere sunk to stand h proud, in a collar ring, held by straps that hug it | r 2.6-6.4 m, h 0.8-1.05 m |
| Fans | A recess, a hub, fins tilted like a turbine's, a ring of lamps | r 3.8-4.6 m, 17-21 fins |
| Louvres | Chevron and raked-slot outlines cut through the armour, slanted slats clipped to them; four per ship, each its own shape and size | slats 0.34-0.5 m apart, 28-46 degrees |
| Lamps | Lit blocks; the red eye in a two-step housing; amber pairs; green markers | 0.32-0.6 m |
| Liveries | Green bands and a chevron stripe across the armour, painted from the baked position | 0.6-1.4 m wide |
| Light strips | The strip faces become a dark channel with a lit line down the middle | 0.7 m line |

- **Symmetry**: everything off the centreline is built on the port side and mirrored through a reflected frame; every
  piece's id is a hash of its mirrored position, so a plate and its mirror take the same paint.
- **Shading**: a low sun from port and ahead (ship space 0.6, 0.55, 0.58) baked with its cast shadows, AO reaching
  2.2 m, armour edges rolled 0.3 m by the Bevel node in the normals and the light, each plate pillowed darker toward its
  gaps, then the painted-bake paint-over. The page lays the object-space normal map under the live light.
- **Cost**: about 31-38 thousand high-poly faces a ship, baked at 2048 px; the high-poly never leaves Blender. The low-poly hulls are unchanged (294-454 triangles).

## Version 12: the picks, grown as one structure, each with its own texture (owner, 2026-11-05)

**The picks** (survey H4, answered in the conversation): "spearhead: there we go again with the ltiterals lol / headedf
lance: great! / forked prow, great! bit of a mess of geometry / trident: awesome! / harpoon yes!". Picked: 1 Hooded lance,
3 Forked prow, 7 Harpoon, 9 Trident. Spearhead (5) is withdrawn from the board. The other seven stay on the board, unpicked,
as the record of what was offered.

**Its own texture** (version 11): "every ship needs custom uv Unwraped textures. From NOW on . ALL SHIPS MUST AVOID
REPETTIVE TILE FOR MAIN COLOR MAP", "tile textures are for DETAIL only, we'll probably need a detail mask in the future,
same goes for Concord and Krai ships", "Also dont forget to bake AO as well". Now a rule for every ship (CLAUDE.md section
10; Ship Design Bible chapter 15, T1 to T4). Each picked ship leaves the page as OBJ (`tools/mockups/export_blockouts.mjs`)
and `tools/blender/painted_bake.py --ship` unwraps it (Smart UV Project at 60 degrees, islands packed again by shape),
bakes AO (1.5 m), normals, a part id, edges, glow and the surface position onto its own UVs at 2048 px, and paints its
colour map: the painted-bake paint-over, plus plating laid in ship space (courses of 4.5 m, plates about 11 m with a
length and offset per course), ochre chevrons in two bands on the dorsal faces, grooves with lit and shaded lips, lit ports
on the flanks and ink on the seams. The page shows that UV mesh with its own map and glow; the chevron tile is multiplied in
faintly, every 4 m, as detail only. The other blockouts keep the tile (they have no unwrap) until picked. A detail mask is
not designed yet.

**One structure, grown**: "these shapes need work, they arent connect together well, and violate our "no deformed
primitive rules", shapes must be made with purpose and flow into the grater structures". The version 10 picks were boxes,
tapered booms and flat plates placed against each other. Each picked ship is now one closed skin (Ship Design Bible chapter
14, F1 to F5):

- The body is lofted through cross-sections of one fixed 18-point layout (`hsec`): keel bottom, keel root, bottom chine,
  lower side, a side chine that can swell out, the hood's root, edge and top, and the crown. A feature a station lacks
  lies flat in its neighbours' line, so every station lofts to every other point to point.
- At the bow the body's last section is split along cut lines into regions; each region lofts on forward as a named
  branch, scaled and moved station by station, and ends in a cap. Two branches that split from one section share their cut
  line, so the skin never opens or overlaps.
- Light strips are faces of the skin (the keel's flat bottom, a lance's underside); the drive is a recess cut 1.6 m into
  the stern, its mouth lit.

| Ship | Body | Splits into | Triangles |
|---|---|---|---|
| 1 Hooded lance | 98 m to the bow split at +6 m; hood swept to 52 m across aft | hood (narrows to its point), lance (the body's middle, drawn out 56 m), keel blade (deepens and sweeps down) | 394 |
| 3 Forked prow | split at +10 m along the side chines | high prong (the upper half, rising 5.5 m), low prong (the lower half and keel, dropping 8.6 m) | 374 |
| 7 Harpoon | the hull closes onto its keel at +16 m | bow (capped), rail (the keel run on 50 m, flaring into a barbed head) | 294 |
| 9 Trident | split at +12 m from one point low in its middle | brow (lifts over the prongs), port and starboard prongs (the flanks, reaching out 8.8 m), keel prong | 454 |

The rooms-first design of each pick (task 3.1) still comes before any of this is final.

## Version 10: the inspiration's shape language, painted (owner, 2026-11-05)

With three images of the reference game's and a fan's Gorn ships: "gorn from star trek", "they looked menacing!", then
"make ships like this, with this kind of texture", "might need a new skill to make high -poly AO baked texture that looks
drawn on". The images are not committed (someone else's art). What is taken is their shape language and the way their
texture reads, in our own words and our own forms, never their designs (CLAUDE.md sections 1 and 10):

- **Reach forward**: lances, prongs and rails running well past the bow, often from low down.
- **A hood over a narrow neck**: a wide flat cowl swept back at its tips, so head-on a ship reads as a T or an anchor.
- **Low raked blades**: deep keel blades sweeping forward and down under the bow; fins raked aft.
- **Layered and chevroned**: overlapping plates, chevrons pointing forward, the mass heavy aft.
- **Light in strips** along the booms and blades.

None of these comes from the species' body (C1): they are weapon and hull forms. The twelve version 9 massings, made
before the examples arrived, are replaced by twelve in this language:

| # | Name | The arrangement |
|---|---|---|
| 1 | Hooded lance | An arrowhead hood over a short heavy body, a long lance below the bow, a keel blade sweeping forward and down |
| 2 | Twin lances | A flat wedge body, two lances far past the bow from its lower flanks, one tall fin raked aft |
| 3 | Forked prow | The bow splits into a high prong rising forward and a long low prong dropping forward |
| 4 | Anchor | Head-on an anchor: a wide swept hood, a narrow neck, a curved blade below |
| 5 | Spearhead | A slim body carrying a great broad blade-head, three raked fins round its stern |
| 6 | Mantle | A compact hull under a huge flat mantle swept back to two points, two short spurs beneath |
| 7 | Harpoon | A long rail running the whole ship and far past the bow to a barbed head, the hull riding aft on it |
| 8 | Raked blade | A deep blade of a hull raked forward to a keel point, a tall fin raked aft |
| 9 | Trident | Three prongs forward from a heavy core: port, starboard and keel |
| 10 | Wedge | A thick armoured wedge, two spurs at its bow corners, a raised spine |
| 11 | Arc | From the side an open arc: a raised upper hull and a long low boom joined only at the stern |
| 12 | Crown | A core hull, a short lance forward, six blades swept back round its stern |

**The texture: a new skill, `painted-bake`.** `tools/blender/painted_bake.py` models a high-poly armour tile (8 m by 6 m:
two rows of chevron plates pointing forward over seamed under-plates, a spine with a light strip in its groove, conduits
in clamps, vent wells), renders its normals, occlusion (to 0.45 m), edges and per-plate ids in Cycles, then paints over
them: flat olive, khaki and sand plates with a rare bronze one, a painter's light from the upper left in soft steps, lit
and shaded bevels, inked crevices, brush grain, green glow. On the board it is projected from three sides (the blockouts
have no UVs) at 2.5 times its bake, so a chevron is about 5 m across; about a third of the paint shows as emission so its
own light reads in shadow, and the live light plays over its normal map. A Painted switch shows the clay underneath.

Known: projecting from three sides mottles the angled faces; the next step for a picked blockout is a painted sheet laid
on its own parts, following their form (the skill's last row).

## Version 9: a blockout board (owner, 2026-11-05)

"Delete this design. It sucks, look at the inspiration. Block out more stuff. We need a variety of designs. Not just 3
lame ones that look like coffins. Look at my examples - gorn from Starfleet command".

The examples had not reached the session then; they came with version 10.

**Breadth first.** Twelve massings, each a different answer to what a frigate's body is, built from a few primitives
(ellipsoids, capsules, cylinders, lathed profiles, chamfered lofts, an extruded delta, tori), grey clay so only the form is
judged, each about 70-85 m long against the Concord Petrel's 88 m. None is drawn from the species' body (C1).

| # | Name | The massing |
|---|---|---|
| 1 | Bulwark | A broad flattened ovoid, a low dome forward, two round engine pods riding high astern |
| 2 | Lantern | A big forward sphere on a long neck, an aft block wrapped in three radiator rings |
| 3 | Catamaran | Two long hulls side by side, a deck spanning them midships, the command pod on the deck |
| 4 | Ring | A spindle hull run through a great ring on four spokes |
| 5 | Terrace | Three decks stepped back: a wide base, a narrower middle, a small top set forward |
| 6 | T-bar | A narrow hull with a wide bar across its bow ending in two pods; engines splayed astern |
| 7 | Crescent | A flattened crescent open astern, a core hull riding in its hollow on two bridges |
| 8 | Bundle | Six long pods bundled round a spine under a round cap |
| 9 | Teardrop | A smooth teardrop, broad and round forward, a second hull slung under it on a neck |
| 10 | Delta | A flat delta wedge with a raised keel ridge and three engines in its trailing edge |
| 11 | Offset | Asymmetric: the main hull to one side, a big pod on a stub wing to the other, a tall mast |
| 12 | Disc and drum | A flat disc forward over a drum-shaped engine hull aft and below, on a raked neck |

**Then depth, on the chosen ones only**: each picked blockout gets its rooms (the bible's chapter 2, rule M1: these
massings are studies, and nothing is called a ship until its rooms are fitted), its tradition from the Hunt's culture as
the owner sets it (C2-C4), its parts, and its plating rendered from a modelled tile (the panel-textures method of version
8, kept in git at 6f7a22c).

The page measures each massing's length, beam, height and triangles from the built geometry, and shows the board from
the front, from above and from the side.

## Versions 1 to 8 (deleted)

Version 1 took claws, scales and a reptile's sprawl from the species' body (withdrawn, 2026-11-04). Versions 3 to 8 drew
three traditions (Old Lodge, Trackers, Field Hull, after Course) on one long fitted hull, then smoothed, detailed and
textured it; the owner: "Not just 3 lame ones that look like coffins". Their records stay in `mockup-work.md`, the
artifact record and git.

## Open questions

| # | Question | Recommendation (provisional) |
|---|---|---|
| H1 | How is the Hunt's culture to be set? | We draft several traditions inside the Hunt for the owner to pick and change (accepted as recommended, 2026-11-04) |
| H5 | What colour are the Hunt's windows and floodlights? | Pale green-white windows, warm white floods (provisional, as drawn) |
| H4 | Which blockouts go on to be designed? | Answered by the owner, 2026-11-05: 1 Hooded lance, 3 Forked prow, 7 Harpoon, 9 Trident; Spearhead withdrawn |
