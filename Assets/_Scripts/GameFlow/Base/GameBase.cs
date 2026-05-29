using UnityEngine;

public abstract class GameBase : MonoBehaviour
{
    protected GameManager gameManager;

    [SerializeField] protected Transform[] gameContainers;

    public Transform[] GameContainers
    {
        get => gameContainers;
        set => gameContainers = value;
    }

    protected virtual void SetupUI()
    {
        //나중에 사용할 수도 있으니 남겨둠
        if (UIManager.Instance == null)
        {
            Debug.LogError("[GameBase] UIManager.Instance is null.");
            return;
        }

        if (UIManager.Instance.canvasUiList == null || UIManager.Instance.canvasUiList.Length == 0)
        {
            Debug.LogError("[GameBase] UIManager canvasUiList is not set.");
            return;
        }
    }

    public virtual void Initialize(GameManager manager)
    {
        gameManager = manager;
        OnGameInitialized();
    }

    protected virtual void OnGameInitialized()
    {
        
    }

    public virtual void OnGameStarted()
    {
        Debug.Log("[GameBase] StartGameplay");
    }

    public virtual void OnGamePaused()
    {
        Debug.Log("[GameBase] PauseGameplay");
    }

    public virtual void OnGameResumed()
    {
        Debug.Log("[GameBase] ResumeGameplay");
    }

    public virtual void OnGameRestarted()
    {
        Debug.Log("[GameBase] Restart");
    }

    public virtual void OnReturnedToHome()
    {
        Debug.Log("[GameBase] ReturnToHome");
    }

    //요청 함수
    public virtual void RequestStartGame() => gameManager?.StartGame();
    public virtual void RequestPauseGame() => gameManager?.PauseGame();
    public virtual void RequestResumeGame() => gameManager?.ResumeGame();

    public virtual void RequestGameOver() => gameManager?.RequestGameOver();

    public bool IsGameActive
    {
        get
        {
            if (GameStateManager.Instance == null)
            {
                return false;
            }

            return GameStateManager.Instance.CurrentState == GameState.Playing;
        }
    }

    public abstract int GetCurrentScore();
}