using UnityEngine;

namespace Portfolio.Gameplay.Player
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(PlayerInputReader))]
    public class TopDownCharacterMover : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private PlayerHealth playerHealth;

        private Rigidbody2D rb;
        private PlayerInputReader inputReader;
        private Vector2 moveDirection;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            inputReader = GetComponent<PlayerInputReader>();

            if (playerHealth == null)
            {
                playerHealth = GetComponent<PlayerHealth>();
            }

            rb.gravityScale = 0f;
            rb.freezeRotation = true;
        }

        private void Update()
        {
            if (!CanMove())
            {
                moveDirection = Vector2.zero;
                return;
            }

            moveDirection = inputReader.MoveInput;
        }

        private void FixedUpdate()
        {
            if (moveDirection.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            Vector2 nextPosition = rb.position + moveDirection * moveSpeed * Time.fixedDeltaTime;
            rb.MovePosition(nextPosition);
        }

        private bool CanMove()
        {
            if (playerHealth != null && playerHealth.IsDead)
            {
                return false;
            }

            if (GameStateManager.Instance == null)
            {
                return true;
            }

            return GameStateManager.Instance.CurrentState == GameState.Playing;
        }
    }
}