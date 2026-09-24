param(
    [Parameter(Mandatory=$true)][string]$Executable,
    [Parameter(Mandatory=$true)][string]$OutputDirectory,
    [ValidateSet('default','direct')][string]$Rendering='default',
    [ValidateSet('fullscreen','windowed')][string]$Display='fullscreen',
    [float]$PlaybackRate=4,
    [int]$GraceSeconds=30
)
$ErrorActionPreference='Stop'
$probeExe=(Resolve-Path -LiteralPath $Executable).Path
$probeRoot=[IO.Path]::GetFullPath($OutputDirectory)
if(Test-Path -LiteralPath $probeRoot){throw 'Choose a new evidence folder.'}
New-Item -ItemType Directory -Path $probeRoot|Out-Null
$probeReport=Join-Path $probeRoot 'race.json';$probeLog=Join-Path $probeRoot 'run.log'
$probeArguments=@('--trial-auto','--trial-track','cedar','--trial-setup','club','--trial-cars','2','--trial-opponents','stock','--trial-laps','1',
    '--trial-timescale',$PlaybackRate.ToString([Globalization.CultureInfo]::InvariantCulture),'--driver-policy','traffic','--traffic-case','pass','--physical-recovery','--racing-line','cedar-smooth-v2',
    '--trial-report',('"'+$probeReport+'"'),'-logFile',('"'+$probeLog+'"'),'-screen-width','1600','-screen-height','900')
if($Display -eq 'fullscreen'){$probeArguments+='--start-fullscreen'}else{$probeArguments+=@('--start-windowed','-screen-fullscreen','0')}
if($Rendering -eq 'direct'){$probeArguments+='-force-gfx-direct'}
$probeProcess=Start-Process -FilePath $probeExe -ArgumentList $probeArguments -WindowStyle Normal -PassThru
$probeStarted=[DateTime]::UtcNow;$probeReportAt=$null;$probeForced=$false;$probeReason=$null
$probeLaunch=[pscustomobject]@{pid=$probeProcess.Id;executable=$probeExe;arguments=$probeArguments;started=$probeStarted.ToString('o');rendering=$Rendering;display=$Display;playbackRate=$PlaybackRate}
$probeLaunch|ConvertTo-Json|Set-Content (Join-Path $probeRoot 'launch.json')
Write-Output "Started shutdown probe pid=$($probeProcess.Id) rendering=$Rendering display=$Display"
while(-not $probeProcess.HasExited){
    if(-not $probeReportAt -and (Test-Path -LiteralPath $probeReport)){$probeReportAt=[DateTime]::UtcNow;Write-Output 'Race report saved; observing normal shutdown.'}
    if($probeReportAt -and ([DateTime]::UtcNow-$probeReportAt).TotalSeconds -ge $GraceSeconds){$probeReason='Process remained live beyond post-report shutdown deadline.'}
    if(([DateTime]::UtcNow-$probeStarted).TotalSeconds -gt 360){$probeReason='Probe exceeded total runtime bound.'}
    if($probeReason){
        # A timeout does not mean the process exited. Recheck the exact live
        # process and its executable before deliberately cleaning up this probe.
        $probeProcess.Refresh();if($probeProcess.HasExited){break}
        $probeLive=Get-Process -Id $probeProcess.Id -ErrorAction Stop
        if($probeLive.Path -ne $probeExe){throw 'Probe identity changed; refusing cleanup.'}
        $probeForced=$true;Write-Output "$probeReason Cleaning up verified probe pid=$($probeLive.Id)."
        Stop-Process -Id $probeLive.Id -Force;break
    }
    Start-Sleep -Seconds 1;$probeProcess.Refresh()
}
$probeProcess.WaitForExit()
$probeResult=[pscustomobject]@{pid=$probeProcess.Id;executable=$probeExe;rendering=$Rendering;display=$Display;playbackRate=$PlaybackRate;normalExit=(-not $probeForced);exitCode=$probeProcess.ExitCode;forced=$probeForced;reason=$probeReason;totalSeconds=([DateTime]::UtcNow-$probeStarted).TotalSeconds;postReportSeconds=$(if($probeReportAt){([DateTime]::UtcNow-$probeReportAt).TotalSeconds}else{$null});reportExists=(Test-Path -LiteralPath $probeReport)}
$probeResult|ConvertTo-Json|Set-Content (Join-Path $probeRoot 'shutdown.json')
$probeResult|ConvertTo-Json
