using System;
using UnityEngine;

namespace Game.Gameplay.Interaction
{
    /// <summary>
    /// Объект, с которым игрок взаимодействует клавишей действия (дверь, выключатель, предмет).
    /// </summary>
    public interface IInteractable
    {
        /// <summary>Что произойдёт при взаимодействии - подпись у значка («Открыть», «Включить»).</summary>
        string Prompt { get; }

        /// <summary>Подпись сменилась (дверь открыли - теперь «Закрыть»).</summary>
        event Action PromptChanged;

        void Interact(GameObject interactor);
    }
}
