#!/usr/bin/env bash
# Headless stills of the running game under Xvfb (CLAUDE.md section 9: cloud sessions cannot record; they render stills
# with Mesa's software renderer and say so). Each shot plays the stand-in battle from its seed for a number of steps,
# sets the console as named, and saves a PNG into docs/screenshots/game/ (or the folder given).
# Usage: scripts/game-stills.sh [out_dir]
set -euo pipefail
REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$REPO_ROOT"
OUT="${1:-docs/screenshots/game}"
mkdir -p "$OUT"
DOTNET="$(scripts/install-dotnet.sh 2>/dev/null)"
GODOT="$(scripts/install-godot.sh 2>/dev/null)"
export DOTNET_ROOT="$(dirname "$DOTNET")" PATH="$(dirname "$DOTNET"):$PATH"
"$DOTNET" build FleetCommand.csproj -v q --nologo >/dev/null

shot() { # name size args...
  local name="$1" size="$2"; shift 2
  timeout 240 xvfb-run -a -s "-screen 0 ${size}x24" "$GODOT" --path . --rendering-driver opengl3 --resolution "$size" -- \
    --still="$OUT/$name.png" "$@" 2>&1 | grep -E "^STILL" || { echo "FAILED $name"; return 1; }
}
shot battle 1920x1080 --steps=600
shot power 1920x1080 --steps=600 --station=power
shot shields 1920x1080 --steps=900 --page=shields
shot station-expanded 1920x1080 --steps=600 --expand=station
shot weapons-expanded 1920x1080 --steps=600 --expand=weapons
shot menu 1920x1080 --steps=300 --menu
shot wide-2560 2560x1080 --steps=1200
