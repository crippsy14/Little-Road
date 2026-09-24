# -Version 0 (default) runs the reusable dev build (tools/build-dev.ps1); N>0 runs ../little-road-context-N-review.
# Per-tick telemetry (~75 MB/race) is deleted after the run unless -KeepTelemetry; race.json etc. stay.
param([int]$Version=0,[int]$Study=104,[ValidateSet('bayview','cedar')][string]$Track='bayview',[int]$Seed=1709,[switch]$Rendered,[switch]$StaggerContextPlans,[switch]$PaceAudit,[ValidateRange(1,24)][int]$ContextPlanGroups=3,[ValidatePattern('^[a-z0-9-]+$')][string]$Name='baseline',[string[]]$Extra=@(),[ValidateRange(.25,4)][float]$PlaybackRate=1,[switch]$KeepTelemetry)
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$exe=if($Version -gt 0){"../little-road-context-$Version-review/Little Road Unity.exe"}else{"../little-road-dev-build/Little Road Unity.exe"}
# AI behaviour comes from the game's shared preset (RaceAiPreset in RaceRoster.cs, the same one adventure races use);
# only race settings are passed here. Drop one preset switch with -Extra @('--preset-without','--some-switch').
$opts=@{Executable=(Join-Path $root $exe);Track=$Track;Loadout='club';Cars=24;Laps=1;Duration=180;Seed=$Seed;Policy='context';RacingLine='cedar-smooth-v2';TrafficCase='field';DriverRisk=5;PassProcess=$true;RaceHud=$true;Width=1920;Height=1080;CleanView=$true;PlaybackRate=$PlaybackRate}
$opts.ExtraArguments=@('--ai-preset','field')+$Extra
if($Track -eq 'bayview'){$opts.BayviewTransferStudy=$true;$opts.RacingLine='bayview-smooth-v1'}
if($PaceAudit){$opts.PedalStageAudit=$true;$opts.ContextPlanAudit=$true;$opts.OpeningDecisionAudit=$true;$opts.ContinuationAudit=$true}
# Staggered planning (3 groups) is part of the preset; -StaggerContextPlans is accepted for old commands.
if($ContextPlanGroups -ne 3){$opts.ExtraArguments+=@('--context-plan-groups',$ContextPlanGroups.ToString())}
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
