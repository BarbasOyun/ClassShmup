using System;
using UnityEngine;

// Rosaure ECS
public class ECS<TData, TState> where TState : IECSState // where T2 : struct, Enum
{
    public readonly struct EntityContext
    {
        public readonly int index;
        public readonly EntityType type;

        public EntityContext(int index, EntityType type)
        {
            this.index = index;
            this.type = type;
        }
    }

    public delegate void EntityLogic(in EntityContext ctx, TState worldState);
    public delegate void RemoveLogic(in EntityContext ctx, TState worldState, in int lastIndex);

    public struct EntityType
    {
        public EntityType(int type, int maxEntities, GameObject prefab, TData entityData,
        EntityLogic spawnLogic, RemoveLogic removeLogic, EntityLogic updateLogic)
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

    public EntityType[] entityTypes;

    // Default Dynamic Arrays -> Length = MaxEntities
    public int[] versions;
    public GameObject[] entityGameObjects;

    public void Init(GameObject holder, EntityType[] entityTypes, int totalEntity)
    {
        this.entityTypes = entityTypes;

        // Create Dynamic Arrays
        entityGameObjects = new GameObject[totalEntity];
        versions = new int[totalEntity];

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

    public GameObject SpawnEntityType(int type, TState state)
    {
        ref EntityType entityType = ref FindEntityType(type);

        if (entityType.activeCount >= entityType.maxEntities)
        {
            Debug.LogWarning($"Max Entities Reached on : {entityType}");
            return null;
        }

        // Spawn Next
        int spawnIndex = entityType.startIndex + entityType.activeCount;
        GameObject spawnedEnemy = entityGameObjects[spawnIndex];
        spawnedEnemy.SetActive(true);

        versions[spawnIndex]++;

        if (entityType.spawnLogic != null)
        {
            entityType.spawnLogic(new EntityContext(spawnIndex, entityType), state); // Additional SpawnLogic
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

    public void RemoveEntity(int indexToRemove, TState state)
    {
        ref EntityType entityType = ref IndexToEntityType(indexToRemove);

        GameObject removedEnemy = entityGameObjects[indexToRemove];
        removedEnemy.SetActive(false);

        // Update Version
        versions[indexToRemove]++;
        entityType.activeCount--;

        int lastEntityIndex = entityType.startIndex + entityType.activeCount;

        // Debug.Log($"REMOVE {entityType.type} Entity at Index = {indexToRemove}, REPLACE Last Index = {lastEntityIndex}");

        if (indexToRemove != lastEntityIndex)
        {
            // Swap GameObjects
            GameObject movedEnemy = entityGameObjects[lastEntityIndex];
            entityGameObjects[lastEntityIndex] = removedEnemy;

            // Move Data : Last Object -> Removed Index
            entityGameObjects[indexToRemove] = movedEnemy;
            entityType.removeLogic(new EntityContext(indexToRemove, entityType), state, lastEntityIndex);

            // Update Entity
            if (movedEnemy.TryGetComponent(out Entity entity))
            {
                entity.index = indexToRemove;
                entity.version = versions[indexToRemove];
            }
        }
    }

    public void EntitiesUpdateLogic(TState state)
    {
        foreach (EntityType entityType in entityTypes)
        {
            for (int i = entityType.startIndex; i < entityType.startIndex + entityType.activeCount; i++)
            {
                entityType.updateLogic(new EntityContext(i, entityType), state);
            }
        }
    }

    // UTILS
    public ref EntityType FindEntityType(int type)
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

    public ref EntityType IndexToEntityType(int index)
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

public interface IECSState
{
    public GameObject[] entityGameObjects {get;}
    // versions
}