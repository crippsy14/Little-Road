# Usage (from project root): .\tools\capture-map.ps1 -Name before
# Runs the dev build's world review with the fixed Bayview map-sweep views (--map-views) and saves
# captures plus the road clearance/support evidence to review/map-sweep/<Name>. Never overwrites a run.
param([Parameter(Mandatory)][ValidatePattern('^[a-z0-9-]+$')][string]$Name,[string[]]$Flags=@())
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$folder=Join-Path $root "review/map-sweep/$Name"
if(Test-Path -LiteralPath $folder){throw "Map capture $Name already exists"}
New-Item -ItemType Directory -Path $folder | Out-Null
$exe=[IO.Path]::GetFullPath((Join-Path $root '../little-road-dev-build/Little Road Unity.exe'))
Copy-Item (Join-Path $root 'review/watch-cars/save.json') (Join-Path $folder 'save.json')
$arguments=@('-batchmode','--world-review','--map-views','--adventure-save',('"'+(Join-Path $folder 'save.json')+'"'),'--trial-captures',('"'+$folder+'"'),'-logFile',('"'+(Join-Path $folder 'player.log')+'"'))+$Flags
$run=Start-Process -FilePath $exe -WindowStyle Hidden -PassThru -ArgumentList $arguments
$run.WaitForExit()
Write-Output "exit=$($run.ExitCode)"
Select-String -LiteralPath (Join-Path $folder 'player.log') -Pattern 'Exception|WORLD_REVIEW_COMPLETE' | Select-Object -First 6 | ForEach-Object { $_.Line }
