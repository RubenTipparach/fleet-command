# Design: battle-stars

## The data

`scenarios.json`, on a scenario with `planets`:

| Field | Values | Meaning |
|---|---|---|
| `star.kind` | `distant`, `in_map` | Where the star is |
| `star.class` | a spectral class (`O` to `M`) | Its colour (one table, `data/space/stars.json`) |
| `star.dir_deg` | azimuth and elevation | A distant star's direction; the battle's light comes from there |
| `star.size_deg` | degrees | How big a distant star's disc looks |
| `star.x_m`, `star.z_m`, `star.radius_m` | metres | A star in the map: its place and size |

Validation: a scenario with a planet and no `star` stops the load naming the scenario; an `in_map` star never stands on
the engagement zone's field of fire (it is outside the zone, or the zone is set round it) and never inside a planet.

## The view

- **Distant**: a disc and a glow in the sky at `dir_deg`, bright past the glow threshold so it blooms; the battle's
  `DirectionalLight3D` points from it, with its colour. The planets and plumes already read that light.
- **In the map**: a star body built like a planet (`PlanetBody`'s shells: a surface of granulation and spots, a corona),
  unshaded and emissive; the light's direction is from the star to the battle's middle (one directional light: at the
  field's scale the star's light is near parallel).

## Open questions

| # | Question | Provisional answer |
|---|---|---|
| BS1 | Does a battle with no planet show its star too? | Yes, when its sector has one; otherwise deep space as now |
| BS2 | Can a ship fly into an in-map star? | It cannot: the star is a hazard that destroys a ship (outside the zone by default) |
| BS3 | Does a near star change the rules (heat, sensors)? | Not in this change |
