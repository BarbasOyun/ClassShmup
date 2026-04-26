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
    Vector2[] laserDirections;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        versions = new int[maxProjectile];
        playerLasers = new GameObject[maxProjectile];
        laserDirections = new Vector2[maxProjectile];

        for (int i = 0; i < maxProjectile; i++)
        {
            GameObject laser = Instantiate(laserPrefab, transform);
            laser.SetActive(false);

            if (laser.TryGetComponent(out Entity entity))
            {
                entity.index = i;
            }

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

    public GameObject SpawnProjectile(Vector2 direction)
    {
        if (activeCount >= maxProjectile)
        {
            return null;
        }

        // Spawn GameObject
        GameObject newLaser = playerLasers[activeCount];
        newLaser.SetActive(true);

        // Rotate Laser
        // newLaser.transform.up = direction; // Unity
        newLaser.transform.rotation = Player.LookRotation2D(direction); // Engine Agnostic

        // Set Data
        versions[activeCount]++;
        laserDirections[activeCount] = direction;

        if (newLaser.TryGetComponent(out Bullet entity))
        {
            entity.index = activeCount;
            entity.version = versions[activeCount];
        }

        activeCount++;

        return newLaser;
    }

    public void RemoveProjectile(int indexToRemove)
    {
        GameObject removedLaser = playerLasers[indexToRemove];
        removedLaser.SetActive(false);

        // Update Version
        versions[indexToRemove]++;
        activeCount--; // LastIndex

        // Debug.Log($"REMOVE laser Entity at Index = {indexToRemove}, REPLACE Last Index = {activeCount}");

        if (indexToRemove != activeCount)
        {
            // Last Object -> Removed Index
            GameObject movedProjectile = playerLasers[activeCount];
            playerLasers[activeCount] = removedLaser;

            // Swap Data
            playerLasers[indexToRemove] = movedProjectile;
            laserDirections[indexToRemove] = laserDirections[activeCount];

            // Update GameObject Index
            if (movedProjectile.TryGetComponent(out Entity entity))
            {
                entity.index = indexToRemove;
                entity.version = versions[indexToRemove];
            }
        }
    }

    public void LaserHit(int laserIndex, int laserVersion, int entityIndex, int entityVersion)
    {
        if (laserVersion != versions[laserIndex]) return;

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
            // playerLasers[i].transform.position += playerLasers[i].transform.up * laserSpeed;
            playerLasers[i].transform.position += (Vector3) (laserDirections[i] * laserSpeed);

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
