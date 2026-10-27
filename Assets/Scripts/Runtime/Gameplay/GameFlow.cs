using System.Collections;
using System.Collections.Generic;
using BoxPushing.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BoxPushing.Gameplay
{
    /// <summary>
    /// Persistent application flow state machine. Lives on the Persistent scene and
    /// additively loads/unloads MainMenu, LevelSelect and Gameplay around it. Only the
    /// Persistent scene is the active build scene; the others are additive.
    /// </summary>
    public class GameFlow : MonoBehaviour
    {
        public enum State
        {
            Boot,
            MainMenu,
            LevelSelect,
            Gameplay
        }

        public const string MainMenuScene = "MainMenu";
        public const string LevelSelectScene = "LevelSelect";
        public const string GameplayScene = "Gameplay";

        public static GameFlow Instance { get; private set; }

        [SerializeField] private int selectedLevel;
        [SerializeField] private LevelList levelList;

        public State CurrentState { get; private set; }
        public ILevelRepository Levels { get; private set; }
        public int SelectedLevel => selectedLevel;

        public bool HasNextLevel => Levels != null && selectedLevel + 1 < Levels.Count;

        private string currentAdditive;
        private Coroutine transitionRoutine;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            Levels = new LevelListRepository(levelList);
        }

        private void Start()
        {
            CurrentState = State.Boot;
            GoToMainMenu();
        }

        public void GoToMainMenu() => TransitionTo(MainMenuScene, State.MainMenu);

        public void GoToLevelSelect() => TransitionTo(LevelSelectScene, State.LevelSelect);

        public void StartLevel(int index)
        {
            selectedLevel = index;
            TransitionTo(GameplayScene, State.Gameplay);
        }

        public void StartNextLevel()
        {
            if (HasNextLevel)
                StartLevel(selectedLevel + 1);
        }

        public void ReturnToLevelSelect() => TransitionTo(LevelSelectScene, State.LevelSelect);

        private void TransitionTo(string sceneName, State newState)
        {
            CurrentState = newState;

            if (transitionRoutine != null)
            {
                StopCoroutine(transitionRoutine);
                transitionRoutine = null;
            }

            TransitionOverlay.Instance?.Show();
            transitionRoutine = StartCoroutine(SwapAdditiveScene(sceneName));
        }

        private IEnumerator SwapAdditiveScene(string targetScene)
        {
            currentAdditive = targetScene;

            // Desired sequence: fade in -> swap scenes -> fade out. Wait until the mask
            // fully covers the outgoing scene before unloading or loading anything.
            TransitionOverlay overlay = TransitionOverlay.Instance;
            while (overlay != null && !overlay.IsFullyOpaque)
                yield return null;

            // Unload every additive scene (including the target when it is already
            // loaded) so re-entering a scene rebuilds it from scratch. This is required
            // for next-level: Gameplay must reload so GameplayBootstrap re-runs for the
            // newly selected level index.
            var toUnload = new List<string>();
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (scene.isLoaded && scene.name != SceneManager.GetActiveScene().name)
                {
                    toUnload.Add(scene.name);
                }
            }

            foreach (string sceneName in toUnload)
            {
                AsyncOperation unload = SceneManager.UnloadSceneAsync(sceneName);
                if (unload != null)
                    yield return unload;
            }

            AsyncOperation load = SceneManager.LoadSceneAsync(targetScene, LoadSceneMode.Additive);
            if (load != null)
                yield return load;

            TransitionOverlay.Instance?.FadeOut();
            transitionRoutine = null;
        }
    }
}
