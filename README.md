# Fleet Command

A real-time tactical starship combat game in Godot. You captain one ship from a console of panels
(the helm, the ship systems display, weapons and power) beside a live 3D view of the battle. It
plays on desktop and phones: any panel can be expanded to full screen, with the battle view moved
to the side.

Status: the battle console is in the engine with the approved Concord and Krai ships and scenarios for both sides; the mockups lead, and `docs/design/mockup-sync.md` lists what the game still lacks.

- Rules for contributors (and Claude): [CLAUDE.md](CLAUDE.md)
- Open questions: [docs/design/README.md](docs/design/README.md)
- Specs and changes: [openspec/](openspec/)

## Tools

```bash
scripts/install-godot.sh        # Godot 4.7.1 (build.config) into .tools/godot/
scripts/install-blender.sh      # Blender 4.4.3 into /opt/blender
npm install -g @fission-ai/openspec@1.14.0
```

Mockups: copy `docs/mockups/lib/template.html`, then screenshot with
`NODE_PATH=$(npm root -g) node tools/mockups/shoot.mjs docs/mockups/<page>.html docs/screenshots/mockups`.

## The game (Godot 4.7, C#)

The battle console is being built in the engine (OpenSpec change `battle-console-engine`). To run it:

```bash
scripts/install-dotnet.sh && scripts/install-godot.sh
```

```bash
DOTNET_ROOT=$PWD/.tools/dotnet PATH=$PWD/.tools/dotnet:$PATH .tools/godot/godot --path .
```

On Windows the same scripts run under Git Bash and install the win64 editor and the SDK into `.tools/`. Then open the
editor (`.tools/godot/Godot_v4.7.1-stable_mono_win64.exe`, with the project folder) and press Play, or run it from Git
Bash:

```bash
DOTNET_ROOT=$PWD/.tools/dotnet .tools/godot/Godot_v4.7.1-stable_mono_win64_console.exe --path .
```

Pick a scenario from the view's scene selector (the Concord's or the Krai Warhost's side), or start one with
`-- --scenario=krai_battle`. Checks: `scripts/run-tests.sh`; stills: `scripts/game-stills.sh` (into
`docs/screenshots/game/`). The ships are exported from the ship mockups with
`NODE_PATH=$(npm root -g) node tools/mockups/export_ships.mjs` (into `assets/ships/`).
