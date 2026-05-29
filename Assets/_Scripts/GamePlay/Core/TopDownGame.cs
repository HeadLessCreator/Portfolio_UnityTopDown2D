using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class TopDownGame : GameBase
{
    [Header("TopDown Game")]
    [SerializeField] private GameObject playerRoot;
    [SerializeField] private Transform playerSpawnPoint;

    [Header("Systems")]
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private EnemySpawner enemySpawner;

    [Header("TopDown UI")]
    [SerializeField] private TopDownGameHUD gameHUD;

    [Header("Currency")]
    [SerializeField] private TopDownSessionWallet sessionWallet;
    [SerializeField] private TopDownRewardService rewardService;
    [SerializeField] private TopDownCurrencyStorage currencyStorage;

    [Header("Timer")]
    [SerializeField] private GameSessionTimer sessionTimer;

    [Header("Score")]
    [SerializeField] private int currentScore;
    [SerializeField] private int scorePerEnemy = 1;

    [Header("Audio")]
    [SerializeField] private string gamePlayBgmKey = "BGM_02";
    [SerializeField] private string gameLobbyBgmKey = "BGM_01";
    [SerializeField] private string gameStartSfxKey = "GameStart";
    [SerializeField] private string gameOverSfxKey = "GameOver";
    [SerializeField] private string gamePauseSfxKey = "GamePause";
    [SerializeField] private string gameResumeSfxKey = "GameResume";
    [SerializeField, Range(0f, 1f)] private float bgmVolume = 1f;

    [Header("Reward Feedback")]
    [SerializeField] private FloatingTextPool floatingTextPool;

    private const string GoldCurrencyId = "Gold";
    private bool hasCommittedSessionCurrency;

    private void Awake()
    {
        if (gameHUD == null)
        {
            gameHUD = FindAnyObjectByType<TopDownGameHUD>();
        }

        if (sessionWallet == null)
        {
            sessionWallet = FindAnyObjectByType<TopDownSessionWallet>();
        }

        if (rewardService == null)
        {
            rewardService = FindAnyObjectByType<TopDownRewardService>();
        }

        if (currencyStorage == null)
        {
            currencyStorage = FindAnyObjectByType<TopDownCurrencyStorage>();
        }

        if (sessionTimer == null)
        {
            sessionTimer = FindAnyObjectByType<GameSessionTimer>();
        }

        if (floatingTextPool == null)
        {
            floatingTextPool = FindAnyObjectByType<FloatingTextPool>();
        }
    }

    protected override void OnGameInitialized()
    {
        if (sessionWallet != null)
        {
            sessionWallet.OnSessionCurrencyChanged -= HandleSessionCurrencyChanged;
            sessionWallet.OnSessionCurrencyChanged += HandleSessionCurrencyChanged;
        }

        EnterLobby();

        Debug.Log("[TopDownGame] InitializeGameComponents");
    }

    public override void OnGameStarted()
    {
        base.OnGameStarted();

        EnterGamePlay();
        SoundManager.Instance?.PlayEffect(gameStartSfxKey);

        Debug.Log("[TopDownGame] OnGameStarted");
    }

    public override void OnGamePaused()
    {
        base.OnGamePaused();

        sessionTimer?.StopTimer();

        SoundManager.Instance?.StopBGM();
        SoundManager.Instance?.PlayEffect(gamePauseSfxKey);

        Debug.Log("[TopDownGame] OnGamePaused");
    }

    public override void OnGameResumed()
    {
        base.OnGameResumed();

        sessionTimer?.StartTimer();

        PlayGameplayBgm();
        SoundManager.Instance?.PlayEffect(gameResumeSfxKey);

        Debug.Log("[TopDownGame] OnGameResumed");
    }

    public override void OnGameRestarted()
    {
        base.OnGameRestarted();

        EnterLobby();

        Debug.Log("[TopDownGame] OnGameRestarted");
    }

    public override void OnReturnedToHome()
    {
        base.OnReturnedToHome();

        EnterLobby();        

        Debug.Log("[TopDownGame] OnReturnedToHome");
    }

    private void EnterLobby()
    {
        floatingTextPool?.ClearAll();

        currentScore = 0;
        hasCommittedSessionCurrency = false;

        sessionTimer?.ResetTimer();
        sessionWallet?.ResetSessionCurrencies();

        enemySpawner?.StopSpawn();
        enemySpawner?.ClearAllEnemies();

        ResetPlayer();
        SetPlayerActive(false);

        playerHealth?.ResetHealth();

        int totalGold = currencyStorage != null
            ? currencyStorage.GetTotalCurrency(GoldCurrencyId)
            : 0;

        gameHUD?.ResetHUD(
            currentScore,
            playerHealth != null ? playerHealth.CurrentHeart : 5
        );

        gameHUD?.SetTotalGold(totalGold);

        PlayLobbyBgm();

        Debug.Log("[TopDownGame] EnterLobby");
    }

    private void EnterGamePlay()
    {
        currentScore = 0;
        hasCommittedSessionCurrency = false;

        floatingTextPool?.ClearAll();
        sessionWallet?.ResetSessionCurrencies();
        sessionTimer?.ResetTimer();
        sessionTimer?.StartTimer();

        ResetPlayer();
        SetPlayerActive(true);

        if (playerHealth != null)
        {
            playerHealth.ResetHealth();

            playerHealth.OnDamaged -= HandlePlayerDamaged;
            playerHealth.OnDamaged += HandlePlayerDamaged;

            playerHealth.OnDead -= HandlePlayerDead;
            playerHealth.OnDead += HandlePlayerDead;

            gameHUD?.ResetHUD(currentScore, playerHealth.CurrentHeart);
        }
        else
        {
            gameHUD?.SetScore(currentScore);
        }

        if (enemySpawner != null && playerRoot != null)
        {
            enemySpawner.OnEnemyKilled -= HandleEnemyKilled;
            enemySpawner.OnEnemyKilled += HandleEnemyKilled;

            enemySpawner.StartSpawn(playerRoot.transform);
        }

        PlayGameplayBgm();

        Debug.Log("[TopDownGame] EnterGameplay");
    }

    private void HandleEnemyKilled(EnemyController enemy)
    {
        AddScore(scorePerEnemy);
        rewardService?.GrantEnemyKillReward(enemy);
        ShowEnemyKillRewardFeedback(enemy);
    }

    private void AddScore(int amount)
    {
        currentScore += amount;
        gameHUD?.SetScore(currentScore);
    }

    private void HandlePlayerDamaged(int currentHeart)
    {
        gameHUD?.SetHearts(currentHeart);
    }

    private void HandlePlayerDead()
    {
        sessionTimer?.StopTimer();

        enemySpawner?.StopSpawn();

        SoundManager.Instance?.StopBGM();
        SoundManager.Instance?.PlayEffect(gameOverSfxKey);

        int earnedGold = sessionWallet != null
            ? sessionWallet.GetSessionCurrency(GoldCurrencyId)
            : 0;

        if (!hasCommittedSessionCurrency)
        {
            hasCommittedSessionCurrency = true;
            currencyStorage?.AddSessionToTotal(sessionWallet);
        }

        int score = currentScore;
        float survivalTime = sessionTimer != null ? sessionTimer.CurrentTime : 0f;

        UIManager.Instance?.GameOverPopupOnOff(true, score, survivalTime, earnedGold);

        RequestGameOver();
    }

    private void HandleSessionCurrencyChanged(string currencyId, int amount)
    {
        gameHUD?.SetSessionCurrency(currencyId, amount);
    }

    public override int GetCurrentScore()
    {
        return currentScore;
    }

    private void PlayLobbyBgm()
    {
        if (string.IsNullOrWhiteSpace(gameLobbyBgmKey))
        {
            return;
        }

        SoundManager.Instance?.PlayBGM(gameLobbyBgmKey, bgmVolume);
    }

    private void PlayGameplayBgm()
    {
        if (string.IsNullOrWhiteSpace(gamePlayBgmKey))
        {
            return;
        }

        SoundManager.Instance?.PlayBGM(gamePlayBgmKey, bgmVolume);
    }

    private void ShowEnemyKillRewardFeedback(EnemyController enemy)
    {
        if (enemy == null || floatingTextPool == null)
        {
            return;
        }

        IReadOnlyList<TopDownCurrencyReward> rewards = enemy.Rewards;

        if (rewards == null || rewards.Count == 0)
        {
            return;
        }

        Vector3 textPosition = enemy.GetAimPosition();

        for (int i = 0; i < rewards.Count; i++)
        {
            TopDownCurrencyReward reward = rewards[i];

            if (string.IsNullOrWhiteSpace(reward.currencyId))
            {
                continue;
            }

            if (reward.amount <= 0)
            {
                continue;
            }

            if (reward.currencyId == GoldCurrencyId)
            {
                floatingTextPool.ShowGold(reward.amount, textPosition);
            }
            else
            {
                floatingTextPool.ShowText($"+{reward.amount} {reward.currencyId}", textPosition);
            }
        }
    }

    private void ResetPlayer()
    {
        if (playerRoot == null || playerSpawnPoint == null)
        {
            return;
        }

        playerRoot.transform.position = playerSpawnPoint.position;
        playerRoot.transform.rotation = playerSpawnPoint.rotation;
    }

    private void SetPlayerActive(bool isActive)
    {
        if (playerRoot != null)
        {
            playerRoot.SetActive(isActive);
        }
    }

    private void OnDestroy()
    {
        if (playerHealth != null)
        {
            playerHealth.OnDamaged -= HandlePlayerDamaged;
            playerHealth.OnDead -= HandlePlayerDead;
        }

        if (enemySpawner != null)
        {
            enemySpawner.OnEnemyKilled -= HandleEnemyKilled;
        }

        if (sessionWallet != null)
        {
            sessionWallet.OnSessionCurrencyChanged -= HandleSessionCurrencyChanged;
        }
    }
}