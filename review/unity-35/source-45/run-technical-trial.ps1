param(
    [string]$Executable=(Join-Path $PSScriptRoot '../../little-road-ai-review/Little Road Unity.exe'),
    [string]$OutputDirectory,
    [ValidateSet('cedar','bayview')][string]$Track='cedar',
    [ValidateSet('stock','street','club')][string]$Loadout='club',
    [ValidateSet('pack','line')][string]$Grid='pack',
    [ValidateRange(1,24)][int]$Cars=1,
    [ValidateRange(1,20)][int]$Laps=3,
    [ValidateRange(5,3600)][int]$Duration=300,
    [int]$Seed=1709,
    [ValidateSet('legacy','reference','traffic','context')][string]$Policy='legacy',
    [string]$RacingLine='cedar-mincurve-v1',
    [switch]$Visible,
    [ValidateRange(640,7680)][int]$Width=1600,
    [ValidateRange(480,4320)][int]$Height=900,
    [switch]$Manual,
    [switch]$Fullscreen,
    [switch]$Windowed,
    [switch]$DirectGraphics,
    [switch]$SharedSnapshot,
    [switch]$Captures,
    [ValidateRange(.25,4)][float]$PlaybackRate=1,
    [switch]$PhysicalRecovery,
    [string]$RecoveryCase,
    [string]$TrafficCase,
    [switch]$TrafficDiagnostics,
    [switch]$SweepContacts,
    [switch]$NoImmediateSafety,
    [switch]$NoRearAnticipation,
    [string]$MotionAudit,
    [string]$PathProbe,
    [ValidateRange(1,6)][int]$PathControlSteps=1,
    [switch]$PathAdaptiveEnvelope,
    [switch]$RoadFeasibility,
    [switch]$PreparedPath,
    [switch]$GuidePathFallback,
    [switch]$ContextPlanAudit,
    [switch]$TimedRearCorridors,
    [switch]$CoastWithinEnvelope,
    [switch]$PedalStageAudit,
    [ValidateSet('neutral','earlier','early')][string]$DriverBraking='neutral',
    [ValidateRange(0,10)][int]$DriverRisk=0,
    [switch]$NoGapCentres,
    [ValidateRange(0,24)][int]$ForecastWorkers=0,
    [ValidateRange(0,24)][int]$ParallelForecastWorkers=0,
    [string]$CapturePathApproach,
    [string]$PathApproachFile,
    [ValidateRange(0,4)][int]$PathApproachIndex=2,
    [ValidateRange(12,60)][float]$TrafficGap=24,
    [ValidateRange(5,30)][float]$TrafficSpeed=18,
    [ValidateRange(5,35)][float]$TrafficRearSpeed=26,
    [string]$Opponents,
    [switch]$PassProcess
)
$ErrorActionPreference='Stop'
$trialExecutable=(Resolve-Path -LiteralPath $Executable).Path
if($Manual -and (-not $Visible -or $Cars -ne 1)){throw 'Recorded manual laps require -Visible and one car.'}
if($Manual -and $PlaybackRate -ne 1){throw 'Human comparison laps must use real-time playback.'}
if($Fullscreen -and $Windowed){throw 'Choose fullscreen or windowed, not both.'}
if($Policy -eq 'traffic' -and ($Track -ne 'cedar' -or -not $TrafficCase)){throw 'Traffic policy requires Cedar and an explicit -TrafficCase (use field for a standing 24-car race).'}
if($Policy -eq 'context' -and ($Track -ne 'cedar' -or $TrafficCase -notin @('solo','pace-solo','rear-control','rear-control-left','rear-control-right','follow','pass','brake-leader','brake-probe','queue','no-room','field','merge-left','merge-right','merge-witness-left','merge-witness-right','corner-solo-left','corner-solo-middle','corner-solo-right','corner-pair-left','corner-pair-right','corner-three'))){throw 'Context prototype requires a supported explicit Cedar fixture.'}
if(-not $OutputDirectory){$OutputDirectory=Join-Path $PSScriptRoot ('../review/technical-runs/'+[DateTime]::Now.ToString('yyyyMMdd-HHmmss')+'-'+[Guid]::NewGuid().ToString('N').Substring(0,6))}
$trialOutput=[IO.Path]::GetFullPath($OutputDirectory)
if(Test-Path -LiteralPath $trialOutput){throw 'Choose a new evidence folder.'}
New-Item -ItemType Directory -Path $trialOutput | Out-Null
$trialMode=if($Manual){'--trial-race'}else{'--trial-auto'}
$trialDuration=if($PSBoundParameters.ContainsKey('Duration')){$Duration}else{[Math]::Max(300,$Laps*180)}
$trialArguments=@($trialMode,'--trial-track',$Track,'--trial-setup',$Loadout,'--trial-cars',$Cars.ToString(),'--trial-laps',$Laps.ToString(),
    '--trial-seed',$Seed.ToString(),'--trial-grid',$Grid,'--trial-duration',$trialDuration.ToString(),'--trial-report',('"'+(Join-Path $trialOutput 'race.json')+'"'),
    '-logFile',('"'+(Join-Path $trialOutput 'run.log')+'"'),'-screen-width',$Width.ToString(),'-screen-height',$Height.ToString())
