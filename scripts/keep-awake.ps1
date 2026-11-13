# keep-awake.ps1: keeps this Windows machine's display on and its session unlocked while a playtest, a recording or a
# co-op game runs (owner, 2026-11-11: "there should be wake scripts", "Same on windows"). Windows' own power request
# (SetThreadExecutionState with ES_CONTINUOUS, ES_SYSTEM_REQUIRED and ES_DISPLAY_REQUIRED) holds the display and the system
# awake, so the screen saver and its lock never start. It cannot unlock a session that is already locked: run it before.
#
#   powershell -File scripts/keep-awake.ps1                 hold until Ctrl-C
#   powershell -File scripts/keep-awake.ps1 -Hours 4        hold for 4 hours
#   powershell -File scripts/keep-awake.ps1 -- <cmd> <args> hold while <cmd> runs
param([double]$Hours = 0, [Parameter(ValueFromRemainingArguments = $true)][string[]]$Command)
$ErrorActionPreference = 'Stop'
Add-Type -Namespace FleetCommand -Name Power -MemberDefinition @'
[System.Runtime.InteropServices.DllImport("kernel32.dll")]
public static extern uint SetThreadExecutionState(uint esFlags);
'@
$ES_CONTINUOUS = [uint32]'0x80000000'; $ES_SYSTEM_REQUIRED = [uint32]1; $ES_DISPLAY_REQUIRED = [uint32]2
function Hold { [void][FleetCommand.Power]::SetThreadExecutionState($ES_CONTINUOUS -bor $ES_SYSTEM_REQUIRED -bor $ES_DISPLAY_REQUIRED) }
function Release { [void][FleetCommand.Power]::SetThreadExecutionState($ES_CONTINUOUS) }
try {
    Hold
    if ($Command -and $Command.Count -gt 0) {
        $args2 = if ($Command[0] -eq '--') { $Command[1..($Command.Count - 1)] } else { $Command }
        Write-Host "keeping the display awake while $($args2[0]) runs"
        $p = Start-Process -FilePath $args2[0] -ArgumentList $args2[1..($args2.Count - 1)] -NoNewWindow -PassThru
        while (-not $p.HasExited) { Hold; Start-Sleep -Seconds 30 }
        exit $p.ExitCode
    }
    $end = if ($Hours -gt 0) { (Get-Date).AddHours($Hours) } else { [DateTime]::MaxValue }
    Write-Host ($(if ($Hours -gt 0) { "keeping the display awake for $Hours h" } else { "keeping the display awake until Ctrl-C" }))
    while ((Get-Date) -lt $end) { Hold; Start-Sleep -Seconds 30 }
}
finally { Release }
