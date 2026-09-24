param(
    [Parameter(Mandatory=$true)][string]$Executable,
    [Parameter(Mandatory=$true)][string]$OutputDirectory
)
$ErrorActionPreference='Stop'
$laneExecutable=(Resolve-Path -LiteralPath $Executable).Path
$laneRoot=[IO.Path]::GetFullPath($OutputDirectory)
if(Test-Path -LiteralPath $laneRoot){throw 'Choose a new evidence folder.'}
New-Item -ItemType Directory -Path $laneRoot|Out-Null
$laneHash=(Get-FileHash -LiteralPath (Join-Path (Split-Path $laneExecutable) 'Little Road Unity_Data/Managed/Assembly-CSharp.dll')).Hash
$laneResults=[Collections.Generic.List[object]]::new()
foreach($side in @('left','middle','right')) {
    $laneJobs=@()
    foreach($setup in @('street','club')) {
        $folder=Join-Path $laneRoot "$side-$setup"
        $launch=& "$PSScriptRoot/run-technical-trial.ps1" -Executable $laneExecutable -OutputDirectory $folder -Loadout $setup -Cars 1 -Laps 1 -Duration 130 -Policy traffic -TrafficCase "lane-$side" -PhysicalRecovery -RacingLine cedar-smooth-v2 -PassProcess
        $laneJobs+=[pscustomobject]@{setup=$setup;side=$side;folder=$folder;process=$launch.process}
        Write-Output "Started $side $setup pid=$($launch.process.Id)"
    }
    foreach($job in $laneJobs) {
        while(-not $job.process.HasExited){Start-Sleep -Seconds 2;$job.process.Refresh()}
        $job.process.WaitForExit()
        $laneResults.Add([pscustomobject]@{side=$job.side;loadout=$job.setup;exitCode=$job.process.ExitCode;report=(Join-Path $job.folder 'race.json')})
        Write-Output "Finished $($job.side) $($job.setup) exit=$($job.process.ExitCode)"
    }
    [pscustomobject]@{executable=$laneExecutable;assemblyHash=$laneHash;complete=($side -eq 'right');runs=$laneResults.ToArray()}|ConvertTo-Json -Depth 5|Set-Content (Join-Path $laneRoot 'manifest.json')
}
