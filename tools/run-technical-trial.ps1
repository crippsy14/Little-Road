param(
    [string]$Executable=(Join-Path $PSScriptRoot '../../little-road-ai-review/Little Road Unity.exe'),
    [string]$OutputDirectory,
    [ValidateSet('cedar','bayview','cedar-hills','hillcrest','quarry','bramblewick','vinelands','vinelands-long')][string]$Track='cedar',
    [ValidateSet('stock','street','club')][string]$Loadout='club',
    [ValidateSet('pack','line')][string]$Grid='pack',
    [ValidateRange(1,24)][int]$Cars=1,
    [ValidateRange(1,20)][int]$Laps=3,
    [ValidateRange(5,3600)][int]$Duration=300,
    [int]$Seed=1709,
    [ValidateSet('legacy','reference','racer','context')][string]$Policy='legacy',   # (context: old name for racer)
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
    [switch]$LightDrafting,
    [switch]$BayviewTransferStudy,
    [ValidateSet("neutral","mixed","cautious","assertive","daring","early","steady","variable")][string]$DriverRoster,
    [switch]$DriverForm,
    [switch]$RacePreview,
    [switch]$RacePreviewReview,
    [switch]$RaceHud,
    [switch]$RaceHudReview,
    [switch]$CleanView,
    [ValidateRange(12,60)][float]$TrafficGap=24,
    [ValidateRange(5,30)][float]$TrafficSpeed=18,
    [ValidateRange(5,35)][float]$TrafficRearSpeed=26,
    [string]$Opponents,
    [switch]$PassProcess,
    # Pass-through experiment flags (e.g. --path-aware-leader); recorded in launch metadata.
    [string[]]$ExtraArguments=@()
)
$ErrorActionPreference='Stop'
. "$PSScriptRoot/quiet-start.ps1"
$trialExecutable=(Resolve-Path -LiteralPath $Executable).Path
if($Manual -and -not $Visible){throw 'Recorded manual driving requires -Visible.'}
if($Manual -and $Cars -gt 1 -and $TrafficCase -ne 'field'){throw 'Manual multi-car comparisons require the normal field fixture.'}
if($Manual -and $PlaybackRate -ne 1){throw 'Human comparison laps must use real-time playback.'}
if($Fullscreen -and $Windowed){throw 'Choose fullscreen or windowed, not both.'}
if($BayviewTransferStudy -and ($Track -notin @('bayview','hillcrest','quarry','bramblewick','vinelands','vinelands-long') -or $Policy -notin @('legacy','racer','context') -or -not (($Cars -eq 1 -and $TrafficCase -eq 'standing-solo') -or ($Cars -eq 24 -and $TrafficCase -eq 'field')))){throw 'Invalid Bayview transfer fixture.'}
if($Policy -in 'racer','context' -and -not $BayviewTransferStudy -and ($Track -notin @('cedar','cedar-hills') -or $TrafficCase -notin @('draft-straight','draft-pullout','solo','standing-solo','pace-solo','rear-control','rear-control-left','rear-control-right','follow','pass','crossing-pass','brake-leader','brake-probe','queue','no-room','field','merge-left','merge-right','merge-witness-left','merge-witness-right','corner-solo-left','corner-solo-middle','corner-solo-right','corner-solo-deep','corner-pair-left','corner-pair-right','corner-three'))){throw 'Context prototype requires a supported explicit Cedar fixture.'}
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
# Visible runs play silently (--silent: audio runs and can be captured, nothing reaches the speakers).
if(-not $Visible){$trialArguments=@('-batchmode')+$trialArguments}else{$trialArguments=@('--silent')+$trialArguments}
$trialRate=if($PSBoundParameters.ContainsKey('PlaybackRate')){$PlaybackRate}elseif($Visible){1}else{4}
$trialArguments+=@('--trial-timescale',$trialRate.ToString([Globalization.CultureInfo]::InvariantCulture))
if($Captures){$trialArguments+=@('--trial-captures',('"'+$trialOutput+'"'))}
if($SharedSnapshot){$trialArguments+='--shared-driver-snapshot'}
if($PhysicalRecovery){$trialArguments+='--physical-recovery'}
if($RecoveryCase){$trialArguments+=@('--recovery-case',$RecoveryCase)}
if($TrafficCase){$trialArguments+=@('--traffic-case',$TrafficCase,'--traffic-gap',$TrafficGap.ToString([Globalization.CultureInfo]::InvariantCulture),'--traffic-speed',$TrafficSpeed.ToString([Globalization.CultureInfo]::InvariantCulture))}
if($TrafficCase -like 'merge-*' -or ($TrafficCase -like 'rear-control*' -and $PSBoundParameters.ContainsKey('TrafficRearSpeed'))){$trialArguments+=@('--traffic-rear-speed',$TrafficRearSpeed.ToString([Globalization.CultureInfo]::InvariantCulture))}
if($LightDrafting){$trialArguments+='--light-drafting'}
if($BayviewTransferStudy){$trialArguments+='--bayview-transfer-study'}
if($DriverRoster){$trialArguments+=@('--driver-roster',$DriverRoster)}
if($DriverForm){$trialArguments+='--driver-form'}
if($RacePreview){$trialArguments+='--race-preview'}
if($RacePreviewReview){$trialArguments+='--race-preview-review'}
if($RaceHud){$trialArguments+='--race-hud'}
if($RaceHudReview){$trialArguments+='--race-hud-review'}
if($CleanView){$trialArguments+='--clean-view'}
if($Opponents){$trialArguments+=@('--trial-opponents',$Opponents)}
if($Manual){$trialArguments+=@('--trial-human','--hide-racing-line')}
if($Fullscreen){$trialArguments+='--start-fullscreen'}
if($Windowed){$trialArguments+=@('--start-windowed','-screen-fullscreen','0','-monitor','1')}
if($DirectGraphics){$trialArguments+='-force-gfx-direct'}
$trialArguments+=@('--driver-policy',$Policy,'--racing-line',$RacingLine)
foreach($extra in $ExtraArguments){if($extra -notmatch '^--[a-z0-9-]+$' -and $extra -notmatch '^[A-Za-z0-9._-]+$'){throw "Unsafe extra argument: $extra"};$trialArguments+=$extra}
$trialStyle=if($Visible){'Normal'}else{'Hidden'}
$trialAssembly=Join-Path (Split-Path $trialExecutable) 'Little Road Unity_Data/Managed/Assembly-CSharp.dll'
$trialAssemblyHash=if(Test-Path -LiteralPath $trialAssembly){(Get-FileHash -LiteralPath $trialAssembly -Algorithm SHA256).Hash}else{$null}
$trialProcess=Start-Quiet -FilePath $trialExecutable -ArgumentList $trialArguments -WindowStyle $trialStyle -PassThru
$trialLaunch=[pscustomobject]@{pid=$trialProcess.Id;executable=$trialExecutable;assemblyHash=$trialAssemblyHash;arguments=$trialArguments;output=$trialOutput;visible=[bool]$Visible;requestedWidth=$Width;requestedHeight=$Height;windowed=[bool]$Windowed;directGraphics=[bool]$DirectGraphics;physicalRecovery=[bool]$PhysicalRecovery;trafficCase=$TrafficCase;trafficRearSpeed=$TrafficRearSpeed;recoveryCase=$RecoveryCase;opponents=$Opponents;duration=$trialDuration;track=$Track;loadout=$Loadout;grid=$Grid;cars=$Cars;laps=$Laps;seed=$Seed;policy=$Policy;racingLine=$RacingLine;playbackRate=$trialRate}
$trialLaunch | Add-Member -NotePropertyName lightDrafting -NotePropertyValue ([bool]$LightDrafting)
if($PassProcess){[pscustomobject]@{metadata=$trialLaunch;process=$trialProcess}}else{$trialLaunch|ConvertTo-Json}

