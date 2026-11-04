# Design: weapon-fx

## What each faction's light says

The Ship Design Bible fixes what a colour means in damage (orange fire, cyan-white coolant, violet plasma, sparks for
power); the console skins fix each faction's palette and geometry. The weapons follow both:

| | Concord | Krai Warhost |
|---|---|---|
| Light | Clean light: Sky (`#3fa9f0`) with Lilac strands (`#b7a6f2`, the many species), a white core | Heat: blackbody from deep red through forge orange (`#ff8a2a`) to white; blood red (`#d02a20`) only on edges and the bayonet |
| Geometry | Round: a lens flare with a long thin streak, a hex shield lattice, round motes | Triangles: a three-bladed star flare, a triangle shield lattice, a three-sided spike |
| Motion | Calm and exact: strands that wind, a beam that holds | Violent: a pour that flows, a recoil, a shock that tears |

Standard weapons take their colour from `data/battle/weapons.json`, the same for every faction: the lance blue, the
disruptor red, the pulse torpedo a strong purple (owner: "torpedos should be purple", "stronger purple please"), point
defence gold; missiles by grade, red, orange, blue, green (`data/battle/missiles.json`). No weapon is drawn white (owner:
"please avoid the color white for weapons"): a hot core is the weapon's own colour at full strength (`hotOf`).

## The pipeline (`FCWeaponFX(...).pipeline`)

1. The sky, then the battle, into one HDR target (half floats; 4 x MSAA on WebGL 2).
2. The heat haze, into its own half-resolution target as screen offsets (red x, green y) from flowing noise.
3. Bloom: three's UnrealBloomPass over the HDR target, its own targets in half floats too (in bytes its wide blur stepped
   into visible rings round a flash, seen on the first shots); what goes into it is clamped at 2.5, because a flare far brighter
   than that overran the blur's kernel and drew a square round itself.
4. One final pass: the scene sampled through the haze, a little chromatic aberration where the haze bends it and at the
   edges, ACES tone mapping (three's fit), sRGB, a soft vignette, and a one-step dither so slow gradients never band.

The space backdrop (`lib/space.js`) writes display colours, so the range turns them back to linear before the one tone
map. Quality **Phone**: bloom at half resolution, no haze pass, no aberration, no MSAA, pixel ratio 1.

## The effects

| Effect | Shader | Used by |
|---|---|---|
| `ribbon` | A strip turned about its axis to face the camera, so a beam reads as a round column from any side. Lance (every faction's, in the weapon's own colour): a white core, flowing light and pulses racing to the target. Bolt (the disruptor): a short streak, dim and narrow behind, a hot core at the head. Chorus: a white core in Sky glow, flowing fbm light, Lilac strands winding round it (two, plus one a joined ship), a bright node where joining beams meet it, travelling rings; a front that races out at 7 km/s. Join: thinner, one strand. Barrel: blackbody along the spinal cannon as it heats | Chorus, its joiners, the forge's charge |
| `flare` | Camera-facing. Lens: core, long horizontal streak, faint halo ring. Tri: three blades turned with the ship. Six: the pulse torpedo's star, three thin beaded diffraction spikes at 60 degrees with warm and cyan fringes, a faint horizontal strut and a streaked hot core (owner: "pulse torpedo needs that iconic 6 armed flare"). Ball: a missile's plume | Every muzzle and impact |
| `shield` | An ellipsoid half again as large as the hull's box with its pad (owner: "I would make the shield bubble 50% larger"), in six 60 degree facings counted as the battle counts them. Invisible at rest (owner: "shield bubble invisible unless hit"); a hit lights a hot spot, a ring that runs out over the surface and the lattice round it, dimmer on a weaker facing; a failing facing flashes and its cells dissolve by noise | Every ship |
| `scars` | Not a decal: the hull's own materials are patched (`onBeforeCompile`), in ship space, so a scar rides the ship and nothing z-fights (CLAUDE.md 10). Molten crater: a white-hot pool cooling by blackbody to crust, voronoi fissures glowing forge orange to twice its radius and fading last (the Krai's own fissured metal); stretched along its direction for a ram's gouge. Beam burn: a white spot in a ring of char. Quell lattice: Lilac and Sky cells growing over the whole hull at 60 m/s, light pulsing along them, every glow under them dimmed until the stun ends, then flickering back | Forge, ram, chorus, quell |
| `orb` | Two counter-turning icosahedral shells (Lilac, Sky) round a soft core | Quell |
| `spike` | A three-sided cone of flowing blackbody metal, white at the point | Forge |
| `trail` | A camera-facing strip of the last positions, narrowing and fading; light or heat | Missiles (a long tail behind the plume), the quell, the forge |
| `beads` | A discrete trail: a glowing bead dropped where the shot is every few hundredths of a second, each shrinking and fading where it was left (owner: "torpedo should have like discrete trail instead of a long streak") | Pulse torpedoes |
| `dart` | A missile: a plain four-finned body at the front, a big boiling ball of the firing faction's drive light behind it (owner, with the reference game's missiles: "that 3d missile model at the front end, a big ball plume, and a tail") | Missiles, every faction |
| `plume` | An open cone with standing shock diamonds that tighten and lengthen with the push | The Krai drives in a blood charge |
| `bowShock` | A paraboloid shell before the bow, lit at the rim, streaming back; red rim to an orange-white nose | Blood charge |
| `ring` | A thin ring that breaks up as it runs out, square to the struck surface | Every impact |
| `haze` | Screen offsets in the haze pass | Muzzles, the spike, the beam's end, the charging bow |
| `motes` | Points spiralling in from a ring and brightening | The chorus's charge round the saucer |

