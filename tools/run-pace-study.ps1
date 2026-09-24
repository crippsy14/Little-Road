# Usage (from project root): .\tools\run-pace-study.ps1 -Study 116 -Label x1 -Flags @('--my-flag')
# Runs the dev build (tools/build-dev.ps1) unless -Version N is given. -KeepTelemetry keeps per-tick files.
# Screen a variation first on one track/seed: add -Tracks @('cedar') -Seeds @(1709)
param([int]$Version=0,[int]$Study,[string]$Label,[string[]]$Flags=@(),[string[]]$Tracks=@('cedar','bayview'),[int[]]$Seeds=@(1709,1710,1711),[switch]$SkipBaseline,[switch]$KeepTelemetry)
$arms=@(); if(-not $SkipBaseline){$arms+=,@('off',@())}; $arms+=,@($Label,$Flags)
foreach($track in $Tracks){foreach($seed in $Seeds){foreach($arm in $arms){
  $name="$track-s$seed-$($arm[0])"
  $folder=Join-Path (Get-Location) "review/unity-35/frame-cost-$Study/$name"
  if(Test-Path $folder){Write-Output "skip existing $name";continue}
  $t=Get-Date
  try{
    $runArgs=@{Version=$Version;Study=$Study;Track=$track;Seed=$seed;Name=$name}
    if($arm[1].Count -gt 0){$runArgs.Extra=$arm[1]}
    if($KeepTelemetry){$runArgs.KeepTelemetry=$true}
    & .\tools\run-frame-cost.ps1 @runArgs | Out-String | Write-Output
  }catch{Write-Output "FAILED $name : $($_.Exception.Message)"}
  Write-Output ("{0} took {1:n0}s" -f $name,((Get-Date)-$t).TotalSeconds)
}}}
