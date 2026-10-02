# Usage (from project root): .\tools\capture-map.ps1 -Name before
# Runs the dev build's world review with the fixed Bayview map-sweep views (--map-views) and saves
# captures plus the road clearance/support evidence to review/map-sweep/<Name>. Never overwrites a run.
param([Parameter(Mandatory)][ValidatePattern('^[a-z0-9-]+$')][string]$Name,[string[]]$Flags=@(),[switch]$Rendered)
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$folder=Join-Path $root "review/map-sweep/$Name"
if(Test-Path -LiteralPath $folder){throw "Map capture $Name already exists"}
New-Item -ItemType Directory -Path $folder | Out-Null
$exe=[IO.Path]::GetFullPath((Join-Path $root '../little-road-dev-build/Little Road Unity.exe'))
Copy-Item (Join-Path $root 'tools/fixtures/watch-cars-save.json') (Join-Path $folder 'save.json')
$mode=@(if($Rendered){'--start-windowed','-screen-width','1920','-screen-height','1080','-screen-fullscreen','0','-monitor','1'}else{'-batchmode'})
$arguments=$mode+@('--world-review','--map-views','--adventure-save',('"'+(Join-Path $folder 'save.json')+'"'),'--trial-captures',('"'+$folder+'"'),'-logFile',('"'+(Join-Path $folder 'player.log')+'"'))+$Flags
$run=if($Rendered){Start-Process -FilePath $exe -PassThru -ArgumentList $arguments}else{Start-Process -FilePath $exe -WindowStyle Hidden -PassThru -ArgumentList $arguments}
$run.WaitForExit()
Write-Output "exit=$($run.ExitCode)"
Select-String -LiteralPath (Join-Path $folder 'player.log') -Pattern 'Exception|WORLD_REVIEW_COMPLETE' | Select-Object -First 6 | ForEach-Object { $_.Line }
