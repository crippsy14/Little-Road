param([Parameter(Mandatory=$true)][string]$Executable,[Parameter(Mandatory=$true)][string]$SourceCapture,[Parameter(Mandatory=$true)][string]$OutputDirectory,[ValidateRange(1,6)][int]$ControlSteps=1,[switch]$AdaptiveEnvelope)
$ErrorActionPreference='Stop'
$replayExe=(Resolve-Path -LiteralPath $Executable).Path;$replaySource=(Resolve-Path -LiteralPath $SourceCapture).Path
$replayRoot=[IO.Path]::GetFullPath($OutputDirectory)
if(Test-Path -LiteralPath $replayRoot){throw 'Choose a new evidence directory.'}
New-Item -ItemType Directory -Path $replayRoot|Out-Null
Copy-Item -LiteralPath $replaySource -Destination (Join-Path $replayRoot 'source-approaches.json')
$cases=@();foreach($index in @(1,2)){foreach($mode in @('current','reanchor','guide')){$cases+=,"start-$index-$mode"}}
[ordered]@{scope='Captured field-car approaches at two observed times, then two seconds on declared frozen paths using normal Rigidbody forces. No assumption that any path is safe; accuracy and fate are measured before choosing a correction. Not traffic or race acceptance.';cases=$cases;horizon=2;positionMaximumMetres=.25;bodyUnderestimateMaximumMetres=.1;departureTimingErrorSeconds=.1;minimumEligibleSamples=24;requireNormalPathsContained=$false;requireAtLeastOneOverspeedDeparture=$false;requireContactFree=$true;forecastControlSteps=$ControlSteps;adaptiveEnvelope=[bool]$AdaptiveEnvelope;envelopeUnderestimateTolerance=.002;sourceSha256=(Get-FileHash -LiteralPath $replaySource).Hash}|ConvertTo-Json|Set-Content (Join-Path $replayRoot 'expectations.json')
$results=[Collections.Generic.List[object]]::new()
foreach($mode in @('current','reanchor','guide')) {
    $jobs=@()
    foreach($index in @(1,2)) {
        $name="start-$index-$mode";$folder=Join-Path $replayRoot $name
        $launch=& "$PSScriptRoot/run-technical-trial.ps1" -Executable $replayExe -OutputDirectory $folder -Policy reference -Loadout club -Cars 1 -Laps 1 -Duration 5 -PhysicalRecovery -PathProbe "approach-$mode" -PathApproachFile $replaySource -PathApproachIndex $index -PathControlSteps $ControlSteps -PathAdaptiveEnvelope:$AdaptiveEnvelope -RacingLine cedar-smooth-v2 -PassProcess
        $launch.metadata|ConvertTo-Json -Depth 6|Set-Content (Join-Path $folder 'launch.json')
        $jobs+=[pscustomobject]@{name=$name;folder=$folder;process=$launch.process}
        Write-Output "Started $name pid=$($launch.process.Id)"
    }
    foreach($job in $jobs){
        $job.process.WaitForExit()
        [pscustomobject]@{pid=$job.process.Id;exitCode=$job.process.ExitCode}|ConvertTo-Json|Set-Content (Join-Path $job.folder 'exit.json')
        $results.Add([pscustomobject]@{name=$job.name;exitCode=$job.process.ExitCode})
        Write-Output "Finished $($job.name) exit=$($job.process.ExitCode)"
    }
    [pscustomobject]@{complete=($results.Count -eq $cases.Count);executable=$replayExe;runs=$results.ToArray()}|ConvertTo-Json -Depth 5|Set-Content (Join-Path $replayRoot 'manifest.json')
}