if(-not $Visible){$trialArguments=@('-batchmode')+$trialArguments}
$trialRate=if($PSBoundParameters.ContainsKey('PlaybackRate')){$PlaybackRate}elseif($Visible){1}else{4}
$trialArguments+=@('--trial-timescale',$trialRate.ToString([Globalization.CultureInfo]::InvariantCulture))
if($Captures){$trialArguments+=@('--trial-captures',('"'+$trialOutput+'"'))}
if($SharedSnapshot){$trialArguments+='--shared-driver-snapshot'}
if($PhysicalRecovery){$trialArguments+='--physical-recovery'}
if($RecoveryCase){$trialArguments+=@('--recovery-case',$RecoveryCase)}
if($TrafficCase){$trialArguments+=@('--traffic-case',$TrafficCase,'--traffic-gap',$TrafficGap.ToString([Globalization.CultureInfo]::InvariantCulture),'--traffic-speed',$TrafficSpeed.ToString([Globalization.CultureInfo]::InvariantCulture))}
if($TrafficCase -like 'merge-*' -or ($TrafficCase -like 'rear-control*' -and $PSBoundParameters.ContainsKey('TrafficRearSpeed'))){$trialArguments+=@('--traffic-rear-speed',$TrafficRearSpeed.ToString([Globalization.CultureInfo]::InvariantCulture))}
if($TrafficDiagnostics){$trialArguments+='--traffic-diagnostics'}
if($SweepContacts){$trialArguments+='--sweep-contacts'}
if($NoImmediateSafety){$trialArguments+='--no-immediate-safety'}
if($NoRearAnticipation){$trialArguments+='--no-rear-anticipation'}
if($MotionAudit){$trialArguments+=@('--motion-audit',$MotionAudit)}
if($PathProbe){$trialArguments+=@('--path-probe',$PathProbe,'--path-control-steps',$PathControlSteps.ToString())}
if($PathAdaptiveEnvelope){$trialArguments+='--path-adaptive-envelope'}
if($RoadFeasibility){$trialArguments+='--road-feasibility'}
if($PreparedPath){$trialArguments+='--prepared-path'}
if($GuidePathFallback){$trialArguments+='--guide-path-fallback'}
if($ContextPlanAudit){$trialArguments+='--context-plan-audit'}
if($TimedRearCorridors){$trialArguments+='--timed-rear-corridors'}
if($CoastWithinEnvelope){$trialArguments+='--coast-within-envelope'}
if($PedalStageAudit){$trialArguments+='--pedal-stage-audit'}
$trialArguments+=@('--driver-braking',$DriverBraking,'--driver-risk',$DriverRisk.ToString())
if($NoGapCentres){$trialArguments+='--no-gap-centres'}
if($ParallelForecastWorkers -gt 0){$trialArguments+=@('--parallel-road-forecasts','--parallel-forecast-workers',$ParallelForecastWorkers.ToString())}
if($ForecastWorkers -gt 0){$trialArguments+=@('--forecast-batch-probe','--forecast-batch-workers',$ForecastWorkers.ToString())}
if($CapturePathApproach){$trialArguments+=@('--capture-path-approach',$CapturePathApproach)}
if($PathApproachFile){$trialArguments+=@('--path-approach-file',('"'+(Resolve-Path -LiteralPath $PathApproachFile).Path+'"'),'--path-approach-index',$PathApproachIndex.ToString())}
if($Opponents){$trialArguments+=@('--trial-opponents',$Opponents)}
if($Manual){$trialArguments+=@('--trial-human','--hide-racing-line')}
if($Fullscreen){$trialArguments+='--start-fullscreen'}
if($Windowed){$trialArguments+=@('--start-windowed','-screen-fullscreen','0')}
if($DirectGraphics){$trialArguments+='-force-gfx-direct'}
$trialArguments+=@('--driver-policy',$Policy,'--racing-line',$RacingLine)
$trialStyle=if($Visible){'Normal'}else{'Hidden'}
$trialAssembly=Join-Path (Split-Path $trialExecutable) 'Little Road Unity_Data/Managed/Assembly-CSharp.dll'
$trialAssemblyHash=if(Test-Path -LiteralPath $trialAssembly){(Get-FileHash -LiteralPath $trialAssembly -Algorithm SHA256).Hash}else{$null}
$trialProcess=Start-Process -FilePath $trialExecutable -ArgumentList $trialArguments -WindowStyle $trialStyle -PassThru
$trialLaunch=[pscustomobject]@{pid=$trialProcess.Id;executable=$trialExecutable;assemblyHash=$trialAssemblyHash;arguments=$trialArguments;output=$trialOutput;visible=[bool]$Visible;requestedWidth=$Width;requestedHeight=$Height;windowed=[bool]$Windowed;directGraphics=[bool]$DirectGraphics;trafficDiagnostics=[bool]$TrafficDiagnostics;sweepContacts=[bool]$SweepContacts;noImmediateSafety=[bool]$NoImmediateSafety;noRearAnticipation=[bool]$NoRearAnticipation;motionAudit=$MotionAudit;physicalRecovery=[bool]$PhysicalRecovery;trafficCase=$TrafficCase;trafficRearSpeed=$TrafficRearSpeed;recoveryCase=$RecoveryCase;opponents=$Opponents;duration=$trialDuration;track=$Track;loadout=$Loadout;grid=$Grid;cars=$Cars;laps=$Laps;seed=$Seed;policy=$Policy;racingLine=$RacingLine;playbackRate=$trialRate}
$trialLaunch | Add-Member -NotePropertyName roadFeasibility -NotePropertyValue ([bool]$RoadFeasibility)
$trialLaunch | Add-Member -NotePropertyName preparedPath -NotePropertyValue ([bool]$PreparedPath)
$trialLaunch | Add-Member -NotePropertyName forecastWorkers -NotePropertyValue $ForecastWorkers
$trialLaunch | Add-Member -NotePropertyName parallelForecastWorkers -NotePropertyValue $ParallelForecastWorkers
$trialLaunch | Add-Member -NotePropertyName guidePathFallback -NotePropertyValue ([bool]$GuidePathFallback)
$trialLaunch | Add-Member -NotePropertyName contextPlanAudit -NotePropertyValue ([bool]$ContextPlanAudit)
$trialLaunch | Add-Member -NotePropertyName timedRearCorridors -NotePropertyValue ([bool]$TimedRearCorridors)
$trialLaunch | Add-Member -NotePropertyName coastWithinEnvelope -NotePropertyValue ([bool]$CoastWithinEnvelope)
$trialLaunch | Add-Member -NotePropertyName pedalStageAudit -NotePropertyValue ([bool]$PedalStageAudit)
$trialLaunch | Add-Member -NotePropertyName driverBraking -NotePropertyValue $DriverBraking
$trialLaunch | Add-Member -NotePropertyName driverRisk -NotePropertyValue $DriverRisk
$trialLaunch | Add-Member -NotePropertyName noGapCentres -NotePropertyValue ([bool]$NoGapCentres)
if($PassProcess){[pscustomobject]@{metadata=$trialLaunch;process=$trialProcess}}else{$trialLaunch|ConvertTo-Json}

