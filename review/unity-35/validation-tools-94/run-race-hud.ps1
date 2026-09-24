param([int]$Version=94,[ValidateSet('review','parity','performance')][string]$Stage='review')
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$common=@{Executable=(Join-Path $root "../little-road-context-$Version-review/Little Road Unity.exe");Track='cedar';Loadout='club';Cars=24;Laps=1;Duration=180;Seed=1709;Policy='context';RacingLine='cedar-smooth-v2';TrafficCase='field';PhysicalRecovery=$true;TrafficDiagnostics=$true;RoadFeasibility=$true;PreparedPath=$true;GuidePathFallback=$true;CompareContinuation=$true;TrafficAwareProgress=$true;FollowingFeasibilityFallback=$true;SharedObservedTraffic=$true;ParallelContinuationForecast=$true;RearPressureFilter=$true;LightDrafting=$true;CoastWithinEnvelope=$true;DriverRisk=5;NoGapCentres=$true;TrafficPathCheck=$true;AnticipatePassingSpace=$true;TrafficCoastContinuation=$true;ParallelForecastWorkers=8;DynamicRejoin=$true;DynamicTraffic=$true;LocalOriginCurvature=$true;DynamicFieldStudy=$true;DynamicBlockingTarget=$true;DynamicTrafficFallback=$true;ParallelDynamicForecast=$true;OpenSpacePriority=$true;PassProcess=$true;DriverRoster='mixed';DriverForm=$true;RaceHud=$true;Width=1920;Height=1080}
$names=if($Stage -eq 'review'){@('bayview-hud')}elseif($Stage -eq 'performance'){@('view-off','view-on')}else{@('cedar-hud')}
foreach($name in $names){
 $opts=$common.Clone()
 if($name.StartsWith('bayview')){$opts.Track='bayview';$opts.BayviewTransferStudy=$true;$opts.RacingLine='bayview-smooth-v1';$opts.RaceHudReview=$true}
 if($Stage -eq 'performance'){$opts.Track='bayview';$opts.BayviewTransferStudy=$true;$opts.RacingLine='bayview-smooth-v1';$opts.RaceHud=($name -eq 'view-on');$opts.CleanView=$true;$opts.Visible=$true;$opts.Windowed=$true;$opts.PlaybackRate=1}
 $folder=Join-Path $root "review/unity-35/race-hud-$Version/$name";$opts.OutputDirectory=$folder
 $run=& "$PSScriptRoot/run-technical-trial.ps1" @opts
 $run.metadata|ConvertTo-Json -Depth 6|Set-Content (Join-Path $folder 'launch.json')
 Write-Output "Started $name pid=$($run.process.Id)"
 $run.process.WaitForExit()
 @{exitCode=$run.process.ExitCode;name=$name}|ConvertTo-Json|Set-Content (Join-Path $folder 'exit.json')
 Write-Output "Finished $name exit=$($run.process.ExitCode)"
 if($run.process.ExitCode -ne 0){throw 'HUD trial failed'}
}
