using System;
using UnityEngine;
using UnityEngine.UI;

namespace BoxPushing.Gameplay
{
    /// <summary>
    /// Gameplay HUD and overlays (pause menu + win screen). GameplayBootstrap owns the
    /// session and drives these through the public methods; UI actions are exposed as
    /// events so the bootstrap can react.
    /// </summary>
    public class GameplayHud : MonoBehaviour
    {
        public event Action PauseClicked;
        public event Action RestartClicked;
        public event Action ResumeClicked;
        public event Action ExitToLevelSelectClicked;
        public event Action ReplayClicked;
        public event Action NextLevelClicked;

        private Text countsText;
        private GameObject pauseOverlay;
        private GameObject winOverlay;
        private Text winScoreText;
        private Button nextLevelButton;

        private void Awake()
        {
            Build();
            ShowPause(false);
            ShowWin(false, 0, 0, 0, 0, false);
        }

        public void SetCounts(int moves, int pushes)
        {
            if (countsText != null)
                countsText.text = $"步数 {moves}    推箱 {pushes}";
        }

        public void ShowPause(bool show)
        {
            if (pauseOverlay != null)
                pauseOverlay.SetActive(show);
        }

        public void ShowWin(bool show, int moves, int pushes, int bestMoves, int bestPushes, bool hasNext)
        {
            if (winOverlay != null)
                winOverlay.SetActive(show);

            if (winScoreText != null)
            {
                string best = bestMoves >= 0
                    ? $"最佳  {bestMoves} 步 / {bestPushes} 推"
                    : "暂无最佳记录";
                winScoreText.text = $"本次  {moves} 步 / {pushes} 推\n{best}";
            }

            if (nextLevelButton != null)
                nextLevelButton.interactable = hasNext;
        }

        private void Build()
        {
            Canvas canvas = UiFactory.CreateCanvas("GameplayHudCanvas", transform);
            RectTransform root = (RectTransform)canvas.transform;

            // Top-left: pause + restart.
            Button pause = UiFactory.CreateButton(root, "PauseButton", "暂停", () => PauseClicked?.Invoke(), 26);
            UiFactory.SetAnchored((RectTransform)pause.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(20f, -20f), new Vector2(120f, 48f));

            Button restart = UiFactory.CreateButton(root, "RestartButton", "重开", () => RestartClicked?.Invoke(), 26);
            UiFactory.SetAnchored((RectTransform)restart.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(150f, -20f), new Vector2(120f, 48f));

            // Top-right: moves/pushes counter box.
            Image counterBox = UiFactory.CreateImage(root, "CounterBox", new Color(0f, 0f, 0f, 0.45f));
            UiFactory.SetAnchored((RectTransform)counterBox.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-20f, -20f), new Vector2(300f, 56f));

            countsText = UiFactory.CreateText(counterBox.transform, "Counts", "", 28, Color.white, TextAnchor.MiddleCenter);
            UiFactory.Stretch((RectTransform)countsText.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            BuildPauseOverlay(root);
            BuildWinOverlay(root);
        }

        private void BuildPauseOverlay(RectTransform root)
        {
            pauseOverlay = UiFactory.CreateRect("PauseOverlay", root).gameObject;
            UiFactory.Stretch((RectTransform)pauseOverlay.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            Image dim = UiFactory.CreateImage(pauseOverlay.transform, "Dim", new Color(0f, 0f, 0f, 1f));
            UiFactory.Stretch((RectTransform)dim.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            Text title = UiFactory.CreateText(pauseOverlay.transform, "Title", "已暂停", 60, Color.white, TextAnchor.MiddleCenter);
            UiFactory.Stretch(
                (RectTransform)title.transform,
                new Vector2(0.5f, 0.62f), new Vector2(0.5f, 0.78f),
                Vector2.zero, Vector2.zero);

            Button resume = UiFactory.CreateButton(pauseOverlay.transform, "ResumeButton", "继续", () => ResumeClicked?.Invoke());
            UiFactory.Stretch(
                (RectTransform)resume.transform,
                new Vector2(0.35f, 0.42f), new Vector2(0.65f, 0.55f),
                Vector2.zero, Vector2.zero);

            Button exit = UiFactory.CreateButton(pauseOverlay.transform, "ExitButton", "返回选关", () => ExitToLevelSelectClicked?.Invoke());
            UiFactory.Stretch(
                (RectTransform)exit.transform,
                new Vector2(0.35f, 0.26f), new Vector2(0.65f, 0.39f),
                Vector2.zero, Vector2.zero);
        }

        private void BuildWinOverlay(RectTransform root)
        {
            winOverlay = UiFactory.CreateRect("WinOverlay", root).gameObject;
            UiFactory.Stretch((RectTransform)winOverlay.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            Image dim = UiFactory.CreateImage(winOverlay.transform, "Dim", new Color(0f, 0f, 0f, 1f));
            UiFactory.Stretch((RectTransform)dim.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            Text title = UiFactory.CreateText(winOverlay.transform, "Title", "胜利！", 76, new Color(1f, 0.9f, 0.4f, 1f), TextAnchor.MiddleCenter);
            UiFactory.Stretch(
                (RectTransform)title.transform,
                new Vector2(0.5f, 0.70f), new Vector2(0.5f, 0.88f),
                Vector2.zero, Vector2.zero);

            winScoreText = UiFactory.CreateText(winOverlay.transform, "Score", "", 36, Color.white, TextAnchor.MiddleCenter);
            UiFactory.Stretch(
                (RectTransform)winScoreText.transform,
                new Vector2(0.3f, 0.52f), new Vector2(0.7f, 0.68f),
                Vector2.zero, Vector2.zero);

            Button replay = UiFactory.CreateButton(winOverlay.transform, "ReplayButton", "重玩", () => ReplayClicked?.Invoke(), 28);
            UiFactory.Stretch(
                (RectTransform)replay.transform,
                new Vector2(0.35f, 0.34f), new Vector2(0.65f, 0.46f),
                Vector2.zero, Vector2.zero);

            Button next = UiFactory.CreateButton(winOverlay.transform, "NextButton", "下一关", () => NextLevelClicked?.Invoke(), 28);
            UiFactory.Stretch(
                (RectTransform)next.transform,
                new Vector2(0.35f, 0.20f), new Vector2(0.65f, 0.32f),
                Vector2.zero, Vector2.zero);
            nextLevelButton = next;

            Button exit = UiFactory.CreateButton(winOverlay.transform, "ExitButton", "返回", () => ExitToLevelSelectClicked?.Invoke(), 28);
            UiFactory.Stretch(
                (RectTransform)exit.transform,
                new Vector2(0.35f, 0.06f), new Vector2(0.65f, 0.18f),
                Vector2.zero, Vector2.zero);
        }
    }
}
