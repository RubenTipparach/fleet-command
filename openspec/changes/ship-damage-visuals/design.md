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

At a core breach or zero hull the ship comes apart along the lines it was built in: eight saucer wedges, the crown,
the neck, the engineering hull's two halves, both nacelles and their pylons. Each piece gets an outward velocity and a
spin, its broken faces charred with ember lines, a fire on it that dies within about 10 s. A flash, a shockwave ring
in the ship's plane, a burst of fire, 300 debris shards and a coolant cloud from the core.

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
| A5 | Which pieces can be shot off? | Nacelles, the bridge crown and saucer sections; whole break-up only at a core breach or zero hull |
| A6 | One nacelle left: how does the ship fly? | Half top speed, slower turns, can still disengage; none: impulse only |
