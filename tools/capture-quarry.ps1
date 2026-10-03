# Usage (from project root): .\tools\capture-quarry.ps1 -Name q02 [-Shots "name:x,y,z>lx,ly,lz;..."] [-Flags @(...)]
# Rendered stills of Quarry Hills (docs/59) from fixed viewpoints, with --quarry-hills, saved to review/quarry-hills/<Name>.
# The same views every time so each build can be compared with the last. Never overwrites a run.
param([Parameter(Mandatory)][ValidatePattern('^[a-z0-9-]+$')][string]$Name,[string]$Shots='',[string[]]$Flags=@())
$ErrorActionPreference='Stop'
. "$PSScriptRoot/quiet-start.ps1"
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$folder=Join-Path $root "review/quarry-hills/$Name"
if(Test-Path -LiteralPath $folder){throw "Quarry capture $Name already exists"}
New-Item -ItemType Directory -Path $folder | Out-Null
$exe=[IO.Path]::GetFullPath((Join-Path $root '../little-road-dev-build/Little Road Unity.exe'))
Copy-Item (Join-Path $root 'tools/fixtures/watch-cars-save.json') (Join-Path $folder 'save.json')
if(-not $Shots){$Shots=@(
  'overview:-900,1300,-1500>-2600,0,-2950','reveal:-1765,86,-1915>-2450,35,-2650','town:-2380,80,-2120>-2650,30,-2400',
  'galleries:-2350,62,-2930>-1800,130,-3560','pitlake:-2250,110,-3120>-1900,31,-3420','tips:-2850,85,-2480>-3250,100,-2870',
  'quay:-2780,25,-1930>-3080,-9,-1750','gateway:-300,45,-950>-1250,25,-1380','seam:-650,170,-1750>-1800,40,-2650',
  'rimview:-1650,260,-3800>-2600,30,-2400','edgewest:-2500,160,-2600>-4300,150,-3200','galleryclose:-2050,120,-3250>-1750,160,-3640') -join ';'}
$arguments=@('--start-windowed','-screen-width','1600','-screen-height','900','-screen-fullscreen','0','-monitor','1','--silent','--world-review','--quarry-hills',
  '--adventure-save',('"'+(Join-Path $folder 'save.json')+'"'),'--trial-captures',('"'+$folder+'"'),'--view-shots',('"'+$Shots+'"'),'-logFile',('"'+(Join-Path $folder 'player.log')+'"'))+$Flags
$run=Start-Quiet -FilePath $exe -PassThru -ArgumentList $arguments
$run.WaitForExit()
Write-Output "exit=$($run.ExitCode)"
Select-String -LiteralPath (Join-Path $folder 'player.log') -Pattern 'Exception|QUARRY|VIEW_FRAME|LOAD_STEP (mainland|quarry)|VALLEY_FIELD' | Select-Object -First 40 | ForEach-Object { $_.Line }
