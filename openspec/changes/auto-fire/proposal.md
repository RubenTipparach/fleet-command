# Proposal: auto-fire

## Why

The owner, 2026-11-08: "Add to spec, mockups and all current UI in game. Fire button should be divided into two sections.
Auto and Fire. Auto is a toggle that will enable the ship to fire all currently active weapons."

## What Changes

- **The Fire key is split in two: Auto, then Fire.** Auto is a toggle. While it is on, the ship you command fires every
  selected, armed weapon by itself the moment it is ready, in arc and in range of its target, with no press of Fire.
  Fire still fires on a press.
- The R key toggles Auto, as M toggles Multiple and Single.
- The same split on the weapons panel in every skin, in the game and in every battle console mockup (the faction pages
  are written from the Concord page by `tools/mockups/skin_page.py`).

## Impact

- `src/Core/Sim`: `Ship.AutoFire`, `Battle.ToggleAutoFire`, `Battle.AutoFireStep` (each step, after the helm).
- `src/Game/Ui/Panels.cs` (the weapons panel, every skin), `GameActions.cs`, `project.godot` (`toggle_auto`, R).
- `docs/mockups/battle-console.html` (`A.auto`, `autoFire`, the weapons panel, KeyR) and the 12 faction pages written
  from it.
