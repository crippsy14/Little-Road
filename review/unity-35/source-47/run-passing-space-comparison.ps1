param([Parameter(Mandatory=$true)][string]$Executable,[Parameter(Mandatory=$true)][string]$OutputDirectory)
$ErrorActionPreference='Stop'
$exe=(Resolve-Path -LiteralPath $Executable).Path
$root=[IO.Path]::GetFullPath($OutputDirectory)
if(Test-Path -LiteralPath $root){throw 'Choose a new evidence folder.'}
New-Item -ItemType Directory -Path $root|Out-Null
$results=[Collections.Generic.List[object]]::new()
foreach($anticipate in @($false,$true)){
 $name=if($anticipate){'field-anticipation'}else{'field-control'}
 $folder=Join-Path $root $name
 $run=& "$PSScriptRoot/run-technical-trial.ps1" -Executable $exe -OutputDirectory $folder -Loadout club -Cars 24 -Laps 1 -Duration 180 -Policy context -RacingLine cedar-smooth-v2 -TrafficCase field -PhysicalRecovery -TrafficDiagnostics -RoadFeasibility -PreparedPath -ParallelForecastWorkers 8 -GuidePathFallback -CoastWithinEnvelope -DriverRisk 5 -NoGapCentres -TrafficPathCheck -AnticipatePassingSpace:$anticipate -PassProcess
 $run.metadata|ConvertTo-Json -Depth 6|Set-Content (Join-Path $folder 'launch.json')
 Write-Output "Started $name pid=$($run.process.Id)"
 $run.process.WaitForExit()
 $result=@{name=$name;anticipate=$anticipate;folder=$folder;exitCode=$run.process.ExitCode}
 $result|ConvertTo-Json|Set-Content (Join-Path $folder 'exit.json')
 $results.Add($result)
 @{fieldsComplete=($results.Count -eq 2);complete=$false;runs=$results.ToArray()}|ConvertTo-Json -Depth 6|Set-Content (Join-Path $root 'manifest.json')
 Write-Output "Finished $name exit=$($run.process.ExitCode)"
 if($run.process.ExitCode -ne 0){throw 'Passing-space comparison process failed'}
}
& "$PSScriptRoot/run-coasting-regressions.ps1" -Executable $exe -OutputDirectory (Join-Path $root 'regressions') -DriverRisk 5 -NoGapCentres -TrafficPathCheck -AnticipatePassingSpace
$regressions=Get-Content -LiteralPath (Join-Path $root 'regressions/manifest.json') -Raw | ConvertFrom-Json
@{fieldsComplete=$true;complete=$regressions.complete;runs=$results.ToArray()}|ConvertTo-Json -Depth 6|Set-Content (Join-Path $root 'manifest.json')
