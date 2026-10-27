using System.Collections;
using System.Collections.Generic;
using BoxPushing.Core;
using UnityEngine;

namespace BoxPushing.Gameplay
{
    /// <summary>
    /// Builds the static board (walls, floors, goals) and owns box visuals. Static cells
    /// are rebuilt per level; box GameObjects are tracked by grid position so the
    /// animator can move them during push/undo/redo.
    /// </summary>
    public class BoardView : MonoBehaviour
    {
        [SerializeField] private float wallHeight = 0.6f;
        [SerializeField] private float tileThickness = 0.05f;
        [SerializeField] private float boxScale = 0.85f;

        [Header("Win Pulse")]
        [SerializeField] private float winPulseScale = 1.06f;
        [SerializeField] private float winPulseDuration = 0.24f;
        [SerializeField] private AnimationCurve winPulseCurve = new AnimationCurve(
            new Keyframe(0f, 0f),
            new Keyframe(0.5f, 1f),
            new Keyframe(1f, 0f));

        private readonly Dictionary<GridPosition, GameObject> boxes = new Dictionary<GridPosition, GameObject>();
        private readonly List<Transform> goalTiles = new List<Transform>();

        private LevelData level;
        private Transform staticRoot;
        private Transform boxRoot;
        private Coroutine winPulseRoutine;

        public LevelData Level => level;
        public int Width => level != null ? level.Width : 0;
        public int Height => level != null ? level.Height : 0;

        public void Build(LevelData level)
        {
            Clear();

            this.level = level;
            staticRoot = new GameObject("Static").transform;
            staticRoot.SetParent(transform, false);
            boxRoot = new GameObject("Boxes").transform;
            boxRoot.SetParent(transform, false);

            if (level == null)
            {
                Debug.LogError("BoardView.Build: level is null.");
                return;
            }

            BuildStatic(level);
            BuildBoxes(level);
        }

        public void Clear()
        {
            if (winPulseRoutine != null)
            {
                StopCoroutine(winPulseRoutine);
                winPulseRoutine = null;
            }

            DestroyChild(staticRoot);
            DestroyChild(boxRoot);

            boxes.Clear();
            goalTiles.Clear();
            staticRoot = null;
            boxRoot = null;
        }

        private void DestroyChild(Component child)
        {
            if (child == null)
                return;

            if (Application.isPlaying)
                Destroy(child.gameObject);
            else
                DestroyImmediate(child.gameObject);
        }

        /// <summary>Returns the box GameObject currently mapped to a cell, or null.</summary>
        public GameObject GetBoxAt(GridPosition position)
            => boxes.TryGetValue(position, out GameObject box) ? box : null;

        /// <summary>
        /// Updates the logical box mapping after the animator has finished moving a box.
        /// </summary>
        public void MoveBox(GridPosition from, GridPosition to)
        {
            if (!boxes.TryGetValue(from, out GameObject box))
            {
                Debug.LogError($"BoardView.MoveBox: no box at {from}.");
                return;
            }

            boxes.Remove(from);
            boxes[to] = box;
            ApplyBoxColor(box, to);
        }

        /// <summary>
        /// Reconciles the box GameObjects with an arbitrary set of positions. Used when a
        /// restored run (or a reset) differs from the level's initial box layout.
        /// </summary>
        public void SyncBoxes(System.Collections.Generic.IEnumerable<GridPosition> positions)
        {
            var wanted = new HashSet<GridPosition>(positions);

            var toRemove = new List<GridPosition>();
            foreach (var pair in boxes)
            {
                if (!wanted.Contains(pair.Key))
                    toRemove.Add(pair.Key);
            }

            foreach (GridPosition position in toRemove)
            {
                DestroyChild(boxes[position].transform);
                boxes.Remove(position);
            }

            foreach (GridPosition position in wanted)
            {
                if (boxes.ContainsKey(position))
                    continue;

                GameObject box = PrimitiveFactory.Create(
                    "Box",
                    boxRoot,
                    BoardGeometry.CellToWorld(position, level.Width, level.Height) + new Vector3(0f, boxScale * 0.5f, 0f),
                    new Vector3(boxScale, boxScale, boxScale),
                    StyledRole.Box,
                    PrimitiveFactory.GetCube());

                boxes[position] = box;
                ApplyBoxColor(box, position);
            }
        }

