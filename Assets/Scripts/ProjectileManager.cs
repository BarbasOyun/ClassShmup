using UnityEngine;

public class ProjectileManager : MonoBehaviour
{
    // TODO : Use Service Locator
    public static ProjectileManager instance;

    [Header("Player Laser")]
    public GameObject laserPrefab;
    public int laserDamage = 10;
    public float laserSpeed = 0.3f;

    // Laser ECS
    int maxProjectile = 500;
    int activeCount = 0;

    int[] versions;
    GameObject[] playerLasers;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        versions = new int[maxProjectile];
        playerLasers = new GameObject[maxProjectile];

        for (int i = 0; i < maxProjectile; i++)
        {
            GameObject laser = Instantiate(laserPrefab, transform);
            laser.SetActive(false);

            var identity = laser.GetComponent<Entity>();
            identity.index = i;

            playerLasers[i] = laser;
        }
    }

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

    public GameObject SpawnProjectile()
    {
        if (activeCount >= maxProjectile)
        {
            return null;
        }

        GameObject newLaser = playerLasers[activeCount];
        newLaser.SetActive(true);
        versions[activeCount]++;

        if (newLaser.TryGetComponent(out Bullet entity))
        {
            entity.version = versions[activeCount];
        }

        activeCount++;

        return newLaser;
    }

    public void RemoveProjectile(int indexToRemove)
    {
        GameObject removedLaser = playerLasers[indexToRemove];
        removedLaser.SetActive(false);

        int lastIndex = activeCount - 1;

        if (indexToRemove != lastIndex)
        {
            // Last Object -> Removed Index
            GameObject movedProjectile = playerLasers[lastIndex];

            // Swap
            playerLasers[indexToRemove] = movedProjectile;
            playerLasers[lastIndex] = removedLaser;

            // Update GameObject Index
            if (movedProjectile.TryGetComponent(out Entity entity))
            {
                entity.index = indexToRemove;
            }

            if (removedLaser.TryGetComponent(out Entity removedEntity))
            {
                removedEntity.index = lastIndex;
            }
        }

        // Update Version
        versions[indexToRemove]++;
        activeCount--;
    }

    public void LaserHit(int laserIndex, int entityIndex, int entityVersion)
    {
        EnemyManager.instance.ApplyDamage(entityIndex, entityVersion, laserDamage);
        RemoveProjectile(laserIndex);
    }

    public bool IsOutOfBond(Vector3 pos, float horizontalLimit, float verticalLimit)
    {
        return pos.x > horizontalLimit || pos.x < -horizontalLimit || pos.y > verticalLimit || pos.y < -verticalLimit;
    }

    void LaserMovements()
    {
        // Cleanup Lasers out of frame
        float verticalLimit = Camera.main.orthographicSize;
        float horizontalLimit = Camera.main.orthographicSize * Screen.width / Screen.height;

        for (int i = 0; i < activeCount; i++)
        {
            playerLasers[i].transform.position += playerLasers[i].transform.up * laserSpeed;

            if (IsOutOfBond(playerLasers[i].transform.position, horizontalLimit, verticalLimit))
            {
                RemoveProjectile(i);
            }
        }

        // foreach (GameObject laser in laserGameObjects)
        // {
        //     laser.transform.position += laser.transform.up * laserSpeed; // * Time.deltaTime;
        // }
    }
}
