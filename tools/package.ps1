param()
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$taskProject = Split-Path -Parent $PSScriptRoot
$taskOutputs = Split-Path -Parent $taskProject

function Write-ProjectArchive([string]$taskSource, [string]$taskArchivePath, [string]$taskPrefix) {
  $taskSource = [IO.Path]::GetFullPath($taskSource).TrimEnd('\')
  $taskStream = [IO.File]::Open($taskArchivePath,[IO.FileMode]::Create)
  $taskArchive = [IO.Compression.ZipArchive]::new($taskStream,[IO.Compression.ZipArchiveMode]::Create)
  try {
    foreach ($taskFile in Get-ChildItem -LiteralPath $taskSource -File -Recurse -Force) {
      $taskRelative = $taskFile.FullName.Substring($taskSource.Length+1).Replace('\','/')
      if ($taskRelative -match '(^|/)(\.git|\.godot)/|\.blend[12]$|\.tmp$|review/test-save\.json') { continue }
      [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($taskArchive,$taskFile.FullName,($taskPrefix+'/'+$taskRelative),[IO.Compression.CompressionLevel]::Optimal) | Out-Null
    }
  } finally {
    $taskArchive.Dispose()
    $taskStream.Dispose()
  }
}

Write-ProjectArchive $taskProject (Join-Path $taskOutputs 'little-road-project.zip') 'little-road'
Write-ProjectArchive (Join-Path $taskOutputs 'little-road-windows') (Join-Path $taskOutputs 'little-road-windows.zip') 'little-road-windows'
Get-Item (Join-Path $taskOutputs 'little-road-project.zip'),(Join-Path $taskOutputs 'little-road-windows.zip') | Select-Object Name,Length
