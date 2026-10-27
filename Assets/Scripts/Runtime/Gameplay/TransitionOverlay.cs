using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace BoxPushing.Gameplay
{
    /// <summary>
    /// Full-screen opaque transition mask living on the Persistent scene. GameFlow shows it
    /// the moment a scene switch starts and fades it out once the additive load completes.
    /// It owns a top-most ScreenSpaceOverlay canvas so it always covers in-game UI.
    /// </summary>
    public class TransitionOverlay : MonoBehaviour
    {
        [Tooltip("Opaque cover color. Defaults to the palette background (#F4F0E6).")]
        [SerializeField] private Color overlayColor = Palette.BackgroundCenter;

        [Tooltip("Fade-in duration in seconds when the mask covers the outgoing scene.")]
        [SerializeField] private float fadeInDuration = 0.2f;

        [Tooltip("Fade-out duration in seconds after the target scene finishes loading.")]
        [SerializeField] private float fadeOutDuration = 0.3f;

        [Tooltip("Minimum time the fully-opaque mask stays before starting the fade-out.")]
        [SerializeField] private float holdDuration = 0.08f;

        [Tooltip("Canvas sorting order. High value keeps the mask above all other overlay canvases.")]
        [SerializeField] private int sortingOrder = 100;

        public static TransitionOverlay Instance { get; private set; }

        private CanvasGroup group;
        private Coroutine fadeRoutine;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (!Application.isPlaying)
                return;

            Build();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void Build()
        {
            var go = new GameObject("TransitionOverlayCanvas");
            go.transform.SetParent(transform, false);

            Canvas canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;

            CanvasScaler scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            go.AddComponent<GraphicRaycaster>();

            Image image = UiFactory.CreateImage(go.transform, "Overlay", overlayColor);
            UiFactory.Stretch((RectTransform)image.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            group = go.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
        }

        /// <summary>True when the mask has reached full opacity.</summary>
        public bool IsFullyOpaque => group != null && group.alpha >= 1f - 0.0001f;

        /// <summary>Fades the mask in to fully opaque over <see cref="fadeInDuration"/> seconds.</summary>
        public void Show()
        {
            if (group == null)
                return;

            StopFade();
            group.blocksRaycasts = true;
            fadeRoutine = StartCoroutine(FadeInRoutine());
        }

        /// <summary>
        /// Finishes any partial fade-in, holds the opaque mask briefly, then fades out
        /// over <see cref="fadeOutDuration"/>. Waiting for full coverage prevents a fast
        /// scene load from cutting the transition short and flashing between screens.
        /// </summary>
        public void FadeOut()
        {
            if (group == null)
                return;

            StopFade();
            fadeRoutine = StartCoroutine(FadeOutRoutine());
        }

        private IEnumerator FadeInRoutine()
        {
            yield return AnimateAlpha(group.alpha, 1f, fadeInDuration);
            group.alpha = 1f;
            fadeRoutine = null;
        }

        private IEnumerator FadeOutRoutine()
        {
            group.blocksRaycasts = true;

            // Complete any interrupted fade-in before revealing the new scene.
            if (group.alpha < 1f)
            {
                float remaining = Mathf.Max(0.0001f, fadeInDuration) * (1f - group.alpha);
                yield return AnimateAlpha(group.alpha, 1f, remaining);
            }

            if (holdDuration > 0f)
                yield return new WaitForSecondsRealtime(holdDuration);

            yield return AnimateAlpha(1f, 0f, fadeOutDuration);
            group.alpha = 0f;
            group.blocksRaycasts = false;
            fadeRoutine = null;
        }

        private IEnumerator AnimateAlpha(float from, float to, float duration)
        {
            float safeDuration = Mathf.Max(0.0001f, duration);
            float elapsed = 0f;

            while (elapsed < safeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(elapsed / safeDuration);
                group.alpha = Mathf.Lerp(from, to, k);
                yield return null;
            }

            group.alpha = to;
        }

        private void StopFade()
        {
            if (fadeRoutine != null)
            {
                StopCoroutine(fadeRoutine);
                fadeRoutine = null;
            }
        }
    }
}
