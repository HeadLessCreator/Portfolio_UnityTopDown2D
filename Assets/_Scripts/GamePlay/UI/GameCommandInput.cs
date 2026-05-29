using UnityEngine;
using UnityEngine.InputSystem;

public class GameCommandInput : MonoBehaviour
{
    private void Update()
    {
        if (Keyboard.current == null)
        {
            return;
        }

        HandleEscape();
    }

    private void HandleEscape()
    {
        if (!Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            return;
        }

        if (UIManager.Instance == null)
        {
            return;
        }

        GameState currentState = GameStateManager.Instance != null
            ? GameStateManager.Instance.CurrentState
            : GameState.Home;

        switch (currentState)
        {
            case GameState.Home:
                UIManager.Instance.ToggleSettings();
                break;

            case GameState.Playing:
                UIManager.Instance.OpenSettings();
                break;

            case GameState.Paused:
                UIManager.Instance.CloseSettings();
                break;

            case GameState.GameOver:
                // GameOverPopup이 떠 있는 상태에서는 일단 아무것도 안 하거나,
                // 나중에 PopupManager.CloseTop() 같은 정책을 넣을 수 있음.
                break;
        }
    }
}