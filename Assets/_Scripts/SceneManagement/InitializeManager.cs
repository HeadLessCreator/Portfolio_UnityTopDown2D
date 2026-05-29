using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class InitializeManager : MonoBehaviour
{
    public static InitializeManager Instance { get; private set; }

    private const float ManagerLoadProgressEnd = 0.7f;
    private const float SceneLoadProgressEnd = 1f;

    [Header("Scene")]
    [SerializeField] private string firstSceneName = "GameScene";
    [SerializeField] private bool useNextBuildIndex = true;

    [Header("UI")]
    [SerializeField] private Slider loadingSlider;
    [SerializeField] private Button startButton;
    [SerializeField] private TextMeshProUGUI loadingText;
    [SerializeField] private bool autoStart = true;

    [Header("Manager Wait")]
    [SerializeField] private float managerWaitTimeout = 5f;

    [SerializeField] private bool requireUserDataManager = true;
    [SerializeField] private bool requireConfigManager = true;
    [SerializeField] private bool requireAddressablesAssetManager = true;
    [SerializeField] private bool requirePopupManager = true;
    [SerializeField] private bool requireSoundManager = false;

    [Header("Timing")]
    [SerializeField] private float beforeLoadDelay = 0.3f;

    private bool isStarted;
    private bool isLoading; 
    private bool isBootstrapSucceeded;
    private float currentProgress;
    private string lastLoadingMessage;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        InitializeUI();
        StartCoroutine(BootstrapRoutine());
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void InitializeUI()
    {
        SetProgress(0f, "Initializing...");

        if (startButton == null)
        {
            return;
        }

        startButton.gameObject.SetActive(false);
        startButton.interactable = true;
        startButton.onClick.RemoveAllListeners();
        startButton.onClick.AddListener(StartGame);
    }

    private IEnumerator BootstrapRoutine()
    {
        SetProgress(0f, "Starting bootstrap...");

        yield return WaitForCoreManagersRoutine();

        if (!isBootstrapSucceeded)
        {
            SetProgress(currentProgress, "Initialization failed. Check required managers.");
            Debug.LogError("[InitializeManager] Bootstrap failed. Game scene loading stopped.");

            if (startButton != null)
            {
                startButton.gameObject.SetActive(false);
                startButton.interactable = false;
            }

            yield break;
        }

        SetProgress(ManagerLoadProgressEnd, "Core managers ready.");

        if (autoStart)
        {
            StartGame();
        }
        else
        {
            SetProgress(1f, "Ready. Press Start.");

            if (startButton != null)
            {
                startButton.gameObject.SetActive(true);
                startButton.interactable = true;
            }
        }
    }

    private IEnumerator WaitForCoreManagersRoutine()
    {
        isBootstrapSucceeded = false;

        float startTime = Time.realtimeSinceStartup;
        float timeoutAt = startTime + managerWaitTimeout;

        while (Time.realtimeSinceStartup < timeoutAt)
        {
            bool isReady = AreCoreManagersReady(out string pendingMessage);

            float elapsed = Time.realtimeSinceStartup - startTime;
            float waitProgress = Mathf.Clamp01(elapsed / managerWaitTimeout);
            float totalProgress = waitProgress * ManagerLoadProgressEnd;

            if (isReady)
            {
                isBootstrapSucceeded = true;
                SetProgress(ManagerLoadProgressEnd, "Core managers ready.");
                yield break;
            }

            SetProgress(totalProgress, $"Loading managers... {pendingMessage}");

            yield return null;
        }

        AreCoreManagersReady(out string finalPendingMessage);

        isBootstrapSucceeded = false;

        Debug.LogError($"[InitializeManager] Core Managers Timeout. Pending: {finalPendingMessage}");

        SetProgress(
            ManagerLoadProgressEnd,
            $"Initialization failed. Pending: {finalPendingMessage}"
        );
    }

    private bool AreCoreManagersReady(out string pendingMessage)
    {
        System.Text.StringBuilder pending = new System.Text.StringBuilder();
        bool ready = true;

        if (requireUserDataManager)
        {
            if (UserDataManager.Instance == null)
            {
                ready = false;
                pending.Append("UserDataManager missing; ");
            }
            else if (!UserDataManager.Instance.IsLoadAll)
            {
                ready = false;
                pending.Append("UserDataManager loading; ");
            }
        }

        if (requireConfigManager)
        {
            if (ConfigManager.Instance == null)
            {
                ready = false;
                pending.Append("ConfigManager missing; ");
            }
        }

        if (requireAddressablesAssetManager)
        {
            if (AddressablesAssetManager.Instance == null)
            {
                ready = false;
                pending.Append("AddressablesAssetManager missing; ");
            }
            else if (!AddressablesAssetManager.Instance.IsReady)
            {
                ready = false;
                pending.Append("AddressablesAssetManager loading; ");
            }
        }

        if (requirePopupManager)
        {
            if (PopupManager.Instance == null)
            {
                ready = false;
                pending.Append("PopupManager missing; ");
            }
            else if (!PopupManager.Instance.IsReady)
            {
                ready = false;
                pending.Append("PopupManager loading; ");
            }
        }

        if (requireSoundManager)
        {
            if (SoundManager.Instance == null)
            {
                ready = false;
                pending.Append("SoundManager missing; ");
            }
        }

        pendingMessage = pending.Length > 0 ? pending.ToString() : "None";
        return ready;
    }

    public void StartGame()
    {
        if (!isBootstrapSucceeded)
        {
            Debug.LogError("[InitializeManager] StartGame blocked. Bootstrap is not completed.");
            SetProgress(currentProgress, "Cannot start. Initialization failed.");
            return;
        }

        if (isStarted || isLoading)
        {
            return;
        }

        isStarted = true;

        if (startButton != null)
        {
            startButton.interactable = false;
        }

        StartCoroutine(LoadGameSceneRoutine());
    }

    private IEnumerator LoadGameSceneRoutine()
    {
        isLoading = true;

        float prepareProgress = autoStart ? ManagerLoadProgressEnd : 1f;
        SetProgress(prepareProgress, "Preparing game scene...");

        yield return new WaitForSecondsRealtime(beforeLoadDelay);

        AsyncOperation operation = CreateLoadOperation();

        if (operation == null)
        {
            SetProgress(currentProgress, "Failed to load game scene.");

            isStarted = false;
            isLoading = false;

            if (startButton != null)
            {
                startButton.interactable = true;
            }

            yield break;
        }

        operation.allowSceneActivation = false;

        while (operation.progress < 0.9f)
        {
            float sceneProgress = Mathf.Clamp01(operation.progress / 0.9f);

            float totalProgress = Mathf.Lerp(
                ManagerLoadProgressEnd,
                SceneLoadProgressEnd,
                sceneProgress
            );

            SetProgress(
                totalProgress,
                $"Loading game scene... {(totalProgress * 100f):0}%"
            );

            yield return null;
        }

        SetProgress(1f, "Game scene loaded.");

        yield return null;

        operation.allowSceneActivation = true;

        while (!operation.isDone)
        {
            yield return null;
        }

        isLoading = false;
    }

    private AsyncOperation CreateLoadOperation()
    {
        if (useNextBuildIndex)
        {
            int currentIndex = SceneManager.GetActiveScene().buildIndex;
            int nextIndex = currentIndex + 1;

            if (nextIndex >= SceneManager.sceneCountInBuildSettings)
            {
                Debug.LogError($"[InitializeManager] Invalid next scene index. Current:{currentIndex}, Next:{nextIndex}");
                return null;
            }

            Debug.Log($"[InitializeManager] Load Scene By Build Index: {nextIndex}");
            return SceneManager.LoadSceneAsync(nextIndex);
        }

        if (string.IsNullOrWhiteSpace(firstSceneName))
        {
            Debug.LogError("[InitializeManager] firstSceneName is empty.");
            return null;
        }

        Debug.Log($"[InitializeManager] Load Scene By Name: {firstSceneName}");
        return SceneManager.LoadSceneAsync(firstSceneName);
    }

    private void SetProgress(float progress, string message)
    {
        currentProgress = Mathf.Clamp01(progress);

        if (loadingSlider != null)
        {
            loadingSlider.value = currentProgress;
        }

        if (loadingText != null)
        {
            loadingText.text = message;
        }

        if (lastLoadingMessage != message)
        {
            Debug.Log($"[InitializeManager] {message} ({currentProgress:P0})");
            lastLoadingMessage = message;
        }
    }
}