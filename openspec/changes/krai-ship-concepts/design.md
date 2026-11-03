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

## The rules, provisional

1. **Everything is a blade.** A diamond-section spine (the Concord's shared loft with a chamfer larger than the section,
   so it clamps to a diamond) and flat faceted blades: each blade's outline is fanned from a low ridge, with a honed
   bevel band 2 m wide at frigate size round every edge. No curves anywhere.
2. **Colour has meanings, as on the console.** Blade steel `#2a2526` everywhere; honed steel on the bevels (the
   console's worn-bright rolls); blood red `#b3121b` only on leading edges and the head's war mask; forge orange
   `#ff7a1a` only for light (engines, window slits, the fissures' glow); hot `#ffb070` at gun tips.
3. **A class grows by adding blade pairs.** Corvette: wings and tail. Frigate: canards and a keel blade added. Cruiser:
   an aft pair angled up against the wings' droop. Dreadnought: a second dorsal blade. A player counts blades to rank a
   Krai ship, as they count nacelles on a Concord one.
4. **Leaner than the Kethran.** Both empires take the bird (A8). The Kethran skin is armoured plate and trapezoid wedges,
   so its ships should be heavy; the Krai are thin blades with daylight between them.

## The three directions (frigate, about 82 m)

| | A Stoop | B Crossed blades | C Spearhead |
|---|---|---|---|
| Body | Dagger head on a thin neck, a deeper chest | Short body, same head | One flat delta, a raised ridge down its back |
| Blades | Wings swept back, 16 degrees down; canards; tail blade up; keel blade down | Four forward-swept blades in an X, 32 degrees up and down | Forward-raked talons at the rear corners; tail blade |
| Reads as | A raptor diving | Two crossed knives, head-on | A spear point |
| Measured | 89.7 m long (gun spikes included), 76.1 m span, 24.0 m high | 80.7 m, 50.1 m, 30.8 m | 84.8 m, 64.5 m, 15.4 m |

A is recommended: it is the clearest bird's head and wings (A8), and it grows by blade pairs without changing shape.

## The line, direction A

| Class | Scale of the frigate | Blade pairs | Concord partner |
|---|---|---|---|
| Corvette | 0.58 (about 52 m) | wings, tail | Swift (51 m) |
| Frigate | 1.0 (about 90 m) | canards, wings, tail, keel | Petrel (88 m) |
| Cruiser | 1.5 (about 134 m) | adds the aft pair | Albatross (112 m) |
| Dreadnought | 2.3 (about 206 m) | adds the dorsal crest | Condor (184 m) |

Each Krai class is a little longer than its Concord partner and much lighter in volume: predators, not bricks. The page
measures every length, span, height and triangle count from the built geometry.

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
| A15 | Which direction do the Krai take: A Stoop, B Crossed blades or C Spearhead? | A Stoop |
| A16 | Sizes and growth: corvette 52 m to dreadnought 206 m, a class growing by blade pairs? | Yes |
| A17 | What the Krai call their ship classes and ships (culture, the owner's to decide, U7) | Placeholders (corvette, frigate, cruiser, dreadnought) until the owner names them |
