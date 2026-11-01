---
name: unity-verify
description: Проверка Unity-проекта в batchmode - компиляция, EditMode- и PlayMode-тесты через Tools\verify.ps1, разбор логов и results.xml. Использовать перед сдачей любой работы с кодом (CLAUDE.md требует 0 ошибок компиляции и 0 упавших тестов), после правок C#/asmdef и когда пользователь просит "проверь", "прогони тесты", "скомпилируй".
---

# unity-verify

Единственная точка проверки проекта - скрипт `Tools\verify.ps1`. Не запускай Unity вручную с другими флагами, если скрипт справляется.

## Запуск (PowerShell, из корня проекта)

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File Tools\verify.ps1 all        # компиляция + editmode + playmode
pwsh -NoProfile -ExecutionPolicy Bypass -File Tools\verify.ps1 compile
pwsh -NoProfile -ExecutionPolicy Bypass -File Tools\verify.ps1 editmode
pwsh -NoProfile -ExecutionPolicy Bypass -File Tools\verify.ps1 playmode
```

Параметры: `-UnityExe <путь>` (или переменная `UNITY_EXE`; по умолчанию `C:\Program Files\Unity\Hub\Editor\6000.6.7f1\Editor\Unity.exe`), `-TimeoutMinutes <n>` (по умолчанию 30).

Первый прогон на чистом `Library/` долгий (импорт, несколько минут) - ставь таймаут вызова инструмента 600000 мс или запускай в фоне.

## Коды выхода

| Код | Значение | Что делать |
|---|---|---|
| 0 | GREEN | Работа проверена |
| 1 | RED: ошибки компиляции, упавшие тесты или сбой запуска | Читай `Logs\verify\summary.txt`, чини, перезапускай |
| 2 | LOCKED: проект открыт в редакторе | **Не обходить.** Сказать пользователю закрыть Unity и ждать |

## Что делает скрипт

1. Проверяет блокировку: если `Temp\UnityLockfile` занят открытым редактором - сразу выход 2. Также ловит в логе сообщение Unity о другом запущенном экземпляре.
2. **compile**: `Unity.exe -batchmode -nographics -quit -projectPath <проект> -logFile Logs\verify\compile.log`. Ошибки - строки лога с `error CS####` (без дублей). При ошибках компиляции тесты не запускаются.
3. **editmode / playmode**: `Unity.exe -batchmode -nographics -projectPath <проект> -runTests -testPlatform editmode|playmode -testResults Logs\verify\<platform>-results.xml -logFile Logs\verify\<platform>.log` (без `-quit` - раннер закрывает Unity сам). Exit code раннера: 0 - все прошли, 2 - есть упавшие, 3 и другое - сбой запуска.
4. Разбирает `results.xml` (NUnit 3): атрибуты `test-run` `total/passed/failed/skipped/inconclusive`, упавшие - `//test-case[@result='Failed']` с `fullname` и первой строкой `failure/message`. Если xml не создан - ищет в логе `error CS` (обычно это ошибка компиляции тестовой сборки).
5. Пишет сводку в `Logs\verify\summary.txt` и в stdout.
6. **Зависание Unity на выходе.** Иногда Unity доделывает работу и виснет при остановке (в логе последние строки - `abort_threads: Failed aborting ...`). Скрипт ждёт маркер завершения в логе (`Batchmode quit successfully invoked` или `Test run completed. Exiting with code N`), даёт ещё 90 с (`-ShutdownGraceSeconds`) и завершает процесс сам; код выхода берётся из маркера, в сводке - строка `note: Unity завис при выходе...`. Это не ошибка проекта. Перед запуском старый лог удаляется, чтобы не принять старый маркер за новый.

## Файлы в `Logs\verify\` (папка `Logs/` в .gitignore)

- `summary.txt` - итог, читать первым
- `compile.log`, `editmode.log`, `playmode.log` - полные логи Unity
- `editmode-results.xml`, `playmode-results.xml` - результаты NUnit

## "Зелёный" =

- 0 ошибок компиляции (`error CS`), Unity вышел с кодом 0 на шаге compile;
- 0 упавших тестов в editmode и playmode, раннер вышел с кодом 0;
- в итоге `RESULT: GREEN`, код выхода скрипта 0.

0 тестов в платформе - не ошибка, но если ты добавлял правило в Core/Gameplay, а EditMode-тестов 0 - тест не подхватился (проверь asmdef тестов).

## Отчёт пользователю

Покажи строки `[OK]/[FAIL]` из `summary.txt` с цифрами. При RED - конкретные `error CS` (файл, строка) и имена упавших тестов с сообщением. Не пиши "зелёный", пока скрипт не вернул 0.

## Чего не делать

- Не удалять `Temp\UnityLockfile` и не убивать процесс редактора, чтобы обойти блокировку.
- Не трогать `Library/` "для чистой сборки" без просьбы пользователя.
