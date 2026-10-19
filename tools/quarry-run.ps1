# Usage (from project root): .\tools\quarry-run.ps1 -Name seam-audit-1 -Flags @('--seam-audit') [-NoQuarry] [-NoReview]
# One run of the dev build for Quarry Hills audits and tests (docs/59): --quarry-hills (unless -NoQuarry), a fixture save, a
# windowed silent game opened without taking the focus (quiet-start.ps1), output in review/quarry-hills/<Name>. Prints the
# lines that matter (audit summaries, test results, exceptions). Never overwrites a run.
# -Hidden: batch mode with no window at all (it still renders its photos), for when Richard is using the PC.
param([Parameter(Mandatory)][ValidatePattern('^[a-z0-9-]+$')][string]$Name,[string[]]$Flags=@(),[switch]$NoQuarry,[switch]$NoReview,[int]$TimeoutSeconds=300,[switch]$Hidden)
$ErrorActionPreference='Stop'
. "$PSScriptRoot/quiet-start.ps1"
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$folder=Join-Path $root "review/quarry-hills/$Name"
if(Test-Path -LiteralPath $folder){throw "Run $Name already exists"}
New-Item -ItemType Directory -Path $folder | Out-Null
Copy-Item (Join-Path $root 'tools/fixtures/watch-cars-save.json') (Join-Path $folder 'save.json')
$exe=[IO.Path]::GetFullPath((Join-Path $root '../little-road-dev-build/Little Road Unity.exe'))
$arguments=@('--start-windowed','-screen-width','1280','-screen-height','720','-screen-fullscreen','0','-monitor','1','--silent',
  '--adventure-save',('"'+(Join-Path $folder 'save.json')+'"'),'--trial-captures',('"'+$folder+'"'),'-logFile',('"'+(Join-Path $folder 'player.log')+'"'))
if(-not $NoQuarry){$arguments+='--quarry-hills'}
if(-not $NoReview){$arguments+='--world-review'}
if($Hidden){$arguments=@('-batchmode')+($arguments|Where-Object{$_ -notin '--start-windowed','-screen-width','1280','-screen-height','720','-screen-fullscreen','0','-monitor','1'})}
$run=Start-Quiet -FilePath $exe -ArgumentList ($arguments+$Flags) -WindowStyle $(if($Hidden){'Hidden'}else{'Normal'})
if(-not $run.WaitForExit($TimeoutSeconds*1000)){$run.Kill();Write-Output "$Name TIMEOUT"}else{Write-Output "$Name exit=$($run.ExitCode)"}
foreach($f in Get-ChildItem -LiteralPath $folder -Filter '*.txt'){Get-Content $f.FullName -TotalCount 1|ForEach-Object{"$($f.Name): $_"}}
Select-String -LiteralPath (Join-Path $folder 'player.log') -Pattern '_TEST |Exception|LITTLE_ROAD_RESUME' | Select-Object -First 12 | ForEach-Object { $_.Line }
