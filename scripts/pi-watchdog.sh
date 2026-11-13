#!/usr/bin/env bash
# pi-watchdog: runs a command (a Godot run on the Raspberry Pi 5) and kills it, never the machine, the moment it looks
# like locking the Pi up (the owner, 2026-11-13: "the device just crashed, so becareful", "have something detect that
# lock up and terminate the process if it happens"). Every two seconds it checks:
#   - the run's output: no new line for STALL_S seconds (a GPU driver stuck compiling a shader stopped the heavy-fleet
#     battle's frames for minutes, docs/playtests/pi/README.md);
#   - memory: MemAvailable under MIN_AVAIL_MB, or swap more than MAX_SWAP_PCT used;
#   - heat: the SoC over MAX_TEMP_C (vcgencmd);
#   - the kernel log: a new v3d, vc4 or drm error or an out-of-memory kill since the run began;
#   - time: past LIMIT_S.
# On any of them it asks the run to quit (SIGINT, as Ctrl+C, then SIGTERM, ten seconds apart) and only then kills it
# (SIGKILL): a run killed outright while the GPU still has its work in flight is one suspect for the Pi's hard stalls of
# 2026-11-13, so it is the last resort. It says why on stderr and in the log. Usage: scripts/pi-watchdog.sh [-l log] [-t limit_s] [-s stall_s] -- command args...
# The command's stdout and stderr go to the log (default /tmp/pi-watchdog.log), which the stall check reads.
set -uo pipefail
LOG=/tmp/pi-watchdog.log LIMIT_S=600 STALL_S=45
MIN_AVAIL_MB=${MIN_AVAIL_MB:-700} MAX_SWAP_PCT=${MAX_SWAP_PCT:-60} MAX_TEMP_C=${MAX_TEMP_C:-82}
while [[ $# -gt 0 && $1 != -- ]]; do
  case $1 in -l) LOG=$2; shift 2 ;; -t) LIMIT_S=$2; shift 2 ;; -s) STALL_S=$2; shift 2 ;; *) echo "pi-watchdog: unknown option $1" >&2; exit 2 ;; esac
done
[[ ${1:-} == -- ]] && shift
[[ $# -gt 0 ]] || { echo "usage: pi-watchdog.sh [-l log] [-t limit_s] [-s stall_s] -- command..." >&2; exit 2; }

: > "$LOG"
kmark=$(dmesg 2>/dev/null | wc -l)
# background jobs ignore SIGINT; give the run back its default so the polite quit below reaches it
# (a shell cannot undo an ignore it started with, so Python resets it and execs the run)
setsid python3 -c 'import os, signal, sys; signal.signal(signal.SIGINT, signal.SIG_DFL); os.execvp(sys.argv[1], sys.argv[1:])' "$@" >> "$LOG" 2>&1 &
pid=$!
start=$(date +%s) last_size=0 last_change=$start reason=""

why() { reason=$1; echo "PI_WATCHDOG killed the run: $1" | tee -a "$LOG" >&2; }

while kill -0 "$pid" 2>/dev/null; do
  sleep 2
  now=$(date +%s)
  size=$(stat -c %s "$LOG" 2>/dev/null || echo 0)
  if [[ $size != "$last_size" ]]; then last_size=$size; last_change=$now; fi
  avail=$(awk '/MemAvailable/ {print int($2/1024)}' /proc/meminfo)
  swap=$(awk '/SwapTotal/ {t=$2} /SwapFree/ {f=$2} END {print (t > 0 ? int(100 * (t - f) / t) : 0)}' /proc/meminfo)
  temp=$(vcgencmd measure_temp 2>/dev/null | grep -oE '[0-9]+' | head -1)
  knew=$(dmesg 2>/dev/null | tail -n +$((kmark + 1)) | grep -iE 'v3d|vc4|drm.*(error|timeout|hang)|out of memory|oom-kill' | head -1)
  if (( now - start > LIMIT_S )); then why "ran past its ${LIMIT_S} s limit"
  elif (( now - last_change > STALL_S )); then why "no output for ${STALL_S} s (stalled)"
  elif (( avail < MIN_AVAIL_MB )); then why "memory low: ${avail} MB available"
  elif (( swap > MAX_SWAP_PCT )); then why "swap ${swap}% used"
  elif [[ -n $temp ]] && (( temp > MAX_TEMP_C )); then why "too hot: ${temp} C"
  elif [[ -n $knew ]]; then why "kernel: $knew"
  fi
  if [[ -n $reason ]]; then
    for sig in INT TERM; do
      kill -$sig -- "-$pid" 2>/dev/null
      for _ in 1 2 3 4 5 6 7 8 9 10; do kill -0 "$pid" 2>/dev/null || break; sleep 1; done
      kill -0 "$pid" 2>/dev/null || break
    done
    kill -0 "$pid" 2>/dev/null && { echo "PI_WATCHDOG had to kill the run outright" | tee -a "$LOG" >&2; kill -KILL -- "-$pid" 2>/dev/null; }
    wait "$pid" 2>/dev/null
    exit 124
  fi
done
wait "$pid"
