#!/usr/bin/env bash
# Install Blender for the headless model, panel and bake generators (CLAUDE.md section 10) into
# /opt/blender. A cloud session runs this on demand (about a minute), not at session start.
# The version and mirror match Crab Breeze's (CLAUDE.md section 10 there).
#
# Usage: scripts/install-blender.sh   (prints the binary's path)
set -euo pipefail

BLENDER_SERIES="4.4"
BLENDER_VERSION="4.4.3"
BIN=/opt/blender/blender

if [[ ! -x "$BIN" ]] || ! "$BIN" --version 2>/dev/null | grep -q "Blender $BLENDER_VERSION"; then
  tmp="$(mktemp -d)"
  curl -sSL --fail --retry 3 -o "$tmp/blender.tar.xz" \
    "https://mirrors.dotsrc.org/blender/release/Blender$BLENDER_SERIES/blender-$BLENDER_VERSION-linux-x64.tar.xz"
  rm -rf /opt/blender
  mkdir -p /opt/blender
  tar -xJf "$tmp/blender.tar.xz" -C /opt/blender --strip-components=1
  rm -rf "$tmp"
  # Blender's bundled Python needs these system libraries in a slim container.
  if ! ldconfig -p | grep -q libXi.so.6; then
    (apt-get update -qq && DEBIAN_FRONTEND=noninteractive apt-get install -y -qq \
      libxi6 libxxf86vm1 libxfixes3 libxrender1 libxkbcommon0 libsm6 libgl1 libegl1) >/dev/null 2>&1 || true
  fi
fi
"$BIN" --version | head -1 >&2
echo "$BIN"
