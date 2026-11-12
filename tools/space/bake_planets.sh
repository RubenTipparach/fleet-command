#!/usr/bin/env bash
# bake_planets: every planet kind baked to textures for the low-spec mode (owner, 2026-11-12: "planets should be textures,
# have the mac render some planets as textures to be cached and used for pi only"). Runs the game's PlanetBaker in a window
# (a GPU is needed: the maps come from the real planet shaders), writes assets/space/planets_baked/, then imports so the
# .import files beside the maps can be committed with them (CLAUDE.md section 2). Deterministic: one fixed seed per kind.
set -euo pipefail
cd "$(dirname "$0")/../.."
GODOT="$(scripts/install-godot.sh 2>/dev/null)"
DOTNET="$(scripts/install-dotnet.sh 2>/dev/null)"
export DOTNET_ROOT="$(dirname "$DOTNET")" PATH="$(dirname "$DOTNET"):$PATH"
"$DOTNET" build FleetCommand.csproj -v q --nologo >/dev/null
COMMIT="$(git log -1 --format=%h -- src/Game/View/planet*.gdshader* src/Game/View/PlanetBody.cs src/Game/View/PlanetLooks.cs data/space/planets.json)"
"$GODOT" --path . --resolution 640x360 -- --planet-bake=assets/space/planets_baked --planet-bake-commit="$COMMIT" --planet-bake-check=output/planet_bake_check 2>&1 | grep -E "^BAKE|ERROR"
"$GODOT" --headless --path . --import >/dev/null 2>&1 || true
ls assets/space/planets_baked
