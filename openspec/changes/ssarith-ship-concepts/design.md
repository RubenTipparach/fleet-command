# Design: ssarith-ship-concepts

## Version 9: a blockout board (owner, 2026-11-05)

"Delete this design. It sucks, look at the inspiration. Block out more stuff. We need a variety of designs. Not just 3
lame ones that look like coffins. Look at my examples - gorn from Starfleet command".

**The examples did not reach this session**: the only image in it is the owner's version 6 screenshot of the spine, and
the repository holds no reference images (someone else's art is never committed, CLAUDE.md section 1). The owner is asked
to attach them again. When they come, what is taken is their massing and proportions, never their designs (CLAUDE.md
sections 1 and 10: "Take the shape, not the text"; "Nothing is traced, sampled or modelled from another game's assets").

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
| H4 | Which blockouts go on to be designed? | None recommended: the owner picks; and the examples of the owner's inspiration, attached again |
