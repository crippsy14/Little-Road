param([Parameter(Mandatory=$true)][string]$Executable,[Parameter(Mandatory=$true)][string]$OutputDirectory)
$ErrorActionPreference='Stop'
$exe=(Resolve-Path -LiteralPath $Executable).Path
$root=[IO.Path]::GetFullPath($OutputDirectory)
if(Test-Path -LiteralPath $root){throw 'Choose a new evidence folder.'}
New-Item -ItemType Directory -Path $root|Out-Null
$jobs=@(@{name='field-neutral';setup='club';preset='neutral';cars=24;scenario='field';laps=1})
foreach($setup in @('street','club')){foreach($preset in @('neutral','earlier','early')){$jobs+=@{name="$setup-$preset";setup=$setup;preset=$preset;cars=1;scenario='solo';laps=3}}}
@{scope='Deterministic judgement only, no form/mistakes. Same car/seed/start; three-lap solo contains a partial first lap plus two complete flying laps. Neutral field must match42coast. No personalities or adventure acceptance.';presets=@{neutral=.75;earlier=.65;early=.55};setups=@('street','club');laps=3;flyingLapOrdering='early >= earlier >= neutral, within 0.1s measurement tolerance';clearStraightSpeedTolerance=.15;clearStraightMinimumPedal=.95;zeroSoloContacts=$true;zeroSoloDepartures=$true;zeroSoloRecovery=$true;allOrderedGates=$true}|ConvertTo-Json -Depth 6|Set-Content (Join-Path $root 'expectations.json')
$results=[Collections.Generic.List[object]]::new()
foreach($job in $jobs){
 $folder=Join-Path $root $job.name
 $run=& "$PSScriptRoot/run-technical-trial.ps1" -Executable $exe -OutputDirectory $folder -Loadout $job.setup -Cars $job.cars -Laps $job.laps -Duration 300 -Policy context -RacingLine cedar-smooth-v2 -TrafficCase $job.scenario -PhysicalRecovery -TrafficDiagnostics -RoadFeasibility -PreparedPath -ParallelForecastWorkers 8 -GuidePathFallback -CoastWithinEnvelope -DriverBraking $job.preset -PassProcess
 $run.metadata|ConvertTo-Json -Depth 6|Set-Content (Join-Path $folder 'launch.json')
 Write-Output "Started $($job.name) pid=$($run.process.Id)"
 $run.process.WaitForExit()
 $result=@{name=$job.name;setup=$job.setup;preset=$job.preset;cars=$job.cars;scenario=$job.scenario;laps=$job.laps;folder=$folder;exitCode=$run.process.ExitCode}
 $result|ConvertTo-Json|Set-Content (Join-Path $folder 'exit.json')
 $results.Add($result)
 @{complete=($results.Count -eq $jobs.Count);runs=$results.ToArray()}|ConvertTo-Json -Depth 6|Set-Content (Join-Path $root 'manifest.json')
 Write-Output "Finished $($job.name) exit=$($run.process.ExitCode)"
 if($run.process.ExitCode -ne 0){throw 'Matrix process failed'}
}
