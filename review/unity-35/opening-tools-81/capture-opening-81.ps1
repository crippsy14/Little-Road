$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$folder=Join-Path $root 'review/unity-35/dynamic-field-81/opening-audit'
$options=@{Executable=(Join-Path $root '../little-road-context-81-review/Little Road Unity.exe');OutputDirectory=$folder;Track='cedar';Loadout='club';Cars=24;Laps=1;Duration=180;Seed=1709;Policy='context';RacingLine='cedar-smooth-v2';TrafficCase='field';PhysicalRecovery=$true;TrafficDiagnostics=$true;RoadFeasibility=$true;PreparedPath=$true;GuidePathFallback=$true;CompareContinuation=$true;TrafficAwareProgress=$true;FollowingFeasibilityFallback=$true;SharedObservedTraffic=$true;ParallelContinuationForecast=$true;RearPressureFilter=$true;LightDrafting=$true;CoastWithinEnvelope=$true;DriverRisk=5;NoGapCentres=$true;TrafficPathCheck=$true;AnticipatePassingSpace=$true;TrafficCoastContinuation=$true;ParallelForecastWorkers=8;PassProcess=$true;OpenSpacePriority=$true;PedalStageAudit=$true;OpeningDecisionAudit=$true;DynamicRejoin=$true;DynamicTraffic=$true;DynamicFieldStudy=$true;LocalOriginCurvature=$true}
$run=& "$PSScriptRoot/run-technical-trial.ps1" @options
$run.metadata|ConvertTo-Json -Depth 6|Set-Content (Join-Path $folder 'launch.json')
Write-Output "Started unchanged81 pedal audit pid=$($run.process.Id)"
$run.process.WaitForExit()
@{exitCode=$run.process.ExitCode}|ConvertTo-Json|Set-Content (Join-Path $folder 'exit.json')
Write-Output "Finished audit exit=$($run.process.ExitCode)"
if($run.process.ExitCode -ne 0){throw 'Diagnostic process failed'}

