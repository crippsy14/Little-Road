param(
    [Parameter(Mandatory=$true)][string]$Executable,
    [Parameter(Mandatory=$true)][string]$OutputDirectory,
    [switch]$IncludeQueue,
    [ValidateSet('traffic','context')][string]$Policy='traffic',
    [ValidateSet('follow','pass','no-room')][string[]]$Scenarios=@('follow','pass','no-room'),
    [ValidateRange(1,3)][int]$Concurrency=2,
    [switch]$RoadFeasibility,
    [switch]$PreparedPath,
    [switch]$GuidePathFallback,
    [ValidateRange(0,24)][int]$ParallelForecastWorkers=0
)
$ErrorActionPreference='Stop'
if($ParallelForecastWorkers -gt 0 -and (-not $RoadFeasibility -or $Policy -ne 'context')){throw 'Parallel forecasts need context road feasibility.'}
if($ParallelForecastWorkers -gt 0){$Concurrency=1}
$trafficExecutable=(Resolve-Path -LiteralPath $Executable).Path
$trafficRoot=[IO.Path]::GetFullPath($OutputDirectory)
if(Test-Path -LiteralPath $trafficRoot){throw 'Choose a new evidence directory.'}
New-Item -ItemType Directory -Path $trafficRoot|Out-Null
$trafficHash=(Get-FileHash -LiteralPath (Join-Path (Split-Path $trafficExecutable) 'Little Road Unity_Data/Managed/Assembly-CSharp.dll') -Algorithm SHA256).Hash
$trafficQueue=[Collections.Generic.Queue[object]]::new()
foreach($scenario in $Scenarios){
    $loadout=if($scenario -eq 'follow'){'stock'}else{'club'}
    $trafficQueue.Enqueue(@{name=$scenario;fixture=$scenario;loadout=$loadout;policy=$Policy;cars=2;opponents='stock'})
}
foreach($loadout in @('stock','club')){foreach($controlPolicy in @('reference',$Policy)){
    $trafficQueue.Enqueue(@{name="solo-$loadout-$controlPolicy";fixture='solo';loadout=$loadout;policy=$controlPolicy;cars=1;opponents='stock'})
}}
if($IncludeQueue){$trafficQueue.Enqueue(@{name='queue';fixture='queue';loadout='club';policy=$Policy;cars=3;opponents='street,stock'})}
$trafficActive=[Collections.Generic.List[object]]::new();$trafficResults=[Collections.Generic.List[object]]::new()
while($trafficQueue.Count -gt 0 -or $trafficActive.Count -gt 0){
    while($trafficQueue.Count -gt 0 -and $trafficActive.Count -lt $Concurrency){
        $job=$trafficQueue.Dequeue();$folder=Join-Path $trafficRoot $job.name
        $options=@{}
        if($job.policy -eq 'context'){$options=@{RoadFeasibility=[bool]$RoadFeasibility;PreparedPath=[bool]$PreparedPath;GuidePathFallback=[bool]$GuidePathFallback;ParallelForecastWorkers=$ParallelForecastWorkers}}
        $launch=& "$PSScriptRoot/run-technical-trial.ps1" -Executable $trafficExecutable -OutputDirectory $folder -Loadout $job.loadout -Policy $job.policy -RacingLine cedar-smooth-v2 -Cars $job.cars -Opponents $job.opponents -Laps 1 -PhysicalRecovery -TrafficCase $job.fixture @options -PassProcess
        $launch.metadata|ConvertTo-Json -Depth 6|Set-Content (Join-Path $folder 'launch.json')
        $trafficActive.Add([pscustomobject]@{job=$job;process=$launch.process;folder=$folder})
        Write-Output "Started $($job.name) pid=$($launch.process.Id)"
    }
    foreach($entry in @($trafficActive.ToArray())){
        $entry.process.Refresh();if(-not $entry.process.HasExited){continue};$entry.process.WaitForExit()
        [pscustomobject]@{pid=$entry.process.Id;exitCode=$entry.process.ExitCode}|ConvertTo-Json|Set-Content (Join-Path $entry.folder 'exit.json')
        $trafficResults.Add([pscustomobject]@{name=$entry.job.name;fixture=$entry.job.fixture;loadout=$entry.job.loadout;policy=$entry.job.policy;cars=$entry.job.cars;opponents=$entry.job.opponents;pid=$entry.process.Id;exitCode=$entry.process.ExitCode;report=(Join-Path $entry.folder 'race.json')})
        Write-Output "Finished $($entry.job.name) exit=$($entry.process.ExitCode)"
        $trafficActive.Remove($entry)|Out-Null
        [pscustomobject]@{executable=$trafficExecutable;assemblyHash=$trafficHash;policy=$Policy;scenarios=$Scenarios;includeQueue=[bool]$IncludeQueue;roadFeasibility=[bool]$RoadFeasibility;preparedPath=[bool]$PreparedPath;guidePathFallback=[bool]$GuidePathFallback;parallelForecastWorkers=$ParallelForecastWorkers;complete=($trafficQueue.Count -eq 0 -and $trafficActive.Count -eq 0);runs=$trafficResults.ToArray()}|ConvertTo-Json -Depth 6|Set-Content (Join-Path $trafficRoot 'manifest.json')
    }
    if($trafficQueue.Count -gt 0 -or $trafficActive.Count -gt 0){Start-Sleep -Seconds 2}
}
