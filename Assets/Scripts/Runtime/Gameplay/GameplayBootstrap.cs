using BoxPushing.Core;
using UnityEngine;

namespace BoxPushing.Gameplay
{
    /// <summary>
    /// Drives one gameplay run for the selected level. Owns the <see cref="GameSession"/>,
    /// wires the new Input System through <see cref="InputReader"/>, builds the board view,
    /// plays animations, and coordinates the HUD / save flow.
    /// </summary>
    public class GameplayBootstrap : MonoBehaviour
    {
        [SerializeField] private MoveAnimationConfig animationConfig = new MoveAnimationConfig();
        [SerializeField] private GameplayCameraSettings cameraSettings = new GameplayCameraSettings();
        [SerializeField] private BlockFeedbackConfig blockFeedback = new BlockFeedbackConfig();

        [Header("Hold-to-repeat")]
        [SerializeField] private float initialRepeatDelay = 0.3f;
        [SerializeField] private float repeatInterval = 0.15f;

        private InputReader inputReader;
        private GameplayHud hud;

        private GameSession session;
        private BoardView board;
        private PlayerView player;
        private MoveAnimator animator;

        private LevelData level;
        private string levelId;
        private bool won;
        private bool paused;
        private bool progressSaved;

        private Direction? heldDirection;
        private float nextRepeatTime;

        private void Start()
        {
            inputReader = InputReader.Instance;
            hud = GetComponent<GameplayHud>();

            GameFlow flow = GameFlow.Instance;
            if (flow?.Levels == null)
            {
                Debug.LogError("GameplayBootstrap: GameFlow/level repository is not available.");
                return;
            }

            if (flow.SelectedLevel < 0 || flow.SelectedLevel >= flow.Levels.Count)
            {
                Debug.LogError($"GameplayBootstrap: invalid selected level index {flow.SelectedLevel}.");
                return;
            }

            levelId = flow.Levels.GetLevelId(flow.SelectedLevel);
            Bootstrap(flow.Levels.GetLevel(flow.SelectedLevel));

            SubscribeInput();
            SubscribeHud();
            Application.quitting += OnApplicationQuitting;
        }

        private void Update()
        {
            if (paused || won || inputReader == null || animator == null)
                return;

            Direction? direction = DirectionInput.FromVector(inputReader.MoveValue);
            if (direction.HasValue)
            {
                if (!heldDirection.HasValue || heldDirection.Value != direction.Value)
                {
                    heldDirection = direction.Value;
                    nextRepeatTime = Time.unscaledTime + initialRepeatDelay;
                    TryMove(direction.Value);
                }
                else if (Time.unscaledTime >= nextRepeatTime)
                {
                    nextRepeatTime = Time.unscaledTime + repeatInterval;
                    TryMove(direction.Value);
                }
            }
            else
            {
                heldDirection = null;
            }
        }

        private void OnDestroy()
        {
            UnsubscribeInput();
            UnsubscribeHud();
            Application.quitting -= OnApplicationQuitting;

            if (Application.isPlaying)
                SaveProgressIfNeeded();
        }

        // ---- Session driving ----

        public void Bootstrap(LevelData levelData)
        {
            level = levelData;
            if (level == null)
            {
                Debug.LogError("GameplayBootstrap.Bootstrap: level is null.");
                return;
            }

            session = new GameSession(level);
            won = false;
            paused = false;
            progressSaved = false;
            heldDirection = null;

            RunSnapshot snapshot = SaveManager.Instance?.GetProgress(levelId);
            if (snapshot != null && !session.TryRestore(snapshot))
            {
                Debug.LogWarning($"GameplayBootstrap: ignoring invalid in-progress save for '{levelId}'.");
                SaveManager.Instance?.ClearProgress(levelId);
            }

            if (board == null)
                board = gameObject.AddComponent<BoardView>();
            board.Build(level);
            board.SyncBoxes(session.State.BoxPositions);

            DestroyPlayer();
            player = PlayerView.Create(
                transform,
                BoardGeometry.CellToWorld(session.State.PlayerPosition, level.Width, level.Height),
                session.State.Facing);

            if (animator == null)
                animator = gameObject.AddComponent<MoveAnimator>();

            Camera gameplayCamera = Camera.main;
            if (gameplayCamera != null)
                GameplayCamera.Frame(gameplayCamera, level, cameraSettings);

            RefreshHud();
            hud?.ShowPause(false);
            hud?.ShowWin(false, 0, 0, 0, 0, false);
        }

        public void TryMove(Direction direction)
        {
            if (animator == null || animator.IsAnimating || won || paused)
                return;

            (MoveResult result, MoveDelta delta) = session.TryMove(direction);
            if (!result.Success)
            {
                OnBlocked(direction);
                return;
            }

            if (result.PushedBox)
                AudioManager.Instance?.PlayPush();
            else
                AudioManager.Instance?.PlayMove();

            PlayMove(delta);
            RefreshHud();

            if (result.Won)
                OnWon();
        }

        public void Undo()
        {
            if (animator == null || animator.IsAnimating || won || paused)
                return;

            MoveDelta? delta = session.Undo();
            if (delta.HasValue)
            {
                PlayMove(delta.Value);
                RefreshHud();
            }
        }

        public void Redo()
        {
            if (animator == null || animator.IsAnimating || won || paused)
                return;

            MoveDelta? delta = session.Redo();
            if (delta.HasValue)
            {
                PlayMove(delta.Value);
                RefreshHud();
            }
        }

