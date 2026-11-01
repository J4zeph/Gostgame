<#
.SYNOPSIS
    Проверка Unity-проекта: компиляция, EditMode- и PlayMode-тесты, консоль.
    Редактор закрыт -> batchmode. Редактор открыт и пакет com.unity.pipeline готов -> через Unity CLI в этом редакторе.

.EXAMPLE
    pwsh -NoProfile -ExecutionPolicy Bypass -File Tools\verify.ps1 all

.NOTES
    Коды выхода: 0 - зелёный, 1 - есть ошибки компиляции/упавшие тесты/сбой запуска,
                 2 - проект открыт в редакторе, но проверить через него нельзя (сервер Pipeline не готов, Play Mode).
    Логи и результаты: Logs\verify\
#>
[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [ValidateSet('compile', 'editmode', 'playmode', 'all')]
    [string]$Mode = 'all',

    [ValidateSet('auto', 'editor', 'batch')]
    [string]$Via = 'auto',

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

# --- режим «через открытый редактор» (Unity CLI + com.unity.pipeline) ---

$env:UNITY_NO_BANNER = '1'
$env:UNITY_NO_PAGER = '1'
$env:UNITY_NON_INTERACTIVE = '1'

function Invoke-UnityCli([string[]]$CliArgs, [string]$SaveTo) {
    $raw = (& unity @CliArgs 2>$null) -join "`n"
    if ($SaveTo) { $raw | Set-Content -Encoding utf8 $SaveTo }
    try { return $raw | ConvertFrom-Json } catch { return $null }
}

function Get-EditorState {
    # $null - CLI нет или редактор с этим проектом не отвечает через Pipeline.
    if (-not (Get-Command unity -ErrorAction SilentlyContinue)) { return $null }
    $status = Invoke-UnityCli @('status', '--project-path', $ProjectPath, '--json') $null
    if ($null -eq $status -or -not $status.success -or $status.data.count -lt 1) { return $null }
    return $status.data.instances[0]
}

function Format-CliError($e) {
    if ($e -is [string]) { return $e }
    $where = if ($e.file) { "$($e.file)($($e.line)): " } else { '' }
    $text = if ($e.message) { $e.message } else { ($e | ConvertTo-Json -Compress) }
    return "$where$text"
}

function Invoke-EditorCompile {
    Write-Section 'Compile (editor)'
    $log = Join-Path $OutDir 'compile.json'
    $r = Invoke-UnityCli @('recompile', '--project-path', $ProjectPath, '--timeout', '300', '--json') $log
    if ($null -eq $r) {
        return [pscustomobject]@{ Step = 'compile'; Ok = $false; Errors = @('Unity CLI не вернул ответ (см. compile.json)'); Log = $log; Summary = 'нет ответа от редактора' }
    }

    $errors = @($r.data.errors | ForEach-Object { Format-CliError $_ })
    $warnings = [int]$r.data.summary.warnings
    $ok = $r.success -and -not $r.data.failed -and $errors.Count -eq 0
    return [pscustomobject]@{
        Step = 'compile'; Ok = $ok; Errors = $errors; Log = $log
        Summary = "ошибок компиляции: $($errors.Count), предупреждений: $warnings, статус: $($r.data.status)"
    }
}

function New-TestResult([string]$Step, $Summary, $Results, [string]$Log, [string]$RunError) {
    $failed = @($Results | Where-Object { $_.Status -eq 'Failed' } | ForEach-Object {
            $msg = if ($_.Message) { ($_.Message -split "`n")[0].Trim() } else { '' }
            "$($_.FullName): $msg"
        })
    # Доступ к свойствам в PowerShell не зависит от регистра: Total и total (ответы test run / test status) - одно поле.
    $total = [int]$Summary.Total
    $passed = [int]$Summary.Passed
    $failedCount = [int]$Summary.Failed
    $skipped = [int]$Summary.Skipped + [int]$Summary.Inconclusive
    $ok = (-not $RunError) -and $failedCount -eq 0
    $text = if ($RunError) { "ошибка запуска: $RunError" } else { "всего: $total, прошло: $passed, упало: $failedCount, пропущено: $skipped" }
    return [pscustomobject]@{
        Step = $Step; Ok = $ok; Total = $total; Passed = $passed; Failed = $failedCount; Skipped = $skipped
        FailedTests = $failed; Errors = @(); Log = $Log; Summary = $text
    }
}

function Invoke-EditorTests([string]$Platform) {
    Write-Section "Tests: $Platform (editor)"
    $log = Join-Path $OutDir "$Platform-results.json"
    $state = Get-EditorState
    if ($null -ne $state) {
        $editor = Invoke-UnityCli @('command', 'editor', 'status', '--project-path', $ProjectPath, '--result-only') $null
        if ($null -ne $editor -and $editor.playMode -ne 'stopped') {
            return New-TestResult $Platform @{} @() $log 'редактор в Play Mode - выйди из него (тесты в чужом Play Mode не запускаем)'
        }
    }

    if ($Platform -eq 'editmode') {
        $r = Invoke-UnityCli @('command', 'test', 'run', '--project-path', $ProjectPath, '--result-only', '--timeout', "$($TimeoutMinutes * 60)", '--', '--mode', 'editor') $log
        if ($null -eq $r -or -not $r.success) {
            $err = if ($null -ne $r -and $r.error) { $r.error } else { 'нет ответа от редактора' }
            return New-TestResult $Platform @{} @() $log $err
        }
        return New-TestResult $Platform $r.Summary $r.Results $log $null
    }

    # PlayMode: вход в Play Mode перезагружает домен и рвёт синхронный запрос, поэтому - асинхронно + опрос статуса.
    $start = Invoke-UnityCli @('command', 'test', 'run', '--project-path', $ProjectPath, '--result-only', '--', '--mode', 'playmode', '--async_tests', 'true') $null
    if ($null -eq $start -or -not $start.success) {
        $err = if ($null -ne $start -and $start.error) { $start.error } else { 'не удалось запустить PlayMode-тесты' }
        return New-TestResult $Platform @{} @() $log $err
    }

    $deadline = (Get-Date).AddMinutes($TimeoutMinutes)
    do {
        Start-Sleep -Seconds 2
        $s = Invoke-UnityCli @('command', 'test', 'status', '--project-path', $ProjectPath, '--result-only') $log
        $status = if ($null -ne $s) { "$($s.status)" } else { '' }
    } while ($status -notin @('completed', 'failed', 'error', 'cancelled') -and (Get-Date) -lt $deadline)

    if ($status -ne 'completed' -and $status -ne 'failed') {
        $err = if ($status) { "статус: $status" } else { "нет результата за $TimeoutMinutes мин" }
        return New-TestResult $Platform @{} @() $log $err
    }
    return New-TestResult $Platform $s.summary $s.results $log $null
}

function Get-EditorConsoleNotes {
    $c = Invoke-UnityCli @('command', 'console', 'status', '--project-path', $ProjectPath, '--result-only') (Join-Path $OutDir 'console.json')
    if ($null -eq $c) { return @('console: нет ответа') }
    $notes = @("console: ошибок $($c.groundTruth.consoleErrors), предупреждений $($c.groundTruth.consoleWarnings), compilationFailed: $($c.groundTruth.compilationFailed)")
    if ([int]$c.groundTruth.consoleErrors -gt 0) {
        $tail = Invoke-UnityCli @('command', 'console', 'get', '--project-path', $ProjectPath, '--result-only', '--', '--tail', '10', '--level', 'error') $null
        foreach ($e in @($tail.entries)) {
            $notes += "    console error: $(($e.message -split "`n")[0])"
        }
    }
    return $notes
}

# --- main ---

$script:Locked = $false
$script:LockReason = ''
$script:Notes = @()
$useEditor = $false

$editorOpen = Test-ProjectLocked
if ($Via -eq 'editor' -or ($Via -eq 'auto' -and $editorOpen)) {
    $state = Get-EditorState
    if ($null -ne $state -and $state.state -eq 'ready') {
        $useEditor = $true
    } else {
        $why = if (-not (Get-Command unity -ErrorAction SilentlyContinue)) { 'нет Unity CLI (unity) в PATH' }
               elseif ($null -eq $state) { 'сервер Pipeline в редакторе не отвечает (переключись на окно Unity, проверь пакет com.unity.pipeline)' }
               else { "редактор в состоянии '$($state.state)'" }
        Write-Host "PROJECT LOCKED: проект открыт в редакторе, а проверить через него нельзя: $why. Не обхожу - переключись на Unity или закрой его." -ForegroundColor Yellow
        exit 2
    }
}

if (-not $useEditor -and -not (Test-Path $UnityExe)) {
    Write-Host "Unity.exe не найден: $UnityExe. Укажи -UnityExe или переменную UNITY_EXE." -ForegroundColor Red
    exit 1
}

if (-not $useEditor -and $editorOpen) {
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
    $r = if ($useEditor) {
        if ($s -eq 'compile') { Invoke-EditorCompile } else { Invoke-EditorTests $s }
    } else {
        if ($s -eq 'compile') { Invoke-Compile } else { Invoke-Tests $s }
    }
    $results += $r
    if ($script:Locked) { break }
    # Без компиляции тесты гонять бессмысленно.
    if ($s -eq 'compile' -and -not $r.Ok) { break }
}

if ($useEditor) { $script:Notes += Get-EditorConsoleNotes }

$viaText = if ($useEditor) { "editor (Unity CLI, pid $($state.pid), port $($state.port))" } else { 'batchmode' }
$lines = @("verify $Mode  $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')  via: $viaText", '')
foreach ($r in $results) {
    $lines += "[$(if ($r.Ok) { 'OK  ' } else { 'FAIL' })] $($r.Step): $($r.Summary)"
    foreach ($e in $r.Errors) { $lines += "    $e" }
    if ($r.PSObject.Properties['FailedTests']) { foreach ($t in $r.FailedTests) { $lines += "    FAILED $t" } }
    $lines += "    log: $($r.Log)"
}

foreach ($n in $script:Notes) { $lines += $n }

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
