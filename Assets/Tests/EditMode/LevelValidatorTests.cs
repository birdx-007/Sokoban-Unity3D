using BoxPushing.Core;
using NUnit.Framework;

namespace BoxPushing.Tests
{
    public class LevelValidatorTests
    {
        [Test]
        public void IsValid_ValidLevel_ReturnsTrue()
        {
            var level = new LevelData(
                "Valid",
                new[]
                {
                    "#####",
                    "#.$@#",
                    "#   #",
                    "#####"
                });

            Assert.IsTrue(LevelValidator.IsValid(level));
            Assert.IsEmpty(LevelValidator.GetErrors(level));
        }

        [Test]
        public void IsValid_NoPlayer_ReturnsFalse()
        {
            var level = new LevelData("No player", new[] { "####", "#.$#", "####" });
            Assert.IsFalse(LevelValidator.IsValid(level));
        }

        [Test]
        public void IsValid_BoxGoalMismatch_ReturnsFalse()
        {
            var level = new LevelData("Mismatch", new[] { "####", "#.@#", "####" });
            Assert.IsFalse(LevelValidator.IsValid(level));
        }

        [Test]
        public void IsValid_OpenBoundary_ReturnsFalse()
        {
            var level = new LevelData("Open boundary", new[] { "#####", "#.$@ ", "#####" });
            Assert.IsFalse(LevelValidator.IsValid(level));
        }

        [Test]
        public void IsValid_MultiplePlayers_ReturnsFalse()
        {
            var level = new LevelData("Two players", new[] { "####", "#@@#", "####" });
            Assert.IsFalse(LevelValidator.IsValid(level));
        }

        [Test]
        public void GetErrors_ReportsSpecificReasons()
        {
            var level = new LevelData("Mismatch", new[] { "####", "#.@#", "####" });
            var errors = LevelValidator.GetErrors(level);

            StringAssert.IsMatch(
                "Box count \\(0\\) does not equal goal count \\(1\\).",
                string.Join("\n", errors));
        }

        [Test]
        public void IsValid_UnknownCharacter_ReturnsFalse()
        {
            var level = new LevelData("Unknown", new[] { "######", "#@$.x#", "######" });
            Assert.IsFalse(LevelValidator.IsValid(level));
        }

        [Test]
        public void GetErrors_UnknownCharacter_ReportsPosition()
        {
            var level = new LevelData("Unknown", new[] { "######", "#@$.x#", "######" });
            var errors = string.Join("\n", LevelValidator.GetErrors(level));

            StringAssert.Contains("Unknown XSB character 'x' at column 4, row 1.", errors);
        }

        [Test]
        public void IsValid_DisconnectedFloorRegion_ReturnsFalse()
        {
            var level = new LevelData(
                "Disconnected",
                new[]
                {
                    "#######",
                    "#@#   #",
                    "# #   #",
                    "#######"
                });

            Assert.IsFalse(LevelValidator.IsValid(level));
            StringAssert.Contains(
                "Not all non-wall cells are reachable from the player.",
                string.Join("\n", LevelValidator.GetErrors(level)));
        }

        [Test]
        public void GetErrors_UnreachableBox_ReportsBoxPosition()
        {
            var level = new LevelData(
                "Blocked box",
                new[]
                {
                    "########",
                    "#@$ $..#",
                    "########"
                });

            var errors = string.Join("\n", LevelValidator.GetErrors(level));
            StringAssert.Contains("Box at column 4, row 1 is unreachable by the player.", errors);
        }
    }
}
