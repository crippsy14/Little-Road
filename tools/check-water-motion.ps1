param([Parameter(Mandatory=$true)][string]$CaptureDirectory)
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$taskFolder=(Resolve-Path -LiteralPath $CaptureDirectory).Path
$taskFirst=[Drawing.Bitmap]::new((Join-Path $taskFolder '09-water-time-0.png'))
$taskSecond=[Drawing.Bitmap]::new((Join-Path $taskFolder '10-water-time-1-6.png'))
try {
    if($taskFirst.Width -ne 1920 -or $taskFirst.Height -ne 1080 -or $taskSecond.Size -ne $taskFirst.Size){throw 'Unexpected review image dimensions'}
    $taskSamples=0;$taskChanged=0;$taskSum=0.0
    # This water-only region belongs to the fixed low bridge camera in VillageReview.
    for($taskY=850;$taskY -lt 970;$taskY+=4){for($taskX=350;$taskX -lt 1150;$taskX+=4){
        $taskA=$taskFirst.GetPixel($taskX,$taskY);$taskB=$taskSecond.GetPixel($taskX,$taskY)
        $taskDelta=([Math]::Abs([int]$taskA.R-$taskB.R)+[Math]::Abs([int]$taskA.G-$taskB.G)+[Math]::Abs([int]$taskA.B-$taskB.B))/3.0
        $taskSamples++;$taskSum+=$taskDelta;if($taskDelta -gt 2){$taskChanged++}
    }}
    $taskMean=$taskSum/$taskSamples;$taskFraction=$taskChanged/[double]$taskSamples
    $taskResult=[ordered]@{scope='Fixed camera, water-only image region at shader times 0 and 1.6 seconds; verifies rendered change, not fluid physics or visual acceptance';samples=$taskSamples;changedFraction=$taskFraction;meanAbsoluteChannelDelta=$taskMean;passed=($taskFraction -gt .01 -and $taskMean -gt .1)}
    $taskResult|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $taskFolder 'water-motion.json')
    $taskResult|ConvertTo-Json
    if(-not $taskResult.passed){throw 'Water review frames did not visibly change'}
} finally {$taskFirst.Dispose();$taskSecond.Dispose()}
