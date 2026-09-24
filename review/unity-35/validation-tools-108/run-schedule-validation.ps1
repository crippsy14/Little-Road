param([int]$Version=108,[int]$Study=108,[int[]]$Seeds=@(1710,1711))
$ErrorActionPreference='Stop'
$taskPython='C:\Users\cripp\.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe'
foreach($taskSeed in $Seeds){foreach($taskTrack in @('bayview','cedar')){
    $taskPrefix="$taskTrack-seed$taskSeed"
    & "$PSScriptRoot/run-frame-cost.ps1" -Version $Version -Study $Study -Track $taskTrack -Seed $taskSeed -Rendered -Name "$taskPrefix-before"
    & "$PSScriptRoot/run-frame-cost.ps1" -Version $Version -Study $Study -Track $taskTrack -Seed $taskSeed -Rendered -StaggerContextPlans -ContextPlanGroups 3 -Name "$taskPrefix-after"
    $taskFolder=Join-Path $PSScriptRoot "../review/unity-35/frame-cost-$Study"
    foreach($taskArm in @('before','after')){
        & $taskPython "$PSScriptRoot/analyze-driver-frames.py" (Join-Path $taskFolder "$taskPrefix-$taskArm") | Out-Null
        if($LASTEXITCODE -ne 0){throw 'Frame analysis failed'}
    }
    & $taskPython "$PSScriptRoot/compare-planning-schedule.py" (Join-Path $taskFolder "$taskPrefix-before") (Join-Path $taskFolder "$taskPrefix-after") --group-gates
    # Comparison failure is evidence, not a reason to omit the remaining seed.
    if($LASTEXITCODE -notin @(0,1)){throw 'Comparison tool failed'}
}}