        public bool IsGoalAt(GridPosition position)
        {
            return level != null
                && level.TryGetCell(position.X, position.Y, out char cell)
                && XsbParser.IsGoal(cell);
        }

        /// <summary>
        /// Plays a single restrained scale pulse on goal tiles and boxes to celebrate a win.
        /// Targets return to their exact base scales when the pulse finishes.
        /// </summary>
        public void PlayWinPulse()
        {
            if (winPulseRoutine != null)
                StopCoroutine(winPulseRoutine);

            winPulseRoutine = StartCoroutine(WinPulseRoutine());
        }

        private IEnumerator WinPulseRoutine()
        {
            var targets = new List<(Transform transform, Vector3 baseScale)>();
            foreach (Transform tile in goalTiles)
            {
                if (tile != null)
                    targets.Add((tile, tile.localScale));
            }

            foreach (GameObject box in boxes.Values)
            {
                if (box != null)
                    targets.Add((box.transform, box.transform.localScale));
            }

            float duration = Mathf.Max(0.0001f, winPulseDuration);
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float k = Mathf.Clamp01(elapsed / duration);
                float envelope = winPulseCurve != null
                    ? winPulseCurve.Evaluate(k)
                    : Mathf.Sin(k * Mathf.PI);
                float multiplier = 1f + (winPulseScale - 1f) * envelope;

                foreach ((Transform target, Vector3 baseScale) in targets)
                {
                    if (target != null)
                        target.localScale = baseScale * multiplier;
                }

                yield return null;
            }

            foreach ((Transform target, Vector3 baseScale) in targets)
            {
                if (target != null)
                    target.localScale = baseScale;
            }

            winPulseRoutine = null;
        }

        private void BuildStatic(LevelData level)
        {
            for (int y = 0; y < level.Height; y++)
            {
                string row = level.map[y];
                if (row == null)
                    continue;

                for (int x = 0; x < row.Length; x++)
                {
                    GridPosition cell = new GridPosition(x, y);
                    if (row[x] == XsbParser.Wall)
                    {
                        PrimitiveFactory.Create(
                            "Wall",
                            staticRoot,
                            BoardGeometry.CellToWorld(cell, level.Width, level.Height) + new Vector3(0f, wallHeight * 0.5f, 0f),
                            new Vector3(BoardGeometry.CellSize, wallHeight, BoardGeometry.CellSize),
                            StyledRole.Wall,
                            PrimitiveFactory.GetCube());
                    }
                    else
                    {
                        bool isGoal = XsbParser.IsGoal(row[x]);
                        GameObject tile = PrimitiveFactory.Create(
                            isGoal ? "Goal" : "Floor",
                            staticRoot,
                            BoardGeometry.CellToWorld(cell, level.Width, level.Height) + new Vector3(0f, tileThickness * 0.5f, 0f),
                            new Vector3(BoardGeometry.CellSize, tileThickness, BoardGeometry.CellSize),
                            isGoal ? StyledRole.Goal : StyledRole.Floor,
                            PrimitiveFactory.GetCube());

                        if (isGoal)
                            goalTiles.Add(tile.transform);
                    }
                }
            }
        }

        private void BuildBoxes(LevelData level)
        {
            foreach (GridPosition position in level.BoxPositions)
            {
                GameObject box = PrimitiveFactory.Create(
                    "Box",
                    boxRoot,
                    BoardGeometry.CellToWorld(position, level.Width, level.Height) + new Vector3(0f, boxScale * 0.5f, 0f),
                    new Vector3(boxScale, boxScale, boxScale),
                    StyledRole.Box,
                    PrimitiveFactory.GetCube());

                boxes[position] = box;
                ApplyBoxColor(box, position);
            }
        }

        private void ApplyBoxColor(GameObject box, GridPosition position)
        {
            var renderer = box.GetComponent<MeshRenderer>();
            if (renderer == null)
                return;

            PrimitiveFactory.ApplyRole(renderer, IsGoalAt(position) ? StyledRole.BoxOnGoal : StyledRole.Box);
        }
    }
}
