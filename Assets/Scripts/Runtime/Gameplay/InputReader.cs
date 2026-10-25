using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BoxPushing.Gameplay
{
    /// <summary>
    /// Owns the shared Input Action Asset and exposes the Gameplay map to the gameplay
    /// scene. Lives on the Persistent scene. The UI map is consumed by the EventSystem's
    /// InputSystemUIInputModule.
    /// </summary>
    public class InputReader : MonoBehaviour
    {
        [SerializeField] private InputActionAsset inputActions;

        public static InputReader Instance { get; private set; }

        public event Action UndoPressed;
        public event Action RedoPressed;
        public event Action RestartPressed;
        public event Action PausePressed;

        private InputAction moveAction;
        private InputAction undoAction;
        private InputAction redoAction;
        private InputAction restartAction;
        private InputAction pauseAction;

        public InputActionAsset InputActions => inputActions;

        /// <summary>Current Move value from the Gameplay map (zero when disabled/unassigned).</summary>
        public Vector2 MoveValue => moveAction != null ? moveAction.ReadValue<Vector2>() : Vector2.zero;

        private void OnEnable()
        {
            Instance = this;

            if (inputActions == null)
            {
                Debug.LogError("InputReader: inputActions is not assigned.");
                return;
            }

            InputActionMap gameplay = inputActions.FindActionMap("Gameplay", throwIfNotFound: false);
            if (gameplay == null)
            {
                Debug.LogError("InputReader: 'Gameplay' action map not found.");
                return;
            }

            moveAction = gameplay.FindAction("Move", throwIfNotFound: false);
            undoAction = gameplay.FindAction("Undo", throwIfNotFound: false);
            redoAction = gameplay.FindAction("Redo", throwIfNotFound: false);
            restartAction = gameplay.FindAction("Restart", throwIfNotFound: false);
            pauseAction = gameplay.FindAction("Pause", throwIfNotFound: false);

            if (undoAction != null)
                undoAction.performed += OnUndoPerformed;
            if (redoAction != null)
                redoAction.performed += OnRedoPerformed;
            if (restartAction != null)
                restartAction.performed += OnRestartPerformed;
            if (pauseAction != null)
                pauseAction.performed += OnPausePerformed;

            gameplay.Enable();
        }

        private void OnDisable()
        {
            if (Instance == this)
                Instance = null;

            if (undoAction != null)
                undoAction.performed -= OnUndoPerformed;
            if (redoAction != null)
                redoAction.performed -= OnRedoPerformed;
            if (restartAction != null)
                restartAction.performed -= OnRestartPerformed;
            if (pauseAction != null)
                pauseAction.performed -= OnPausePerformed;

            if (inputActions != null)
                inputActions.FindActionMap("Gameplay", throwIfNotFound: false)?.Disable();
        }

        private void OnUndoPerformed(InputAction.CallbackContext context) => UndoPressed?.Invoke();
        private void OnRedoPerformed(InputAction.CallbackContext context) => RedoPressed?.Invoke();
        private void OnRestartPerformed(InputAction.CallbackContext context) => RestartPressed?.Invoke();
        private void OnPausePerformed(InputAction.CallbackContext context) => PausePressed?.Invoke();
    }
}
