using UnityEngine;

public class Frigate : MonoBehaviour
{
    public int frigateHp = 30;
    public int frigateDamage = 20;
    public float frigateSpeed = 1f;

    void Start()
    {

    }

    void Update()
    {
        EnemySpawner.OscilatingMovement(gameObject, frigateSpeed);

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
            player.TakeDamage(frigateDamage);
            Die();
        }
    }

    public void TakeDamage(int damage)
    {
        frigateHp -= damage;

        if (frigateHp <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        EnemySpawner.instance.IncrementScore(3);
        Destroy(gameObject);
    }
}
