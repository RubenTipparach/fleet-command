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
# Linux (cloud sessions, CI) or Windows under Git Bash (the owner's playtest PC), which takes Microsoft's PowerShell
# installer and an .exe.
case "$(uname -s)" in
  MINGW*|MSYS*|CYGWIN*) HOST=windows; DOTNET="$DEST/dotnet.exe" ;;
  *) HOST=linux; DOTNET="$DEST/dotnet" ;;
esac

if [[ ! -x "$DOTNET" ]] || ! "$DOTNET" --list-sdks 2>/dev/null | grep -q "^$DOTNET_CHANNEL\."; then
  mkdir -p "$DEST"
  tmp="$(mktemp -d)"
  if [[ "$HOST" == "windows" ]]; then
    curl -sSL --fail --retry 3 --retry-delay 2 -o "$tmp/dotnet-install.ps1" https://dot.net/v1/dotnet-install.ps1
    powershell.exe -NoProfile -ExecutionPolicy Bypass -File "$(cygpath -w "$tmp/dotnet-install.ps1")"       -Channel "$DOTNET_CHANNEL" -InstallDir "$(cygpath -w "$DEST")" -NoPath >&2
  else
    curl -sSL --fail --retry 3 --retry-delay 2 -o "$tmp/dotnet-install.sh" https://dot.net/v1/dotnet-install.sh
    bash "$tmp/dotnet-install.sh" --channel "$DOTNET_CHANNEL" --install-dir "$DEST" --no-path >&2
  fi
  rm -rf "$tmp"
fi
export DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1
"$DOTNET" --version >&2
echo "$DOTNET"
