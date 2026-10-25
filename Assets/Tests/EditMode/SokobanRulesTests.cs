using BoxPushing.Core;
using NUnit.Framework;

namespace BoxPushing.Tests
{
    public class SokobanRulesTests
    {
        [Test]
        public void TryMove_ToFloor_SucceedsAndUpdatesFacing()
        {
            var level = new LevelData("Move", new[] { "#####", "#@  #", "#####" });
            var state = new GameState(level);

            MoveResult result = SokobanRules.TryMove(state, Direction.Right);

            Assert.IsTrue(result.Success);
            Assert.IsFalse(result.PushedBox);
            Assert.IsFalse(result.Won);
            Assert.AreEqual(new GridPosition(2, 1), state.PlayerPosition);
            Assert.AreEqual(Direction.Right, state.Facing);
        }

        [Test]
        public void TryMove_IntoWall_IsBlockedAndChangesNothing()
        {
            var level = new LevelData("Wall", new[] { "#####", "#@  #", "#####" });
            var state = new GameState(level);

            MoveResult result = SokobanRules.TryMove(state, Direction.Left);

            Assert.IsFalse(result.Success);
            Assert.IsFalse(result.PushedBox);
            Assert.IsFalse(result.Won);
            Assert.AreEqual(new GridPosition(1, 1), state.PlayerPosition);
            Assert.AreEqual(Direction.Down, state.Facing);
        }

        [Test]
        public void TryMove_PushBoxOntoGoal_Wins()
        {
            var level = new LevelData("Push", new[] { "#####", "#@$.#", "#####" });
            var state = new GameState(level);

            MoveResult result = SokobanRules.TryMove(state, Direction.Right);

            Assert.IsTrue(result.Success);
            Assert.IsTrue(result.PushedBox);
            Assert.IsTrue(result.Won);
            Assert.AreEqual(new GridPosition(2, 1), state.PlayerPosition);
            Assert.AreEqual(Direction.Right, state.Facing);
            Assert.IsTrue(state.HasBoxAt(new GridPosition(3, 1)));
            Assert.IsFalse(state.HasBoxAt(new GridPosition(2, 1)));
        }

        [Test]
        public void TryMove_PushIntoWall_IsBlocked()
        {
            var level = new LevelData("Box vs wall", new[] { "#####", "# @$#", "#####" });
            var state = new GameState(level);

            MoveResult result = SokobanRules.TryMove(state, Direction.Right);

            Assert.IsFalse(result.Success);
            Assert.IsFalse(result.PushedBox);
            Assert.AreEqual(new GridPosition(2, 1), state.PlayerPosition);
            Assert.IsTrue(state.HasBoxAt(new GridPosition(3, 1)));
        }

        [Test]
        public void TryMove_PushIntoBox_IsBlocked()
        {
            var level = new LevelData("Box vs box", new[] { "######", "#@$$.#", "######" });
            var state = new GameState(level);

            MoveResult result = SokobanRules.TryMove(state, Direction.Right);

            Assert.IsFalse(result.Success);
            Assert.IsFalse(result.PushedBox);
            Assert.AreEqual(new GridPosition(1, 1), state.PlayerPosition);
            Assert.IsTrue(state.HasBoxAt(new GridPosition(2, 1)));
            Assert.IsTrue(state.HasBoxAt(new GridPosition(3, 1)));
        }

        [Test]
        public void TryMove_WinRequiresAllGoalsCovered()
        {
            var level = new LevelData("Two goals", new[] { "#####", "#@$.#", "#  .#", "#####" });
            var state = new GameState(level);

            MoveResult result = SokobanRules.TryMove(state, Direction.Right);

            Assert.IsTrue(result.Success);
            Assert.IsTrue(result.PushedBox);
            Assert.IsFalse(result.Won);
        }

        [Test]
        public void TryMove_MoveOntoGoal_DoesNotWinAlone()
        {
            var level = new LevelData("Step on goal", new[] { "#####", "#.@$#", "#####" });
            var state = new GameState(level);

            MoveResult result = SokobanRules.TryMove(state, Direction.Left);

            Assert.IsTrue(result.Success);
            Assert.IsFalse(result.PushedBox);
            Assert.IsFalse(result.Won);
        }

        [Test]
        public void Facing_DefaultsToDown_AndCanBeConfigured()
        {
            var level = new LevelData("Facing", new[] { "#####", "#@  #", "#####" });

            Assert.AreEqual(Direction.Down, new GameState(level).Facing);
            Assert.AreEqual(Direction.Up, new GameState(level, Direction.Up).Facing);
        }

        [Test]
        public void GameState_Reset_RestoresInitialLayoutAndFacing()
        {
            var level = new LevelData("Reset", new[] { "#####", "#@$.#", "#####" });
            var state = new GameState(level, Direction.Left);

            SokobanRules.TryMove(state, Direction.Right);
            state.Reset();

            Assert.AreEqual(new GridPosition(1, 1), state.PlayerPosition);
            Assert.IsTrue(state.HasBoxAt(new GridPosition(2, 1)));
            Assert.IsFalse(state.HasBoxAt(new GridPosition(3, 1)));
            Assert.AreEqual(Direction.Left, state.Facing);
            Assert.IsFalse(state.IsWon);
        }

        [Test]
        public void GameState_IsWon_TrueWhenBoxStartsOnGoal()
        {
            var level = new LevelData("Box on goal", new[] { "######", "#@*  #", "######" });
            var state = new GameState(level);

            Assert.IsTrue(state.IsWon);
        }

        [Test]
        public void TryMove_NullState_Throws()
        {
            Assert.Throws<System.ArgumentNullException>(() => SokobanRules.TryMove(null, Direction.Right));
        }
    }
}
