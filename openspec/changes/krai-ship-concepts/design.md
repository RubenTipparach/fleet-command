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
   a barbed spine, a hooked talon point, a lit fuller; centred on the nose, in polished mirror steel), a crest along the spine, a keel blade, a muzzle for the spinal cannon, edge-on
   blade nacelles at 15 m either side on swept struts (6.6 m of daylight to the hull), a forge-slot drive.
4. **Skin.** Blued steel with forward-pointing chevron plates, tempered bronze edges, one crimson marking (placeholder),
   pale gold light; 52 windows on crew and command rooms only; nav lights and four floods.

Rule X1 of the guide (the hull complements the console) is a guide, not a check (owner: "remove the color hull to
console requirements", "I said compliment, so not a hard rule"): the page shows the two palettes side by side and
measures nothing between them.

## Brought to the Concord line's standard (owner, 2026-11-03)

Owner: "now bring the ship up to standrads, detailed normal maps/panels, paralx windows, detailed thrusters, lighting
etc", then "this is an alien ship, so consider cultural differences, use wrmer orangy lighting".

- **One implementation.** The Concord line's detail tile generator, its tri-planar GLSL, its room ray and its thruster
  move into `docs/mockups/lib/ship-skin.js` (style `strake`, the Concord's); mockup 6 now uses the module, and renders pixel-identical before and after
  (hero, windows, detail and thrusters shots compared, no pixel differing). The Krai page uses the same module.
- **Panels.** The chevron seams are gone (owner: "the diagonal plating kinda bugs me", with references of square-cut
  sci-fi plating, taken for their shape only). The shared tile generator gains a style `kit` (the Krai's, 8 m square,
  seed 913): plates split and split again, every line square to the hull, one or two corners cut at 45 degrees, raised
  or set in, and on each plate at most one feature (a bank of vent slots, a round hatch, a row of bolt holes, a smaller
  inset panel, or a recessed conduit with a pipe and its clamps); as a normal map and a grime map, the seams tinted
  bronze.
- **Windows.** Each window from the plan is a pane of dark glass in a bronze rim, with a room behind it by interior
  mapping. The rooms are the Krai's, provisional until the owner sets the culture (U7): bare blued-steel walls ribbed
  like a rib cage, a grating floor, no carpet or plants, forge-orange strip lamps and braziers or dark red for a sleeping
  watch; a barracks of racked bunks, an armoury wall of blades, a mess round a brazier, and a command room (a lit war
  table and consoles) wherever a pane meets a command room.
- **Thrusters.** The shared bell with six sides (faceted, not turned), blued and ribbed, white-hot at the throat to forge
  orange at the lip, an orange plume: a main bell and two smaller in the stern face, and one at each nacelle's tail.
- **Lighting.** Warm orange (#ff8a3c) floodlights as real spotlights with 90 degree cones: four down each flank grazing
  the plating (these eight cast shadows), a logo flood on each flank marking, an inspection flood on each nacelle, and a
  pair either side of the crest and of the keel blade (one lamp over a fin lit only one face of it); on in every light preset, brightest with no sun. Position lights, amber
  strobes on the nacelle tips that double-flash, red beacons top and bottom that pulse. A Shadow side light preset, as
  on the Concord line.

## The classes (owner, 2026-11-03: "sketch out various ship classes")

Four classes on one builder (`buildKrai` in the page): each is its plan and a few numbers for its parts; the hull, the
bayonet, the crest and keel blade, the guns, the nacelles, the bells, the windows, the marking and the lamps are fitted
from the plan, scaled by the class's length against the frigate's 87 m.

| Class | Length | Decks | Rooms | Crew | Weapons | Nacelles | Notes |
|---|---|---|---|---|---|---|---|
| Corvette | 47 m | 3 | 22 | 14 | twin bow guns | one pair | the smallest |
| Frigate | 87 m | 4 | 24 | 36 | one spinal cannon | one pair | plan laid by hand, the pattern |
| Cruiser | 126 m | 5 | 84 | 120 | two spinal cannons | two pairs | |
| Dreadnought | 211 m | 7 | 166 | 420 | three spinal cannons, broadside batteries | two pairs | raises a command block (B1, a super capital) |

- The corvette's, cruiser's and dreadnought's plans are written by `tools/mockups/krai_plans.py` from a list of rooms per
  deck (bow room, port and starboard runs, a keel weapon band with a corridor either side, stern room); it checks P1 as
  it writes, and `--check` fails when a committed plan differs from its list. Rooms are functional only (U7).
- The line-up shows the four one above another, sterns level, so a side view reads their true lengths.
- Every class passes the page's checks: P1, H1 (every room 1.05 m inside), nacelle daylight of 3 m at least, and L2 (no
  two lamp pools on one face meet).

## Damage (owner, 2026-11-03: "Implement damage systems")

- **One implementation.** The Concord line's generic damage code moves into `docs/mockups/lib/ship-fx.js`: the particle
  pools and the table of looks, emitters and their programmes, weapon light, flashes, the shockwave, debris, beams and
  torpedoes, the "flicker a couple of times, then hold" rule, and the hull shader's hole and scorch rules with the decks
  seen through a hole. Mockup 6 uses the module; its damage scenarios render as before (compared shot for shot).
- **What is the Krai's own** is how a Krai ship fails: systems reactor, bridge, guns, drive, nacelles port and starboard,
  and life support, each healthy, damaged, critical or destroyed. A hit takes the system of the room the plan puts under
  it (D1), and its hole is lit by that room: violet plasma at a nacelle, cyan-white coolant at the reactor, fire
  elsewhere (D2). Nacelles vent, spark, then break away and drift burning; the reactor leaks coolant from the belly under
  its room, jets it, the lamps stutter, then breaches; the bridge smokes, burns, then blows out; the guns spark, then burn
  at their muzzles; the bells cool and stutter; life support darkens the windows a section at a time.
- **Breaking apart** (owner, 2026-11-04: "Ship isn't breaking apart like concord. Please fix"). The Concord line's breakup
  moves into `docs/mockups/lib/ship-break.js`: fracture into seeded cells, cut faces showing the decks and frames a break
  goes through, caps, pieces breaking away and burning, the breach sequence (the outer cells first, then the outer parts,
  then the main blast), and cables that stretch and snap; the hull shader's half (a piece keeps only its cell, its broken
  edges charred) is `dmgBreak` in `lib/ship-fx.js`. Mockup 6 uses both and renders as before (five breakup scenarios
  compared: four pixel-identical, one with 357 pixels of fireball flicker). A Krai hull is one body cut into 7 cells on the
  corvette, 9 on the frigate, 11 on the cruiser and 14 on the dreadnought; its fixed parts go with the cell they sit in.
  The whole hull mesh stays with the whole hull and is hidden when the cells replace it, and each cell carries the windows
  nearest its seed (owner, 2026-11-04: "a piece broke off but the original stayed": the whole hull had been handed to one
  cell and stayed drawn under the pieces).
  A part that crosses a cell line (the keel blade, the guns, the bronze edges, the bayonet and the dorsal crest, which now
  sit on the hull's piece instead of breaking off on their own) is cut along the same lines: one copy per cell it reaches,
  each drawing only its own cell by the hull's own rule (`CELL_GLSL` in `lib/ship-fx.js`, which `dmgBreak` uses too)
  (owner: "failing to break cleanly please fix", "blade and fin did NOT break cleanylu"). Each floodlight belongs to the
  cell it lights and goes out only when that cell breaks away (owner: "exterior hull lights also dissaperaed after ship
  took damage"). The room blocks show only in the Rooms to Parts steps, also on a ship rebuilt after damage (owner: "hide
  ship plan by default").
  The cut-face shader held 12 hull stations, enough for every Concord hull; a Krai hull is fitted every 2 m, so every cut
  face past the bow was thrown away and a break left a hole (owner: "hole in the hull", "missing geometry, fix urgently").
  It takes 48 now, and the stations it is given drop any that lie on a straight line between their neighbours.
- **The bayonets** (owner, 2026-11-04: "The blade should be a different shape on each ship. Cruiser and dreadnaught would
  probably ave a wider shape like a saw rather than a pointy knife. The frigate could have a thin needle. They are more
  decorative than practical"): the corvette keeps the barbed knife, the frigate a thin needle with a collar at its root,
  the cruiser and the dreadnought first had a broad saw, replaced the same day (owner: "cruiser/dreadnaught nose has the
  wrong shape of blade. I want a wide blade instead of long blade", with photographs of crescent war blades) by a wide
  crescent standing across the nose: its back bowed forward, its horns raked back above and below the hull, two
  openings in the band, seated in the nose by a tang; a clean edge (its spurs taken off: owner, "this blade here doesnt
  need any poky bits, a clian scresnt plande is good"). Our own outline;
  only the shape (a crescent across the bow) is taken. The cruiser's crescent is smaller with longer, sharper horns (owner: "have the cruiser haave the blade be more
  pointy, smaller"). The cruiser carries one nacelle pair, not two (owner, 2026-11-04:
  "cruser I think only needs two nacelles").
- **Livery** (owner, 2026-11-04: "ships dont have much liveries on main hull, please add them"), painted in the hull shader
  so nothing lies on the plating to fight it: a crimson war stripe low on each flank, widening forward and sweeping up
  into a point toward the bow; bronze pin lines either side; battle tallies in fives aft above it, more on the bigger
  classes (3, 7, 12, 19). The hull only, not the nacelles. A crimson talon on the bow's top and bottom faces was
  taken off the same day (owner: "great liveries, but I'd remove the triangle on top if the noses").
- **Nacelles** (owner, 2026-11-04: "nacelles still kinda overly simplistic"): light strips on the outboard face (the
  old lit strip was inside the blade, so never seen; framed square vents were tried first and replaced the same day, owner:
  "make nacelles lit more like this. instead of squares", with a picture of nacelles lit by continuous bars): a long strip
  along the top and the bottom, joined at the aft end; then (owner: "nacelles have wrong designs here. they should look
  like glowing panels", "dont use the big bulge") two thick glowing panels stacked on the outboard face, each in a dark
  frame proud of the plating with a soft additive glow, red as the Krai console's backlight; bronze collars at the ends, a raked fin above and a smaller one
  below, two conduits along the inboard face in clamps, and a collector at the bow.
- **Windows** (owner, 2026-11-04: "window geometry extending a bit over the hull slope", "the last window ... geometry is
  clipped", "window paralax on slope has the wrong room angle"): a pane lies on one flat face of the section, checked at
  its top and bottom edges and both ends (same slope, same plane), or there is no window: a pane is never moved off its row's height, so every row stays level (owner: "remove these
  windows if they cannot be placed in an aligned fasion");
  the room behind a pane on a sloped face is level.
- **Thrusters** (owner, 2026-11-04: "look at all that zfighting"): the throat disc of the shared thruster has the bell's
  own number of sides and sits 6% of the bell's length inside it; 5 cm in, it lay exactly on the Krai stern face.
- The Damage tab: fire a phaser or a torpedo by clicking the ship (or Fire at it), hurt or destroy one system, repair (a
  new ship), and each system's health. Scenario shots replay from a seed.

## Open questions

Survey A15-A17 were withdrawn with the old directions (owner, 2026-11-03: "nuke, the krai survey since we're redoing
them"). None are open. Class names, ship names and markings stay placeholders until the owner sets them.
