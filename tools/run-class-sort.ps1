# Usage (from project root): .\tools\run-class-sort.ps1 -Name base [-Tracks @('cedar','hillcrest')] [-Seeds @(1709,1710,1711)] [-Laps 2] [-Flags @()] [-MixedDrivers] [-KeepTelemetry]
# Overtaking fixture: 15 context cars on a class-mixed standing grid, rows front to back
#   3-3-3 / 2-2-2 / 3-3-3 / 1-1-1 / (empty) / 2-2-2   (1 = club kit, fastest; 2 = street; 3 = stock)
# Clean racing should sort the field by class (1s, then 2s, then 3s). Drivers are uniform ('neutral',
# no form) so equipment is the only pace difference; -MixedDrivers keeps the adventure's varied roster.
# Writes review/class-sort/<Name>-<track>-s<seed>/ (never overwrites), runs sequentially (the races are
# deterministic only one at a time), then prints tools/analyze-class-sort.py for the new runs.
param([Parameter(Mandatory)][ValidatePattern('^[a-z0-9-]+$')][string]$Name,[ValidateSet('cedar','bayview','hillcrest','cedar-hills')][string[]]$Tracks=@('cedar','hillcrest'),
  [int[]]$Seeds=@(1709,1710,1711),[int]$Laps=2,[string[]]$Flags=@(),[switch]$MixedDrivers,[switch]$KeepTelemetry)
$ErrorActionPreference='Stop'
. "$PSScriptRoot/quiet-start.ps1"
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$exe=[IO.Path]::GetFullPath((Join-Path $root '../little-road-dev-build/Little Road Unity.exe'))
$runs=@()
foreach($track in $Tracks){foreach($seed in $Seeds){
  $folder=Join-Path $root "review/class-sort/$Name-$track-s$seed"
  if(Test-Path -LiteralPath $folder){Write-Output "skip existing $Name-$track-s$seed";$runs+=$folder;continue}
  New-Item -ItemType Directory -Path $folder|Out-Null
  $arguments=@('-batchmode','--trial-track',$track,'--trial-auto','--trial-cars','15','--trial-laps',$Laps,'--trial-duration','900','--trial-seed',$seed,'--trial-timescale','4',
    '--ai-preset','field','--traffic-case','class-sort','--trial-report',('"'+(Join-Path $folder 'race.json')+'"'),'-logFile',('"'+(Join-Path $folder 'player.log')+'"'))
  if($track -eq 'bayview' -or $track -eq 'hillcrest'){$arguments+='--bayview-transfer-study'}
  if($track -eq 'cedar-hills'){$arguments+='--grade-aware'}
  if(-not $MixedDrivers){$arguments+=@('--driver-roster','neutral','--preset-without','--driver-form')}
  $arguments+=$Flags
  $t=Get-Date
  $run=Start-Quiet -FilePath $exe -WindowStyle Hidden -PassThru -ArgumentList $arguments
  $run.WaitForExit()
  if(-not $KeepTelemetry){Get-ChildItem -LiteralPath $folder -File|Where-Object{$_.Name -match '\.(track|driver-frames|traffic|motion-prediction|path-prediction)\.json$'}|Remove-Item -Force}
  Write-Output ("{0}-{1}-s{2} exit={3} took {4:n0}s" -f $Name,$track,$seed,$run.ExitCode,((Get-Date)-$t).TotalSeconds)
  $runs+=$folder
}}
$python=(Get-Command python -ErrorAction SilentlyContinue).Source
if(-not $python -or $python -like '*WindowsApps*'){$python=Get-ChildItem 'C:\Program Files\Blender Foundation\*\*\python\bin\python.exe' -ErrorAction SilentlyContinue|Select-Object -First 1 -ExpandProperty FullName}
if($python){& $python (Join-Path $PSScriptRoot 'analyze-class-sort.py') @runs}
