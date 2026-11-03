# Design: concord-hull-concepts (v7)

## The family, from Star Crew Bridge Simulator

Every rule here is read off Star Crew Bridge Simulator at revision 95a49b2. Take the shape, not the text: that project
is the owner's own, and the Concord ships take its build rules; their names, sizes and livery are new.

| Trait of the Tern family | Where in Star Crew | Here |
|---|---|---|
| Octagonal hull lofted linearly through stations along Z, every corner chamfered at 45 degrees | `data/ships/tern/layout.json` hull.note and hull.sections; `exteriors.json` ships[osprey, shrike].sections_m as `[z, half beam, top, bottom, chamfer]` | Every Concord hull is a list of such stations, lofted the same way |
| White armour over a grey subhull | ship-exteriors design ("broad angular white/ivory armor over a grey subhull") and the Copper hero capture | Shader rule: top and side faces armour, bottom faces subhull |
| A narrow grey service band at the waterline with a pipe and clamps | `exteriors.json` tern.service_band (y -0.65 to 0.35 m, pipe at -0.18 m, clamps every 8 m) | The same band, the same pipe line and a dark clamp every 8 m |
| Grey dorsal channels | the Tern top capture | One channel pair (two on the cruiser) down the back |
| An identification stripe that wraps the hull, outlined | `exteriors.json` liveries; Copper and Cyan hero captures | Livery stripes at two to four stations, with a dark seam each side; they skip the service band |
| Windows on decks 3.5 m apart, warm panes in copper rims, only where crew live | `layout.json` decks (floors 3.5, 0, -3.5 m); `window_rim_m` 0.12 | Rows 1.5 m above each deck floor along the lived-in length; rims 0.12 m; a frame every fifth window |
| Level swept pylons at mid-height; long octagonal nacelles with a profile along their length | `exteriors.json` tern.pylon and tern.nacelle (`profile_t_scale`) | The same pylon shape and the Tern's nacelle profile on every class |
| Cyan field strip down each nacelle side, copper intake forward | the nacelle-detail and pylons captures | Emissive strip over the middle 72 % of the length; an orange intake disc at the front |
| Dorsal radiator banks aft, turret housings, twin engine bells, painted registry, navigation lamps | ship-exteriors design and captures | All five, placed from each ship's data |
| Sun from `[-0.5, 0.8, 0.35]` | `exteriors.json` sun_dir_ship | The page's key light |

## The Concord's own

- **Livery**: Concord blue stripes (sRGB 0.10, 0.40, 0.86) with a dark navy seam.
- **A raised command block**, glazed at the front and sides, on every class: the bridge is always where you look first.
- **Nacelles on upright pylons** as well as the family's level pair (below).
- **A secondary hull on the cruiser**, the pylons sweeping out of it.
- **Bird names**, as the family has: Swift, Petrel, Albatross. Prefix CNS, registry CC-.

## The line

| Class | Length | Hull beam | Decks | Built after |
|---|---|---|---|---|
| Swift corvette | 51 m | 16.0 m | 1 | Osprey |
| Petrel frigate | 88 m | 24.0 m | 3 | Tern |
| Albatross cruiser | 112 m | 32.0 m | 4, in the primary hull | Tern, in two hulls |
| SCS Tern (yardstick) | 86 m | 24.4 m | 3 | its own data |

Lengths are measured off the built geometry and shown on the page, nacelles included.

### The Albatross in two hulls

The owner, 2026-11-03: "albatross is a bid long, I would prefer it to have like a secondary hull where pulons extend
outr of". The primary hull is cut from 128 m of stations to 84 m (z 50 to -34 m) and keeps the four decks of windows and
the command block. A secondary hull, 52 m long (z -6 to -58 m), 15.2 m wide and 10.6 m deep, hangs under its aft half on
a neck 22 m long; it carries the engine bells. One nacelle pair (64 m) rides at the primary hull's waist, 24 m out, on
pylons that sweep out and up from the secondary hull's sides. Overall 112 m.

## Nacelle layouts on the Swift and the Petrel

The owner, 2026-11-03: "try some vertically aligned pulons too ... 1 pylon vertical, 2 pulons vertical", then "swift
having 1 below would be better, petrel having 1 above and below", then "instead of opptions. please just apply the
variants for both petrel and swift and include horizontal aligned ones aas well". The reference game's federation ships
often carry nacelles on upright pylons (Starfleet Command 2 manual, the ship pages; taken as a shape only).

