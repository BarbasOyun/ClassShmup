using System;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;

public class EnemyECS : ECS<EnemyManager, UnitData> { }

public class EnemyManager : MonoBehaviour, EnemyECS.IECSState
{
    // TODO : Use Service Locator
    // TODO : use partial class
    // TODO : Custom Physics : Velocity, Collision Detection, Solver
    public static EnemyManager instance;

    [Header("ENEMY SPAWNER")]
    public GameObject[] spawnLocations; // 3
    public float spawnDelta = 1f;
    public Vector2 enemiesDirection;
    Quaternion lookDirection;
    public AudioSource explosionSound;
    public TextMeshProUGUI scoreText;
    public long score;

    public float verticalLimit;
    public float horizontalLimit;

    // DATA SOURCE
    // TODO : Game Engine Agnostic = Move to file, Unity = ScriptableObject
    [Header("ENEMY - Scout")]
    public GameObject scoutPrefab;
    public int maxScout = 500;
    public int minScout = 10;
    public int baseMinScout;
    public int scoutSpawnRange = 10;
    public int baseScoutSpawnRange;
    public int scoutScore = 1;
    public int scoutHp = 10;
    public int scoutDamage = 10;
    public float scoutSpeed = 0.1f;

    [Header("ENEMY - Frigate")]
    public GameObject frigatePrefab;
    public int maxFrigate = 200;
    public int minFrigate = 1;
    public int baseMinFrigate;
    public int frigateSpawnRange = 1;
    public int baseFrigateSpawnRange;
    public int frigateScore = 3;
    public int frigateHp = 30;
    public int frigateArmor = 1;
    public int frigateShield = 10;
    public int frigateDamage = 20;
    public float frigateSpeed = 0.05f;

    // [Header("ECS")]
    public enum EnemyType { Scout, Frigate, _Count }
    public enum Capability
    {
        Armor,
        Shield,
        _Count,
    }

    // IECSState Implementation
    public UnitData[] entityTypes { get; set; }
    public GameObject[] entityGameObjects { get; set; }
    public int[] versions { get; set; }

    // Custom fields
    public int[] enemyHps;
    // Entity Status (Timer)

    // Type arrays -> Length Vary
    public int[] enemyShields;
    // StateMachines

