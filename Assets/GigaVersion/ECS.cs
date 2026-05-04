using UnityEngine;

// Rosaure ECS
public class ECS<TData, TState> where TState : ECS<TData, TState>.IECSState
{
    public delegate void EntityLogic(ref EntityType type, in int index);
    public delegate void RemoveLogic(in EntityType type, in int index, in int lastIndex);

    // TODO : EntityType = Interface -> ECS Doesnt care about entityData + Optimization
    public struct EntityType
    {
        public EntityType(int type, int maxEntities, GameObject prefab, TData entityData,
        EntityLogic spawnLogic, RemoveLogic removeLogic, EntityLogic updateLogic)
        {
            this.type = type;
            startIndex = 0;
            this.maxEntities = maxEntities;
            activeCount = 0;
            this.prefab = prefab;
            this.entityData = entityData;
            this.spawnLogic = spawnLogic;
            this.removeLogic = removeLogic;
            this.updateLogic = updateLogic;
        }

        public int type;
        public int startIndex;
        public int maxEntities;
        public int activeCount;
        public GameObject prefab;
        public TData entityData;
        public EntityLogic spawnLogic;
        public RemoveLogic removeLogic;
        public EntityLogic updateLogic;

        public override string ToString() => $"(Type = {type}, StartIndex = {startIndex}, MaxEntities = {maxEntities}, ActiveCount = {activeCount})";
    }

    public interface IECSState
    {
        // ECS Core data
        // TODO : Subscribe to systems per Entity Type
        public EntityType[] entityTypes { get; set; }
        public GameObject[] entityGameObjects { get; set; }
        public int[] versions { get; set; }
    }

    public static void Init(TState state, in GameObject holder)
    {
        var entityTypes = state.entityTypes;

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

                state.entityGameObjects[index] = spawnedPrefab;
            }

            entityTypes[i].startIndex = totalEntityCount;
            totalEntityCount += entityTypes[i].maxEntities;
        }
    }

    public static GameObject SpawnEntityType(TState state, in int type)
    {
        ref EntityType entityType = ref state.entityTypes[type];

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

    public static void RemoveEntity(TState state, ref EntityType entityType, int indexToRemove) // ref
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
    public static ref EntityType IndexToEntityType(in TState state, int index)
    {
        var entityTypes = state.entityTypes;

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