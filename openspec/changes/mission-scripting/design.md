# Design: mission-scripting

## The choice of UI package

| Package | Windows and macOS | Reuses the game's C# rules | Notes |
|---|---|---|---|
| **Avalonia UI 11** (chosen) | yes, native windows, one code base (Linux too) | yes: references `src/Core` directly | MIT; Fluent theme; code-only UI, no designer needed |
| .NET MAUI | yes (macOS through Mac Catalyst) | yes | heavier workloads to install; desktop is not its focus |
| Electron or Tauri | yes | no: the rules would need a JavaScript or Rust copy, or a separate process | breaks one implementation (CLAUDE.md 6.1) |
| A Godot tool scene | yes | yes | the owner asked for writing missions *outside* Godot |

Avalonia is the only option that is cross-platform, desktop first, and in the same language and runtime as the game's
rules. So the editor is a thin window over the code the game runs.

## The format

The format reference is `docs/design/mission-files.md`. The shape is the mockup's script shape, with each function
replaced by data:

| Mockup function | In the file |
|---|---|
| `where` | an expression per sector |
| `setup` | a list of named rolls |
| `brief`, `objective` | text with conditional parts |
| `needs` | conditions with messages |
| `battle` | a list of cases, each with groups and objectives (an objective's `done` is an expression on the battle's end) |
| `grade` | a list of rules, or the default |
| `result` | a list of outcome rules |

Every name an expression may use is listed once, in `Scopes` (`MissionScript.cs`). The loader checks against that list,
the scopes provide exactly it, and the editor prints it.

## Battle end

The objectives are read from the battle as it ended:
- `alive`, `destroyed`, `taken` and `hull_min` of a group;
- `how` the battle ended: `end` (one side gone), `withdraw` (the player's Withdraw key) or `lost` (the fleet is gone).

A prize strikes at a quarter of its hull (tuning `capture_hull_frac`). It then stops fighting and no longer counts as an
enemy, so a battle against a prize and its escorts ends when the escorts are gone. Boarding parties are not simulated
yet (change `boarding-party`). Marines are only a launch condition (`needs`).

## Open questions

| # | Question | Provisional answer |
|---|---|---|
| S1 | Should the campaign mockup read the mission files too, so one copy serves both? | Yes. Until it does, the page's `campaign-missions.js` and the files are kept the same by hand (task 2.1) |
| S2 | Should the editor also place mission sectors on a map view? | Not yet: the preview names the sector and how many sectors the mission could go to |
| S3 | Capture: is a quarter of the hull the right line for a ship to strike? | 0.25, provisional |
