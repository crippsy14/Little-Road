# Usage (from project root, Unity editor closed): .\tools\build-dev.ps1
# Builds into one reusable folder, ../little-road-dev-build/, replacing the previous build.
# The log goes to review/build-dev.log and the witness to review/unity-01/dynamic-rejoin-witness.json (both replaced).
# After a good build, tools/cleanup.ps1 prunes old generated evidence under review/ (to the Recycle Bin).
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
if(Get-Process Unity -ErrorAction SilentlyContinue){throw 'Close the Unity editor before a batch build.'}
# Only a game running from the dev build blocks replacing it; the play copy (../little-road-play) can keep running.
$devDir=[IO.Path]::GetFullPath((Join-Path $root '../little-road-dev-build'))
if(Get-Process 'Little Road Unity' -ErrorAction SilentlyContinue|Where-Object{$_.Path -and $_.Path.StartsWith($devDir,[StringComparison]::OrdinalIgnoreCase)}){throw 'Close Little Road games running from the dev build before replacing it.'}
# Car Forge models are build output (tools/forge-cars.txt): export and install whatever changed since the last build.
# A fresh machine exports them all once (a few minutes); after that it takes a second.
$forge=[IO.Path]::GetFullPath((Join-Path $root '../car-forge')); $py='C:\Program Files\Blender Foundation\Blender 5.2\5.2\python\bin\python.exe'
if(-not (Test-Path (Join-Path $forge 'tools/sync_game.py'))){throw "Car Forge not found at $forge (the game's Forge cars are built from it)."}
$env:PYTHONPATH="$forge\vendor;$forge"; & $py (Join-Path $forge 'tools/sync_game.py') --game $root --jobs 6
if($LASTEXITCODE -ne 0){throw 'Car Forge sync failed (a car failed its export checks); build not started.'}
# Prop Forge's kits (docs/77), the same way: build output in Resources/Props, written only when a kit changed.
& $py (Join-Path $forge 'tools/sync_props.py') --game $root
if($LASTEXITCODE -ne 0){throw 'Prop Forge sync failed; build not started.'}
# Compile errors in ~2 s instead of after Unity's start-up (tools/compile-check.ps1).
& (Join-Path $PSScriptRoot 'compile-check.ps1') -Quiet
if($LASTEXITCODE -ne 0){throw 'Compile check failed; Unity build not started.'}
$folder=[IO.Path]::GetFullPath((Join-Path $root '../little-road-dev-build'))
if(Test-Path -LiteralPath $folder){Remove-Item -LiteralPath $folder -Recurse -Force}
$export=Join-Path $folder 'Little Road Unity.exe'
$log=Join-Path $root 'review/build-dev.log'
$unity='C:\Program Files\Unity\Hub\Editor\6000.6.1f1\Editor\Unity.exe'
$p=Start-Process -FilePath $unity -ArgumentList @('-batchmode','-quit','-projectPath',"`"$root\unity`"",'-executeMethod','MigrationBuild.Build','--review-export',"`"$export`"",'-logFile',"`"$log`"") -PassThru -Wait
$ok=Select-String -LiteralPath $log -Pattern 'LITTLE_ROAD_BUILD_OK' | Select-Object -First 1
if($p.ExitCode -ne 0 -or -not $ok){
  Select-String -LiteralPath $log -Pattern 'error CS|threw exception|Exception: ' | Select-Object -First 10 | ForEach-Object { $_.Line }
  throw "Dev build failed (exit $($p.ExitCode)); see review/build-dev.log"
}
$ok.Line
& (Join-Path $PSScriptRoot 'cleanup.ps1') -Apply -Quiet
