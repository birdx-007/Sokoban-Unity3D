using BoxPushing.Core;
using UnityEngine;

namespace BoxPushing.Gameplay
{
    /// <summary>
    /// Converts grid coordinates to world-space layout. The board lives on the XZ plane,
    /// is centered on the world origin, and uses one world unit per cell.
    /// </summary>
    public static class BoardGeometry
    {
        public const float CellSize = 1f;

        /// <summary>
        /// World position for a cell anchor (ground level, y=0). X is the column, and Z
        /// is the row mapped so that row 0 (top) sits at +Z and the last row (bottom)
        /// sits at -Z. The origin is centered so the board is symmetric around (0,0,0).
        /// </summary>
        public static Vector3 CellToWorld(GridPosition cell, int width, int height)
        {
            float x = (cell.X - (width - 1) * 0.5f) * CellSize;
            float z = ((height - 1) * 0.5f - cell.Y) * CellSize;
            return new Vector3(x, 0f, z);
        }

        /// <summary>
        /// World Y rotation for a grid-facing direction. Face geometry points along +Z
        /// locally, so Up=0 (toward +Z), Down=180 (toward -Z), Right=90, Left=-90.
        /// </summary>
        public static float ToYRotation(Direction facing)
        {
            switch (facing)
            {
                case Direction.Up:
                    return 0f;
                case Direction.Down:
                    return 180f;
                case Direction.Left:
                    return -90f;
                case Direction.Right:
                    return 90f;
                default:
                    return 0f;
            }
        }
    }
}
