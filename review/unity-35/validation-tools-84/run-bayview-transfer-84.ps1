param([ValidateSet('all','solo','field','cedar')][string]$Stage='all')
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$exe=Join-Path $root '../little-road-context-84-review/Little Road Unity.exe'
$latest=@{Policy='context';PhysicalRecovery=$true;TrafficDiagnostics=$true;RoadFeasibility=$true;PreparedPath=$true;GuidePathFallback=$true;CompareContinuation=$true;TrafficAwareProgress=$true;FollowingFeasibilityFallback=$true;SharedObservedTraffic=$true;ParallelContinuationForecast=$true;RearPressureFilter=$true;LightDrafting=$true;CoastWithinEnvelope=$true;DriverRisk=5;NoGapCentres=$true;TrafficPathCheck=$true;AnticipatePassingSpace=$true;TrafficCoastContinuation=$true;ParallelForecastWorkers=8;DynamicRejoin=$true;DynamicTraffic=$true;LocalOriginCurvature=$true;DynamicBlockingTarget=$true;DynamicTrafficFallback=$true;OpenSpacePriority=$true}
$jobs=if($Stage -eq 'all'){@('solo','field')}else{@($Stage)}
foreach($job in $jobs){
 $modes=if($job -eq 'cedar'){@('latest')}else{@('legacy','latest')}
 foreach($mode in $modes){
  $folder=Join-Path $root "review/unity-35/bayview-transfer-84/$job-$mode"
  $opts=@{Executable=$exe;OutputDirectory=$folder;Track='bayview';Loadout='club';Cars=1;Laps=1;Duration=180;Seed=1709;TrafficCase='standing-solo';Policy='legacy';BayviewTransferStudy=$true;RacingLine='bayview-smooth-v1';PassProcess=$true}
  if($mode -eq 'latest'){foreach($key in $latest.Keys){$opts[$key]=$latest[$key]}}
  if($job -ne 'solo'){$opts.Cars=24;$opts.TrafficCase='field';if($mode -eq 'latest'){$opts.DynamicFieldStudy=$true}}
  if($job -eq 'cedar'){$opts.Track='cedar';$opts.BayviewTransferStudy=$false;$opts.RacingLine='cedar-smooth-v2'}
  $run=& "$PSScriptRoot/run-technical-trial.ps1" @opts
  $run.metadata|ConvertTo-Json -Depth 6|Set-Content (Join-Path $folder 'launch.json')
  Write-Output "Started $job-$mode pid=$($run.process.Id)"
  $run.process.WaitForExit()
  @{exitCode=$run.process.ExitCode;name="$job-$mode"}|ConvertTo-Json|Set-Content (Join-Path $folder 'exit.json')
  Write-Output "Finished $job-$mode exit=$($run.process.ExitCode)"
  if($run.process.ExitCode -ne 0){throw 'Comparison process failed'}
 }
}
