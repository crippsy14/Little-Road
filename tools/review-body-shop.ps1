# Usage (from project root): .\tools\review-body-shop.ps1 -Name b1
# Rendered review of the Hillcrest Pit Stop body shop on a throwaway save: Lina's menu, the shop,
# buying the Comet, then Gio's garage bay swapping the live car. Captures to
# review/ui-screens/<Name>/ (never overwrites); exit 0 only if the swap happened.
param([Parameter(Mandatory)][ValidatePattern('^[a-z0-9-]+$')][string]$Name)
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$folder=Join-Path $root "review/ui-screens/$Name"
if(Test-Path -LiteralPath $folder){throw "Review $Name already exists"}
New-Item -ItemType Directory -Path $folder | Out-Null
$exe=[IO.Path]::GetFullPath((Join-Path $root '../little-road-dev-build/Little Road Unity.exe'))
Copy-Item (Join-Path $root 'tools/fixtures/watch-cars-save.json') (Join-Path $folder 'save.json')
$arguments=@('--silent','-screen-width','1920','-screen-height','1080','-screen-fullscreen','0','-monitor','1','--body-shop-review','--adventure-save',('"'+(Join-Path $folder 'save.json')+'"'),
  '--trial-captures',('"'+$folder+'"'),'-logFile',('"'+(Join-Path $folder 'player.log')+'"'))
$run=Start-Process -FilePath $exe -PassThru -ArgumentList $arguments
$run.WaitForExit()
Write-Output "exit=$($run.ExitCode)"
Select-String -LiteralPath (Join-Path $folder 'player.log') -Pattern 'BODY_SHOP_REVIEW|Exception' | Select-Object -First 6 | ForEach-Object { $_.Line }
