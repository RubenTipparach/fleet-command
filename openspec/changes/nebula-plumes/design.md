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
| `lock_km` | 60 km | A ship in a plume cannot be locked from farther than this, unless the locker's ECCM is above its ECM (owner: "can hide you from enemies") |
| `ew_gain_x` | 2 | How much more ECM and ECCM count in a full plume (unchanged from `cloaking`) |
| `edge_thin_frac` | 0.3 | How much thinner a plume is at its edge than its middle |
| `storm_strikes_per_min` | 3 a minute | How often lightning finds each ship in a full ion storm |
| `storm_shutdown_s` | 5 s | How long a struck ship has no power |
| `storm_grace_s` | 15 s | After its core restarts, how long no bolt finds that ship again |

One reader for each rule (CLAUDE.md 6.1): the chance to hit goes through `Rules.NebulaHitX` in the same function the
shot and the hover card call; every range (the shot, the in-range lamps, the weapon arcs, the AI, the scripted captain)
goes through `Rules.RangeKm`; a lock goes through `Battle.CanLock`.

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
  says how far inside the column a point is: the rule's circle on the plane, a lean as it climbs, a head that spreads
  (`anvil`), towers of different heights that round over, a foot that hangs to different depths. It is baked once per
  plume into a 64 x 48 x 64 texture of one byte a voxel. The shader multiplies it by the shared noise
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
  and cinder.

**Render order.** Planet layers draw at priorities -10 to -6, a plume at -1, everything else (ships, glows, beams,
shields) at 0. A plume reads the depth buffer, so opaque ships and planets hide the cloud behind them. Known limit: a
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
| The camera inside a plume, in a played battle | IN_PLUME_MS |

- **Steps**: 128 along the view ray and 3 toward the sun on Forward+; 44 and 2 on Compatibility (phones), with the wisp
  octaves dropped. The phone path compiles from the same file but is not yet run or timed on a phone (task 2.10; survey
  T3 asks 60 fps of a mid-range 2022 phone, and a plume filling the screen is the worst case for it).
- **Memory**: the shared noise 1 MB once; each plume's shape 196 KB (64 x 48 x 64 bytes). A scene with two plumes: 1.4 MB.
- **Draw calls**: one a plume.
- **Load**: baking a plume's shape is 196,608 calls of `Cover` on the CPU when the battle is built, a few tens of
  milliseconds a plume.
- **Download**: `plume_noise.bin` is 1 MB in the repository and the export.

## The scenes

Four heavy-fleet scenes, three ships a side, each with two planets, one plume and one ion storm between the fleets,
which start about 105 km apart:

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
