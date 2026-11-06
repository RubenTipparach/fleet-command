# Proposal: kelvane-console-engine

## Why

The owner approved mockup 20, the battle console in the Kelvane Institute skin, for implementation (2026-11-06, in conversation:
"I meant institute. Build institute into the game", "Yes, approve both for the game"). The game draws the Concord and the Krai consoles; this change adds the Kelvane Institute's, from the same panels, the way
`krai-console-engine` added the Krai's.

## What Changes

- `src/Game/Ui/Kit/KelvaneKit.cs`: the skin's kit, a port of `docs/mockups/lib/kelvane.js` (its palette, colour roles, ink, type (Share Tech Mono and Courier Prime),
  key shapes, icons and its features), every piece from its rendered kit (`assets/ui/kelvane/manifest.json`), registered in `Kits`.
- `SkinStyle` gains the features this skin uses that the Krai did not: icon tabs on the square key (`IconKeys`),
  measured label fields (`KeyField`, `TabField`), a title offset (`TitleDy`) and strip needle meters (`Meter`, `Needle`).
- Its strips carry needle meters (hull integrity on top, speed on the bottom) over their printed scales, and its keys and tabs have measured label fields (`KeyField`, `TabField`).
- It is no scenario's side yet: the Institute has no approved ships, and the scenarios use approved ships only (owner, 2026-11-04: "NO PLACE HOLDERS SHOULD BE USED FOR KRAI VS CONCORD"). Its console runs on any battle with `--skin=kelvane`.
- The skin parity check covers it.
