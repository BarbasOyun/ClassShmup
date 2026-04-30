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
    public int frigateArmor = 1;
    public int frigateShield = 10;
    public int frigateDamage = 20;
    public float frigateSpeed = 0.05f;

    // [Header("ECS")]
    delegate void UpdateLogic(int index, EntityType entityType);
    delegate void TakeDamageLogic(int index, EntityType entityType, int dmg);
    delegate void SpawnLogic(int index, EntityType entityType);

    struct EntityType
    {
        public EntityType(EnemyType type, int maxEntities, GameObject prefab, UnitData unitData, SpawnLogic spawnLogic, UpdateLogic updateLogic, TakeDamageLogic takeDamageLogic)
        {
            this.type = type;
            typeAsInt = (int)type;
            startIndex = 0;
            this.maxEntities = maxEntities;
            activeCount = 0;
            this.prefab = prefab;
            this.unitData = unitData;
            this.spawnLogic = spawnLogic;
            this.updateLogic = updateLogic;
            this.takeDamageLogic = takeDamageLogic;
        }

        public EnemyType type;
        public int typeAsInt;
        public int startIndex;
        public int maxEntities;
        public int activeCount;
        public GameObject prefab;
        public UnitData unitData;
        public SpawnLogic spawnLogic;
        public UpdateLogic updateLogic;
        public TakeDamageLogic takeDamageLogic;

        public override string ToString() => $"(Type = {type}, StartIndex = {startIndex}, MaxEntities = {maxEntities}, ActiveCount = {activeCount})";
    }

    EnemyDataRegistry enemyDataRegistry;
    EntityType[] entityTypes;

    // Default Dynamic Arrays -> Length = MaxEntities
    int[] versions;
    GameObject[] enemyGameObjects;
    int[] enemyHps;

    // Type Dynamic Arrays -> Length Vary
    // Use Sparse Set/Array for Data in Update Or
    // Use IndexDelta -> Index - EntityType.StartIndex
    // Types have to be organised e.g. Shield1, Shield2, Shield3 + Armor1, Armor2 + Movement1
    int[] enemyShields;
    // Modifiers
    // StateMachines

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

    // Rosaure ECS
    void InitECS()
    {
        int[] additionalData = InitEntityType();

        // Create Dynamic Arrays
        int totalEntity = 0;

        foreach (EntityType entityType in entityTypes)
        {
            totalEntity += entityType.maxEntities;
        }

        enemyGameObjects = new GameObject[totalEntity];
        versions = new int[totalEntity];
        enemyHps = new int[totalEntity];
        enemyShields = new int[additionalData[0]];

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

    int[] InitEntityType()
    {
        // Bake Entity Types
        // ENTITY TYPE > LOGIC DEFINITION
        // + Where to get Data
        enemyDataRegistry = new EnemyDataRegistry(
            armorDataCount: 1,
            shieldDataCount: 1
            );

        // SCOUT Type
        UnitData scoutData = new UnitData(scoutHp, scoutDamage, scoutSpeed, null);

        UpdateLogic scoutUpdate = (index, entityType) => StraightMovement(enemyGameObjects[index], entityType.unitData.speed);
        TakeDamageLogic scoutDamageLogic = (index, entityType, dmg) => enemyHps[index] -= dmg;

        // FRIGATE Type
        int maxFrigate = 250;

        enemyDataRegistry.armorData[0] = new ArmorData(frigateArmor); // Register Additional Data (Armor)
        enemyDataRegistry.shieldData[0] = new ShieldData(frigateShield);
        UnitData frigateData = new UnitData(frigateHp, frigateDamage, frigateSpeed, new int[] { 0, 0 }); // First element = index for Registered Armor

        SpawnLogic frigateSpawn = (index, entityType) => enemyShields[index - entityType.startIndex] = enemyDataRegistry.shieldData[frigateData.additionalData[1]].shield;
        UpdateLogic frigateUpdate = (index, entityType) => OscilatingMovement(enemyGameObjects[index], entityType.unitData.speed);
        TakeDamageLogic frigateDamageLogic = (index, entityType, dmg) =>
        {
            int shield = enemyShields[index - entityType.startIndex];
            ArmorAndShield(ref enemyHps[index], enemyDataRegistry.armorData[frigateData.additionalData[0]].armor, ref shield, dmg);
        };

        // CREATE Types
        entityTypes = new[] {
            new EntityType(EnemyType.Scout, 250, scoutPrefab, scoutData, null, scoutUpdate, scoutDamageLogic),
            new EntityType(EnemyType.Frigate, maxFrigate, frigatePrefab, frigateData, frigateSpawn, frigateUpdate, frigateDamageLogic)
            };

        return new int[] { maxFrigate }; // Return Data to Create Dense Arrays
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

        if (entityType.spawnLogic != null)
        {
            entityType.spawnLogic(spawnIndex, entityType); // Additional SpawnLogic
        }

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
            for (int i = entityType.startIndex; i < entityType.startIndex + entityType.activeCount; i++)
            {
                // Cleanup Enemies out of frame
                if (ProjectileManager.instance.IsOutOfBond(enemyGameObjects[i].transform.position,
                horizontalLimit, verticalLimit))
                {
                    // Remove Life
                    RemoveEnemy(i);
                }

                entityType.updateLogic(i, entityType);
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

        EntityType entityType = IndexToEntityType(index);
        entityType.takeDamageLogic(index, entityType, damage);

        if (enemyHps[index] <= 0)
        {
            RemoveEnemy(index);
        }
    }

    // UTILS
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

    // ENEMY LOGIC
    void ArmorAndShield(ref int hp, int armor, ref int shield, int dmg)
    {
        hp -= ArmorLogic(armor, ShieldLogic(ref shield, dmg));
    }

    int ArmorLogic(int armor, int dmg)
    {
        int mitigatedDamage = dmg - armor;
        // Debug.Log($"{armor} ARMOR -> DMG = {dmg} -> {mitigatedDamage}");
        return mitigatedDamage;
    }

    int ShieldLogic(ref int shield, int dmg)
    {
        int mitigatedDamage = dmg - shield;

        int delta = shield - mitigatedDamage;
        shield = Math.Min(delta, 0);
        mitigatedDamage = Math.Sign(delta) == -1 ? Math.Abs(delta) : 0;
        // Debug.Log($"{shield} SHIELD -> DMG = {dmg} -> {mitigatedDamage}");
        return mitigatedDamage;
    }

    void StraightMovement(GameObject entityBody, float speed)
    {
        entityBody.transform.position += entityBody.transform.up * speed;
    }

    void OscilatingMovement(GameObject entityBody, float speed)
    {
        entityBody.transform.position += (entityBody.transform.right * (float)Math.Sin(Time.time) + entityBody.transform.up) * speed;
    }
}

// ENTITY TYPE > DATA DEFINITION
public struct UnitData
{
    public UnitData(int hp, int damage, float speed, int[] additionalData)
    {
        this.hp = hp;
        this.damage = damage;
        this.speed = speed;
        this.additionalData = additionalData;
    }

    // public readonly int maxHp;
    public readonly int hp;
    public readonly int damage;
    public readonly float speed;
    public readonly int[] additionalData;
}

public struct ArmorData // Armor reduce flat Dmg
{
    public ArmorData(int armor)
    {
        this.armor = armor;
    }

    public int armor;
    // public int increasedArmor;
}

public struct ShieldData // Additional HP not affected by Armor
{
    public ShieldData(int shield)
    {
        this.shield = shield;
    }

    // public int maxShield;
    public int shield;
    // Recovery rate
}

public class EnemyDataRegistry
{
    public readonly ArmorData[] armorData;
    public readonly ShieldData[] shieldData;

    public EnemyDataRegistry(int armorDataCount, int shieldDataCount)
    {
        armorData = new ArmorData[armorDataCount];
        shieldData = new ShieldData[shieldDataCount];
    }
}
