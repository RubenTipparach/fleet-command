#!/usr/bin/env bash
# Install the pinned Godot editor (build.config) into TOOLS_DIR, and its export templates with
# --templates. Idempotent: a stamp file records the installed version, so changing the pin
# replaces a stale editor instead of keeping it. Adapted from the-federation's
# scripts/install-godot.sh (RubenTipparach/the-federation at 02791b6).
#
# Usage: scripts/install-godot.sh [--templates]
# Prints the editor's path on stdout.
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
# shellcheck source=/dev/null
source "$REPO_ROOT/build.config"

FULL_VERSION="$GODOT_VERSION-$GODOT_RELEASE"
DEST="$REPO_ROOT/$TOOLS_DIR/godot"
GODOT_BIN="$DEST/godot"
STAMP="$DEST/.installed-version"
TEMPLATE_DIR="$HOME/.local/share/godot/export_templates/$GODOT_VERSION.$GODOT_RELEASE"

# Godot's own download endpoint: the canonical entry point, reachable where github.com is not.
asset_url() {
  printf 'https://downloads.godotengine.org/?version=%s&flavor=%s&slug=%s&platform=%s' \
    "$GODOT_VERSION" "$GODOT_RELEASE" "$1" "$2"
}

fetch() {
  echo "downloading $1" >&2
  curl -sSL --fail --retry 3 --retry-delay 2 -o "$2" "$1" || {
    echo "ERROR: download failed: $1 (check GODOT_VERSION and GODOT_RELEASE in build.config)" >&2
    exit 1
  }
}

if [[ ! -x "$GODOT_BIN" || "$(cat "$STAMP" 2>/dev/null)" != "$FULL_VERSION" ]]; then
  rm -rf "$DEST"
  mkdir -p "$DEST"
  fetch "$(asset_url linux.x86_64.zip linux.64)" "$DEST/godot.zip"
  unzip -q -o "$DEST/godot.zip" -d "$DEST"
  rm -f "$DEST/godot.zip"
  mv "$(find "$DEST" -maxdepth 1 -type f -name 'Godot_v*' -print -quit)" "$GODOT_BIN"
  chmod +x "$GODOT_BIN"
  printf '%s' "$FULL_VERSION" > "$STAMP"
fi
"$GODOT_BIN" --headless --version >&2

if [[ "${1:-}" == "--templates" && -z "$(ls -A "$TEMPLATE_DIR" 2>/dev/null)" ]]; then
  tmp="$(mktemp -d)"
  fetch "$(asset_url export_templates.tpz templates)" "$tmp/templates.tpz"
  unzip -q -o "$tmp/templates.tpz" -d "$tmp"
  mkdir -p "$TEMPLATE_DIR"
  mv "$tmp/templates"/* "$TEMPLATE_DIR"/
  rm -rf "$tmp"
  echo "export templates in $TEMPLATE_DIR" >&2
fi

echo "$GODOT_BIN"
