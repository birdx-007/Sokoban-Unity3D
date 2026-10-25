using System;
using System.Collections.Generic;

namespace BoxPushing.Core
{
    /// <summary>
    /// Command-pattern undo/redo with two stacks. Commands store one-step GameState
    /// diffs; only successful moves should be recorded. Recording a new command clears
    /// the redo stack.
    /// </summary>
    public sealed class UndoSystem
    {
        private readonly Stack<ICommand> undoStack = new Stack<ICommand>();
        private readonly Stack<ICommand> redoStack = new Stack<ICommand>();

        public bool CanUndo => undoStack.Count > 0;
        public bool CanRedo => redoStack.Count > 0;
        public int UndoCount => undoStack.Count;
        public int RedoCount => redoStack.Count;

        /// <summary>Records a command and clears the redo stack.</summary>
        public void Record(ICommand command)
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command));

            undoStack.Push(command);
            redoStack.Clear();
        }

        /// <summary>Undoes the most recent command. Returns the reverse delta to animate, or null when there is nothing to undo.</summary>
        public MoveDelta? Undo(GameState state)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));

            if (undoStack.Count == 0)
                return null;

            ICommand command = undoStack.Pop();
            command.Undo(state);
            redoStack.Push(command);
            return command is MoveCommand move ? move.Delta.Reversed() : (MoveDelta?)null;
        }

        /// <summary>Redoes the most recently undone command. Returns the forward delta to animate, or null when there is nothing to redo.</summary>
        public MoveDelta? Redo(GameState state)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));

            if (redoStack.Count == 0)
                return null;

            ICommand command = redoStack.Pop();
            command.Execute(state);
            undoStack.Push(command);
            return command is MoveCommand move ? move.Delta : (MoveDelta?)null;
        }

        public void Clear()
        {
            undoStack.Clear();
            redoStack.Clear();
        }

        /// <summary>Exports the undo stack as move deltas, ordered top-first (next undo first).</summary>
        public MoveDelta[] ExportUndoStack() => Export(undoStack);

        /// <summary>Exports the redo stack as move deltas, ordered top-first (next redo first).</summary>
        public MoveDelta[] ExportRedoStack() => Export(redoStack);

        private static MoveDelta[] Export(Stack<ICommand> stack)
        {
            var deltas = new MoveDelta[stack.Count];
            int i = 0;
            foreach (ICommand command in stack)
            {
                if (command is MoveCommand move)
                    deltas[i++] = move.Delta;
            }

            if (i != deltas.Length)
                Array.Resize(ref deltas, i);

            return deltas;
        }

        /// <summary>Rebuilds both stacks from exported top-first deltas.</summary>
        public void Restore(MoveDelta[] undoDeltas, MoveDelta[] redoDeltas)
        {
            undoStack.Clear();
            redoStack.Clear();

            Rebuild(undoStack, undoDeltas);
            Rebuild(redoStack, redoDeltas);
        }

        private static void Rebuild(Stack<ICommand> stack, MoveDelta[] deltas)
        {
            if (deltas == null)
                return;

            // Deltas arrive top-first; push bottom-first so the first delta ends on top.
            for (int i = deltas.Length - 1; i >= 0; i--)
                stack.Push(new MoveCommand(deltas[i]));
        }
    }
}
