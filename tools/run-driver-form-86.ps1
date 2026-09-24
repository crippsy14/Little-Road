$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$common=@{Executable=(Join-Path $root '../little-road-context-86-review/Little Road Unity.exe');Track='cedar';Loadout='club';Cars=24;Laps=1;Duration=180;Seed=1709;Policy='context';RacingLine='cedar-smooth-v2';TrafficCase='field';PhysicalRecovery=$true;TrafficDiagnostics=$true;RoadFeasibility=$true;PreparedPath=$true;GuidePathFallback=$true;CompareContinuation=$true;TrafficAwareProgress=$true;FollowingFeasibilityFallback=$true;SharedObservedTraffic=$true;ParallelContinuationForecast=$true;RearPressureFilter=$true;LightDrafting=$true;CoastWithinEnvelope=$true;DriverRisk=5;NoGapCentres=$true;TrafficPathCheck=$true;AnticipatePassingSpace=$true;TrafficCoastContinuation=$true;ParallelForecastWorkers=8;DynamicRejoin=$true;DynamicTraffic=$true;LocalOriginCurvature=$true;DynamicFieldStudy=$true;DynamicBlockingTarget=$true;DynamicTrafficFallback=$true;ParallelDynamicForecast=$true;OpenSpacePriority=$true;PassProcess=$true}
foreach($seed in 2000..2019){foreach($profile in @('steady','variable')){
 $opts=$common.Clone();$opts.Track='bayview';$opts.BayviewTransferStudy=$true;$opts.RacingLine='bayview-smooth-v1';$opts.Cars=1;$opts.TrafficCase='standing-solo';$opts.DynamicFieldStudy=$false;$opts.DriverRoster=$profile;$opts.DriverForm=$true;$opts.Seed=$seed
 $folder=Join-Path $root "review/unity-35/driver-form-86/$seed-$profile";$opts.OutputDirectory=$folder
 $run=& "$PSScriptRoot/run-technical-trial.ps1" @opts
 $run.metadata|ConvertTo-Json -Depth 6|Set-Content (Join-Path $folder 'launch.json')
 Write-Output "Started $seed-$profile pid=$($run.process.Id)"
 $run.process.WaitForExit()
 @{exitCode=$run.process.ExitCode}|ConvertTo-Json|Set-Content (Join-Path $folder 'exit.json')
 if($run.process.ExitCode -ne 0){throw 'Form trial failed'}
 Write-Output "Finished $seed-$profile"
}}
