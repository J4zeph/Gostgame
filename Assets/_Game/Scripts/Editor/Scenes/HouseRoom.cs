using UnityEngine;

namespace Game.Editor.Scenes
{
    /// <summary>
    /// Описание комнаты дома для сборщика: границы по осям стен, дверной проём и лампы.
    /// Дверь открывается внутрь комнаты (в сторону DoorForward), выключатель - у проёма со стороны защёлки.
    /// </summary>
    public readonly struct HouseRoom
    {
        public readonly string Name;

        /// <summary>Прямоугольник XZ по осевым линиям стен (x = X, y = Z).</summary>
        public readonly Rect Bounds;

        /// <summary>Центр проёма на уровне пола, на оси стены.</summary>
        public readonly Vector3 DoorwayCenter;

        /// <summary>Поворот двери вокруг Y: при 0 дверь в стене вдоль X и открывается в +Z.</summary>
        public readonly float DoorYaw;

        /// <summary>Позиции ламп на плане (x, z).</summary>
        public readonly Vector2[] Lamps;

        public HouseRoom(string name, Rect bounds, Vector3 doorwayCenter, float doorYaw, params Vector2[] lamps)
        {
            Name = name;
            Bounds = bounds;
            DoorwayCenter = doorwayCenter;
            DoorYaw = doorYaw;
            Lamps = lamps;
        }

        public Quaternion DoorRotation => Quaternion.Euler(0f, DoorYaw, 0f);

        /// <summary>Из проёма внутрь комнаты.</summary>
        public Vector3 DoorForward => DoorRotation * Vector3.forward;

        /// <summary>Вдоль стены от петли к защёлке.</summary>
        public Vector3 DoorRight => DoorRotation * Vector3.right;
    }
}
