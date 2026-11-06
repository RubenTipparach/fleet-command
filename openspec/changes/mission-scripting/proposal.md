# Proposal: mission-scripting

## Why

The owner, 2026-11-05: "start building out mission scripting so we can write up mission files outside of godot, pick a ui
package that works good for windows/mac", and "implement game flow into the game, but I want you to implmenent missions as
well". The campaign mockup's missions were JavaScript functions inside the page (`docs/mockups/lib/campaign-missions.js`):
only someone editing the page could write one, and the game could not read them.

## What Changes

- **Mission files**: one JSON file per mission in `data/missions/`. Its rules are small expressions in strings, checked
  when the file loads; text fills in `{expression}` parts. The format is `docs/design/mission-files.md`.
- **One loader and evaluator in Core**: `src/Core/Campaign/Expr.cs` and `MissionScript.cs`. The game, the tests and the
  editor all read mission files through it. A file naming something its scope does not have, an unknown key or a bad
  expression stops the load, naming the file and the field.
- **Every mission of the mockup as a file**: the fifteen scripts (patrol, fleet action, dispatch run, stem the tide,
  capture, capture the prize and its delivery, distress call, station and planet assault, convoy ambush and defense,
  assassination, shakedown and first blood).
- **Missions in battle** (`MissionRun.cs`):
  - a mission's battle is planned into a Scenario the simulation plays, with its groups placed by bearing and range;
  - when the battle ends, every objective's `done` is checked and the outcome graded (the five grades);
  - the result is applied: credits, prestige, standing, a prize, the next missions, damage and losses.
  - The simulation gains a prize that strikes when beaten down to `capture_hull_frac` (it is boarded, not destroyed),
    and a Withdraw order.
- **The mission editor** (`tools/mission-editor`), on **Avalonia UI**, chosen because:
  - it is one C# desktop app for Windows and macOS on the game's own .NET 8;
  - it references `FleetCommand.Core`, so an author sees the game's own errors, briefing, battle plan and grading
    (`MissionPreview.cs`), never a second copy of the rules.

  The editor edits a file as a form and as JSON, checks it as it is typed, and previews it on a new campaign: won,
  withdrawn or lost.
