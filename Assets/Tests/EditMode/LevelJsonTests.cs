using BoxPushing.Core;
using NUnit.Framework;

namespace BoxPushing.Tests
{
    public class LevelJsonTests
    {
        [Test]
        public void RoundTrip_PreservesNameAndMap()
        {
            var original = new LevelData(
                "Level 01",
                new[]
                {
                    "#####",
                    "#.$@#",
                    "#   #",
                    "#####"
                });

            string json = LevelJson.ToJson(original);
            LevelData restored = LevelJson.FromJson(json);

            Assert.AreEqual(original.name, restored.name);
            CollectionAssert.AreEqual(original.map, restored.map);
        }

        [Test]
        public void ToJson_ContainsNameAndMapFields()
        {
            var level = new LevelData("A", new[] { "###", "#.#", "###" });
            string json = LevelJson.ToJson(level);

            StringAssert.Contains("\"name\":\"A\"", json);
            StringAssert.Contains("\"map\"", json);
        }
    }
}
