using System;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Pool")]
    [SerializeField] private EnemyController enemyPrefab;
    [SerializeField] private int initialPoolSize = 30;
    [SerializeField] private bool allowPoolExpansion = false;
    [SerializeField] private int maxPoolSize = 50;

    [Header("Spawn Points")]
    [SerializeField] private Transform spawnPointRoot;
    [SerializeField] private bool refreshSpawnPointsOnStart = true;

    [Header("Camera Filter")]
    [SerializeField] private Camera spawnCamera;
    [SerializeField] private bool excludeCameraVisiblePoints = true;
    [SerializeField] private float cameraViewportMargin = 0.15f;
    [SerializeField] private bool skipSpawnIfNoValidPoint = false;

    [Header("Spawn")]
    [SerializeField] private float spawnInterval = 1f;
    [SerializeField] private int maxAliveEnemy = 30;

    [Header("Enemy Speed")]
    [SerializeField] private float minMoveSpeed = 1.5f;
    [SerializeField] private float maxMoveSpeed = 3.5f;

    [Header("Difficulty Scaling")]
    [SerializeField] private bool useDifficultyScaling = true;
    [SerializeField] private float difficultyRampDuration = 90f;

    [Header("Difficulty - Spawn Interval")]
    [SerializeField] private float startSpawnInterval = 1.2f;
    [SerializeField] private float endSpawnInterval = 0.35f;

    [Header("Difficulty - Max Alive Enemy")]
    [SerializeField] private int startMaxAliveEnemy = 8;
    [SerializeField] private int endMaxAliveEnemy = 35;

    [Header("Difficulty - Enemy Speed")]
    [SerializeField] private float startSpeedMultiplier = 1f;
    [SerializeField] private float endSpeedMultiplier = 1.7f;

    [Header("Difficulty - Spawn Count")]
    [SerializeField] private float doubleSpawnTime = 60f;
    [SerializeField] private float tripleSpawnTime = 90f;

    [Header("Difficulty Curve")]
    [SerializeField]
    private AnimationCurve difficultyCurve =
        AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private readonly List<EnemyController> enemyPool = new();
    private readonly List<Transform> spawnPoints = new();
    private readonly List<Transform> validSpawnPoints = new();

    private Transform target;
    private float spawnTimer;
    private float spawnElapsedTime;
    private bool isSpawning;
    private bool isPoolInitialized;

    public event Action<EnemyController> OnEnemyKilled;

    private void Awake()
    {
        CacheSpawnPoints();
        InitializePool();
    }

    public void InitializePool()
    {
        if (isPoolInitialized)
        {
            return;
        }

        if (enemyPrefab == null)
        {
            Debug.LogError("[EnemySpawner] enemyPrefab is null.");
            return;
        }

        int poolSize = GetRecommendedInitialPoolSize();

        for (int i = 0; i < poolSize; i++)
        {
            CreateEnemy();
        }

        isPoolInitialized = true;

        Debug.Log($"[EnemySpawner] Pool initialized. Count: {enemyPool.Count}");
    }

    public void StartSpawn(Transform targetTransform)
    {
        if (refreshSpawnPointsOnStart)
        {
            CacheSpawnPoints();
        }

        InitializePool();

        target = targetTransform;
        spawnTimer = 0f;
        spawnElapsedTime = 0f;
        isSpawning = true;

        ClearAllEnemies();

        Debug.Log("[EnemySpawner] StartSpawn");
    }

    public void StopSpawn()
    {
        isSpawning = false;

        Debug.Log("[EnemySpawner] StopSpawn");
    }

    public void CacheSpawnPoints()
    {
        spawnPoints.Clear();

        if (spawnPointRoot == null)
        {
            Debug.LogWarning("[EnemySpawner] spawnPointRoot is null.");
            return;
        }

        for (int i = 0; i < spawnPointRoot.childCount; i++)
        {
            Transform child = spawnPointRoot.GetChild(i);

            if (child == null)
            {
                continue;
            }

            spawnPoints.Add(child);
        }

        Debug.Log($"[EnemySpawner] Cached SpawnPoints: {spawnPoints.Count}");
    }

    private void Update()
    {
        if (!isSpawning)
        {
            return;
        }

        if (!IsPlaying())
        {
            return;
        }

        if (target == null)
        {
            return;
        }

        spawnElapsedTime += Time.deltaTime;
        spawnTimer += Time.deltaTime;

        ApplySpeedMultiplierToAliveEnemies(GetCurrentSpeedMultiplier());

        float currentSpawnInterval = GetCurrentSpawnInterval();

        if (spawnTimer < currentSpawnInterval)
        {
            return;
        }

        spawnTimer = 0f;

        int currentMaxAliveEnemy = GetCurrentMaxAliveEnemy();
        int spawnCount = GetCurrentSpawnCount();

        for (int i = 0; i < spawnCount; i++)
        {
            if (GetAliveCount() >= currentMaxAliveEnemy)
            {
                break;
            }

            SpawnEnemy();
        }
    }

    private void SpawnEnemy()
    {
        if (!TryGetSpawnPosition(out Vector3 spawnPosition))
        {
            Debug.LogWarning("[EnemySpawner] Failed to find spawn position.");
            return;
        }

        EnemyController enemy = GetInactiveEnemy();

        if (enemy == null)
        {
            if (!CanExpandPool())
            {
                Debug.LogWarning("[EnemySpawner] No inactive enemy in pool and pool expansion is disabled.");
                return;
            }

            enemy = CreateEnemy();
        }

        enemy.transform.position = spawnPosition;
        enemy.transform.rotation = Quaternion.identity;

        float randomBaseSpeed = UnityEngine.Random.Range(minMoveSpeed, maxMoveSpeed);

        enemy.Initialize(target, randomBaseSpeed);
        enemy.SetSpeedMultiplier(GetCurrentSpeedMultiplier());
    }

    private float GetDifficulty01()
    {
        if (!useDifficultyScaling)
        {
            return 0f;
        }

        if (difficultyRampDuration <= 0f)
        {
            return 1f;
        }

        float normalizedTime = Mathf.Clamp01(spawnElapsedTime / difficultyRampDuration);

        if (difficultyCurve == null)
        {
            return normalizedTime;
        }

        return Mathf.Clamp01(difficultyCurve.Evaluate(normalizedTime));
    }

    private float GetCurrentSpawnInterval()
    {
        if (!useDifficultyScaling)
        {
            return spawnInterval;
        }

        float difficulty = GetDifficulty01();
        return Mathf.Lerp(startSpawnInterval, endSpawnInterval, difficulty);
    }

    private int GetCurrentMaxAliveEnemy()
    {
        if (!useDifficultyScaling)
        {
            return maxAliveEnemy;
        }

        float difficulty = GetDifficulty01();
        return Mathf.RoundToInt(Mathf.Lerp(startMaxAliveEnemy, endMaxAliveEnemy, difficulty));
    }

    private float GetCurrentSpeedMultiplier()
    {
        if (!useDifficultyScaling)
        {
            return 1f;
        }

        float difficulty = GetDifficulty01();
        return Mathf.Lerp(startSpeedMultiplier, endSpeedMultiplier, difficulty);
    }

    private int GetCurrentSpawnCount()
    {
        if (!useDifficultyScaling)
        {
            return 1;
        }

        if (spawnElapsedTime >= tripleSpawnTime)
        {
            return 3;
        }

        if (spawnElapsedTime >= doubleSpawnTime)
        {
            return 2;
        }

        return 1;
    }

    private bool TryGetSpawnPosition(out Vector3 spawnPosition)
    {
        spawnPosition = Vector3.zero;

        if (spawnPoints.Count == 0)
        {
            Debug.LogError("[EnemySpawner] spawnPoints is empty.");
            return false;
        }

        validSpawnPoints.Clear();

        foreach (Transform spawnPoint in spawnPoints)
        {
            if (spawnPoint == null)
            {
                continue;
            }

            if (excludeCameraVisiblePoints && IsInsideCameraViewWithMargin(spawnPoint.position))
            {
                continue;
            }

            validSpawnPoints.Add(spawnPoint);
        }

        if (validSpawnPoints.Count > 0)
        {
            return TryGetRandomSpawnPoint(validSpawnPoints, out spawnPosition);
        }

        if (skipSpawnIfNoValidPoint)
        {
            Debug.LogWarning("[EnemySpawner] No valid spawn point outside camera. Spawn skipped.");
            return false;
        }

        Debug.LogWarning("[EnemySpawner] No valid spawn point outside camera. Using fallback random spawn point.");

        return TryGetRandomSpawnPoint(spawnPoints, out spawnPosition);
    }

    private bool TryGetRandomSpawnPoint(List<Transform> points, out Vector3 spawnPosition)
    {
        spawnPosition = Vector3.zero;

        if (points == null || points.Count == 0)
        {
            return false;
        }

        const int maxTryCount = 20;

        for (int i = 0; i < maxTryCount; i++)
        {
            Transform point = points[UnityEngine.Random.Range(0, points.Count)];

            if (point == null)
            {
                continue;
            }

            spawnPosition = point.position;
            return true;
        }

        return false;
    }

    private bool IsInsideCameraViewWithMargin(Vector3 worldPosition)
    {
        Camera targetCamera = spawnCamera != null ? spawnCamera : Camera.main;

        if (targetCamera == null)
        {
            return false;
        }

        Vector3 viewportPosition = targetCamera.WorldToViewportPoint(worldPosition);

        return viewportPosition.z > 0f &&
               viewportPosition.x >= -cameraViewportMargin &&
               viewportPosition.x <= 1f + cameraViewportMargin &&
               viewportPosition.y >= -cameraViewportMargin &&
               viewportPosition.y <= 1f + cameraViewportMargin;
    }

    private EnemyController CreateEnemy()
    {
        EnemyController enemy = Instantiate(enemyPrefab, transform);
        enemy.gameObject.SetActive(false);
        enemy.OnKilled += HandleEnemyKilled;

        enemyPool.Add(enemy);

        return enemy;
    }

    private EnemyController GetInactiveEnemy()
    {
        foreach (EnemyController enemy in enemyPool)
        {
            if (enemy != null && !enemy.gameObject.activeSelf)
            {
                return enemy;
            }
        }

        return null;
    }

    private bool CanExpandPool()
    {
        return allowPoolExpansion && enemyPool.Count < maxPoolSize;
    }

    private void HandleEnemyKilled(EnemyController enemy)
    {
        OnEnemyKilled?.Invoke(enemy);
    }

    private int GetAliveCount()
    {
        int count = 0;

        foreach (EnemyController enemy in enemyPool)
        {
            if (enemy != null && enemy.gameObject.activeSelf)
            {
                count++;
            }
        }

        return count;
    }

    public void ClearAllEnemies()
    {
        foreach (EnemyController enemy in enemyPool)
        {
            if (enemy != null)
            {
                enemy.Deactivate();
            }
        }
    }

    private bool IsPlaying()
    {
        return GameStateManager.Instance != null &&
               GameStateManager.Instance.CurrentState == GameState.Playing;
    }

    private int GetRecommendedInitialPoolSize()
    {
        if (!useDifficultyScaling)
        {
            return initialPoolSize;
        }

        return Mathf.Max(initialPoolSize, startMaxAliveEnemy);
    }

    private void ApplySpeedMultiplierToAliveEnemies(float speedMultiplier)
    {
        foreach (EnemyController enemy in enemyPool)
        {
            if (enemy == null)
            {
                continue;
            }

            if (!enemy.gameObject.activeSelf)
            {
                continue;
            }

            enemy.SetSpeedMultiplier(speedMultiplier);
        }
    }

    private void OnDestroy()
    {
        foreach (EnemyController enemy in enemyPool)
        {
            if (enemy != null)
            {
                enemy.OnKilled -= HandleEnemyKilled;
            }
        }
    }

#if UNITY_EDITOR
    [ContextMenu("Refresh Spawn Points")]
    private void RefreshSpawnPointsInEditor()
    {
        CacheSpawnPoints();
    }
#endif
}