The Swift is shown as three ships and the Petrel as two (the owner, 2026-11-03: "remove petrel with 1 pylon"):

| Layout | What it is | Rule |
|---|---|---|
| One below | One nacelle on the centreline under the hull, on one vertical swept pylon | The class's nacelle at its own size and station; 5.5 m daylight below the lowest hull point along its whole length (owner: "would also be nice for the vertical pyons to extend out a little further") |
| Above and below | One nacelle over the hull and one under it, each on a vertical swept pylon | 5.5 m daylight above the highest and below the lowest hull point |
| Level | The family's own: a nacelle pair on level swept pylons at the waist | `exteriors.json` pylon and nacelle |

The owner's picks are one below for the Swift and above and below for the Petrel; the other layouts stay beside them for
comparison. The page measures the daylight between each nacelle and every hull along the nacelle's whole length (sideways,
above or below, whichever is open) and shows it; every ship holds at least 3.0 m.

## Built on mockup 5

The owner, 2026-11-03: "need to bring lighting standards up to what we had before, and damage systems". The page is mockup 5's
page with the ship builder replaced; everything else is the same code:

| From mockup 5, unchanged | Adapted to the lofted hulls |
|---|---|
| Light presets (sunlit, shadow side, no sun), environment reflections, navigation lights, the shield bubble | Life support runs in eight sections along the primary hull, bow to stern, instead of round a saucer |
| Holes (cut away or drawn by interior mapping), scorch marks, venting fire, coolant, plasma, sparks, smoke | Fracture seeds and cut faces test "inside" with the signed distance into the hull's stations (`loftDist`, and the same octagon test in the cut-face shader) |
| Random fracture into cells, cut faces with decks, bulkheads and rooms, burn rims and embers, wires that stretch, arc and snap | The nacelle snaps at the middle of its pylon; the outer half goes with it |
| The staged core breach, chunks knocked off, the main blast, debris and shockwave | The bridge is the command block, blown off with a cap under it and a hole in the hull below |
| Weapon and blast lighting, the ship firing back | The ship fires from its nearest turret, or the torpedo launcher in the bow |

## Detail, normal maps and windows

The owner: "add detail textures, norrmal maps, fix up windows". One detail tile, 8 m along the hull by 7.8 m round it, so its
plate seams fall on the paint's own grid (every 4 m and every 2.6 m), drawn once from a seeded generator: a height field
(seams, inset and raised sub-panels, rivet rows every 0.25 m, hatches, vent grilles) turned into a normal map by a Sobel
filter, and a wear map (plate shades, grime streaks, speckle, scratches) that multiplies the paint. Both are projected from
three sides in ship space and blended by the face normal, so the flat-faced hulls need no UVs.

