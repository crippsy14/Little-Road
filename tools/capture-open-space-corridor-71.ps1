$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$folder=Join-Path $root 'review/unity-35/open-space-71/no-room-audit'
$options=@{Executable=(Join-Path $root '../little-road-context-71-review/Little Road Unity.exe');OutputDirectory=$folder;Cars=2;Loadout='club';Opponents='stock';Policy='context';RacingLine='cedar-smooth-v2';TrafficCase='no-room';TrafficGap=24;TrafficSpeed=18;TrafficRearSpeed=26;Laps=1;Duration=180;PhysicalRecovery=$true;TrafficDiagnostics=$true;RoadFeasibility=$true;PreparedPath=$true;ParallelForecastWorkers=8;GuidePathFallback=$true;CoastWithinEnvelope=$true;PassProcess=$true;DriverRisk=5;NoGapCentres=$true;TrafficPathCheck=$true;AnticipatePassingSpace=$true;TrafficCoastContinuation=$true;CompareContinuation=$true;TrafficAwareProgress=$true;FollowingFeasibilityFallback=$true;RearPressureFilter=$true;SharedObservedTraffic=$true;ParallelContinuationForecast=$true;LightDrafting=$true;OpenSpacePriority=$true;ContextPlanAudit=$true}
$run=& "$PSScriptRoot/run-technical-trial.ps1" @options
$run.metadata|ConvertTo-Json -Depth 6|Set-Content (Join-Path $folder 'launch.json')
Copy-Item -LiteralPath (Join-Path $root 'review/unity-35/regression-38/no-room/expectations.json') -Destination (Join-Path $folder 'expectations.json')
Write-Output "Started no-room decision audit pid=$($run.process.Id)"
$run.process.WaitForExit()
@{exitCode=$run.process.ExitCode}|ConvertTo-Json|Set-Content (Join-Path $folder 'exit.json')
Write-Output "Finished audit exit=$($run.process.ExitCode)"
if($run.process.ExitCode -ne 0){throw 'Diagnostic process failed'}
