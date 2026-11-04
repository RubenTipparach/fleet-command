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

FULL_VERSION="$GODOT_VERSION-$GODOT_RELEASE-$GODOT_FLAVOR"
DEST="$REPO_ROOT/$TOOLS_DIR/godot"
STAMP="$DEST/.installed-version"
# Linux (cloud sessions, CI), macOS (the owner's Mac) or Windows under Git Bash (the owner's playtest PC).
case "$(uname -s)" in
  MINGW*|MSYS*|CYGWIN*) HOST=windows ;;
  Darwin) HOST=macos ;;
  *) HOST=linux ;;
esac
# The .NET (mono) build ships as a folder (the editor beside its GodotSharp assemblies) and has its own templates.
MONO_TAG=""; [[ "$GODOT_FLAVOR" == "mono" ]] && MONO_TAG="mono_"
TEMPLATE_SUFFIX=""; [[ "$GODOT_FLAVOR" == "mono" ]] && TEMPLATE_SUFFIX=".mono"
TEMPLATES_SLUG="${MONO_TAG}export_templates.tpz"
if [[ "$HOST" == "windows" ]]; then
  # Windows keeps the zip's own file names: the _console.exe wrapper launches the editor beside it by name, and it is
  # the one that prints to the shell, so it is the binary this script hands back.
  EDITOR_SLUG="${MONO_TAG}win64.zip"; EDITOR_PLATFORM="windows.64"
  GODOT_BIN="$DEST/Godot_v$GODOT_VERSION-${GODOT_RELEASE}_${MONO_TAG}win64_console.exe"
  TEMPLATE_DIR="$APPDATA/Godot/export_templates/$GODOT_VERSION.$GODOT_RELEASE$TEMPLATE_SUFFIX"
elif [[ "$HOST" == "macos" ]]; then
  # The macOS zip is one universal app bundle (Godot_mono.app for the .NET build); the binary inside prints to the shell.
  EDITOR_SLUG="${MONO_TAG}macos.universal.zip"; EDITOR_PLATFORM="macos.universal"
  GODOT_BIN="$DEST/Godot$([[ -n "$MONO_TAG" ]] && echo _mono).app/Contents/MacOS/Godot"
  TEMPLATE_DIR="$HOME/Library/Application Support/Godot/export_templates/$GODOT_VERSION.$GODOT_RELEASE$TEMPLATE_SUFFIX"
else
  EDITOR_SLUG="${MONO_TAG}linux$([[ -n "$MONO_TAG" ]] && echo _ || echo .)x86_64.zip"; EDITOR_PLATFORM="linux.64"
  GODOT_BIN="$DEST/godot"
  TEMPLATE_DIR="$HOME/.local/share/godot/export_templates/$GODOT_VERSION.$GODOT_RELEASE$TEMPLATE_SUFFIX"
fi

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
  fetch "$(asset_url "$EDITOR_SLUG" "$EDITOR_PLATFORM")" "$DEST/godot.zip"
  unzip -q -o "$DEST/godot.zip" -d "$DEST"
  rm -f "$DEST/godot.zip"
  # The mono zip holds one folder; lift its contents (the editor and GodotSharp/) up beside the stamp.
  sub="$(find "$DEST" -mindepth 1 -maxdepth 1 -type d -name 'Godot_v*' -print -quit)"
  if [[ -n "$sub" ]]; then mv "$sub"/* "$DEST"/; rmdir "$sub"; fi
  if [[ "$HOST" == "linux" ]]; then
    mv "$(find "$DEST" -maxdepth 1 -type f -name 'Godot_v*' -print -quit)" "$GODOT_BIN"
  fi
  chmod +x "$GODOT_BIN"
  printf '%s' "$FULL_VERSION" > "$STAMP"
fi
"$GODOT_BIN" --headless --version >&2

if [[ "${1:-}" == "--templates" && -z "$(ls -A "$TEMPLATE_DIR" 2>/dev/null)" ]]; then
  tmp="$(mktemp -d)"
  fetch "$(asset_url "$TEMPLATES_SLUG" templates)" "$tmp/templates.tpz"
  unzip -q -o "$tmp/templates.tpz" -d "$tmp"
  mkdir -p "$TEMPLATE_DIR"
  mv "$tmp/templates"/* "$TEMPLATE_DIR"/
  rm -rf "$tmp"
  echo "export templates in $TEMPLATE_DIR" >&2
fi

echo "$GODOT_BIN"
