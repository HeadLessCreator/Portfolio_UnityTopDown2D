using UnityEngine;
using UnityEngine.InputSystem;
using Portfolio.UI.Input;

namespace Portfolio.Gameplay.Player
{
    public class PlayerInputReader : MonoBehaviour
    {
        [Header("Input System")]
        [SerializeField] private InputActionReference moveActionRef;

        [Header("Virtual Joystick")]
        [SerializeField] private VirtualJoystick virtualJoystick;

        public Vector2 MoveInput { get; private set; }

        private Vector2 joystickInput;
        private bool isInputEnabled;

        private InputAction MoveAction => moveActionRef != null
            ? moveActionRef.action
            : null;

        private void OnEnable()
        {
            MoveAction?.Enable();

            if (virtualJoystick != null)
            {
                virtualJoystick.OnInputVectorEvent -= HandleJoystickInput;
                virtualJoystick.OnInputVectorEvent += HandleJoystickInput;
            }

            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.OnStateChanged.RemoveListener(HandleGameStateChanged);
                GameStateManager.Instance.OnStateChanged.AddListener(HandleGameStateChanged);

                HandleGameStateChanged(GameStateManager.Instance.CurrentState);
            }
            else
            {
                ApplyInputEnabled(false);
            }
        }

        private void OnDisable()
        {
            MoveAction?.Disable();

            if (virtualJoystick != null)
            {
                virtualJoystick.OnInputVectorEvent -= HandleJoystickInput;
            }

            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.OnStateChanged.RemoveListener(HandleGameStateChanged);
            }

            ResetInput();
        }

        private void Update()
        {
            if (!isInputEnabled)
            {
                MoveInput = Vector2.zero;
                return;
            }

            Vector2 actionInput = MoveAction != null
                ? MoveAction.ReadValue<Vector2>()
                : Vector2.zero;

            MoveInput = actionInput.sqrMagnitude > 0.01f
                ? actionInput.normalized
                : joystickInput;
        }

        private void HandleJoystickInput(Vector2 input)
        {
            if (!isInputEnabled)
            {
                joystickInput = Vector2.zero;
                return;
            }

            joystickInput = input;
        }

        private void HandleGameStateChanged(GameState state)
        {
            bool enableInput = state == GameState.Playing;
            ApplyInputEnabled(enableInput);
        }

        private void ApplyInputEnabled(bool enabled)
        {
            isInputEnabled = enabled;

            if (!isInputEnabled)
            {
                ResetInput();
            }
        }

        private void ResetInput()
        {
            joystickInput = Vector2.zero;
            MoveInput = Vector2.zero;
        }
    }
}