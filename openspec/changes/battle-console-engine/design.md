# Design: battle-console-engine

The battle console (mockup 2) in the game. The mockup's design (`openspec/changes/battle-console/design.md`) is the
design of what the player sees; this file says how the game is built to show it.

## 1. Two projects

| Project | Holds | Depends on |
|---|---|---|
| `src/Core/FleetCommand.Core.csproj` | `Data/` (strict JSON loader, typed records), `Sim/` (the battle, rules, orders, seeded random streams), `Console/` (the layout model) | .NET 8 only |
| `FleetCommand.csproj` (`src/Game`) | `Main` (composition root), `Ui/` (kit, panels, immediate-mode input, text), `View/` (3D view, overlay) | Godot 4.7.1 .NET, Core |
| `tests/Core.Tests` | xUnit tests over Core, loading the real `data/` files | Core |

The Godot project excludes `src/Core` and `tests` from its own compile and references Core, so the simulation can never
reach for a node.

## 2. The console in the engine

- **Layout**: `ConsoleLayout.Compute` turns `data/console/layout.json` and the window into rectangles, the one layout
  used by the game and by both layout tests. The project's base viewport is 1500 x 1080 with `canvas_items` stretch and
  `expand`, which is the mockup's scale rule (height sets the scale, 1500 px of width at most).
- **Panels** are `Control`s at fixed rectangles with `ClipContents`, redrawn every frame. Controls register their
  rectangles as they are drawn (`Imm`), and input matches the last frame's rectangles, as the mockup's canvas console did
  (battle-console design: "Drawn in the engine").
- **The kit**: `IKit` with the fixed list of piece, hero and icon names (`KitPieces`); `ConcordKit` draws each in
  vector, ported shape for shape from `docs/mockups/lib/concord.js`. `KitPieces.Verify` refuses a skin that lacks a name
  at startup. Drawing each piece whole at its size is the 9-slice contract without seams.
- **Text**: Antonio (OFL, `assets/fonts/`), shaped once per string and size, trimmed with an ellipsis to its room.
- **3D**: rendered full screen behind the console; the camera's frustum is offset so the commanded ship sits 74 % down
  the view rectangle at every zoom, without moving the camera. Expanding a panel moves the view rectangle to the right
  third and the battle keeps playing. Ships are stand-in primitives (`scenes/standins/`) until mockups 3 and 5.

## 3. The stand-in battle

Ported from the mockup's `step()`: fixed steps of `step_s` (1/30 s), streams forked per subsystem from the scenario's
seed (`hit`, `damage`, `ai`), ships visited in scenario order. One implementation each: `Rules.HitChance` (rolled by a
shot, shown on hover cards, the weapons table and the view's readout), `Rules.FacingFrom` (damage and the systems
display's white arc), `Rules.MaxSpeed` (movement and the locked speed boxes). The reactor's coils, warp, tractor,
transport and boarding are not ported yet, so `Output` is the reactor's rating.

## 3a. Checked against the mockup

Every built screen is compared side by side with the mockup's own named shot (`docs/screenshots/compare/`, made by
`tools/compare/side_by_side.py`), and the differences that remain are listed there. The game's stills use the mockup's
set-up for its scripted shots: the target locked, intercepting at full speed, a volley six steps before the end.

## 4. Costs

Measured on the cloud container's software renderer only (Mesa llvmpipe); no frame time is claimed (CLAUDE.md
section 9). The console draws about 2,000 vector primitives a frame; the scene is six stand-in ships of 3 to 6
primitive meshes and one sky shader. Phone budgets come with the phone layouts and mockup 3.

## Open questions

| Id | Question | Provisional answer |
|---|---|---|
| E1 | Keep the console drawn in vector every frame, or bake each kit piece to a texture per size? | Vector every frame; measure on the owner's phone at the first playtest |
| E2 | The camera follows the ship's position only (the player owns its yaw). Should it also follow the heading, as a chase view? | Position only, per CLAUDE.md section 8 |
