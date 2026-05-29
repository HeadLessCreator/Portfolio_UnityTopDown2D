using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("UI Roots")]
    [SerializeField] private GameObject homeRoot;
    [SerializeField] private GameObject playRoot;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject loadingPopup;

    [Header("Play HUD")]
    [SerializeField] private TextMeshProUGUI scoreText;

    [Header("Legacy")]
    [SerializeField] private GameObject gameStartButton;

    [Header("Canvas")]
    public Canvas[] canvasUiList;

    [Header("Canvas Pools")]
    [SerializeField] private List<PopupManager.CanvasPool> sceneCanvasPools = new();

    private bool resumeGameWhenSettingsClosed;

    private void Awake()
    {
        InitializeSingleton();
    }

    private void Start()
    {
        RegisterPopupCanvasPools();
        SubscribeGameState();
        ApplyGameState(GameStateManager.Instance != null
            ? GameStateManager.Instance.CurrentState
            : GameState.Home);

        CloseSettings();
    }

    private void OnDestroy()
    {
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.OnStateChanged.RemoveListener(HandleGameStateChanged);
        }

        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void InitializeSingleton()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void RegisterPopupCanvasPools()
    {
        if (PopupManager.Instance == null)
        {
            Debug.LogWarning("[UIManager] PopupManager.Instance is null. Popup canvas pools were not registered.");
            return;
        }

        PopupManager.Instance.CleanupAndRefreshCanvasPools();
        PopupManager.Instance.RegisterCanvasPools(sceneCanvasPools);

        Debug.Log($"[UIManager] Registered {sceneCanvasPools.Count} CanvasPools to PopupManager.");
    }

    private void SubscribeGameState()
    {
        if (GameStateManager.Instance == null)
        {
            Debug.LogWarning("[UIManager] GameStateManager.Instance is null.");
            return;
        }

        GameStateManager.Instance.OnStateChanged.RemoveListener(HandleGameStateChanged);
        GameStateManager.Instance.OnStateChanged.AddListener(HandleGameStateChanged);
    }

    private void HandleGameStateChanged(GameState state)
    {
        ApplyGameState(state);
    }

    private void ApplyGameState(GameState state)
    {
        switch (state)
        {
            case GameState.Home:
                SetHomeUI();
                break;

            case GameState.Playing:
                SetPlayingUI();
                break;

            case GameState.Paused:
                SetPausedUI();
                break;

            case GameState.GameOver:
                SetGameOverUI();
                break;
        }
    }

    private void SetHomeUI()
    {
        SetActive(homeRoot, true);
        SetActive(playRoot, false);
        CloseSettings();
    }

    private void SetPlayingUI()
    {
        SetActive(homeRoot, false);
        SetActive(playRoot, true);
        CloseSettings();
    }

    private void SetPausedUI()
    {
        SetActive(homeRoot, false);
        SetActive(playRoot, true);
    }

    private void SetGameOverUI()
    {
        SetActive(homeRoot, false);
        SetActive(playRoot, false);
        CloseSettings();
    }

    public void OnGameStartButtonClick()
    {
        GameManager.Instance?.StartGame();
        PlayButtonSound(4);
    }

    public void OpenSettings()
    {
        bool isPlaying = GameStateManager.Instance != null &&
                         GameStateManager.Instance.CurrentState == GameState.Playing;

        resumeGameWhenSettingsClosed = isPlaying;

        if (isPlaying)
        {
            GameManager.Instance?.PauseGame();
        }

        SetActive(settingsPanel, true);
        PlayButtonSound(1);
    }

    public void CloseSettings()
    {
        SetActive(settingsPanel, false);

        if (resumeGameWhenSettingsClosed)
        {
            resumeGameWhenSettingsClosed = false;
            GameManager.Instance?.ResumeGame();
        }
    }

    public void ToggleSettings()
    {
        if (settingsPanel == null)
        {
            return;
        }

        if (settingsPanel.activeSelf)
        {
            CloseSettings();
        }
        else
        {
            OpenSettings();
        }
    }

    public void GameOverPopupOnOff(bool isOn = false, int score = 0, float survivalTime = 0f, int earnedGold = 0)
    {
        if (PopupManager.Instance == null || !PopupManager.Instance.IsReady)
        {
            Debug.LogWarning("[UIManager] PopupManager is not ready for GameOverPopup.");
            return;
        }

        if (isOn)
        {
            PopupManager.Instance.ShowPopup<GameOverPopup>(
                "Game Over",
                string.Empty,
                popup =>
                {
                    popup.ResultSetting(score, survivalTime, earnedGold);
                },
                PopupManager.PopupPriority.Notice
            );
        }
        else
        {
            PopupManager.Instance.ClosePopup<GameOverPopup>();
        }
    }

    public void LoadingOnOff(bool isOn = false)
    {
        SetActive(loadingPopup, isOn);
    }

    public void GameStartOnOff(bool isOn)
    {
        SetActive(gameStartButton, isOn);
    }

    private void PlayButtonSound(int index)
    {
        SoundManager.Instance?.PlayEffect($"BUTTON_0{index}");
    }

    private void SetActive(GameObject target, bool isActive)
    {
        if (target != null)
        {
            target.SetActive(isActive);
        }
    }

    public void SetScore(int score)
    {
        if (scoreText != null)
        {
            scoreText.text = score.ToString();
        }
    }    
}