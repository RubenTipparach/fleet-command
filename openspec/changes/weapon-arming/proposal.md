# Proposal: weapon-arming

## Why

The owner, 2026-11-04, answering the weapon-targeting change's recommendation "Weapon arming choices per type: proximity
torpedoes (a wider block, lower damage), overloaded lances at short range only": "2 cool."

## What Changes

- **A fourth arming mode, Proximity**, on the weapons panel and each weapon's details: a torpedo fused to burst beside
  its target, easier to land on a fast or jinking ship, for less damage, over a wider chopping block.
- **Overloaded lances**: lances can now overload, for more damage at 4 km or less.
- A mode a weapon does not have is shown and refused, and the hit details name a weapon's mode.
- **The Arming scene**: two fast frigates jinking in close.

## Impact

- `docs/mockups/battle-console.html` (both skins): `ARM_MODES`, the modes on `WEAPONS`, `spec()`, `canArm()`,
  `hitChance()`, `exposure()` and `damage()` with a reach, the arming tabs, the Arming scene, shots `arming`, `arming_hits`.
- Mockup only.
