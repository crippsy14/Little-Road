@echo off
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0tools\run-technical-trial.ps1" -Executable "%~dp0..\little-road-recovery-11-review\Little Road Unity.exe" -Visible -Fullscreen -Loadout stock -Laps 1 -Policy reference -RacingLine cedar-smooth-v2 -PhysicalRecovery -RecoveryCase wall -Captures
if errorlevel 1 pause
