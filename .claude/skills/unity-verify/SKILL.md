---
name: unity-verify
description: Проверка Unity-проекта - компиляция, EditMode- и PlayMode-тесты, консоль - через Tools\verify.ps1. Сам выбирает способ - через открытый редактор (Unity CLI + com.unity.pipeline) или batchmode, если редактор закрыт. Использовать перед сдачей любой работы с кодом (CLAUDE.md требует 0 ошибок компиляции и 0 упавших тестов), после правок C#/asmdef и когда пользователь просит "проверь", "прогони тесты", "скомпилируй".
---

# unity-verify

Единственная точка проверки проекта - `Tools\verify.ps1`. Не запускай Unity/CLI вручную с другими флагами, если скрипт справляется.

## Запуск (PowerShell, из корня проекта)

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File Tools\verify.ps1 all        # компиляция + editmode + playmode
pwsh -NoProfile -ExecutionPolicy Bypass -File Tools\verify.ps1 compile
pwsh -NoProfile -ExecutionPolicy Bypass -File Tools\verify.ps1 editmode
pwsh -NoProfile -ExecutionPolicy Bypass -File Tools\verify.ps1 playmode
```

Эта команда разрешена в `.claude/settings.json` - подтверждение не нужно.

Параметры: `-Via auto|editor|batch` (по умолчанию `auto`), `-UnityExe <путь>` (или `UNITY_EXE`), `-TimeoutMinutes <n>` (30), `-ShutdownGraceSeconds <n>` (90, только batchmode).

## Как выбирается способ (`-Via auto`)

| Редактор с проектом | Что делает скрипт |
|---|---|
| закрыт | **batchmode**: `Unity.exe -batchmode ...` (первый прогон на чистом `Library/` - несколько минут; ставь таймаут вызова 600000 мс или запускай в фоне) |
| открыт, `unity status` -> `ready` | **editor**: через Unity CLI в этом же редакторе (секунды, редактор не закрывается) |
| открыт, но Pipeline не отвечает / не `ready` | выход **2** (LOCKED) с причиной. **Не обходить**: попросить пользователя переключиться на окно Unity (пакеты подхватываются только при активном окне) или закрыть его |

Проверить состояние руками: `unity status --project-path "<проект>"` (табличный вывод без фильтра иногда пустой - смотри с `--project-path` или `--json`).

## Режим editor (Unity CLI, пакет `com.unity.pipeline`)

1. **compile**: `unity recompile --project-path <p> --json` - `data.errors`, `data.summary.warnings`, `data.status` (`up_to_date`/`completed`). Ответ -> `Logs\verify\compile.json`.
2. Перед тестами - `unity command editor status --result-only`: если `playMode != stopped`, тесты **не запускаем** (это Play Mode пользователя) - шаг FAIL с объяснением.
3. **editmode**: `unity command test run --result-only -- --mode editor` (синхронно) -> `Summary`, `Results[].Status/FullName/Message`. -> `editmode-results.json`.
4. **playmode**: синхронно нельзя (вход в Play Mode перезагружает домен и рвёт HTTP-запрос). `test run -- --mode playmode --async_tests true`, потом опрос `unity command test status --result-only` каждые 2 с до `completed`. -> `playmode-results.json`.
5. **console**: `unity command console status --result-only` -> счётчики ошибок/предупреждений и `compilationFailed`; если ошибки есть - последние 10 через `console get -- --tail 10 --level error`. Попадает в сводку строкой `console: ...` (на зелёный не влияет, но в отчёте упомянуть).

`unity test` (без `command`) с открытым редактором **не работает** - он сам запускает batchmode и отказывается, если проект открыт. Поэтому в режиме editor используется `unity command test run`.

## Режим batchmode

1. Если `Temp\UnityLockfile` занят - см. таблицу выше.
2. **compile**: `Unity.exe -batchmode -nographics -quit -projectPath <p> -logFile Logs\verify\compile.log`, ошибки - строки `error CS####`.
3. **editmode/playmode**: `-runTests -testPlatform editmode|playmode -testResults Logs\verify\<platform>-results.xml` (без `-quit`). Разбор NUnit XML: `test-run` total/passed/failed/skipped, упавшие - `//test-case[@result='Failed']`.
4. **Зависание на выходе**: если в логе есть маркер завершения (`Batchmode quit successfully invoked` / `Test run completed. Exiting with code N`), а процесс жив ещё 90 с - скрипт завершает его и берёт код из маркера (`note: Unity завис при выходе...`). Старый лог удаляется перед запуском.

## Коды выхода

| Код | Значение | Что делать |
|---|---|---|
| 0 | GREEN | Работа проверена |
| 1 | RED: ошибки компиляции, упавшие тесты, сбой запуска, Play Mode | Читай `Logs\verify\summary.txt`, чини, перезапускай |
| 2 | LOCKED: редактор открыт, а проверить через него нельзя | **Не обходить.** Сказать пользователю переключиться на Unity / закрыть его |

## Файлы в `Logs\verify\` (в .gitignore)

- `summary.txt` - итог, читать первым; первая строка говорит, каким способом шла проверка (`via: editor ...` / `via: batchmode`)
- editor: `compile.json`, `editmode-results.json`, `playmode-results.json`, `console.json`
- batchmode: `compile.log`, `editmode.log`, `playmode.log`, `*-results.xml`

## "Зелёный" =

- 0 ошибок компиляции;
- 0 упавших тестов в editmode и playmode, без ошибки запуска;
- `RESULT: GREEN`, код выхода 0.

0 тестов в платформе - не ошибка, но если добавлял правило в Core/Gameplay, а EditMode-тестов не прибавилось - тест не подхватился (проверь asmdef тестов).

## Отчёт пользователю

По `work-report`: строки `[OK]/[FAIL]` с цифрами, способ (`via`), строка `console:`. При RED - конкретные ошибки и упавшие тесты с сообщением. Не писать "зелёный", пока скрипт не вернул 0.

## Чего не делать

- Не удалять `Temp\UnityLockfile`, не закрывать и не убивать редактор пользователя, чтобы обойти блокировку.
- Не запускать тесты, пока пользователь в Play Mode.
- Не трогать `Library/` "для чистой сборки" без просьбы пользователя.
- `unity command package ...` и `script eval/run` - только с подтверждением (меняют пакеты / выполняют произвольный код).
