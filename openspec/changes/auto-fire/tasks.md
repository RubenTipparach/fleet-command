# Tasks: auto-fire

- [x] 1.1 Proposal, design and spec delta
- [x] 1.2 The simulation: `Ship.AutoFire`, `Battle.ToggleAutoFire`, `AutoFireStep` on `Fireable`, with tests
- [x] 1.3 The game: Auto and Fire on the weapons panel (every skin), the `toggle_auto` action on R
- [x] 1.4 The mockups: `A.auto`, `autoFire`, the split key and KeyR in the Concord page, the faction pages rewritten from it
- [ ] 1.5 The owner's review; F1, F2
- [x] 1.6 The console skins page (`docs/mockups/console-skins.html`): Auto, Fire and Multiple on the weapons row
- [ ] 1.7 The Vhorr Brood's organic keys: re-measure the blockout and re-sculpt `key.auto`, `key.fire` and `key.multiple`
  at their new sizes (`tools/blender/build_ui_vhorr_organic.py --only ...`). Blocked: `tools/mockups/vhorr_blockout.mjs`
  no longer finds the helm pad on the page ("Cannot read properties of undefined (reading 'protected')", line 135),
  so the tool needs mending first. Until then the Brood's sculpted Fire and Multiple keys keep their old 192 px
