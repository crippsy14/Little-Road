param(
    [Parameter(Mandatory=$true)][string]$Executable,
    [Parameter(Mandatory=$true)][string]$OutputDirectory
)
$ErrorActionPreference='Stop'
$paceExecutable=(Resolve-Path -LiteralPath $Executable).Path
$paceRoot=[IO.Path]::GetFullPath($OutputDirectory)
if(Test-Path -LiteralPath $paceRoot){throw 'Choose a new evidence folder.'}
New-Item -ItemType Directory -Path $paceRoot|Out-Null
$paceHash=(Get-FileHash -LiteralPath (Join-Path (Split-Path $paceExecutable) 'Little Road Unity_Data/Managed/Assembly-CSharp.dll')).Hash
$paceResults=[Collections.Generic.List[object]]::new()
foreach($scenario in @('solo','rear-reference','rear-control','rejoin-left','rejoin-right')) {
    $paceJobs=@()
    foreach($setup in @('street','club')) {
        $folder=Join-Path $paceRoot "$scenario-$setup"
        $cars=if($scenario.StartsWith('rear-')){2}else{1}
        $policy=if($scenario -eq 'rear-reference'){'reference'}else{'traffic'}
        $fixture=if($scenario -eq 'rear-reference'){'rear-control'}elseif($scenario -eq 'solo'){'pace-solo'}else{$scenario}
        $launch=& "$PSScriptRoot/run-technical-trial.ps1" -Executable $paceExecutable -OutputDirectory $folder -Loadout $setup -Cars $cars -Opponents stock -Laps 1 -Duration 100 -Policy $policy -TrafficCase $fixture -PhysicalRecovery -RacingLine cedar-smooth-v2 -PassProcess
        $paceJobs+=[pscustomobject]@{setup=$setup;scenario=$scenario;folder=$folder;process=$launch.process;metadata=$launch.metadata}
        Write-Output "Started $scenario $setup pid=$($launch.process.Id)"
    }
    foreach($job in $paceJobs) {
        while(-not $job.process.HasExited){Start-Sleep -Seconds 2;$job.process.Refresh()}
        $job.process.WaitForExit()
        $paceResults.Add([pscustomobject]@{scenario=$job.scenario;loadout=$job.setup;exitCode=$job.process.ExitCode;report=(Join-Path $job.folder 'race.json');launch=$job.metadata})
        Write-Output "Finished $($job.scenario) $($job.setup) exit=$($job.process.ExitCode)"
    }
    [pscustomobject]@{executable=$paceExecutable;assemblyHash=$paceHash;complete=($scenario -eq 'rejoin-right');runs=$paceResults.ToArray()}|ConvertTo-Json -Depth 6|Set-Content (Join-Path $paceRoot 'manifest.json')
}
