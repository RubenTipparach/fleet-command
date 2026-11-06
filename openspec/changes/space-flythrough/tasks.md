# Tasks: space fly-through

## 1. Mockup

- [x] 1.1 `docs/mockups/space-flythrough.html`: ray-marched clouds, seeded stars, the flight, palettes, quality, free look
- [x] 1.2 Shots in `docs/screenshots/mockups/space-flythrough/`, looked at (no bands, no grid, no grain after blending)
- [x] 1.3 Published, on the tracker, recorded in `docs/artifacts/space-flythrough/README.md`
- [ ] 1.4 The owner's approval for build, then for implementation

## 2. The starfield (done with the mockup)

- [x] 2.1 `Starfield2D`: Poisson-disc field, a wandering band, Gaussian clusters, the classes and colours of starfield.json;
  tests in `StarfieldTests`
- [x] 2.2 The menu draws it in place of the modular-hash dots
- [x] 2.3 CLAUDE.md section 10: stars and every scatter come from a real generator

## 3. In the game (after approval for implementation)

- [ ] 3.1 The menu's backdrop as the fly-through (a Godot shader on a full-screen quad, the same field and numbers)
- [ ] 3.2 A phone path (survey T3): quarter resolution, or a baked loop
