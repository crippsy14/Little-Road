param(
    [string]$Executable=(Join-Path $PSScriptRoot '../../little-road-unity-windows/Little Road Unity.exe'),
    [string]$OutputDirectory,
    [string[]]$Courses=@('straight','brake','uturn'),
    [string[]]$Loadouts=@('stock','engine_workshop','engine_club','transmission_long','transmission_close','tyres_sport','brakes_sport','brakes_sport+tyres_sport','engine_club+transmission_long','club','engine_club+transmission_long+tyres_sport','engine_club+transmission_long+tyres_sport+brakes_sport'),
    [ValidateRange(1,100)][int]$Repeats=3,
    [ValidateRange(0,12)][float]$TimeScale=0,
    [switch]$Visible,
    [switch]$Manual,
    [switch]$Captures
)
$ErrorActionPreference='Stop'
$trialExecutable=(Resolve-Path -LiteralPath $Executable).Path
$trialCourses=$Courses -join ','
$trialLoadouts=$Loadouts -join ','
if($Manual -and (-not $Visible -or $trialCourses.Contains(',') -or $trialLoadouts.Contains(',') -or $Repeats -ne 1)) {
    throw 'Manual driving requires -Visible, one course, one loadout and -Repeats 1.'
}
if(-not $OutputDirectory) {
    $trialName=[DateTime]::Now.ToString('yyyyMMdd-HHmmss')+'-'+[Guid]::NewGuid().ToString('N').Substring(0,6)
    $OutputDirectory=Join-Path $PSScriptRoot "../review/proving-runs/$trialName"
}
$trialOutput=[IO.Path]::GetFullPath($OutputDirectory)
if(Test-Path -LiteralPath (Join-Path $trialOutput 'batch.json')){throw 'Choose a new evidence folder.'}
New-Item -ItemType Directory -Path $trialOutput -Force | Out-Null
if($TimeScale -eq 0){$TimeScale=if($Visible){1}else{8}}
$trialLog=Join-Path $trialOutput 'run.log'
$trialArguments=@('-screen-width','1600','-screen-height','900','--performance-trial','--performance-output',('"'+$trialOutput+'"'),
    '--performance-courses',('"'+$trialCourses+'"'),'--performance-loadouts',('"'+$trialLoadouts+'"'),'--performance-repeats',$Repeats.ToString(),
    '--performance-timescale',$TimeScale.ToString([Globalization.CultureInfo]::InvariantCulture),'-logFile',('"'+$trialLog+'"'))
if(-not $Visible){$trialArguments=@('-batchmode')+$trialArguments}
if($Manual){$trialArguments+='--performance-manual'}
if($Captures){$trialArguments+='--performance-captures'}
# A visible window is opened only when explicitly requested with -Visible.
$trialStyle=if($Visible){'Normal'}else{'Hidden'}
$trialProcess=Start-Process -FilePath $trialExecutable -ArgumentList $trialArguments -WindowStyle $trialStyle -PassThru
[pscustomobject]@{pid=$trialProcess.Id;output=$trialOutput;log=$trialLog;visible=[bool]$Visible;manual=[bool]$Manual;simulationScale=$TimeScale;expectedRuns=($trialCourses.Split(',').Count*$trialLoadouts.Split(',').Count*$Repeats)} | ConvertTo-Json
