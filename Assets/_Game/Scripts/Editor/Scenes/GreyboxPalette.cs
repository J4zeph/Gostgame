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
        public readonly Material Ground;
        public readonly Material Furniture;
        public readonly Material Fixture;

        public GreyboxPalette(Material floor, Material wall, Material ceiling, Material door,
            Material ground, Material furniture, Material fixture)
        {
            Floor = floor;
            Wall = wall;
            Ceiling = ceiling;
            Door = door;
            Ground = ground;
            Furniture = furniture;
            Fixture = fixture;
        }
    }
}
