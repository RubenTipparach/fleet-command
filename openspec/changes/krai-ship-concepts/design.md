# Design: krai-ship-concepts

## Where the language comes from

Everything here starts from the Krai console skin (`openspec/changes/console-skins/design.md`, the Krai column and
version 23), not from a reference game's ships. Section 1 of CLAUDE.md applies: the shape of an idea, never another
game's ships, names or art.

| Source | What it gives the ships |
|---|---|
| Owner, the skin: "combat focused ... triangular motifs, angry red ... orange lit buttons ... blade like" | Hulls made of blades and triangles; red and forge orange |
| Owner, version 23: "rough gun metal ... akin to volcanic magma type metal" | The hull material: near-black cast gunmetal, a cooled-lava crust, fissures that glow faintly |
| Survey A8 (blank, so its recommendation stands) | A bird's head and wings |
| The skin's world view (written for the skin, provisional): war is the only honest test; lean predators | Lean, sharp, never bulky |

## Version 2: spaceships, from the culture (owner, 2026-11-03)

On version 1: "Those are fighter jets, not spaceships. Think about how we defined culture and story". Version 1 read
"blades and swept triangles" as wings, canards and tail fins: things that fly in air. They are withdrawn. Version 2 takes
each line of the culture as written in the console skin and turns it into part of a spaceship:

| As written (console-skins design, Krai column) | In the ship |
|---|---|
| World view: "War is the only honest test" | They meet the enemy face to face: every hull is thin head-on, armoured edge first, and every direction is a weapon you charge with |
| Bodies and technology: "Lean predators; forged steel, edged everything" | One forged blade of volcanic gunmetal, honed bright at every edge, no curves |
| Ships: "Knife-blade hulls, swept triangles" | The blade is the hull itself, not wings on a body; triangles are vanes, warheads and gun houses |
| Method: "Black blade steel with red, lit orange when engaged"; version 23: "volcanic magma type metal" | Red only on edges and the war mask; forge orange for the furnace-mouth drive, window slits and fissures |
| Survey A8: a bird's head and wings | A hooked beak, the ram; vanes folded along the hull like a raptor's wings in a stoop, never spread to fly |
| "Rank is won in blood" | A class grows by adding blades; names and any marks of kills are the owner's to decide (A17, U7) |

## The rules, provisional

1. **Nothing flies.** No wings, canards or tail fins: every surface is armour, a vane folded against the hull, a ram or
   a gun house. Thrust is one drive (or one per blade, C) on the ship's axis.
2. **The hull is the blade.** Its section is a blade's: a tall diamond (A), a shaft under a warhead (B) or three blades
   round a core (C).
3. **Colour has meanings, as on the console.** Blade steel; honed steel on the bevels; blood red only on edges and the
   war mask; forge orange only as light.
4. **A class grows by adding blades.** Direction A: one vane pair and the keel edge on a corvette, a dorsal crest on a
   frigate, a second vane pair on a cruiser, a third on a dreadnought.
5. **Edge first.** Thin head-on, a wall from the side: the opposite of the Concord, which shows its broad face.

## The three directions (frigate)

| | A Standing blade | B Lance | C Three-edged dagger |
|---|---|---|---|
| Section | A tall diamond: a sword stood on its edge | A shaft, a crew block, a three-sided warhead | Three blades at 120 degrees round a core |
| Forward | A hooked beak, the ram | The warhead, the ram, red at its point | The three blades meet in one point |
| Blades | Vanes folded along the flanks, a crest on the back, the keel edge below | Three fletching vanes at the stern | The three blades themselves |
| Guns | Three-sided gun houses on the back and keel | Gun houses on the crew block | None yet |
| Drive | One furnace-mouth drive | One | One between each pair of blades |

A is recommended: it is the most direct reading of "knife-blade hulls", it carries the bird, and it grows by blades
without changing shape. The page measures every length, span, height and triangle count from the built geometry.

## Version 1 (withdrawn)

A Stoop, B Crossed blades, C Spearhead: blade wings and tail fins on a body. Rejected as fighter jets.

## Windows

The Concord's person-sized window (1.1 m by 0.6 m, one row a deck, 3.4 m apart) cut as narrow slits along the diamond's
upper flanks, lit forge orange, in runs with gaps, seeded so a shot is the same every run.

## One implementation

The loft, the diamond section and the station arithmetic are `docs/mockups/lib/concord-line.js` (inlined), the same code
the Concord line uses. The Concord yardstick beside each Krai ship is that module's `plainModel`. The Krai builder lives
in the page for now; if the Krai ships go into the battle console, it moves into its own module beside the Concord's.

## Not done yet

Damage, hull lamps, a detail texture and the 40 px silhouette check against the Kethran (whose ships do not exist yet).

## Cost

About 1,000 triangles a frigate and 2,000 a dreadnought, before detail: well inside a phone budget.

## Version 4: the frigate on the Ship Design Bible (owner, 2026-11-03)

Owner: "did you make new krai ships yet?", then "ok nuke, the krai survey since we're redoing them". Versions 1 to 3 were
drawn hull first in the console's own volcanic metal; version 4 follows the guide (`ship-design-guide`) step by step,
for one class, the frigate:

1. **Rooms.** `data/ships/krai/frigate_plan.json`: 24 functional rooms on four decks 3.5 m apart (floors 5.25, 1.75,
   -1.75 and -5.25 m), crew 36. The two middle decks are wide (to 6.5 m each side), the top and keel decks narrow, so
   the section is a lozenge; the spinal cannon runs the keel deck from z -10 to 42 m beside its passage. The rooms are
   functional only; what the Krai call them, and any room their culture adds, is the owner's (U7).
2. **Hull.** Fitted round the rooms by `docs/mockups/lib/ship-plan.js` (one module, shared with the guide) in its
   `blade` section: the chamfers are cut as deep as the rooms allow, so it is a blade on its edge. Every room is
   1.05 m inside (0.45 m clearance plus 0.6 m structure), measured on the page.
3. **Parts.** An alien bayonet fixed under the spinal cannon's muzzle as the ram (owner: "make it more like a weapon, a
   bayonette if you will", "an alien bayonette, not a human one": no guard, ring or grip; a serrated belly raking back,
   a barbed spine, a hooked talon point, a lit fuller), a crest along the spine, a keel blade, a muzzle for the spinal cannon, edge-on
   blade nacelles at 15 m either side on swept struts (6.6 m of daylight to the hull), a forge-slot drive.
4. **Skin.** Blued steel with forward-pointing chevron plates, tempered bronze edges, one crimson marking (placeholder),
   pale gold light; 52 windows on crew and command rooms only; nav lights and four floods.

Rule X1 of the guide (the hull complements the console) is a guide, not a check (owner: "remove the color hull to
console requirements", "I said compliment, so not a hard rule"): the page shows the two palettes side by side and
measures nothing between them.

## Open questions

Survey A15-A17 were withdrawn with the old directions (owner, 2026-11-03: "nuke, the krai survey since we're redoing
them"). None are open. The other classes follow the frigate once it is approved; names and markings stay placeholders
until the owner sets them.
