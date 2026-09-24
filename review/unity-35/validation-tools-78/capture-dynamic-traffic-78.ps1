$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$common=@{Executable=(Join-Path $root '../little-road-context-78-review/Little Road Unity.exe');Track='cedar';Loadout='club';Cars=1;Laps=1;Duration=180;Seed=1709;Policy='context';RacingLine='cedar-smooth-v2';PhysicalRecovery=$true;TrafficDiagnostics=$true;RoadFeasibility=$true;PreparedPath=$true;GuidePathFallback=$true;CompareContinuation=$true;TrafficAwareProgress=$true;FollowingFeasibilityFallback=$true;SharedObservedTraffic=$true;ParallelContinuationForecast=$true;RearPressureFilter=$true;LightDrafting=$true;CoastWithinEnvelope=$true;DriverRisk=5;NoGapCentres=$true;TrafficPathCheck=$true;AnticipatePassingSpace=$true;TrafficCoastContinuation=$true;ParallelForecastWorkers=8;PassProcess=$true;OpenSpacePriority=$true;TrafficSpeed=20}

$jobs=@(@{name='corner-pair-left-audit';scenario='corner-pair-left';cars=2;enabled=$true;solo=$false})
foreach($job in $jobs){
 $opts=$common.Clone();$folder=Join-Path $root "review/unity-35/dynamic-traffic-78/$($job.name)"
 $opts.OutputDirectory=$folder;$opts.TrafficCase=$job.scenario;$opts.Cars=$job.cars
 $opts.DynamicRejoin=$job.enabled;$opts.LocalOriginCurvature=$job.enabled;$opts.DynamicTraffic=($job.enabled -and -not $job.solo)
 $opts.Opponents='street,street';$opts.ContextPlanAudit=$true;$opts.OpeningDecisionAudit=$true
 if($job.solo){$opts.Loadout='street'}
 $run=& "$PSScriptRoot/run-technical-trial.ps1" @opts
 $run.metadata|ConvertTo-Json -Depth 6|Set-Content (Join-Path $folder 'launch.json')
 Write-Output "Started $($job.name) pid=$($run.process.Id)"
 $run.process.WaitForExit()
 @{exitCode=$run.process.ExitCode;name=$job.name}|ConvertTo-Json|Set-Content (Join-Path $folder 'exit.json')
 Write-Output "Finished $($job.name) exit=$($run.process.ExitCode)"
 if($run.process.ExitCode -ne 0){throw 'Dynamic traffic study failed to run'}
}
