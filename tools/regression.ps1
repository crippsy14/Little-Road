# Usage (from project root): .\tools\regression.ps1 -Name sr7
# The full regression on the dev build, in parallel batches of up to five games (each test has its own folder and save, so
# they can't collide; Richard's game keeps the focus: quiet-start). The sound smoke runs last, on its own, because it records
# audio. Prints one line per test. Run names are <Name>-<test>; never reuses a run.
param([Parameter(Mandatory)][ValidatePattern('^[a-z0-9-]+$')][string]$Name)
$ErrorActionPreference='Continue'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
function Run-Batch($jobs){
  $started=foreach($j in $jobs){Start-Job -Name $j.name -ArgumentList $root,$j.script -ScriptBlock {param($r,$s)Set-ExecutionPolicy -Scope Process Bypass -Force;Set-Location $r;& ([scriptblock]::Create($s))}}
  $started|Wait-Job|Out-Null
  foreach($j in $started){Receive-Job $j|ForEach-Object{"$($j.Name): $_"};Remove-Job $j}
}
$smoke='| Select-String "SMOKE_PASSED|SMOKE_FAILED|Exception" | Select-Object -First 2 | ForEach-Object Line'
$test='2>&1 | Select-String "_TEST |Exception" | Select-Object -First 2 | ForEach-Object Line'
Run-Batch @(
  @{name='smoke';script=".\tools\capture-adventure.ps1 -Name $Name-smoke 2>&1 $smoke"},
  @{name='smoke-noqh';script=".\tools\capture-adventure.ps1 -Name $Name-smoke-noqh -Flags @('--no-quarry-hills') 2>&1 $smoke"},
  @{name='lesson';script=".\tools\quarry-run.ps1 -Name $Name-lesson -NoReview -Flags @('--lesson-test') $test"},
  @{name='climb';script=".\tools\quarry-run.ps1 -Name $Name-climb -NoReview -Flags @('--climb-test') $test"},
  @{name='sprint';script=".\tools\quarry-run.ps1 -Name $Name-sprint -NoReview -Flags @('--sprint-test') $test"})
Run-Batch @(
  @{name='carving';script=".\tools\quarry-run.ps1 -Name $Name-carving -NoReview -Flags @('--carving-test') $test"},
  @{name='blondin';script=".\tools\quarry-run.ps1 -Name $Name-blondin -NoReview -Flags @('--blondin-test') $test"},
  @{name='wayfinding';script=".\tools\quarry-run.ps1 -Name $Name-wayfinding -NoReview -Flags @('--wayfinding-test') $test"},
  @{name='wayfinding-noqh';script=".\tools\quarry-run.ps1 -Name $Name-way-noqh -NoReview -Flags @('--wayfinding-test','--no-quarry-hills') $test"},
  @{name='world-tiles';script=".\tools\quarry-run.ps1 -Name $Name-world-tiles -NoReview -Flags @('--world-tiles-test') $test"})
Run-Batch @(
  @{name='festival';script=".\tools\quarry-run.ps1 -Name $Name-festival -NoReview -Flags @('--festival','--festival-test') $test"},
  @{name='area-audit';script=".\tools\quarry-run.ps1 -Name $Name-area-audit -Flags @('--area-audit') 2>&1 | Out-Null; Get-Content review/quarry-hills/$Name-area-audit/area-audit.txt | Select-String 'FAIL|PASS|^    ' | ForEach-Object Line"})
# The sound smoke, alone: player-engine.wav RMS (about 6890 since 30 Sep).
. "$PSScriptRoot/quiet-start.ps1"
$exe=Join-Path $root '../little-road-dev-build/Little Road Unity.exe';$f=Join-Path $root "review/sound/$Name-smoke";New-Item -ItemType Directory -Force $f|Out-Null
$p=Start-Quiet -FilePath $exe -ArgumentList @('-monitor','1','--start-windowed','-screen-width','640','-screen-height','360','-screen-fullscreen','0','--trial-track','test-loop','--trial-cars','1','--trial-laps','10','--trial-duration','60','--trial-auto','--sound-drive','--audio-capture',('"'+(Join-Path $f 'player.wav')+'"'),'--audio-capture-seconds','60','--audio-capture-silent','-logFile',('"'+(Join-Path $f 'player.log')+'"'))
if(-not $p.WaitForExit(200000)){$p.Kill()}
& "C:\Program Files\Blender Foundation\Blender 5.2\5.2\python\bin\python.exe" -c "import wave,struct,math,sys;w=wave.open(sys.argv[1]);n=w.getnframes();d=w.readframes(n);s=struct.unpack('<%dh'%(len(d)//2),d);print('sound smoke rms',round(math.sqrt(sum(x*x for x in s)/max(1,len(s))),1))" (Join-Path $f 'player-engine.wav')
