<#
.SYNOPSIS
    Проверка Unity-проекта в batchmode: компиляция, EditMode- и PlayMode-тесты.

.EXAMPLE
    pwsh -NoProfile -ExecutionPolicy Bypass -File Tools\verify.ps1 all

.NOTES
    Коды выхода: 0 - зелёный, 1 - есть ошибки компиляции/упавшие тесты/сбой запуска,
                 2 - проект открыт в редакторе (заблокирован).
    Логи и результаты: Logs\verify\
#>
[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [ValidateSet('compile', 'editmode', 'playmode', 'all')]
    [string]$Mode = 'all',

    [string]$UnityExe = $(if ($env:UNITY_EXE) { $env:UNITY_EXE } else { 'C:\Program Files\Unity\Hub\Editor\6000.6.7f1\Editor\Unity.exe' }),

    [int]$TimeoutMinutes = 30,

    [int]$ShutdownGraceSeconds = 90
)

$ErrorActionPreference = 'Stop'
$script:ShutdownHangs = @()

$ProjectPath = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$OutDir = Join-Path $ProjectPath 'Logs\verify'
New-Item -ItemType Directory -Force $OutDir | Out-Null

function Write-Section([string]$Text) { Write-Host "`n=== $Text ===" -ForegroundColor Cyan }

function Test-ProjectLocked {
    # Открытый редактор держит Temp\UnityLockfile эксклюзивно.
    $lock = Join-Path $ProjectPath 'Temp\UnityLockfile'
    if (-not (Test-Path $lock)) { return $false }
    try {
        $fs = [System.IO.File]::Open($lock, 'Open', 'ReadWrite', 'None')
        $fs.Close()
        return $false
    } catch {
        return $true
    }
}

function Invoke-Unity([string[]]$UnityArgs, [string]$LogFile) {
    # Старый лог удаляем, иначе маркер завершения из прошлого прогона примется за текущий.
    if (Test-Path $LogFile) { Remove-Item $LogFile -Force }
    $all = @('-batchmode', '-nographics', '-projectPath', $ProjectPath, '-logFile', $LogFile) + $UnityArgs
    # Start-Process не экранирует пробелы сам ("My project"), поэтому собираем строку вручную.
    $argLine = ($all | ForEach-Object { if ($_ -match '\s') { '"' + $_ + '"' } else { $_ } }) -join ' '
    Write-Host "Unity $argLine" -ForegroundColor DarkGray
    $p = Start-Process -FilePath $UnityExe -ArgumentList $argLine -PassThru -NoNewWindow
    $deadline = (Get-Date).AddMinutes($TimeoutMinutes)
    $doneAt = $null

    # Unity иногда виснет уже после работы, на остановке (abort_threads). Если в логе есть маркер
    # завершения, а процесс жив дольше $ShutdownGraceSeconds - завершаем его и берём код из лога.
    while (-not $p.WaitForExit(5000)) {
        if ((Get-Date) -gt $deadline) {
            $p.Kill()
            throw "Unity не завершился за $TimeoutMinutes мин. Лог: $LogFile"
        }

        $marker = Get-DoneMarker $LogFile
        if ($null -eq $marker) { continue }
        if ($null -eq $doneAt) { $doneAt = Get-Date; continue }
        if (((Get-Date) - $doneAt).TotalSeconds -ge $ShutdownGraceSeconds) {
            $p.Kill()
            $script:ShutdownHangs += (Split-Path $LogFile -Leaf)
            Write-Host "Unity завис при выходе после завершения работы - процесс завершён, код взят из лога: $marker" -ForegroundColor Yellow
            return $marker
        }
    }
    return $p.ExitCode
}

function Get-DoneMarker([string]$LogFile) {
    if (-not (Test-Path $LogFile)) { return $null }
    $tests = Select-String -Path $LogFile -Pattern 'Test run completed\. Exiting with code (\d+)' | Select-Object -Last 1
    if ($tests) { return [int]$tests.Matches[0].Groups[1].Value }
    if (Select-String -Path $LogFile -Pattern 'Batchmode quit successfully invoked' -Quiet) { return 0 }
    return $null
}

function Get-CompileErrors([string]$LogFile) {
    if (-not (Test-Path $LogFile)) { return @() }
    # Одна ошибка может повторяться в логе несколько раз - убираем дубли.
    return @(Select-String -Path $LogFile -Pattern 'error CS\d{4}' | ForEach-Object { $_.Line.Trim() } | Sort-Object -Unique)
}

function Test-LogLockMessage([string]$LogFile) {
    if (-not (Test-Path $LogFile)) { return $false }
    return [bool](Select-String -Path $LogFile -Pattern 'another Unity instance is running|It looks like another Unity instance' -Quiet)
}

