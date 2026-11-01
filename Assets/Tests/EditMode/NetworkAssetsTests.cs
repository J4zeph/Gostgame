using System.IO;
using Game.Editor.Prefabs;
using Game.Editor.Scenes;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    /// <summary>
    /// Сгенерированные сетевые ассеты пригодны для сети: у каждого NetworkObject ненулевой GlobalObjectIdHash
    /// (иначе клиент не сопоставит префаб игрока и дверь сцены).
    /// </summary>
    public class NetworkAssetsTests
    {
        private const string HashKey = "GlobalObjectIdHash:";
        private const string ZeroHash = "GlobalObjectIdHash: 0";

        [TestCase(PlayerPrefabBuilder.PrefabPath)]
        [TestCase(HouseBuilder.ScenePath)]
        public void NetworkObjects_HaveNonZeroGlobalObjectIdHash(string assetPath)
        {
            Assert.IsTrue(File.Exists(assetPath), $"Нет файла {assetPath} - запусти генератор");
            var text = File.ReadAllText(assetPath);

            StringAssert.Contains(HashKey, text, "В ассете нет NetworkObject");
            foreach (var line in text.Split('\n'))
            {
                Assert.AreNotEqual(ZeroHash, line.Trim(), $"{assetPath}: NetworkObject с нулевым хешем");
            }
        }
    }
}