Windows are no longer boxes on the hull. Each is drawn in the skin: a 1.0 by 0.62 m pane in a 0.12 m copper rim, on the
vertical sides only, one row 1.5 m above each deck floor, every 2.2 m with a frame after each run of four, never on a
stripe. The glass is smooth and reflects; behind it a room 4 m wide, 3 m high and 4 m deep, set off-centre, is drawn by interior
mapping and its power follows its life-support section (owner, 2026-11-03: "make the interiors more interesting? like a
room with furniture, and sci fi wall panels? carpet? maybe a plant in the corner?"):

| Part | How it is drawn |
|---|---|
| Carpet | One of four colours (navy, burgundy, teal, olive), a woven pattern, a darker border |
| Walls | Sci-fi panels in 0.8 m bays with grooves, a dark dado to 0.9 m, a light strip at 2.35 m, the odd vent; a wall display in the mess and the office |
| Ceiling | Light panels in a 2 m grid |
| Furniture | Ray against boxes and a sphere: quarters (a bed with pillow and blanket, a desk with a glowing screen), a mess (a table on a pedestal, four chairs) or an office (a long desk, two screens, a chair) |
| A plant | A pot and its foliage in the far front corner of every room |
| Light | Warm, cold or dark from the ceiling; screens and strips glow in the room's accent colour |

A hole in the hull (a torpedo, or a phaser's third hit burning through) shows the deck behind it the same way; its back
plane sits at least 5 m in and is lit, so a small burn-through shows a room, not a black disc.

## Floodlights are spotlights along the hull

The owner, 2026-11-03: "these are all point lights, can we have 90 degree tangent spotlights like in sfc?". Each hull lamp
sits about 0.35 m off the plating and throws a 90 degree cone along it: full inside 40 degrees of its axis, gone by 50,
bright at the lamp and fading to nothing at its range. It is taken against the detail normal, so the fan grazes the plating
and every seam and rivet in it catches the light.

The owner, 2026-11-03: "I would prefer lights aim forward and down", "so lights on top and bottom face forward, and lights on
side aim down":

| Lamp | Where | Throws |
|---|---|---|
| Registry | On the dorsal plating behind the name | Forward along the plating, over the name |
| Command block | On the block's roof, at its front | Forward over the bow |
| Nacelle, on its side | Above the outboard flank | Down the flank |
| Nacelle, above or below the hull | At its aft end | Forward along it |
| Waist | On the shoulders, every 18 m, both sides | Straight down the hull's side |
| Stern, dorsal | On the plating aft | Forward along it |
| Belly | A pair at mid-hull on the ventral plating (owner: "on the bottom, maybe a light aiming forward and backward") | One forward, one aft |

## Turrets have a clear line of fire

The owner: "this turret's line of sight is obscured btw". The dorsal turret that sat behind the command block is on the
block's roof now, and the shoulder turrets sit outboard of the block's sides, so nothing on the ship stands in front of a
barrel.

## The Tern is its latest model, in our paint

The owner, 2026-11-03: "we are using the wrong tern model, I'd like to use the latest tern model, but with our standard
texture, instead of the custom texture the tern is using right now in star crew repo", and on its windows: "I meant no
internal geometry for rooms, use same room parallax trick". `tools/mockups/tern_geometry.py` copies the positions, normals
and triangles of Star Crew's `assets/models/exteriors/tern.glb` (one part per material) into the page between marker
comments, with the source revision; its textures, UVs and vertex colours stay behind. The page paints every part with its
own shader and detail maps (hull, nacelles, copper rims, livery accents, dark trim), and its window panes show rooms by the
same interior mapping as the Concord ships. It is split at load into the hull, the inner half of each pylon and the outer
half with its nacelle, so it breaks up and loses nacelles like the others.

## The Albatross, two ways

The owner: "albatross is a cool ship, can we try placing pylons attached to the main hull instead? I just wanna see if I
like that better". A second Albatross carries the same nacelle pair on level pylons swept from the primary hull's waist
(roots 14.5 m out, nacelles 25 m out, 6.2 m of daylight), with the secondary hull clear beneath.

## Thrusters

The owner: "fix thrusters, make them more detailed, properly texture them". Each engine is a bell lathed from a flared
profile (lip radius r, 1.5 r deep, narrowing to 0.55 r at the throat), dark metal outside with 32 ribs and 7 rings, a lip
ring and an octagonal mounting collar; inside it is lit by its own heat, red at the lip to white-orange at the throat, the
throat glowing and a faint additive plume behind. All of it follows the impulse engines' state.

## Lights are on or off

The owner: "I dont like lights flickering constantly when a ship take damage, flicker a couple times, then no more
flickering. lights are either on or off, not flickering indefinitely". When a light's state changes (a section losing
power, the bridge, a nacelle's strip, the deflector, an engine, a broken-off piece) it flickers through four short windows
in the first 1.06 s, showing its old state, then holds its new one. A system that is damaged but still working stutters the
same way once and stays lit. Fire and the reactor's red warning pulse are not lights and keep moving.

## Cost

A frigate is about 1,500 triangles of hull and fittings before its fracture pieces (hidden until it breaks), and 40-90 draws on the page. That is under the
12,000-triangle full-detail budget Star Crew sets per ship (`exteriors.json` budget_triangles). Windows are two
instanced meshes. In the game each class is a Blender generator (`blender-hard-surface`) from the same station data.

## Open questions

| Id | Question | Recommendation (built in the mockup) |
|---|---|---|
| A11 | Which nacelle layout does each class keep? | The owner's picks: Swift one below, Petrel above and below, the Albatross its secondary hull and level pair (provisional until the owner chooses from the eight on the page) |
| A12 | Concord sizes: corvette 51 m, frigate 88 m, cruiser 112 m? | Yes, so the frigate stands beside the 86 m Tern |
