using System.Collections.Generic;
using BoxPushing.Core;
using NUnit.Framework;

namespace BoxPushing.Tests
{
    public class XsbParserTests
    {
        [TestCase('#', CellType.Wall)]
        [TestCase(' ', CellType.Floor)]
        [TestCase('.', CellType.Goal)]
        [TestCase('@', CellType.Player)]
        [TestCase('$', CellType.Box)]
        [TestCase('+', CellType.PlayerOnGoal)]
        [TestCase('*', CellType.BoxOnGoal)]
        public void Parse_ReturnsExpectedCellType(char symbol, CellType expected)
        {
            Assert.AreEqual(expected, XsbParser.Parse(symbol));
        }

        [Test]
        public void TryParse_UnknownCharacter_ReturnsFalse()
        {
            Assert.IsFalse(XsbParser.TryParse('x', out _));
        }

        [TestCase('.', true)]
        [TestCase('+', true)]
        [TestCase('*', true)]
        public void IsGoal_IncludesCombinedStates(char symbol, bool expected)
        {
            Assert.AreEqual(expected, XsbParser.IsGoal(symbol));
        }

        [TestCase('@', true)]
        [TestCase('+', true)]
        public void IsPlayer_IncludesPlayerOnGoal(char symbol, bool expected)
        {
            Assert.AreEqual(expected, XsbParser.IsPlayer(symbol));
        }

        [TestCase('$', true)]
        [TestCase('*', true)]
        public void IsBox_IncludesBoxOnGoal(char symbol, bool expected)
        {
            Assert.AreEqual(expected, XsbParser.IsBox(symbol));
        }

        [Test]
        public void LevelData_DerivedQueries_HandlePlusAndStarStates()
        {
            var level = new LevelData(
                "Mixed states",
                new[]
                {
                    "######",
                    "# +* #",
                    "# @$ #",
                    "######"
                });

            Assert.AreEqual(6, level.Width);
            Assert.AreEqual(4, level.Height);

            CollectionAssert.AreEqual(
                new List<GridPosition> { new GridPosition(2, 1), new GridPosition(2, 2) },
                level.PlayerPositions);

            CollectionAssert.AreEqual(
                new List<GridPosition> { new GridPosition(3, 1), new GridPosition(3, 2) },
                level.BoxPositions);

            CollectionAssert.AreEqual(
                new List<GridPosition> { new GridPosition(2, 1), new GridPosition(3, 1) },
                level.GoalPositions);

            Assert.AreEqual((GridPosition?)new GridPosition(2, 1), level.PlayerPosition);
        }
    }
}
