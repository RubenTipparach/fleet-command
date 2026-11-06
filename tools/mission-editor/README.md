# Mission editor

Writes, checks and previews Fleet Command's mission files (`data/missions/*.json`) outside Godot, with the game's own code
(`src/Core`). Avalonia UI, so one app runs on Windows and macOS. The format and how to use the window are in
`docs/design/mission-files.md`; the change is `openspec/changes/mission-scripting`.

```sh
.tools/dotnet/dotnet run --project tools/mission-editor          # from anywhere inside the repository
.tools/dotnet/dotnet run --project tools/mission-editor -- --shot=out.png --file=prize.json --end=win   # a picture, drawn off screen
```
