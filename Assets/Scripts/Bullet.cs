using UnityEngine;

public class Bullet : MonoBehaviour
{
    public int index;
    public int version;

    [Header("STATS")]
    public int damage = 10;

    // void Start()
    // {

    // }

    // void Update()
    // {

    // }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent<EnemyEntity>(out var identity))
        {
            // print("Bullet Collision");
            EnemyManager.instance.ApplyDamage(identity.index, identity.version, damage);
            Destroy(gameObject);
        }
    }
}
