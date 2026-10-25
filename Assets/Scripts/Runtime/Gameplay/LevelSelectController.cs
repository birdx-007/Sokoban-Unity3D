using UnityEngine;
using UnityEngine.UI;

namespace BoxPushing.Gameplay
{
    /// <summary>
    /// Builds and drives the level-select screen: a scrollable, linearly-unlocked list of
    /// levels with completion status, plus an editor-only unlock-all helper.
    /// </summary>
    public class LevelSelectController : MonoBehaviour
    {
        private GameObject progressPopup;
        private Text progressMessage;
        private int pendingLevelIndex;

        private void Start()
        {
            Build();
        }

        private void Build()
        {
            Canvas canvas = UiFactory.CreateCanvas("LevelSelectCanvas", transform);
            RectTransform root = (RectTransform)canvas.transform;

            Image background = UiFactory.CreateImage(root, "Background", new Color(0.12f, 0.14f, 0.20f, 1f));
            UiFactory.Stretch((RectTransform)background.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            Text title = UiFactory.CreateText(root, "Title", "选择关卡", 60, Color.white, TextAnchor.MiddleCenter);
            UiFactory.Stretch(
                (RectTransform)title.transform,
                new Vector2(0.5f, 0.88f), new Vector2(0.5f, 1f),
                Vector2.zero, Vector2.zero);

            RectTransform scrollView = BuildScrollView(root);
            UiFactory.Stretch(scrollView, new Vector2(0.15f, 0.12f), new Vector2(0.85f, 0.86f), Vector2.zero, Vector2.zero);

            Button back = UiFactory.CreateButton(root, "BackButton", "返回", () => GameFlow.Instance?.GoToMainMenu(), 28);
            UiFactory.Stretch(
                (RectTransform)back.transform,
                new Vector2(0.38f, 0.03f), new Vector2(0.62f, 0.10f),
                Vector2.zero, Vector2.zero);

#if UNITY_EDITOR
            Button unlockAll = UiFactory.CreateButton(root, "UnlockAllButton", "全解锁", UnlockAll, 24);
            UiFactory.Stretch(
                (RectTransform)unlockAll.transform,
                new Vector2(0.85f, 0.03f), new Vector2(0.98f, 0.10f),
                Vector2.zero, Vector2.zero);
#endif

            BuildProgressPopup(root);
        }

        private RectTransform BuildScrollView(Transform parent)
        {
            RectTransform scrollRect = UiFactory.CreateRect("ScrollView", parent);
            var scrollViewImage = scrollRect.gameObject.AddComponent<Image>();
            scrollViewImage.color = new Color(0f, 0f, 0f, 0.35f);

            var scroll = scrollRect.gameObject.AddComponent<ScrollRect>();

            RectTransform viewport = UiFactory.CreateRect("Viewport", scrollRect);
            UiFactory.Stretch(viewport, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var viewportImage = viewport.gameObject.AddComponent<Image>();
            viewportImage.color = new Color(1f, 1f, 1f, 0.02f);
            var mask = viewport.gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            RectTransform content = UiFactory.CreateRect("Content", viewport);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.offsetMin = Vector2.zero;
            content.offsetMax = Vector2.zero;

            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 10, 10);
            layout.spacing = 10f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;

            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 40f;

            Populate(content);
            return scrollRect;
        }

        private void Populate(Transform content)
        {
            GameFlow flow = GameFlow.Instance;
            SaveManager saves = SaveManager.Instance;
            if (flow?.Levels == null)
                return;

            for (int i = 0; i < flow.Levels.Count; i++)
            {
                int index = i;
                string id = flow.Levels.GetLevelId(i);
                string name = flow.Levels.GetLevelName(i);
                bool completed = saves != null && saves.IsCompleted(id);
                bool unlocked = index == 0 || (saves != null && saves.IsCompleted(flow.Levels.GetLevelId(index - 1)));

                string status = unlocked ? (completed ? "已完成" : "未完成") : "锁定";
                string label = $"第 {index + 1} 关 · {name} · {status}";

                if (completed && saves != null)
                {
                    var entry = saves.GetEntry(id);
                    label += $"  (最佳 {entry.bestMoves} 步 / {entry.bestPushes} 推)";
                }

                Button button = UiFactory.CreateButton(content, $"LevelButton_{index}", label, () => OnLevelClicked(index), 30);

                button.interactable = unlocked;
                if (!unlocked)
                {
                    var image = button.GetComponent<Image>();
                    if (image != null)
                        image.color = new Color(0.30f, 0.30f, 0.32f, 0.5f);
                }

                var layout = button.GetComponent<LayoutElement>();
                if (layout == null)
                    layout = button.gameObject.AddComponent<LayoutElement>();
                layout.preferredHeight = 64f;
            }
        }

        private void OnLevelClicked(int index)
        {
            GameFlow flow = GameFlow.Instance;
            SaveManager saves = SaveManager.Instance;
            if (flow?.Levels == null)
                return;

            bool unlocked = index == 0
                || (saves != null && saves.IsCompleted(flow.Levels.GetLevelId(index - 1)));
            if (!unlocked)
                return;

            string id = flow.Levels.GetLevelId(index);
            if (saves != null && saves.HasProgress(id))
            {
                pendingLevelIndex = index;
                ShowProgressPopup(true, flow.Levels.GetLevelName(index));
            }
            else
            {
                flow.StartLevel(index);
            }
        }

        private void BuildProgressPopup(RectTransform root)
        {
            progressPopup = UiFactory.CreateRect("ProgressPopup", root).gameObject;
            UiFactory.Stretch((RectTransform)progressPopup.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            Image dim = UiFactory.CreateImage(progressPopup.transform, "Dim", new Color(0f, 0f, 0f, 1f));
            UiFactory.Stretch((RectTransform)dim.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            Image panel = UiFactory.CreateImage(progressPopup.transform, "Panel", new Color(0.16f, 0.18f, 0.24f, 1f));
            UiFactory.Stretch((RectTransform)panel.transform, new Vector2(0.25f, 0.25f), new Vector2(0.75f, 0.75f), Vector2.zero, Vector2.zero);

            Text title = UiFactory.CreateText(panel.transform, "Title", "继续进度？", 44, Color.white, TextAnchor.MiddleCenter);
            UiFactory.Stretch((RectTransform)title.transform, new Vector2(0.05f, 0.76f), new Vector2(0.95f, 0.92f), Vector2.zero, Vector2.zero);

            progressMessage = UiFactory.CreateText(panel.transform, "Message", "", 30, Color.white, TextAnchor.MiddleCenter);
            UiFactory.Stretch((RectTransform)progressMessage.transform, new Vector2(0.05f, 0.52f), new Vector2(0.95f, 0.74f), Vector2.zero, Vector2.zero);

            Button continueButton = UiFactory.CreateButton(panel.transform, "ContinueButton", "继续已有进度", OnContinueProgress, 28);
            UiFactory.Stretch((RectTransform)continueButton.transform, new Vector2(0.35f, 0.40f), new Vector2(0.65f, 0.52f), Vector2.zero, Vector2.zero);

            Button restartButton = UiFactory.CreateButton(panel.transform, "RestartProgressButton", "重新开始", OnRestartProgress, 28);
            UiFactory.Stretch((RectTransform)restartButton.transform, new Vector2(0.35f, 0.24f), new Vector2(0.65f, 0.36f), Vector2.zero, Vector2.zero);

            Button cancelButton = UiFactory.CreateButton(panel.transform, "CancelProgressButton", "取消", () => ShowProgressPopup(false), 28);
            UiFactory.Stretch((RectTransform)cancelButton.transform, new Vector2(0.35f, 0.08f), new Vector2(0.65f, 0.20f), Vector2.zero, Vector2.zero);

            progressPopup.SetActive(false);
        }

        private void ShowProgressPopup(bool show, string levelName = null)
        {
            if (progressPopup == null)
                return;

            if (show && !string.IsNullOrEmpty(levelName) && progressMessage != null)
                progressMessage.text = $"「{levelName}」有未完成的进度";

            progressPopup.SetActive(show);
        }

        private void OnContinueProgress()
        {
            ShowProgressPopup(false);
            GameFlow.Instance?.StartLevel(pendingLevelIndex);
        }

        private void OnRestartProgress()
        {
            GameFlow flow = GameFlow.Instance;
            SaveManager saves = SaveManager.Instance;
            if (flow?.Levels != null && saves != null)
                saves.ClearProgress(flow.Levels.GetLevelId(pendingLevelIndex));

            ShowProgressPopup(false);
            flow?.StartLevel(pendingLevelIndex);
        }

        private void UnlockAll()
        {
            GameFlow flow = GameFlow.Instance;
            SaveManager saves = SaveManager.Instance;
            if (flow?.Levels == null || saves == null)
                return;

            for (int i = 0; i < flow.Levels.Count; i++)
                saves.UnlockLevel(flow.Levels.GetLevelId(i));

            // Rebuild the list so statuses reflect the change immediately.
            DestroyCanvasChildren();
            Build();
        }

        private void DestroyCanvasChildren()
        {
            Canvas canvas = GetComponentInChildren<Canvas>();
            if (canvas != null)
                Destroy(canvas.gameObject);
        }
    }
}
