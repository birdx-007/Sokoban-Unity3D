using UnityEngine;

namespace BoxPushing.Gameplay
{
    /// <summary>
    /// Canonical pastel palette for P5. <see cref="StylizedScheme"/> defaults to these
    /// values, and <see cref="ForRole"/> provides a fallback when no stylization
    /// controller is active (e.g. editor tooling or tests).
    /// </summary>
    public static class Palette
    {
        public static readonly Color Wall = Hex("#C7BBA2");
        public static readonly Color Floor = Hex("#EBE6DB");
        public static readonly Color Goal = Hex("#85C09A");
        public static readonly Color Box = Hex("#B9855F");
        public static readonly Color BoxOnGoal = Hex("#5FA37D");
        public static readonly Color PlayerBody = Hex("#F0A05A");
        public static readonly Color PlayerFace = Hex("#6E4F3A");

        public static readonly Color BackgroundCenter = Hex("#F4F0E6");
        public static readonly Color BackgroundEdge = Hex("#CFDCD7");

        public static readonly Color GridLine = new Color(0f, 0f, 0f, 0x26 / 255f);

        public static Color ForRole(StyledRole role)
        {
            switch (role)
            {
                case StyledRole.Wall: return Wall;
                case StyledRole.Floor: return Floor;
                case StyledRole.Goal: return Goal;
                case StyledRole.Box: return Box;
                case StyledRole.BoxOnGoal: return BoxOnGoal;
                case StyledRole.PlayerBody: return PlayerBody;
                case StyledRole.PlayerFace: return PlayerFace;
                default: return Color.white;
            }
        }

        private static Color Hex(string hex)
        {
            Color color;
            return ColorUtility.TryParseHtmlString(hex, out color) ? color : Color.magenta;
        }
    }
}
