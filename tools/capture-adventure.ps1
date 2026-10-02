# Usage (from project root): .\tools\capture-adventure.ps1 -Name v1
# Runs the connected-adventure smoke test on the dev build and saves screenshots of every
# menu it passes through (garage, race preview, results, race story, retry) to
# review/ui-screens/<Name>/captures. Hidden window (or -Visible for a real on-screen run), 4x race speed, ~1-2 minutes.
param([Parameter(Mandatory)][ValidatePattern('^[a-z0-9-]+$')][string]$Name,[switch]$Visible,[string[]]$Flags=@())
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$folder=Join-Path $root "review/ui-screens/$Name"
if(Test-Path -LiteralPath $folder){Remove-Item -LiteralPath $folder -Recurse -Force}
New-Item -ItemType Directory -Path $folder | Out-Null
$exe=[IO.Path]::GetFullPath((Join-Path $root '../little-road-dev-build/Little Road Unity.exe'))
$save=Join-Path $folder 'save.json'
$arguments=@($(if($Visible){@('-screen-width','1920','-screen-height','1080','-screen-fullscreen','0','-monitor','1')}else{'-batchmode'}),'--silent','--race-pause-review','--adventure-smoke','--adventure-save',('"'+$save+'"'),'--adventure-report',('"'+$folder+'/adventure.json"'),'--trial-report',('"'+$folder+'/race.json"'),'--trial-captures',('"'+$folder+'/captures"'),'--trial-duration','420','--trial-timescale','4','-logFile',('"'+$folder+'/player.log"'),'--adventure-retry-review')+$Flags
$run=Start-Process -FilePath $exe -WindowStyle $(if($Visible){'Normal'}else{'Hidden'}) -PassThru -ArgumentList ($arguments|ForEach-Object{$_})
$run.WaitForExit()
Write-Output "exit=$($run.ExitCode)"
Select-String -LiteralPath (Join-Path $folder 'player.log') -Pattern 'Exception|PASSED' | Select-Object -First 12 | ForEach-Object { $_.Line }
Get-ChildItem -Recurse (Join-Path $folder 'captures') -Filter *.png | ForEach-Object { $_.FullName.Substring($root.Length+1) }
