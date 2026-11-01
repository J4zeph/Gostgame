<#
.SYNOPSIS
    Запуск статического editor-метода Unity в batchmode (-executeMethod).

.EXAMPLE
    pwsh -NoProfile -ExecutionPolicy Bypass -File Tools\unity-exec.ps1 Game.Editor.Scenes.GreyboxRoomBuilder.BuildFromCommandLine

.NOTES
    Метод должен быть public static, без параметров, синхронный; ошибку сообщает исключением.
    Коды выхода: 0 - успех, 1 - метод упал / ошибки компиляции / нет метода, 2 - проект открыт в редакторе.
    Лог: Logs\exec\<Метод>.log
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory, Position = 0)]
    [ValidatePattern('^[A-Za-z_][\w.]*\.[A-Za-z_]\w*$')]
    [string]$Method,

    [string]$UnityExe = $(if ($env:UNITY_EXE) { $env:UNITY_EXE } else { 'C:\Program Files\Unity\Hub\Editor\6000.6.7f1\Editor\Unity.exe' }),

    [int]$TimeoutMinutes = 20
)

$ErrorActionPreference = 'Stop'

$ProjectPath = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$OutDir = Join-Path $ProjectPath 'Logs\exec'
New-Item -ItemType Directory -Force $OutDir | Out-Null
$LogFile = Join-Path $OutDir "$Method.log"

if (-not (Test-Path $UnityExe)) {
    Write-Host "Unity.exe не найден: $UnityExe" -ForegroundColor Red
    exit 1
}

$lock = Join-Path $ProjectPath 'Temp\UnityLockfile'
if (Test-Path $lock) {
    try { [System.IO.File]::Open($lock, 'Open', 'ReadWrite', 'None').Close() }
    catch {
        Write-Host 'PROJECT LOCKED: проект открыт в редакторе Unity. Закрой Unity и запусти снова.' -ForegroundColor Yellow
        exit 2
    }
}

$argLine = "-batchmode -nographics -quit -projectPath `"$ProjectPath`" -logFile `"$LogFile`" -executeMethod $Method"
Write-Host "Unity $argLine" -ForegroundColor DarkGray
$p = Start-Process -FilePath $UnityExe -ArgumentList $argLine -PassThru -NoNewWindow
if (-not $p.WaitForExit($TimeoutMinutes * 60 * 1000)) {
    $p.Kill()
    Write-Host "Unity не завершился за $TimeoutMinutes мин. Лог: $LogFile" -ForegroundColor Red
    exit 1
}

$compileErrors = @(Select-String -Path $LogFile -Pattern 'error CS\d{4}' | ForEach-Object { $_.Line.Trim() } | Sort-Object -Unique)
$methodErrors = @(Select-String -Path $LogFile -Pattern 'executeMethod method .* threw exception|could not be found|Exception:' | ForEach-Object { $_.Line.Trim() } | Select-Object -First 10)
$ok = ($p.ExitCode -eq 0) -and ($compileErrors.Count -eq 0)

Write-Host ''
Write-Host "[$(if ($ok) { 'OK  ' } else { 'FAIL' })] $Method  exit code: $($p.ExitCode)"
foreach ($e in $compileErrors) { Write-Host "    $e" -ForegroundColor Red }
if (-not $ok) { foreach ($e in $methodErrors) { Write-Host "    $e" -ForegroundColor Red } }
Write-Host "    log: $LogFile"

if ($ok) { exit 0 } else { exit 1 }
