param([ValidateSet('parity','preview')][string]$Stage='parity')
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$common=@{Executable=(Join-Path $root '../little-road-context-87-review/Little Road Unity.exe');Track='cedar';Loadout='club';Cars=24;Laps=1;Duration=180;Seed=1709;Policy='context';RacingLine='cedar-smooth-v2';TrafficCase='field';PhysicalRecovery=$true;TrafficDiagnostics=$true;RoadFeasibility=$true;PreparedPath=$true;GuidePathFallback=$true;CompareContinuation=$true;TrafficAwareProgress=$true;FollowingFeasibilityFallback=$true;SharedObservedTraffic=$true;ParallelContinuationForecast=$true;RearPressureFilter=$true;LightDrafting=$true;CoastWithinEnvelope=$true;DriverRisk=5;NoGapCentres=$true;TrafficPathCheck=$true;AnticipatePassingSpace=$true;TrafficCoastContinuation=$true;ParallelForecastWorkers=8;DynamicRejoin=$true;DynamicTraffic=$true;LocalOriginCurvature=$true;DynamicFieldStudy=$true;DynamicBlockingTarget=$true;DynamicTrafficFallback=$true;ParallelDynamicForecast=$true;OpenSpacePriority=$true;PassProcess=$true}
$jobs=if($Stage -eq 'preview'){@('bayview-preview')}else{@('bayview-mixed','cedar-mixed')}
foreach($name in $jobs){
 $opts=$common.Clone();$opts.DriverRoster='mixed';$opts.DriverForm=$true
 if($name.StartsWith('bayview')){$opts.Track='bayview';$opts.BayviewTransferStudy=$true;$opts.RacingLine='bayview-smooth-v1'}
 if($Stage -eq 'preview'){$opts.RacePreviewReview=$true}
 $folder=Join-Path $root "review/unity-35/race-preview-87/$name";$opts.OutputDirectory=$folder
 $run=& "$PSScriptRoot/run-technical-trial.ps1" @opts
 $run.metadata|ConvertTo-Json -Depth 6|Set-Content (Join-Path $folder 'launch.json')
 Write-Output "Started $name pid=$($run.process.Id)"
 $run.process.WaitForExit()
 @{exitCode=$run.process.ExitCode;name=$name}|ConvertTo-Json|Set-Content (Join-Path $folder 'exit.json')
 Write-Output "Finished $name exit=$($run.process.ExitCode)"
 if($run.process.ExitCode -ne 0){throw 'Roster trial failed'}
}
