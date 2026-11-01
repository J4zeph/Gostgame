using UnityEngine;

namespace Game.Editor.Scenes
{
    /// <summary>
    /// Материалы серой заготовки. Любое поле может быть null - тогда остаётся материал примитива по умолчанию.
    /// </summary>
    public readonly struct GreyboxPalette
    {
        public readonly Material Floor;
        public readonly Material Wall;
        public readonly Material Ceiling;
        public readonly Material Door;

        public GreyboxPalette(Material floor, Material wall, Material ceiling, Material door)
        {
            Floor = floor;
            Wall = wall;
            Ceiling = ceiling;
            Door = door;
        }
    }
}