    public EnemyDataRegistry dataRegistry;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }else {
            instance.scoreText = scoreText;
            Destroy(gameObject);
        }

        float angle = Mathf.Atan2(enemiesDirection.y, enemiesDirection.x) * Mathf.Rad2Deg - 90;
        lookDirection = Quaternion.Euler(0, 0, angle);

        baseMinScout = minScout;
        baseScoutSpawnRange = scoutSpawnRange;
        baseMinFrigate = minFrigate;
        baseFrigateSpawnRange = frigateSpawnRange;

        InitEntityType();
    }

    void Start()
    {
        InvokeRepeating(nameof(SpawnEnemies), 1f, spawnDelta);
        InvokeRepeating(nameof(IncrementEnemies), 1f, 15f);
    }

    void FixedUpdate()
    {
        verticalLimit = Camera.main.orthographicSize;
        horizontalLimit = Camera.main.orthographicSize * Screen.width / Screen.height;

        EnemyECS.EntitiesUpdateLogic(this);
    }

    // void Update()
    // {

    // }

    // GLOBAL BEHAVIORS
    public static void StraightMovement(GameObject entityBody, float speed)
    {
        entityBody.transform.position += entityBody.transform.up * speed;
    }

    public static void OscilatingMovement(GameObject entityBody, float speed)
    {
        entityBody.transform.position += (entityBody.transform.right * (float)Math.Sin(Time.time) + entityBody.transform.up) * speed;
    }

    // ENEMY SPAWNER
    void SpawnEnemies()
    {
        // Spawn Scout
        int scoutsRoll = UnityEngine.Random.Range(minScout, minScout + scoutSpawnRange);

        for (int i = 0; i < scoutsRoll; i++)
        {
            SetEnemyTransform(EnemyECS.SpawnEntityType(this, (int)EnemyType.Scout));
        }

        // Spawn Frigate
        int frigatesRoll = UnityEngine.Random.Range(minFrigate, minFrigate + frigateSpawnRange);

        for (int i = 0; i < frigatesRoll; i++)
        {
            SetEnemyTransform(EnemyECS.SpawnEntityType(this, (int)EnemyType.Frigate));
        }
    }

    void IncrementEnemies()
    {
        // Zoom out
        minScout += 3;
        scoutSpawnRange += 4;
        minFrigate += 1;
        frigateSpawnRange += 2;
    }

    public void ResetIncrement()
    {
        minScout = baseMinScout;
        scoutSpawnRange = baseScoutSpawnRange;
        minFrigate = baseMinFrigate;
        frigateSpawnRange = baseFrigateSpawnRange;
    }

    void SetEnemyTransform(GameObject enemy)
    {
        if (!enemy) return;

        float xOffset = UnityEngine.Random.Range(spawnLocations[1].transform.position.x, spawnLocations[2].transform.position.x);
        float yOffset = UnityEngine.Random.Range(0, 2);
        enemy.transform.position = spawnLocations[0].transform.position + new Vector3(xOffset, yOffset);
        enemy.transform.rotation = lookDirection;
    }

    // ECS Implementation
    void InitEntityType()
    {
        // TODO : int totalShieldType = iterate over entityTypes
        // List<EnemyECS.EntityType> typeList = new List<ECS<UnitData, EnemySystemState>.EntityType>();

        var dataRegistry = new EnemyDataRegistry(
            armorDataCount: 1,
            shieldDataCount: 1
        );

        int[] typeCount = new int[(int)EnemyType._Count];
        typeCount[(int)EnemyType.Scout] = maxScout;
        typeCount[(int)EnemyType.Frigate] = maxFrigate;
        int totalEntity = typeCount.Sum();

        // CREATE Types
        entityTypes = new[] {
            ScoutType(typeCount[(int)EnemyType.Scout]),
            FrigateType(typeCount, dataRegistry),
            };

        // CREATE Runtime arrays
        this.dataRegistry = dataRegistry;
        entityGameObjects = new GameObject[totalEntity];
        versions = new int[totalEntity];
        enemyHps = new int[totalEntity];
        enemyShields = new int[typeCount[(int)EnemyType.Frigate]];

        EnemyECS.Init(this, gameObject);
    }

    UnitData ScoutType(int maxScout)
    {
        // Composition
        void ScoutUpdate(ref UnitData type, in int index)
        {
            RemoveOutOfBound(ref type, index);
            Straight(in type, in index);
        }

        return new UnitData(
            (int)EnemyType.Scout,
            maxScout,
            scoutPrefab,
            SpawnLogic,
            RemoveLogic,
            ScoutUpdate,
            scoutScore,
            scoutHp,
            scoutDamage,
            scoutSpeed
            );
    }

    UnitData FrigateType(int[] typeCount, EnemyDataRegistry dataRegistry)
    {
        void FrigateUpdate(ref UnitData type, in int index)
        {
            RemoveOutOfBound(ref type, index);
            Oscilating(in type, in index);
        }

        int maxFrigate = typeCount[(int)EnemyType.Frigate];

        // TODO : Count types without armor/shield

        // Register Additional Data (Armor + Shield)
        // TODO : index = typeIndex - noShieldType
        dataRegistry.armorData[0] = new ArmorData(frigateArmor);
        dataRegistry.shieldData[0] = new ShieldData(frigateShield);

        int capabilities = (1 << (int)Capability.Armor) | (1 << (int)Capability.Shield); // Shield and Armor

        UnitData frigateData = new UnitData(
            (int)EnemyType.Frigate,
            maxFrigate,
            frigatePrefab,
            SpawnLogic,
            RemoveLogic,
            FrigateUpdate,
            frigateScore,
            frigateHp,
            frigateDamage,
            frigateSpeed,
            capabilities
        ); // First element = index for Registered Armor

        // Authoring Data
        frigateData.dataIndex[(int)Capability.Armor] = 0;
        frigateData.dataIndex[(int)Capability.Shield] = 0; // typeIndex - noShieldType

        // Runtime Data
        // frigateData.dataOffsets[(int)Capability.Armor] = typeCount[(int)EnemyType.Scout]; // Later use Runtime armor
        frigateData.dataOffsets[(int)Capability.Shield] = typeCount[(int)EnemyType.Scout];

        return frigateData;
    }

    public void ApplyDamage(in int index, in int version, in int dmg)
    {
        if (versions[index] != version)
        {
            Debug.LogWarning("Wrong Version");
            return;
        }

        ref UnitData entityType = ref EnemyECS.IndexToEntityType(this, index);
        enemyHps[index] -= DamageModifiers(entityType, index, dmg);

        if (enemyHps[index] <= 0)
        {
            KillEnemy(index, ref entityType);
        }
    }

    void KillEnemy(in int index, ref UnitData entityType)
    {
        explosionSound.Stop();
        explosionSound.Play();
        score += entityType.scoreValue;
        scoreText.SetText("Score : {0}", score);
        EnemyECS.RemoveEntity(this, ref entityType, index);
        // Explosion Visual
    }

    public void RemoveAll()
    {
        EnemyECS.RemoveAll(this);
    }

    // ENEMY LOGIC
    public void SpawnLogic(ref UnitData type, in int index)
    {
        enemyHps[index] = type.hp;

        // Shield -> Set Runtime array using Authoring array
        if ((type.capabilityMask & (1 << (int)Capability.Shield)) != 0)
        {
            int i = type.dataIndex[(int)Capability.Shield];
            int offset = type.dataOffsets[(int)Capability.Shield];
            enemyShields[index - offset] = dataRegistry.shieldData[i].shield;
        }
    }

    public void RemoveLogic(in UnitData type, in int index, in int originalIndex)
    {
        enemyHps[index] = enemyHps[originalIndex];

        // Shield
        if ((type.capabilityMask & (1 << (int)Capability.Shield)) != 0)
        {
            int offset = type.dataOffsets[(int)Capability.Shield];
            enemyShields[index - offset] = enemyShields[originalIndex - offset];
        }
    }

    // Update Logics
    // TODO : RemoveOutOfBound System
    public void RemoveOutOfBound(ref UnitData type, in int index)
    {
        var pos = entityGameObjects[index].transform.position;

        // Horizontal OOB
        if (pos.x > horizontalLimit || pos.x < -horizontalLimit)
        {
            EnemyECS.RemoveEntity(this, ref type, index);
        }

        // Vertical OOB
        if (pos.y < -(verticalLimit + 3))
        {
            // Debug.Log($"OOB -> Type = {type} Index = {index} Pos = {pos} Active = {entityGameObjects[index].activeSelf}");
            EnemyECS.RemoveEntity(this, ref type, index);
            Player.instance?.TakeDamage(type.damage);
        }
    }

    public void Straight(in UnitData type, in int index)
    {
        StraightMovement(entityGameObjects[index], type.speed);
    }

    public void Oscilating(in UnitData type, in int index)
    {
        OscilatingMovement(entityGameObjects[index], type.speed);
    }

    // Damage Taken Logics
    int DamageModifiers(in UnitData type, in int index, int dmg)
    {
        int finalDamage = dmg;

        // Shield -> Use Runtime array
        if ((type.capabilityMask & (1 << (int)Capability.Shield)) != 0)
        {
            int delta = type.dataOffsets[(int)Capability.Shield];
            finalDamage = ShieldLogic(finalDamage, ref enemyShields[index - delta]);
        }

        // Armor -> Use Authoring array
        if ((type.capabilityMask & (1 << (int)Capability.Armor)) != 0)
        {
            int i = type.dataIndex[(int)Capability.Armor];
            // int delta = type.dataOffsets[(int)Capability.Armor]; // Use Runtime array Later
            finalDamage = ArmorLogic(finalDamage, ref dataRegistry.armorData[i].armor);
        }

        return finalDamage;
    }

    int ArmorLogic(int dmg, ref int armor)
    {
        int mitigatedDamage = dmg - armor;
        // Debug.Log($"{armor} ARMOR -> DMG = {dmg} -> {mitigatedDamage}");
        return mitigatedDamage;
    }

    int ShieldLogic(int dmg, ref int shield)
    {
        // Debug.Log($"SHIELD START -> dmg = {dmg}, shield = {shield}");
        int mitigatedDamage = Math.Max(0, dmg - shield);
        shield = Math.Max(shield - dmg, 0);

        // Debug.Log($"SHIELD END -> dmg = {mitigatedDamage}, shield = {shield}");
        return mitigatedDamage;
    }
}

