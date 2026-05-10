using UnityEngine;

public class Laser : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent(out Scout scout))
        {
            scout.TakeDamage(CPlayer.instance.laserDamage);
        }
        else if (other.TryGetComponent(out Frigate frigate))
        {
            frigate.TakeDamage(CPlayer.instance.laserDamage);
        }

        CPlayer.instance.RemoveLaser(gameObject);
    }
}
