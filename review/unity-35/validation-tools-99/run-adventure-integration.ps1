param([int]$Version=88,[switch]$VerifySave)
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$base=Join-Path $root "review/unity-35/race-preview-$Version"
$folder=Join-Path $base $(if($VerifySave){'reload'}else{'adventure'})
if(Test-Path -LiteralPath $folder){throw 'Choose a fresh evidence folder'}
New-Item -ItemType Directory -Path $folder | Out-Null
$save=Join-Path $base 'adventure/save.json'
if($VerifySave -and !(Test-Path -LiteralPath $save)){throw 'Complete the connected adventure first'}
$exe=[IO.Path]::GetFullPath((Join-Path $root "../little-road-context-$Version-review/Little Road Unity.exe"))
$arguments=@('-batchmode',$(if($VerifySave){'--adventure-verify-save'}else{'--adventure-smoke'}),'--adventure-save',('"'+$save+'"'),'--adventure-report',('"'+$folder+'/adventure.json"'),'--trial-report',('"'+$folder+'/race.json"'),'--trial-captures',('"'+$folder+'/captures"'),'--trial-duration','420','--trial-timescale','4','-logFile',('"'+$folder+'/player.log"'))
if(!$VerifySave){$arguments+='--adventure-retry-review'}
@{executable=$exe;arguments=$arguments}|ConvertTo-Json|Set-Content (Join-Path $folder 'launch.json')
$run=Start-Process -FilePath $exe -WindowStyle Hidden -PassThru -ArgumentList $arguments
Write-Output "Started adventure $Version verify=$VerifySave pid=$($run.Id)"
$run.WaitForExit()
@{exitCode=$run.ExitCode}|ConvertTo-Json|Set-Content (Join-Path $folder 'exit.json')
Write-Output "Finished adventure $Version verify=$VerifySave exit=$($run.ExitCode)"
if($run.ExitCode -ne 0){throw 'Adventure integration failed'}
