using UnityEngine;

namespace Game.Gameplay.Interaction
{
    /// <summary>
    /// Объект, с которым игрок взаимодействует клавишей действия (дверь, выключатель, предмет).
    /// </summary>
    public interface IInteractable
    {
        void Interact(GameObject interactor);
    }
}
