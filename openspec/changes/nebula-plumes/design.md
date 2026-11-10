# Design: nebula-plumes

## Rules

A scenario's `nebulae` is a list of plumes: a place on the battle plane (`x_m`, `z_m`), a radius (`r_m`), a `density`
(0 to 1), a `look` and whether it is an ion storm (`storm`). `Rules.NebulaAt(x, z)` is the one reading of how thick the
nebula is at a point: the thickest plume the point is inside, each plume thinning by `edge_thin_frac` from its middle to
its edge (`density * (1 - 0.3 * d / r)`) and nothing outside its circle. Every effect is the clear-space value moved
toward the plume's value by that thickness, so a thin plume is a mild one. Between two ships the thicker end counts.

| Number (`data/battle/systems/cloak.json`, `nebula`) | Value | Meaning |
|---|---|---|
| `hit_x` | 0.5 | A shot's chance to hit, multiplied, when either ship is in a full plume (owner: "and 1/2 acuracy to hit") |
| `energy_range_x`, `energy_kinds` | 0.5; `beam`, `bolt` | An energy weapon's range, multiplied (owner: "energy weapons 1/2 range"); torpedoes, missiles and drivers keep theirs |
| `missile_lost_per_s` | 0.08 a second | A missile's chance each second to lose its target and fly on straight, while it or its target is in a full plume (owner: "missiles dont quite work"): about half are lost over 8 s of flight |
| `lock_km` | 20 km | No ship can lock another through a plume (any plume on the sight line between them, either ship's own included) from farther than this, unless its ECCM is above the other's ECM (owner: "can hide you from enemies", "ships whose targeting raycasts goes through a cloud looses target locks"); a lock held when a plume comes between is lost |
| `ew_gain_x` | 2 | How much more ECM and ECCM count in a full plume (unchanged from `cloaking`) |
| `edge_thin_frac` | 0.3 | How much thinner a plume is at its edge than its middle |
| `storm_strikes_per_min` | 3 a minute | How often lightning finds each ship in a full ion storm |
| `storm_shutdown_s` | 5 s | How long a struck ship has no power |
| `storm_grace_s` | 15 s | After its core restarts, how long no bolt finds that ship again |

One reader for each rule (CLAUDE.md 6.1): the chance to hit goes through `Rules.NebulaHitX` in the same function the
shot and the hover card call; every range (the shot, the in-range lamps, the weapon arcs, the AI, the scripted captain)
goes through `Rules.RangeKm`; a lock goes through `Battle.CanLock`, which asks `Rules.NebulaOnLine` how thick the cloud
is on the sight line.

**The sight line** (`Rules.NebulaOnLine`). A plume is a column standing through the battle plane (the view draws it from
the deck below to the deck above), so a sight line between two ships on the plane passes through it exactly where the
line, seen from above, crosses its circle. The thickness on the line is the plume's thickness at the line's nearest point
to the plume's middle, the densest plume counting; 0 when the line is clear. It was 60 km and "a ship inside a plume"
before the owner asked for the sight line; at 20 km a plume between two fleets hides each from the other until they close
to knife range (survey NP8).

**An ion storm** (`Battle.Nebula.cs`). Each fixed step, each ship inside a storm rolls against
`storm_strikes_per_min / 60 * thickness * dt`. A strike does no damage. It sets the ship's core down for
`storm_shutdown_s`: nothing charges (battery, capacitor, mounts), the brownout factor is 0 so the drive, warp, tractor
and repair crews stop, shields neither hold nor regenerate, the ship cannot fire, its point defence is silent, and a
cloak drops. The rolls come from the nebula's own fork of the battle's seed and are drawn only for ships inside a storm,
so a battle without a storm replays as it did before (6.3). Both sides are struck alike; AI captains do not yet steer
round a storm (task 1.4).

## The cloud

After the volumetric clouds the owner named (Ace Combat 7's): a density field marched along the view ray, lit by a
second short march toward the sun.

- **The shape is baked, the detail is noise.** `NebulaPlume.Cover` (C#, the one implementation of the column's shape)
  says how far inside the column a point is: the rule's circle on the plane, and above and below it a wander, bulges and
  a waist, each of its own size from the plume's seed and its lobes of its own count (owner: "we also dont want all the
  columns to have the same shape"). It is baked once per plume into a 64 x 64 x 64 texture of one byte a voxel.
- **Four forms** (`form` in the scenario, `NebulaDef.Forms`; owner: "some nebula clouds can just be scattered puffs, or
  columns that dont quite connect, to give it a more varied look"): a `column` from the deck below into the deck above,
  flaring into each (`anvil`, sized against its height so a wide column never spreads into a table) and thinning away
  where the deck's funnel comes down to meet it; a `rising` column with a head of towers short of the deck above; a
  `hanging` one with a foot short of the deck below; a `puff` joined to neither. Every form covers the rule's circle on
  the plane. Each scene of the four has two big columns (the plume and the storm), five more of 8 to 18 km in all four
  forms, and four puffs of 4 to 6 km scattered across the field.
- **The decks** (`NebulaDeck`, `nebula_deck.gdshader`, `data/space/nebula_decks.json`; owner: "make a cloud mesh for
  top and bottom of nebula and fade out the edges, so it looks like the nebula columns are connected to a larger
  structure above and below the map"). A battle with plumes has two slabs of cloud, mirror images, 18 km above and below
  the plane and 20 km deep, marched like a plume (40 steps, 2 toward the sun) from the same noise and the same light
  (`nebula_cloud.gdshaderinc`, the one implementation of how a nebula's cloud is made and lit). The face toward the battle
  hangs and lifts by 5 km and comes down 8 km more in a funnel round the root of each column that reaches it. A deck is
  whole 40 km past the outermost column and gone 90 km beyond, clear round every planet, and fades into the sky with
  distance from the camera (110 to 260 km); it thins away as the camera climbs toward it, so a zoomed-out view looks down
  through where the top deck was. Its colour is set by where the columns stand (owner: "color the nebula decks with vertex
  colors too", "which can be done uppon column placement"): each column's look over its funnel and round it, their mean
  far from any, varied in broad patches toward the glow, toward the other looks, lighter and darker (`variety`). The top
  deck's face lies in its own shadow and takes a fill; a column darkens the same way as it climbs into it, so they meet
  in one shade. The decks are the view only: the rules know nothing of them.
- **Varying density** (owner: "the nebula cloud column has uniform density, so its not something convincing"): a broader
  noise carves dense cores, thin veils and holes inside the cloud (`contrast`, `nb_vary`), and the absorption is low
  enough (2 to 2.6 a radius, from 7) that the inside shows.
- **Fog** (owner: "add in a general layer of fog too, so detail in the distance fades into fog"): a battle among plumes is
  hazed in the plumes' own colour (the engine's exponential fog, `fog_km` 150 km, a little over half of it over the sky),
  and the clouds take the same haze at their own depth inside their march.
- **Three styles to compare** (owner: "either the deck would have to be clouds too or everything is geometry. lets try
  both", "mixing them doesnt look great, lets see how all one or the other looks", "third technique to try out:
  crab-breezes cloud system", "cheap 3d billboards with lighting"; `style` in `nebula_decks.json`, `--nebula-style=` for a
  still): `volume` (above: every column and both decks marched), `meshes` (puffs of displaced ball, `nebula_mesh.gdshader`)
  and `billboards` (Crab Breeze's lit camera-facing puffs at `5ad3de22`, `nebula_billboard.gdshader`). The two puff styles
  share one placement (`NebulaPuffs`: each column filled inside its own `Cover`, each deck a field of puffs over the
  marched deck's ground, every puff's colour set when it is placed) and one light (`nebula_puff.gdshaderinc`), drawn as
  one sorted MultiMesh. Side by side: `docs/screenshots/game/nebula-styles.png`. While the owner compares them, the
  deck's colour rule is written twice (the deck shader and `NebulaPuffs.DeckColor`); the styles not chosen go, and the
  copy with them (survey NP12).
- **The sky** (`tools/space/nebula_recipes.py`; owner: "for nebulat cloud battles - we should design a custom background
  to match the cloud colors"): the sky of a battle among plumes takes its clouds, space and core from that battle's plume
  looks (read from `scenarios.json` and `plumes.json`, so the colours have one home), widest first, with a wall of the same
  cloud right round the horizon, the band that shows between the decks. The shader multiplies it by the shared noise
  (`assets/space/plume_noise.bin`, 64 x 64 x 64, four channels: a low Perlin-Worley billow and three Worley octaves,
  tiling, written by `tools/space/plume_noise.py` from a seeded generator) for billows and the wisps that eat their edges.
- **The march** (`nebula_plume.gdshader`): the ray is clipped to the plume's bounding column and to the scene's depth
  (so ships and planets cut the cloud where they stand), stepped with a per-pixel jitter from an integer hash, and
  stopped once the cloud is opaque. At each step in cloud a short march toward the sun gives its shadow; the colour runs
  from `deep` (in its own shadow) to `lit`, with `glow` where the cloud is thin, and a storm's flashes add light near
  where they are.
- **From inside**, the cloud thins toward the camera over `clear_km` (40 km), so the captain's own ship and its
  neighbours stay in sight while the far field is fog. The rules do not thin: only the picture does.
- **Lightning** (`BattleView.Nebula.cs`): a storm flickers at `flashes_per_min` of its look at seeded places in the
  cloud (the view's own, never a strike). A strike (`Battle.Lightning`) draws a forked bolt from the cloud's head to the
  ship, lights the cloud round it, and the ship's lamps and drive glows go dark while its core is down.
- **The radar dial and the tactical map** draw each plume's circle at its radius: lilac for a plume, the caution colour
  and "ION STORM" for a storm.
- **Seven looks** (`data/space/plumes.json`): violet, garnet, brass, teal, orchid, and the two dark storm looks, slate
  and cinder. A look's colours reach the deck shader as given: Godot already turns a `Color` passed to a shader uniform
  into linear light, and converting it again in C# (the first deck build) drew the decks a saturated red beside their
  columns.

**Render order.** Planet layers draw at priorities -10 to -6, the decks at -2, a plume at -1, everything else (ships,
glows, beams, shields) at 0. Plumes among themselves are drawn far to near by the nearest side of each column (its
sorting offset, set each frame), not by its middle, so two wide columns one behind the other do not swap. A plume reads the depth buffer, so opaque ships and planets hide the cloud behind them. Known limit: a
planet's air halo or rings standing in front of a plume draw under it, since both are see-through and the plume draws
later (task 2.12). No scene puts a plume behind a planet's limb today.

## Costs

Measured on the owner's Mac (Apple M4 Max, 1920 x 1080, Forward+), `concord_heavy` at its opening (two plumes 50 to
90 km off, two planets, six ships):

| | Frame time |
|---|---|
| The same battle type without plumes (`concord_battle`) | 5.7 ms (175 fps) |
| Two plumes, the shape computed in the shader every step (first build) | 14.5 to 24 ms (69 to 42 fps) |
| Two plumes, the shape baked (this design) | 8.2 to 8.6 ms (116 to 122 fps) |
| The whole of a played battle in a window (the scripted captain, 173 s, through the storm and the plume, fighters, missiles and all) | median 11.8 ms (85 fps); slowest second 16.7 ms (60 fps); fastest 9.7 ms (103 fps) |

**The styles compared** (2026-11-10, after the decks, forms and fog; the same Mac, a 1920 x 1080 window, v-sync off,
`--print-fps`; the scripted captain playing each heavy battle through, 150 to 260 s; eleven plumes and both decks;
`none` draws no cloud, a measurement instrument only):

| Battle, renderer | No cloud | Billboards | Volume |
|---|---|---|---|
| `concord_heavy`, Forward+ | median 80 fps (12.5 ms), slowest 58 | median 80 fps (12.5 ms), slowest 54 | median 37 fps (27.0 ms), p10 30, slowest 18 |
| `kelvane_heavy`, Forward+ | | median 80 fps, p10 58 | median 36 fps (27.8 ms), p10 30 |
| `concord_heavy`, Compatibility (the phones' renderer, on the Mac) | median 80 fps, slowest 46 | median 76 fps (13.2 ms), p10 67 | median 61 fps (16.4 ms), p10 52 |

Every battle tops out at 80 fps with no cloud at all: the frame is held up on the CPU (the simulation, the console, the
scripted captain), and GPU work under about 12 ms hides beneath it. So the billboards cost nothing measurable here,
while the volume adds at least 14.5 ms a frame on Forward+ and at least 4 ms on Compatibility. As built, the volume does
not hold 60 fps even on the M4 Max with eleven plumes and both decks: as a high setting it needs its march at half
resolution, or fewer steps on the decks (task 2.21). The billboards are the low setting the owner asked for (owner,
2026-11-10: "we could save puffs for a lower end setting device like phones"), and on this evidence the default too until
the volume is cheaper.

**Making the volume cheaper** (2026-11-10; owner: "optimize ray march some more? 64 samples instead of 128 first. then
what else can we do?"). Measured by the still tool's `--frame-ms` (a measurement instrument: the view held for 6 s with
v-sync off and the time a frame read by the clock; the battle stands still, so the CPU's share is small; Godot's GPU
timer reads nothing on Metal), ms a frame at 1920 x 1080 on the M4 Max:

| View | No cloud | Volume, 128 steps | 64 steps, each longer | and the decks baked | and each column's shadow baked | Billboards |
|---|---|---|---|---|---|---|
| `concord_heavy`, wide (zoom 1.5, pitch 0.15) | 5.9 | 11.9 | 9.8 | 6.2 | 6.1 | 6.3 |
| `concord_heavy`, the chase camera | 5.9 | 21.7 | 17.0 | 10.4 | 8.9 | 6.5 |
| `kelvane_heavy`, in the storm | 10.1 | 17.4 | 14.0 | 10.8 | 10.7 | 10.8 |
| `ssarith_heavy`, low | 3.6 | 7.3 | 6.9 | 3.9 | 3.9 | 3.8 |

- **64 steps**: 64 plain steps cut the march short of the far side of a column (it showed thinner, with a hard outline);
  each step half again as long (`FINE` 0.028 to 0.05 radii) reaches as far, and looks the same beside 128.
- **The decks baked** (`DeckField`): measured with the decks off, the columns were about 3 ms of the chase view and the
  decks about 8. A deck sample looped over all sixteen columns three times (the funnel, the colour, and again in each
  light step). The funnels, the edge and the colours are now worked out once, when the columns are placed, into four
  256-texel textures over the field (`DeckField.Bake`), and the shader reads them. The puffs read the same field, so the
  deck's colour rule has one home again.
- **Each column's shadow baked** (owner: "do 1 first, skip 2, and try the half resolution trick with 128"): the battle's
  sun holds still, so the march toward it is done once when the plume is built, through the baked shape, into a 32-voxel
  texture (`NebulaPlume.ShadowTexture`: the cloud toward the sun, times the billows' mean share); the shader reads it in
  one fetch and keeps one near step through the noise for the billows' fine shadow on themselves. Three of the four
  noise reads of a step were the march toward the sun.
- **Half resolution at 128 steps, estimated** (the whole 3D view at half scale, which shrinks the ships too, so a cost
  estimate only), the chase view over its 5.7 ms floor: 64 steps 4.7 ms at full resolution and about 0.4 at half; 128
  steps 9.5 at full and about 2.4 at half.

- **Steps**: 64 along the view ray and 3 toward the sun on Forward+; 44 and 2 on Compatibility (phones), with the wisp
  octaves dropped. The phone path compiles from the same file but is not yet run or timed on a phone (task 2.10; survey
  T3 asks 60 fps of a mid-range 2022 phone, and a plume filling the screen is the worst case for it).
- **Memory**: the shared noise 1 MB once; each plume's shape 196 KB (64 x 48 x 64 bytes). A scene with two plumes: 1.4 MB.
- **Draw calls**: one a plume.
- **Load**: baking a plume's shape is 196,608 calls of `Cover` on the CPU when the battle is built, a few tens of
  milliseconds a plume.
- **Download**: `plume_noise.bin` is 1 MB in the repository and the export.

## The scenes

Four heavy-fleet scenes, three ships a side, each with two planets, one plume and one ion storm between the fleets
(which start about 105 km apart), and nine smaller plumes and puffs across the field:

| Scene | Player | Against | Sky, planets | Plume, storm |
|---|---|---|---|---|
| `concord_heavy` | 3 heavy cruisers | A Krai dreadnought and 2 heavy cruisers | Garnet front | garnet; cinder storm |
| `krai_heavy` | A dreadnought and 2 heavy cruisers | A Kelvane research vessel, carrier and battlecruiser | Brass squall | brass; slate storm |
| `kelvane_heavy` | A research vessel, carrier and battlecruiser | 2 Hunt cruisers and a carrier | Teal anvil | teal; slate storm |
| `ssarith_heavy` | 2 Hunt cruisers and a carrier | 3 Concord heavy cruisers | Orchid wall | orchid; cinder storm |

The player's ships carry a kit (missiles, a shuttle, fighters, mines where the class has the bay) so the scripted
captain's `all_out` beat has every system to use. The four Nebula scenes of `cloaking` now stand under one plume each,
with the new numbers.

## Open questions

| # | Question | Provisional answer |
|---|---|---|
| NP1 | Half accuracy and half energy range: full strength anywhere inside, or by how thick the plume is there? | By thickness: a thin plume or a plume's edge is milder |
| NP2 | From how far can a ship in a plume be locked? | 60 km, and from any range by a ship whose ECCM beats its ECM |
| NP3 | How badly do missiles fail in a plume? | A chance of 0.08 each second to lose the target: about half over 8 s of flight |
| NP4 | How often does an ion storm strike, how long is the core down, and how long before it can be struck again? | 3 times a minute a ship; 5 s; 15 s |
| NP5 | Does a strike also do damage? | No: the loss of power is the hazard |
| NP6 | Do AI captains avoid storms and use plumes? | Not yet: they fly through both |
| NP7 | The seven looks, the towers' shape and the lightning | As built, until the owner has seen the stills and the takes |
| NP8 | A sight line through a plume breaks a lock beyond what range, and does ECCM above the target's ECM still see through? | 20 km, and yes |
| NP9 | The decks: how far off the plane, how deep, how far they reach, how fast they fade into the sky | 18 km off, 20 km deep, whole 40 km past the outermost column, gone 90 km beyond; fading from 110 to 260 km off |
| NP10 | How many columns, puffs and of which forms in a nebula battle | Two big columns (the plume and the storm), five of 8 to 18 km in all four forms, four puffs of 4 to 6 km |
| NP11 | A nebula battle's sky in its plumes' colours, with a wall of cloud round the horizon | As built |
| NP12 | Which style draws the nebula: marched volume, mesh puffs or Crab Breeze's billboards? | Billboards: the most convincing cloud in the stills, and the cheapest |
