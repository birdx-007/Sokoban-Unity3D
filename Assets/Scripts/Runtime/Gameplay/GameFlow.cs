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

            if (currentAdditive == sceneName && transitionRoutine == null)
                return;

            if (transitionRoutine != null)
            {
                StopCoroutine(transitionRoutine);
                transitionRoutine = null;
            }

            transitionRoutine = StartCoroutine(SwapAdditiveScene(sceneName));
        }

        private IEnumerator SwapAdditiveScene(string targetScene)
        {
            currentAdditive = targetScene;

            // Unload every additive scene except the target. This also cleans up any
            // scene left behind by a superseded in-flight transition, which is the
            // source of the occasional "level not laid out" symptom.
            var toUnload = new List<string>();
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (scene.isLoaded
                    && scene.name != SceneManager.GetActiveScene().name
                    && scene.name != targetScene)
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

            if (!SceneManager.GetSceneByName(targetScene).isLoaded)
            {
                AsyncOperation load = SceneManager.LoadSceneAsync(targetScene, LoadSceneMode.Additive);
                if (load != null)
                    yield return load;
            }

            transitionRoutine = null;
        }
    }
}
