param([Parameter(Mandatory=$true)][string]$Executable,[Parameter(Mandatory=$true)][string]$WitnessExecutable,[Parameter(Mandatory=$true)][string]$OutputDirectory,[switch]$ContextOnly)
$ErrorActionPreference='Stop'
$contextExe=(Resolve-Path -LiteralPath $Executable).Path
$witnessExe=(Resolve-Path -LiteralPath $WitnessExecutable).Path
$passingRoot=[IO.Path]::GetFullPath($OutputDirectory)
if(Test-Path -LiteralPath $passingRoot){throw 'Choose a new evidence folder.'}
New-Item -ItemType Directory -Path $passingRoot|Out-Null
$passingResults=[Collections.Generic.List[object]]::new()
$passingCases=@(@{name='standard';gap=24;speed=18;repeats=3},@{name='close-fast';gap=12;speed=26;repeats=1},@{name='distant-slow';gap=36;speed=12;repeats=1})
[pscustomobject]@{definition='Frozen before this batch: repeatability <=0.5% time range; every feasible pass must be clean and sustained within 1.25 times the independently driven witness time plus 1 second. Invalid witness means feasibility unproven, not an easier deadline.';cases=$passingCases}|ConvertTo-Json -Depth 5|Set-Content (Join-Path $passingRoot 'expectations.json')
foreach($case in $passingCases){for($repeat=1;$repeat -le $case.repeats;$repeat++){
    $passingJobs=@()
    $policies=if($ContextOnly){@('context')}else{@('traffic','context')}
    foreach($policy in $policies){
        $exe=if($policy -eq 'traffic'){$witnessExe}else{$contextExe}
        $name="$($case.name)-$policy-r$repeat";$folder=Join-Path $passingRoot $name
        $launch=& "$PSScriptRoot/run-technical-trial.ps1" -Executable $exe -OutputDirectory $folder -Loadout club -Opponents stock -Policy $policy -TrafficCase pass -Cars 2 -Laps 1 -Duration 80 -TrafficGap $case.gap -TrafficSpeed $case.speed -PhysicalRecovery -RacingLine cedar-smooth-v2 -PassProcess
        $launch.metadata|ConvertTo-Json -Depth 5|Set-Content (Join-Path $folder 'launch.json')
        $passingJobs+=[pscustomobject]@{name=$name;case=$case.name;repeat=$repeat;policy=$policy;folder=$folder;process=$launch.process}
        Write-Output "Started $name pid=$($launch.process.Id)"
    }
    foreach($job in $passingJobs){
        while(-not $job.process.HasExited){Start-Sleep -Seconds 2;$job.process.Refresh()};$job.process.WaitForExit()
        $status=[pscustomobject]@{name=$job.name;case=$job.case;repeat=$job.repeat;policy=$job.policy;pid=$job.process.Id;exitCode=$job.process.ExitCode;folder=$job.folder}
        $status|ConvertTo-Json|Set-Content (Join-Path $job.folder 'exit.json');$passingResults.Add($status)
        Write-Output "Finished $($job.name) exit=$($job.process.ExitCode)"
    }
    [pscustomobject]@{complete=($case.name -eq 'distant-slow');scope='Independent historical driver as physical passing witness; shared physics provenance must be checked separately. No field or rendering acceptance.';runs=$passingResults.ToArray()}|ConvertTo-Json -Depth 5|Set-Content (Join-Path $passingRoot 'manifest.json')
}}
