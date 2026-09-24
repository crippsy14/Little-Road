# Usage (from project root): .\tools\run-pace-study.ps1 -Version 112 -Study 112 -Label p2 -Flags @('--path-aware-leader')
param([int]$Version,[int]$Study,[string]$Label,[string[]]$Flags=@(),[string[]]$Tracks=@('cedar','bayview'),[int[]]$Seeds=@(1709,1710,1711),[switch]$SkipBaseline)
$arms=@(); if(-not $SkipBaseline){$arms+=,@('off',@())}; $arms+=,@($Label,$Flags)
foreach($track in $Tracks){foreach($seed in $Seeds){foreach($arm in $arms){
  $name="$track-s$seed-$($arm[0])"
  $folder=Join-Path (Get-Location) "review/unity-35/frame-cost-$Study/$name"
  if(Test-Path $folder){Write-Output "skip existing $name";continue}
  $t=Get-Date
  try{
    $runArgs=@{Version=$Version;Study=$Study;Track=$track;Seed=$seed;Name=$name;StaggerContextPlans=$true;ContextPlanGroups=3}
    if($arm[1].Count -gt 0){$runArgs.Extra=$arm[1]}
    & .\tools\run-frame-cost.ps1 @runArgs | Out-String | Write-Output
  }catch{Write-Output "FAILED $name : $($_.Exception.Message)"}
  Write-Output ("{0} took {1:n0}s" -f $name,((Get-Date)-$t).TotalSeconds)
}}}
