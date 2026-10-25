using System;

namespace BoxPushing.Core
{
    /// <summary>Serializable grid coordinate (mirrors <see cref="GridPosition"/> for JSON).</summary>
    [Serializable]
    public class GridPositionDto
    {
        public int x;
        public int y;

        public GridPositionDto()
        {
        }

        public GridPositionDto(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

        public GridPosition ToGridPosition() => new GridPosition(x, y);

        public static GridPositionDto From(GridPosition position) => new GridPositionDto(position.X, position.Y);
    }

    /// <summary>Serializable one-step move delta (mirrors <see cref="MoveDelta"/> for JSON).</summary>
    [Serializable]
    public class MoveDeltaDto
    {
        public GridPositionDto playerFrom;
        public GridPositionDto playerTo;
        public bool hasBox;
        public GridPositionDto boxFrom;
        public GridPositionDto boxTo;
        public Direction facingFrom;
        public Direction facingTo;

        public MoveDelta ToMoveDelta()
        {
            return new MoveDelta(
                playerFrom != null ? playerFrom.ToGridPosition() : default,
                playerTo != null ? playerTo.ToGridPosition() : default,
                hasBox && boxFrom != null ? boxFrom.ToGridPosition() : (GridPosition?)null,
                hasBox && boxTo != null ? boxTo.ToGridPosition() : (GridPosition?)null,
                facingFrom,
                facingTo);
        }

        public static MoveDeltaDto From(MoveDelta delta)
        {
            return new MoveDeltaDto
            {
                playerFrom = GridPositionDto.From(delta.PlayerFrom),
                playerTo = GridPositionDto.From(delta.PlayerTo),
                hasBox = delta.PushedBox,
                boxFrom = delta.BoxFrom.HasValue ? GridPositionDto.From(delta.BoxFrom.Value) : null,
                boxTo = delta.BoxTo.HasValue ? GridPositionDto.From(delta.BoxTo.Value) : null,
                facingFrom = delta.FacingFrom,
                facingTo = delta.FacingTo
            };
        }
    }

    /// <summary>
    /// Serializable snapshot of an in-progress run. Stores only dynamic state (player,
    /// facing, counters, boxes) plus the undo/redo stacks so a session can be restored.
    /// </summary>
    [Serializable]
    public class RunSnapshot
    {
        public int playerX;
        public int playerY;
        public Direction facing;
        public int moves;
        public int pushes;
        public GridPositionDto[] boxes;
        public MoveDeltaDto[] undoStack;
        public MoveDeltaDto[] redoStack;
    }
}
