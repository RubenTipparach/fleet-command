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

## Open questions

| # | Question | Recommendation (provisional until answered) |
|---|---|---|
| A15 | Which direction do the Krai take: A Standing blade, B Lance or C Three-edged dagger? | A Standing blade |
| A16 | Sizes and growth: corvette about 55 m to dreadnought 212 m, a class growing by blades? | Yes |
| A17 | What the Krai call their ships and classes, and whether a ship wears its record (kill marks, captured blades) (culture, the owner's to decide, U7) | Placeholders and no marks until the owner decides |
