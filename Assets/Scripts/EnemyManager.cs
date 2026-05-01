using System;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    // TODO : Use Service Locator
    public static EnemyManager instance;

    public enum EnemyType { Scout, Frigate }
    public enum Capability { Armor = 0, Shield = 1, _Count = 2 }

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
    public class EnemyECS : ECS<UnitData, SystemState> { }
    public EnemyECS ecs;
    public delegate int DamageModifier(EnemyECS.EntityContext ctx, SystemState state, int dmg);

    // Old
    delegate void EntityHandler(EntityContext ctx);
    delegate void TakeDamageLogic(EntityContext ctx, int dmg);

    readonly struct EntityContext
    {
        public readonly int index;
        public readonly EntityType type;

        public EntityContext(int index, EntityType type)
        {
            this.index = index;
            this.type = type;
        }
    }

    struct EntityType
    {
        public EntityType(EnemyType type, int maxEntities, GameObject prefab, UnitData unitData, EntityHandler spawnLogic, EntityHandler updateLogic, TakeDamageLogic takeDamageLogic)
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
        public EntityHandler spawnLogic;
        public EntityHandler updateLogic;
        public TakeDamageLogic takeDamageLogic;

        public override string ToString() => $"(Type = {type}, StartIndex = {startIndex}, MaxEntities = {maxEntities}, ActiveCount = {activeCount})";
    }

    EnemyDataRegistry enemyDataRegistry;
    EnemyLogicRegistry enemyLogicRegistry;
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

        // InitECS();
        InitEntityType();
    }

    void Start()
    {
        InvokeRepeating(nameof(SpawnEnemies), 1.0f, spawnDelta);
    }

    void FixedUpdate()
    {
        // EntitiesUpdateLogic();

        ecs.EntitiesUpdateLogic(CaptureState());
    }

    void Update()
    {

    }

    // ENEMY SPAWNER
    void SpawnEnemies()
    {
        // Spawn Scout
        int enemiesNbrRoll = UnityEngine.Random.Range(minEnemies, minEnemies + enemiesRange);
        SystemState state = CaptureState();

        for (int i = 0; i < enemiesNbrRoll; i++)
        {
            // GameObject spawnedEnemy = SpawnEnemyType(EnemyType.Scout);
            GameObject spawnedEnemy = ecs.SpawnEntityType(0, state);

            if (!spawnedEnemy) return;

            SetEnemyTransform(spawnedEnemy);
        }

        // Spawn Frigate
        // SetEnemyTransform(SpawnEnemyType(EnemyType.Frigate));
        SetEnemyTransform(ecs.SpawnEntityType((int)EnemyType.Frigate, state));
    }

    void SetEnemyTransform(GameObject enemy)
    {
        enemy.transform.rotation = lookDirection;
        int locationOffset = (int)UnityEngine.Random.Range(spawnLocations[1].transform.position.x, spawnLocations[2].transform.position.x);
        enemy.transform.position = spawnLocations[0].transform.position + new Vector3(locationOffset, 0);
    }

    // ECS Implementation
    void InitECS()
    {
        int[] additionalData = null; // InitEntityType();

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

    void InitEntityType() //int[]
    {
        // Bake Entity Types
        // ENTITY TYPE > LOGIC DEFINITION
        // + Where to get Data

        enemyDataRegistry = new EnemyDataRegistry(
            armorDataCount: 1,
            shieldDataCount: 1
            );
        enemyLogicRegistry = new EnemyLogicRegistry();

        int maxScout = 300;
        int maxFrigate = 250;
        int totalEntity = maxScout + maxFrigate;

        // CREATE Types
        // entityTypes = new[] {
        //     new EntityType(EnemyType.Scout, 250, scoutPrefab, scoutData, null, scoutUpdate, scoutDamageLogic),
        //     new EntityType(EnemyType.Frigate, maxFrigate, frigatePrefab, frigateData, frigateSpawn, frigateUpdate, frigateDamageLogic)
        //     };

        EnemyECS.EntityType[] entityTypes = new[] {
            ScoutType(maxScout),
            FrigateType(maxFrigate),
            };

        // return new int[] { maxFrigate }; // Return Data to Create Dense Arrays

        enemyHps = new int[totalEntity];
        enemyShields = new int[maxFrigate];

        ecs = new EnemyECS();
        ecs.Init(gameObject, entityTypes, totalEntity);
    }

    EnemyECS.EntityType ScoutType(int maxScout)
    {
        UnitData scoutData = new UnitData(scoutHp, scoutDamage, scoutSpeed);

        // EnemyECS.EntityLogic scoutUpdate = (ctx) => StraightMovement(enemyGameObjects[ctx.index], ctx.type.entityData.speed);
        EnemyECS.EntityLogic scoutUpdate = Straight;
        // TakeDamageLogic scoutDamageLogic = (ctx, dmg) => enemyHps[ctx.index] -= dmg;

        // return new EntityType(EnemyType.Scout, 250, scoutPrefab, scoutData, null, scoutUpdate, scoutDamageLogic);
        return new EnemyECS.EntityType(0, maxScout, scoutPrefab, scoutData, CommonSpawnLogic, CommonRemoveLogic, scoutUpdate);
    }

    EnemyECS.EntityType FrigateType(int maxFrigate)
    {
        enemyDataRegistry.armorData[0] = new ArmorData(frigateArmor); // Register Additional Data (Armor)
        enemyDataRegistry.shieldData[0] = new ShieldData(frigateShield);
        UnitData frigateData = new UnitData(frigateHp, frigateDamage, frigateSpeed, new int[] { 0, 0 }); // First element = index for Registered Armor

        // EnemyECS.EntityLogic frigateSpawn = (ctx) => enemyShields[ctx.index - ctx.type.startIndex] = enemyDataRegistry.shieldData[frigateData.additionalData[1]].shield;
        EnemyECS.EntityLogic frigateSpawn = SetShield; // Events System?
        EnemyECS.EntityLogic frigateUpdate = Oscilating;
        // TakeDamageLogic frigateDamageLogic = (ctx, dmg) =>
        // {
        //     int shield = enemyShields[ctx.index - ctx.type.startIndex];
        //     int armor = enemyDataRegistry.armorData[frigateData.additionalData[0]].armor;
        //     ArmorAndShield(ref enemyHps[ctx.index], armor, ref shield, dmg);
        // };

        // return new EntityType(EnemyType.Frigate, maxFrigate, frigatePrefab, frigateData, frigateSpawn, frigateUpdate, frigateDamageLogic);
        return new EnemyECS.EntityType(1, maxFrigate, frigatePrefab, frigateData, frigateSpawn, CommonRemoveLogic, frigateUpdate);
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
            entityType.spawnLogic(new EntityContext(spawnIndex, entityType)); // Additional SpawnLogic
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

                entityType.updateLogic(new EntityContext(i, entityType));
            }
        }
    }

    public void ApplyDamage(int index, int version, int dmg)
    {
        // if (versions[index] != version)
        // {
        //     Debug.LogWarning("Wrong Version");
        //     return;
        // }

        // EntityType entityType = IndexToEntityType(index);
        // entityType.takeDamageLogic(new EntityContext(index, entityType), dmg);

        // if (enemyHps[index] <= 0)
        // {
        //     RemoveEnemy(index);
        // }

        SystemState state = CaptureState();

        if (state.versions[index] != version)
        {
            Debug.LogWarning("Wrong Version");
            return;
        }

        EnemyECS.EntityType entityType = ecs.IndexToEntityType(index);
        int resultDmg = enemyLogicRegistry.takeDamageLogics[entityType.type](new EnemyECS.EntityContext(index, entityType), state, dmg);
        state.enemyHps[index] -= resultDmg;

        if (state.enemyHps[index] <= 0)
        {
            ecs.RemoveEntity(index, state);
        }
    }

    // UTILS
    SystemState CaptureState()
    {
        return new SystemState(ecs.entityGameObjects, ecs.versions, enemyHps, enemyShields, enemyDataRegistry);
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

    // ENTITY WRAPPER
    public void Straight(in EnemyECS.EntityContext ctx, SystemState state)
    {
        StraightMovement(state.entityGameObjects[ctx.index], ctx.type.entityData.speed); // Movement Interface?
    }

    public void Oscilating(in EnemyECS.EntityContext ctx, SystemState state)
    {
        OscilatingMovement(state.entityGameObjects[ctx.index], ctx.type.entityData.speed);
    }

    public void SetShield(in EnemyECS.EntityContext ctx, SystemState state)
    {
        state.enemyShields[ctx.index - ctx.type.startIndex] = state.dataRegistry.shieldData[ctx.type.entityData.additionalData[1]].shield; // ctx.type.frigateData.additionalData[1]
    }

    public void CommonSpawnLogic(in EnemyECS.EntityContext ctx, SystemState state)
    {
        state.enemyHps[ctx.index] = ctx.type.entityData.hp;
    }

    public void CommonRemoveLogic(in EnemyECS.EntityContext ctx, SystemState state, in int originalIndex)
    {
        state.enemyHps[ctx.index] = state.enemyHps[originalIndex];
    }
}

