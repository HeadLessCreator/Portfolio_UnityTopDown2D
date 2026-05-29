using System;
using System.Collections;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private int maxHeart = 5;
    [SerializeField] private float invincibleDuration = 1f;

    [Header("Animation")]
    [SerializeField] private PlayerAnimationController animationController;

    [Header("References")]
    [SerializeField] private PlayerAutoAttack playerAutoAttack;

    [Header("Invincible Visual")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private float blinkInterval = 0.1f;
    [SerializeField, Range(0f, 1f)] private float blinkAlpha = 0.35f;
    
    [SerializeField] private string playerHitSfxKey = "PlayerHit";

    public int CurrentHeart { get; private set; }
    public bool IsInvincible { get; private set; }
    public bool IsDead { get; private set; }

    public event Action<int> OnDamaged;
    public event Action OnDead;

    private Coroutine invincibleRoutine;

    private void Awake()
    {
        if (animationController == null)
        {
            animationController = GetComponent<PlayerAnimationController>();
        }

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        if (playerAutoAttack == null)
        {
            playerAutoAttack = GetComponent<PlayerAutoAttack>();
        }

        ResetHealth();
    }

    public void ResetHealth()
    {
        CurrentHeart = maxHeart;
        IsInvincible = false;
        IsDead = false;

        if (invincibleRoutine != null)
        {
            StopCoroutine(invincibleRoutine);
            invincibleRoutine = null;
        }

        SetSpriteAlpha(1f);
        animationController?.ResetAnimationState();
    }

    public void TakeDamage(int damage = 1)
    {
        if (IsDead)
        {
            return;
        }

        if (CurrentHeart <= 0)
        {
            return;
        }

        if (IsInvincible)
        {
            return;
        }

        CurrentHeart = Mathf.Max(0, CurrentHeart - damage);
        OnDamaged?.Invoke(CurrentHeart);

        playerAutoAttack?.ResetAttackState();

        if (CurrentHeart <= 0)
        {
            Die();
            return;
        }

        animationController?.PlayHit();
        invincibleRoutine = StartCoroutine(InvincibleRoutine());
        SoundManager.Instance.PlayEffect(playerHitSfxKey);
    }

    private void Die()
    {
        if (IsDead)
        {
            return;
        }

        IsDead = true;
        IsInvincible = false;

        playerAutoAttack?.ResetAttackState();

        if (invincibleRoutine != null)
        {
            StopCoroutine(invincibleRoutine);
            invincibleRoutine = null;
        }

        SetSpriteAlpha(1f);
        animationController?.PlayDie();

        OnDead?.Invoke();
    }

    private IEnumerator InvincibleRoutine()
    {
        IsInvincible = true;

        float elapsed = 0f;

        while (elapsed < invincibleDuration)
        {
            SetSpriteAlpha(blinkAlpha);
            yield return new WaitForSeconds(blinkInterval);

            SetSpriteAlpha(1f);
            yield return new WaitForSeconds(blinkInterval);

            elapsed += blinkInterval * 2f;
        }

        SetSpriteAlpha(1f);
        IsInvincible = false;
        invincibleRoutine = null;
    }

    private void SetSpriteAlpha(float alpha)
    {
        if (spriteRenderer == null)
        {
            return;
        }

        Color color = spriteRenderer.color;
        color.a = alpha;
        spriteRenderer.color = color;
    }
}