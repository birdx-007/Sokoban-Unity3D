using UnityEngine;
using UnityEngine.UI;

namespace BoxPushing.Gameplay
{
    /// <summary>Builds and drives the main menu UI (start / quit).</summary>
    public class MainMenuController : MonoBehaviour
    {
        private void Start()
        {
            Build();
        }

        private void Build()
        {
            Canvas canvas = UiFactory.CreateCanvas("MainMenuCanvas", transform);
            RectTransform root = (RectTransform)canvas.transform;

            Image background = UiFactory.CreateImage(root, "Background", new Color(0.12f, 0.14f, 0.20f, 1f));
            UiFactory.Stretch((RectTransform)background.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            Text title = UiFactory.CreateText(root, "Title", "推箱子", 84, Color.white, TextAnchor.MiddleCenter);
            UiFactory.Stretch(
                (RectTransform)title.transform,
                new Vector2(0.5f, 0.66f), new Vector2(0.5f, 0.90f),
                Vector2.zero, Vector2.zero);

            Button start = UiFactory.CreateButton(root, "StartButton", "开始游戏", () => GameFlow.Instance?.GoToLevelSelect());
            UiFactory.Stretch(
                (RectTransform)start.transform,
                new Vector2(0.35f, 0.44f), new Vector2(0.65f, 0.58f),
                Vector2.zero, Vector2.zero);

            Button quit = UiFactory.CreateButton(root, "QuitButton", "退出", QuitGame);
            UiFactory.Stretch(
                (RectTransform)quit.transform,
                new Vector2(0.35f, 0.27f), new Vector2(0.65f, 0.41f),
                Vector2.zero, Vector2.zero);
        }

        private void QuitGame()
        {
#if UNITY_EDITOR
            // Stop play mode without taking a compile-time dependency on UnityEditor in
            // this runtime assembly.
            System.Type editorApplication = System.Type.GetType("UnityEditor.EditorApplication, UnityEditor");
            editorApplication?.GetProperty("isPlaying")?.SetValue(null, false);
#else
            Application.Quit();
#endif
        }
    }
}
