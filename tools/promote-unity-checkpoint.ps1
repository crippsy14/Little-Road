param(
    [Parameter(Mandatory=$true)][string]$Candidate,
    [Parameter(Mandatory=$true)][int]$Checkpoint,
    [Parameter(Mandatory=$true)][string]$Manifest
)
$ErrorActionPreference='Stop'
$promotionOutputs=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..')).TrimEnd('\')
$promotionSource=(Resolve-Path -LiteralPath $Candidate).Path.TrimEnd('\')
$promotionManifestPath=(Resolve-Path -LiteralPath $Manifest).Path
$promotionRecord=Get-Content -LiteralPath $promotionManifestPath -Raw | ConvertFrom-Json
if(-not $promotionRecord.allArchivedFilesMatchExport){throw 'Package verification has not passed'}
if((Get-FileHash -LiteralPath $promotionRecord.archive).Hash -ne $promotionRecord.sha256){throw 'Frozen package no longer matches its manifest'}
$promotionTarget=Join-Path $promotionOutputs 'little-road-unity-windows'
$promotionStage=Join-Path $promotionOutputs "little-road-unity-windows-stage-$Checkpoint"
$promotionBackup=Join-Path $promotionOutputs "little-road-unity-windows-before-$Checkpoint"
foreach($promotionPath in @($promotionSource,$promotionTarget,$promotionStage,$promotionBackup)) {
    if(-not [IO.Path]::GetFullPath($promotionPath).StartsWith($promotionOutputs+'\',[StringComparison]::OrdinalIgnoreCase)){throw 'Promotion path is outside workspace outputs'}
}
if($promotionSource -eq $promotionTarget){throw 'Candidate must be a separate export'}
foreach($promotionPath in @($promotionStage,$promotionBackup)){if(Test-Path -LiteralPath $promotionPath){throw "Preserved path already exists: $promotionPath"}}
foreach($promotionProcess in Get-Process -Name 'Little Road Unity' -ErrorAction SilentlyContinue) {
    if(-not $promotionProcess.Path){throw 'Cannot verify a running game process path'}
    if($promotionProcess.Path.StartsWith($promotionTarget+'\',[StringComparison]::OrdinalIgnoreCase)){throw 'Normal game export is in use'}
}
New-Item -ItemType Directory -Path $promotionStage | Out-Null
foreach($promotionFile in $promotionRecord.files) {
    $promotionInput=[IO.Path]::GetFullPath((Join-Path $promotionSource $promotionFile.path))
    $promotionOutput=[IO.Path]::GetFullPath((Join-Path $promotionStage $promotionFile.path))
    if(-not $promotionInput.StartsWith($promotionSource+'\',[StringComparison]::OrdinalIgnoreCase) -or
       -not $promotionOutput.StartsWith($promotionStage+'\',[StringComparison]::OrdinalIgnoreCase)){throw 'File path escapes export'}
    if((Get-FileHash -LiteralPath $promotionInput).Hash -ne $promotionFile.sha256){throw "Candidate changed after packaging: $($promotionFile.path)"}
    New-Item -ItemType Directory -Path ([IO.Path]::GetDirectoryName($promotionOutput)) -Force | Out-Null
    Copy-Item -LiteralPath $promotionInput -Destination $promotionOutput
    if((Get-FileHash -LiteralPath $promotionOutput).Hash -ne $promotionFile.sha256){throw "Staged file mismatch: $($promotionFile.path)"}
}
if(@(Get-ChildItem -LiteralPath $promotionStage -Recurse -File).Count -ne $promotionRecord.fileCount){throw 'Staged file count mismatch'}
Move-Item -LiteralPath $promotionTarget -Destination $promotionBackup
try { Move-Item -LiteralPath $promotionStage -Destination $promotionTarget }
catch { Move-Item -LiteralPath $promotionBackup -Destination $promotionTarget; throw }
$promotionResult=[ordered]@{checkpoint=$Checkpoint;source=$promotionSource;target=$promotionTarget;backup=$promotionBackup;
    verifiedFiles=$promotionRecord.fileCount;package=$promotionRecord.archive;packageSha256=$promotionRecord.sha256;utc=[DateTime]::UtcNow.ToString('O')}
$promotionResult | ConvertTo-Json | Set-Content -LiteralPath (Join-Path ([IO.Path]::GetDirectoryName($promotionManifestPath)) 'promotion.json')
$promotionResult | ConvertTo-Json
