@echo off
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0tools\run-technical-trial.ps1" -Executable "%~dp0..\little-road-traffic-07-review\Little Road Unity.exe" -Visible -Loadout club -Cars 24 -Laps 3 -Policy traffic -RacingLine cedar-smooth-v2 -PhysicalRecovery -TrafficCase field -PlaybackRate 1
if errorlevel 1 pause
