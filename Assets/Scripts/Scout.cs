using UnityEngine;

public class Scout : MonoBehaviour
{
    public int scoutHp = 10;
    public int scoutDamage = 10;
    public float scoutSpeed = 3f;

    void Start()
    {

    }

    void Update()
    {
        EnemySpawner.StraightMovement(gameObject, scoutSpeed);

        if (EnemySpawner.instance.IsOutOfBounds(transform.position))
        {
            Die();
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent<CPlayer>(out var player))
        {
            // Debug.Log("Player Collision");
            player.TakeDamage(scoutDamage);
            Die();
        }
    }

    public void TakeDamage(int damage)
    {
        scoutHp -= damage;

        if (scoutHp <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        EnemySpawner.instance.IncrementScore(1);
        Destroy(gameObject);
    }
}
