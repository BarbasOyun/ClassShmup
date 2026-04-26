using System;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    // TODO : Use Service Locator
    public static EnemyManager instance;

    public enum EnemyType { Scout, Frigate }

    [Header("ENEMY SPAWNER")]
    public GameObject[] spawnLocations; // 3
    public float spawnDelta = 1f;
    public int minEnemies = 2;
    public int enemiesRange = 2;
    public Vector2 enemiesDirection;
    float angle;
    Quaternion lookDirection;

    // [Header("ECS")]
    struct EntityType
    {
        public EntityType(EnemyType type, int maxEntities, GameObject prefab, UnitData unitData, UnitLogic unitLogic)
        {
            this.type = type;
            startIndex = 0;
            this.maxEntities = maxEntities;
            activeCount = 0;
            this.prefab = prefab;
            this.unitData = unitData;
            this.unitLogic = unitLogic;
        }

        public EnemyType type;
        public int startIndex;
        public int maxEntities;
        public int activeCount;
        public GameObject prefab;
        public UnitData unitData;
        public UnitLogic unitLogic;

        public override string ToString() => $"(Type = {type}, StartIndex = {startIndex}, MaxEntities = {maxEntities}, ActiveCount = {activeCount})";
    }

    EntityType[] entityTypes;

    int maxEnemies;
    int[] versions;
    GameObject[] enemyGameObjects;
    int[] enemyHps;
    // int[] enemyShield;

    // DATA SOURCE
    // TODO : Game Engine Agnostic = Move to file, Unity = ScriptableObject
    [Header("ENEMY - Scout")]
    public GameObject scoutPrefab;
    public int scoutHp = 10;
    public int scoutDamage = 10;
    public float scoutSpeed = 0.1f;

    [Header("ENEMY - Frigate")]
    public GameObject frigatePrefab;
    public int frigateHp = 30;
    public int frigateDamage = 20;
    public float frigateSpeed = 0.05f;
    public float shield = 10;

    // LOGIC
    public EntityUpdateLogic straightMovement = (entityBody, speed) => entityBody.transform.position += entityBody.transform.up * speed;
    public EntityUpdateLogic oscilatingMovement = (entityBody, speed) => entityBody.transform.position += (entityBody.transform.right * (float) Math.Sin(Time.time) + entityBody.transform.up) * speed;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        angle = Mathf.Atan2(enemiesDirection.y, enemiesDirection.x) * Mathf.Rad2Deg - 90;
        lookDirection = Quaternion.Euler(0, 0, angle);

        InitECS();
    }

    void Start()
    {
        InvokeRepeating(nameof(SpawnEnemies), 1.0f, spawnDelta);
    }

    void FixedUpdate()
    {
        EntitiesUpdateLogic();
    }

    void Update()
    {

    }

    // ENEMY SPAWNER
    void SpawnEnemies()
    {
        // Spawn Scout
        int enemiesNbrRoll = UnityEngine.Random.Range(minEnemies, minEnemies + enemiesRange);

        for (int i = 0; i < enemiesNbrRoll; i++)
        {
            GameObject spawnedEnemy = SpawnEnemyType(EnemyType.Scout);

            if (!spawnedEnemy) return;

            SetEnemyTransform(spawnedEnemy);
        }

        // Spawn Frigate
        SetEnemyTransform(SpawnEnemyType(EnemyType.Frigate));
    }

    void SetEnemyTransform(GameObject enemy)
    {
        enemy.transform.rotation = lookDirection;
        int locationOffset = (int)UnityEngine.Random.Range(spawnLocations[1].transform.position.x, spawnLocations[2].transform.position.x);
        enemy.transform.position = spawnLocations[0].transform.position + new Vector3(locationOffset, 0);
    }

    // ECS
    void InitECS()
    {
        InitEntityType();

        // Create Dynamic Arrays
        int totalEntity = 0;

        foreach (EntityType entityType in entityTypes)
        {
            totalEntity += entityType.maxEntities;
        }

        enemyGameObjects = new GameObject[totalEntity];
        versions = new int[totalEntity];
        enemyHps = new int[totalEntity];
        maxEnemies = totalEntity;

        // Instantiate/Setup GameObjects
        int totalEntityCount = 0;

        for (int i = 0; i < entityTypes.Length; i++)
        {
            for (int j = 0; j < entityTypes[i].maxEntities; j++)
            {
                GameObject spawnedPrefab = Instantiate(entityTypes[i].prefab, transform);
                spawnedPrefab.SetActive(false);

                int index = totalEntityCount + j;

                if (spawnedPrefab.TryGetComponent(out Entity entity))
                {
                    entity.index = index;
                }

                enemyGameObjects[index] = spawnedPrefab;
            }

            entityTypes[i].startIndex = totalEntityCount;
            totalEntityCount += entityTypes[i].maxEntities;
        }
    }

    void InitEntityType()
    {
        // Scout
        UnitData scoutData = new UnitData( scoutHp, scoutDamage, scoutSpeed);
        UnitLogic scoutLogic = new UnitLogic(straightMovement);

        // Frigate
        UnitData frigateData = new UnitData(frigateHp, frigateDamage, frigateSpeed, new ShieldData());
        UnitLogic frigateLogic = new UnitLogic(oscilatingMovement);

        entityTypes = new EntityType[] {
            new EntityType(EnemyType.Scout, 250, scoutPrefab, scoutData, scoutLogic),
            new EntityType(EnemyType.Frigate, 250, frigatePrefab, frigateData, frigateLogic)
            };
    }

    ref EntityType FindEntityType(EnemyType type)
    {
        int typeIndex = 0;
        EntityType currentType = entityTypes[typeIndex];

        while (currentType.type != type)
        {
            typeIndex++;

            // Exit loop if type not found
            if (typeIndex >= entityTypes.Length)
            {
                Debug.LogError("Type Not Found");
                return ref entityTypes[typeIndex];
            }

            currentType = entityTypes[typeIndex];
        }

        // Debug.Log($"FIND ENTITY TYPE : EnemyType = {type} -> EntityType = {currentType}");
        return ref entityTypes[typeIndex];
    }

    ref EntityType IndexToEntityType(int index)
    {
        int typeIndex = 0;
        EntityType currentType = entityTypes[typeIndex];

        while (index < currentType.startIndex || currentType.startIndex + currentType.maxEntities - 1 < index)
        {
            typeIndex++;

            // Exit loop if type not found
            if (typeIndex >= entityTypes.Length)
            {
                Debug.LogError("Type Not Found");
                return ref entityTypes[typeIndex];
            }

            currentType = entityTypes[typeIndex];
        }

        // Debug.Log($"INDEX TO ENTITY : index = {index} -> EntityType = {currentType}");
        return ref entityTypes[typeIndex]; ;
    }

    public GameObject SpawnEnemyType(EnemyType type)
    {
        ref EntityType entityType = ref FindEntityType(type);

        if (entityType.activeCount >= entityType.maxEntities)
        {
            Debug.LogWarning($"Max Entities Reached on : {entityType}");
            return null;
        }

        // Spawn Next
        int spawnIndex = entityType.startIndex + entityType.activeCount;
        GameObject spawnedEnemy = enemyGameObjects[spawnIndex];
        spawnedEnemy.SetActive(true);

        versions[spawnIndex]++;
        enemyHps[spawnIndex] = entityType.unitData.hp;

        if (spawnedEnemy.TryGetComponent(out Entity entity))
        {
            entity.index = spawnIndex;
            entity.version = versions[spawnIndex];
        }

        entityType.activeCount++;

        // Debug.Log($"Spawned {entityType.type} HP = {enemyHps[spawnIndex]}");

        return spawnedEnemy;
    }

    public void RemoveEnemy(int indexToRemove)
    {
        ref EntityType entityType = ref IndexToEntityType(indexToRemove);

        GameObject removedEnemy = enemyGameObjects[indexToRemove];
        removedEnemy.SetActive(false);

        // Update Version
        versions[indexToRemove]++;
        entityType.activeCount--;

        int lastEntityIndex = entityType.startIndex + entityType.activeCount;

        // Debug.Log($"REMOVE {entityType.type} Entity at Index = {indexToRemove}, REPLACE Last Index = {lastEntityIndex}");

        if (indexToRemove != lastEntityIndex)
        {
            // Swap GameObjects
            GameObject movedEnemy = enemyGameObjects[lastEntityIndex];
            enemyGameObjects[lastEntityIndex] = removedEnemy;

            // Move Data : Last Object -> Removed Index
            enemyHps[indexToRemove] = enemyHps[lastEntityIndex];
            enemyGameObjects[indexToRemove] = movedEnemy;

            // Update Entity
            if (movedEnemy.TryGetComponent(out Entity entity))
            {
                entity.index = indexToRemove;
                entity.version = versions[indexToRemove];
            }
        }
    }

    void EntitiesUpdateLogic()
    {
        float horizontalLimit = (Camera.main.orthographicSize * Screen.width / Screen.height) + 5;
        float verticalLimit = Camera.main.orthographicSize + 5;

        foreach (EntityType entityType in entityTypes)
        {
            for (int i = entityType.startIndex; i < entityType.startIndex + entityType.maxEntities; i++)
            {
                if (!enemyGameObjects[i].activeSelf) continue;

                // Cleanup Enemies out of frame
                if (ProjectileManager.instance.IsOutOfBond(enemyGameObjects[i].transform.position,
                horizontalLimit, verticalLimit))
                {
                    RemoveEnemy(i);
                }

                // if (entityType.unitLogic.movementLogic == null) continue; // with immobile Units

                // entityType.unitLogic.movementLogic.UpdateMovement(enemyGameObjects[i], entityType.unitData.speed);
                entityType.unitLogic.movementLogic(enemyGameObjects[i], entityType.unitData.speed);
            }
        }
    }

    public void ApplyDamage(int index, int version, int damage)
    {
        if (versions[index] != version)
        {
            Debug.LogWarning("Wrong Version");
            return;
        }

        // TODO : Unit Type behavior
        int hp = enemyHps[index] -= damage;

        if (hp <= 0)
        {
            RemoveEnemy(index);
        }
    }
}

