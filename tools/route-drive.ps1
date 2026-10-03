# Usage (from project root): .\tools\route-drive.ps1 -Name d1
# Autopilot drive from the Bayview road junction over the causeway, through the Headland Tunnel, into
# Hillcrest and up Via Crest to the castle (TurnReview --route-drive). Writes review/map-sweep/<Name>/route-drive.txt.
param([Parameter(Mandatory)][ValidatePattern('^[a-z0-9-]+$')][string]$Name)
$ErrorActionPreference='Stop'
. "$PSScriptRoot/quiet-start.ps1"
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$folder=Join-Path $root "review/map-sweep/$Name"
if(Test-Path -LiteralPath $folder){throw "Route drive $Name already exists"}
New-Item -ItemType Directory -Path $folder | Out-Null
$exe=[IO.Path]::GetFullPath((Join-Path $root '../little-road-dev-build/Little Road Unity.exe'))
Copy-Item (Join-Path $root 'tools/fixtures/watch-cars-save.json') (Join-Path $folder 'save.json')
$run=Start-Quiet -FilePath $exe -WindowStyle Hidden -PassThru -ArgumentList @('-batchmode','--turn-review','--route-drive','--no-valley-life','--adventure-save',('"'+(Join-Path $folder 'save.json')+'"'),'--trial-captures',('"'+$folder+'"'),'-logFile',('"'+(Join-Path $folder 'player.log')+'"'))
$run.WaitForExit()
Get-Content (Join-Path $folder 'route-drive.txt')
