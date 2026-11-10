#!/usr/bin/env bash
# The checks before a push (CLAUDE.md section 12): the headless C# tests of FleetCommand.Core (simulation, data, layout
# model), a warnings-as-errors build of the Godot project, then the layout test and the skin parity check in the engine
# (headless), each in every skin.
# Usage: scripts/run-tests.sh
set -euo pipefail
REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$REPO_ROOT"
# macOS has no timeout(1): Homebrew coreutils' gtimeout is the same tool (brew install coreutils).
command -v timeout >/dev/null || timeout() { gtimeout "$@"; }
DOTNET="$(scripts/install-dotnet.sh 2>/dev/null)"
GODOT="$(scripts/install-godot.sh 2>/dev/null)"
export DOTNET_ROOT="$(dirname "$DOTNET")" PATH="$(dirname "$DOTNET"):$PATH" DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1

echo "== faction readiness page is what the game says (docs/design/faction-readiness.html, CLAUDE.md section 12)"
python3 tools/factions/readiness.py --check

echo "== the plumes' noise is what its generator writes (assets/space/plume_noise.bin, change nebula-plumes)"
python3 tools/space/plume_noise.py --check

echo "== headless tests (FleetCommand.Core)"
"$DOTNET" test tests/Core.Tests -v q --nologo

echo "== Godot project build (warnings are errors)"
"$DOTNET" build FleetCommand.csproj -v q --nologo

echo "== headless import writes nothing new (every .import and .uid file is committed, CLAUDE.md section 2)"
timeout 900 "$GODOT" --headless --path . --import >/dev/null 2>&1 || true
left="$(git status --porcelain -- '*.import' '*.uid')"
[[ -z "$left" ]] || { echo "FAIL: the import wrote files that are not committed; commit them with their assets:"; echo "$left"; exit 1; }

echo "== layout test (Godot, headless)"
out="$(timeout 300 "$GODOT" --headless --path . -- --layout-test 2>&1)" || { echo "$out"; exit 1; }
echo "$out" | grep -E "LAYOUT_(TEST|FAIL)"
echo "$out" | grep -q "LAYOUT_TEST PASS"

echo "== skin parity (Godot, headless): every control in every skin, inside its panel; the console follows the ship you command"
out="$(timeout 300 "$GODOT" --headless --path . --resolution 1920x1080 -- --parity-test --scenario=concord_battle 2>&1)" || { echo "$out" | grep -E "PARITY|ERROR"; exit 1; }
echo "$out" | grep -E "PARITY_TEST"