Fire, sparks, embers, arcs, shards and weapon light are the shared `lib/ship-fx.js`'s (`emit`, `flash`, `debrisAdd`,
`fxLight`), never copied (CLAUDE.md 6.1).

## Beams and bolts keep their ends on their ships

The owner, 2026-11-04: "source and destination of phaser and disruptors must maintain in sync", "as they fire, the source
and destination of these beams must follow the ship source and ship destination", "a missile fired can break this rule as
it leaves behind a particle trail".

- **Beams** (lances, point defence, the chorus): each step both ends are resolved again: the mount on the firing ship as
  it now stands, and the target as it now stands (the hit point is held in the target's own frame; a miss lands beside
  it, riding with it; a point defence beam ends on the shot it fires on). An end whose ship is gone stays where it was.
- **Direct-fire bolts** (the disruptor, the forge spike): they fly, but along that live line, so they stay between the
  mount and the target as both move, and land when they have run its length.
- **Seekers** (missiles, torpedoes, the quell): they fly free, turning toward the target, and leave their trail in space.
- One rule a place: `shotEnds` in the battle console mockup, `Battle.Ends` in the game (both draw and fly by it), and the
  range's runs. Pinned by `A_beam_keeps_its_ends_on_the_firing_mount_and_the_target_as_both_ships_move` and
  `A_disruptor_bolt_flies_along_the_live_line_between_its_mount_and_its_target`. The SFC2 manual counts photon torpedoes
  as direct fire (printed page 22); ours fly and can be shot down, so they are seekers here, provisionally (X6).

## Where the effects are used

- **The weapons range** (mockup 12): every standard and special weapon, the full pipeline. One fixed view, side on,
  that never turns or follows (owner, 2026-11-04: "no rotation on on camera please. I just want to see the fx fired
  between two ships"); the ships hold station face to face and drift a few metres so the beams can be seen riding them;
  the view's distance fits the ships and their shields into the part of the screen the panels leave free, and the
  projection is shifted to its middle ("Kinda hard to see the ships on the phone. Make sure zoom is appropriate").
- **The battle console** (mockup 2 v77, and mockup 8 v16 written from it): its lances and point defence as `ribbon`
  lance strips, its disruptor bolts as `ribbon` bolts on the live line, its torpedoes and missiles as `flare` warheads with
  a `trail`, sized in battle units; that page has no bloom pass, so the range shows the full look.

## In Godot (after approval)

| Mockup | Godot 4.7 |
|---|---|
| HDR, bloom, ACES | `WorldEnvironment`: glow and ACES tone mapping (Forward+); the Compatibility renderer's glow, measured on the phone (X7) |
| Heat haze | A spatial shader reading `hint_screen_texture`, desktop only |
| `ribbon`, `flare`, `ring`, `trail` | `MeshInstance3D` quads and strips with unshaded additive spatial shaders, billboarded in the vertex function |
| `shield` | A sphere mesh with its own spatial shader, uniforms set by the battle's shield state |
| `scars` | Uniform arrays in `src/Game/View/hull.gdshader`, the one hull shader |
| Particles | `GPUParticles3D` on Forward+, `CPUParticles3D` on Compatibility |

## Cost (measured from the range page, and its geometry)

| Effect | Triangles | Draw calls |
|---|---|---|
| A beam (`ribbon`) | 192 | 1 |
| A shield bubble | 5,040 | 1 a ship |
| A flare, a ring, a haze sprite | 2 | 1 each |
| A trail of 60 points | 118 | 1 |
| A drive plume | 1,536 | 1 a drive (5 on the Krai frigate) |
| The bow shock | 2,304 | 1 |
| The chorus's motes | 700 points | 1 |
| Scars | 0 | 0 (in the hull's own draw) |

The range's busiest view (the chorus with two joiners, five ships) draws about 75,000 triangles in about 120 calls, most
of them the ships. The passes: the scene, the haze at half resolution, bloom's five mip levels (about eleven small
full-screen passes) and the final pass. The scars' price is per pixel of hull: eight slots, each an fbm and, for a molten
crater, a 27-cell voronoi; on phones that becomes four slots without the voronoi fissures (provisional, X7). A cloud
session measures no frame time (CLAUDE.md 9): the page's meter shows it on the owner's machine and phone.

## Open questions

| # | Question | Provisional answer |
|---|---|---|
| X6 | Torpedoes: seekers that fly free and leave a trail, or direct fire on the live line? | Seekers |
| X7 | The split: High on desktop (bloom, haze, aberration, MSAA, eight scars), Phone on Android (half bloom, no haze, four scars)? | As built |
