# Tasks: placeholder-fleets

## 1. Mockup

- [x] 1.1 Proposal, design and spec delta
- [x] 1.2 `data/ships/placeholders.json` from `tools/factions/placeholder_fleets.py`: 36 classes in nine factions
- [x] 1.3 `docs/mockups/lib/placeholder-hulls.js`: the body loft, the limbs, drive recesses and stripes
- [x] 1.4 Placeholder emblems: `assets/emblems/<faction>/placeholder.svg` and `assets/emblems/placeholders.json` from
  `tools/factions/placeholder_emblems.py`, with their `.import` files
- [x] 1.5 Mockup 36, `docs/mockups/placeholder-fleets.html`, shot at desktop, phone and portrait sizes and looked at
- [x] 1.6 The owner's review: approved for implementation (2026-11-08, "cool start implementation"); PF1-PF3 stand as recommended

## 2. In the game (once approved for implementation)

- [x] 2.1 Export the 36 hulls to glb with `tools/mockups/export_ships.mjs` (`exportPlaceholdersInPage`): a node per piece, an
  engine empty, manifest entries whose break blows each piece off whole; schematics by `tools/mockups/ship_schematics.py`
- [x] 2.2 Classes in `data/battle/classes.json`, the nine factions in `data/battle/factions.json` and `data/campaign/campaign.json`
  with their hulls, by `tools/factions/placeholder_classes.py`; each class's numbers and mounts are its role's approved class's
  (frigate `ff`, destroyer `dd`, cruiser `ca`, carrier `kelvane_cv`, capital `kelvane_bc`); the older stand-ins `kff`, `kdd`
  and `vcr` now draw the Kethran and Vos placeholder hulls
- [x] 2.4 The emblems on the faction screen, every faction in a grid (`faction-select` 3.5)
- [x] 2.5 Hits on a placeholder: fireballs where shots land on its plain surfaces (`ShipDamage` samples them when a ship has
  no hull shader), its pieces blown off whole in the breakup
- [ ] 2.6 Hull scars: the placeholders take no scorch or hole marks (they have no hull shader)
- [ ] 2.3 Replace each faction's placeholders when its ship mockup and emblem are approved
