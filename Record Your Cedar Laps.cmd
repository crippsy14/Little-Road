@echo off
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0tools\run-technical-trial.ps1" -Executable "%~dp0..\little-road-racing-line-04-review\Little Road Unity.exe" -Visible -Manual -Fullscreen -Loadout club -Laps 4 -Captures
if errorlevel 1 pause
