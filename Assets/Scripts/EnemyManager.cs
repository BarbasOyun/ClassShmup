using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    // TODO : Use Service Locator
    public static EnemyManager instance;

    public enum EnemyType { Scout, Frigate }

    [Header("ENEMY SPAWNER")]
    public GameObject[] spawnLocations; // 3
    public int minEnemies = 2;
    public int enemiesRange = 2;
    public Vector2 enemiesDirection;
    float angle;
    Quaternion lookDirection;

    // [Header("ECS")]
    struct EntityType
    {
        public EntityType(EnemyType type, int maxEntities, GameObject prefab, EntityUpdateLogic updateAction)
        {
            this.type = type;
            startIndex = 0;
            this.maxEntities = maxEntities;
            activeCount = 0;
            this.prefab = prefab;
            this.updateAction = updateAction;
        }

        public EnemyType type;
        public int startIndex;
        public int maxEntities;
        public int activeCount;
        public GameObject prefab;
        public EntityUpdateLogic updateAction;
        // Type Logic Definition
        // Type Data Definition -> Create runtime arrays based on it

        public override string ToString() => $"(Type = {type}, StartIndex = {startIndex}, MaxEntities = {maxEntities}, ActiveCount = {activeCount})";
    }

    EntityType[] entityTypes;

    int maxEnemies = 500;
    int activeCount = 0;

    int[] versions;
    GameObject[] enemyGameObjects;
    int[] enemyHps;
    // int[] enemyShield;

    // DATA
    // TODO : Game Engine Agnostic = Move to file, Unity = ScriptableObject
    [Header("ENEMY - Scout")]
    public GameObject scoutPrefab;
    public int scoutHp = 10;
    public float scoutSpeed = 0.1f;
    public float scoutDamage = 10;

    [Header("ENEMY - Frigate")]
    public GameObject frigatePrefab;
    public int frigateHp = 30;
    public float frigateSpeed = 0.05f;
    public float frigateDamage = 20;

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
        InvokeRepeating(nameof(SpawnEnemies), 2.0f, 1f);
    }

    void FixedUpdate()
    {
        EnemyMovements();
    }

    void Update()
    {

    }

    // ENEMY SPAWNER
    void SpawnEnemies()
    {
        if (activeCount >= maxEnemies)
        {
            Debug.LogWarning("Max Enemies");
            return;
        }

        int enemiesNbrRoll = Random.Range(minEnemies, minEnemies + enemiesRange);

        for (int i = 0; i < enemiesNbrRoll; i++)
        {
            GameObject spawnedEnemy = SpawnEnemyType(EnemyType.Scout);
            SetEnemyTransform(spawnedEnemy);
        }

        SetEnemyTransform(SpawnEnemyType(EnemyType.Frigate));
    }

    void SetEnemyTransform(GameObject enemy)
    {
        enemy.transform.rotation = lookDirection;
        int locationOffset = (int)Random.Range(spawnLocations[1].transform.position.x, spawnLocations[2].transform.position.x);
        enemy.transform.position = spawnLocations[0].transform.position + new Vector3(locationOffset, 0);
    }

    // ECS
    void InitECS()
    {
        // New ECS: Sub Region Array
        EntityUpdateLogic scoutMovements = (entityBody) => { entityBody.transform.position += entityBody.transform.up * scoutSpeed; };
        EntityUpdateLogic frigateMovements = (entityBody) => { entityBody.transform.position += entityBody.transform.up * frigateSpeed; };

        entityTypes = new EntityType[] {
            new EntityType(EnemyType.Scout, 250, scoutPrefab, scoutMovements),
            new EntityType(EnemyType.Frigate, 250, frigatePrefab, frigateMovements)
            };

        // Create Gameobjects Array
        int totalEntity = 0;

        foreach (EntityType entityType in entityTypes)
        {
            totalEntity += entityType.maxEntities;
        }

        enemyGameObjects = new GameObject[totalEntity];
        versions = new int[totalEntity];
        enemyHps = new int[totalEntity];
        maxEnemies = totalEntity;

        int totalEntityCount = 0;

        for (int i = 0; i < entityTypes.Length; i++)
        {
            for (int j = 0; j < entityTypes[i].maxEntities; j++)
            {
                GameObject spawnedPrefab = Instantiate(entityTypes[i].prefab, transform);
                spawnedPrefab.SetActive(false);

                if (spawnedPrefab.TryGetComponent(out Entity entity))
                {
                    entity.index = j;
                }

                enemyGameObjects[totalEntityCount + j] = spawnedPrefab;
            }

            entityTypes[i].startIndex = totalEntityCount;
            totalEntityCount += entityTypes[i].maxEntities;
        }
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
        enemyHps[spawnIndex] = scoutHp;

        if (spawnedEnemy.TryGetComponent(out Entity entity))
        {
            entity.index = spawnIndex;
            entity.version = versions[spawnIndex];
        }

        entityType.activeCount++;

        return spawnedEnemy;
    }

    public void RemoveEnemy(int indexToRemove)
    {
        ref EntityType entityType = ref IndexToEntityType(indexToRemove);

        GameObject removedEnemy = enemyGameObjects[indexToRemove];
        removedEnemy.SetActive(false);

        // Update Version
        versions[indexToRemove]++;

        int lastEntityIndex = entityType.startIndex + entityType.activeCount - 1;

        if (indexToRemove != lastEntityIndex)
        {
            // Last Object -> Removed Index
            GameObject movedEnemy = enemyGameObjects[lastEntityIndex];

            // Swap
            enemyGameObjects[indexToRemove] = movedEnemy;
            enemyGameObjects[lastEntityIndex] = removedEnemy;

            // Update Entity
            if (movedEnemy.TryGetComponent(out Entity entity))
            {
                entity.index = indexToRemove;
                entity.version = versions[indexToRemove];
            }
        }

        entityType.activeCount--;
    }

    void EnemyMovements()
    {
        float horizontalLimit = (Camera.main.orthographicSize * Screen.width / Screen.height) + 5;
        float verticalLimit = Camera.main.orthographicSize + 5;

        foreach (EntityType entityType in entityTypes)
        {
            for (int i = 0; i < entityType.maxEntities; i++)
            {
                int index = entityType.startIndex + i;

                if (enemyGameObjects[index].activeSelf)
                {
                    // Cleanup Enemies out of frame
                    if (ProjectileManager.instance.IsOutOfBond(enemyGameObjects[index].transform.position,
                    horizontalLimit, verticalLimit))
                    {
                        RemoveEnemy(index);
                    }

                    entityType.updateAction(enemyGameObjects[index]);
                }
            }
        }
    }

    public void ApplyDamage(int index, int version, int damage)
    {
        if (versions[index] != version)
        {
            Debug.Log("Wrong Version");
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

public delegate void EntityUpdateLogic(GameObject entityBody);
