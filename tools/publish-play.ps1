# Usage (from project root): .\tools\publish-play.ps1
# Richard's play copy: copies the current dev build (../little-road-dev-build, overwritten by every build) to
# ../little-road-play, which changes only when this runs, and points the "Little Road" desktop shortcut at it.
# Run it only after a build has passed its checks and smoke tests. The shortcut opens on the main screen
# (-monitor 1); saves are shared with every build (they live in the user's AppData), so nothing is lost either way.
# -Flags: game flags the shortcut opens with, e.g. -Flags '--prop-forge','--new-bayview' (the remodelled towns, docs/80).
# -From / -To: another build folder and play folder (from a second worktree, whose ../ is not the project's: 23 Oct).
param([string[]]$Flags=@(),[string]$From,[string]$To)
$ErrorActionPreference='Stop'
$Flags=@($Flags|ForEach-Object{$_ -split ','}|Where-Object{$_})   # (from powershell -File, a list arrives as one comma-joined string)
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$from=[IO.Path]::GetFullPath($(if($From){$From}else{Join-Path $root '../little-road-dev-build'}))
$to=[IO.Path]::GetFullPath($(if($To){$To}else{Join-Path $root '../little-road-play'}))
$exe='Little Road Unity.exe'
if(-not (Test-Path (Join-Path $from $exe))){throw "No dev build at $from"}
# Only a game running from the play copy blocks replacing it (test runs from a dev build can carry on, as in build-dev).
if(Get-Process 'Little Road Unity' -ErrorAction SilentlyContinue|Where-Object{-not $_.Path -or $_.Path.StartsWith($to,[StringComparison]::OrdinalIgnoreCase)}){throw 'Close the game first (it is running).'}
robocopy $from $to /MIR /NFL /NDL /NJH /NJS /NP | Out-Null
if($LASTEXITCODE -ge 8){throw "Copy failed (robocopy $LASTEXITCODE)"}
$git='-c',"safe.directory=$($root -replace '\\','/')"
$branch=git -C $root @git branch --show-current;$commit=git -C $root @git log -1 --format='%h %s';$dirty=(git -C $root @git status --short --untracked-files=no|Measure-Object).Count
Set-Content -Encoding utf8 (Join-Path $to 'BUILD.txt') @"
Little Road play copy
Published: $(Get-Date -Format 'yyyy-MM-dd HH:mm')
Built from: branch $branch, commit $commit$(if($dirty){" (plus $dirty uncommitted changed files)"})
Dev build time: $((Get-Item (Join-Path $from $exe)).LastWriteTime.ToString('yyyy-MM-dd HH:mm'))
Shortcut flags: $(if($Flags){$Flags -join ' '}else{'none'})
"@
$shell=New-Object -ComObject WScript.Shell
$link=$shell.CreateShortcut((Join-Path ([Environment]::GetFolderPath('Desktop')) 'Little Road.lnk'))
$link.TargetPath=Join-Path $to $exe;$link.Arguments=(@('-monitor','1')+$Flags) -join ' ';$link.WorkingDirectory=$to;$link.IconLocation=(Join-Path $to $exe)+',0'
$link.Description="Little Road (play copy, $branch $(($commit -split ' ')[0]))";$link.Save()
# One build (Richard, 4 Oct: "we only need this one"): Quarry Hills is part of the game now, and the old extra
# shortcuts (max build hot laps and test drive, the continent scale test, Quarry Hills) go to the Recycle Bin.
# The same runs are still there from a terminal: --max-build-test, --scale-test, and so on.
Add-Type -AssemblyName Microsoft.VisualBasic
foreach($old in 'Little Road - Max Build Hot Laps','Little Road - Max Build Test Drive','Little Road - Continent Scale Test','Little Road - Quarry Hills'){
  $lnk=Join-Path ([Environment]::GetFolderPath('Desktop')) ($old+'.lnk')
  if(Test-Path -LiteralPath $lnk){[Microsoft.VisualBasic.FileIO.FileSystem]::DeleteFile($lnk,'OnlyErrorDialogs','SendToRecycleBin')}
}
Write-Output "Published to $to ($branch $commit); desktop shortcut 'Little Road' updated (the only one), flags: $(if($Flags){$Flags -join ' '}else{'none'})."
