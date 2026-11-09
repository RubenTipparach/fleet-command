# Tasks: auto-fire

- [x] 1.1 Proposal, design and spec delta
- [x] 1.2 The simulation: `Ship.AutoFire`, `Battle.ToggleAutoFire`, `AutoFireStep` on `Fireable`, with tests
- [x] 1.3 The game: Auto and Fire on the weapons panel (every skin), the `toggle_auto` action on R
- [x] 1.4 The mockups: `A.auto`, `autoFire`, the split key and KeyR in the Concord page, the faction pages rewritten from it
- [ ] 1.5 The owner's review; F1, F2
- [x] 1.6 The console skins page (`docs/mockups/console-skins.html`): Auto, Fire and Multiple on the weapons row
- [x] 1.7 The Vhorr Brood's organic keys: `tools/mockups/vhorr_blockout.mjs` mended (it named a hero piece only by the
  placeholder kit's images, so it no longer found the helm pad once the sculpted kit was in; and it measured the page with
  every sculpted slot pinned to its old size, so a changed row read as unchanged: it now unpins the stretchable slots
  first). Re-measured, the weapons row only was taken into `assets/ui/vhorr/organic/blockout.json` (Auto 82, Fire 78.5,
  Multiple 223 px; the other slots differ from their sculpts by page changes older than this one, left for their own
  re-sculpt), and `key.auto`, `key.fire`, `key.multiple` and the weapons panel re-sculpted in Blender
  (`--only panel.weapons`, larva keys). G1 holds (every label at least 2.5 mm clear); console fit clear in every skin
