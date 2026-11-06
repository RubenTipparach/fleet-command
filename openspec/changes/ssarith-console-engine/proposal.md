# Proposal: ssarith-console-engine

## Why

The owner approved mockup 15, the battle console in the Ssarith Hunt skin, for implementation (2026-11-06, in conversation:
"Yes, approve both for the game"). The game draws the Concord and the Krai consoles; this change adds the Ssarith Hunt's, from the same panels, the way
`krai-console-engine` added the Krai's.

## What Changes

- `src/Game/Ui/Kit/SsarithKit.cs`: the skin's kit, a port of `docs/mockups/lib/ssarith.js` (its palette, colour roles, ink, type (VT323 and Black Ops One),
  key shapes, icons and its features), every piece from its rendered kit (`assets/ui/ssarith/manifest.json`), registered in `Kits`.
- `SkinStyle` gains the features this skin uses that the Krai did not: icon tabs on the square key (`IconKeys`),
  measured label fields (`KeyField`, `TabField`), a title offset (`TitleDy`) and strip needle meters (`Meter`, `Needle`).
- A new scenario, the Hunt's Battle (`ssarith_battle`): the Hunt's cruiser, lance frigate and carrier under its own names against the Concord, under its own sky (verdigris), so the Hunt's side is played in its own console.
- Its tabs have a measured label field (`TabField`).
- The skin parity check covers it.
