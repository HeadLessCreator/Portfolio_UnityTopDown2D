using UnityEngine;

public class PlayerAutoAttack : MonoBehaviour
{
    [Header("Attack")]
    [SerializeField] private float attackRange = 3f;
    [SerializeField] private float attackInterval = 0.5f;
    [SerializeField] private LayerMask enemyLayer;

    [Header("References")]
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private PlayerAnimationController animationController;
    [SerializeField] private PlayerProjectileSpawner projectileSpawner;

    [Header("Debug")]
    [SerializeField] private bool drawAttackRange = true;
    [SerializeField] private Color attackRangeColor = new Color(0.2f, 0.8f, 1f, 0.9f);
    
    [Header("Sound")]
    [SerializeField] private string attackSfxKey = "MagicSpell";
    [SerializeField] private bool playAttackSfx = true;
    
    private float attackTimer;
    private EnemyController cachedTarget;
    private Vector2 lastAttackDirection = Vector2.right;

    private bool isAttacking;
    private bool hasFiredCurrentAttack;

    private void Awake()
    {
        if (playerHealth == null)
        {
            playerHealth = GetComponent<PlayerHealth>();
        }

        if (animationController == null)
        {
            animationController = GetComponent<PlayerAnimationController>();
        }

        if (projectileSpawner == null)
        {
            projectileSpawner = GetComponent<PlayerProjectileSpawner>();
        }
    }

    private void OnEnable()
    {
        lastAttackDirection = Vector2.right;
        attackTimer = attackInterval;
        ResetAttackState();
    }

    private void OnDisable()
    {
        ResetAttackState();
    }

    private void Update()
    {
        if (!CanAttack())
        {
            return;
        }

        if (isAttacking)
        {
            return;
        }

        attackTimer += Time.deltaTime;

        if (attackTimer < attackInterval)
        {
            return;
        }

        EnemyController target = FindNearestEnemy();

        if (target == null)
        {
            return;
        }

        StartAttack(target);
    }

    private bool CanAttack()
    {
        if (GameStateManager.Instance == null ||
            GameStateManager.Instance.CurrentState != GameState.Playing)
        {
            return false;
        }

        if (playerHealth != null && playerHealth.IsDead)
        {
            return false;
        }

        return true;
    }

    private void StartAttack(EnemyController target)
    {
        if (target == null)
        {
            return;
        }

        isAttacking = true;
        hasFiredCurrentAttack = false;

        attackTimer = 0f;
        cachedTarget = target;

        Vector2 direction = target.GetAimPosition() - transform.position;

        if (direction.sqrMagnitude > 0.0001f)
        {
            lastAttackDirection = direction.normalized;
        }

        animationController?.PlayAttack();
    }

    public void AnimEvent_FireProjectile()
    {
        if (!isAttacking)
        {
            return;
        }

        if (hasFiredCurrentAttack)
        {
            return;
        }

        hasFiredCurrentAttack = true;

        if (!CanAttack())
        {
            return;
        }

        if (projectileSpawner == null)
        {
            Debug.LogWarning("[PlayerAutoAttack] ProjectileSpawner is missing.");
            return;
        }

        EnemyController target = GetValidCachedTarget();

        if (target == null)
        {
            target = FindNearestEnemy();
        }

        if (target != null)
        {
            cachedTarget = target;
            projectileSpawner.FireAt(target, lastAttackDirection);
            PlayAttackSound();
            return;
        }

        projectileSpawner.Fire(lastAttackDirection);
        PlayAttackSound();
    }

    public void AnimEvent_EndAttack()
    {
        ResetAttackState();
    }

    public void ResetAttackState()
    {
        isAttacking = false;
        hasFiredCurrentAttack = false;
        cachedTarget = null;
    }

    private EnemyController GetValidCachedTarget()
    {
        if (cachedTarget == null)
        {
            return null;
        }

        if (!cachedTarget.gameObject.activeInHierarchy)
        {
            return null;
        }

        return cachedTarget;
    }

    private EnemyController FindNearestEnemy()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(
            transform.position,
            attackRange,
            enemyLayer
        );

        EnemyController nearest = null;
        float nearestDistanceSqr = float.MaxValue;

        foreach (Collider2D hit in hits)
        {
            EnemyController enemy = hit.GetComponentInParent<EnemyController>();

            if (enemy == null)
            {
                continue;
            }

            float distanceSqr = ((Vector2)enemy.transform.position - (Vector2)transform.position).sqrMagnitude;

            if (distanceSqr < nearestDistanceSqr)
            {
                nearestDistanceSqr = distanceSqr;
                nearest = enemy;
            }
        }

        return nearest;
    }

    private void PlayAttackSound()
    {
        if (!playAttackSfx)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(attackSfxKey))
        {
            return;
        }

        SoundManager.Instance?.PlayEffect(attackSfxKey);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!drawAttackRange)
        {
            return;
        }

        Gizmos.color = attackRangeColor;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
#endif
}