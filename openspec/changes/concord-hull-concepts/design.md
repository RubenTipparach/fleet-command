# Design: concord-hull-concepts (v2)

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
- **Classes counted by nacelles**: one pair on the corvette and frigate, two pairs on the cruiser.
- **Bird names**, as the family has: Swift, Petrel, Albatross. Prefix CNS, registry CC-.

## The line

| Class | Length | Hull beam | Decks | Nacelles | Built after |
|---|---|---|---|---|---|
| Swift corvette | 51 m | 16.0 m | 1 | 2, slung low | Osprey |
| Petrel frigate | 88 m | 24.0 m | 3 | 2, level at the waist | Tern |
| Albatross cruiser | 131 m | 32.0 m | 4 | 4: a level pair and a ventral pair | Tern, grown |
| SCS Tern (yardstick) | 86 m | 24.4 m | 3 | 2 | its own data |

Lengths are measured off the built geometry and shown on the page, nacelles included.

## Pylon arrangements

The owner, 2026-11-03: "try some vertically aligned pulons too, i thnkn those from sfc was interesting - 1 pylon
vertical, 2 pulons vertical". The reference game's federation ships often carry their nacelles above the hull on
upright pylons (Starfleet Command 2 manual, the ship pages; taken as a shape only).

| Arrangement | What it is | Rule |
|---|---|---|
| Level | The family's own: a nacelle pair on level swept pylons at the waist | `exteriors.json` pylon and nacelle |
| One upright | One nacelle on the centreline above the hull, on one vertical swept pylon | Same nacelle size and station as the class's first pair; 3.0 m daylight above the highest hull point under it |
| Two upright | A nacelle pair above the hull, each on a vertical swept pylon rising from the shoulders | Nacelle at 0.62 of the hull's half beam (and at least 2.5 m clear of the centre), 3.0 m daylight above the hull |

On the cruiser the second pair follows: under the centreline on one upright pylon with One upright, level under the
waist with Two upright. The Tern always keeps its own level pylons. The page's Measured block gives the daylight
between hull and nacelle for whichever arrangement is shown.

## Cost

A frigate is about 2,600-2,900 triangles and 40 draws on the page; the cruiser about 4,400. That is under the
12,000-triangle full-detail budget Star Crew sets per ship (`exteriors.json` budget_triangles). Windows are two
instanced meshes. In the game each class is a Blender generator (`blender-hard-surface`) from the same station data.

## Open questions

| Id | Question | Recommendation (built in the mockup) |
|---|---|---|
| A11 | Which pylon arrangement for the Concord: level, one upright, two upright, or a different one per class? | Per class: the corvette one upright, the frigate level (the family's own), the cruiser two upright over a level ventral pair (provisional; the page shows every class in all three) |
| A12 | Concord sizes: corvette 51 m, frigate 88 m, cruiser 131 m? | Yes, so the frigate stands beside the 86 m Tern |
