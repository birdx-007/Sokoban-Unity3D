using System;
using BoxPushing.Core;
using NUnit.Framework;

namespace BoxPushing.Tests
{
    public class UndoSystemTests
    {
        private static bool MoveAndRecord(GameState state, UndoSystem undo, Direction direction)
        {
            MoveResult result = SokobanRules.TryMove(state, direction, out MoveDelta delta);
            if (result.Success)
                undo.Record(new MoveCommand(delta));

            return result.Success;
        }

        [Test]
        public void Undo_SimpleMove_RestoresPositionAndFacing()
        {
            var level = new LevelData("Undo move", new[] { "#####", "#@  #", "#####" });
            var state = new GameState(level);
            var undo = new UndoSystem();

            MoveAndRecord(state, undo, Direction.Right);
            Assert.AreEqual(new GridPosition(2, 1), state.PlayerPosition);
            Assert.AreEqual(Direction.Right, state.Facing);

            Assert.IsTrue(undo.Undo(state).HasValue);
            Assert.AreEqual(new GridPosition(1, 1), state.PlayerPosition);
            Assert.AreEqual(Direction.Down, state.Facing);

            Assert.IsTrue(undo.Redo(state).HasValue);
            Assert.AreEqual(new GridPosition(2, 1), state.PlayerPosition);
            Assert.AreEqual(Direction.Right, state.Facing);
        }

        [Test]
        public void Undo_PushRestoresBoxAndWinState()
        {
            var level = new LevelData("Undo push", new[] { "#####", "#@$.#", "#####" });
            var state = new GameState(level);
            var undo = new UndoSystem();

            MoveAndRecord(state, undo, Direction.Right);
            Assert.IsTrue(state.IsWon);
            Assert.IsTrue(state.HasBoxAt(new GridPosition(3, 1)));

            Assert.IsTrue(undo.Undo(state).HasValue);
            Assert.IsFalse(state.IsWon);
            Assert.AreEqual(new GridPosition(1, 1), state.PlayerPosition);
            Assert.IsTrue(state.HasBoxAt(new GridPosition(2, 1)));
            Assert.IsFalse(state.HasBoxAt(new GridPosition(3, 1)));

            Assert.IsTrue(undo.Redo(state).HasValue);
            Assert.IsTrue(state.IsWon);
            Assert.IsTrue(state.HasBoxAt(new GridPosition(3, 1)));
        }

        [Test]
        public void Undo_MultipleMoves_UndoInReverseOrder()
        {
            var level = new LevelData("Lifo", new[] { "#####", "#@  #", "#####" });
            var state = new GameState(level);
            var undo = new UndoSystem();

            MoveAndRecord(state, undo, Direction.Right); // (2,1)
            MoveAndRecord(state, undo, Direction.Right); // (3,1)

            Assert.IsTrue(undo.Undo(state).HasValue);
            Assert.AreEqual(new GridPosition(2, 1), state.PlayerPosition);

            Assert.IsTrue(undo.Undo(state).HasValue);
            Assert.AreEqual(new GridPosition(1, 1), state.PlayerPosition);
        }

        [Test]
        public void Record_NewMove_ClearsRedoStack()
        {
            var level = new LevelData("Clear redo", new[] { "#####", "#@  #", "#####" });
            var state = new GameState(level);
            var undo = new UndoSystem();

            MoveAndRecord(state, undo, Direction.Right);
            Assert.IsTrue(undo.Undo(state).HasValue);
            Assert.IsTrue(undo.CanRedo);

            MoveAndRecord(state, undo, Direction.Right);
            Assert.IsFalse(undo.CanRedo);
        }

        [Test]
        public void BlockedMove_IsNotRecorded()
        {
            var level = new LevelData("Blocked", new[] { "#####", "#@  #", "#####" });
            var state = new GameState(level);
            var undo = new UndoSystem();

            bool success = MoveAndRecord(state, undo, Direction.Left);

            Assert.IsFalse(success);
            Assert.AreEqual(0, undo.UndoCount);
            Assert.AreEqual(0, undo.RedoCount);
            Assert.AreEqual(new GridPosition(1, 1), state.PlayerPosition);
        }

        [Test]
        public void Undo_OnEmptyStack_ReturnsFalse()
        {
            var level = new LevelData("Empty", new[] { "#####", "#@  #", "#####" });
            var state = new GameState(level);
            var undo = new UndoSystem();

            Assert.IsFalse(undo.CanUndo);
            Assert.IsFalse(undo.CanRedo);
            Assert.IsFalse(undo.Undo(state).HasValue);
            Assert.IsFalse(undo.Redo(state).HasValue);
        }

        [Test]
        public void Clear_EmptiesBothStacks()
        {
            var level = new LevelData("Clear", new[] { "#####", "#@  #", "#####" });
            var state = new GameState(level);
            var undo = new UndoSystem();

            MoveAndRecord(state, undo, Direction.Right);
            Assert.IsTrue(undo.Undo(state).HasValue);

            undo.Clear();

            Assert.IsFalse(undo.CanUndo);
            Assert.IsFalse(undo.CanRedo);
        }

        [Test]
        public void UndoAndRedo_ReturnDeltasForAnimation()
        {
            var level = new LevelData("Deltas", new[] { "#####", "#@$.#", "#####" });
            var state = new GameState(level);
            var undo = new UndoSystem();

            MoveAndRecord(state, undo, Direction.Right); // pushes box from (2,1) to (3,1)

            MoveDelta? undoDelta = undo.Undo(state);
            Assert.IsTrue(undoDelta.HasValue);
            Assert.AreEqual(new GridPosition(2, 1), undoDelta.Value.PlayerFrom);
            Assert.AreEqual(new GridPosition(1, 1), undoDelta.Value.PlayerTo);
            Assert.AreEqual(new GridPosition(3, 1), undoDelta.Value.BoxFrom);
            Assert.AreEqual(new GridPosition(2, 1), undoDelta.Value.BoxTo);
            Assert.AreEqual(Direction.Right, undoDelta.Value.FacingFrom);
            Assert.AreEqual(Direction.Down, undoDelta.Value.FacingTo);

            MoveDelta? redoDelta = undo.Redo(state);
            Assert.IsTrue(redoDelta.HasValue);
            Assert.AreEqual(new GridPosition(1, 1), redoDelta.Value.PlayerFrom);
            Assert.AreEqual(new GridPosition(2, 1), redoDelta.Value.PlayerTo);
            Assert.AreEqual(new GridPosition(2, 1), redoDelta.Value.BoxFrom);
            Assert.AreEqual(new GridPosition(3, 1), redoDelta.Value.BoxTo);
            Assert.AreEqual(Direction.Down, redoDelta.Value.FacingFrom);
            Assert.AreEqual(Direction.Right, redoDelta.Value.FacingTo);
        }

        [Test]
        public void Record_NullCommand_Throws()
        {
            var undo = new UndoSystem();
            Assert.Throws<ArgumentNullException>(() => undo.Record(null));
        }

        [Test]
        public void Undo_NullState_Throws()
        {
            var undo = new UndoSystem();
            Assert.Throws<ArgumentNullException>(() => undo.Undo(null));
        }
    }
}
