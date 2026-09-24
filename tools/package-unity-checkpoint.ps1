param(
    [Parameter(Mandatory=$true)][string]$ExportPath,
    [Parameter(Mandatory=$true)][string]$ArchivePath,
    [Parameter(Mandatory=$true)][string]$ReviewDirectory,
    [Parameter(Mandatory=$true)][string]$BuildLog,
    [Parameter(Mandatory=$true)][string]$RaceReport,
    [Parameter(Mandatory=$true)][string]$WorldReview
)
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$taskExport=(Resolve-Path -LiteralPath $ExportPath).Path.TrimEnd('\')
$taskArchivePath=[IO.Path]::GetFullPath($ArchivePath)
$taskReview=(Resolve-Path -LiteralPath $ReviewDirectory).Path
if(Test-Path -LiteralPath $taskArchivePath){throw 'Refusing to overwrite a frozen checkpoint'}
if($taskArchivePath.StartsWith($taskExport+'\',[StringComparison]::OrdinalIgnoreCase)){throw 'Archive must be outside the export'}
foreach($taskRequired in @('Little Road Unity.exe','README.txt','REVIEW.txt','THIRD_PARTY_ASSETS.md')) {
    if(-not(Test-Path -LiteralPath (Join-Path $taskExport $taskRequired))){throw "Missing export file: $taskRequired"}
}
$taskRecords=[System.Collections.Generic.List[object]]::new()
$taskArchive=[IO.Compression.ZipFile]::Open($taskArchivePath,[IO.Compression.ZipArchiveMode]::Create)
try {
    foreach($taskFile in Get-ChildItem -LiteralPath $taskExport -Recurse -File | Sort-Object FullName) {
        $taskRelative=$taskFile.FullName.Substring($taskExport.Length+1).Replace('\','/')
        if($taskRelative -match 'BackUpThisFolder'){continue}
        $taskHash=(Get-FileHash -LiteralPath $taskFile.FullName -Algorithm SHA256).Hash
        [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($taskArchive,$taskFile.FullName,$taskRelative,[IO.Compression.CompressionLevel]::Optimal)|Out-Null
        $taskRecords.Add([ordered]@{path=$taskRelative;bytes=$taskFile.Length;sha256=$taskHash})
    }
} finally {$taskArchive.Dispose()}
$taskArchive=[IO.Compression.ZipFile]::OpenRead($taskArchivePath)
try {
    if($taskArchive.Entries.Count -ne $taskRecords.Count){throw 'Archive file count mismatch'}
    foreach($taskRecord in $taskRecords) {
        $taskEntry=$taskArchive.GetEntry($taskRecord.path)
        if($null -eq $taskEntry -or $taskEntry.Length -ne $taskRecord.bytes){throw "Archive entry mismatch: $($taskRecord.path)"}
        $taskStream=$taskEntry.Open();$taskHasher=[Security.Cryptography.SHA256]::Create()
        try {$taskHash=[BitConverter]::ToString($taskHasher.ComputeHash($taskStream)).Replace('-','')} finally {$taskHasher.Dispose();$taskStream.Dispose()}
        if($taskHash -ne $taskRecord.sha256){throw "Archive hash mismatch: $($taskRecord.path)"}
    }
} finally {$taskArchive.Dispose()}
$taskManifest=[ordered]@{
    archive=$taskArchivePath;bytes=(Get-Item -LiteralPath $taskArchivePath).Length;
    sha256=(Get-FileHash -LiteralPath $taskArchivePath -Algorithm SHA256).Hash;
    allArchivedFilesMatchExport=$true;fileCount=$taskRecords.Count;
    buildLog=$BuildLog;raceReport=$RaceReport;worldReview=$WorldReview;files=$taskRecords
}
$taskManifest|ConvertTo-Json -Depth 6|Set-Content -LiteralPath (Join-Path $taskReview 'package-manifest.json')
[pscustomobject]$taskManifest|Select-Object archive,bytes,sha256,fileCount,allArchivedFilesMatchExport|ConvertTo-Json
