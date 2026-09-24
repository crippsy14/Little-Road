param(
  [string]$Godot = 'C:\Users\cripp\Documents\Godot\Godot_v4.7.2-stable_win64_console.exe',
  [switch]$Capture
)
$ErrorActionPreference = 'Stop'
$taskProject = Split-Path -Parent $PSScriptRoot
$taskGame = Join-Path $taskProject 'game'
if (-not (Test-Path -LiteralPath $Godot)) { throw 'Supply the Godot console executable using -Godot.' }
& $Godot --headless --editor --path $taskGame --import 2>&1 | Tee-Object -Variable taskImportOutput
if ($LASTEXITCODE -ne 0 -or ($taskImportOutput -match 'SCRIPT ERROR:|^ERROR:')) { throw 'Godot import failed.' }
& $Godot --headless --path $taskGame --fixed-fps 60 --quit-after 3000 --script res://tests/run_tests.gd 2>&1 | Tee-Object -Variable taskTestOutput
if ($LASTEXITCODE -ne 0 -or ($taskTestOutput -match 'SCRIPT ERROR:|^ERROR:') -or -not ($taskTestOutput -match 'LITTLE_ROAD_TESTS')) { throw 'Functional checks failed or did not complete.' }
& $Godot --headless --path $taskGame --script res://tests/power_trials.gd 2>&1 | Tee-Object -Variable taskPowerOutput
if ($LASTEXITCODE -ne 0 -or ($taskPowerOutput -match 'SCRIPT ERROR:|^ERROR:|^FAIL ') -or -not ($taskPowerOutput -match 'POWER_TRIALS 9/9')) { throw 'Power, grip or save migration checks failed.' }
if ($Capture) {
  $taskCapture = Join-Path $taskProject ('review\captures\' + (Get-Date -Format 'yyyyMMdd-HHmmss'))
  & $Godot --path $taskGame --resolution 1600x900 -- "--capture-dir=$taskCapture"
  if ($LASTEXITCODE -ne 0) { throw 'Capture run failed.' }
  Write-Output "Images saved to $taskCapture"
}
