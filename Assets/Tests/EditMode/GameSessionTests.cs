using BoxPushing.Core;
using NUnit.Framework;

namespace BoxPushing.Tests
{
    public class GameSessionTests
    {
        private static LevelData TwoBoxLevel()
            => new LevelData(
                "Two boxes",
                new[]
                {
                    "#####",
                    "#@$.#",
                    "#  .#",
                    "#####"
                });

        [Test]
        public void TryMove_MoveIncrementsMovesOnly()
        {
            var session = new GameSession(new LevelData("M", new[] { "#####", "#@  #", "#####" }));

            session.TryMove(Direction.Right);

            Assert.AreEqual(1, session.State.Moves);
            Assert.AreEqual(0, session.State.Pushes);
        }

        [Test]
        public void TryMove_PushIncrementsMovesAndPushes()
        {
            var session = new GameSession(TwoBoxLevel());

            session.TryMove(Direction.Right);

            Assert.AreEqual(1, session.State.Moves);
            Assert.AreEqual(1, session.State.Pushes);
        }

        [Test]
        public void TryMove_Blocked_DoesNotIncrementCounters()
        {
            var session = new GameSession(new LevelData("M", new[] { "#####", "#@  #", "#####" }));

            var (result, delta) = session.TryMove(Direction.Left);

            Assert.IsFalse(result.Success);
            Assert.AreEqual(default(MoveDelta), delta);
            Assert.AreEqual(0, session.State.Moves);
            Assert.AreEqual(0, session.State.Pushes);
            Assert.AreEqual(0, session.UndoSystem.UndoCount);
        }

        [Test]
        public void TryMove_RecordsCommand_ReturnsForwardDelta()
        {
            var session = new GameSession(TwoBoxLevel());

            var (result, delta) = session.TryMove(Direction.Right);

            Assert.IsTrue(result.Success);
            Assert.IsTrue(result.PushedBox);
            Assert.AreEqual(new GridPosition(1, 1), delta.PlayerFrom);
            Assert.AreEqual(new GridPosition(2, 1), delta.PlayerTo);
            Assert.AreEqual(new GridPosition(2, 1), delta.BoxFrom);
            Assert.AreEqual(new GridPosition(3, 1), delta.BoxTo);
            Assert.AreEqual(1, session.UndoSystem.UndoCount);
        }

        [Test]
        public void Undo_DecrementsCounters_RedoIncrementsThem()
        {
            var session = new GameSession(TwoBoxLevel());
            session.TryMove(Direction.Right);

            Assert.AreEqual(1, session.State.Moves);
            Assert.AreEqual(1, session.State.Pushes);

            Assert.IsTrue(session.Undo().HasValue);
            Assert.AreEqual(0, session.State.Moves);
            Assert.AreEqual(0, session.State.Pushes);

            Assert.IsTrue(session.Redo().HasValue);
            Assert.AreEqual(1, session.State.Moves);
            Assert.AreEqual(1, session.State.Pushes);
        }

        [Test]
        public void Reset_ClearsStateHistoryAndCounters()
        {
            var session = new GameSession(TwoBoxLevel());
            session.TryMove(Direction.Right);

            session.Reset();

            Assert.AreEqual(0, session.State.Moves);
            Assert.AreEqual(0, session.State.Pushes);
            Assert.AreEqual(new GridPosition(1, 1), session.State.PlayerPosition);
            Assert.IsFalse(session.CanUndo);
            Assert.IsFalse(session.CanRedo);
        }

        [Test]
        public void ToSnapshot_And_Restore_RoundTripsFullRun()
        {
            var level = new LevelData(
                "Roundtrip",
                new[]
                {
                    "#######",
                    "#@  $.#",
                    "#######"
                });

            var session = new GameSession(level);
            session.TryMove(Direction.Right); // (2,1)
            session.TryMove(Direction.Right); // (3,1)
            session.TryMove(Direction.Right); // pushes box to goal, (4,1)
            session.Undo();                   // back to (3,1)

            RunSnapshot snapshot = session.ToSnapshot();
            Assert.AreEqual(3, snapshot.playerX);
            Assert.AreEqual(1, snapshot.playerY);
            Assert.AreEqual(2, snapshot.moves);
            Assert.AreEqual(0, snapshot.pushes);
            Assert.AreEqual(2, snapshot.undoStack.Length);
            Assert.AreEqual(1, snapshot.redoStack.Length);

            var restored = new GameSession(level);
            restored.Restore(snapshot);

            Assert.AreEqual(session.State.PlayerPosition, restored.State.PlayerPosition);
            Assert.AreEqual(session.State.Facing, restored.State.Facing);
            Assert.AreEqual(session.State.Moves, restored.State.Moves);
            Assert.AreEqual(session.State.Pushes, restored.State.Pushes);
            CollectionAssert.AreEquivalent(session.State.BoxPositions, restored.State.BoxPositions);
            Assert.AreEqual(session.CanUndo, restored.CanUndo);
            Assert.AreEqual(session.CanRedo, restored.CanRedo);

            // History is actually restorable: undo then redo behaves like the original.
            Assert.IsTrue(restored.Undo().HasValue);
            Assert.IsTrue(restored.Redo().HasValue);
            Assert.AreEqual(session.State.Moves, restored.State.Moves);
        }

