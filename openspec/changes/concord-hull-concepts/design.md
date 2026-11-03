# Design: concord-hull-concepts (v4)

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

Each of the two classes is shown as three ships, side by side in the line-up:

| Layout | What it is | Rule |
|---|---|---|
| One below | One nacelle on the centreline under the hull, on one vertical swept pylon | The class's nacelle at its own size and station; 3.0 m daylight below the lowest hull point along its whole length |
| Above and below | One nacelle over the hull and one under it, each on a vertical swept pylon | 3.0 m daylight above the highest and below the lowest hull point |
| Level | The family's own: a nacelle pair on level swept pylons at the waist | `exteriors.json` pylon and nacelle |

The owner's picks are one below for the Swift and above and below for the Petrel; the other layouts stay beside them for
comparison. The page measures the daylight between each nacelle and every hull along the nacelle's whole length (sideways,
above or below, whichever is open) and shows it; every ship holds at least 3.0 m.

## Cost

A frigate is about 2,500-2,900 triangles and 40 draws on the page; the cruiser about 3,700. That is under the
12,000-triangle full-detail budget Star Crew sets per ship (`exteriors.json` budget_triangles). Windows are two
instanced meshes. In the game each class is a Blender generator (`blender-hard-surface`) from the same station data.

## Open questions

| Id | Question | Recommendation (built in the mockup) |
|---|---|---|
| A11 | Which nacelle layout does each class keep? | The owner's picks: Swift one below, Petrel above and below, the Albatross its secondary hull and level pair (provisional until the owner chooses from the eight on the page) |
| A12 | Concord sizes: corvette 51 m, frigate 88 m, cruiser 112 m? | Yes, so the frigate stands beside the 86 m Tern |
