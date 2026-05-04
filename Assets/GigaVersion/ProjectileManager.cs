using System.Linq;
using UnityEngine;

public class ProjectileManager : MonoBehaviour, ECS<ProjectileData, ProjectileManager>.IECSState
{
    // TODO : Use Service Locator
    public static ProjectileManager instance;

    public float verticalLimit;
    public float horizontalLimit;

    // DATA SOURCE
    [Header("Player Laser")]
    public GameObject laserPrefab;
    public int laserDamage = 10;
    public float laserSpeed = 0.3f;

    // ECS Implementation
    public class ProjectileECS : ECS<ProjectileData, ProjectileManager> { }

    public enum ProjectileType { Laser, _Count }

    public ProjectileECS.EntityType[] entityTypes { get; set; }
    public GameObject[] entityGameObjects { get; set; }
    public int[] versions { get; set; }
    // Custom fields
    Vector2[] projectileDirections;
    Vector2 nextDirection;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        InitEntityType();
    }

    void Start()
    {

    }

    void FixedUpdate()
    {
        verticalLimit = Camera.main.orthographicSize;
        horizontalLimit = Camera.main.orthographicSize * Screen.width / Screen.height;

        ProjectileECS.EntitiesUpdateLogic(this);
    }

    void Update()
    {

    }

    public GameObject SpawnProjectile(ProjectileType type, Vector2 direction)
    {
        // ProjectileECS.EntityType entityType = entityTypes[(int)type];
        // int spawnIndex = entityType.startIndex + entityType.activeCount;
        // projectileDirections[spawnIndex] = direction;

        nextDirection = direction;
        GameObject newLaser = ProjectileECS.SpawnEntityType(this, (int)type);

        return newLaser;
    }

    public bool IsOutOfBond(Vector3 pos, float horizontalLimit, float verticalLimit)
    {
        return pos.x > horizontalLimit || pos.x < -horizontalLimit || pos.y > verticalLimit || pos.y < -verticalLimit;
    }

    // ECS Implementation
    void InitEntityType()
    {
        int[] typeCount = new int[(int)ProjectileType._Count];
        typeCount[(int)ProjectileType.Laser] = 300;
        int totalEntity = typeCount.Sum();

        // CREATE Types
        entityTypes = new[] {
            LaserType(typeCount[(int)ProjectileType.Laser]),
            };

        // CREATE Runtime arrays
        // this.dataRegistry = dataRegistry;
        entityGameObjects = new GameObject[totalEntity];
        versions = new int[totalEntity];
        projectileDirections = new Vector2[totalEntity];

        ProjectileECS.Init(this, gameObject);
    }

    ProjectileECS.EntityType LaserType(int maxLaser)
    {
        void LaserUpdate(ref ProjectileECS.EntityType type, in int index)
        {
            RemoveOutOfBound(ref type, index);
            ProjectileMovements(in type, in index);
        }

        ProjectileData laserData = new ProjectileData(laserDamage, laserSpeed);

        return new ProjectileECS.EntityType(
            (int)ProjectileType.Laser,
            maxLaser,
            laserPrefab,
            laserData,
            SpawnLogic,
            RemoveLogic,
            LaserUpdate
        );
    }

    public void SpawnLogic(ref ProjectileECS.EntityType type, in int index)
    {
        // var entityData = type.entityData;
        projectileDirections[index] = nextDirection;

        // Capability
    }

    public void RemoveLogic(in ProjectileECS.EntityType type, in int index, in int originalIndex)
    {
        var entityData = type.entityData;
        projectileDirections[index] = projectileDirections[originalIndex];

        // Capability
    }

    public void ProjectileHit(int projectilIndex, int projectileVersion, int entityIndex, int entityVersion)
    {
        if (versions[projectilIndex] != projectileVersion)
        {
            Debug.LogWarning("Wrong Laser Version");
            return;
        }

        ref ProjectileECS.EntityType entityType = ref ProjectileECS.IndexToEntityType(this, projectilIndex);

        // TODO : Projectile Type Effect
        EnemyManager.instance.ApplyDamage(entityIndex, entityVersion, laserDamage);
        ProjectileECS.RemoveEntity(this, ref entityType, projectilIndex);
    }

    public void RemoveOutOfBound(ref ProjectileECS.EntityType type, in int index)
    {
        if (IsOutOfBond(entityGameObjects[index].transform.position, horizontalLimit, verticalLimit))
        {
            ProjectileECS.RemoveEntity(this, ref type, index);
        }
    }

    public static void DirectionalMovement(GameObject gameObject, Vector2 direction, float speed)
    {
        gameObject.transform.position += (Vector3)(direction * speed);
    }

    public void ProjectileMovements(in ProjectileECS.EntityType type, in int index)
    {
        DirectionalMovement(entityGameObjects[index], projectileDirections[index], type.entityData.speed);
    }
}

// PROJECTILE TYPE > Data Definition
public readonly struct ProjectileData
{
    public ProjectileData(int damage, float speed) // int capabilityMask = 0
    {
        this.damage = damage;
        this.speed = speed;

        // this.capabilityMask = capabilityMask;
        // dataIndex = new int[(int)EnemyManager.Capability._Count]; // TODO : Set size = Last capacity index
        // dataOffsets = new int[(int)EnemyManager.Capability._Count];
    }

    // DEFAULT STATS
    public readonly int damage;
    public readonly float speed;

    // ADDITIONAL STATS > Modifiers Later
    // public readonly int capabilityMask; // Bitmask
    // public readonly int[] dataIndex; // Authoring array // Sparse? index = Capability -> For
    // public readonly int[] dataOffsets; // Runtime array
}
