---
name: unity-editor-scripting
description: Как менять сцены, префабы, материалы, ScriptableObject-ассеты и настройки проекта через editor-скрипты Unity и запускать их в batchmode (Tools\unity-exec.ps1). Использовать каждый раз, когда задача требует создать или изменить .unity/.prefab/.asset/.mat или Build Settings - руками эти файлы править запрещено (CLAUDE.md).
---

# unity-editor-scripting

`.unity`, `.prefab`, `.asset`, `.mat`, `.meta`, `ProjectSettings/*` руками не правим - это YAML с GUID-ссылками. Вместо этого пишем editor-скрипт, который делает изменение через API Unity, и запускаем его.

## Где и как писать

- Папка: `Assets/_Game/Scripts/Editor/<Подпапка>/`, сборка `Game.Editor` (только редактор).
- Пространство имён: `Game.Editor.<Подпапка>` (например `Game.Editor.Scenes`, `Game.Editor.Prefabs`, `Game.Editor.Data`).
- Один `public static class` на инструмент. В нём:
  - `[MenuItem("Tools/Gostgame/<Действие>")] public static void ...FromMenu()` - для пользователя в редакторе. Перед заменой сцены: `EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()`.
  - `public static void ...FromCommandLine()` - для batchmode: без параметров, без диалогов, синхронно.
  - Публичный метод, который строит содержимое под переданным `Transform` и **не трогает ассеты** - его вызывают тесты.
- Пути к ассетам и имена ключевых объектов - `public const` в классе, чтобы тесты и другие инструменты ссылались на них, а не дублировали строки.
- Ошибка = исключение (`throw`). Unity с `-quit` вернёт ненулевой код, `unity-exec.ps1` покажет FAIL. Не глотать исключения, не `Debug.LogError` вместо `throw`.
- Итог работы - одна строка `Debug.Log("[ИмяИнструмента] ...")`, по ней видно результат в логе.

## Идемпотентность

Повторный запуск должен давать тот же результат, а не дубли:
- Сцену пересобирать целиком: `EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single)` -> построить -> `SaveScene(scene, path)`.
- Ассет: `AssetDatabase.LoadAssetAtPath<T>(path)`; нет - `CreateAsset`, есть - обновить поля. После изменения `EditorUtility.SetDirty` + `AssetDatabase.SaveAssets()`.
- Папки: `AssetDatabase.IsValidFolder` / `CreateFolder` (не `Directory.CreateDirectory` - Unity не создаст `.meta` вовремя).
- Build Settings: добавлять сцену, только если её там нет.
- Перенос/переименование ассета - только `AssetDatabase.MoveAsset` / `RenameAsset` (переносит и `.meta`).

## Полезный API

| Задача | API |
|---|---|
| Новая/открыть/сохранить сцену | `EditorSceneManager.NewScene / OpenScene / SaveScene` |
| Примитив | `GameObject.CreatePrimitive(PrimitiveType.Cube)` |
| Префаб из объекта | `PrefabUtility.SaveAsPrefabAsset(go, path)` |
| Правка префаба | `PrefabUtility.LoadPrefabContents(path)` -> изменить -> `SaveAsPrefabAsset` -> `UnloadPrefabContents` |
| ScriptableObject | `ScriptableObject.CreateInstance<T>()` + `AssetDatabase.CreateAsset` |
| Материал URP | `new Material(Shader.Find("Universal Render Pipeline/Lit"))`, цвет `_BaseColor` |
| Сериализованные приватные поля | `new SerializedObject(obj)` -> `FindProperty("_field")` -> `ApplyModifiedPropertiesWithoutUndo()` |
| Build Settings | `EditorBuildSettings.scenes` |

## Запуск

Пользователь - из меню `Tools/Gostgame/...` (если редактор открыт).

Агент - в batchmode (редактор должен быть закрыт):

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File Tools\unity-exec.ps1 Game.Editor.Scenes.GreyboxRoomBuilder.BuildFromCommandLine
```

Коды выхода: 0 - успех, 1 - метод упал / ошибки компиляции / метод не найден, 2 - проект открыт в редакторе (не обходить, сказать пользователю закрыть Unity). Лог: `Logs\exec\<Метод>.log`.

## Проверка результата

1. `unity-exec.ps1` вернул 0, в логе есть строка `[ИмяИнструмента] ...`.
2. Файлы появились там, где ожидалось, вместе с `.meta` (Unity создаёт их сам).
3. EditMode-тест на метод построения: вызвать его под временным `GameObject`, проверить структуру/геометрию (`Transform.Find`, `Physics.SyncTransforms()` + `Physics.CheckCapsule`/`Raycast`), в `TearDown` - `Object.DestroyImmediate`. Тесты не должны создавать ассеты.
4. `unity-verify` (`Tools\verify.ps1 all`) зелёный.
5. Сгенерированные ассеты коммитятся вместе с editor-скриптом, который их создал.

## Существующие инструменты

| Инструмент | Меню | Batchmode-метод | Что делает |
|---|---|---|---|
| `GreyboxRoomBuilder` | Tools/Gostgame/Build Greybox Room | `Game.Editor.Scenes.GreyboxRoomBuilder.BuildFromCommandLine` | Сцена `Assets/_Game/Scenes/Greybox_Room.unity`: комната 8x8x3 м, дверь с пивотом на петле, коридор, 4 точки появления, свет, временная камера; материалы в `_Game/Art/Materials` |

Новый инструмент - добавь строку сюда.
