using System.Collections;
using Game.Gameplay.Equipment;
using Game.Gameplay.Interaction;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Tests.PlayMode
{
    /// <summary>
    /// Режим «решает не объект, а хост»: Interact/Toggle только просят, состояние меняет SetOpen/SetOn.
    /// </summary>
    public class RemoteHandlingTests
    {
        private GameObject _doorObject;
        private Door _door;
        private DoorSettings _doorSettings;
        private GameObject _flashlightObject;
        private Flashlight _flashlight;
        private Light _light;
        private FlashlightSettings _flashlightSettings;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _doorSettings = ScriptableObject.CreateInstance<DoorSettings>();
            _doorObject = new GameObject("Door");
            _doorObject.SetActive(false);
            _door = _doorObject.AddComponent<Door>();
            _door.Init(_doorSettings);
            _doorObject.SetActive(true);

            _flashlightSettings = ScriptableObject.CreateInstance<FlashlightSettings>();
            _flashlightObject = new GameObject("Flashlight");
            _flashlightObject.SetActive(false);
            _light = _flashlightObject.AddComponent<Light>();
            _flashlight = _flashlightObject.AddComponent<Flashlight>();
            _flashlight.Init(_flashlightSettings, null);
            _flashlightObject.SetActive(true);
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Object.Destroy(_doorObject);
            Object.Destroy(_flashlightObject);
            Object.Destroy(_doorSettings);
            Object.Destroy(_flashlightSettings);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Door_NotHandledLocally_OnlyRequests_ThenOpensOnSetOpen()
        {
            _door.HandleInteractLocally = false;
            GameObject requestedBy = null;
            _door.InteractRequested += who => requestedBy = who;
            var interactor = new GameObject("Interactor");

            _door.Interact(interactor);
            yield return null;

            Assert.AreSame(interactor, requestedBy);
            Assert.IsFalse(_door.IsOpen, "Дверь не должна открываться без решения хоста");

            _door.SetOpen(true);
            Assert.IsTrue(_door.IsOpen);
            Object.Destroy(interactor);
        }

        [UnityTest]
        public IEnumerator Flashlight_NotHandledLocally_OnlyRequests_ThenSwitchesOnSetOn()
        {
            _flashlight.HandleToggleLocally = false;
            var requests = 0;
            _flashlight.ToggleRequested += () => requests++;

            _flashlight.Toggle();
            yield return null;

            Assert.AreEqual(1, requests);
            Assert.IsFalse(_light.enabled);

            _flashlight.SetOn(true);
            Assert.IsTrue(_light.enabled);
        }

        [UnityTest]
        public IEnumerator LightSwitch_NotHandledLocally_OnlyRequests_ThenLightsOnSetOn()
        {
            var settings = ScriptableObject.CreateInstance<LightSwitchSettings>();
            var lampObject = new GameObject("Lamp");
            var lamp = lampObject.AddComponent<Light>();
            var switchObject = new GameObject("LightSwitch");
            switchObject.SetActive(false);
            var lightSwitch = switchObject.AddComponent<LightSwitch>();
            lightSwitch.Init(settings, new[] { lamp });
            switchObject.SetActive(true);

            Assert.IsFalse(lamp.enabled, "По умолчанию свет в доме выключен");

            lightSwitch.HandleInteractLocally = false;
            var requests = 0;
            lightSwitch.InteractRequested += _ => requests++;

            lightSwitch.Interact(switchObject);
            yield return null;

            Assert.AreEqual(1, requests);
            Assert.IsFalse(lamp.enabled, "Свет не должен включаться без решения хоста");

            lightSwitch.SetOn(true);
            Assert.IsTrue(lamp.enabled);

            Object.Destroy(switchObject);
            Object.Destroy(lampObject);
            Object.Destroy(settings);
        }

        [UnityTest]
        public IEnumerator LightSwitch_HandledLocally_TogglesAllLamps()
        {
            var settings = ScriptableObject.CreateInstance<LightSwitchSettings>();
            var lamps = new[] { new GameObject("Lamp1").AddComponent<Light>(), new GameObject("Lamp2").AddComponent<Light>() };
            var switchObject = new GameObject("LightSwitch");
            switchObject.SetActive(false);
            var lightSwitch = switchObject.AddComponent<LightSwitch>();
            lightSwitch.Init(settings, lamps);
            switchObject.SetActive(true);
            yield return null;

            lightSwitch.Interact(switchObject);

            Assert.IsTrue(lightSwitch.IsOn);
            Assert.IsTrue(lamps[0].enabled && lamps[1].enabled, "Выключатель зажигает все лампы комнаты");
            Assert.AreEqual(settings.TurnOffPrompt, lightSwitch.Prompt);

            Object.Destroy(switchObject);
            Object.Destroy(lamps[0].gameObject);
            Object.Destroy(lamps[1].gameObject);
            Object.Destroy(settings);
        }
    }
}
