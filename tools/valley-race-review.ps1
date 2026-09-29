# Usage (from project root): .\tools\valley-race-review.ps1 -Name c1
# Enters the Valley Cup from adventure mode (Vito's preview, confirm) on a copy of the watch-cars
# save with the player's car on autopilot (--valley-race-review), waits for the player to finish and the
# reward to settle, then closes. Writes review/hillcrest-race/<Name>/ (player.log, save.json).
param([Parameter(Mandatory)][ValidatePattern('^[a-z0-9-]+$')][string]$Name,[int]$TimeoutSeconds=420)
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$folder=Join-Path $root "review/hillcrest-race/$Name"
if(Test-Path -LiteralPath $folder){throw "Valley race review $Name already exists"}
New-Item -ItemType Directory -Path $folder | Out-Null
$exe=[IO.Path]::GetFullPath((Join-Path $root '../little-road-dev-build/Little Road Unity.exe'))
$save=Join-Path $folder 'save.json';Copy-Item (Join-Path $root 'review/watch-cars/save.json') $save
$log=Join-Path $folder 'player.log'
$run=Start-Process -FilePath $exe -WindowStyle Hidden -PassThru -ArgumentList @('-batchmode','--valley-race-review','--adventure-save',('"'+$save+'"'),'-logFile',('"'+$log+'"'))
$deadline=(Get-Date).AddSeconds($TimeoutSeconds)
while(-not $run.HasExited -and (Get-Date) -lt $deadline){
  Start-Sleep -Seconds 5
  if((Test-Path $log) -and (Select-String -LiteralPath $log -Pattern 'FINISH id=0 ' -Quiet)){Start-Sleep -Seconds 8;break}
}
if(-not $run.HasExited){Stop-Process -Id $run.Id -Force}
Select-String -LiteralPath $log -Pattern 'Exception|UNITY_READY|FINISH id=0 |ASSISTED' | Select-Object -First 12 | ForEach-Object { $_.Line }
Get-Content $save | Select-String 'credits|raceSerial|settledSerial|bestPlace'
