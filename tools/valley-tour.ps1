# Usage (from project root): .\tools\valley-tour.ps1
# Opens the dev build in a window and plays the Hillcrest Valley tour (--valley-tour): the car
# drives itself from Bayview to Hillcrest Castle, then the camera glides past the sights.
param([int]$Width=1600,[int]$Height=900)
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$exe=[IO.Path]::GetFullPath((Join-Path $root '../little-road-dev-build/Little Road Unity.exe'))
$tmp=Join-Path $root 'review/tmp';New-Item -ItemType Directory -Force -Path $tmp | Out-Null
$save=Join-Path $tmp 'valley-tour-save.json'
Copy-Item (Join-Path $root 'review/watch-cars/save.json') $save -Force
Start-Process -FilePath $exe -ArgumentList @('-screen-width',$Width,'-screen-height',$Height,'-screen-fullscreen','0','-monitor','1','--valley-tour','--adventure-save',('"'+$save+'"'))
