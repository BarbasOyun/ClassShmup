using UnityEngine;

// Rosaure ECS
public class ECS<T1> : MonoBehaviour // where T2 : struct, Enum
{
    delegate void EntityHandler(in EntityContext ctx);

    delegate void TakeDamageLogic(in EntityContext ctx, in int dmg);
    delegate void RemoveLogic(in EntityContext ctx, in int lastIndex);

    readonly struct EntityContext
    {
        public readonly int index;
        readonly EntityType type;

        public EntityContext(int index, EntityType type)
        {
            this.index = index;
            this.type = type;
        }
    }

    struct EntityType
    {
        public EntityType(int type, int maxEntities, GameObject prefab, T1 entityData,
        EntityHandler spawnLogic, RemoveLogic removeLogic, EntityHandler updateLogic, TakeDamageLogic takeDamageLogic)
        {
            this.type = type;
            // typeAsInt = System.Runtime.CompilerServices.Unsafe.As<T2, int>(ref type);
            startIndex = 0;
            this.maxEntities = maxEntities;
            activeCount = 0;
            this.prefab = prefab;
            this.entityData = entityData;
            this.spawnLogic = spawnLogic;
            this.removeLogic = removeLogic;
            this.updateLogic = updateLogic;
            this.takeDamageLogic = takeDamageLogic;
        }

        public int type;
        public int startIndex;
        public int maxEntities;
        public int activeCount;
        public GameObject prefab;
        public T1 entityData;
        public EntityHandler spawnLogic;
        public RemoveLogic removeLogic;
        public EntityHandler updateLogic;
        public TakeDamageLogic takeDamageLogic;

        public override string ToString() => $"(Type = {type}, StartIndex = {startIndex}, MaxEntities = {maxEntities}, ActiveCount = {activeCount})";
    }

    EntityType[] entityTypes;

    // Default Dynamic Arrays -> Length = MaxEntities
    int[] versions;
    GameObject[] enemyGameObjects;

    void Start()
    {

    }

    void Update()
    {

    }

    void InitECS()
    {
        // Create Dynamic Arrays
        int totalEntity = 0;

        foreach (EntityType entityType in entityTypes)
        {
            totalEntity += entityType.maxEntities;
        }

        enemyGameObjects = new GameObject[totalEntity];
        versions = new int[totalEntity];
        // Init Additional

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

    public GameObject SpawnEntityType(int type)
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

    public void RemoveEntity(int indexToRemove)
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
            entityType.removeLogic(new EntityContext(indexToRemove, entityType), lastEntityIndex);
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
        foreach (EntityType entityType in entityTypes)
        {
            for (int i = entityType.startIndex; i < entityType.startIndex + entityType.activeCount; i++)
            {
                entityType.updateLogic(new EntityContext(i, entityType));
            }
        }
    }

    // UTILS
    ref EntityType FindEntityType(int type)
    {
        int typeIndex = 0;
        EntityType currentType = entityTypes[typeIndex];

        while (currentType.type != type)// (EqualityComparer<T2>.Default.Equals(currentType.type, type)) // (currentType.type != type)
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
}