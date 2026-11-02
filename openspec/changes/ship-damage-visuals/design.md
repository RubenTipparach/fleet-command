# Design: ship-damage-visuals

## Reading damage by eye (the owner's question)

"How can I understand ship subsystem damage by looking at it?" Our answer: **every subsystem fails in its own colour,
its own place and its own motion**, so no two look alike, and a state reads from the far chase camera on a phone.

| Subsystem | Where | Healthy | Damaged (below 66%) | Critical (below 33%) | Destroyed |
|---|---|---|---|---|---|
| Nacelle (each) | the legs | grille glows blue, steady | grille flickers; plasma wisps | violet plasma jets from the nacelle, grille dim | snaps at the knee and tumbles away burning; its lights die; the ship limps |
| Reactor (warp core) | engineering hull belly | core strip glows cyan | **coolant leak**: white-cyan gas that freezes into glitter, from the belly | core pulses red-orange; coolant jets; every window on the ship flickers | **core breach**: the ship breaks apart |
| Bridge (the crown) | top of the saucer | crown windows lit | smoke, windows flicker | windows dark, fire venting | crown blown off, a hole in its place |
| Weapons | phaser rings, torpedo ports | strips catch the light | sparks at the mounts | strips dark, scorched | mounts cratered |
| Shields (deflector and emitters) | the deflector dish | dish glows; hits ripple the bubble | dish flickers | dish dark, sparks | no bubble: hits land on the hull |
| Impulse engines | saucer aft vents | red glow | sputter, black smoke | one vent dark, fire | dark, burning |
| Life support and power | saucer sections | windows lit | that section flickers | section dark, red emergency light at its breaches | venting fire, dark |
| Hull | everywhere | clean | scorch marks | holes, venting | breaks apart (at 0) |

Colours carry meaning across factions: **fire orange is a fire** (atmosphere burning), **cyan-white gas is coolant**
(the reactor), **violet is plasma** (drive), **red light inside a hole is people** (emergency lighting), **sparks are
power** (weapons and shields). A dark area is a power loss.

### Fire in a vacuum

Venting fire burns while the compartment's air vents: a jet of flame for a few seconds per breach, then smoke and
embers, then a glow at the hole's rim (survey A4). Cinematic, but short, so a burning ship means fresh damage.

### Scorch marks and burnt edges

Every hit leaves a scorch: a charred centre with ragged noise, a matt finish (metal loses its shine) and, while fresh,
a glowing rim that cools over about 20 s. Holes have a wider char ring and an ember edge. Broken pieces are charred
**toward their broken edges** ("burnmarks towards the dges"): a char gradient over the last 6 m before a break, an
ember line along it that cools over about 30 s.

### Holes: modelled or parallax

The page shows both, on the same hits (the Holes toggle), for survey A3:

| | Modelled | Parallax |
|---|---|---|
| How | The hull skin is cut away (on the page, fragments inside a ragged sphere are discarded), its inner skin shows dark; deck plates every 3.4 m and ribs fill the hole, lit red by emergency light; through-holes where a hit passes through | The skin stays whole; the shader draws a room behind it by interior mapping: decks every 3.4 m, walls every 6 m, a back wall, red light, depth fog |
| Silhouette | Holes break the outline; light passes through | The outline stays whole |
| Cost on a phone | Extra geometry per hole (on the page, about 200 triangles) and a pre-cut mesh in the game | One shader branch, no geometry |
| Close up | Holds up from any angle | Flattens at grazing angles |

Recommended: **both**: parallax for ordinary hits (cheap, any number), modelled breaks at fixed fracture lines
(pre-cut in Blender) where a system is destroyed or a chunk comes off.

### Chunks shot off

Destroying a nacelle snaps its pylon at the knee: the nacelle and lower pylon drift and tumble away with a fire trail,
its lights flicker out within 2 s, its grille fades; the thigh left on the ship sparks and vents plasma from a charred
stump. The ship keeps flying on one nacelle (survey A6: recommended, half top speed and a slower turn, and it can
still disengage; with none, impulse only and no escape). Survey A5 asks which pieces may come off.

### Breaking apart

The owner, 2026-11-02, on version 1 (eight equal saucer wedges with hollow insides): "interiors are hollowed, this is
not what I want", then "the ship breaks very programmatically, how do we make these chunks look more structurally
random?", then "hull pieces look interesting, I think they should look more scorched, and have paralax so they look like
actual interiors", pointing at interior mapping in Spider-Man PS4 (polycount.com discussion 204601).

- **Random chunks.** Each body (the saucer, the engineering hull) is split by fracture seeds scattered through its volume,
  seeded per class so the same ship always breaks the same way: 11 saucer pieces on the cruiser, 9 on the frigate, 7 on
  the corvette, 3 or 4 in the engineering hull. A piece is the body clipped to its seed's cell (the points nearer that
  seed than any other), so pieces vary in size and shape and some split the saucer through its thickness. The skin's
  edge is torn by noise of about 1 m. The crown, the neck, the pylons and nacelles stay whole pieces: they are
  subsystems and break where the subsystem does.
