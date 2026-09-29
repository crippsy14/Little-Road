# Usage (from project root): .\tools\cleanup.ps1           dry run: lists what would go and how much
#                            .\tools\cleanup.ps1 -Apply    sends it to the Recycle Bin (recoverable)
# Prunes generated evidence under review/ so it never piles up into gigabytes. It never touches a
# git-tracked file, an image linked from any .md/.txt in the repo, or the newest -KeepRuns capture
# runs of each series. Rules (all untracked files only):
#   - screenshots in capture runs older than the newest -KeepRuns of their series (parent folder +
#     run-name letter prefix, e.g. map-sweep/v*, map-sweep/m*)
#   - logs older than -LogDays, and any log bigger than -BigLogMB (runaway error spam)
#   - per-tick race telemetry (*.track.json etc.) older than -TelemetryDays
#   - racing AI study folders beyond the newest -KeepStudies; smoke/UI runs beyond the newest 6
#   - editor-check scratch profiles; review/tmp files older than -ScratchDays
#   - empty folders left behind
# build-dev.ps1 runs it with -Apply -Quiet after every successful build.
param([switch]$Apply,[switch]$Quiet,[int]$KeepRuns=3,[int]$LogDays=7,[int]$BigLogMB=50,[int]$TelemetryDays=1,[int]$KeepStudies=2,[int]$ScratchDays=3)
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'));$review=Join-Path $root 'review'
function Rel([string]$path){$path.Substring($root.Length+1) -replace '\\','/'}
$tracked=New-Object 'System.Collections.Generic.HashSet[string]'
foreach($f in (git -c safe.directory=$root -C $root ls-files review)){[void]$tracked.Add($f)}
# Images mentioned in any document stay (matched by their path tail, so relative links count).
$refs=New-Object 'System.Collections.Generic.List[string]'
Get-ChildItem $root -Recurse -File -Include *.md,*.txt -ErrorAction SilentlyContinue |
  Where-Object { $_.FullName -notmatch '\\(Library|\.git|Temp)\\' -and $_.Length -lt 4MB } |
  ForEach-Object { foreach($m in [regex]::Matches([IO.File]::ReadAllText($_.FullName),'[\w\-./\\]+\.(png|jpe?g)')){$refs.Add(($m.Value -replace '\\','/').TrimStart('.','/').ToLowerInvariant())} }
function Referenced([string]$rel){$r=$rel.ToLowerInvariant();foreach($x in $refs){if($r.EndsWith($x)){return $true}};return $false}
$now=Get-Date;$items=New-Object System.Collections.ArrayList
function Add($file,[string]$why){$rel=Rel $file.FullName;if($tracked.Contains($rel)){return};[void]$items.Add([pscustomobject]@{Path=$file.FullName;Rel=$rel;MB=$file.Length/1MB;Why=$why})}
$files=@(Get-ChildItem $review -Recurse -File -Force)
foreach($f in $files) {
  $age=($now-$f.LastWriteTime).TotalDays
  if($f.Extension -eq '.log'){if($f.Length -gt $BigLogMB*1MB){Add $f 'log over size'}elseif($age -gt $LogDays){Add $f 'old log'}}
  elseif($f.Name -match '\.(track|driver-frames|traffic|motion-prediction|path-prediction)\.json$' -and $age -gt $TelemetryDays){Add $f 'per-tick telemetry'}
}
# Racing AI studies (review/unity-35/frame-cost-N): keep the newest -KeepStudies studies whole; the results
# of older ones are written up in docs (docs/46 for the v2 AI).
$studies=@(Get-ChildItem (Join-Path $review 'unity-35') -Directory -Filter 'frame-cost-*' -ErrorAction SilentlyContinue | Sort-Object LastWriteTime -Descending)
foreach($study in ($studies | Select-Object -Skip $KeepStudies)){foreach($f in (Get-ChildItem $study.FullName -Recurse -File -Force)){Add $f 'older AI study run'}}
# Adventure/UI smoke runs (review/ui-screens/<run>): keep the newest -KeepRuns*2 runs.
$screens=@(Get-ChildItem (Join-Path $review 'ui-screens') -Directory -ErrorAction SilentlyContinue | Sort-Object LastWriteTime -Descending)
foreach($run in ($screens | Select-Object -Skip ($KeepRuns*2))){foreach($f in (Get-ChildItem $run.FullName -Recurse -File -Force)){if(-not (Referenced (Rel $f.FullName))){Add $f 'older smoke/UI run'}}}
# Editor-check scratch profiles (older builds left one per build) and scratch files older than -ScratchDays.
foreach($f in $files){
  if((Rel $f.FullName) -match '^review/unity-04/(bodies|profiles)-[0-9a-f]{32}/'){Add $f 'editor-check scratch'}
  elseif((Rel $f.FullName) -match '^review/tmp/' -and ($now-$f.LastWriteTime).TotalDays -gt $ScratchDays){Add $f 'old scratch (review/tmp)'}
}
# Capture runs: folders holding images. A series is the parent folder plus the run name's letter
# prefix (map-sweep/v12-..., v13-... are one series; m21-... another); keep the newest few of each.
$images=$files | Where-Object { $_.Extension -match '^\.(png|jpe?g)$' }
foreach($series in ($images | Group-Object { $_.Directory.Parent.FullName+'|'+([regex]::Match($_.Directory.Name,'^[a-z]*')).Value })) {
  $runs=$series.Group | Group-Object DirectoryName | Sort-Object { ($_.Group | Sort-Object LastWriteTime | Select-Object -Last 1).LastWriteTime } -Descending
  foreach($run in ($runs | Select-Object -Skip $KeepRuns)){foreach($f in $run.Group){if(-not (Referenced (Rel $f.FullName))){Add $f 'screenshot in an older capture run'}}}
}
$total=($items | Measure-Object MB -Sum).Sum
if(-not $Quiet){$items | Group-Object Why | ForEach-Object { '{0,-36} {1,6} files {2,9:N1} MB' -f $_.Name,$_.Count,($_.Group | Measure-Object MB -Sum).Sum }}
if($Apply -and $items.Count -gt 0) {
  Add-Type -AssemblyName Microsoft.VisualBasic
  foreach($i in $items){[Microsoft.VisualBasic.FileIO.FileSystem]::DeleteFile($i.Path,'OnlyErrorDialogs','SendToRecycleBin')}
  # Folders emptied by the pruning (deepest first).
  Get-ChildItem $review -Recurse -Directory -Force | Sort-Object { $_.FullName.Length } -Descending |
    Where-Object { -not (Get-ChildItem $_.FullName -Force | Select-Object -First 1) } |
    ForEach-Object { [Microsoft.VisualBasic.FileIO.FileSystem]::DeleteDirectory($_.FullName,'OnlyErrorDialogs','SendToRecycleBin') }
}
'LITTLE_ROAD_CLEANUP files={0} MB={1:N0} applied={2}' -f $items.Count,$total,[bool]$Apply
