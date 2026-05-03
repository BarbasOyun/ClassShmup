using System.Collections.Generic;
using UnityEngine;

public class CProjectileManager : MonoBehaviour
{
    [Header("Player Laser")]
    public GameObject laserPrefab;
    public int laserDamage = 10;
    public float laserSpeed = 0.3f;
    public List<GameObject> laserGameObjects = new List<GameObject>();

    public float verticalLimit;
    public float horizontalLimit;

    void Start()
    {

    }

    void FixedUpdate()
    {
        LaserMovements();
    }

    void Update()
    {

    }

    void LaserMovements()
    {
        // Cleanup Lasers out of frame
        verticalLimit = Camera.main.orthographicSize;
        horizontalLimit = Camera.main.orthographicSize * Screen.width / Screen.height;

        foreach (GameObject laser in laserGameObjects)
        {
            laser.transform.position += laser.transform.up * laserSpeed; // * Time.deltaTime;
        }
    }

    public bool IsOutOfBond(Vector3 pos, float horizontalLimit, float verticalLimit)
    {
        return pos.x > horizontalLimit || pos.x < -horizontalLimit || pos.y > verticalLimit || pos.y < -verticalLimit;
    }
}
