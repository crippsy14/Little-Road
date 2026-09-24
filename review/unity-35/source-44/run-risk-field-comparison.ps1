param([Parameter(Mandatory=$true)][string]$Executable,[Parameter(Mandatory=$true)][string]$OutputDirectory)
$ErrorActionPreference='Stop'
$exe=(Resolve-Path -LiteralPath $Executable).Path
$root=[IO.Path]::GetFullPath($OutputDirectory)
if(Test-Path -LiteralPath $root){throw 'Choose a new evidence folder.'}
New-Item -ItemType Directory -Path $root|Out-Null
$results=[Collections.Generic.List[object]]::new()
foreach($risk in @(0,5)){
 $folder=Join-Path $root "field-risk$risk"
 $run=& "$PSScriptRoot/run-technical-trial.ps1" -Executable $exe -OutputDirectory $folder -Loadout club -Cars 24 -Laps 1 -Duration 180 -Policy context -RacingLine cedar-smooth-v2 -TrafficCase field -PhysicalRecovery -TrafficDiagnostics -RoadFeasibility -PreparedPath -ParallelForecastWorkers 8 -GuidePathFallback -CoastWithinEnvelope -DriverRisk $risk -PassProcess
 $run.metadata|ConvertTo-Json -Depth 6|Set-Content (Join-Path $folder 'launch.json')
 Write-Output "Started risk$risk pid=$($run.process.Id)"
 $run.process.WaitForExit()
 $result=@{risk=$risk;folder=$folder;exitCode=$run.process.ExitCode}
 $result|ConvertTo-Json|Set-Content (Join-Path $folder 'exit.json')
 $results.Add($result)
 @{complete=($results.Count -eq 2);runs=$results.ToArray()}|ConvertTo-Json -Depth 6|Set-Content (Join-Path $root 'manifest.json')
 Write-Output "Finished risk$risk exit=$($run.process.ExitCode)"
 if($run.process.ExitCode -ne 0){throw 'Risk comparison process failed'}
}
