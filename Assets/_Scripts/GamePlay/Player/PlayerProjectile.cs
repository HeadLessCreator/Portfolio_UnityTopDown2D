using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class PlayerProjectile : MonoBehaviour
{
    private Rigidbody2D rb;
    private Collider2D projectileCollider;

    private Vector2 moveDirection;
    private float moveSpeed;
    private int damage;

    private Coroutine lifeRoutine;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        projectileCollider = GetComponent<Collider2D>();

        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.bodyType = RigidbodyType2D.Kinematic;

        projectileCollider.isTrigger = true;
    }

    public void Initialize(Vector2 direction, float speed, float lifeTime, int projectileDamage)
    {
        moveDirection = direction.normalized;
        moveSpeed = speed;
        damage = projectileDamage;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        gameObject.SetActive(true);

        if (lifeRoutine != null)
        {
            StopCoroutine(lifeRoutine);
        }

        lifeRoutine = StartCoroutine(LifeRoutine(lifeTime));
    }

    private void FixedUpdate()
    {
        if (!IsPlaying())
        {
            return;
        }

        Vector2 nextPosition = rb.position + moveDirection * moveSpeed * Time.fixedDeltaTime;
        rb.MovePosition(nextPosition);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        EnemyController enemy = other.GetComponentInParent<EnemyController>();

        if (enemy == null)
        {
            return;
        }

        enemy.TakeDamage(damage);
        Deactivate();
    }

    public void Deactivate()
    {
        if (lifeRoutine != null)
        {
            StopCoroutine(lifeRoutine);
            lifeRoutine = null;
        }

        moveDirection = Vector2.zero;
        moveSpeed = 0f;
        damage = 0;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        gameObject.SetActive(false);
    }

    private IEnumerator LifeRoutine(float lifeTime)
    {
        yield return new WaitForSeconds(lifeTime);
        Deactivate();
    }

    private bool IsPlaying()
    {
        return GameStateManager.Instance != null &&
               GameStateManager.Instance.CurrentState == GameState.Playing;
    }
}