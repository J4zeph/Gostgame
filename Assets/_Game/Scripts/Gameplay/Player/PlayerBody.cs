using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Gameplay.Player
{
    /// <summary>
    /// Видимое тело игрока. Другие игроки видят его целиком; сам владелец - только тень,
    /// иначе камера оказалась бы внутри собственной капсулы.
    /// </summary>
    public sealed class PlayerBody : MonoBehaviour
    {
        [SerializeField] private Renderer[] _renderers = System.Array.Empty<Renderer>();

        /// <summary>Для сборки из кода (тесты, editor-скрипты).</summary>
        public void Init(Renderer[] renderers)
        {
            _renderers = renderers ?? System.Array.Empty<Renderer>();
        }

        /// <param name="firstPerson">true - это свой игрок: тело не рисуем, оставляем только тень.</param>
        public void SetFirstPerson(bool firstPerson)
        {
            var mode = firstPerson ? ShadowCastingMode.ShadowsOnly : ShadowCastingMode.On;
            foreach (var bodyRenderer in _renderers)
            {
                if (bodyRenderer != null)
                {
                    bodyRenderer.shadowCastingMode = mode;
                }
            }
        }
    }
}
