using System;
using UnityEngine;

namespace BoxPushing.Gameplay
{
    public enum StyledRole
    {
        Wall,
        Floor,
        Goal,
        Box,
        BoxOnGoal,
        PlayerBody,
        PlayerFace
    }

    /// <summary>
    /// A complete stylization preset: toon shading parameters, palette, background
    /// colors, optional grid lines, and feedback glow settings.
    /// </summary>
    [Serializable]
    public class StylizedScheme
    {
        public string name = "Scheme";

        [Header("Toon Shading")]
        [Range(1f, 8f)] public float rampBands = 3f;
        [Range(0f, 1f)] public float rampSmoothness = 0.65f;
        public Color shadowTint = new Color(0.58f, 0.52f, 0.44f, 1f);
        [Range(0f, 2f)] public float ambientStrength = 0.6f;
        public bool flatShade = false;

        [Header("Palette")]
        public Color wall = Palette.Wall;
        public Color floor = Palette.Floor;
        public Color goal = Palette.Goal;
        public Color box = Palette.Box;
        public Color boxOnGoal = Palette.BoxOnGoal;
        public Color playerBody = Palette.PlayerBody;
        public Color playerFace = Palette.PlayerFace;

        [Header("Background")]
        public Color backgroundCenter = Palette.BackgroundCenter;
        public Color backgroundEdge = Palette.BackgroundEdge;

        [Header("Grid Lines")]
        public bool showGridLines = true;
        public Color gridLine = Palette.GridLine;

        [Header("Feedback")]
        public Color goalEmission = new Color(0.32f, 0.75f, 0.48f, 1f);
        [Range(0f, 3f)] public float goalEmissionStrength = 0.35f;
        [Range(0f, 1f)] public float goalPulseAmount = 0.4f;
        public Color boxOnGoalEmission = new Color(0.32f, 0.75f, 0.48f, 1f);
        [Range(0f, 3f)] public float boxOnGoalEmissionStrength = 0.45f;
        [Range(0f, 1f)] public float boxOnGoalPulseAmount = 0.35f;
        public float pulseSpeed = 2.2f;

        public Color GetColor(StyledRole role)
        {
            switch (role)
            {
                case StyledRole.Wall: return wall;
                case StyledRole.Floor: return floor;
                case StyledRole.Goal: return goal;
                case StyledRole.Box: return box;
                case StyledRole.BoxOnGoal: return boxOnGoal;
                case StyledRole.PlayerBody: return playerBody;
                case StyledRole.PlayerFace: return playerFace;
                default: return Color.white;
            }
        }
    }
}
