param([Parameter(Mandatory=$true)][string]$ReportPath)
$ErrorActionPreference='Stop'
$taskRace=Get-Content -LiteralPath $ReportPath -Raw | ConvertFrom-Json
$taskFailures=[System.Collections.Generic.List[string]]::new()
$taskCars=[System.Collections.Generic.List[object]]::new()
if($taskRace.finished -ne $taskRace.carCount){$taskFailures.Add('Incomplete field')}
$taskGateSpacing=$taskRace.trackLength/12
foreach($taskCar in $taskRace.cars){
    if(-not $taskCar.finished -or $taskCar.laps -ne $taskRace.requiredLaps){$taskFailures.Add("Car $($taskCar.id): incomplete laps")}
    $taskSamples=@($taskRace.motion|Where-Object {$_.id -eq $taskCar.id -and ($taskCar.time -le 0 -or $_.time -le $taskCar.time)})
    $taskPrevious=$null
    foreach($taskSample in $taskSamples){
        if($null -ne $taskPrevious){
            $taskDelta=$taskSample.nextGateProgress-$taskPrevious.nextGateProgress
            if($taskDelta -lt -.05 -or $taskDelta -gt $taskGateSpacing+.05){$taskFailures.Add("Car $($taskCar.id): unexpected checkpoint jump at $($taskSample.time)")}
            if($taskSample.progress -lt $taskPrevious.progress-10 -and $taskDelta -gt .05){$taskFailures.Add("Car $($taskCar.id): backward recovery also advanced a gate at $($taskSample.time); inspect trace")}
        }
        $taskPrevious=$taskSample
    }
    $taskMax=($taskSamples|ForEach-Object {[math]::Abs($_.offset)}|Measure-Object -Maximum).Maximum
    $taskCars.Add([ordered]@{id=$taskCar.id;equipment=$taskCar.equipment;place=$taskCar.place;time=$taskCar.time;maxRaceOffset=$taskMax;
        samplesOutsideRoad=@($taskSamples|Where-Object {[math]::Abs($_.offset)-gt 8}).Count;
        samplesBeyondRunoff=@($taskSamples|Where-Object {[math]::Abs($_.offset)-gt 12}).Count;
        contacts=$taskCar.contacts;recoveries=$taskCar.recoveries;missedGates=$taskCar.missedGates;maxContactPenetration=$taskCar.maxPenetration})
}
$taskPoseAudit=$null
if($taskRace.PSObject.Properties.Name -contains 'overlapSampledSteps') {
    $taskPoseAudit=[ordered]@{sampledSteps=$taskRace.overlapSampledSteps;querySaturations=$taskRace.overlapQuerySaturations;
        physicsStepSeconds=$taskRace.physicsStepSeconds;maxCarOverlap=$taskRace.maxCarPoseOverlap;
        maxEnvironmentOverlap=$taskRace.maxEnvironmentPoseOverlap;
        scope='Box collider overlap at completed physics poses against cars and tagged solid props. Does not inspect visual triangles, wheel steering bounds, or interpolation between steps.'}
}
$taskSummary=[ordered]@{source=(Resolve-Path -LiteralPath $ReportPath).Path;scope='Completion and 1Hz ordered-gate/offset evidence; not a continuous no-clipping proof or performance benchmark';
    completionAndSampledGatesPassed=($taskFailures.Count -eq 0);failures=$taskFailures;seed=$taskRace.seed;cars=$taskCars;
    totalRecoveryResets=($taskRace.cars|Measure-Object recoveries -Sum).Sum;
    totalMissedGates=($taskRace.cars|Measure-Object missedGates -Sum).Sum;
    maxRecordedContactPenetration=($taskRace.cars|Measure-Object maxPenetration -Maximum).Maximum;
    poseOverlapAudit=$taskPoseAudit}
$taskSummary | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath ([IO.Path]::ChangeExtension($ReportPath,'.validation.json'))
Write-Output ($taskSummary | ConvertTo-Json -Depth 5)
if($taskFailures.Count -gt 0){exit 1}