        public void RestartRun()
        {
            if (session == null || animator == null || animator.IsAnimating)
                return;

            session.Reset();
            won = false;
            paused = false;
            progressSaved = false;
            heldDirection = null;

            SyncViewToState();
            RefreshHud();
            hud?.ShowPause(false);
            hud?.ShowWin(false, 0, 0, 0, 0, false);
        }

        // ---- Presentation helpers ----

        private void OnBlocked(Direction direction)
        {
            AudioManager.Instance?.PlayWallHit();
            player?.PlayBlockBounce(direction, blockFeedback);
        }

        private void PlayMove(MoveDelta delta)
        {
            animator.Play(delta, player, board, animationConfig, () =>
            {
                if (delta.PushedBox)
                    board.MoveBox(delta.BoxFrom.Value, delta.BoxTo.Value);
            });
        }

        private void SyncViewToState()
        {
            if (board != null)
                board.SyncBoxes(session.State.BoxPositions);

            if (player != null && level != null)
            {
                player.transform.position = BoardGeometry.CellToWorld(session.State.PlayerPosition, level.Width, level.Height);
                player.SetFacing(session.State.Facing);
            }
        }

        private void RefreshHud()
        {
            hud?.SetCounts(session.State.Moves, session.State.Pushes);
        }

        private void OnWon()
        {
            won = true;
            heldDirection = null;

            RunSnapshot snapshot = session.ToSnapshot();
            if (SaveManager.Instance != null && !string.IsNullOrEmpty(levelId))
                SaveManager.Instance.RecordWin(levelId, snapshot);

            LevelSaveData entry = SaveManager.Instance?.GetEntry(levelId);
            int bestMoves = entry != null && entry.completed ? entry.bestMoves : -1;
            int bestPushes = entry != null && entry.completed ? entry.bestPushes : -1;
            hud?.ShowWin(true, snapshot.moves, snapshot.pushes, bestMoves, bestPushes, GameFlow.Instance?.HasNextLevel ?? false);

            board?.PlayWinPulse();
            AudioManager.Instance?.PlayWin();
        }

        private void SaveProgressIfNeeded()
        {
            if (progressSaved || won || session == null)
                return;

            progressSaved = true;

            SaveManager saves = SaveManager.Instance;
            if (saves == null || string.IsNullOrEmpty(levelId))
                return;

            if (session.State.Moves > 0)
                saves.RecordProgress(levelId, session.ToSnapshot());
            else
                saves.ClearProgress(levelId);
        }

        private void DestroyPlayer()
        {
            if (player == null)
                return;

            if (Application.isPlaying)
                Destroy(player.gameObject);
            else
                DestroyImmediate(player.gameObject);

            player = null;
        }

        // ---- Input events ----

        private void SubscribeInput()
        {
            if (inputReader == null)
                return;

            inputReader.UndoPressed += OnUndoPressed;
            inputReader.RedoPressed += OnRedoPressed;
            inputReader.RestartPressed += OnRestartPressed;
            inputReader.PausePressed += OnPausePressed;
        }

        private void UnsubscribeInput()
        {
            if (inputReader == null)
                return;

            inputReader.UndoPressed -= OnUndoPressed;
            inputReader.RedoPressed -= OnRedoPressed;
            inputReader.RestartPressed -= OnRestartPressed;
            inputReader.PausePressed -= OnPausePressed;
        }

        private void OnUndoPressed() { if (!paused && !won) Undo(); }
        private void OnRedoPressed() { if (!paused && !won) Redo(); }
        private void OnRestartPressed() { if (!paused && !won) RestartRun(); }
        private void OnPausePressed()
        {
            if (won)
                return;

            SetPaused(!paused);
        }

        private void SetPaused(bool value)
        {
            paused = value;
            hud?.ShowPause(value);
        }

        // ---- HUD events ----

        private void SubscribeHud()
        {
            if (hud == null)
                return;

            hud.PauseClicked += OnHudPauseClicked;
            hud.RestartClicked += OnHudRestartClicked;
            hud.ResumeClicked += OnHudResumeClicked;
            hud.ExitToLevelSelectClicked += OnExitToLevelSelect;
            hud.ReplayClicked += OnHudReplayClicked;
            hud.NextLevelClicked += OnNextLevelClicked;
        }

        private void UnsubscribeHud()
        {
            if (hud == null)
                return;

            hud.PauseClicked -= OnHudPauseClicked;
            hud.RestartClicked -= OnHudRestartClicked;
            hud.ResumeClicked -= OnHudResumeClicked;
            hud.ExitToLevelSelectClicked -= OnExitToLevelSelect;
            hud.ReplayClicked -= OnHudReplayClicked;
            hud.NextLevelClicked -= OnNextLevelClicked;
        }

        private void OnHudPauseClicked() => SetPaused(true);
        private void OnHudRestartClicked() { if (!paused && !won) RestartRun(); }
        private void OnHudResumeClicked() => SetPaused(false);
        private void OnHudReplayClicked() => RestartRun();
        private void OnNextLevelClicked()
        {
            SaveProgressIfNeeded();
            GameFlow.Instance?.StartNextLevel();
        }

        private void OnExitToLevelSelect()
        {
            SaveProgressIfNeeded();
            GameFlow.Instance?.ReturnToLevelSelect();
        }

        private void OnApplicationQuitting()
        {
            SaveProgressIfNeeded();
        }
    }
}
