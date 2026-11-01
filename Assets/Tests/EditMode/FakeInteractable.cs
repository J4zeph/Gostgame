using System;
using Game.Gameplay.Interaction;
using UnityEngine;

namespace Game.Tests.EditMode
{
    /// <summary>
    /// IInteractable для тестов UI: подпись задаётся полем.
    /// </summary>
    public sealed class FakeInteractable : IInteractable
    {
        public string PromptText;

        public string Prompt => PromptText;

        public event Action PromptChanged;

        public void Interact(GameObject interactor)
        {
            PromptChanged?.Invoke();
        }
    }
}
