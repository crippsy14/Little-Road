param([Parameter(Mandatory=$true)][string]$Executable,[Parameter(Mandatory=$true)][string]$OutputDirectory)
$ErrorActionPreference='Stop'
$contextExe=(Resolve-Path -LiteralPath $Executable).Path
$contextRoot=[IO.Path]::GetFullPath($OutputDirectory)
if(Test-Path -LiteralPath $contextRoot){throw 'Choose a new evidence folder.'}
New-Item -ItemType Directory -Path $contextRoot | Out-Null
$contextQueue=[Collections.Generic.Queue[object]]::new()
foreach($setup in @('stock','street','club')) {foreach($policy in @('reference','context')) {
    $contextQueue.Enqueue(@{name="solo-$setup-$policy";setup=$setup;policy=$policy;fixture='solo';cars=1;opponents='stock';laps=2})
}}
foreach($policy in @('reference','context')) {
    $contextQueue.Enqueue(@{name="rear-$policy";setup='club';policy=$policy;fixture='rear-control';cars=2;opponents='stock';laps=1})
}
$contextQueue.Enqueue(@{name='pass-context';setup='club';policy='context';fixture='pass';cars=2;opponents='stock';laps=1})
$contextActive=[Collections.Generic.List[object]]::new()
$contextResults=[Collections.Generic.List[object]]::new()
while($contextQueue.Count -gt 0 -or $contextActive.Count -gt 0) {
    while($contextQueue.Count -gt 0 -and $contextActive.Count -lt 2) {
        $job=$contextQueue.Dequeue();$folder=Join-Path $contextRoot $job.name
        $launch=& "$PSScriptRoot/run-technical-trial.ps1" -Executable $contextExe -OutputDirectory $folder -Loadout $job.setup -Policy $job.policy -Cars $job.cars -Opponents $job.opponents -Laps $job.laps -Duration 240 -TrafficCase $job.fixture -PhysicalRecovery -RacingLine cedar-smooth-v2 -PassProcess
        $launch.metadata | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $folder 'launch.json')
        $contextActive.Add([pscustomobject]@{job=$job;folder=$folder;process=$launch.process})
        Write-Output "Started $($job.name) pid=$($launch.process.Id)"
    }
    foreach($entry in @($contextActive.ToArray())) {
        $entry.process.Refresh();if(-not $entry.process.HasExited){continue};$entry.process.WaitForExit()
        $status=[pscustomobject]@{name=$entry.job.name;pid=$entry.process.Id;exitCode=$entry.process.ExitCode;report=(Join-Path $entry.folder 'race.json')}
        $status | ConvertTo-Json | Set-Content (Join-Path $entry.folder 'exit.json')
        $contextResults.Add($status);$contextActive.Remove($entry)|Out-Null
        Write-Output "Finished $($entry.job.name) exit=$($entry.process.ExitCode)"
    }
    [pscustomobject]@{executable=$contextExe;scope='Initial mid-course solo/flying-lap and two-car screen; not full acceptance, repeatability, full-field or rendering proof.';complete=($contextQueue.Count -eq 0 -and $contextActive.Count -eq 0);runs=$contextResults.ToArray()} | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $contextRoot 'manifest.json')
    if($contextQueue.Count -gt 0 -or $contextActive.Count -gt 0){Start-Sleep -Seconds 2}
}
