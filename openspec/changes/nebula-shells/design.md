# Design: nebula-shells

## D1. The field

Every shape is a density field sampled on a grid, positive inside cloud. Units are kilometres.

- **A column** (one per plume) is a soft cylinder whose radius swells and narrows with height, reaching from the plane up
  into the deck above and down into the deck below (`underside_km + thickness_km / 2`). Its edge is eaten by four octaves
  of value noise, domain-warped first so the billows fold over each other. The noise is an integer hash with full
  avalanche (CLAUDE.md section 10), never `fract(sin)`.
- **A deck** stands `underside_km` off the plane and is `thickness_km` deep (18 km and 20 km today). Its underside hangs
  `hang_km` lower in billows, reaches `join_km` further down in a funnel `funnel_km` wide round each column's root, is
  whole to `margin_km` past the outermost column, then thins away over `fade_km`. `coverage`, `base_km`, `detail_km` and
  `erosion` set how much of it is cloud and how its edges break. These are `data/space/nebula_decks.json`'s numbers, read,
  not copied.
- The lower deck is the upper deck mirrored, as the decks are now.

## D2. Polygonising: naive surface nets

Naive surface nets, the table-free cousin of marching cubes:

- one vertex in each grid cell the level crosses, at the mean of its edge crossings;
- one quad for each lattice edge whose ends straddle the level, joining the four cells round it;
- the normal from the field's gradient.

It gives smoother, more even triangles than marching cubes, with no 256-entry tables, and it is about 60 lines in C#. The
same function serves every shell, column and deck (CLAUDE.md section 6.1).

**Shells.** Each shape is cut at 1-4 levels from thin (0.05) to dense (0.5). The thin shell is the dark outer haze in the
look's `deep` colour; the dense shell is the bright core toward `lit`.

## D3. Drawing

- One mesh per shell, unshaded or lit per vertex by the battle's sun. They are alpha blended, either additive (Glow) or
  over (Soft), double-sided, with no depth writes, and drawn from the outermost shell in.
- Each fragment's alpha is multiplied by `|n . v|^soft`, so a shell seen edge-on fades to nothing and its silhouette goes
  soft instead of showing a hard rim.
- `glow` adds at the rims.
- No noise, no loops and no texture reads per pixel. The shader is small enough that no GPU driver should spend long
  compiling it.

## D4. Cost

Measured in the mockup (desktop, SwiftShader) and on the Pi:

| Case | Triangles | Built in |
|---|---|---|
| 3 columns, 3 shells, grid 32, no decks | about 100,000 | about 0.35 s (browser) |
| 3 columns, 3 shells, grid 32, with decks | about 160,000 | about 0.5 s (browser) |
| Pi test: 1 cloud, 3 shells, grid 40 (GDScript) | 20,714 | about 2 s |

On the Pi at 1280x720, not recording, the 20,714-triangle test ran at 104 fps from a fixed view and at 66-103 fps
(median 85) with the camera orbiting through it. The cost on the GPU is mostly overdraw (several transparent layers
filling the screen), not triangles. The grid is the knob: 32 for phones and the Pi, 48 for desktop.

## D5. If it goes in the game

- `NebulaShells.cs` (view) builds the meshes at load from `NebulaPlume` and `NebulaDeck`'s fields, on a worker thread.
- `style: "shells"` in `nebula_decks.json`, and `--lowspec` picks it.
- The field functions move into Core so the view and any check share one copy.

## Open questions

| # | Question | Recommendation (provisional until answered) | Survey |
|---|---|---|---|
| NS1 | Is this the nebula on every platform, or only the low-spec one? | Low-spec and phones first; desktop keeps `volume` until the owner compares them | NS1 |
| NS2 | Glow (additive) or Soft (over)? | Glow: it reads on black and never darkens what is behind it | NS2 |
| NS3 | How many shells, and what grid? | 3 shells; grid 32 on the Pi and phones, 48 on desktop | NS3 |
| NS4 | Should the shells drift (the marched cloud drifts)? | A slow vertex wobble, no rebuild; or none | NS4 |
