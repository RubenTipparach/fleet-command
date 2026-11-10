---
name: planet-shaders
description: Make, change and judge Fleet Command's planets and asteroids in Godot, the godot-sandbox way - a planet as a stack of seamless shells round a unit sphere (ground, crater shell, domed cloud layer, rings, a single-scattering atmosphere), every pattern on the sphere's direction in model space with no UVs or seams, relief as a height field differenced into a normal, gas giants as warped, sheared bands with storms and ring shadows, asteroids grown in the vertex stage from one ball mesh, the detail growing as the camera comes down (Falchion's octaves), looks in data/space/planets.json and asteroids.json, and stills from the planet gallery looked at before it is done. Use whenever making or changing a planet, a planet kind, a gas giant, rings, clouds, craters, an atmosphere, an asteroid or rock, or when a planet "looks flat", "looks painted", "has seams", "is too smooth up close" ("planet shader", "new planet type", "gas giant", "rings", "asteroid", "proc gen rocks", "clouds on the planet", "craters").
---

# Planet shaders

Owner, 2026-11-09: "We need some really good in game planet shaders. Read planet code from godot sandbox and falchion
(detail level). Make a slick planet skill mainly based on the godot sandbox stuff and port shaders over here. Make a few
different planet types ... gas giants with rings. We also need shaders for proc gen asteroids too."

The method is godot-sandbox's (`RubenTipparach/godot-sandbox` at `d5d8e461`: `resources/shaders/comic_dirt_ground`,
`comic_crater_overlay`, `comic_cloud_layer`, `comic_atmosphere_shell`, and `scenes/planet_body_3d.tscn` that stacks them).
The detail by distance is Falchion's (`RubenTipparach/falchion` at `5d128af8`, `src/render/planets/PlanetMaterial.ts`): more
noise octaves as the camera comes down. Provenance is in `.claude/skills/PROVENANCE.md`.

## Where things are

