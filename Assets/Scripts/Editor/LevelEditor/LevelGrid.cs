using BoxPushing.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace BoxPushing.Editor
{
    /// <summary>Brush choices for the level grid editor. Wall/Floor/Goal are terrain; Player/Box are entities.</summary>
    public enum BrushKind
    {
        Wall,
        Floor,
        Goal,
        Player,
        Box
    }

    /// <summary>
    /// Custom <see cref="VisualElement"/> that draws a <see cref="LevelData"/> grid and edits it
    /// with layered brushes. Left mouse button paints the current brush; right mouse button erases
    /// according to the current brush's layer. Painting/erasing continues while dragging.
    /// </summary>
    public sealed class LevelGrid : VisualElement
    {
        private const float CellSize = 30f;
        private const float CellInset = 1f;

        public static readonly Color WallColor = new Color(0.24f, 0.26f, 0.31f);
        public static readonly Color FloorColor = new Color(0.62f, 0.64f, 0.68f);
        public static readonly Color GoalColor = new Color(0.24f, 0.62f, 0.34f);
        public static readonly Color PlayerColor = new Color(0.20f, 0.55f, 1f);
        public static readonly Color BoxColor = new Color(0.95f, 0.62f, 0.16f);

        private LevelData level;
        private BrushKind brush = BrushKind.Wall;
        private bool erasing;

        public LevelGrid()
        {
            pickingMode = PickingMode.Position;
            generateVisualContent += OnGenerateVisualContent;

            RegisterCallback<PointerDownEvent>(OnPointerDown);
            RegisterCallback<PointerMoveEvent>(OnPointerMove);
            RegisterCallback<PointerUpEvent>(OnPointerUp);
            RegisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);
        }

        /// <summary>Raised after any cell is painted or erased.</summary>
        public event System.Action Changed;

        public LevelData Level
        {
            get => level;
            set
            {
                level = value;
                UpdateSize();
                MarkDirtyRepaint();
            }
        }

        public BrushKind Brush
        {
            get => brush;
            set => brush = value;
        }

        public static Color GetBrushColor(BrushKind kind)
        {
            switch (kind)
            {
                case BrushKind.Wall:
                    return WallColor;
                case BrushKind.Floor:
                    return FloorColor;
                case BrushKind.Goal:
                    return GoalColor;
                case BrushKind.Player:
                    return PlayerColor;
                case BrushKind.Box:
                    return BoxColor;
                default:
                    return Color.magenta;
            }
        }

        private void UpdateSize()
        {
            int width = level != null ? level.Width : 0;
            int height = level != null ? level.Height : 0;
            style.width = width * CellSize;
            style.height = height * CellSize;
        }

        // ---- Drawing ----

        private void OnGenerateVisualContent(MeshGenerationContext ctx)
        {
            Painter2D painter = ctx.painter2D;
            if (level == null || level.map == null)
                return;

            for (int y = 0; y < level.Height; y++)
            {
                string row = level.map[y];
                if (row == null)
                    continue;

                for (int x = 0; x < row.Length; x++)
                {
                    DrawCell(painter, row[x], x, y);
                }
            }

            DrawGridLines(painter, level.Width, level.Height);
        }

        private static void DrawCell(Painter2D painter, char cell, int x, int y)
        {
            Rect rect = CellRect(x, y);
            painter.fillColor = TerrainColor(cell);
            FillRect(painter, rect);

            if (XsbParser.IsPlayer(cell))
            {
                painter.fillColor = PlayerColor;
                FillCircle(painter, rect.center, CellSize * 0.28f);
            }
            else if (XsbParser.IsBox(cell))
            {
                painter.fillColor = BoxColor;
                float half = CellSize * 0.28f;
                FillRect(painter, new Rect(rect.center.x - half, rect.center.y - half, half * 2f, half * 2f));
            }
        }

        private static void FillRect(Painter2D painter, Rect rect)
        {
            painter.BeginPath();
            painter.MoveTo(new Vector2(rect.xMin, rect.yMin));
            painter.LineTo(new Vector2(rect.xMax, rect.yMin));
            painter.LineTo(new Vector2(rect.xMax, rect.yMax));
            painter.LineTo(new Vector2(rect.xMin, rect.yMax));
            painter.ClosePath();
            painter.Fill();
        }

        private static void FillCircle(Painter2D painter, Vector2 center, float radius)
        {
            const int segments = 32;
            painter.BeginPath();
            for (int i = 0; i < segments; i++)
            {
                float angle = Mathf.PI * 2f * i / segments;
                Vector2 point = center + new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius);
                if (i == 0)
                    painter.MoveTo(point);
                else
                    painter.LineTo(point);
            }
            painter.ClosePath();
            painter.Fill();
        }

        private static Color TerrainColor(char cell)
        {
            if (XsbParser.IsWall(cell))
                return WallColor;

            if (XsbParser.IsGoal(cell))
                return GoalColor;

            return FloorColor;
        }

        private void DrawGridLines(Painter2D painter, int width, int height)
        {
            painter.strokeColor = new Color(0f, 0f, 0f, 0.35f);
            painter.lineWidth = 1f;

            for (int x = 0; x <= width; x++)
            {
                painter.BeginPath();
                painter.MoveTo(new Vector2(x * CellSize, 0f));
                painter.LineTo(new Vector2(x * CellSize, height * CellSize));
                painter.Stroke();
            }

            for (int y = 0; y <= height; y++)
            {
                painter.BeginPath();
                painter.MoveTo(new Vector2(0f, y * CellSize));
                painter.LineTo(new Vector2(width * CellSize, y * CellSize));
                painter.Stroke();
            }
        }

        private static Rect CellRect(int x, int y)
        {
            return new Rect(x * CellSize + CellInset, y * CellSize + CellInset, CellSize - CellInset * 2f, CellSize - CellInset * 2f);
        }

        // ---- Editing ----

        private void OnPointerDown(PointerDownEvent evt)
        {
            if (level == null)
                return;

            if (evt.button != 0 && evt.button != 1)
                return;

            erasing = evt.button == 1;
            this.CapturePointer(evt.pointerId);
            ApplyTool(evt.localPosition);
            evt.StopPropagation();
        }

        private void OnPointerMove(PointerMoveEvent evt)
        {
            if (level == null || !this.HasPointerCapture(evt.pointerId))
                return;

            ApplyTool(evt.localPosition);
            evt.StopPropagation();
        }

        private void OnPointerUp(PointerUpEvent evt)
        {
            if (this.HasPointerCapture(evt.pointerId))
                this.ReleasePointer(evt.pointerId);

            erasing = false;
            evt.StopPropagation();
        }

        private void OnPointerCaptureOut(PointerCaptureOutEvent evt)
        {
            erasing = false;
        }

        private void ApplyTool(Vector2 localPosition)
        {
            if (level == null)
                return;

            int x = Mathf.FloorToInt(localPosition.x / CellSize);
            int y = Mathf.FloorToInt(localPosition.y / CellSize);

            if (x < 0 || y < 0 || x >= level.Width || y >= level.Height)
                return;

            if (erasing)
                EraseAt(x, y);
            else
                PaintAt(x, y);

            MarkDirtyRepaint();
            Changed?.Invoke();
        }

        private void PaintAt(int x, int y)
        {
            switch (brush)
            {
                case BrushKind.Wall:
                    SetCell(x, y, XsbParser.Wall);
                    break;
                case BrushKind.Floor:
                    SetCell(x, y, XsbParser.Floor);
                    break;
                case BrushKind.Goal:
                    PaintGoal(x, y);
                    break;
                case BrushKind.Player:
                    PaintEntity(x, y, isPlayer: true);
                    break;
                case BrushKind.Box:
                    PaintEntity(x, y, isPlayer: false);
                    break;
            }
        }

        private void EraseAt(int x, int y)
        {
            bool entityLayer = brush == BrushKind.Player || brush == BrushKind.Box;
            if (entityLayer)
            {
                EraseEntity(x, y);
                return;
            }

            // Terrain erase clears the cell to floor and removes any entity on it.
            SetCell(x, y, XsbParser.Floor);
        }

        private void PaintGoal(int x, int y)
        {
            char current = level.map[y][x];
            if (XsbParser.IsPlayer(current))
                SetCell(x, y, XsbParser.PlayerOnGoal);
            else if (XsbParser.IsBox(current))
                SetCell(x, y, XsbParser.BoxOnGoal);
            else
                SetCell(x, y, XsbParser.Goal);
        }

        private void PaintEntity(int x, int y, bool isPlayer)
        {
            char current = level.map[y][x];

            // Entities cannot be placed on a wall (there is no XSB character for it).
            if (XsbParser.IsWall(current))
                return;

            if (isPlayer)
            {
                RemoveExistingPlayer(x, y);
                SetCell(x, y, XsbParser.IsGoal(current) ? XsbParser.PlayerOnGoal : XsbParser.Player);
            }
            else
            {
                SetCell(x, y, XsbParser.IsGoal(current) ? XsbParser.BoxOnGoal : XsbParser.Box);
            }
        }

        private void EraseEntity(int x, int y)
        {
            char current = level.map[y][x];
            switch (current)
            {
                case XsbParser.PlayerOnGoal:
                case XsbParser.BoxOnGoal:
                    SetCell(x, y, XsbParser.Goal);
                    break;
                case XsbParser.Player:
                case XsbParser.Box:
                    SetCell(x, y, XsbParser.Floor);
                    break;
                default:
                    break;
            }
        }

        private void RemoveExistingPlayer(int exceptX, int exceptY)
        {
            for (int y = 0; y < level.Height; y++)
            {
                string row = level.map[y];
                if (row == null)
                    continue;

                for (int x = 0; x < row.Length; x++)
                {
                    if (x == exceptX && y == exceptY)
                        continue;

                    char cell = row[x];
                    if (XsbParser.IsPlayer(cell))
                        SetCell(x, y, XsbParser.IsGoal(cell) ? XsbParser.Goal : XsbParser.Floor);
                }
            }
        }

        private void SetCell(int x, int y, char cell)
        {
            char[] row = level.map[y].ToCharArray();
            row[x] = cell;
            level.map[y] = new string(row);
        }
    }
}
