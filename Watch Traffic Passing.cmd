@echo off
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0tools\run-technical-trial.ps1" -Executable "%~dp0..\little-road-traffic-04-review\Little Road Unity.exe" -Visible -Fullscreen -Loadout club -Opponents street,stock -Cars 3 -Laps 1 -Policy traffic -RacingLine cedar-smooth-v2 -PhysicalRecovery -TrafficCase queue
if errorlevel 1 pause
