using Portfolio.Gameplay.Player;
using UnityEngine;

public class PlayerAnimationController : MonoBehaviour
{
    [SerializeField] private PlayerInputReader inputReader;
    [SerializeField] private Animator animator;

    private static readonly int IsWalkingHash = Animator.StringToHash("IsWalking");
    private static readonly int AttackHash = Animator.StringToHash("Attack");
    private static readonly int HitHash = Animator.StringToHash("Hit");
    private static readonly int DieHash = Animator.StringToHash("Die");

    private bool isDead;

    private void Awake()
    {
        if (inputReader == null)
        {
            inputReader = GetComponent<PlayerInputReader>();
        }

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
    }

    private void Update()
    {
        if (isDead)
        {
            return;
        }

        if (animator == null || inputReader == null)
        {
            return;
        }

        bool isWalking = inputReader.MoveInput.sqrMagnitude > 0.01f;
        animator.SetBool(IsWalkingHash, isWalking);
    }

    public void ResetAnimationState()
    {
        isDead = false;

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

    public void PlayAttack()
    {
        if (isDead)
        {
            return;
        }

        animator?.SetTrigger(AttackHash);
    }

    public void PlayHit()
    {
        if (isDead)
        {
            return;
        }

        animator?.SetTrigger(HitHash);
    }

    public void PlayDie()
    {
        if (isDead)
        {
            return;
        }

        isDead = true;

        if (animator == null)
        {
            return;
        }

        animator.SetBool(IsWalkingHash, false);
        animator.ResetTrigger(AttackHash);
        animator.ResetTrigger(HitHash);
        animator.SetTrigger(DieHash);
    }
}