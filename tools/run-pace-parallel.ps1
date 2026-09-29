# Parallel version of run-pace-study.ps1: races every arm/track/seed as hidden processes, up to -Jobs at once.
# Race outcomes (times, contacts, resets) are unaffected; don't use it for frame-cost timing, which needs one
# race at a time. Existing run folders are skipped, as in run-pace-study.
# Usage: .\tools\run-pace-parallel.ps1 -Study 206 -Label my-change -Flags @('--my-flag') [-Jobs 6] [-PlaybackRate 1]
# The default driver is Racing AI v2; race the first AI with -Label first-ai -Flags @('--first-ai').
param([int]$Study,[string]$Label,[string[]]$Flags=@(),[string[]]$Tracks=@('cedar','bayview'),[int[]]$Seeds=@(1709,1710,1711),
  [switch]$SkipBaseline,[int]$Jobs=6,[ValidateRange(.25,4)][float]$PlaybackRate=1,[switch]$KeepTelemetry)
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$arms=@();if(-not $SkipBaseline){$arms+=,@('off',@())};$arms+=,@($Label,$Flags)
$queue=[Collections.Generic.Queue[object]]::new()
foreach($track in $Tracks){foreach($seed in $Seeds){foreach($arm in $arms){
  $name="$track-s$seed-$($arm[0])"
  if(Test-Path (Join-Path $root "review/unity-35/frame-cost-$Study/$name")){Write-Output "skip existing $name";continue}
  $queue.Enqueue(@{Track=$track;Seed=$seed;Name=$name;Extra=$arm[1]})
}}}
$running=@()
while($queue.Count -gt 0 -or $running.Count -gt 0){
  while($queue.Count -gt 0 -and $running.Count -lt $Jobs){
    $r=$queue.Dequeue()
    $job=Start-Job -ScriptBlock{param($root,$study,$r,$rate,$keep)
      Set-Location $root
      $a=@{Study=$study;Track=$r.Track;Seed=$r.Seed;Name=$r.Name;PlaybackRate=$rate}
      if($r.Extra.Count){$a.Extra=[string[]]$r.Extra};if($keep){$a.KeepTelemetry=$true}
      & .\tools\run-frame-cost.ps1 @a | Select-Object -Last 1
    } -ArgumentList $root,$Study,$r,$PlaybackRate,[bool]$KeepTelemetry
    $running+=,@($job,$r.Name,(Get-Date))
  }
  Start-Sleep -Seconds 3
  foreach($entry in @($running)){
    if($entry[0].State -in 'Completed','Failed','Stopped'){
      $out=try{Receive-Job $entry[0] -ErrorAction Stop|Out-String}catch{"FAILED $($_.Exception.Message)"}
      Write-Output ("{0} took {1:n0}s: {2}" -f $entry[1],((Get-Date)-$entry[2]).TotalSeconds,$out.Trim())
      Remove-Job $entry[0] -Force;$running=@($running|Where-Object{$_[1] -ne $entry[1]})
    }
  }
}
