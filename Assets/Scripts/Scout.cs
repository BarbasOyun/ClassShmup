using UnityEngine;

public class Scout : MonoBehaviour
{
    public int scoutHp = 10;
    public int scoutDamage = 10;
    public float scoutSpeed = 0.1f;

    void Start()
    {

    }

    void Update()
    {
        EnemySpawner.StraightMovement(gameObject, scoutSpeed);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent<CPlayer>(out var player))
        {
            // Debug.Log("Player Collision");
            player.TakeDamage(scoutDamage);
        }
    }
}
