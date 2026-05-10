using System.Linq;
using UnityEngine;

public class ProjectileECS : ECS<ProjectileManager, ProjectileData> { }

public class ProjectileManager : MonoBehaviour, ProjectileECS.IECSState
{
    public static ProjectileManager instance;

    // DATA SOURCE
    [Header("Player Laser")]
    public GameObject laserPrefab;
    public int laserDamage = 10;
    public float laserSpeed = 0.3f;

    // ECS Implementation
    public enum ProjectileType { Laser, _Count }

    public ProjectileData[] entityTypes { get; set; }
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
        }else {
            Destroy(gameObject);
        }

        InitEntityType();
    }

    void Start()
    {

    }

    void FixedUpdate()
    {
        ProjectileECS.EntitiesUpdateLogic(this);
    }

    void Update()
    {

    }

    public GameObject SpawnProjectile(ProjectileType type, Vector2 direction)
    {
        // ProjectileData entityType = entityTypes[(int)type];
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

    ProjectileData LaserType(int maxLaser)
    {
        void LaserUpdate(ref ProjectileData type, in int index)
        {
            RemoveOutOfBound(ref type, index);
            ProjectileMovements(in type, in index);
        }

        return new ProjectileData(
            (int)ProjectileType.Laser,
            maxLaser,
            laserPrefab,
            SpawnLogic,
            RemoveLogic,
            LaserUpdate,
            laserDamage,
            laserSpeed
        );
    }

    public void SpawnLogic(ref ProjectileData type, in int index)
    {
        // var entityData = type.entityData;
        projectileDirections[index] = nextDirection;

        // Capability
    }

    public void RemoveLogic(in ProjectileData type, in int index, in int originalIndex)
    {
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

        ref ProjectileData entityType = ref ProjectileECS.IndexToEntityType(this, projectilIndex);

        // TODO : Projectile Type Effect
        EnemyManager.instance.ApplyDamage(entityIndex, entityVersion, laserDamage);
        ProjectileECS.RemoveEntity(this, ref entityType, projectilIndex);
    }

    public void RemoveOutOfBound(ref ProjectileData type, in int index)
    {
        if (IsOutOfBond(entityGameObjects[index].transform.position, EnemyManager.instance.horizontalLimit, EnemyManager.instance.verticalLimit))
        {
            ProjectileECS.RemoveEntity(this, ref type, index);
        }
    }

    public static void DirectionalMovement(GameObject gameObject, Vector2 direction, float speed)
    {
        gameObject.transform.position += (Vector3)(direction * speed);
    }

    public void ProjectileMovements(in ProjectileData type, in int index)
    {
        DirectionalMovement(entityGameObjects[index], projectileDirections[index], type.speed);
    }

    public void RemoveAll()
    {
        ProjectileECS.RemoveAll(this);
    }
}

// PROJECTILE TYPE > Data Definition
public struct ProjectileData : ProjectileECS.IEntityType
{
    public ProjectileData(int type, int maxEntities, GameObject prefab, ProjectileECS.EntityLogic spawnLogic, ProjectileECS.RemoveLogic removeLogic, ProjectileECS.EntityLogic updateLogic,
        int damage, float speed) // int capabilityMask = 0
    {
        this.type = type;
        startIndex = 0;
        this.maxEntities = maxEntities;
        activeCount = 0;
        this.prefab = prefab;
        this.spawnLogic = spawnLogic;
        this.removeLogic = removeLogic;
        this.updateLogic = updateLogic;

        this.damage = damage;
        this.speed = speed;

        // this.capabilityMask = capabilityMask;
        // dataIndex = new int[(int)EnemyManager.Capability._Count]; // TODO : Set size = Last capacity index
        // dataOffsets = new int[(int)EnemyManager.Capability._Count];
    }

     // IEntityType Implementation
    public int type { get; }
    public int startIndex { get; set; }
    public int maxEntities { get; }
    public int activeCount { get; set; }
    public GameObject prefab { get; }
    public ProjectileECS.EntityLogic spawnLogic { get; }
    public ProjectileECS.RemoveLogic removeLogic { get; }
    public ProjectileECS.EntityLogic updateLogic { get; }

    // DEFAULT STATS
    public readonly int damage;
    public readonly float speed;

    // ADDITIONAL STATS > Modifiers Later
    // public readonly int capabilityMask; // Bitmask
    // public readonly int[] dataIndex; // Authoring array // Sparse? index = Capability -> For
    // public readonly int[] dataOffsets; // Runtime array
}