// ENTITY TYPE > DATA DEFINITION
public struct UnitData : EnemyECS.IEntityType
{
    public UnitData(int type, int maxEntities, GameObject prefab, EnemyECS.EntityLogic spawnLogic, EnemyECS.RemoveLogic removeLogic, EnemyECS.EntityLogic updateLogic,
    int scoreValue, int hp, int damage, float speed, int capabilityMask = 0)
    {
        this.type = type;
        startIndex = 0;
        this.maxEntities = maxEntities;
        activeCount = 0;
        this.prefab = prefab;
        this.spawnLogic = spawnLogic;
        this.removeLogic = removeLogic;
        this.updateLogic = updateLogic;

        this.scoreValue = scoreValue;
        this.hp = hp;
        this.damage = damage;
        this.speed = speed;

        this.capabilityMask = capabilityMask;
        dataIndex = new int[(int)EnemyManager.Capability._Count]; // TODO : Set size = Last capacity index
        dataOffsets = new int[(int)EnemyManager.Capability._Count];
    }

    // IEntityType Implementation
    public int type { get; }
    public int startIndex { get; set; }
    public int maxEntities { get; }
    public int activeCount { get; set; }
    public GameObject prefab { get; }
    public EnemyECS.EntityLogic spawnLogic { get; }
    public EnemyECS.RemoveLogic removeLogic { get; }
    public EnemyECS.EntityLogic updateLogic { get; }

    // DEFAULT STATS
    public readonly int scoreValue;
    // public readonly int maxHp;
    public readonly int hp;
    public readonly int damage;
    public readonly float speed;

    // ADDITIONAL STATS
    public readonly int capabilityMask; // Bitmask
    public readonly int[] dataIndex; // Authoring array // Sparse? index = Capability -> For
    public readonly int[] dataOffsets; // Runtime array

    public override string ToString()
    {
        return $"UnitData Type = {type}, StartIndex = {startIndex}, ActiveCount = {activeCount}";
    }
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