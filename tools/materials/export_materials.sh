#!/usr/bin/env bash
# Render every Fleet Command material graph with Material Maker, headless (the material-maker-headless skill), at
# 2048 px into tools/materials/raw/ (gitignored), then write assets/textures/materials/<name>/ and the contact sheet
# docs/screenshots/materials/contact-sheet.png (postprocess.py). Pass material names to rebuild only those graphs.
# After Crab Breeze's export_house_materials.sh (69ce196).
# Set up Material Maker first: eval "$(bash .claude/skills/material-maker-headless/scripts/setup.sh | grep ^export)".
set -euo pipefail
cd "$(dirname "$0")/../.."
python3 tools/materials/build_ptex.py "$@"
graphs=()
if [ $# -gt 0 ]; then
  for g in "$@"; do graphs+=("tools/materials/ptex/$g.ptex"); done
else
  rm -rf tools/materials/raw
  graphs=(tools/materials/ptex/*.ptex)
fi
MM_TIMEOUT="${MM_TIMEOUT:-3600}" bash .claude/skills/material-maker-headless/scripts/render.sh \
  -o tools/materials/raw --size 2048 "${graphs[@]}"
python3 tools/materials/postprocess.py tools/materials/raw
