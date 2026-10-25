using UnityEngine;

namespace BoxPushing.Gameplay
{
    /// <summary>
    /// Placeholder flat color palette for P2 presentation. Stylized materials replace
    /// these values in a later phase.
    /// </summary>
    public static class Palette
    {
        public static readonly Color Wall = new Color(0.24f, 0.26f, 0.34f);
        public static readonly Color Floor = new Color(0.82f, 0.83f, 0.86f);
        public static readonly Color Goal = new Color(0.33f, 0.73f, 0.40f);
        public static readonly Color Box = new Color(0.86f, 0.58f, 0.24f);
        public static readonly Color BoxOnGoal = new Color(0.95f, 0.84f, 0.26f);
        public static readonly Color PlayerBody = new Color(0.22f, 0.47f, 0.85f);
        public static readonly Color PlayerFace = new Color(0.95f, 0.95f, 0.98f);
    }
}
