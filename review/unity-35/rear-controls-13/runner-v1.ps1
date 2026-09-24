param([Parameter(Mandatory=$true)][string]$Baseline,[Parameter(Mandatory=$true)][string]$Candidate,[Parameter(Mandatory=$true)][string]$OutputDirectory)
$ErrorActionPreference='Stop'
$controlRoot=[IO.Path]::GetFullPath($OutputDirectory)
if(Test-Path -LiteralPath $controlRoot){throw 'Choose a new evidence directory.'}
New-Item -ItemType Directory -Path $controlRoot|Out-Null
$executables=@{baseline=(Resolve-Path -LiteralPath $Baseline).Path;candidate=(Resolve-Path -LiteralPath $Candidate).Path}
$cases=@(
    @{name='receding-left';fixture='merge-left';rear=12;gap=12;opponents='stock'},
    @{name='receding-right';fixture='merge-right';rear=12;gap=12;opponents='stock'},
    @{name='parallel-left';fixture='merge-left';rear=18;gap=12;opponents='street'},
    @{name='parallel-right';fixture='merge-right';rear=18;gap=12;opponents='street'},
    @{name='same-course';fixture='rear-control';rear=18;gap=12;opponents='street'},
    @{name='distant-closing';fixture='merge-left';rear=26;gap=60;opponents='club'}
)
[pscustomobject]@{scope='Initially harmless rear traffic, six declared states. Exact first 2 s lead-car commands/motion against frozen pre-change prototype, then all cars must complete cleanly. Later genuine interactions may differ.';unchangedWindowSeconds=2;timeout=45;cases=$cases}|ConvertTo-Json -Depth 5|Set-Content (Join-Path $controlRoot 'expectations.json')
$results=[Collections.Generic.List[object]]::new()
foreach($case in $cases){
    $jobs=@()
    foreach($version in @('baseline','candidate')){
        $name="$($case.name)-$version";$folder=Join-Path $controlRoot $name
        $launch=& "$PSScriptRoot/run-technical-trial.ps1" -Executable $executables[$version] -OutputDirectory $folder -Loadout street -Opponents $case.opponents -Cars 2 -Laps 1 -Duration 45 -Policy context -TrafficCase $case.fixture -TrafficGap $case.gap -TrafficSpeed 18 -TrafficRearSpeed $case.rear -PhysicalRecovery -RacingLine cedar-smooth-v2 -PassProcess
        $launch.metadata|ConvertTo-Json -Depth 6|Set-Content (Join-Path $folder 'launch.json')
        $jobs+=[pscustomobject]@{name=$name;case=$case.name;version=$version;folder=$folder;process=$launch.process}
        Write-Output "Started $name pid=$($launch.process.Id)"
    }
    foreach($job in $jobs){
        $job.process.WaitForExit()
        [pscustomobject]@{pid=$job.process.Id;exitCode=$job.process.ExitCode}|ConvertTo-Json|Set-Content (Join-Path $job.folder 'exit.json')
        $results.Add([pscustomobject]@{name=$job.name;case=$job.case;version=$job.version;exitCode=$job.process.ExitCode})
        Write-Output "Finished $($job.name) exit=$($job.process.ExitCode)"
    }
    [pscustomobject]@{complete=($case.name -eq 'distant-closing');executables=$executables;runs=$results.ToArray()}|ConvertTo-Json -Depth 5|Set-Content (Join-Path $controlRoot 'manifest.json')
}
