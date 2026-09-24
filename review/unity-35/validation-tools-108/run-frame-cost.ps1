param([int]$Version=104,[int]$Study=104,[ValidateSet('bayview','cedar')][string]$Track='bayview',[int]$Seed=1709,[switch]$Rendered,[switch]$StaggerContextPlans,[ValidateRange(1,24)][int]$ContextPlanGroups=3,[ValidatePattern('^[a-z0-9-]+$')][string]$Name='baseline')
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$opts=@{Executable=(Join-Path $root "../little-road-context-$Version-review/Little Road Unity.exe");Track=$Track;Loadout='club';Cars=24;Laps=1;Duration=180;Seed=1709;Policy='context';RacingLine='cedar-smooth-v2';TrafficCase='field';PhysicalRecovery=$true;TrafficDiagnostics=$true;RoadFeasibility=$true;PreparedPath=$true;GuidePathFallback=$true;CompareContinuation=$true;TrafficAwareProgress=$true;FollowingFeasibilityFallback=$true;SharedObservedTraffic=$true;ParallelContinuationForecast=$true;RearPressureFilter=$true;LightDrafting=$true;CoastWithinEnvelope=$true;DriverRisk=5;NoGapCentres=$true;TrafficPathCheck=$true;AnticipatePassingSpace=$true;TrafficCoastContinuation=$true;ParallelForecastWorkers=8;DynamicRejoin=$true;DynamicTraffic=$true;LocalOriginCurvature=$true;DynamicFieldStudy=$true;DynamicBlockingTarget=$true;DynamicTrafficFallback=$true;ParallelDynamicForecast=$true;OpenSpacePriority=$true;PassProcess=$true;DriverRoster='mixed';DriverForm=$true;RaceHud=$true;Width=1920;Height=1080;CleanView=$true;PlaybackRate=1}
if($Track -eq 'bayview'){$opts.BayviewTransferStudy=$true;$opts.RacingLine='bayview-smooth-v1'}
$opts.Seed=$Seed
if($StaggerContextPlans){$opts.StaggerContextPlans=$true;$opts.ContextPlanGroups=$ContextPlanGroups}
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
if($run.process.ExitCode -ne 0){throw 'Frame-cost trial failed'}