public readonly struct SystemState : IECSState
{
    public SystemState(GameObject[] entityGameObjects, int[] versions, int[] enemyHps, int[] enemyShields, EnemyDataRegistry dataRegistry)
    {
        this.entityGameObjects = entityGameObjects;
        this.versions = versions;
        this.enemyHps = enemyHps;
        this.enemyShields = enemyShields;
        this.dataRegistry = dataRegistry;
    }

    public readonly GameObject[] entityGameObjects { get; }
    public readonly int[] versions;
    public readonly int[] enemyHps;
    public readonly int[] enemyShields;
    public readonly EnemyDataRegistry dataRegistry;
}

// ENTITY TYPE > DATA DEFINITION
public struct UnitData
{
    public UnitData(int hp, int damage, float speed, int[] additionalData = null)
    {
        this.hp = hp;
        this.damage = damage;
        this.speed = speed;
        this.additionalData = additionalData;

        CapabilityMask = (1 << (int)EnemyManager.Capability.Shield) | (1 << (int)EnemyManager.Capability.Armor);
        DataOffsets = new int[(int)EnemyManager.Capability._Count];
    }

    // public readonly int maxHp;
    public readonly int hp;
    public readonly int damage;
    public readonly float speed;
    public readonly int[] additionalData; // Sparse?

