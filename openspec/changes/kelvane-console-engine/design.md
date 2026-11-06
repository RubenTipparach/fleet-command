# Design: kelvane-console-engine

The game's console draws every skin through one set of panels (`krai-console-engine`, decision 1: a panel picks a role
from `SkinStyle` and a piece by name from `IKit`, never a skin's colour or shape). The Kelvane Institute's kit is one more `IKit`.

## Decisions

1. **A port, field for field.** `KelvaneKit` takes every value from `docs/mockups/lib/kelvane.js`'s `api` object and its `ink`,
   `keyInk` and `font` functions, and every icon shape for shape on the 48 x 48 grid, so the game draws what the owner
   reviewed. Its pieces come from the Blender kit's manifest through `RenderedPieces`; console names the mockup maps onto
   other pieces are mapped the same way in the kit.
2. **New features live in `SkinStyle`, not in the panels.** `IconKeys`, `KeyField`, `TabField`, `TitleDy` and the strip
   meter (`Meter` with the skin's `Needle`) are read by the shared `Button`, `Tab`, `Frame` and the strips
   (`StripNeedle`), so a skin that lacks a feature leaves it null and every other skin is unchanged.
3. **The helm keys' glass is measured** from the kit's own key renders (`RenderedPieces.Glass`), and their content is
   laid out by `HelmKeyLayout` (owner, 2026-11-06: "the numbers arent in the buttons for turn angle, also these icons need
   a little more space on these buttons").

## Costs

The kit is images the game already ships (`assets/ui/kelvane/`), loaded once when the skin is first worn; its icons are
vector, drawn per frame like the other skins'. No frame time is measured in a cloud session (CLAUDE.md section 9).

## Open questions

| # | Question | Provisional answer |
|---|---|---|
| 1 | Hover cards in the skin's own card shape, as the mockup's `tip` piece? | Later, with the Krai's (krai-console-engine 2.3) |