- **No hollow shells.** Where two cells meet, each piece gets a cut face on their shared plane, clipped to the body's
  outline and to that pair's border. It shows what the break cut through: the skin edge on, deck plates every 3.4 m,
  bulkheads at a room width that changes deck by deck (6, 9 or 12 m, staggered so no two decks line up), and machinery
  spaces packed solid.
- **Interiors with depth (interior mapping).** Between the deck plates and bulkheads the cut face draws rooms behind
  it without any geometry: a ray from the eye is traced into the room grid and the first floor, ceiling or wall it meets
  is shaded, so the rooms shift with parallax as the piece tumbles. Rooms are dark (power out, about half), red
  (emergency light), burning (flickering orange) or still lit, with ceiling light strips, crates on the floor, light that
  falls off toward the back.
- **Rooms square to the break.** Rooms are laid out in the face's own frame (decks level, bulkheads upright, rooms
  running straight back 5-8 m), after the owner: "The walls seems slanted there". No room is orange (owner: "no orange
  rooms"): they are dark, dim emergency red or cold-lit; their walls are sooty, with a reddish-orange flame licking up
  them near the break. The deck plates and bulkheads of the face carry a ragged orange burn rim (owner: "I like the
  orange burn highlights, I wanted something like that for the room wall dividers").
- **Scorched.** The whole cut face is sooty; soot and char deepen over the last 12 m toward each broken edge, rooms
  there are blackened, and an ember line runs along every broken edge and cools over about 30 s. The skin is charred
  over the last 11 m before the break.

- **Pieces fall off first** (owner: "I also expect pieces to fall off the ship before the main piece"). A core breach
  is a 3.6 s sequence: secondary blasts, then the outer saucer pieces (outermost first), the crown, the nacelles and the
  fin come off one by one, then the main blast throws the rest apart. Before any breach, a torpedo hit on a saucer whose
  hull is below 60% knocks off the outer piece it lands on; the pieces still attached close their faces toward the gap.
- **Wires and sparks.** Cables cross every border two pieces share; as the pieces separate the cables stretch, glow at
  their hot ends and arc with blue-white sparks, then snap at a random length (2-16 m on the cruiser) and hang from each
  piece, cooling over 25 s.
- **A tougher reactor** (owner: "please give the rector some more health before exploding"): it takes 45% of each hit,
  so it lasts about twice as long, and reaching zero starts the sequence above rather than an instant explosion.

Each piece gets an outward velocity and a spin, a fire on it that dies within about 10 s. A flash, a shockwave ring in
the ship's plane, a burst of fire, 300 debris shards and a coolant cloud from the core. In the game the cells are
pre-cut in Blender (Voronoi fracture of each body, seeded per class) and the cut faces carry the same interior-mapping
shader; survey A3 decides where modelled breaks and parallax are used.

### Light from weapons and blasts

Owner: "shots from the ship should have lighting, shots hitting the ship should also have lighting (explosions + whatever
weapon was fired)". A pool of four lights is reused oldest first: a phaser beam lights the hull where it lands, a
torpedo carries its red glow to the hit, every blast flashes the hull around it, and the ship's own fire (the page's
"Ship fires" button: a phaser from the arc facing the enemy, or a torpedo from the launcher) lights its mount and the
plating beside it. One `shoot()` draws and lights a shot in either direction. In the game these are a few dynamic lights
over the baked hull lights (CLAUDE.md section 10), capped per frame on phones.

### Holes, burnt through

A torpedo opens a hole; three phaser hits within about 5 m burn one through too. A hole's parallax interior is the same
room grid with crates on the deck, ceiling light strips and soot near the rim.

## One implementation

`damage(ship, system, amount, hit)` is the one function every source calls (a click on the hull, a system button, a
scenario). `stateOf(health)` maps health to the four states, and `applyLook(ship, system)` is the one place a state
becomes a look. The game does the same: the simulation owns health and states; the view only reads them.

## Cost

On the page: particles from two pools (5000 additive, 2500 alpha-blended), one draw call each; hole interiors about
200 triangles each, at most 8 holes; up to four fire lights per ship, allocated at start so shaders never recompile.
The hull shader adds up to 8 hole, 24 scorch and 4 break tests per pixel. In the game: the same tests in a Godot
spatial shader, on the phone only the nearest 4 scorches, and particles on the GPU.

## Open questions

| Id | Question | Recommendation (built in the mockup) |
|---|---|---|
| A2 | How do you read subsystem damage by looking at the ship? | A signature per subsystem (the table above) |
| A3 | Holes: modelled, parallax, or both? | Both: parallax for hits, modelled at fracture lines |
| A4 | Venting fire: brief and cinematic, realistic, or long-burning? | Brief: jets while air vents, then smoke and embers |
| A10 | The break-up: random chunks with interior-mapped cut faces, as in version 2? | As shown |
| A5 | Which pieces can be shot off? | Nacelles, the bridge crown and saucer sections; whole break-up only at a core breach or zero hull |
| A6 | One nacelle left: how does the ship fly? | Half top speed, slower turns, can still disengage; none: impulse only |
