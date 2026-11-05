# Design: ssarith-ship-concepts

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
| H4 | Which blockouts go on to be designed? | None recommended: the owner picks |
