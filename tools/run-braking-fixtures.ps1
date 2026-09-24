param([Parameter(Mandatory=$true)][string]$Executable,[Parameter(Mandatory=$true)][string]$OutputDirectory,[ValidateSet('traffic','context')][string]$Policy='traffic',[ValidateSet('brake-leader','brake-probe')][string[]]$Scenarios=@('brake-leader','brake-probe'))
$ErrorActionPreference='Stop'
$brakingExecutable=(Resolve-Path -LiteralPath $Executable).Path
$brakingRoot=[IO.Path]::GetFullPath($OutputDirectory)
if(Test-Path -LiteralPath $brakingRoot){throw 'Choose a new evidence folder.'}
New-Item -ItemType Directory -Path $brakingRoot|Out-Null
$brakingHash=(Get-FileHash -LiteralPath (Join-Path (Split-Path $brakingExecutable) 'Little Road Unity_Data/Managed/Assembly-CSharp.dll')).Hash
$brakingResults=[Collections.Generic.List[object]]::new()
foreach($scenario in $Scenarios) {foreach($setup in @('street','club')) {
    $brakingJobs=@()
    foreach($disabled in @($true,$false)) {
        $mode=if($disabled){'baseline'}else{'safety'}
        $folder=Join-Path $brakingRoot "$scenario-$setup-$mode"
        $speed=if($setup -eq 'club'){26}else{18}
        $launch=& "$PSScriptRoot/run-technical-trial.ps1" -Executable $brakingExecutable -OutputDirectory $folder -Loadout $setup -Opponents $setup -Cars 2 -Laps 1 -Duration 45 -Policy $Policy -TrafficCase $scenario -TrafficGap 12 -TrafficSpeed $speed -PhysicalRecovery -RacingLine cedar-smooth-v2 -NoImmediateSafety:$disabled -PassProcess
        $launch.metadata|ConvertTo-Json|Set-Content (Join-Path $folder 'launch.json')
        $brakingJobs+=[pscustomobject]@{scenario=$scenario;setup=$setup;mode=$mode;folder=$folder;process=$launch.process}
        Write-Output "Started $scenario $setup $mode pid=$($launch.process.Id)"
    }
    foreach($job in $brakingJobs) {
        $job.process.WaitForExit()
        [pscustomobject]@{pid=$job.process.Id;exitCode=$job.process.ExitCode}|ConvertTo-Json|Set-Content (Join-Path $job.folder 'exit.json')
        $brakingResults.Add([pscustomobject]@{scenario=$job.scenario;setup=$job.setup;mode=$job.mode;folder=$job.folder;exitCode=$job.process.ExitCode})
        Write-Output "Finished $($job.scenario) $($job.setup) $($job.mode) exit=$($job.process.ExitCode)"
    }
    [pscustomobject]@{executable=$brakingExecutable;assemblyHash=$brakingHash;policy=$Policy;scenarios=$Scenarios;complete=($scenario -eq $Scenarios[-1] -and $setup -eq 'club');runs=$brakingResults.ToArray()}|ConvertTo-Json -Depth 5|Set-Content (Join-Path $brakingRoot 'manifest.json')
}}
