using UnityEngine;

public class Bullet : Entity
{
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent<EnemyEntity>(out var entity))
        {
            // print("Bullet Collision");
            ProjectileManager.instance.LaserHit(index, entity.index, entity.version);
        }
    }
}