// ENTITY > DATA DEFINITION
public struct UnitData
{
    public UnitData(int hp, int damage, float speed, ShieldData? shieldData = null)
    {
        this.hp = hp;
        this.damage = damage;
        this.speed = speed;
        this.shieldData = shieldData;
    }

    // public readonly int maxHp;
    public readonly int hp;
    public readonly int damage;
    public readonly float speed;

    public ShieldData? shieldData;
}

// Atomic Data Composition
public struct ShieldData
{
    // public int maxShield;
    public int shield;
    // Recovery rate
}

// ENTITY > LOGIC DEFINITION
public delegate void EntityUpdateLogic(GameObject entityBody, float speed);

public struct UnitLogic
{
    public UnitLogic(EntityUpdateLogic movementLogic = null)
    {
        this.movementLogic = movementLogic;
    }

    public EntityUpdateLogic movementLogic;
}

// public class EnemyLogicRegistry
// {
//     private readonly IMovementLogic[] _movementRegistry;

//     public EnemyLogicRegistry()
//     {
//         _movementRegistry = new IMovementLogic[] {
//             new GroundMovement(), // Scout
//             new FlyMovement(), // Frigate
//         };

//         // AttackRegistry
//     }

//     public IMovementLogic GetMovement(EnemyManager.EnemyType type) => _movementRegistry[(int)type];
// }