| File | Owns |
|---|---|
| `src/Game/View/PlanetBody.cs` | **The one builder of a planet**: its layers, their meshes and render order, the tilt, and `Sync` (sun and detail each frame). The battle and the gallery both call it; never build a planet anywhere else (CLAUDE.md 6.1) |
| `src/Game/View/PlanetLooks.cs` | Reads and validates `data/space/planets.json` into a `PlanetLook` and sets the ground and air materials (`Apply`) |
| `src/Game/View/planet_noise.gdshaderinc` | The shared noise (PCG3D hash, value noise, fbm, fbm by octaves), the tangent frame, the capped relief normal, the Fibonacci lattice, the day term and the ring density. Every planet shader and the asteroid include it |
| `planet.gdshader` | Rocky and lava ground (Falchion's: seas, caps, craters, canyons, volcanoes, night lights, sites), and the island chains (`islands`) |
| `planet_patterns.gdshaderinc` | What was built, or died, on a rocky ground (`pattern`): the city, the ruins, the bones. Included by `planet.gdshader` (below) |
| `planet_gas.gdshader` | Gas giant tops: bands, jets, eddies, storms, ovals, limb, ring shadow |
| `planet_craters.gdshader` | The crater shell (sandbox) |
| `planet_clouds.gdshader` | The cloud layer (sandbox) |
| `planet_rings.gdshader` | Rings |
| `planet_atmosphere.gdshader` | The single-scattering air (sandbox) |
| `asteroid.gdshader`, `Asteroids.cs`, `data/space/asteroids.json` | Procedural rocks |
| `PlanetGallery.cs` | The stills (below) |

The planet kinds are `PlanetDef.Kinds` (`src/Core/Data/Scenes.Comms.cs`); `planets.json` must have a look for every one and
no other. A new kind is a new name there and a new row there, nothing else.

## The rules (sandbox)

1. **No UVs.** Every pattern is a function of `normalize(VERTEX)` in model space, so there is no seam, no pinched pole,
   and the pattern turns with the planet. A `SphereMesh`'s UVs and tangents are never read.
2. **A shell per layer**, each its own sphere a little above the last, in render priority order: ground 0, craters 1
   (1.004), clouds 2 (a third of the air's height up), rings 3, air 4. A shell between features `discard`s, so the one
   under shows.
3. **Relief is a height field differenced into a normal** (`pn_frame`, `pn_relief`): four samples either side along two
   tangents, the step widened to the fragment's own footprint on the sphere (`max(step, fwidth-ish * 1.5)`, so the limb
   does not sparkle), the slope capped (`max_slope`, so no fine feature turns the surface inside out). Amplitudes of fine
   terms stay small: each term's slope scales with its frequency.
4. **A crater is a profile, not a painting**, shaped like a real impact (owner, 2026-11-09: "Craters didn't look natural
   close up"): a flat floor (wider in big craters) with a central peak in the biggest, a wall steepest just under a sharp
   rim crest, an ejecta blanket falling away outside it and fading into the plain, the rim wandering off the circle by a
   low noise, a fine rubble relief on the floor. Craters sit on a jittered Fibonacci lattice in three sizes, big first,
   small stamped over. **The crater shell multiplies the ground (`blend_mul`), it never paints a colour of its own**: the
   ground's texture runs on through every crater and only the light on its slopes and a slight tint change. A crater
   painted in palette colours read as a dark disc stuck on the plain.
5. **A cloud has tops**: its height is the square root of the density above the edge (fast climb at the rim, round
   over the top) plus a finer billow inside; its edge thins over a soft fringe, never a cut line. Two layers turn against
   each other about a wind axis.
6. **One sun, one light.** Every layer is `unshaded` and lit by the same `sun_dir` uniform (toward the sun, world space)
   and `sun_energy`, set every frame by `PlanetBody.Sync` from the battle's `DirectionalLight3D`. That keeps Forward+ and
   Compatibility identical. The light itself is one function, `pn_lit(col, day, ndl, air, sun)`, which the ground, the
   clouds and the gas tops all call: the day term (`pn_day_air`), a narrow, lightly reddened band at the terminator where there is air (`pn_twilight`), and
   the night side's fill (`PN_NIGHT_FILL`). On the day side a relief may shade a layer, never
   black it: shade relative to the sphere's own light (the clouds), or multiply by the ratio of the slope's day term over
   the plain's (the crater shell).
   **The terminator is a strong line, made in the shader** (owner, 2026-11-10: "Stronger terminator lines. i would prefer
   it be done in the shader"). Two things make it one, both in `pn_day_air`: the band is narrow (a few degrees under air,
   under one on an airless world), and the day's body is already at two fifths of full day at the line
   (`0.42 + 0.58 * pow(ndl, 0.6)`), so the fall into night is a step and not a long fade. A wide band or a day that dims
   toward the line reads as a soft gradient however dark the night is.
   **The night keeps a minimal fill** (owner, 2026-11-10: "keep fill.lights please. Just minimal. But I'd like to somewhat
   be able to make out features"): a cold light of about a two hundredth of the day, enough to make out land, seas,
   weather and bands in the dark and no more. It is linear light (a hundredth of the day already shows as a tenth grey),
   and it multiplies the surface's colour, so a bright world (ice, cloud decks) shows it more than a dark one. Check it by
   measuring night-side pixels in the crescents: a few units of 255 on a dark world, under about 20 on the brightest.
   **The air's rim is one function, `pn_rim`**, on the ground, the cloud tops and the gas tops alike, and it follows the
   day: bright on the lit limb, fading through the terminator, a trace (a twentieth) round the night side (owner,
   2026-11-10: "The fresnel on dark sides of planets is a little too strong"). Never write a rim inline in a layer.
   **Relief moves the terminator** (owner, 2026-11-10, with a photograph of a crescent Earth: "terminator line, normal
   maps? do we have those?"). The normals are each layer's own height field differenced (rule 3), never a texture, and
   the day term is steepest at the terminator, so a layer with relief gives `pn_day_air` its slope's own light:
   `ndl + clamp(turn * terminator_relief, lo, hi)`, where `turn` is the relief normal's light less the sphere's. Along
   the terminator a face toward the sun is lit and one turned away is in night, which is what picks out crater rims,
   ranges and cloud tops there. Clamp the shift to about the band's own width (ground -0.05 to 0.04, clouds -0.06 to 0.03), or lit islands float deep
   in the night and holes open in the day. The ground's gain stays moderate (0.7): its bump is a screen-space derivative of its height, and the
   noise's lattice shows as blocks when it is pushed. Ease the relief in over the coast with the land mask, never
   switch it on at a threshold. **Take `fwidth`, `dFdx` and `dFdy` outside any branch** and pass them in: inside
   `if (land > 0.5)` they are undefined along the branch's edge, which drew every night coast as a line of city lights.
7. **Hashes are PCG on the float's bits** (`pn_hash31`), never `fract(sin)` or a fract-multiply hash (CLAUDE.md 10).

## Realism (owner, 2026-11-10: "make planets more realistic please")

What makes a world read as a place and not a painted ball. Each of these was a fault found in a still first.

- **Land and sea are one branch, and the coast is never sharper than a pixel**: `land = smoothstep(-aw, aw, h - sea)` with
  `aw` from `fwidth(h)`. The sea darkens with depth (a pale shelf at the shore, deep water at 0.55 of its colour), and only
  water takes the sun's glint (a tight lobe and a faint wide one, tinted at the terminator).
- **Biomes come from climate, not from height alone** (`climate` in the look): rainfall is a low noise plus belts by
  latitude, less on high ground; wet land is the palette's green, dry land `arid`, cold land `tundra`, with a dry band at
  the coast. Greens stay muted (`#3a6331`, not a lawn's). A dry world (`climate` 0) keeps the old height ramp.
- **Mountains are ridged ranges** (`mountains`), which carry the snow line; **rifts are arcs and `lineae`** (thin cracks on
  an ice world), not a noise threshold.
- **Night lights are small tight clusters** near coasts and lowlands, never a glowing continent.
- **Lava**: a dark crust broken into bent plates at two scales, its joints glowing only where the heat field says so,
  molten lowlands below `sea`, and each volcano a lake in its caldera with one or two broad tongues that cool along their
  reach. Rings round a volcano read as a bullseye and noise worms read as spiders: both were tried and thrown out.
- **Craters are shallower the bigger they are** (`deep`: big 0.36, medium 0.62, small 1), as real ones are.
- **Clouds have body, and their slopes are eased, never cut**: `ease = 1 / (1 + g / max)` before `pn_relief`. A slope cut
  at a cap gives every steep top one flat tone with a hard edge. Fine billows stay small (`billow_strength` 0.12), the
  relief step narrows only when the camera is near, weather follows belts by latitude with twisted storms, and the deck
  wears the same rim of air as the ground under it (`atmo`, `atmo_strength`), or the ground's rim shows through as patches.
- **Up close** (`close`, from `PlanetBody.Sync`) the ground gains two octaves of grain and the rim of air fades, so the
  surface is not a smooth gradient under the planet view.
- **Colour is restrained.** Palettes are the dusty, desaturated colours of real worlds; the looks' numbers are all in
  `data/space/planets.json` (its `_doc` names each), never in a shader.

Not measured: the ground shader costs more than it did, and no phone frame time has been taken for it yet.

## Worlds with something on the ground (owner, 2026-11-10: "city planet, archipelego, fossil planet (lots of bones). and an old alien ruins planet")

Four kinds after Falchion's world types (`src/content/planetLooks.ts`, `src/render/planets/patterns.glsl.ts`):

| Kind | In `planets.json` | From |
|---|---|---|
| `city` | `"pattern":"city"` | Falchion's `ecumenopolis` (its PATTERN 1), ported |
| `archipelago` | `"islands":1.0` | New here (Falchion has none by the name): chains of small islands along ridged crests, in shoals |
| `fossil` | `"pattern":"bones"` | Falchion's `bones` row for the ground (its PATTERN 4); the skeletons are new here (Falchion's bones are props) |
| `ruins` | `"pattern":"ruins"` | New here, after Falchion's `deadtech` (dying lamps) and `choir` (the pulse). Tied to no faction: who built it is the owner's to say (CLAUDE.md section 1) |

A pattern is a rocky ground's last coat (`pp_surface`, called before the grain): it recolours the ground, adds to the
emission and gives a `rise` that joins the relief. Craters and canyons cut through it (`clear`). The rules it is drawn by:

1. **A cube, even in angle.** Cells sit on the six faces of a cube (`pp_cube`: the tangent undone, so a block at a face's
   corner is the size of one at its middle; `pp_dir` goes back). Every cell count is a whole number to the cube unit, so
   cells meet the faces' edges.
2. **The pixel's size is passed in** (`px`, taken once by the caller outside any branch), never `fwidth` inside a
   pattern: a derivative across a face's edge or inside a loop is wrong or undefined.
3. **Nothing finer than a pixel is drawn sharp.** A line narrower than a pixel is drawn a pixel wide at its share of the
   brightness (`pp_band`, `pp_line`); a cell's number goes to the mean as the cell shrinks under a pixel (`pp_soft`); a
   level of detail fades out as its cells do (`pp_fade`) and its loop stops there. So nothing sparkles from afar, the
   brightness holds at every distance, and the cost falls with distance.
4. **Detail in levels, each a whole multiple of the last** (the city's streets split seven times; the bones come in seven
   sizes, the ring works in four), so there is always something a few pixels wide from orbit down to the planet view's
   closest.
5. **A grid must not read as a grid from orbit.** The city's avenues are bent by a low noise (zero at the faces' edges,
   so they still meet), its open ground is chosen block by block from a noise, its districts differ in tone. A regular
   cube grid read as a disco ball.
6. **Emission is bright under the game's glow.** The city's night is about a third of Falchion's strength, and its
   lights follow a density (thick round old cores, thin between), or the night side is one saturated sheet.
7. **A thing has a state, not only a shape**: each arc of a ring work stands or has fallen by its own number, the ruins
   are buried by a drift mask and broken by a crumble noise, the skeletons are half in the ground. Whole clean shapes read
   as a blueprint.

No frame time has been taken for the patterns on a phone.

## Gas giants and rings

- Bands by latitude (`bands` pairs), each band's shade drawn from its own number (no two pairs repeat), edges torn by two
  passes of domain warp sheared along the latitude (`turbulence`), streaks drawn out along the latitude at more octaves up
  close, jets that alternate by band (`flow_speed`), a great storm (`storm_lat` as a sine, `storm_size`, its longitude
  from the seed) in the palette's fourth colour, small white ovals that twist the flow, darker hazy poles, limb darkening.
- Rings are a flat square in the equator plane cut to the annulus; their density by radius (`pn_ring_density`) is shared
  with the giant's tops, which take the rings' shadow by casting the sun's ray to the ring plane. The rings take the
  planet's shadow by the same ray against the unit sphere. Lean the planet (`tilt_deg`) so the rings open toward the
  camera; the whole stack and the installations lean with it (`PlanetBody.Body`).

## Asteroids

One shared ball mesh; the shape is grown in the vertex stage: low noise at two scales (`lumpiness`), up to four flat split
planes (`facets`: the ball stops where it meets each), crater bowls with rims. The vertex normal is the shape differenced
across the sphere; the fragment mixes it half and half with the screen's facet normal, adds the sandbox ground's grain and
pebbles as relief, and colours by two rock colours, dust on the high ground, darker crater floors. Rocks are lit by the
scene's lights like ships (not unshaded). Seeds go through FNV-1a so neighbouring rocks share nothing.

## Look, then call it done

```sh
.tools/godot/Godot_mono.app/Contents/MacOS/Godot --path . --resolution 1280x800 -- --planet-gallery=docs/screenshots/game/planets
scripts/game-stills.sh docs/screenshots/game ms-planet-battle,ms-planet-terran,ms-planet-ocean,ms-planet-desert,ms-planet-ice,ms-planet-lava,ms-planet-barren,ms-planet-gas,ms-planet-ice_giant,ms-planet-toxic,ms-planet-city,ms-planet-archipelago,ms-planet-fossil,ms-planet-ruins,ms-planet-surface,ms-planet-low-city,ms-planet-surface-city,ms-planet-low-fossil,ms-planet-surface-fossil,ms-planet-low-ruins,ms-planet-surface-ruins,ms-planet-sites,ms-planet-sites-close
```

The gallery saves every kind from afar, up close and as a crescent (the sun round behind, so the terminator crosses the
view) and every rock kind, through `PlanetBody` itself, with a third of each disc in night from afar. Its folder is under `res://`, so give it a path inside the repository. A shader that fails to compile may
print nothing: the stills are the check. Look at each still
(make a contact sheet) for: seams or pinched poles, sparkle at the limb, a black patch in a lit layer, bands that repeat,
a ring shadow on the wrong side, rocks that read as balls; and in the crescents, a terminator that fades instead of stepping, a night side that is
black (no features to make out) or brighter than a faint fill, a rim glowing round the dark limb, lit
patches floating in the night, a coast outlined in lights, blocks or stripes along the terminator. Then the battle stills, which show the same planets under a
battle's sun and in the planet view down to the surface. A world with a pattern is also looked at from low over it
(`ms-planet-low-<kind>`, `ms-planet-surface-<kind>`): its finest levels must still hold something, and from afar it must
not sparkle, show the cube's faces or read as a grid.
