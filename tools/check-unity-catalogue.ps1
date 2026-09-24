$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path $PSScriptRoot -Parent
$taskSourcePath = Join-Path $taskRoot 'game/scripts/parts_catalog.gd'
$taskTargetPath = Join-Path $taskRoot 'unity/Assets/Resources/Data/parts.json'
$taskSource = Get-Content -LiteralPath $taskSourcePath -Raw
$taskMatch = [regex]::Match($taskSource, '(?s)const PARTS := (\{.*?\})\r?\n\r?\nstatic func stock')
if (-not $taskMatch.Success) { throw 'Preserved catalogue boundary not found' }
$taskOriginal = $taskMatch.Groups[1].Value | ConvertFrom-Json -AsHashtable
$taskPort = Get-Content -LiteralPath $taskTargetPath -Raw | ConvertFrom-Json -AsHashtable
$taskChecks = [System.Collections.Generic.List[string]]::new()
if ($taskPort.schemaVersion -ne 1) { throw 'Unexpected catalogue schema' }
if ($taskOriginal.Count -ne $taskPort.parts.Count) { throw 'Part count differs from preserved game' }
$taskIndex = @{}
foreach ($taskPart in $taskPort.parts) {
    if ($taskIndex.ContainsKey($taskPart.id)) { throw "Duplicate ID: $($taskPart.id)" }
    $taskIndex[$taskPart.id] = $taskPart
    if (-not $taskOriginal.ContainsKey($taskPart.id)) { throw "Unknown ID: $($taskPart.id)" }
    $taskExpected = $taskOriginal[$taskPart.id]
    if ($taskPart.Count -ne $taskExpected.Count + 1) { throw "Field count mismatch: $($taskPart.id)" }
    foreach ($taskKey in $taskExpected.Keys) {
        if (-not $taskPart.ContainsKey($taskKey) -or $taskPart[$taskKey] -cne $taskExpected[$taskKey]) {
            throw "Changed source value: $($taskPart.id).$taskKey"
        }
        $taskChecks.Add("$($taskPart.id).$taskKey matches preserved source")
    }
}
$taskSlots = @('engine','turbo','tyres','transmission','brakes','steering','chassis')
if (($taskPort.slots -join ',') -cne ($taskSlots -join ',')) { throw 'Equipment slots or order changed' }
foreach ($taskSlot in $taskSlots) {
    if ($taskIndex[$taskSlot + '_stock'].slot -cne $taskSlot) { throw "Missing stock equipment: $taskSlot" }
}
$taskPaid = @($taskPort.parts | Where-Object { $_.price -gt 0 }).Count
if ($taskPaid -ne 14) { throw 'Expected fourteen purchases from the current milestone' }
$taskReport = [ordered]@{
    utc = [DateTime]::UtcNow.ToString('O')
    scope = 'Data-only comparison against preserved Godot source; does not compile C# or validate Unity runtime'
    passed = $true
    partCount = $taskPort.parts.Count
    paidPartCount = $taskPaid
    slots = $taskSlots
    sourceSHA256 = (Get-FileHash -LiteralPath $taskSourcePath -Algorithm SHA256).Hash
    targetSHA256 = (Get-FileHash -LiteralPath $taskTargetPath -Algorithm SHA256).Hash
    fieldChecks = $taskChecks
}
$taskReport | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $taskRoot 'review/unity-01/catalogue-port-check.json')
Write-Output "PASS: $($taskPort.parts.Count) parts, $taskPaid purchases, seven slots, $($taskChecks.Count) unchanged source fields. Unity runtime remains untested."
