using UnityEngine;

public class Frigate : MonoBehaviour
{
    public int frigateHp = 30;
    public int frigateDamage = 20;
    public float frigateSpeed = 0.05f;

    void Start()
    {

    }

    void Update()
    {
        EnemySpawner.OscilatingMovement(gameObject, frigateSpeed);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent<CPlayer>(out var player))
        {
            // Debug.Log("Player Collision");
            player.TakeDamage(frigateDamage);
        }
    }
}
