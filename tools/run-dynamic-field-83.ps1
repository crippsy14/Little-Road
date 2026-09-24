param([switch]$Visible,[ValidateSet('view','replay')][string]$Label='view')
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$common=@{Executable=(Join-Path $root '../little-road-context-83-review/Little Road Unity.exe');Track='cedar';Loadout='club';Cars=24;Laps=1;Duration=180;Seed=1709;Policy='context';RacingLine='cedar-smooth-v2';TrafficCase='field';PhysicalRecovery=$true;TrafficDiagnostics=$true;RoadFeasibility=$true;PreparedPath=$true;GuidePathFallback=$true;CompareContinuation=$true;TrafficAwareProgress=$true;FollowingFeasibilityFallback=$true;SharedObservedTraffic=$true;ParallelContinuationForecast=$true;RearPressureFilter=$true;LightDrafting=$true;CoastWithinEnvelope=$true;DriverRisk=5;NoGapCentres=$true;TrafficPathCheck=$true;AnticipatePassingSpace=$true;TrafficCoastContinuation=$true;ParallelForecastWorkers=8;DynamicRejoin=$true;DynamicTraffic=$true;LocalOriginCurvature=$true;DynamicFieldStudy=$true;DynamicBlockingTarget=$true;OpenSpacePriority=$true;PassProcess=$true}
$prefix='';$modes=@('off','on')
if($Visible){$prefix="$Label-";$common.Visible=$true;$common.Windowed=$true;$common.Width=1920;$common.Height=1080;$modes=@('on')}
foreach($mode in $modes){
 $opts=$common.Clone();$folder=Join-Path $root "review/unity-35/dynamic-field-83/$prefix$mode"
 $opts.OutputDirectory=$folder
 $opts.DynamicTrafficFallback=$mode -eq 'on'
 $run=& "$PSScriptRoot/run-technical-trial.ps1" @opts
 $run.metadata|ConvertTo-Json -Depth 6|Set-Content (Join-Path $folder 'launch.json')
 Write-Output "Started $prefix$mode pid=$($run.process.Id)"
 $run.process.WaitForExit()
 @{exitCode=$run.process.ExitCode;name="$prefix$mode"}|ConvertTo-Json|Set-Content (Join-Path $folder 'exit.json')
 Write-Output "Finished $prefix$mode exit=$($run.process.ExitCode)"
 if($run.process.ExitCode -ne 0){throw 'Experiment process failed'}
}

