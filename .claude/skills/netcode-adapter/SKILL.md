---
name: netcode-adapter
description: Как сделать сущность сетевой в Gostgame (NGO 2.13) - тонкий адаптер в Network/ над логикой из Gameplay: запрос клиента на хост, проверка на хосте, NetworkVariable с записью сервером, регистрация префаба, хеш NetworkObject. Использовать при любой задаче «синхронизировать X по сети», новом сетевом префабе или объекте сцены, новом RPC.
---

# netcode-adapter

Правила - в CLAUDE.md («сеть - тонкий слой», «авторитет у хоста»). Здесь - как это делается в нашем коде. Образцы: `Network/Interaction/NetworkDoor.cs`, `Network/Equipment/NetworkFlashlight.cs`, `Network/Player/NetworkPlayer.cs`, `Network/Session/NetworkSessionService.cs`.

## Шаблон: состояние, которое решает хост

**1. Gameplay (без NGO)** - компонент умеет работать в двух режимах:

```csharp
public event Action<GameObject> InteractRequested;      // «попросили» (в сетевом режиме)
public bool HandleInteractLocally { get; set; } = true;  // true - одиночная игра и тесты

public void Interact(GameObject who)
{
    if (HandleInteractLocally) _model.Toggle();
    else InteractRequested?.Invoke(who);
}

public void SetOpen(bool open) => _model.SetOpen(open);  // сюда сеть кладёт решение хоста
```

**2. Network/** - адаптер на том же объекте (рядом `NetworkObject`):

```csharp
private readonly NetworkVariable<bool> _isOpen = new NetworkVariable<bool>(
    false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

public override void OnNetworkSpawn()
{
    _door.HandleInteractLocally = false;
    _door.InteractRequested += OnInteractRequested;
    _isOpen.OnValueChanged += OnOpenChanged;
    _door.SetOpen(_isOpen.Value);            // опоздавшие получают текущее состояние
}

public override void OnNetworkDespawn() { /* отписаться, HandleInteractLocally = true */ }

private void OnInteractRequested(GameObject _) => RequestToggleRpc();

[Rpc(SendTo.Server)]
private void RequestToggleRpc(RpcParams rpcParams = default)
{
    if (!CanReach(rpcParams.Receive.SenderClientId)) return;  // клиенту не доверяем
    _isOpen.Value = !_isOpen.Value;
}

private void OnOpenChanged(bool previous, bool current) => _door.SetOpen(current);
```

**Проверки на хосте** (всегда, даже если клиент «честный»):
- предмет игрока - отправитель == `OwnerClientId`;
- объект мира - дистанция: `ReachRules.IsWithinReach(глаз игрока, collider.ClosestPoint(глаз), InteractDistance, NetworkSettings.ReachSlack)`, игрок - `NetworkManager.ConnectedClients[sender].PlayerObject`;
- правила (можно ли сейчас) - чистой функцией из Core, покрытой EditMode-тестом.

**Что куда:**
| Что | Как |
|---|---|
| Постоянное состояние (открыто, включено, заряд) | `NetworkVariable`, запись `Server` |
| Разовый эффект (звук, вспышка, крик) | `[Rpc(SendTo.ClientsAndHost)]` от хоста |
| Запрос игрока | `[Rpc(SendTo.Server)]` + `RpcParams` для отправителя |
| Позиция/поворот своего игрока | `NetworkTransform`, `AuthorityMode = Owner` (исключение из CLAUDE.md) |
| Анимация (угол двери) | не синхронизировать: каждый клиент анимирует сам по состоянию |

## «Мой» и «чужой» игрок

Компоненты, которые должны работать только у владельца (ввод, камера, AudioListener, FOV), в префабе **выключены**, `NetworkPlayer.OnNetworkSpawn` включает их при `IsOwner`. Иначе чужой экземпляр на кадр перехватит ввод/камеру. `PlayerController` без источника ввода стоит на месте - движение чужого приходит из `NetworkTransform`.

## Новый сетевой префаб

1. Собирать editor-скриптом (`unity-editor-scripting`): `NetworkObject` добавлять **первым**, потом NetworkBehaviour'ы.
2. После `SaveAsPrefabAsset` - `NetworkObjectHashUtility.RefreshPrefab(path)` (иначе `GlobalObjectIdHash: 0`, клиент не найдёт префаб).
3. Добавить в наш список `_Game/Data/Network/NetworkPrefabs.asset` (`NetworkAssetsBuilder`). Автогенерируемый `Assets/DefaultNetworkPrefabs.asset` не используем.
4. Тест `NetworkAssetsTests` - добавить путь префаба в `TestCase`.

## Сетевой объект сцены (дверь, выключатель)

`NetworkObject` на объекте в сцене. После сохранения сцены (она должна быть в Build Settings) - `NetworkObjectHashUtility.RefreshScene(scene)` и сохранить ещё раз (так делает `HouseBuilder`). Управление сценами NGO **включено** (`EnableSceneManagement = true`): без него клиент ищет объекты сцены среди префабов, не находит (`Failed to create object locally. NetworkPrefab could not be found`) и не показывает их. Все играют в одной сцене, загруженной до подключения; хост после `StartHost` ставит `SceneManager.SetClientSynchronizationMode(LoadSceneMode.Additive)` (`NetworkSessionService`), чтобы клиент не перезагружал уже открытую сцену. Новые сцены грузит только хост через `NetworkManager.SceneManager`. Тесты на хосте в одном процессе этот случай не ловят - объекты сцены проверять вдвоём (Multiplayer Play Mode; после пересборки сцены перезапустить Player 2).

## Ссылки сборки

`Game.Network` -> `Unity.Netcode.Runtime` (разрешено). `UnityTransport.SetConnectionData` требует ещё `Unity.Networking.Transport` из-за перегрузки - не используем, пишем поле `ConnectionData` напрямую (см. `NetworkSessionService.SetConnection`).

## Тесты

- Правила (лимит игроков, дистанция, кто может) - чистые функции в Core, EditMode.
- Режим «только запрос» у Gameplay-компонента - PlayMode без сети (`RemoteHandlingTests`).
- Адаптер - PlayMode на настоящем хосте в одном процессе (`NetworkDoorTests`): `NetworkManager` + `UnityTransport` из кода, `EnableSceneManagement = false`, свой порт, объекты собраны выключенными -> `Init` -> `SetActive(true)` -> `Spawn()` / `SpawnAsPlayerObject`, в `TearDown` - `Shutdown()`.
- Два игрока вживую - Multiplayer Play Mode (Window > Multiplayer > Multiplayer Play Mode), вручную.

## Чего не делать

- Логику (правила, таймеры, рандом) в Network/ - только синхронизация и проверки доступа.
- Доверять данным из RPC клиента (позиции, «я рядом») - всё берём на хосте.
- `NetworkVariable` с записью Owner для игрового состояния.
- Менять сцену/префаб руками ради NetworkObject - только editor-скриптом.
