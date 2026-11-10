## 1. Mockup (design stage)

- [x] 1.1 The low-hull effects on every ship in the battle console page (`lowHullStep`: smoke, fire, sparks, scraps, one pool)
- [x] 1.2 The warning band, one component for both warnings (`warnings`, `warnBands`), the hull bar's label, a tap to the page that deals with it
- [x] 1.3 The reactor's count read through one rule (`reactorCritical`, `MELT_RECOVER_X`) by the step and the band
- [x] 1.4 Shots `low_hull`, `reactor_unstable`, `damage_warnings`; the 12 faction pages regenerated (`skin_page.py`, `--check` passes)
- [x] 1.5 Looked at: the Concord desktop and portrait, the Krai and Kelvane bands
- [ ] 1.6 Publish mockup 2 and the 12 faction pages; the owner's review (approval for build, then for implementation)
- [ ] 1.7 The owner's answers to Q1-Q6 (survey)

## 2. Game (after approval for implementation)

- [ ] 2.1 `data/battle/tuning.json`: `low_hull` and `warnings` (design D5), validated on load; `Panels.cs`'s 0.35 reads `warnings.hull_below_frac`
- [ ] 2.2 A pooled particle emitter in `Fx.cs` (four MultiMeshes, one per kind), stepped by `BattleView`
- [ ] 2.3 `ShipDamage`: the burning places from the ship's own hits, seeded places to fill in; sized by `ship_frame_x`
- [ ] 2.4 `Battle.MeltdownLeftS(ship)` and `Battle.ReactorCooling(ship)`, reads of the existing count, with tests
- [ ] 2.5 `Panels.WarningBand`: the band, both warnings, flash, tap, hover card, the hull bar's label; every skin
- [ ] 2.6 The red alert cue once on each warning's onset (`data/audio/cues.json`)
- [ ] 2.7 Options: Damage effects, Full or Reduced
- [ ] 2.8 Layout and parity tests: the band inside the view in every skin and state; a playtest (`damage-warnings`) taking hull and coolant hits, stills beside the mockup's shots (`tools/compare/side_by_side.py`), `docs/design/mockup-sync.md` updated

## 3. Check

- [ ] 3.1 `scripts/run-tests.sh`, `scripts/playtest.sh`
- [ ] 3.2 Game stills at desktop and phone, landscape and portrait
- [ ] 3.3 A recorded take on Drive
- [ ] 3.4 Frame time on the owner's phone with every ship burning (240 particles): 60 fps held
