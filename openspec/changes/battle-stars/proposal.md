# Proposal: battle-stars

## Why

The owner, 2026-11-10: "if there is a planet there should be a star, either distant or in the same map. either is fine,
typically the star would be far off unless you stopped a sector that has a star init."

A battle's planets are lit by the battle's sun (`PlanetBody.Sync`, the scene's `DirectionalLight3D`, `sun_energy`), but
nothing in the sky or on the map is that sun. A planet with no star to light it reads wrong.

## What Changes

- **A battle with a planet has a star.** Either distant (the usual case): a bright disc with its glow in the sky, in the
  direction the battle's light comes from, so the light and the star agree; or in the map, when the battle is fought in a
  sector that holds a star (`campaign-space`): a star body on the field, the light coming from it.
- **The data says which**: a scenario's `star` (`distant`, with its colour and how big it looks; or `in_map`, with its
  place and radius). A scenario with a planet and no star is a data error. A campaign battle takes its sector's star:
  in the map when the sector holds one, distant otherwise.
- **The light comes from the star**: its direction (the sun's direction for a distant star, from the star's place for one
  in the map), its colour and its strength (`sun_energy`).

## Impact

- `src/Core/Data`: `StarDef` on a scenario, validated with its planets; the campaign's battle builder.
- `src/Game/View`: the distant star in the sky (`nebula_sky.gdshader` or its own billboard) and the star body (a planet
  kind's builder, `PlanetBody`, lit from inside), the battle's light set from it.
- Every scenario with planets gains a star; the mockups' scenes too (mockup sync).
