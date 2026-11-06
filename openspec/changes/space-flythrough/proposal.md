# Space fly-through: ray-marched nebula clouds and stars

## Why

Owner, 2026-11-05, after the menu's stars showed lines from a cheap hash: "never to use poor RNG stuff for stars, this seems
like a common low effort mistake", "can you make a better proc gen starfield?", then "write up a beautiful fly through of
space with ray marched nebula clouds and stars".

The game's skies are baked panoramas (nebula-skies) seen from far off. A fly-through shows what space looks like when the
camera travels through the clouds: volumetric gas with depth, dust lanes, stars buried in the gas lighting it, and a real
starfield behind. It is a candidate backdrop for the main menu and the loading screen, and a look reference for flying
through a nebula sector in battle.

## What changes

- Mockup 22, `docs/mockups/space-flythrough.html`: the fly-through, four palettes, quality levels, free look.
- `Starfield2D` (src/Core/Space): the seeded flat starfield the menu now draws, replacing the modular-hash dots.
- CLAUDE.md section 10: stars and every scatter come from a real generator, never a cheap hash.

Nothing in the game uses the fly-through until the owner approves it for implementation.
