param([switch]$FieldOnly,[switch]$FixturesOnly)
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$exe=Join-Path $root '../little-road-context-70-review/Little Road Unity.exe'
$common=@{Executable=$exe;Track='cedar';Loadout='club';Laps=1;Duration=180;Seed=1709;Policy='context';RacingLine='cedar-smooth-v2';PhysicalRecovery=$true;TrafficDiagnostics=$true;RoadFeasibility=$true;PreparedPath=$true;GuidePathFallback=$true;CompareContinuation=$true;TrafficAwareProgress=$true;FollowingFeasibilityFallback=$true;SharedObservedTraffic=$true;ParallelContinuationForecast=$true;RearPressureFilter=$true;CoastWithinEnvelope=$true;DriverRisk=5;NoGapCentres=$true;TrafficPathCheck=$true;AnticipatePassingSpace=$true;TrafficCoastContinuation=$true;ParallelForecastWorkers=8;PassProcess=$true}
$jobs=@()
if(!$FixturesOnly){foreach($mode in @('off','on')){$jobs+=@{name=$mode;scenario='field';cars=24;draft=($mode -eq 'on');duration=180}}}
if(!$FieldOnly){foreach($scenario in @('draft-straight','draft-pullout')){foreach($mode in @('off','on')){$jobs+=@{name="$scenario-$mode";scenario=$scenario;cars=2;draft=($mode -eq 'on');duration=5}}}}
foreach($job in $jobs){
 $opts=$common.Clone();$folder=Join-Path $root "review/unity-35/drafting-70/$($job.name)"
 $opts.OutputDirectory=$folder;$opts.TrafficCase=$job.scenario;$opts.Cars=$job.cars;$opts.LightDrafting=$job.draft;$opts.Duration=$job.duration
 if($job.cars -eq 2){$opts.Opponents='club';$opts.TrafficGap=12;$opts.TrafficSpeed=26}
 $run=& "$PSScriptRoot/run-technical-trial.ps1" @opts
 $run.metadata|ConvertTo-Json -Depth 6|Set-Content (Join-Path $folder 'launch.json')
 Write-Output "Started $($job.name) pid=$($run.process.Id)"
 $run.process.WaitForExit()
 @{exitCode=$run.process.ExitCode;name=$job.name}|ConvertTo-Json|Set-Content (Join-Path $folder 'exit.json')
 Write-Output "Finished $($job.name) exit=$($run.process.ExitCode)"
 if($run.process.ExitCode -ne 0){throw "Drafting test process failed: $($job.name)"}
}
