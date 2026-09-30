# Usage (from project root): .\tools\compile-check.ps1
# Compiles the game's C# in a few seconds with Unity's own Roslyn compiler (C# 9, as Unity 6 does),
# without starting Unity: runtime scripts (Assembly-CSharp), then Editor scripts (Assembly-CSharp-Editor)
# against the runtime result. Package references come from unity/Library/ScriptAssemblies (left by the
# last Unity build), so a brand-new package added since then needs one real build first.
# build-dev.ps1 runs this first and stops before launching Unity if it fails.
param([switch]$Quiet)
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$data='C:\Program Files\Unity\Hub\Editor\6000.6.1f1\Editor\Data'
$csc=Join-Path $data 'DotNetSdk\sdk\8.0.318\Roslyn\bincore\csc.dll'
$dotnet=Join-Path $data 'DotNetSdk\dotnet.exe';if(-not (Test-Path $dotnet)){$dotnet='dotnet'}
$assets=Join-Path $root 'unity\Assets';$library=Join-Path $root 'unity\Library\ScriptAssemblies'
$out=Join-Path $root 'review\tmp\compile-check';New-Item -ItemType Directory -Force -Path $out|Out-Null
if(-not (Test-Path $library)){Write-Output 'COMPILE_CHECK skipped: no Library/ScriptAssemblies yet (run one Unity build first)';exit 0}
$refs=@(Join-Path $data 'NetStandard\ref\2.1.0\netstandard.dll')
# Managed\UnityEngine holds the engine modules and the editor modules; game scripts may only see the engine.
$modules=Get-ChildItem (Join-Path $data 'Managed\UnityEngine') -Filter *.dll
# Optional engine modules the project hasn't enabled (Packages/manifest.json) are left out, as Unity's own build does
# (ScreenCapture passed here once and failed the real build).
$manifest=Get-Content (Join-Path $root 'unity\Packages\manifest.json') -Raw
$optional=@{'UnityEngine.ScreenCaptureModule.dll'='com.unity.modules.screencapture';'UnityEngine.VRModule.dll'='com.unity.modules.vr';'UnityEngine.XRModule.dll'='com.unity.modules.xr';'UnityEngine.ClothModule.dll'='com.unity.modules.cloth';'UnityEngine.VehiclesModule.dll'='com.unity.modules.vehicles';'UnityEngine.WindModule.dll'='com.unity.modules.wind';'UnityEngine.VideoModule.dll'='com.unity.modules.video'}
$refs+=$modules|Where-Object{$_.Name -notlike 'UnityEditor*' -and -not ($optional.ContainsKey($_.Name) -and $manifest -notmatch [regex]::Escape($optional[$_.Name]))}|ForEach-Object FullName
$editorModules=$modules|Where-Object{$_.Name -like 'UnityEditor*'}|ForEach-Object FullName
# Package assemblies: runtime scripts see runtime packages only; editor scripts see all.
$packages=Get-ChildItem $library -Filter *.dll|Where-Object{$_.Name -notlike 'Assembly-CSharp*'}
$refs+=$packages|Where-Object{$_.Name -notmatch 'Editor'}|ForEach-Object FullName
$editorPackages=$packages|Where-Object{$_.Name -match 'Editor'}|ForEach-Object FullName
$all=Get-ChildItem $assets -Recurse -Filter *.cs
$runtime=$all|Where-Object{$_.FullName -notmatch '\\Editor\\'}|ForEach-Object FullName
$editor=$all|Where-Object{$_.FullName -match '\\Editor\\'}|ForEach-Object FullName
function Compile([string]$name,[string[]]$sources,[string[]]$references){
  $rsp=Join-Path $out "$name.rsp";$dll=Join-Path $out "$name.dll"
  $lines=@('-nologo','-noconfig','-nostdlib+','-target:library','-langversion:9.0','-nowarn:0169,0414,0649,0618,0219,0162,1998,0108,0114,0672,8321','-debug-',"-out:`"$dll`"")
  $lines+=$references|ForEach-Object{"-r:`"$_`""};$lines+=$sources|ForEach-Object{"`"$_`""}
  Set-Content -LiteralPath $rsp -Value $lines -Encoding utf8
  $output=& $dotnet $csc "@$rsp" 2>&1|Out-String
  $errors=@($output -split "`r?`n"|Where-Object{$_ -match ': error '})
  return [pscustomobject]@{ok=($LASTEXITCODE -eq 0);errors=$errors;dll=$dll}
}
$t=Get-Date
$rt=Compile 'Assembly-CSharp' $runtime $refs
$result=$rt
if($rt.ok){$ed=Compile 'Assembly-CSharp-Editor' $editor ($refs+$editorModules+$editorPackages+@($rt.dll));$result=$ed}
$seconds=((Get-Date)-$t).TotalSeconds
if($result.ok){if(-not $Quiet){"COMPILE_CHECK ok runtime={0} editor={1} files in {2:n1}s" -f $runtime.Count,$editor.Count,$seconds};exit 0}
$result.errors|Select-Object -Unique|ForEach-Object{$_ -replace [regex]::Escape($root+'\'),''}
"COMPILE_CHECK failed ({0} errors) in {1:n1}s" -f ($result.errors|Select-Object -Unique).Count,$seconds
exit 1
