using System.Collections;
using Game.Gameplay.Equipment;
using Game.Gameplay.Interaction;
using Game.Gameplay.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Tests.PlayMode
{
    public class PlayerTests
    {
        private const float DoorDistance = 1.5f;
        private const float DoorWidth = 1.2f;
        private const float DoorHeight = 2f;

        private GameObject _floor;
        private GameObject _playerObject;
        private PlayerController _player;
        private PlayerSettings _playerSettings;
        private FakeInputSource _input;
        private PlayerInteractor _interactor;
        private Light _light;
        private Flashlight _flashlight;
        private GameObject _doorObject;
        private Door _door;
        private DoorSettings _doorSettings;
        private FlashlightSettings _flashlightSettings;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _floor.transform.position = new Vector3(0f, -0.5f, 0f);
            _floor.transform.localScale = new Vector3(20f, 1f, 20f);

            _playerSettings = ScriptableObject.CreateInstance<PlayerSettings>();
            _flashlightSettings = ScriptableObject.CreateInstance<FlashlightSettings>();
            _doorSettings = ScriptableObject.CreateInstance<DoorSettings>();

            // Объекты собираются выключенными: Awake/OnEnable отработают после Init, как у настроенного префаба.
            _playerObject = new GameObject("TestPlayer");
            _playerObject.SetActive(false);
            var controller = _playerObject.AddComponent<CharacterController>();
            controller.height = _playerSettings.Height;
            controller.radius = _playerSettings.Radius;
            controller.center = new Vector3(0f, _playerSettings.Height / 2f, 0f);

            var pivot = new GameObject("CameraPivot").transform;
            pivot.SetParent(_playerObject.transform, false);
            pivot.localPosition = new Vector3(0f, _playerSettings.EyeHeight, 0f);

            _player = _playerObject.AddComponent<PlayerController>();
            _player.Init(_playerSettings, pivot);
            _input = new FakeInputSource();
            _player.SetInputSource(_input);

            _interactor = _playerObject.AddComponent<PlayerInteractor>();
            _interactor.Init(_player, pivot);

            var lightObject = new GameObject("Flashlight");
            lightObject.transform.SetParent(pivot, false);
            _light = lightObject.AddComponent<Light>();
            _flashlight = lightObject.AddComponent<Flashlight>();
            _flashlight.Init(_flashlightSettings, _player);

            _playerObject.transform.position = new Vector3(0f, 0.05f, 0f);
            _playerObject.SetActive(true);
            // В игре локального игрока регистрирует PlayerInputReader; здесь ввод поддельный.
            LocalPlayer.Set(_player);

            // Дверь перед игроком: петля у левого края, полотно вправо.
            _doorObject = new GameObject("TestDoor");
            _doorObject.SetActive(false);
            _doorObject.transform.position = new Vector3(-DoorWidth / 2f, 0f, DoorDistance);
            var panel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            panel.transform.SetParent(_doorObject.transform, false);
            panel.transform.localPosition = new Vector3(DoorWidth / 2f, DoorHeight / 2f, 0f);
            panel.transform.localScale = new Vector3(DoorWidth, DoorHeight, 0.05f);
            _door = _doorObject.AddComponent<Door>();
            _door.Init(_doorSettings);
            _doorObject.SetActive(true);

            Physics.SyncTransforms();
            yield return null;
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            LocalPlayer.Clear(_player);
            Object.Destroy(_playerObject);
            Object.Destroy(_doorObject);
            Object.Destroy(_floor);
            Object.Destroy(_playerSettings);
            Object.Destroy(_flashlightSettings);
            Object.Destroy(_doorSettings);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Move_Forward_WalksAlongFacingAndStaysOnFloor()
        {
            // Поворачиваемся спиной к двери, чтобы в неё не упереться.
            _input.Look = new Vector2(180f / _playerSettings.LookSensitivity, 0f);
            yield return null;
            _input.Look = Vector2.zero;

            var start = _playerObject.transform.position;
            _input.Move = Vector2.up;
            yield return new WaitForSeconds(0.5f);
            _input.Move = Vector2.zero;

            var delta = _playerObject.transform.position - start;
            Assert.Less(delta.z, -0.5f, "Игрок не прошёл вперёд (на -Z после разворота)");
            Assert.Less(Mathf.Abs(delta.x), 0.1f, "Игрока увело вбок");
            Assert.Greater(_playerObject.transform.position.y, -0.1f, "Игрок провалился сквозь пол");
        }

        [UnityTest]
        public IEnumerator Look_TurnsBodyAndTiltsHead()
        {
            _input.Look = new Vector2(300f, 200f);
            yield return null;
            _input.Look = Vector2.zero;

            var sensitivity = _playerSettings.LookSensitivity;
            Assert.AreEqual(300f * sensitivity, _playerObject.transform.eulerAngles.y, 0.01f);
            Assert.AreEqual(-200f * sensitivity, _player.Pitch, 0.01f);
            Assert.AreEqual(-200f * sensitivity, Mathf.DeltaAngle(0f, _player.CameraPivot.localEulerAngles.x), 0.01f);
        }

        [UnityTest]
        public IEnumerator Interact_FacingDoor_OpensItFully()
        {
            _input.PressInteract();
            yield return null;

            Assert.IsTrue(_door.IsOpen, "Дверь не получила команду открыться");

            var swingTime = Mathf.Abs(_doorSettings.OpenAngle) / _doorSettings.Speed;
            yield return new WaitForSeconds(swingTime + 0.3f);

            Assert.AreEqual(_door.OpenAngle, _door.Angle, 0.01f);
            Assert.AreEqual(_door.OpenAngle, Mathf.DeltaAngle(0f, _doorObject.transform.localEulerAngles.y), 0.1f);
            Assert.IsFalse(_door.enabled, "Дверь продолжает обновляться после остановки");
        }

        [UnityTest]
        public IEnumerator Interact_LookingAway_DoesNothing()
        {
            _input.Look = new Vector2(180f / _playerSettings.LookSensitivity, 0f);
            yield return null;
            _input.Look = Vector2.zero;

            _input.PressInteract();
            yield return null;

            Assert.IsFalse(_door.IsOpen);
        }

        [UnityTest]
        public IEnumerator LookingAtDoor_FocusesItWithOpenPrompt_ThenClosePromptAfterOpening()
        {
            yield return null;

            Assert.AreSame(_door, _interactor.Focus, "Дверь перед игроком не в фокусе");
            Assert.AreEqual(_doorSettings.OpenPrompt, _interactor.Focus.Prompt);

            var events = 0;
            _interactor.FocusChanged += _ => events++;
            _input.PressInteract();
            yield return null;

            Assert.AreEqual(_doorSettings.ClosePrompt, _interactor.Focus.Prompt);
            Assert.GreaterOrEqual(events, 1, "HUD не узнал о смене подписи");
        }

        [UnityTest]
        public IEnumerator LookingAway_ClearsFocus()
        {
            yield return null;
            Assert.IsNotNull(_interactor.Focus);

            IInteractable reported = _door;
            _interactor.FocusChanged += focus => reported = focus;
            _input.Look = new Vector2(180f / _playerSettings.LookSensitivity, 0f);
            yield return null;
            _input.Look = Vector2.zero;
            yield return null;

            Assert.IsNull(_interactor.Focus);
            Assert.IsNull(reported);
        }

        [UnityTest]
        public IEnumerator RemotePlayer_DoesNotTrackFocus()
        {
            LocalPlayer.Clear(_player);
            yield return null;

            Assert.IsNull(_interactor.Focus, "Фокус (для HUD) нужен только локальному игроку");
        }

        [UnityTest]
        public IEnumerator FlashlightKey_TogglesLight()
        {
            Assert.IsFalse(_light.enabled, "Фонарик должен стартовать выключенным");

            _input.PressFlashlight();
            yield return null;
            Assert.IsTrue(_flashlight.IsOn);
            Assert.IsTrue(_light.enabled);

            _input.PressFlashlight();
            yield return null;
            Assert.IsFalse(_light.enabled);
        }
    }
}
