# Races the reusable dev build (tools/build-dev.ps1).
# Per-tick telemetry (~75 MB/race) is deleted after the run unless -KeepTelemetry; race.json etc. stay.
param([int]$Study=104,[ValidateSet('bayview','cedar','cedar-hills','hillcrest','quarry','bramblewick','vinelands','vinelands-long')][string]$Track='bayview',[int]$Seed=1709,[switch]$Rendered,[ValidatePattern('^[a-z0-9-]+$')][string]$Name='baseline',[string[]]$Extra=@(),[ValidateRange(.25,4)][float]$PlaybackRate=1,[switch]$KeepTelemetry)
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$exe="../little-road-dev-build/Little Road Unity.exe"
# AI behaviour comes from the game's shared preset (RaceAiPreset in RaceRoster.cs, the same one adventure races use);
# only race settings are passed here. Drop one preset switch with -Extra @('--preset-without','--some-switch').
$opts=@{Executable=(Join-Path $root $exe);Track=$Track;Loadout='club';Cars=24;Laps=1;Duration=180;Seed=$Seed;Policy='racer';RacingLine='cedar-smooth-v2';TrafficCase='field';PassProcess=$true;RaceHud=$true;Width=1920;Height=1080;CleanView=$true;PlaybackRate=$PlaybackRate}
$opts.ExtraArguments=@('--ai-preset','field')+$Extra
# The town circuits race the field preset as the game does (Hillcrest and Quarry Hills bake their line at load; docs/67).
if($Track -in 'bayview','hillcrest','quarry','bramblewick','vinelands','vinelands-long'){$opts.BayviewTransferStudy=$true;$opts.RacingLine='bayview-smooth-v1'}
if($KeepTelemetry){$opts.ExtraArguments+='--telemetry'}   # (the game writes per-tick files only when asked)
if($Rendered){
 if(Get-Process Unity,'Little Road Unity' -ErrorAction SilentlyContinue){throw 'Close competing project editors/game processes before a rendered comparison.'}
 $opts.Visible=$true;$opts.Windowed=$true
}
$folder=Join-Path $root "review/unity-35/frame-cost-$Study/$Name";$opts.OutputDirectory=$folder
$run=& "$PSScriptRoot/run-technical-trial.ps1" @opts
$run.metadata|ConvertTo-Json -Depth 6|Set-Content (Join-Path $folder 'launch.json')
Write-Output "Started $Name pid=$($run.process.Id)"
$run.process.WaitForExit()
@{exitCode=$run.process.ExitCode;name=$Name}|ConvertTo-Json|Set-Content (Join-Path $folder 'exit.json')
Write-Output "Finished $Name exit=$($run.process.ExitCode)"
if(-not $KeepTelemetry){Get-ChildItem -LiteralPath $folder -File|Where-Object{$_.Name -match '\.(track|driver-frames|traffic|motion-prediction|path-prediction)\.json$'}|Remove-Item -Force}
if($run.process.ExitCode -ne 0){throw 'Frame-cost trial failed'}
