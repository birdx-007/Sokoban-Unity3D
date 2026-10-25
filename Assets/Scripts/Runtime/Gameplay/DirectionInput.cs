using BoxPushing.Core;
using UnityEngine;

namespace BoxPushing.Gameplay
{
    /// <summary>
    /// Converts a gameplay Move 2D vector into a discrete <see cref="Direction"/>.
    /// Applies a dead zone, picks the dominant axis, resolves ties vertically, and maps
    /// positive Y to Up.
    /// </summary>
    public static class DirectionInput
    {
        public const float DefaultDeadZone = 0.5f;

        public static Direction? FromVector(Vector2 vector, float deadZone = DefaultDeadZone)
        {
            if (vector.sqrMagnitude < deadZone * deadZone)
                return null;

            float absX = Mathf.Abs(vector.x);
            float absY = Mathf.Abs(vector.y);

            if (absY >= absX)
                return vector.y > 0f ? Direction.Up : Direction.Down;

            return vector.x > 0f ? Direction.Right : Direction.Left;
        }
    }
}
