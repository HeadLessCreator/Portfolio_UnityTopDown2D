using UnityEngine;

public class GameSessionTimer : MonoBehaviour
{
    [Header("Timer")]
    [SerializeField] private bool updateOnlyInPlayingState = true;
    [SerializeField] private bool updateTextEverySecond = true;

    [Header("HUD")]
    [SerializeField] private TopDownGameHUD gameHUD;

    public float CurrentTime { get; private set; }
    public bool IsRunning { get; private set; }

    private int lastDisplayedSecond = -1;

    private void Awake()
    {
        if (gameHUD == null)
        {
            gameHUD = FindAnyObjectByType<TopDownGameHUD>();
        }
    }

    private void Update()
    {
        if (!IsRunning)
        {
            return;
        }

        if (updateOnlyInPlayingState &&
            (GameStateManager.Instance == null ||
             GameStateManager.Instance.CurrentState != GameState.Playing))
        {
            return;
        }

        CurrentTime += Time.deltaTime;

        if (updateTextEverySecond)
        {
            int currentSecond = Mathf.FloorToInt(CurrentTime);

            if (currentSecond == lastDisplayedSecond)
            {
                return;
            }

            lastDisplayedSecond = currentSecond;
        }

        RefreshTimerUI();
    }

    public void ResetTimer()
    {
        CurrentTime = 0f;
        IsRunning = false;
        lastDisplayedSecond = -1;

        RefreshTimerUI();
    }

    public void StartTimer()
    {
        IsRunning = true;
        RefreshTimerUI();
    }

    public void StopTimer()
    {
        IsRunning = false;
        RefreshTimerUI();
    }

    private void RefreshTimerUI()
    {
        gameHUD?.SetTimer(CurrentTime);
    }

    public static string FormatTime(float seconds)
    {
        int totalSeconds = Mathf.FloorToInt(seconds);
        int minutes = totalSeconds / 60;
        int remainSeconds = totalSeconds % 60;

        return $"{minutes:00}:{remainSeconds:00}";
    }
}