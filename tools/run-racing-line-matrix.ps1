param(
    [Parameter(Mandatory=$true)][string]$Executable,
    [Parameter(Mandatory=$true)][string]$OutputDirectory,
    [string[]]$Variants=@('centre','mincurve','half','early','late'),
    [string[]]$Loadouts=@('stock','club'),
    [int]$Repeats=1,
    [int]$Laps=3,
    [ValidateSet('pack','line')][string]$Grid='pack',
    [int]$Concurrency=2
)
$ErrorActionPreference='Stop'
$matrixExecutable=(Resolve-Path -LiteralPath $Executable).Path
$matrixRoot=[IO.Path]::GetFullPath($OutputDirectory)
if(Test-Path -LiteralPath $matrixRoot){throw 'Choose a new matrix folder; existing evidence is never overwritten.'}
New-Item -ItemType Directory -Path $matrixRoot | Out-Null
$matrixAssembly=Join-Path (Split-Path $matrixExecutable) 'Little Road Unity_Data/Managed/Assembly-CSharp.dll'
$matrixHash=(Get-FileHash -LiteralPath $matrixAssembly -Algorithm SHA256).Hash
$matrixQueue=[Collections.Generic.Queue[object]]::new()
foreach($loadout in $Loadouts){foreach($variant in $Variants){for($repeat=1;$repeat -le $Repeats;$repeat++){
    $matrixQueue.Enqueue([pscustomobject]@{loadout=$loadout;variant=$variant;repeat=$repeat;name="$loadout-$variant-r$repeat"})
}}}
$matrixActive=[Collections.Generic.List[object]]::new()
$matrixResults=[Collections.Generic.List[object]]::new()
while($matrixQueue.Count -gt 0 -or $matrixActive.Count -gt 0){
    while($matrixQueue.Count -gt 0 -and $matrixActive.Count -lt [Math]::Max(1,$Concurrency)){
        $job=$matrixQueue.Dequeue()
        $folder=Join-Path $matrixRoot $job.name
        $lineId=if($job.variant -match '-v[0-9]+$'){"cedar-$($job.variant)"}else{"cedar-$($job.variant)-v1"}
        $launch=& "$PSScriptRoot/run-technical-trial.ps1" -Executable $matrixExecutable -OutputDirectory $folder -Loadout $job.loadout -Policy reference -RacingLine $lineId -Laps $Laps -Grid $Grid -PassProcess
        $process=$launch.process
        $matrixActive.Add([pscustomobject]@{job=$job;process=$process;folder=$folder;started=[DateTime]::UtcNow})
        Write-Output "Started $($job.name) pid=$($process.Id)"
    }
    foreach($entry in @($matrixActive.ToArray())){
        $entry.process.Refresh()
        if(-not $entry.process.HasExited){continue}
        $entry.process.WaitForExit()
        $matrixResults.Add([pscustomobject]@{name=$entry.job.name;loadout=$entry.job.loadout;variant=$entry.job.variant;repeat=$entry.job.repeat;pid=$entry.process.Id;exitCode=$entry.process.ExitCode;report=(Join-Path $entry.folder 'race.json');reportExists=(Test-Path (Join-Path $entry.folder 'race.json'));wallSeconds=([DateTime]::UtcNow-$entry.started).TotalSeconds})
        Write-Output "Finished $($entry.job.name) exit=$($entry.process.ExitCode)"
        $matrixActive.Remove($entry)|Out-Null
        [pscustomobject]@{executable=$matrixExecutable;assemblyHash=$matrixHash;laps=$Laps;grid=$Grid;complete=($matrixQueue.Count -eq 0 -and $matrixActive.Count -eq 0);runs=$matrixResults.ToArray()}|ConvertTo-Json -Depth 6|Set-Content (Join-Path $matrixRoot 'manifest.json')
    }
    if($matrixQueue.Count -gt 0 -or $matrixActive.Count -gt 0){Start-Sleep -Seconds 2}
}
if(@($matrixResults|Where-Object{-not $_.reportExists -or $_.exitCode -ne 0}).Count -gt 0){throw 'A matrix run failed; inspect the manifest and retained logs.'}
