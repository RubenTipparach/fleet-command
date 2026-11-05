@echo off
rem Quick launch for a playtest on Windows: build the C# and start the game.
rem   play.bat                          the game, from the main scene (pick a scenario in the view)
rem   play.bat krai_battle              straight into a scenario
rem   play.bat edit                     the Godot editor on this project instead
rem The first run installs the pinned Godot .NET editor (build.config) into .tools\godot with the repo's own
rem scripts\install-godot.sh, through Git Bash. The C# builds with the system .NET 8 SDK.
setlocal
cd /d "%~dp0"

set "GODOT_DIR=%~dp0.tools\godot"
set "GODOT=%GODOT_DIR%\Godot_v4.7.1-stable_mono_win64.exe"
set "GODOT_CONSOLE=%GODOT_DIR%\Godot_v4.7.1-stable_mono_win64_console.exe"
set "BASH=%ProgramFiles%\Git\bin\bash.exe"
set DOTNET_CLI_TELEMETRY_OPTOUT=1
set DOTNET_NOLOGO=1

if not exist "%GODOT_CONSOLE%" (
  echo == Installing Godot 4.7.1 .NET into .tools\godot ^(first run only^)
  "%BASH%" scripts/install-godot.sh >nul || goto :fail
)

if /i "%~1"=="edit" (
  start "" "%GODOT%" --path . -e
  exit /b 0
)

echo == Building the C# ^(FleetCommand.csproj^)
dotnet build FleetCommand.csproj -v q --nologo || goto :fail

rem Every launch: Godot's headless import writes the .import file beside any new or changed asset (they are
rem committed) and refreshes the cache in .godot (git ignored). Quick when nothing changed; minutes on a fresh clone.
echo == Importing assets
"%GODOT_CONSOLE%" --headless --path . --import >nul 2>&1

echo == Starting Fleet Command
if "%~1"=="" (
  start "" "%GODOT%" --path .
) else (
  start "" "%GODOT%" --path . -- --scenario=%~1
)
exit /b 0

:fail
echo.
echo Launch failed, see the lines above.
pause
exit /b 1
