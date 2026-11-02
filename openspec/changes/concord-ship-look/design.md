# Design: concord-ship-look

The page `docs/mockups/concord-ships.html` builds every ship from one parametric builder (`buildShip(cls)`) and one
class table (`CLASSES`). Textures are drawn by one painter (`paintSurface`) into canvases at load. In the game they are
Blender generators and Material Maker graphs; the page shows the look, not the pipeline.

## The owner's notes, worked out

### 1. How windows communicate scale

A window is the one part of a starship whose size the eye already knows: it is sized for a person. If every window
is the same size, the window count tells the size of the ship without a yardstick.

| Rule | Number | Why |
|---|---|---|
| A window is person sized | 1.1 m wide, 0.6 m tall | The same size on every class: a corvette has few, a cruiser has rows of them |
| One row per deck | rows 3.4 m apart | A captain counts decks: two on the corvette's rim, two on the cruiser's rim, five through the cruiser's saucer centre |
| Windows come in runs | 4-12 windows, then a gap | Rooms; the gaps are structural ribs (one every 15 degrees on a saucer) |
| Not every window is lit | about 80% lit, warm or cool per room | People live there; an unlit run is a store room or a sleeping watch |
| A few big windows | 3 m observation windows, two to four per ship | Lounges and the bridge; their size is read against the small ones |
| Nothing person sized is ever scaled | hatches 1 x 2 m, pods 2.2 m, markings text 0.3-0.6 m | Every one is a second yardstick that agrees with the first |

The page's Scale view puts a 1.8 m figure beside a run of rim windows.

### 2. Silhouette: head and legs

The Concord explores and mediates; its ships look like a thinking creature walking. The saucer is the head, the
crown on top of it the brain (the bridge), and the nacelles are legs: each hangs on a two-part pylon with a knee
bend. The three classes differ in how the legs attach:

| Class | Length | Head | Body | Legs |
|---|---|---|---|---|
| Bastion cruiser (CS Meridian, CS-1207) | 312 m | 72 m radius saucer | a neck down to a long engineering hull with the deflector dish | from the engineering hull's hips, knee out, nacelles 150 m |
| Lantern frigate (CS Wayfarer, CS-0514) | 196 m | 50 m radius saucer | a flat engineering block behind the saucer, a dorsal fin | slung below the block, nacelles 108 m |
| Swift corvette (CS Kestrel, CS-2231) | 118 m | a 31 m saucer stretched 15% fore and aft | a small engine pod | straight from the saucer's aft quarter, swept back, nacelles 70 m |

The other factions' silhouettes, as the owner described the reference games: the rival empires take a bird's head and
wings, the hunter race takes claws. Which of our factions take which is survey A8 (recommended: the Kethran Hegemony
and the Krai Warhost take the bird, the Ssarith Hunt takes claws).

### 3. Livery, markings and branding

Palette, from the Concord skin (`console-skins` design): **Signal** `#2f62e0` for the livery bands (the Concord's
flag), **Deep** `#1b2a6b` for the registry and name, **Lilac** `#b7a6f2` for the pinstripes, a pearl hull `#e4e2dc`,
and **Alert** `#e8352b` only on rescue and danger markings (one meaning, as on the console).

- **Registry**: `CS-` and four digits, large on the saucer's top forward and under its aft, on the engineering hull's
  sides and on the nacelle placards. Name: `CS MERIDIAN`, with `CONCORD STARSHIP` under it.
- **Crest**: seven open rings round a centre ring (the circle of species), drawn in the Concord's icon style: open
  circles and arcs, nothing pointed.
- **Marking density is a class choice** ("some ships have more markings than ohters"):

| Class | Density | Markings |
|---|---|---|
| Bastion cruiser | Ceremonial | Twin Signal bands with a Lilac pinstripe, large registry, crest, few service marks |
| Lantern frigate | Working | Registry, a single band, plus hatch outlines and numbers, escape pod rings with `ESC` numbers, docking targets, walkway lines, `KEEP CLEAR` and `RESCUE` marks in Alert red |
| Swift corvette | Minimal | One bold Signal racing stripe down the centreline and the nacelles, the registry, nothing else |

- **Lived in**: panels vary in tone, some panels are newer (replaced), soot streaks run aft from the impulse vents and
  vents, edges weather. Never decals of dirt everywhere: wear follows use.

### 4. Plating says how the ship was built

| Class | Pattern | Story |
|---|---|---|
| Bastion cruiser | Radial: concentric rings, seams staggered like brick, panels halving in width toward the centre | Built ring by ring in a yard |
| Lantern frigate | Patchwork: plates of mixed size and tone, many replaced | A working ship, repaired in the field |
| Swift corvette | Strakes: long fore-and-aft strips with rivet lines | Built for speed; the lines lead the eye forward |

Each plate also varies in roughness, so metal patterns read in reflections as well as in colour.

### 5. Lit for the dark

From mockup 3's lighting table (the owner's airliner guide), baked into each surface's emission map as the albedo
times a light pool, so a dark letter stays dark inside its pool, as under a real floodlight:

- Floodlit registry, name and crest (soft pools).
- **Hull floods that graze the plating**: long, shallow pools from lamps set in the hull, so panel lines read in
  shadow.
- Lit windows.
- Live (not baked), as sprites: port red and starboard green on the saucer rim, white strobes double-flashing at the
  nacelle tips and stern, red beacons slow-pulsing top and bottom.

The page's light presets (Sunlit, Shadow side, No sun) show that the ship and its name still read with the sun behind
it or gone.

## Cost

Measured on the page's meter (three.js, all three classes in the line-up): see the page; each ship is 40-60 meshes
because the saucer is cut into eight wedges for the damage change (in the game, one mesh per wedge and LOD). Textures
per class: saucer top 2048 x 2048, saucer bottom 1024 x 1024, rim 4096 x 128, hulls 1024 x 512 and 2048 x 256, each
with an emission and a roughness map: about 70 MB of RGBA per class uncompressed on the page; in the game they are
compressed (ETC2 or ASTC on Android, BC on desktop) at about a quarter of that, and the phone LOD uses half the size.
The final budgets are set by mockup 3 against survey T3 (a 2022 phone at 60 fps).

## Open questions

| Id | Question | Recommendation (built in the mockup) |
|---|---|---|
| A7 | Marking density per class: ceremonial cruiser, working frigate, minimal corvette? | As shown |
| A8 | Which factions take the bird-of-prey and claw silhouettes? | Kethran and Krai: bird head and wings; Ssarith Hunt: claws |
| A9 | Registry style `CS-1207`, name `CS MERIDIAN`, `CONCORD STARSHIP` under it? | As shown |
