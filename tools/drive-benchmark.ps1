# Usage (from project root): .\tools\drive-benchmark.ps1 -Name db1 [-Flags '--time','night']
# Rendered 1920x1080 autopilot drive (stock car with the Precision rack, 20 m/s cap, no moving traffic: it blocked the autopilot) (TurnReview --route-drive: Bayview junction, causeway, Headland
# Tunnel, Hillcrest, up to the castle) with valley life on, recording every frame (FrameProbe):
# review/map-sweep/<Name>/frame-probe.txt (percentiles, worst frames, cost by area) and frames.csv.
param([Parameter(Mandatory)][ValidatePattern('^[a-z0-9-]+$')][string]$Name,[string[]]$Flags=@())
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$folder=Join-Path $root "review/map-sweep/$Name"
if(Test-Path -LiteralPath $folder){throw "Drive benchmark $Name already exists"}
New-Item -ItemType Directory -Path $folder | Out-Null
$exe=[IO.Path]::GetFullPath((Join-Path $root '../little-road-dev-build/Little Road Unity.exe'))
# A stock car with the Precision rack (steady, repeatable pace), from the watch-cars save.
$save=Get-Content (Join-Path $root 'tools/fixtures/watch-cars-save.json') -Raw | ConvertFrom-Json
foreach($f in $save.fitted){$f.id=$f.slot+'_stock'};($save.fitted | Where-Object {$_.slot -eq 'steering'}).id='steering_precision'
if($save.owned -notcontains 'steering_precision'){$save.owned+= 'steering_precision'}
[IO.File]::WriteAllText((Join-Path $folder 'save.json'),($save | ConvertTo-Json -Depth 5))
# (--start-windowed: otherwise the player's saved display setting wins, and runs at 2560x1440 and 3840x2160 were compared as if alike: 2 Oct.)
$arguments=@('--silent','--start-windowed','-screen-width','1920','-screen-height','1080','-screen-fullscreen','0','-monitor','1','--turn-review','--route-drive','--route-speed','20','--no-valley-life','--adventure-save',('"'+(Join-Path $folder 'save.json')+'"'),'--trial-captures',('"'+$folder+'"'),'--frame-probe',('"'+$folder+'"'),'-logFile',('"'+(Join-Path $folder 'player.log')+'"'))+$Flags
$run=Start-Process -FilePath $exe -PassThru -ArgumentList $arguments
$run.WaitForExit()
Write-Output "exit=$($run.ExitCode)"
$summary=Join-Path $folder 'frame-probe.txt'
if(Test-Path $summary){Get-Content $summary | Select-Object -First 8}else{Write-Output 'No frame-probe.txt (see player.log)'}
