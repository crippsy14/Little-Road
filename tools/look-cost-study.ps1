# Usage (from project root): .\tools\look-cost-study.ps1 -Name p1
# Frame-cost A/B on the current dev build: rendered --valley-views cost-only runs, the baseline
# twice (noise) and once with each --perf-* switch. Writes review/map-sweep/<Name>-<case>/.
param([Parameter(Mandatory)][ValidatePattern('^[a-z0-9-]+$')][string]$Name,[string[]]$Only)
$ErrorActionPreference='Stop'
$cases=[ordered]@{'base-a'=@();'no-ssao'=@('--perf-no-ssao');'shadows-110'=@('--perf-shadows-110');'plain-kit'=@('--perf-plain-kit');'no-clouds'=@('--perf-no-clouds');'sparse-woods'=@('--perf-sparse-woods');'lit-leaves'=@('--perf-lit-leaves');'base-b'=@()}
foreach($c in $cases.Keys){
  if($Only -and $c -notin $Only){continue}
  & (Join-Path $PSScriptRoot 'capture-map.ps1') -Name "$Name-$c" -Rendered -Flags (@('--valley-views','--valley-cost-only')+$cases[$c]) | Out-Null
  $cost=Get-Content (Join-Path $PSScriptRoot "../review/map-sweep/$Name-$c/valley-frame-cost.txt") -ErrorAction SilentlyContinue
  "$c | "+(($cost|ForEach-Object{($_ -split ' ')[0]+' '+(($_ -split ' ')[1] -replace 'mean=','')})-join ', ')
}
