#!/usr/bin/env bash
# The scripted captain's suite (CLAUDE.md section 9; the playtest skill): every run in data/playtests/suite.json, each
# a script played against a scenario it picks from the scene selector, from both sides. Headless at a fixed 30 frames a
# second, so a battle plays as fast as the machine allows and the verdict is the same as in real time: it proves the
# game plays through, never how it looks or how fast it runs (a recorded take does that). Prints each run's
# PLAYTEST_RESULT and a summary; exits 1 if any run is stuck or crashed. --watch plays them in a window in real time
# (only when the owner has said Claude may use the screen).
# Usage: scripts/playtest.sh [--watch] [only,these,scenarios]
set -euo pipefail
REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$REPO_ROOT"
WATCH=0; [[ "${1:-}" == "--watch" ]] && { WATCH=1; shift; }
ONLY="${1:-}"
DOTNET="$(scripts/install-dotnet.sh 2>/dev/null)"
GODOT="$(scripts/install-godot.sh 2>/dev/null)"
export DOTNET_ROOT="$(dirname "$DOTNET")" PATH="$(dirname "$DOTNET"):$PATH"
"$DOTNET" build FleetCommand.csproj -v q --nologo >/dev/null
[[ -d .godot/imported ]] || timeout 900 "$GODOT" --headless --path . --import >/dev/null 2>&1 || true
PY="$(command -v python3 || command -v python)"
runs="$("$PY" -c "import json;[print(r['script'],r['play']) for r in json.load(open('data/playtests/suite.json'))['runs']]")"
fails=0; total=0
while read -r script play; do
  [[ -z "$ONLY" || ",$ONLY," == *",$play,"* ]] || continue
  total=$((total + 1))
  if [[ $WATCH == 1 ]]; then mode=(--resolution 1920x1080); else mode=(--headless --fixed-fps 30 --resolution 1920x1080); fi
  out="$(timeout 900 "$GODOT" "${mode[@]}" --path . -- --playtest="$script" --play="$play" 2>&1)" || true
  line="$(echo "$out" | grep -E "^PLAYTEST_RESULT" || echo "PLAYTEST_RESULT crash no result (exit without PLAYTEST_RESULT)")"
  echo "$script $play: $line"
  [[ "$line" == "PLAYTEST_RESULT ok"* ]] || { fails=$((fails + 1)); echo "$out" | grep -E "ERROR|Exception|PLAYTEST_BEAT" | tail -8; }
done <<< "$runs"
echo "PLAYTEST_SUITE $((total - fails))/$total ok"
[[ $fails == 0 ]]
