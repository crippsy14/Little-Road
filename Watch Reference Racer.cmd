@echo off
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0tools\run-technical-trial.ps1" -Executable "%~dp0..\little-road-racing-line-06-review\Little Road Unity.exe" -Visible -Fullscreen -Loadout club -Laps 3 -Policy reference -RacingLine cedar-smooth-v2 -Captures
if errorlevel 1 pause
