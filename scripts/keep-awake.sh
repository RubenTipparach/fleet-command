#!/bin/sh
# keep-awake.sh: keeps this Mac's display awake and its screen unlocked while a playtest, a recording or a co-op game runs
# (owner, 2026-11-11: "there should be wake scripts", after a co-op recording failed on a screen that had locked: OBS cannot
# capture a window behind a locked screen, and macOS throttles a window it does not show). Adapted from Pale Blue Dot's
# tools/keep_awake.sh. caffeinate -d keeps the display on, -u declares the user active (no screen saver, so no lock behind
# it), -i/-m/-s keep the system, disk and AC sleep off. It cannot unlock a screen that is already locked: run it before.
#
#   scripts/keep-awake.sh              hold until Ctrl-C
#   scripts/keep-awake.sh 4            hold for 4 hours
#   scripts/keep-awake.sh -- <cmd...>  hold while <cmd> runs
set -eu
[ "$(uname)" = Darwin ] || { echo "keep-awake.sh: macOS only (Windows: scripts/keep-awake.ps1)" >&2; exit 1; }
if [ "${1:-}" = "--" ]; then
  shift
  exec caffeinate -dimsu "$@"
elif [ -n "${1:-}" ]; then
  secs=$(awk "BEGIN { print int($1 * 3600) }")
  echo "keeping the display awake for $1 h"
  exec caffeinate -dimsu -t "$secs"
else
  echo "keeping the display awake until Ctrl-C"
  exec caffeinate -dimsu
fi
