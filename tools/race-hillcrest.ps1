# Usage (from project root): .\tools\race-hillcrest.ps1 -Name r2 [-Seed 1709] [-Laps 2] [-Flags @()] [-KeepTelemetry]
# Standalone 24-car Hillcrest Raceway trial on the dev build (legacy drivers, the adventure's
# Hillcrest AI), batch mode at 4x. Writes review/hillcrest-race/<Name>/race.json; never overwrites.
param([Parameter(Mandatory)][ValidatePattern('^[a-z0-9-]+$')][string]$Name,[int]$Seed=1709,[int]$Laps=2,[string[]]$Flags=@(),[switch]$KeepTelemetry)
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$folder=Join-Path $root "review/hillcrest-race/$Name"
if(Test-Path -LiteralPath $folder){throw "Hillcrest race $Name already exists"}
New-Item -ItemType Directory -Path $folder | Out-Null
$exe=[IO.Path]::GetFullPath((Join-Path $root '../little-road-dev-build/Little Road Unity.exe'))
$arguments=@('-batchmode','--trial-track','hillcrest','--trial-auto','--trial-cars','24','--trial-laps',$Laps,'--trial-duration','900','--trial-seed',$Seed,'--trial-timescale','4',
  '--trial-report',('"'+(Join-Path $folder 'race.json')+'"'),'-logFile',('"'+(Join-Path $folder 'player.log')+'"'))+$Flags
$run=Start-Process -FilePath $exe -WindowStyle Hidden -PassThru -ArgumentList $arguments
$run.WaitForExit()
if(-not $KeepTelemetry){Get-ChildItem -LiteralPath $folder -File|Where-Object{$_.Name -match '\.(track|driver-frames|traffic|motion-prediction|path-prediction)\.json$'}|Remove-Item -Force}
Write-Output "exit=$($run.ExitCode)"
