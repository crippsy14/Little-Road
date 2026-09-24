@echo off
set "LITTLE_ROAD_GODOT=C:\Users\cripp\Documents\Godot\Godot_v4.7.2-stable_win64.exe"
if not exist "%LITTLE_ROAD_GODOT%" (
  echo Godot was not found at the configured location.
  echo Open game\project.godot in Godot 4.7.2 instead.
  pause
  exit /b 1
)
"%LITTLE_ROAD_GODOT%" --path "%~dp0game"
