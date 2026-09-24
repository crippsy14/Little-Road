param([Parameter(Mandatory=$true)][string]$ReportPath)
$ErrorActionPreference='Stop'
$taskReport=Get-Content -LiteralPath $ReportPath -Raw | ConvertFrom-Json
$taskFailures=[System.Collections.Generic.List[string]]::new()
$taskPasses=[System.Collections.Generic.List[object]]::new()
if($taskReport.gridMode -ne 'line'){ $taskFailures.Add('Test must start in single file.') }
if($taskReport.carCount -notin @(2,3)){ $taskFailures.Add('Expected a two- or three-car controlled test.') }
if($taskReport.finished -ne $taskReport.carCount){ $taskFailures.Add('Not every car finished.') }
if(@($taskReport.cars | Where-Object {$_.recoveries -gt 0 -or $_.laps -ne $taskReport.requiredLaps}).Count -gt 0){
    $taskFailures.Add('A car needed recovery or did not complete the required laps.')
}
# The cases use stock, then street/club from behind. Verify physical progress
# changes before the slower car finishes, not just the final result order.
for($taskFast=1;$taskFast -lt $taskReport.carCount;$taskFast++) {
    for($taskSlow=0;$taskSlow -lt $taskFast;$taskSlow++) {
        $taskFirstFast=@($taskReport.motion | Where-Object id -eq $taskFast)[0]
        $taskFirstSlow=@($taskReport.motion | Where-Object id -eq $taskSlow)[0]
        if($taskFirstFast.progress -ge $taskFirstSlow.progress-5){ $taskFailures.Add("Car $taskFast did not start sufficiently behind car $taskSlow.") }
        $taskFound=$null
        foreach($taskSample in @($taskReport.motion | Where-Object {$_.id -eq $taskFast -and $_.time -lt $taskReport.cars[$taskSlow].time})) {
            $taskOther=$taskReport.motion | Where-Object {$_.id -eq $taskSlow -and $_.time -eq $taskSample.time} | Select-Object -First 1
            if($taskOther -and $taskSample.progress -gt $taskOther.progress+3 -and [math]::Abs($taskSample.offset) -lt 8 -and [math]::Abs($taskOther.offset) -lt 8){
                $taskFound=[ordered]@{faster=$taskFast;slower=$taskSlow;sampleTime=$taskSample.time;leadMetres=$taskSample.progress-$taskOther.progress;fasterOffset=$taskSample.offset;slowerOffset=$taskOther.offset}
                break
            }
        }
        if($null -eq $taskFound){ $taskFailures.Add("No on-road pass from car $taskFast over car $taskSlow was observed.") }else{ $taskPasses.Add($taskFound) }
        if($taskReport.cars[$taskFast].place -ge $taskReport.cars[$taskSlow].place){ $taskFailures.Add("Car $taskFast did not retain its expected finish advantage over car $taskSlow.") }
    }
}
$taskResult=[ordered]@{source=(Resolve-Path -LiteralPath $ReportPath).Path;scope='Controlled stock/street/club passing from behind; 1Hz physical trajectory and finish evidence';passed=($taskFailures.Count -eq 0);observedPasses=$taskPasses;failures=$taskFailures;carResults=$taskReport.cars}
$taskOutput=[IO.Path]::ChangeExtension($ReportPath,'.validation.json')
$taskResult | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $taskOutput
Write-Output ($taskResult | ConvertTo-Json -Depth 5)
if($taskFailures.Count -gt 0){exit 1}
