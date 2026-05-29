using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyController : MonoBehaviour
{
    private enum EnemyState
    {
        Idle,
        Walk,
        Attack,
        Hit,
        Die
    }

    [Header("Enemy")]
    [SerializeField] private int maxHp = 1;
    [SerializeField] private int contactDamage = 1;

    [Header("Movement")]
    [SerializeField] private float attackDistance = 0.8f;

    [Header("Attack")]
    [SerializeField] private float attackDuration = 0.35f;
    [SerializeField] private float attackCooldown = 0.6f;
    [SerializeField] private bool damageOnAttackStart = true;

    [Header("Hit")]
    [SerializeField] private float hitDuration = 0.15f;

    [Header("Death")]
    [SerializeField] private float deathDeactivateDelay = 2f;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Debug")]
    [SerializeField] private bool drawAttackDistance = true;
    [SerializeField] private Color attackDistanceColor = new Color(1f, 0.2f, 0.2f, 0.9f);

    [Header("Target Point")]
    [SerializeField] private Transform aimPoint;
    [SerializeField] private Collider2D bodyCollider;

    [Header("Reward")]
    [SerializeField] private TopDownCurrencyReward[] rewards =
        {
        new TopDownCurrencyReward { currencyId = "Gold", amount = 1 }
    };

    public IReadOnlyList<TopDownCurrencyReward> Rewards => rewards;

    private int currentHp;
    private float baseMoveSpeed;
    private float speedMultiplier = 1f;
    private float CurrentMoveSpeed => baseMoveSpeed * speedMultiplier; private Transform target;
    private Rigidbody2D rb;
    private Collider2D enemyCollider;

    private EnemyState currentState = EnemyState.Idle;

    private Coroutine attackRoutine;
    private Coroutine hitRoutine;
    private Coroutine deathRoutine;

    private float nextAttackTime;

    private static readonly int IsWalkingHash = Animator.StringToHash("IsWalking");
    private static readonly int AttackHash = Animator.StringToHash("Attack");
    private static readonly int HitHash = Animator.StringToHash("Hit");
    private static readonly int DieHash = Animator.StringToHash("Die");

    public event Action<EnemyController> OnKilled;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        enemyCollider = GetComponent<Collider2D>();

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        if(bodyCollider == null)
        {
            bodyCollider = GetComponent<Collider2D>();
        }

        rb.gravityScale = 0f;
        rb.freezeRotation = true;
    }

    public void Initialize(Transform targetTransform, float speed)
    {
        target = targetTransform;

        baseMoveSpeed = speed;
        speedMultiplier = 1f;

        currentHp = maxHp;

        nextAttackTime = 0f;

        StopRunningCoroutines();

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        if (enemyCollider != null)
        {
            enemyCollider.enabled = true;
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.white;
        }

        gameObject.SetActive(true);

        ResetAnimator();

        ChangeState(target == null ? EnemyState.Idle : EnemyState.Walk);
    }

    private void Update()
    {
        if (!IsPlaying())
        {
            ChangeState(EnemyState.Idle);
            return;
        }

        if (target == null)
        {
            ChangeState(EnemyState.Idle);
            return;
        }

        if (currentState == EnemyState.Die ||
            currentState == EnemyState.Hit ||
            currentState == EnemyState.Attack)
        {
            return;
        }

        if (IsTargetInAttackDistance())
        {
            TryStartAttack();
            return;
        }

        ChangeState(EnemyState.Walk);
    }

    private void FixedUpdate()
    {
        if (!IsPlaying())
        {
            return;
        }

        if (target == null)
        {
            return;
        }

        if (currentState != EnemyState.Walk)
        {
            return;
        }

        MoveToTarget();
    }

    private void MoveToTarget()
    {
        Vector2 direction = ((Vector2)target.position - rb.position).normalized;
        rb.MovePosition(rb.position + direction * CurrentMoveSpeed * Time.fixedDeltaTime);
    }

    private bool IsTargetInAttackDistance()
    {
        if (target == null)
        {
            return false;
        }

        float distanceSqr = ((Vector2)target.position - rb.position).sqrMagnitude;
        float attackDistanceSqr = attackDistance * attackDistance;

        return distanceSqr <= attackDistanceSqr;
    }

    private void TryStartAttack()
    {
        if (Time.time < nextAttackTime)
        {
            ChangeState(EnemyState.Idle);
            return;
        }

        if (attackRoutine != null)
        {
            return;
        }

        attackRoutine = StartCoroutine(AttackRoutine());
    }

    private IEnumerator AttackRoutine()
    {
        ChangeState(EnemyState.Attack);

        nextAttackTime = Time.time + attackCooldown;

        if (animator != null)
        {
            animator.ResetTrigger(HitHash);
            animator.SetTrigger(AttackHash);
        }

        if (damageOnAttackStart)
        {
            TryDamagePlayer();
        }

        yield return new WaitForSeconds(attackDuration);

        attackRoutine = null;

        if (currentState == EnemyState.Die)
        {
            yield break;
        }

        if (target == null)
        {
            ChangeState(EnemyState.Idle);
        }
        else if (IsTargetInAttackDistance())
        {
            ChangeState(EnemyState.Idle);
        }
        else
        {
            ChangeState(EnemyState.Walk);
        }
    }

    private void TryDamagePlayer()
    {
        if (target == null)
        {
            return;
        }

        if (!IsTargetInAttackDistance())
        {
            return;
        }

        if (target.TryGetComponent<PlayerHealth>(out PlayerHealth playerHealth))
        {
            playerHealth.TakeDamage(contactDamage);
        }
    }

    public void TakeDamage(int damage)
    {
        if (!gameObject.activeSelf)
        {
            return;
        }

        if (currentState == EnemyState.Die)
        {
            return;
        }

        currentHp -= damage;

        if (currentHp <= 0)
        {
            Die();
            return;
        }

        PlayHit();
    }

    private void PlayHit()
    {
        if (hitRoutine != null)
        {
            StopCoroutine(hitRoutine);
        }

        hitRoutine = StartCoroutine(HitRoutine());
    }

    private IEnumerator HitRoutine()
    {
        ChangeState(EnemyState.Hit);

        if (animator != null)
        {
            animator.ResetTrigger(AttackHash);
            animator.SetTrigger(HitHash);
        }

        yield return new WaitForSeconds(hitDuration);

        hitRoutine = null;

        if (currentState == EnemyState.Die)
        {
            yield break;
        }

        if (target == null)
        {
            ChangeState(EnemyState.Idle);
        }
        else if (IsTargetInAttackDistance())
        {
            ChangeState(EnemyState.Idle);
        }
        else
        {
            ChangeState(EnemyState.Walk);
        }
    }

    private void Die()
    {
        if (currentState == EnemyState.Die)
        {
            return;
        }

        StopRunningCoroutines();

        ChangeState(EnemyState.Die);

        if (enemyCollider != null)
        {
            enemyCollider.enabled = false;
        }

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        if (animator != null)
        {
            animator.ResetTrigger(AttackHash);
            animator.ResetTrigger(HitHash);
            animator.SetTrigger(DieHash);
        }

        OnKilled?.Invoke(this);

        deathRoutine = StartCoroutine(DeathRoutine());
    }

    private IEnumerator DeathRoutine()
    {
        yield return new WaitForSeconds(deathDeactivateDelay);

        Deactivate();
    }

    public void Deactivate()
    {
        StopRunningCoroutines();

        target = null;
        currentHp = maxHp;
        baseMoveSpeed = 0f;
        speedMultiplier = 1f;
        nextAttackTime = 0f;

        currentState = EnemyState.Idle;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        if (enemyCollider != null)
        {
            enemyCollider.enabled = true;
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.white;
        }

        gameObject.SetActive(false);
    }

    private void ChangeState(EnemyState newState)
    {
        if (currentState == newState)
        {
            return;
        }

        currentState = newState;
        ApplyAnimatorState();
    }

    private void ApplyAnimatorState()
    {
        if (animator == null)
        {
            return;
        }

        bool isWalking = currentState == EnemyState.Walk;
        animator.SetBool(IsWalkingHash, isWalking);
    }

    private void ResetAnimator()
    {
        if (animator == null)
        {
            return;
        }

        animator.Rebind();
        animator.Update(0f);

        animator.ResetTrigger(AttackHash);
        animator.ResetTrigger(HitHash);
        animator.ResetTrigger(DieHash);
        animator.SetBool(IsWalkingHash, false);
    }

    private void StopRunningCoroutines()
    {
        if (attackRoutine != null)
        {
            StopCoroutine(attackRoutine);
            attackRoutine = null;
        }

        if (hitRoutine != null)
        {
            StopCoroutine(hitRoutine);
            hitRoutine = null;
        }

        if (deathRoutine != null)
        {
            StopCoroutine(deathRoutine);
            deathRoutine = null;
        }
    }

    private bool IsPlaying()
    {
        return GameStateManager.Instance != null &&
               GameStateManager.Instance.CurrentState == GameState.Playing;
    }

    public Vector3 GetAimPosition()
    {
        if (aimPoint != null)
        {
            return aimPoint.position;
        }

        if (bodyCollider != null)
        {
            return bodyCollider.bounds.center;
        }

        return transform.position;
    }

    public void SetSpeedMultiplier(float multiplier)
    {
        speedMultiplier = Mathf.Max(0f, multiplier);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!drawAttackDistance)
        {
            return;
        }

        Gizmos.color = attackDistanceColor;
        Gizmos.DrawWireSphere(transform.position, attackDistance);
    }
#endif
}