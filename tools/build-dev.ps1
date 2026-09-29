# Usage (from project root, Unity editor closed): .\tools\build-dev.ps1
# Builds into one reusable folder, ../little-road-dev-build/, replacing the previous build.
# The log goes to review/build-dev.log and the witness to review/unity-01/dynamic-rejoin-witness.json (both replaced).
# After a good build, tools/cleanup.ps1 prunes old generated evidence under review/ (to the Recycle Bin).
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
if(Get-Process Unity -ErrorAction SilentlyContinue){throw 'Close the Unity editor before a batch build.'}
if(Get-Process 'Little Road Unity' -ErrorAction SilentlyContinue){throw 'Close running Little Road games before replacing the dev build.'}
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
