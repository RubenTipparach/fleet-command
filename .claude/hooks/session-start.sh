#!/bin/bash
# SessionStart hook for Claude Code cloud sessions: make the repository's tools and checks
# runnable from the first prompt (CLAUDE.md section 11). Adapted from Crab Breeze's hook.
# Idempotent and synchronous. Blender (scripts/install-blender.sh), LMMS (the music-authoring
# skill), Material Maker (material-maker-headless) and Krita (krita-brushwork) are installed on demand: each takes a minute
# and most sessions need none of them.
set -euo pipefail

if [ "${CLAUDE_CODE_REMOTE:-}" != "true" ]; then
  exit 0
fi
cd "$CLAUDE_PROJECT_DIR"

OPENSPEC_VERSION="1.14.0"
if ! command -v openspec >/dev/null 2>&1 || [ "$(openspec --version 2>/dev/null)" != "$OPENSPEC_VERSION" ]; then
  npm install -g "@fission-ai/openspec@${OPENSPEC_VERSION}" >/dev/null 2>&1
fi
openspec --version >/dev/null

# The pinned Godot editor, headless (build.config).
scripts/install-godot.sh >/dev/null 2>&1 || echo "session-start: Godot install failed; run scripts/install-godot.sh" >&2

# Headless stills render through Mesa's software Vulkan (lavapipe) or OpenGL under Xvfb, and the
# mockup screenshots need Chromium's fonts (CLAUDE.md section 9).
if ! command -v xvfb-run >/dev/null 2>&1 || [ ! -f /usr/share/vulkan/icd.d/lvp_icd.json ]; then
  (apt-get update -qq && DEBIAN_FRONTEND=noninteractive apt-get install -y -qq \
    xvfb mesa-vulkan-drivers libvulkan1 libgl1-mesa-dri) >/dev/null 2>&1 || true
fi
