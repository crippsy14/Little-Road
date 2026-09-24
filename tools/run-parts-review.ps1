# Usage (from project root): .\tools\run-parts-review.ps1 -Name v1
# Runs the scripted Harbour Garage parts-browser review (PartsBrowserReview) on the
# dev build with a fresh save; screenshots go to review/ui-screens/<Name>/parts.
param([Parameter(Mandatory)][ValidatePattern('^[a-z0-9-]+$')][string]$Name)
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$folder=Join-Path $root "review/ui-screens/$Name/parts"
if(Test-Path -LiteralPath $folder){Remove-Item -LiteralPath $folder -Recurse -Force}
New-Item -ItemType Directory -Path $folder | Out-Null
$exe=[IO.Path]::GetFullPath((Join-Path $root '../little-road-dev-build/Little Road Unity.exe'))
$arguments=@('-batchmode','--parts-browser-review','--adventure-save',('"'+(Join-Path $folder 'save.json')+'"'),'--trial-captures',('"'+$folder+'"'),'-logFile',('"'+$folder+'/player.log"'))
$run=Start-Process -FilePath $exe -WindowStyle Hidden -PassThru -ArgumentList $arguments
if(-not $run.WaitForExit(180000)){$run.Kill();throw 'Parts review timed out'}
Write-Output "exit=$($run.ExitCode)"
Select-String -LiteralPath (Join-Path $folder 'player.log') -Pattern 'PARTS_BROWSER_REVIEW|Exception' | Select-Object -First 6 | ForEach-Object { $_.Line }
