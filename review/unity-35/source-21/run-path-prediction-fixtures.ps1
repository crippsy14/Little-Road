param([Parameter(Mandatory=$true)][string]$Executable,[Parameter(Mandatory=$true)][string]$OutputDirectory,[ValidateRange(1,6)][int]$ControlSteps=1,[switch]$AdaptiveEnvelope)
$ErrorActionPreference='Stop'
$probeExe=(Resolve-Path -LiteralPath $Executable).Path;$probeRoot=[IO.Path]::GetFullPath($OutputDirectory)
if(Test-Path -LiteralPath $probeRoot){throw 'Choose a new evidence directory.'}
New-Item -ItemType Directory -Path $probeRoot|Out-Null
$cases=@('rejoin-left','rejoin-right','switchback-left','switchback-right','overspeed-left','overspeed-right')
[ordered]@{scope='First prospective two-second frozen-path probes, club car, normal physics. Five-second timed observations; lap completion is not expected or scored. Neither real-time performance nor general race acceptance.';cases=$cases;horizon=2;positionMaximumMetres=.25;bodyUnderestimateMaximumMetres=.1;departureTimingErrorSeconds=.1;minimumEligibleSamples=24;requireNormalPathsContained=$true;requireAtLeastOneOverspeedDeparture=$true}|ConvertTo-Json|Set-Content (Join-Path $probeRoot 'expectations.json')
$results=[Collections.Generic.List[object]]::new()
$declared=Get-Content (Join-Path $probeRoot 'expectations.json') -Raw|ConvertFrom-Json
$declared|Add-Member -NotePropertyName forecastControlSteps -NotePropertyValue $ControlSteps
$declared|Add-Member -NotePropertyName adaptiveEnvelope -NotePropertyValue ([bool]$AdaptiveEnvelope)
$declared|Add-Member -NotePropertyName envelopeUnderestimateTolerance -NotePropertyValue .002
$declared|ConvertTo-Json|Set-Content (Join-Path $probeRoot 'expectations.json')
for($i=0;$i -lt $cases.Length;$i+=2){
    $jobs=@()
    foreach($case in $cases[$i..($i+1)]){
        $folder=Join-Path $probeRoot $case
        $launch=& "$PSScriptRoot/run-technical-trial.ps1" -Executable $probeExe -OutputDirectory $folder -Policy reference -Loadout club -Cars 1 -Laps 1 -Duration 5 -PhysicalRecovery -PathProbe $case -PathControlSteps $ControlSteps -PathAdaptiveEnvelope:$AdaptiveEnvelope -RacingLine cedar-smooth-v2 -PassProcess
        $launch.metadata|ConvertTo-Json -Depth 6|Set-Content (Join-Path $folder 'launch.json')
        $jobs+=[pscustomobject]@{name=$case;folder=$folder;process=$launch.process}
        Write-Output "Started $case pid=$($launch.process.Id)"
    }
    foreach($job in $jobs){
        $job.process.WaitForExit()
        [pscustomobject]@{pid=$job.process.Id;exitCode=$job.process.ExitCode}|ConvertTo-Json|Set-Content (Join-Path $job.folder 'exit.json')
        $results.Add([pscustomobject]@{name=$job.name;exitCode=$job.process.ExitCode})
        Write-Output "Finished $($job.name) exit=$($job.process.ExitCode)"
    }
    [pscustomobject]@{complete=($i+2 -eq $cases.Length);executable=$probeExe;runs=$results.ToArray()}|ConvertTo-Json -Depth 5|Set-Content (Join-Path $probeRoot 'manifest.json')
}
