param(
    [Parameter(Mandatory=$true)][string]$Executable,
    [Parameter(Mandatory=$true)][string]$OutputDirectory,
    [ValidateRange(1,24)][int]$Workers=8
)
$ErrorActionPreference='Stop'
$exe=(Resolve-Path -LiteralPath $Executable).Path
$root=[IO.Path]::GetFullPath($OutputDirectory)
if(Test-Path -LiteralPath $root){throw 'Choose a new evidence directory.'}
New-Item -ItemType Directory -Path $root | Out-Null
$cases=@(
    @{name='queue';cars=3;loadout='club';opponents='street,stock';gap=24},
    @{name='no-room';cars=2;loadout='club';opponents='stock';gap=24},
    @{name='merge-left';cars=2;loadout='street';opponents='club';gap=12},
    @{name='merge-right';cars=2;loadout='street';opponents='club';gap=12},
    @{name='brake-leader';cars=2;loadout='club';opponents='stock';gap=24},
    @{name='corner-three';cars=3;loadout='club';opponents='street,stock';gap=24}
)
$scope='Same-build serial versus parallel forecasts; recorded motion, controls, decisions, race events/results must match exactly. Existing scenario defects remain defects. No scenario acceptance or rendered performance inferred from parity. One process at a time.'
@{scope=$scope;cases=$cases;workers=$Workers;duration=180;seed=1709;roadFeasibility=$true;preparedPath=$true} | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $root 'expectations.json') -Encoding utf8
$results=[Collections.Generic.List[object]]::new()
foreach($case in $cases){foreach($mode in @('serial','parallel')){
    $count=if($mode -eq 'parallel'){$Workers}else{0}
    $name=$case.name+'-'+$mode
    $folder=Join-Path $root $name
    $run=& "$PSScriptRoot/run-technical-trial.ps1" -Executable $exe -OutputDirectory $folder -Cars $case.cars -Loadout $case.loadout -Opponents $case.opponents -Policy context -RacingLine cedar-smooth-v2 -TrafficCase $case.name -TrafficGap $case.gap -TrafficSpeed 18 -TrafficRearSpeed 26 -Laps 1 -Duration 180 -PhysicalRecovery -TrafficDiagnostics -RoadFeasibility -PreparedPath -ParallelForecastWorkers $count -PassProcess
    $run.metadata | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $folder 'launch.json') -Encoding utf8
    Write-Output ('Started '+$name+' pid='+$run.process.Id)
    $run.process.WaitForExit()
    $status=@{name=$name;scenario=$case.name;mode=$mode;pid=$run.process.Id;exitCode=$run.process.ExitCode;folder=$folder}
    $status | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $folder 'exit.json') -Encoding utf8
    $results.Add($status)
    @{scope=$scope;complete=($results.Count -eq $cases.Count*2);runs=$results.ToArray()} | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $root 'manifest.json') -Encoding utf8
    Write-Output ('Finished '+$name+' exit='+$run.process.ExitCode)
    if($run.process.ExitCode -ne 0){throw 'Fixture failed: '+$name}
}}
