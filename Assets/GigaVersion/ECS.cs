using UnityEngine;

// Rosaure ECS
public class ECS<TState, TData> where TState : ECS<TState, TData>.IECSState where TData : ECS<TState, TData>.IEntityType
{
    public interface IECSState
    {
        // ECS Core data
        // TODO : Subscribe to systems per Entity Type
        public TData[] entityTypes { get; set; }
        public GameObject[] entityGameObjects { get; set; }
        public int[] versions { get; set; }
    }

    public delegate void EntityLogic(ref TData type, in int index);
    public delegate void RemoveLogic(in TData type, in int index, in int lastIndex);

    public interface IEntityType
    {
        public int type { get; }
        public int startIndex { get; set; }
        public int maxEntities { get; }
        public int activeCount { get; set; }
        // TODO : use GPU Instancing -> TryGetComponent has delay
        public GameObject prefab { get; }
        public EntityLogic spawnLogic { get; }
        public RemoveLogic removeLogic { get; }
        public EntityLogic updateLogic { get; }
    }

    public static void Init(TState state, in GameObject holder)
    {
        var entityTypes = state.entityTypes;
        var entityGameObjects = state.entityGameObjects;

        // Instantiate/Setup GameObjects
        int totalEntityCount = 0;

        for (int i = 0; i < entityTypes.Length; i++)
        {
            for (int j = 0; j < entityTypes[i].maxEntities; j++)
            {
                GameObject spawnedPrefab = UnityEngine.Object.Instantiate(entityTypes[i].prefab, holder.transform);
                spawnedPrefab.SetActive(false);

                int index = totalEntityCount + j;

                if (spawnedPrefab.TryGetComponent(out Entity entity))
                {
                    entity.index = index;
                }

                entityGameObjects[index] = spawnedPrefab;
            }

            entityTypes[i].startIndex = totalEntityCount;
            totalEntityCount += entityTypes[i].maxEntities;
        }
    }

    public static GameObject SpawnEntityType(TState state, in int type)
    {
        ref TData entityType = ref state.entityTypes[type];

        if (entityType.activeCount >= entityType.maxEntities)
        {
            Debug.LogWarning($"Max Entities Reached on : {entityType}");
            return null;
        }

        // Spawn Next
        int spawnIndex = entityType.startIndex + entityType.activeCount;
        GameObject spawnedEnemy = state.entityGameObjects[spawnIndex];
        spawnedEnemy.SetActive(true);

        // Custom Spawn Logic
        entityType.spawnLogic(ref entityType, spawnIndex);

        if (spawnedEnemy.TryGetComponent(out Entity entity))
        {
            entity.index = spawnIndex;
            entity.version = state.versions[spawnIndex];
        }

        entityType.activeCount++;

        // Debug.Log($"Spawned {entityType.type} HP = {enemyHps[spawnIndex]}");

        return spawnedEnemy;
    }

    public static void RemoveEntity(TState state, ref TData entityType, int indexToRemove)
    {
        GameObject[] entityGameObjects = state.entityGameObjects;

        GameObject removedEnemy = entityGameObjects[indexToRemove];
        removedEnemy.SetActive(false);

        // Update Version
        state.versions[indexToRemove]++;
        entityType.activeCount--;

        int lastEntityIndex = entityType.startIndex + entityType.activeCount;

        // if (indexToRemove > lastEntityIndex)
        // {
        //     Debug.LogError($"REMOVE Entity type = {entityType} at Index = {indexToRemove}, REPLACE Last Index = {lastEntityIndex}");
        // }

        if (indexToRemove != lastEntityIndex)
        {
            // Swap GameObjects
            GameObject movedEnemy = entityGameObjects[lastEntityIndex];
            entityGameObjects[lastEntityIndex] = removedEnemy;

            // Move Data : Last Object -> Removed Index
            entityGameObjects[indexToRemove] = movedEnemy;

            // Custom Remove Logic
            entityType.removeLogic(in entityType, indexToRemove, lastEntityIndex);

            // Update Entity
            if (movedEnemy.TryGetComponent(out Entity entity))
            {
                entity.index = indexToRemove;
                entity.version = state.versions[indexToRemove];
            }
        }
    }

    public static void RemoveAll(TState state)
    {
        GameObject[] entityGameObjects = state.entityGameObjects;
        var versions = state.versions;

        for (int i = 0; i < state.entityTypes.Length; i++)
        {
            ref var entityType = ref state.entityTypes[i];
            int typeEndIndex = entityType.startIndex + entityType.activeCount;

            for (int j = entityType.startIndex; j < typeEndIndex; j++)
            {
                entityGameObjects[j].SetActive(false);
                versions[j]++;
            }

            entityType.activeCount = 0;
        }
    }

    public static void EntitiesUpdateLogic(TState state)
    {
        for (int i = 0; i < state.entityTypes.Length; i++)
        {
            ref var entityType = ref state.entityTypes[i];

            for (int j = entityType.startIndex; j < entityType.startIndex + entityType.activeCount; j++)
            {
                entityType.updateLogic(ref entityType, j);
            }
        }
    }

    // UTILS
    public static ref TData IndexToEntityType(in TState state, int index)
    {
        var entityTypes = state.entityTypes;

        int typeIndex = 0;
        TData currentType = entityTypes[typeIndex];

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