using System.Collections.Generic;
using UnityEngine;

public class PlayerProjectileSpawner : MonoBehaviour
{
    [Header("Projectile Pool")]
    [SerializeField] private PlayerProjectile projectilePrefab;
    [SerializeField] private int initialPoolSize = 10;
    [SerializeField] private bool allowPoolExpansion = true;
    [SerializeField] private int maxPoolSize = 30;

    [Header("Fire")]
    [SerializeField] private Transform firePoint;

    [Header("Projectile Settings")]
    [SerializeField] private float projectileSpeed = 16f;
    [SerializeField] private float projectileLifeTime = 2f;
    [SerializeField] private int projectileDamage = 1;

    private readonly List<PlayerProjectile> projectilePool = new();
    private bool isInitialized;

    private void Awake()
    {
        InitializePool();
    }

    private void InitializePool()
    {
        if (isInitialized)
        {
            return;
        }

        if (projectilePrefab == null)
        {
            Debug.LogError("[PlayerProjectileSpawner] Projectile prefab is missing.");
            return;
        }

        for (int i = 0; i < initialPoolSize; i++)
        {
            CreateProjectile();
        }

        isInitialized = true;
    }

    public void Fire(Vector2 direction)
    {
        InitializePool();

        if (projectilePrefab == null)
        {
            return;
        }

        if (direction.sqrMagnitude <= 0.0001f)
        {
            direction = Vector2.right;
        }

        PlayerProjectile projectile = GetInactiveProjectile();

        if (projectile == null)
        {
            if (!CanExpandPool())
            {
                Debug.LogWarning("[PlayerProjectileSpawner] No inactive projectile and pool expansion is disabled.");
                return;
            }

            projectile = CreateProjectile();
        }

        Vector3 spawnPosition = GetFirePosition();

        projectile.transform.position = spawnPosition;
        projectile.transform.rotation = Quaternion.identity;

        projectile.Initialize(
            direction.normalized,
            projectileSpeed,
            projectileLifeTime,
            projectileDamage
        );
    }

    public void FireAt(EnemyController enemy, Vector2 fallbackDirection)
    {
        if (enemy == null)
        {
            Fire(fallbackDirection);
            return;
        }

        Vector3 firePosition = GetFirePosition();
        Vector3 aimPosition = enemy.GetAimPosition();

        Vector2 direction = (Vector2)(aimPosition - firePosition);

        if (direction.sqrMagnitude <= 0.0001f)
        {
            direction = fallbackDirection;
        }

        Fire(direction.normalized);
    }

    private Vector3 GetFirePosition()
    {
        return firePoint != null
            ? firePoint.position
            : transform.position;
    }

    private PlayerProjectile GetInactiveProjectile()
    {
        foreach (PlayerProjectile projectile in projectilePool)
        {
            if (projectile != null && !projectile.gameObject.activeSelf)
            {
                return projectile;
            }
        }

        return null;
    }

    private PlayerProjectile CreateProjectile()
    {
        PlayerProjectile projectile = Instantiate(projectilePrefab, transform);
        projectile.gameObject.SetActive(false);
        projectilePool.Add(projectile);

        return projectile;
    }

    private bool CanExpandPool()
    {
        if (!allowPoolExpansion)
        {
            return false;
        }

        return projectilePool.Count < maxPoolSize;
    }

    public void ClearAllProjectiles()
    {
        foreach (PlayerProjectile projectile in projectilePool)
        {
            if (projectile != null)
            {
                projectile.Deactivate();
            }
        }
    }
}