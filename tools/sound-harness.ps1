# Usage (from project root): .\tools\sound-harness.ps1 [-Filter engine_s_v10]
# Builds the game's synthesiser sources (unity/Assets/Scripts/Audio/*Synth*.cs, plain C#) into a small console program with Unity's
# bundled .NET 8 and Roslyn, then renders every engine's drive to review/sound/*.wav and prints the checks
# (pitch against firing frequency, level, peaks, cost per voice). No Unity needed; takes a few seconds.
param([string]$Filter='')
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$data='C:\Program Files\Unity\Hub\Editor\6000.6.1f1\Editor\Data'
$csc=Join-Path $data 'DotNetSdk\sdk\8.0.318\Roslyn\bincore\csc.dll'
$dotnet=Join-Path $data 'DotNetSdk\dotnet.exe'
$framework=Get-ChildItem (Join-Path $data 'DotNetSdk\shared\Microsoft.NETCore.App') -Directory|Sort-Object Name|Select-Object -Last 1
$out=Join-Path $root 'review\tmp\sound-harness';New-Item -ItemType Directory -Force -Path $out|Out-Null
$sources=@(Get-ChildItem (Join-Path $root 'unity\Assets\Scripts\Audio') -Filter *Synth*.cs|ForEach-Object FullName)+@(Join-Path $PSScriptRoot 'sound-harness\Program.cs')
$refs=Get-ChildItem $framework.FullName -Filter System*.dll|Where-Object{($_.Name -eq 'System.Private.CoreLib.dll' -or $_.Name -notlike 'System.Private*') -and $_.Name -notlike '*.Native.dll'}|ForEach-Object FullName
$refs+=@(Join-Path $framework.FullName 'netstandard.dll')
$dll=Join-Path $out 'SoundHarness.dll';$rsp=Join-Path $out 'SoundHarness.rsp'
$lines=@('-nologo','-nostdlib+','-target:exe','-langversion:9.0','-optimize+','-define:SOUND_HARNESS','-nowarn:0169,0414,0649,8632',"-out:`"$dll`"")
$lines+=$refs|ForEach-Object{"-r:`"$_`""};$lines+=$sources|ForEach-Object{"`"$_`""}
Set-Content -LiteralPath $rsp -Value $lines -Encoding utf8
$compile=& $dotnet $csc "@$rsp" 2>&1|Out-String
if($LASTEXITCODE -ne 0){Write-Output $compile;throw 'Sound harness failed to compile'}
Set-Content -LiteralPath (Join-Path $out 'SoundHarness.runtimeconfig.json') -Encoding utf8 -Value ('{"runtimeOptions":{"tfm":"net8.0","framework":{"name":"Microsoft.NETCore.App","version":"'+$framework.Name+'"}}}')
& $dotnet $dll $root $Filter
exit $LASTEXITCODE
