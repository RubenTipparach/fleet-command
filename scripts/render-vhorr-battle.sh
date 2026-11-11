#!/usr/bin/env bash
# Sculpt and render the Vhorr Brood's battle console kit (openspec/changes/vhorr-battle-console): the battle console's
# own slots, written by tools/mockups/vhorr_battle_blockout.py, handed to the parametric sculptors by
# tools/blender/build_ui_vhorr_organic.py, one layout at a time, each into its own folder with its own manifest:
#   assets/ui/vhorr/organic/battle/column     the name plate, the column's panels, the radar's pocket, the crest, the
#                                            strips and the column's tissue backing
#   assets/ui/vhorr/organic/battle/expanded   the expanded panel's frame
#   assets/ui/vhorr/organic/battle/tray       the controls the console draws at many sizes, in every state
# About 25 minutes on a cloud session's CPU (Cycles, 24 samples).
#
# Usage: scripts/render-vhorr-battle.sh [layout ...]   (default: battle expanded tray)
set -euo pipefail
cd "$(dirname "$0")/.."
BLENDER="${BLENDER:-/opt/blender/blender}"
python3 tools/mockups/vhorr_battle_blockout.py
declare -A DIR=([battle]=column [expanded]=expanded [tray]=tray)
for layout in "${@:-battle expanded tray}"; do
  for l in $layout; do
    "$BLENDER" --background --factory-startup --python tools/blender/build_ui_vhorr_organic.py -- \
      --blockout assets/ui/vhorr/organic/battle/blockout.json --styles assets/ui/vhorr/organic/battle/styles.json \
      --layout "$l" --out "assets/ui/vhorr/organic/battle/${DIR[$l]}" 2>&1 | grep -v '^Fra:'
  done
done
