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
# the Ssarith Hunt's console (its own battle) and the Kelvane Institute's (no ships of its own yet: the Concord's battle)
shot ssarith-battle 1920x1080 --steps=600 --scenario=ssarith_battle
shot ssarith-power 1920x1080 --steps=600 --scenario=ssarith_battle --station=power
shot ssarith-ship-expanded 1920x1080 --steps=600 --scenario=ssarith_battle --expand=ship
shot ssarith-weapons-expanded 1920x1080 --steps=600 --scenario=ssarith_battle --expand=weapons
shot ssarith-menu 1920x1080 --steps=300 --scenario=ssarith_battle --menu
shot kelvane-battle 1920x1080 --steps=600 --skin=kelvane
shot kelvane-power 1920x1080 --steps=600 --skin=kelvane --station=power
shot kelvane-ship-expanded 1920x1080 --steps=600 --skin=kelvane --expand=ship
shot kelvane-weapons-expanded 1920x1080 --steps=600 --skin=kelvane --expand=weapons
shot kelvane-menu 1920x1080 --steps=300 --skin=kelvane --menu
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
# the Kelvane Institute (mockup 23, change kelvane-ships-engine): each class close, the carrier's pads, the Institute as a side
shot institute-battlecruiser 1920x1080 --steps=600 --scenario=concord_institute --target=kv_rigour --target-view --zoom=0.5
shot institute-carrier 1920x1080 --steps=600 --scenario=concord_institute --target=kv_curiosity --target-view --zoom=0.5
shot institute-range 1920x1080 --steps=300 --scenario=concord_range --target=t_irv --target-view --zoom=0.5
shot institute-carrier-pads 1920x1080 --steps=300 --scenario=concord_range --target=t_icv --target-view --zoom=0.15
shot institute-frigate 1920x1080 --steps=300 --scenario=concord_range --target=t_iff --target-view --zoom=0.3
shot institute-wrecks 1920x1080 --steps=2400 --scenario=concord_institute_wrecks --target=kw_parallax --target-view --zoom=0.7
# the placeholder lines (mockup 36, change placeholder-fleets): a row of the damage range each
shot placeholder-kethran 1920x1080 --steps=300 --scenario=concord_range --target=ph_kethran_dn --target-view --zoom=0.5
shot placeholder-vhorr 1920x1080 --steps=300 --scenario=concord_range --target=ph_vhorr_cv --target-view --zoom=0.5
shot placeholder-aelith 1920x1080 --steps=300 --scenario=concord_range --target=ph_aelith_cap --target-view --zoom=0.5
shot kelvane-patience 1920x1080 --steps=600 --scenario=kelvane_battle
shot kelvane-patience-ship 1920x1080 --steps=600 --scenario=kelvane_battle --expand=ship
# the Kelvane Institute's console end to end (2026-11-07, owner: "end to end test all kelvane ui systems"): every station
# expanded, every ship page, the radar and the phone shapes, each in the Institute's own scenes
for st in helm power tactical pd science comms repair transport fleet; do
  shot "kelvane-ui-$st" 1920x1080 --steps=600 --scenario=kelvane_battle --station=$st --expand=station
done
shot kelvane-ui-vip 1920x1080 --steps=300 --scenario=kelvane_vip --station=transport --expand=station
shot kelvane-ui-shields 1920x1080 --steps=600 --scenario=kelvane_battle --page=shields --expand=ship
shot kelvane-ui-sensors 1920x1080 --steps=600 --scenario=kelvane_cloak --page=sensors --expand=ship
shot kelvane-ui-tractor 1920x1080 --steps=300 --scenario=kelvane_tractor --page=tractor --expand=ship
shot kelvane-ui-craft 1920x1080 --steps=300 --scenario=kelvane_fighters --page=craft --expand=ship
shot kelvane-ui-radar 1920x1080 --steps=600 --scenario=kelvane_battle --expand=radar
shot kelvane-ui-escape 1920x1080 --steps=300 --scenario=kelvane_escape --station=repair
shot kelvane-phone-landscape 844x390 --steps=600 --scenario=kelvane_battle
shot kelvane-phone-portrait 390x844 --steps=600 --scenario=kelvane_battle
shot kelvane-phone-weapons 844x390 --steps=600 --scenario=kelvane_battle --expand=weapons
shot kelvane-phone-portrait-station 390x844 --steps=600 --scenario=kelvane_battle --station=power --expand=station
# field operations (openspec/changes/mission-systems): planets ported from Falchion and godot-sandbox, the planet view down
# to the surface, found sites and an away team, Scan planet and the Transport station's Sites, unknown contacts beyond the
# sensors, and each placeholder stand-in (into docs/screenshots/game/mission-systems/ when run with that folder)
MS="--scenario=concord_relief --steps=60"
shot ms-planet-battle 1920x1080 $MS --select-planet
# a ship beside a station at their battle sizes, the shields drawn round each (owner, 2026-11-10: "Show me a ship to station comparison")
shot ms-station-compare 1920x1080 $MS --target=harbor --zoom=2.2
for k in terran ocean desert ice lava barren gas ice_giant toxic city archipelago fossil ruins; do shot "ms-planet-$k" 1920x1080 $MS --planet-kind=$k --planet-view=1.4 --yaw=1.9 --pitch=0.3; done
shot ms-planet-surface 1920x1080 $MS --planet-view=0.012 --pitch=0.35
# the worlds with something standing on the ground, from low over it (the pattern's finest levels)
for k in city fossil ruins; do shot "ms-planet-low-$k" 1920x1080 $MS --planet-kind=$k --planet-view=0.05 --pitch=0.35; shot "ms-planet-surface-$k" 1920x1080 $MS --planet-kind=$k --planet-view=0.012 --pitch=0.35; done
shot ms-planet-sites 1920x1080 $MS --sites --survey --secure --near-planet --beam --planet-view=0.5 --yaw=0 --pitch=0.42
shot ms-planet-sites-close 1920x1080 $MS --sites --survey --secure --near-planet --beam --planet-view=0.12 --yaw=-0.05 --pitch=0.3
shot ms-science-planet 1920x1080 $MS --sites --near-planet --select-planet --station=science
shot ms-transport-sites 1920x1080 $MS --sites --survey --secure --near-planet --beam --select-planet --station=transport --tab=cargo --cargo-view=sites
# nebula plumes and an ion storm (change nebula-plumes): the columns from across the field, a storm with its lightning, the
# view from inside a plume, a strike that trips the core, and the plumes on the tactical map
shot nebula-plume 1920x1080 --scenario=concord_heavy --steps=60 --zoom=9 --pitch=0.2 --yaw=1.2
shot nebula-storm 1920x1080 --scenario=kelvane_heavy --steps=60 --zoom=14 --pitch=0.3 --flicker
shot nebula-inside 1920x1080 --scenario=krai_heavy --steps=60 --in-plume=0
shot nebula-strike 1920x1080 --scenario=concord_heavy --in-plume=0 --steps=150 --strike --flicker --zoom=4 --pitch=0.25
shot nebula-map 1920x1080 --scenario=concord_heavy --steps=60 --expand=radar
shot ms-unknown-contact 1920x1080 --steps=10
for c in sb:krai yd:krai ab:krai op:krai pb:krai mon:fauna pod:krai; do
  shot "ms-standin-${c%%:*}" 1920x1080 --steps=300 --swap=kharz:$c --target=kharz --target-view --zoom=0.5
done
