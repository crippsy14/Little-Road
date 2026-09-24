@echo off
powershell.exe -NoProfile -File "%~dp0tools\run-technical-trial.ps1" -Visible -Loadout club -Laps 3 -Captures
if errorlevel 1 pause
