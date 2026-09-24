param([Parameter(Mandatory=$true)][string]$Executable,[Parameter(Mandatory=$true)][string]$OutputDirectory,[ValidateSet('traffic','context')][string]$Policy='traffic')
$ErrorActionPreference='Stop'
$cornerExecutable=(Resolve-Path -LiteralPath $Executable).Path
$cornerRoot=[IO.Path]::GetFullPath($OutputDirectory)
if(Test-Path -LiteralPath $cornerRoot){throw 'Choose a new evidence folder.'}
New-Item -ItemType Directory -Path $cornerRoot|Out-Null
$cornerHash=(Get-FileHash -LiteralPath (Join-Path (Split-Path $cornerExecutable) 'Little Road Unity_Data/Managed/Assembly-CSharp.dll')).Hash
$cornerResults=[Collections.Generic.List[object]]::new()
foreach($scenario in @('corner-solo-left','corner-solo-middle','corner-solo-right','corner-pair-left','corner-pair-right','corner-three')) {
    $cornerJobs=@()
    foreach($setup in @('street','club')) {
        $folder=Join-Path $cornerRoot "$scenario-$setup"
        $cars=if($scenario.StartsWith('corner-solo')){1}elseif($scenario -eq 'corner-three'){3}else{2}
        $launch=& "$PSScriptRoot/run-technical-trial.ps1" -Executable $cornerExecutable -OutputDirectory $folder -Loadout $setup -Cars $cars -Opponents "$setup,$setup" -Laps 1 -Duration 35 -Policy $Policy -TrafficCase $scenario -TrafficSpeed 18 -TrafficDiagnostics -PhysicalRecovery -RacingLine cedar-smooth-v2 -PassProcess
        $launch.metadata|ConvertTo-Json -Depth 5|Set-Content (Join-Path $folder 'launch.json')
        $cornerJobs+=[pscustomobject]@{setup=$setup;scenario=$scenario;folder=$folder;process=$launch.process}
        Write-Output "Started $scenario $setup pid=$($launch.process.Id)"
    }
    foreach($job in $cornerJobs) {
        while(-not $job.process.HasExited){Start-Sleep -Seconds 2;$job.process.Refresh()}
        $job.process.WaitForExit()
        [pscustomobject]@{pid=$job.process.Id;exitCode=$job.process.ExitCode}|ConvertTo-Json|Set-Content (Join-Path $job.folder 'exit.json')
        $cornerResults.Add([pscustomobject]@{scenario=$job.scenario;loadout=$job.setup;exitCode=$job.process.ExitCode;report=(Join-Path $job.folder 'race.json')})
        Write-Output "Finished $($job.scenario) $($job.setup) exit=$($job.process.ExitCode)"
    }
    [pscustomobject]@{executable=$cornerExecutable;assemblyHash=$cornerHash;policy=$Policy;complete=($scenario -eq 'corner-three');runs=$cornerResults.ToArray()}|ConvertTo-Json -Depth 5|Set-Content (Join-Path $cornerRoot 'manifest.json')
}
