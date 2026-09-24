param([Parameter(Mandatory=$true)][string]$Executable,[Parameter(Mandatory=$true)][string]$OutputDirectory)
$ErrorActionPreference='Stop'
$exe=(Resolve-Path -LiteralPath $Executable).Path
$root=[IO.Path]::GetFullPath($OutputDirectory)
if(Test-Path -LiteralPath $root){throw 'Choose a new evidence folder.'}
New-Item -ItemType Directory -Path $root|Out-Null
$jobs=@(@{name='field-control';coast=$false;anticipate=$false},@{name='field-coast';coast=$true;anticipate=$false},@{name='field-combined';coast=$true;anticipate=$true})
$results=[Collections.Generic.List[object]]::new()
foreach($job in $jobs){
 $folder=Join-Path $root $job.name
 $run=& "$PSScriptRoot/run-technical-trial.ps1" -Executable $exe -OutputDirectory $folder -Loadout club -Cars 24 -Laps 1 -Duration 180 -Policy context -RacingLine cedar-smooth-v2 -TrafficCase field -PhysicalRecovery -TrafficDiagnostics -RoadFeasibility -PreparedPath -ParallelForecastWorkers 8 -GuidePathFallback -CoastWithinEnvelope -DriverRisk 5 -NoGapCentres -TrafficPathCheck -TrafficCoastContinuation:$job.coast -AnticipatePassingSpace:$job.anticipate -PedalStageAudit -PassProcess
 $run.metadata|ConvertTo-Json -Depth 6|Set-Content (Join-Path $folder 'launch.json')
 Write-Output "Started $($job.name) pid=$($run.process.Id)"
 $run.process.WaitForExit()
 $result=@{name=$job.name;coast=$job.coast;anticipate=$job.anticipate;folder=$folder;exitCode=$run.process.ExitCode}
 $result|ConvertTo-Json|Set-Content (Join-Path $folder 'exit.json')
 $results.Add($result)
 @{fieldsComplete=($results.Count -eq 3);complete=$false;runs=$results.ToArray()}|ConvertTo-Json -Depth 6|Set-Content (Join-Path $root 'manifest.json')
 Write-Output "Finished $($job.name) exit=$($run.process.ExitCode)"
 if($run.process.ExitCode -ne 0){throw 'Traffic coast comparison failed'}
}
& "$PSScriptRoot/run-coasting-regressions.ps1" -Executable $exe -OutputDirectory (Join-Path $root 'regressions-combined') -DriverRisk 5 -NoGapCentres -TrafficPathCheck -AnticipatePassingSpace -TrafficCoastContinuation
$reg=Get-Content -LiteralPath (Join-Path $root 'regressions-combined/manifest.json') -Raw|ConvertFrom-Json
@{fieldsComplete=$true;complete=$reg.complete;runs=$results.ToArray()}|ConvertTo-Json -Depth 6|Set-Content (Join-Path $root 'manifest.json')
