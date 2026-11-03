#!/usr/bin/env bash
# The checks before a push (CLAUDE.md section 12): the headless C# tests of FleetCommand.Core (simulation, data, layout
# model), a warnings-as-errors build of the Godot project, then the layout test in the engine (headless).
# Usage: scripts/run-tests.sh
set -euo pipefail
REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$REPO_ROOT"
DOTNET="$(scripts/install-dotnet.sh 2>/dev/null)"
GODOT="$(scripts/install-godot.sh 2>/dev/null)"
export DOTNET_ROOT="$(dirname "$DOTNET")" PATH="$(dirname "$DOTNET"):$PATH" DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1

echo "== headless tests (FleetCommand.Core)"
"$DOTNET" test tests/Core.Tests -v q --nologo

echo "== Godot project build (warnings are errors)"
"$DOTNET" build FleetCommand.csproj -v q --nologo

echo "== layout test (Godot, headless)"
[[ -d .godot ]] || timeout 600 "$GODOT" --headless --path . --import >/dev/null 2>&1 || true
out="$(timeout 300 "$GODOT" --headless --path . -- --layout-test 2>&1)" || { echo "$out"; exit 1; }
echo "$out" | grep -E "LAYOUT_(TEST|FAIL)"
echo "$out" | grep -q "LAYOUT_TEST PASS"
