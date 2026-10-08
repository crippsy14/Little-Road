# Usage (from project root): .\tools\event-race-review.ps1 -Event b_coast -Name r1 [-Serial n] [-Flags @('--field-recipe','--claim-tuner','sakura')]
# Enters any calendar event (docs/67: the class B Tuner races) from its preview on a copy of the watch-cars save, with the
# player's car on autopilot (--event-race-review), waits for the player to finish and the reward to settle, then closes.
# Writes review/class-b/<Event>-<Name>/ (player.log, save.json, race.json); per-tick telemetry is dropped unless -KeepTelemetry.
param([Parameter(Mandatory)][ValidatePattern('^[a-z0-9_]+$')][string]$Event,[Parameter(Mandatory)][ValidatePattern('^[a-z0-9-]+$')][string]$Name,[int]$Serial=-1,[int]$TimeoutSeconds=600,[string[]]$Flags=@(),[switch]$KeepTelemetry)
$ErrorActionPreference='Stop'
. "$PSScriptRoot/quiet-start.ps1"
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$folder=Join-Path $root "review/class-b/$Event-$Name"
if(Test-Path -LiteralPath $folder){throw "Event race review $Event-$Name already exists"}
New-Item -ItemType Directory -Path $folder | Out-Null
$exe=[IO.Path]::GetFullPath((Join-Path $root '../little-road-dev-build/Little Road Unity.exe'))
$save=Join-Path $folder 'save.json';$fixture=Get-Content (Join-Path $root 'tools/fixtures/watch-cars-save.json') -Raw
if($Serial -ge 0){$fixture=$fixture -replace '"raceSerial":\s*\d+',"`"raceSerial`": $Serial" -replace '"settledSerial":\s*\d+',"`"settledSerial`": $Serial"}
Set-Content -LiteralPath $save -Value $fixture -Encoding utf8
$log=Join-Path $folder 'player.log'
$run=Start-Quiet -FilePath $exe -WindowStyle Hidden -PassThru -ArgumentList (@('-batchmode','--event-race-review',$Event,'--adventure-save',('"'+$save+'"'),'--trial-report',('"'+(Join-Path $folder 'race.json')+'"'),'-logFile',('"'+$log+'"'))+$Flags)
$deadline=(Get-Date).AddSeconds($TimeoutSeconds)
while(-not $run.HasExited -and (Get-Date) -lt $deadline){
  Start-Sleep -Seconds 5
  # Wait for the whole field (the total time counts every car), then let the reward settle.
  if((Test-Path $log) -and @(Select-String -LiteralPath $log -Pattern 'FINISH id=\d+ ').Count -ge 24){Start-Sleep -Seconds 10;break}
}
if(-not $run.HasExited){Stop-Process -Id $run.Id -Force}
# Per-tick telemetry (~75 MB a race) goes unless -KeepTelemetry, as in run-frame-cost; race.json and the rest stay.
Start-Sleep -Seconds 2
if(-not $KeepTelemetry){Get-ChildItem -LiteralPath $folder -File|Where-Object{$_.Name -match '\.(track|driver-frames|traffic|motion-prediction|path-prediction)\.json$'}|Remove-Item -Force -ErrorAction SilentlyContinue}
Select-String -LiteralPath $log -Pattern 'Exception|FINISH id=0 |ASSISTED|RESET' | Select-Object -First 12 | ForEach-Object { $_.Line }
Get-Content $save | Select-String 'credits|raceSerial|settledSerial|wins|b_'
