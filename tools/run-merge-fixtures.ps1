param([Parameter(Mandatory=$true)][string]$Executable,[Parameter(Mandatory=$true)][string]$OutputDirectory)
$ErrorActionPreference='Stop'
$mergeExecutable=(Resolve-Path -LiteralPath $Executable).Path
$mergeRoot=[IO.Path]::GetFullPath($OutputDirectory)
if(Test-Path -LiteralPath $mergeRoot){throw 'Choose a new evidence directory.'}
New-Item -ItemType Directory -Path $mergeRoot|Out-Null
$mergeHash=(Get-FileHash -LiteralPath (Join-Path (Split-Path $mergeExecutable) 'Little Road Unity_Data/Managed/Assembly-CSharp.dll')).Hash
[pscustomobject]@{scope='Mirrored straight merge with an approaching faster car, 12/24 m rear gaps. Same street/club cars, 18/26 m/s release. Witness holds front car straight at full throttle until the rear is body-clear; normal inputs/physics throughout.';timeout=45;passDeadlineMultiplier=1.25;passDeadlineAllowance=1;requiredClearSeconds=2;zeroContacts=$true;zeroDepartures=$true;zeroRecovery=$true}|ConvertTo-Json|Set-Content (Join-Path $mergeRoot 'expectations.json')
$mergeResults=[Collections.Generic.List[object]]::new()
foreach($gap in @(12,24)){foreach($side in @('left','right')){
    $mergeJobs=@()
    foreach($mode in @('witness','candidate')){
        $scenario=if($mode -eq 'witness'){"merge-witness-$side"}else{"merge-$side"}
        $name="$side-$gap-$mode";$folder=Join-Path $mergeRoot $name
        $launch=& "$PSScriptRoot/run-technical-trial.ps1" -Executable $mergeExecutable -OutputDirectory $folder -Loadout street -Opponents club -Cars 2 -Laps 1 -Duration 45 -Policy context -TrafficCase $scenario -TrafficGap $gap -TrafficSpeed 18 -TrafficRearSpeed 26 -PhysicalRecovery -RacingLine cedar-smooth-v2 -PassProcess
        $launch.metadata|ConvertTo-Json -Depth 6|Set-Content (Join-Path $folder 'launch.json')
        $mergeJobs+=[pscustomobject]@{name=$name;scenario=$scenario;side=$side;gap=$gap;mode=$mode;folder=$folder;process=$launch.process}
        Write-Output "Started $name pid=$($launch.process.Id)"
    }
    foreach($job in $mergeJobs){
        $job.process.WaitForExit()
        [pscustomobject]@{pid=$job.process.Id;exitCode=$job.process.ExitCode}|ConvertTo-Json|Set-Content (Join-Path $job.folder 'exit.json')
        $mergeResults.Add([pscustomobject]@{name=$job.name;scenario=$job.scenario;side=$job.side;gap=$job.gap;mode=$job.mode;folder=$job.folder;exitCode=$job.process.ExitCode})
        Write-Output "Finished $($job.name) exit=$($job.process.ExitCode)"
    }
    [pscustomobject]@{executable=$mergeExecutable;assemblyHash=$mergeHash;complete=($gap -eq 24 -and $side -eq 'right');runs=$mergeResults.ToArray()}|ConvertTo-Json -Depth 6|Set-Content (Join-Path $mergeRoot 'manifest.json')
}}
