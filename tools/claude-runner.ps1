<#
Little Road job runner for Claude (Cowork).

Claude's shell on this PC runs in a Linux VM and cannot launch Windows programs
(Unity, the .exe builds, the PowerShell harnesses). This runner bridges that gap:
it watches .claude-runner\queue for *.ps1 job files, runs them one at a time
from the project root, and writes a log and a status file per job.

Start it from the project root and leave the window open while working:

    powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\claude-runner.ps1

Stop it with Ctrl+C, by closing the window, or by creating .claude-runner\queue\STOP.

SECURITY: it runs ANY .ps1 placed in the queue folder. Only leave it running
while you are actively working with Claude.

Job options (optional comment lines at the top of a job file):
    # timeout-minutes: 90      (default 60; the job's process tree is killed after this)
#>
param(
    [int]$PollSeconds = 2,
    [int]$DefaultTimeoutMinutes = 60
)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$base    = Join-Path $projectRoot '.claude-runner'
$queue   = Join-Path $base 'queue'
$running = Join-Path $base 'running'
$done    = Join-Path $base 'done'
$heartbeat = Join-Path $base 'heartbeat.json'
foreach ($d in @($base, $queue, $running, $done)) {
    if (-not (Test-Path $d)) { New-Item -ItemType Directory -Path $d | Out-Null }
}

# Anything left in 'running' from a previous session was interrupted.
Get-ChildItem $running -Filter *.ps1 -ErrorAction SilentlyContinue | ForEach-Object {
    $stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
    Move-Item $_.FullName (Join-Path $done ($_.BaseName + ".interrupted-$stamp.ps1"))
}

function Write-Heartbeat([string]$state, [string]$job) {
    $obj = [ordered]@{
        state = $state; job = $job; pid = $PID
        time = (Get-Date).ToString('o'); projectRoot = $projectRoot
    }
    $tmp = "$heartbeat.tmp"
    $obj | ConvertTo-Json | Set-Content -Path $tmp -Encoding UTF8
    Move-Item -Force $tmp $heartbeat
}

Write-Host "Little Road runner watching $queue" -ForegroundColor Cyan
Write-Host "Ctrl+C or create queue\STOP to stop. Runs any .ps1 in the queue." -ForegroundColor Yellow

while ($true) {
    if (Test-Path (Join-Path $queue 'STOP')) {
        Remove-Item (Join-Path $queue 'STOP') -Force
        Write-Heartbeat 'stopped' ''
        Write-Host 'STOP file found; exiting.'
        break
    }

    $next = Get-ChildItem $queue -Filter *.ps1 -File -ErrorAction SilentlyContinue |
        Sort-Object Name | Select-Object -First 1
    if (-not $next) {
        Write-Heartbeat 'idle' ''
        Start-Sleep -Seconds $PollSeconds
        continue
    }

    # Let the writer finish (Claude writes .tmp then renames, but be safe).
    $size1 = $next.Length; Start-Sleep -Milliseconds 500
    $next.Refresh(); if ($next.Length -ne $size1) { continue }

    $name = $next.BaseName
    $jobPath = Join-Path $running $next.Name
    Move-Item $next.FullName $jobPath -Force

    $timeout = $DefaultTimeoutMinutes
    foreach ($line in (Get-Content $jobPath -TotalCount 15)) {
        if ($line -match '^\s*#\s*timeout-minutes:\s*(\d+)') { $timeout = [int]$Matches[1] }
    }

    $log    = Join-Path $done "$name.log"
    $errLog = Join-Path $done "$name.err.log"
    $status = Join-Path $done "$name.status.json"
    $start  = Get-Date
    Write-Heartbeat 'running' $name
    Write-Host ("[{0}] running {1} (timeout {2} min)" -f $start.ToString('HH:mm:ss'), $name, $timeout) -ForegroundColor Green

    $exit = $null; $timedOut = $false
    try {
        $proc = Start-Process -FilePath 'powershell.exe' -PassThru -NoNewWindow `
            -WorkingDirectory $projectRoot `
            -ArgumentList @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', "`"$jobPath`"") `
            -RedirectStandardOutput $log -RedirectStandardError $errLog
        $handle = $proc.Handle  # keeps ExitCode available after exit
        $deadline = $start.AddMinutes($timeout)
        while (-not $proc.HasExited) {
            if ((Get-Date) -gt $deadline) {
                $timedOut = $true
                & taskkill.exe /PID $proc.Id /T /F | Out-Null
                break
            }
            Write-Heartbeat 'running' $name
            Start-Sleep -Seconds $PollSeconds
        }
        $proc.WaitForExit()
        $exit = $proc.ExitCode
    } catch {
        Add-Content -Path $errLog -Value ("RUNNER ERROR: " + $_.Exception.Message)
        $exit = -1
    }

    $end = Get-Date
    [ordered]@{
        job = $name; exitCode = $exit; timedOut = $timedOut
        start = $start.ToString('o'); end = $end.ToString('o')
        seconds = [math]::Round(($end - $start).TotalSeconds, 1)
    } | ConvertTo-Json | Set-Content -Path $status -Encoding UTF8
    Move-Item $jobPath (Join-Path $done $next.Name) -Force
    Write-Host ("[{0}] finished {1}: exit {2}{3}" -f $end.ToString('HH:mm:ss'), $name, $exit, $(if ($timedOut) { ' (TIMED OUT)' } else { '' }))
}
