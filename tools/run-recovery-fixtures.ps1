param(
    [Parameter(Mandatory=$true)][string]$Executable,
    [Parameter(Mandatory=$true)][string]$OutputDirectory,
    [string[]]$Cases=@('permanent-block','long-blocked-wait','sideways','wrong-way','off-road','wall','missed-gate','blocked-wait','traffic'),
    [string[]]$Loadouts=@('stock','club'),
    [switch]$RollingRecovery,
    [int]$Concurrency=2
)
$ErrorActionPreference='Stop'
$recoveryExecutable=(Resolve-Path -LiteralPath $Executable).Path
$recoveryRoot=[IO.Path]::GetFullPath($OutputDirectory)
if(Test-Path -LiteralPath $recoveryRoot){throw 'Choose a new evidence directory.'}
New-Item -ItemType Directory -Path $recoveryRoot|Out-Null
$recoveryHash=(Get-FileHash -LiteralPath (Join-Path (Split-Path $recoveryExecutable) 'Little Road Unity_Data/Managed/Assembly-CSharp.dll') -Algorithm SHA256).Hash
$recoveryQueue=[Collections.Generic.Queue[object]]::new()
foreach($loadout in $Loadouts){foreach($fixture in $Cases){$recoveryQueue.Enqueue([pscustomobject]@{loadout=$loadout;fixture=$fixture;name="$loadout-$fixture"})}}
$recoveryActive=[Collections.Generic.List[object]]::new();$recoveryResults=[Collections.Generic.List[object]]::new()
while($recoveryQueue.Count -gt 0 -or $recoveryActive.Count -gt 0){
    while($recoveryQueue.Count -gt 0 -and $recoveryActive.Count -lt [Math]::Max(1,$Concurrency)){
        $job=$recoveryQueue.Dequeue();$folder=Join-Path $recoveryRoot $job.name;$cars=if($job.fixture -eq 'traffic'){2}else{1}
        $launch=& "$PSScriptRoot/run-technical-trial.ps1" -Executable $recoveryExecutable -OutputDirectory $folder -Loadout $job.loadout -Policy reference -RacingLine cedar-smooth-v2 -Cars $cars -Opponents club -Laps 1 -PhysicalRecovery -RollingRecovery:$RollingRecovery -RecoveryCase $job.fixture -PassProcess
        $recoveryActive.Add([pscustomobject]@{job=$job;process=$launch.process;folder=$folder})
        Write-Output "Started $($job.name) pid=$($launch.process.Id)"
    }
    foreach($entry in @($recoveryActive.ToArray())){
        $entry.process.Refresh();if(-not $entry.process.HasExited){continue};$entry.process.WaitForExit()
        $recoveryResults.Add([pscustomobject]@{name=$entry.job.name;loadout=$entry.job.loadout;fixture=$entry.job.fixture;pid=$entry.process.Id;exitCode=$entry.process.ExitCode;report=(Join-Path $entry.folder 'race.json')})
        Write-Output "Finished $($entry.job.name) exit=$($entry.process.ExitCode)"
        $recoveryActive.Remove($entry)|Out-Null
        [pscustomobject]@{executable=$recoveryExecutable;assemblyHash=$recoveryHash;complete=($recoveryQueue.Count -eq 0 -and $recoveryActive.Count -eq 0);runs=$recoveryResults.ToArray()}|ConvertTo-Json -Depth 6|Set-Content (Join-Path $recoveryRoot 'manifest.json')
    }
    if($recoveryQueue.Count -gt 0 -or $recoveryActive.Count -gt 0){Start-Sleep -Seconds 2}
}
