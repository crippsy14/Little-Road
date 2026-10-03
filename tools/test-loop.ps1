# Usage (from project root): .\tools\test-loop.ps1 [-Kit club] [-Auto] [-Minutes 3] [-Capture name]
# The 2.5-mile test loop (two 1-mile straights, two wide turns): gearing, top speed, limiter and engine sound up to
# class E. You drive (or -Auto lets the AI drive); the run ends after -Minutes. -Kit is a test kit (stock, street,
# club, valley) or an exact list like "engine=engine_club,turbo=turbo_street" over the stock car.
# -Capture writes the player's engine sound and revs to review/tmp/<name>/player.wav and .csv.
param([string]$Kit='club',[switch]$Auto,[double]$Minutes=3,[string]$Capture='')
$ErrorActionPreference='Stop'
. "$PSScriptRoot/quiet-start.ps1"
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$exe=Join-Path $root '..\little-road-dev-build\Little Road Unity.exe'
$args=@('-monitor','1','-screen-width','1920','-screen-height','1080','-screen-fullscreen','0','--trial-track','test-loop','--trial-setup',$Kit,
  '--trial-cars','1','--trial-laps','10','--trial-duration',[string][int]($Minutes*60),'--race-hud')
if($Auto){$args+='--trial-auto'}
if($Capture){$folder=Join-Path $root "review\tmp\$Capture";New-Item -ItemType Directory -Force $folder|Out-Null;$args+=@('--audio-capture',"`"$folder\player.wav`"")}
Start-Quiet -FilePath $exe -ArgumentList $args
