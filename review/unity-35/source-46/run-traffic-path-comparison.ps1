param([Parameter(Mandatory=$true)][string]$Executable,[Parameter(Mandatory=$true)][string]$OutputDirectory)
$ErrorActionPreference='Stop'
$exe=(Resolve-Path -LiteralPath $Executable).Path
$root=[IO.Path]::GetFullPath($OutputDirectory)
if(Test-Path -LiteralPath $root){throw 'Choose a new evidence folder.'}
New-Item -ItemType Directory -Path $root|Out-Null
$jobs=@(
 @{name='field-risk0';risk=0;cars=24;scenario='field';check=$false;opponents=''},
 @{name='field-risk5-control';risk=5;cars=24;scenario='field';check=$false;opponents=''},
 @{name='crossing-control';risk=5;cars=2;scenario='crossing-pass';check=$false;opponents='street'},
 @{name='crossing-checked';risk=5;cars=2;scenario='crossing-pass';check=$true;opponents='street'},
 @{name='field-risk5';risk=5;cars=24;scenario='field';check=$true;opponents=''}
)
$results=[Collections.Generic.List[object]]::new()
foreach($job in $jobs){
 $folder=Join-Path $root $job.name
 $run=& "$PSScriptRoot/run-technical-trial.ps1" -Executable $exe -OutputDirectory $folder -Loadout club -Cars $job.cars -Laps 1 -Duration 180 -Policy context -RacingLine cedar-smooth-v2 -TrafficCase $job.scenario -PhysicalRecovery -TrafficDiagnostics -RoadFeasibility -PreparedPath -ParallelForecastWorkers 8 -GuidePathFallback -CoastWithinEnvelope -DriverRisk $job.risk -NoGapCentres -TrafficPathCheck:$job.check -Opponents $job.opponents -PassProcess
 $run.metadata|ConvertTo-Json -Depth 6|Set-Content (Join-Path $folder 'launch.json')
 Write-Output "Started $($job.name) pid=$($run.process.Id)"
 $run.process.WaitForExit()
 $result=@{name=$job.name;risk=$job.risk;check=$job.check;folder=$folder;exitCode=$run.process.ExitCode}
 $result|ConvertTo-Json|Set-Content (Join-Path $folder 'exit.json')
 $results.Add($result)
 @{complete=($results.Count -eq $jobs.Count);runs=$results.ToArray()}|ConvertTo-Json -Depth 6|Set-Content (Join-Path $root 'manifest.json')
 Write-Output "Finished $($job.name) exit=$($run.process.ExitCode)"
 if($run.process.ExitCode -ne 0){throw 'Traffic-path comparison process failed'}
}
