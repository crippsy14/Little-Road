# Usage (from project root): .\tools\quarry-race-review.ps1 -Name r1 [-Seed 0]
# Enters the Quarry Hills Rallycross from adventure mode (Gwen's preview, confirm) on a copy of the watch-cars save with the
# player's car on autopilot (--quarry-hills --quarry-race-review), waits for the player to finish and the reward to settle,
# then closes. -Seed n sets the save's raceSerial to n (each serial is a different field and seed). Writes
# review/quarry-hills/race-<Name>/ (player.log, save.json, race.json).
param([Parameter(Mandatory)][ValidatePattern('^[a-z0-9-]+$')][string]$Name,[int]$Serial=-1,[int]$TimeoutSeconds=600)
$ErrorActionPreference='Stop'
. "$PSScriptRoot/quiet-start.ps1"
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$folder=Join-Path $root "review/quarry-hills/race-$Name"
if(Test-Path -LiteralPath $folder){throw "Quarry race review $Name already exists"}
New-Item -ItemType Directory -Path $folder | Out-Null
$exe=[IO.Path]::GetFullPath((Join-Path $root '../little-road-dev-build/Little Road Unity.exe'))
$save=Join-Path $folder 'save.json';$fixture=Get-Content (Join-Path $root 'tools/fixtures/watch-cars-save.json') -Raw
if($Serial -ge 0){$fixture=$fixture -replace '"raceSerial":\s*\d+',"`"raceSerial`": $Serial" -replace '"settledSerial":\s*\d+',"`"settledSerial`": $Serial"}
Set-Content -LiteralPath $save -Value $fixture -Encoding utf8
$log=Join-Path $folder 'player.log'
$run=Start-Quiet -FilePath $exe -WindowStyle Hidden -ArgumentList @('-batchmode','--quarry-hills','--quarry-race-review','--adventure-save',('"'+$save+'"'),'--trial-report',('"'+(Join-Path $folder 'race.json')+'"'),'-logFile',('"'+$log+'"'))
$deadline=(Get-Date).AddSeconds($TimeoutSeconds)
while(-not $run.HasExited -and (Get-Date) -lt $deadline){
  Start-Sleep -Seconds 5
  if((Test-Path $log) -and (Select-String -LiteralPath $log -Pattern 'FINISH id=0 ' -Quiet)){Start-Sleep -Seconds 10;break}
}
if(-not $run.HasExited){Stop-Process -Id $run.Id -Force}
Select-String -LiteralPath $log -Pattern 'Exception|FINISH id=0 |ASSISTED|TRIAL_REPORT|RESET' | Select-Object -First 14 | ForEach-Object { $_.Line }
Get-Content $save | Select-String 'credits|raceSerial|settledSerial|bestPlace|quarry'