function Invoke-Compile {
    Write-Section 'Compile'
    $log = Join-Path $OutDir 'compile.log'
    $code = Invoke-Unity @('-quit') $log
    $errors = Get-CompileErrors $log
    $ok = ($code -eq 0) -and ($errors.Count -eq 0)
    if (Test-LogLockMessage $log) { $script:Locked = $true }
    return [pscustomobject]@{
        Step = 'compile'; Ok = $ok; ExitCode = $code; Errors = $errors; Log = $log
        Summary = "ошибок компиляции: $($errors.Count), exit code: $code"
    }
}

function Invoke-Tests([string]$Platform) {
    Write-Section "Tests: $Platform"
    $log = Join-Path $OutDir "$Platform.log"
    $xml = Join-Path $OutDir "$Platform-results.xml"
    if (Test-Path $xml) { Remove-Item $xml -Force }

    # С -runTests флаг -quit не передаётся: раннер сам закрывает Unity.
    $code = Invoke-Unity @('-runTests', '-testPlatform', $Platform, '-testResults', $xml) $log
    if (Test-LogLockMessage $log) { $script:Locked = $true }

    $result = [pscustomobject]@{
        Step = $Platform; Ok = $false; ExitCode = $code; Log = $log; Results = $xml
        Total = 0; Passed = 0; Failed = 0; Skipped = 0; FailedTests = @(); Errors = @(); Summary = ''
    }

    if (-not (Test-Path $xml)) {
        $result.Errors = Get-CompileErrors $log
        $result.Summary = "results.xml не создан (exit code $code). Ошибок компиляции в логе: $($result.Errors.Count)"
        return $result
    }

    [xml]$doc = Get-Content -Raw $xml
    $run = $doc.'test-run'
    $result.Total = [int]$run.total
    $result.Passed = [int]$run.passed
    $result.Failed = [int]$run.failed
    $result.Skipped = [int]$run.skipped + [int]$run.inconclusive
    $result.FailedTests = @($doc.SelectNodes("//test-case[@result='Failed']") | ForEach-Object {
            $msg = if ($_.failure -and $_.failure.message) { ($_.failure.message.InnerText -split "`n")[0].Trim() } else { '' }
            "$($_.fullname): $msg"
        })
    # Exit code 0 - все прошли, 2 - есть упавшие, иное - сбой раннера.
    $result.Ok = ($result.Failed -eq 0) -and ($code -eq 0)
    $result.Summary = "всего: $($result.Total), прошло: $($result.Passed), упало: $($result.Failed), пропущено: $($result.Skipped), exit code: $code"
    return $result
}

# --- main ---

if (-not (Test-Path $UnityExe)) {
    Write-Host "Unity.exe не найден: $UnityExe. Укажи -UnityExe или переменную UNITY_EXE." -ForegroundColor Red
    exit 1
}

$script:Locked = $false
if (Test-ProjectLocked) {
    Write-Host 'PROJECT LOCKED: проект открыт в редакторе Unity. Закрой Unity и запусти проверку снова.' -ForegroundColor Yellow
    exit 2
}

$steps = switch ($Mode) {
    'compile' { @('compile') }
    'editmode' { @('editmode') }
    'playmode' { @('playmode') }
    'all' { @('compile', 'editmode', 'playmode') }
}

$results = @()
foreach ($s in $steps) {
    $r = if ($s -eq 'compile') { Invoke-Compile } else { Invoke-Tests $s }
    $results += $r
    if ($script:Locked) { break }
    # Без компиляции тесты гонять бессмысленно.
    if ($s -eq 'compile' -and -not $r.Ok) { break }
}

$lines = @("verify $Mode  $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')", '')
foreach ($r in $results) {
    $lines += "[$(if ($r.Ok) { 'OK  ' } else { 'FAIL' })] $($r.Step): $($r.Summary)"
    foreach ($e in $r.Errors) { $lines += "    $e" }
    if ($r.PSObject.Properties['FailedTests']) { foreach ($t in $r.FailedTests) { $lines += "    FAILED $t" } }
    $lines += "    log: $($r.Log)"
}

if ($script:ShutdownHangs.Count -gt 0) {
    $lines += "note: Unity завис при выходе и был завершён скриптом: $($script:ShutdownHangs -join ', ')"
}

$green = (-not $script:Locked) -and ($results.Count -eq $steps.Count) -and -not ($results | Where-Object { -not $_.Ok })
$lines += ''
if ($script:Locked) {
    $lines += 'RESULT: LOCKED - проект открыт в редакторе Unity. Закрой Unity.'
} elseif ($green) {
    $lines += 'RESULT: GREEN'
} else {
    $lines += 'RESULT: RED'
}

$summaryFile = Join-Path $OutDir 'summary.txt'
$lines | Set-Content -Encoding utf8 $summaryFile
Write-Section 'Summary'
$lines | ForEach-Object { Write-Host $_ }
Write-Host "summary: $summaryFile"

if ($script:Locked) { exit 2 }
if ($green) { exit 0 } else { exit 1 }
