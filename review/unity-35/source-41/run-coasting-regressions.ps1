param([Parameter(Mandatory=$true)][string]$Executable,[Parameter(Mandatory=$true)][string]$OutputDirectory)
$ErrorActionPreference='Stop'
$exe=(Resolve-Path -LiteralPath $Executable).Path
$root=[IO.Path]::GetFullPath($OutputDirectory)
if(Test-Path -LiteralPath $root){throw 'Choose a new evidence folder.'}
New-Item -ItemType Directory -Path $root | Out-Null
$cases=@(
 @{name='queue';cars=3;loadout='club';opponents='street,stock';gap=24},
 @{name='no-room';cars=2;loadout='club';opponents='stock';gap=24},
 @{name='merge-left';cars=2;loadout='street';opponents='club';gap=12},
 @{name='merge-right';cars=2;loadout='street';opponents='club';gap=12},
 @{name='brake-leader';cars=2;loadout='club';opponents='stock';gap=24},
 @{name='corner-three';cars=3;loadout='club';opponents='street,stock';gap=24}
)
$scope='Coasting candidate controlled clean-opportunity regressions versus frozen38; same-build paired solo street/club. Existing corridor contract and rear-merge witness deadlines retained. No rendered or broad personality acceptance.'
@{scope=$scope;cases=$cases;referenceRoot='review/unity-35/regression-38';maximumCompletionRatio=1.05;completionAllowance=.5;zeroContacts=$true;zeroDepartures=$true;zeroRecovery=$true}|ConvertTo-Json -Depth 6|Set-Content (Join-Path $root 'expectations.json')
$jobs=@()
foreach($case in $cases){$jobs+=@{name=$case.name;scenario=$case.name;cars=$case.cars;loadout=$case.loadout;opponents=$case.opponents;gap=$case.gap;coast=$true}}
foreach($loadout in @('street','club')){foreach($mode in @('control','coast')){$jobs+=@{name="solo-$loadout-$mode";scenario='solo';cars=1;loadout=$loadout;opponents='';gap=24;coast=($mode -eq 'coast')}}}
$results=[Collections.Generic.List[object]]::new()
foreach($job in $jobs){
 $folder=Join-Path $root $job.name
 $options=@{Executable=$exe;OutputDirectory=$folder;Cars=$job.cars;Loadout=$job.loadout;Opponents=$job.opponents;Policy='context';RacingLine='cedar-smooth-v2';TrafficCase=$job.scenario;TrafficGap=$job.gap;TrafficSpeed=18;TrafficRearSpeed=26;Laps=1;Duration=180;PhysicalRecovery=$true;TrafficDiagnostics=$true;RoadFeasibility=$true;PreparedPath=$true;ParallelForecastWorkers=8;GuidePathFallback=$true;CoastWithinEnvelope=$job.coast;PassProcess=$true}
 $run=& "$PSScriptRoot/run-technical-trial.ps1" @options
 $run.metadata|ConvertTo-Json -Depth 6|Set-Content (Join-Path $folder 'launch.json')
 Write-Output "Started $($job.name) pid=$($run.process.Id)"
 if($job.name -eq 'no-room'){Copy-Item -LiteralPath (Join-Path $PSScriptRoot '../review/unity-35/regression-38/no-room/expectations.json') -Destination (Join-Path $folder 'expectations.json')}
 $run.process.WaitForExit()
 $status=@{name=$job.name;scenario=$job.scenario;cars=$job.cars;coast=$job.coast;folder=$folder;exitCode=$run.process.ExitCode}
 $status|ConvertTo-Json|Set-Content (Join-Path $folder 'exit.json')
 $results.Add($status)
 @{scope=$scope;complete=($results.Count -eq $jobs.Count);runs=$results.ToArray()}|ConvertTo-Json -Depth 6|Set-Content (Join-Path $root 'manifest.json')
 Write-Output "Finished $($job.name) exit=$($run.process.ExitCode)"
 if($run.process.ExitCode -ne 0){throw 'Fixture process failed'}
}