        [Test]
        public void SaveJson_RoundTripsLevelSaveDataWithInProgressSnapshot()
        {
            var session = new GameSession(TwoBoxLevel());
            session.TryMove(Direction.Right);

            var save = new SaveData
            {
                levels = new[]
                {
                    new LevelSaveData
                    {
                        id = "Level01",
                        completed = false,
                        inProgress = session.ToSnapshot()
                    }
                }
            };

            SaveData restored = SaveJson.FromJson(SaveJson.ToJson(save));

            Assert.AreEqual(1, restored.levels.Length);
            Assert.AreEqual("Level01", restored.levels[0].id);
            Assert.IsFalse(restored.levels[0].completed);
            Assert.IsNotNull(restored.levels[0].inProgress);
            Assert.AreEqual(1, restored.levels[0].inProgress.moves);
            Assert.AreEqual(1, restored.levels[0].inProgress.pushes);
            Assert.AreEqual(1, restored.levels[0].inProgress.undoStack.Length);
        }

        [Test]
        public void IsWall_OutOfBounds_ReturnsTrue()
        {
            var state = new GameState(new LevelData("M", new[] { "#####", "#@  #", "#####" }));

            Assert.IsTrue(state.IsWall(new GridPosition(-1, 0)));
            Assert.IsTrue(state.IsWall(new GridPosition(0, -1)));
            Assert.IsTrue(state.IsWall(new GridPosition(99, 99)));
        }

        [Test]
        public void TryMove_OutOfBounds_IsBlocked()
        {
            var level = new LevelData("Gap", new[] { "####", "#@  ", "####" });
            var state = new GameState(level);

            SokobanRules.TryMove(state, Direction.Right); // (2,1)
            SokobanRules.TryMove(state, Direction.Right); // (3,1), open boundary floor
            MoveResult result = SokobanRules.TryMove(state, Direction.Right); // (4,1), out of bounds

            Assert.IsFalse(result.Success);
            Assert.AreEqual(new GridPosition(3, 1), state.PlayerPosition);
        }

        [Test]
        public void TryRestore_RejectsInvalidSnapshot()
        {
            var level = new LevelData("M", new[] { "#####", "#@$.#", "#####" });
            var session = new GameSession(level);

            var bad = new RunSnapshot
            {
                playerX = -1,
                playerY = 0,
                facing = Direction.Down,
                boxes = System.Array.Empty<GridPositionDto>()
            };

            Assert.IsFalse(session.TryRestore(bad));
            Assert.AreEqual(new GridPosition(1, 1), session.State.PlayerPosition);
        }

        [Test]
        public void TryRestore_AcceptsValidSnapshot()
        {
            var level = new LevelData("M", new[] { "#####", "#@$.#", "#####" });
            var session = new GameSession(level);
            session.TryMove(Direction.Right); // push box to goal

            var fresh = new GameSession(level);
            Assert.IsTrue(fresh.TryRestore(session.ToSnapshot()));
            Assert.AreEqual(session.State.PlayerPosition, fresh.State.PlayerPosition);
            CollectionAssert.AreEquivalent(session.State.BoxPositions, fresh.State.BoxPositions);
        }

        [Test]
        public void BoxPositions_ReturnsDefensiveCopy()
        {
            var level = new LevelData("Boxes", new[] { "#####", "#@$.#", "#####" });
            var state = new GameState(level);

            GridPosition[] boxes = state.BoxPositions;

            Assert.IsNotInstanceOf<System.Collections.Generic.HashSet<GridPosition>>(boxes);
            Assert.AreEqual(1, boxes.Length);
            Assert.AreEqual(new GridPosition(2, 1), boxes[0]);

            // Mutating the returned copy must not affect the underlying state.
            boxes[0] = new GridPosition(99, 99);
            Assert.IsTrue(state.HasBoxAt(new GridPosition(2, 1)));
            Assert.IsFalse(state.HasBoxAt(new GridPosition(99, 99)));
            Assert.AreEqual(1, state.BoxPositions.Length);
        }
    }
}