    public readonly int CapabilityMask; // Bitmask
    public readonly int[] DataOffsets;
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

public class EnemyLogicRegistry
{
    public readonly EnemyManager.DamageModifier[] takeDamageLogics;

    int TakeDamage(EnemyManager.EnemyECS.EntityContext ctx, SystemState state, int dmg)
    {
        int finalDamage = dmg;
        var entityData = ctx.type.entityData;

        if ((entityData.CapabilityMask & (1 << (int)EnemyManager.Capability.Shield)) != 0)
        {
            // 2. Only if the bit is on, look up the specific data offset
            int dataIdx = entityData.DataOffsets[(int)EnemyManager.Capability.Shield];

            // 3. Access the dense data registry
            finalDamage = ShieldLogic(finalDamage, ref state.enemyShields[dataIdx + ctx.index]);
        }

        // Armor

        return finalDamage;
        // return dmg;
    }

    int ShieldLogic(int dmg, ref int shield)
    {
        int mitigatedDamage = dmg - shield;

        int delta = shield - mitigatedDamage;
        shield = Math.Min(delta, 0);
        mitigatedDamage = Math.Sign(delta) == -1 ? Math.Abs(delta) : 0;
        // Debug.Log($"{shield} SHIELD -> DMG = {dmg} -> {mitigatedDamage}");
        return mitigatedDamage;
    }

    // int ShieldLogic(EnemyManager.EnemyECS.EntityContext ctx, SystemState state, int dmg)
    // {
    //     ref int shield = ref state.enemyShields[ctx.index - ctx.type.startIndex];
    //     int mitigatedDamage = dmg - shield;

    //     int delta = shield - mitigatedDamage;
    //     shield = Math.Min(delta, 0);
    //     mitigatedDamage = Math.Sign(delta) == -1 ? Math.Abs(delta) : 0;
    //     // Debug.Log($"{shield} SHIELD -> DMG = {dmg} -> {mitigatedDamage}");
    //     return mitigatedDamage;
    // }

    int ArmorLogic(EnemyManager.EnemyECS.EntityContext ctx, SystemState state, int dmg)
    {
        ref int armor = ref state.dataRegistry.armorData[ctx.type.type].armor;
        int mitigatedDamage = dmg - armor;
        // Debug.Log($"{armor} ARMOR -> DMG = {dmg} -> {mitigatedDamage}");
        return mitigatedDamage;
    }

    public EnemyLogicRegistry()
    {
        takeDamageLogics = new EnemyManager.DamageModifier[]
        {
            TakeDamage,
            TakeDamage,
        };
    }
}