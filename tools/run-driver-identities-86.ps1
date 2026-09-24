param([ValidateSet('neutral','traits','mixed')][string]$Stage='neutral')
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$common=@{Executable=(Join-Path $root '../little-road-context-86-review/Little Road Unity.exe');Track='cedar';Loadout='club';Cars=24;Laps=1;Duration=180;Seed=1709;Policy='context';RacingLine='cedar-smooth-v2';TrafficCase='field';PhysicalRecovery=$true;TrafficDiagnostics=$true;RoadFeasibility=$true;PreparedPath=$true;GuidePathFallback=$true;CompareContinuation=$true;TrafficAwareProgress=$true;FollowingFeasibilityFallback=$true;SharedObservedTraffic=$true;ParallelContinuationForecast=$true;RearPressureFilter=$true;LightDrafting=$true;CoastWithinEnvelope=$true;DriverRisk=5;NoGapCentres=$true;TrafficPathCheck=$true;AnticipatePassingSpace=$true;TrafficCoastContinuation=$true;ParallelForecastWorkers=8;DynamicRejoin=$true;DynamicTraffic=$true;LocalOriginCurvature=$true;DynamicFieldStudy=$true;DynamicBlockingTarget=$true;DynamicTrafficFallback=$true;ParallelDynamicForecast=$true;OpenSpacePriority=$true;PassProcess=$true}
$jobs=if($Stage -eq 'traits'){
 @(
  @{name='pass-cautious';profile='cautious';scenario='pass';cars=2},
  @{name='pass-assertive';profile='assertive';scenario='pass';cars=2},
  @{name='pass-daring';profile='daring';scenario='pass';cars=2},
  @{name='solo-neutral';profile='neutral';scenario='standing-solo';cars=1},
  @{name='solo-early';profile='early';scenario='standing-solo';cars=1}
 )
}else{@(@{name="bayview-$Stage";profile=$Stage;scenario='field';cars=24;bayview=$true},@{name="cedar-$Stage";profile=$Stage;scenario='field';cars=24})}
foreach($job in $jobs){
 $opts=$common.Clone();$opts.DriverRoster=$job.profile;$opts.DriverForm=$Stage -eq 'mixed';$opts.Cars=$job.cars;$opts.TrafficCase=$job.scenario;$opts.DynamicFieldStudy=$job.cars -eq 24
 if($job.scenario -eq 'pass'){$opts.Opponents='street';$opts.TrafficSpeed=20}
 if($job.bayview){$opts.Track='bayview';$opts.BayviewTransferStudy=$true;$opts.RacingLine='bayview-smooth-v1'}
 $folder=Join-Path $root "review/unity-35/driver-identities-86/$($job.name)";$opts.OutputDirectory=$folder
 $run=& "$PSScriptRoot/run-technical-trial.ps1" @opts
 $run.metadata|ConvertTo-Json -Depth 6|Set-Content (Join-Path $folder 'launch.json')
 Write-Output "Started $($job.name) pid=$($run.process.Id)"
 $run.process.WaitForExit()
 @{exitCode=$run.process.ExitCode;name=$job.name}|ConvertTo-Json|Set-Content (Join-Path $folder 'exit.json')
 Write-Output "Finished $($job.name) exit=$($run.process.ExitCode)"
 if($run.process.ExitCode -ne 0){throw 'Identity trial failed'}
}
