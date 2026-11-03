#!/usr/bin/env bash
# Install the pinned .NET SDK (build.config DOTNET_CHANNEL) into TOOLS_DIR/dotnet, for the C# project
# and its headless tests. Idempotent: skipped when a matching SDK is already there.
#
# Usage: scripts/install-dotnet.sh
# Prints the dotnet executable's path on stdout.
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
# shellcheck source=/dev/null
source "$REPO_ROOT/build.config"

DEST="$REPO_ROOT/$TOOLS_DIR/dotnet"
DOTNET="$DEST/dotnet"

if [[ ! -x "$DOTNET" ]] || ! "$DOTNET" --list-sdks 2>/dev/null | grep -q "^$DOTNET_CHANNEL\."; then
  mkdir -p "$DEST"
  tmp="$(mktemp -d)"
  curl -sSL --fail --retry 3 --retry-delay 2 -o "$tmp/dotnet-install.sh" https://dot.net/v1/dotnet-install.sh
  bash "$tmp/dotnet-install.sh" --channel "$DOTNET_CHANNEL" --install-dir "$DEST" --no-path >&2
  rm -rf "$tmp"
fi
export DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1
"$DOTNET" --version >&2
echo "$DOTNET"
