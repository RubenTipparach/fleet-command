# Tasks: ship-schematics

## 1. This change (2026-11-05)

- [x] 1.1 `tools/mockups/ship_schematics.py`, two runs byte-identical; `assets/ships/schematics/` with `manifest.json` and `README.md`
- [x] 1.2 The contact sheet `docs/screenshots/schematics/sheet.png`, looked at
- [x] 1.3 `Schematics.cs`; the systems display draws a class's schematic; the shipyard, fleet and outfitter show each hull's
- [x] 1.4 A test that every campaign hull with a schematic has both layers

## 2. Still to do

- [ ] 2.1 The battle console mockup's systems display and the campaign mockup's shipyard show the same pictures (mockup sync)
- [ ] 2.2 A check that each schematic's recorded glb sha256 matches the glb (a stale schematic fails)
- [ ] 2.3 Weapon mounts placed by the manifest's origin and scale rather than the class's half-length mount positions
