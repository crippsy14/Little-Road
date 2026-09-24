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
    [switch]$RearPressureFilter,
    [switch]$LightDrafting,
    [switch]$OpenSpacePriority,
    [switch]$IntegratedPassingProgress,
    [switch]$PathSpeedEnvelope,
    [string]$MotionAudit,
    [string]$PathProbe,
    [ValidateRange(1,6)][int]$PathControlSteps=1,
    [switch]$PathAdaptiveEnvelope,
    [switch]$RoadFeasibility,
    [switch]$PreparedPath,
    [switch]$GuidePathFallback,
    [switch]$ContextPlanAudit,
    [switch]$ContextSwitchAudit,
    [switch]$CompareContinuation,
    [switch]$ContinuationAudit,
    [switch]$OpeningDecisionAudit,
    [switch]$TrafficAwareProgress,
    [switch]$RetainPassTrajectory,
    [switch]$ComparePassCompletion,
    [switch]$FollowingFeasibilityFallback,
    [ValidateSet("all","car0","rivals")][string]$FollowingFallbackScope="all",
    [switch]$FollowingFallbackAudit,
    [switch]$SharedObservedTraffic,
    [switch]$ParallelContinuationForecast,
    [switch]$TimedRearCorridors,
    [switch]$CoastWithinEnvelope,
    [switch]$PedalStageAudit,
    [ValidateSet('neutral','earlier','early')][string]$DriverBraking='neutral',
    [ValidateRange(0,10)][int]$DriverRisk=0,
    [switch]$NoGapCentres,
    [switch]$TrafficPathCheck,
    [switch]$AnticipatePassingSpace,
    [switch]$AnticipateBlockingCar,
    [switch]$TrafficCoastContinuation,
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
if($ParallelContinuationForecast -and (-not $CompareContinuation -or $ParallelForecastWorkers -eq 0)){throw 'Parallel continuation requires continuation comparison and forecast workers.'}
if($ComparePassCompletion -and (-not $TrafficAwareProgress -or -not $CompareContinuation)){throw 'Pass completion requires traffic-aware continuation.'}
if(-not $FollowingFeasibilityFallback -and $FollowingFallbackScope -ne 'all'){throw 'Component scope requires following fallback enabled.'}
if($FollowingFeasibilityFallback -and -not $RoadFeasibility){throw 'Following fallback requires physical road checks.'}
if($RetainPassTrajectory -and -not $RoadFeasibility){throw 'Retained trajectories require physical road checks.'}
if($TrafficAwareProgress -and -not $CompareContinuation){throw 'Traffic-aware progress requires continuation comparison.'}
$trialExecutable=(Resolve-Path -LiteralPath $Executable).Path
if($Manual -and -not $Visible){throw 'Recorded manual driving requires -Visible.'}
if($Manual -and $Cars -gt 1 -and $TrafficCase -ne 'field'){throw 'Manual multi-car comparisons require the normal field fixture.'}
if($Manual -and $PlaybackRate -ne 1){throw 'Human comparison laps must use real-time playback.'}
if($Fullscreen -and $Windowed){throw 'Choose fullscreen or windowed, not both.'}
if($Policy -eq 'traffic' -and ($Track -ne 'cedar' -or -not $TrafficCase)){throw 'Traffic policy requires Cedar and an explicit -TrafficCase (use field for a standing 24-car race).'}
if($Policy -eq 'context' -and ($Track -ne 'cedar' -or $TrafficCase -notin @('draft-straight','draft-pullout','solo','standing-solo','pace-solo','rear-control','rear-control-left','rear-control-right','follow','pass','crossing-pass','brake-leader','brake-probe','queue','no-room','field','merge-left','merge-right','merge-witness-left','merge-witness-right','corner-solo-left','corner-solo-middle','corner-solo-right','corner-pair-left','corner-pair-right','corner-three'))){throw 'Context prototype requires a supported explicit Cedar fixture.'}
if($TrafficCase -eq 'standing-solo' -and $Cars -ne 1){throw 'Standing solo requires one car in the24-car player grid slot.'}
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
if($RearPressureFilter){$trialArguments+='--rear-pressure-filter'}
if($LightDrafting){$trialArguments+='--light-drafting'}
if($OpenSpacePriority){$trialArguments+='--open-space-priority'}
if($IntegratedPassingProgress){$trialArguments+='--integrated-passing-progress'}
if($PathSpeedEnvelope){$trialArguments+='--path-speed-envelope'}
if($MotionAudit){$trialArguments+=@('--motion-audit',$MotionAudit)}
if($PathProbe){$trialArguments+=@('--path-probe',$PathProbe,'--path-control-steps',$PathControlSteps.ToString())}
if($PathAdaptiveEnvelope){$trialArguments+='--path-adaptive-envelope'}
if($RoadFeasibility){$trialArguments+='--road-feasibility'}
if($PreparedPath){$trialArguments+='--prepared-path'}
if($GuidePathFallback){$trialArguments+='--guide-path-fallback'}
if($ContextPlanAudit){$trialArguments+='--context-plan-audit'}
if($ContextSwitchAudit){$trialArguments+='--context-switch-audit'}
if($CompareContinuation){$trialArguments+='--compare-continuation'}
if($ContinuationAudit){$trialArguments+='--continuation-audit'}
if($OpeningDecisionAudit){$trialArguments+='--opening-decision-audit'}
if($TrafficAwareProgress){$trialArguments+='--traffic-aware-progress'}
if($RetainPassTrajectory){$trialArguments+='--retain-pass-trajectory'}
if($ComparePassCompletion){$trialArguments+='--compare-pass-completion'}
if($FollowingFeasibilityFallback){$trialArguments+='--following-feasibility-fallback'}
$trialArguments+=@('--following-fallback-scope',$FollowingFallbackScope)
if($FollowingFallbackAudit){$trialArguments+='--following-fallback-audit'}
if($SharedObservedTraffic){$trialArguments+='--shared-observed-traffic'}
if($ParallelContinuationForecast){$trialArguments+='--parallel-continuation-forecast'}
if($TimedRearCorridors){$trialArguments+='--timed-rear-corridors'}
if($CoastWithinEnvelope){$trialArguments+='--coast-within-envelope'}
if($PedalStageAudit){$trialArguments+='--pedal-stage-audit'}
$trialArguments+=@('--driver-braking',$DriverBraking,'--driver-risk',$DriverRisk.ToString())
if($NoGapCentres){$trialArguments+='--no-gap-centres'}
if($TrafficPathCheck){$trialArguments+='--traffic-path-check'}
if($AnticipatePassingSpace){$trialArguments+='--anticipate-passing-space'}
if($AnticipateBlockingCar){$trialArguments+='--anticipate-blocking-car'}
if($TrafficCoastContinuation){$trialArguments+='--traffic-coast-continuation'}
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
$trialLaunch | Add-Member -NotePropertyName openSpacePriority -NotePropertyValue ([bool]$OpenSpacePriority)
$trialLaunch | Add-Member -NotePropertyName integratedPassingProgress -NotePropertyValue ([bool]$IntegratedPassingProgress)
$trialLaunch | Add-Member -NotePropertyName pathSpeedEnvelope -NotePropertyValue ([bool]$PathSpeedEnvelope)
$trialLaunch | Add-Member -NotePropertyName lightDrafting -NotePropertyValue ([bool]$LightDrafting)
$trialLaunch | Add-Member -NotePropertyName rearPressureFilter -NotePropertyValue ([bool]$RearPressureFilter)
$trialLaunch | Add-Member -NotePropertyName roadFeasibility -NotePropertyValue ([bool]$RoadFeasibility)
$trialLaunch | Add-Member -NotePropertyName preparedPath -NotePropertyValue ([bool]$PreparedPath)
$trialLaunch | Add-Member -NotePropertyName forecastWorkers -NotePropertyValue $ForecastWorkers
$trialLaunch | Add-Member -NotePropertyName parallelForecastWorkers -NotePropertyValue $ParallelForecastWorkers
$trialLaunch | Add-Member -NotePropertyName guidePathFallback -NotePropertyValue ([bool]$GuidePathFallback)
$trialLaunch | Add-Member -NotePropertyName contextPlanAudit -NotePropertyValue ([bool]$ContextPlanAudit)
$trialLaunch | Add-Member -NotePropertyName contextSwitchAudit -NotePropertyValue ([bool]$ContextSwitchAudit)
$trialLaunch | Add-Member -NotePropertyName compareContinuation -NotePropertyValue ([bool]$CompareContinuation)
$trialLaunch | Add-Member -NotePropertyName continuationAudit -NotePropertyValue ([bool]$ContinuationAudit)
$trialLaunch | Add-Member -NotePropertyName openingDecisionAudit -NotePropertyValue ([bool]$OpeningDecisionAudit)
$trialLaunch | Add-Member -NotePropertyName trafficAwareProgress -NotePropertyValue ([bool]$TrafficAwareProgress)
$trialLaunch | Add-Member -NotePropertyName retainPassTrajectory -NotePropertyValue ([bool]$RetainPassTrajectory)
$trialLaunch | Add-Member -NotePropertyName comparePassCompletion -NotePropertyValue ([bool]$ComparePassCompletion)
$trialLaunch | Add-Member -NotePropertyName followingFeasibilityFallback -NotePropertyValue ([bool]$FollowingFeasibilityFallback)
$trialLaunch | Add-Member -NotePropertyName followingFallbackScope -NotePropertyValue $FollowingFallbackScope
$trialLaunch | Add-Member -NotePropertyName followingFallbackAudit -NotePropertyValue ([bool]$FollowingFallbackAudit)
$trialLaunch | Add-Member -NotePropertyName parallelContinuationForecast -NotePropertyValue ([bool]$ParallelContinuationForecast)
$trialLaunch | Add-Member -NotePropertyName sharedObservedTraffic -NotePropertyValue ([bool]$SharedObservedTraffic)
$trialLaunch | Add-Member -NotePropertyName timedRearCorridors -NotePropertyValue ([bool]$TimedRearCorridors)
$trialLaunch | Add-Member -NotePropertyName coastWithinEnvelope -NotePropertyValue ([bool]$CoastWithinEnvelope)
$trialLaunch | Add-Member -NotePropertyName pedalStageAudit -NotePropertyValue ([bool]$PedalStageAudit)
$trialLaunch | Add-Member -NotePropertyName driverBraking -NotePropertyValue $DriverBraking
$trialLaunch | Add-Member -NotePropertyName driverRisk -NotePropertyValue $DriverRisk
$trialLaunch | Add-Member -NotePropertyName noGapCentres -NotePropertyValue ([bool]$NoGapCentres)
$trialLaunch | Add-Member -NotePropertyName trafficPathCheck -NotePropertyValue ([bool]$TrafficPathCheck)
$trialLaunch | Add-Member -NotePropertyName anticipatePassingSpace -NotePropertyValue ([bool]$AnticipatePassingSpace)
$trialLaunch | Add-Member -NotePropertyName anticipateBlockingCar -NotePropertyValue ([bool]$AnticipateBlockingCar)
$trialLaunch | Add-Member -NotePropertyName trafficCoastContinuation -NotePropertyValue ([bool]$TrafficCoastContinuation)
if($PassProcess){[pscustomobject]@{metadata=$trialLaunch;process=$trialProcess}}else{$trialLaunch|ConvertTo-Json}

