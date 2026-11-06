#!/usr/bin/env bash
# Stills of the running game (CLAUDE.md section 9). On Linux (cloud sessions) headless under Xvfb with Mesa's software
# renderer, said so; on Windows (the owner's PC) in a real window that opens for a few seconds a shot, so run it there
# only when the owner has said Claude may use the screen. On macOS (the owner's Mac) the same, in a real window. Each shot plays a scenario from its seed for a number of steps,
# sets the console as named, and saves a PNG into docs/screenshots/game/ (or the folder given).
# Usage: scripts/game-stills.sh [out_dir] [only,these,names]
set -euo pipefail
REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$REPO_ROOT"
# macOS has no timeout(1): Homebrew coreutils' gtimeout is the same tool (brew install coreutils).
command -v timeout >/dev/null || timeout() { gtimeout "$@"; }
OUT="${1:-docs/screenshots/game}"
ONLY="${2:-}"
mkdir -p "$OUT"
DOTNET="$(scripts/install-dotnet.sh 2>/dev/null)"
GODOT="$(scripts/install-godot.sh 2>/dev/null)"
export DOTNET_ROOT="$(dirname "$DOTNET")" PATH="$(dirname "$DOTNET"):$PATH"
"$DOTNET" build FleetCommand.csproj -v q --nologo >/dev/null
case "$(uname -s)" in MINGW*|MSYS*|CYGWIN*) HOST=windows ;; Darwin) HOST=macos ;; *) HOST=linux ;; esac

shot() { # name size args...
  local name="$1" size="$2"; shift 2
  [[ -z "$ONLY" || ",$ONLY," == *",$name,"* ]] || return 0
  if [[ "$HOST" == windows || "$HOST" == macos ]]; then   # a real window (the Mac and the PC have screens)
    timeout 240 "$GODOT" --path . --resolution "$size" --position 0,0 -- --still="$OUT/$name.png" "$@" 2>&1 \
      | grep -E "^STILL" || { echo "FAILED $name"; return 1; }
  else
    timeout 240 xvfb-run -a -s "-screen 0 ${size}x24" "$GODOT" --path . --rendering-driver opengl3 --resolution "$size" -- \
      --still="$OUT/$name.png" "$@" 2>&1 | grep -E "^STILL" || { echo "FAILED $name"; return 1; }
  fi
}
shot battle 1920x1080 --steps=600
shot power 1920x1080 --steps=600 --station=power
shot shields 1920x1080 --steps=600 --page=shields --reinforce=0
shot station-expanded 1920x1080 --steps=600 --expand=station
shot power-expanded 1920x1080 --steps=600 --station=power --expand=station
shot ship-expanded 1920x1080 --steps=600 --expand=ship
shot weapons-expanded 1920x1080 --steps=600 --expand=weapons
shot menu 1920x1080 --steps=300 --menu
shot wide-2560 2560x1080 --steps=1200
# the other side and the other scenes (change battle-scenarios)
shot krai-battle 1920x1080 --steps=600 --scenario=krai_battle
shot krai-power 1920x1080 --steps=600 --scenario=krai_battle --station=power
shot krai-ship-expanded 1920x1080 --steps=600 --scenario=krai_battle --expand=ship
shot krai-weapons-expanded 1920x1080 --steps=600 --scenario=krai_battle --expand=weapons
shot krai-menu 1920x1080 --steps=300 --scenario=krai_battle --menu
shot krai-duel 1920x1080 --steps=900 --scenario=krai_duel --zoom=0.6
shot scene-list 1920x1080 --steps=300 --scene-list
shot ships-close 1920x1080 --steps=900 --zoom=0.45
# the camera (battle-console design 4e, 4n): locked on the target by default; Target view orbits the target
shot target-view 1920x1080 --steps=600 --target-view
shot krai-target-view 1920x1080 --steps=600 --scenario=krai_battle --target-view
# the Ssarith Hunt (mockup 12, change ssarith-ships-engine): each class close, from the Concord's side, and the battle later on
shot hunt-cruiser 1920x1080 --steps=600 --scenario=concord_hunt --target=hunt_cruiser --target-view --zoom=0.5
shot hunt-carrier 1920x1080 --steps=600 --scenario=concord_hunt --target=hunt_carrier --target-view --zoom=0.5
shot hunt-trident 1920x1080 --steps=600 --scenario=concord_hunt --target=hunt_trident --target-view --zoom=0.5
shot hunt-range 1920x1080 --steps=300 --scenario=concord_range --target=t_hff2 --target-view --zoom=0.8
shot hunt-carrier-bays 1920x1080 --steps=300 --scenario=concord_range --target=t_hcv --target-view --zoom=0.1
shot hunt-trident-windows 1920x1080 --steps=300 --scenario=concord_range --target=t_hff4 --target-view --zoom=0.1
shot hunt-battle-late 1920x1080 --steps=3600 --scenario=concord_hunt --target=hunt_cruiser --target-view --zoom=0.6
shot krai-hunt 1920x1080 --steps=600 --scenario=krai_hunt --target=hunt_harpoon --target-view --zoom=0.5
shot hunt-wrecks 1920x1080 --steps=2400 --scenario=concord_hunt_wrecks --target=hw_lance --target-view --zoom=0.7
