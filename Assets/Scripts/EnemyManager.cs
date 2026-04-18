using Unity.VisualScripting;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    // TODO : Use Service Locator
    public static EnemyManager instance;

    // ECS
    // TODO : Object Pooling
    int maxEnemies = 1000;
    int activeCount = 0;

    int[] versions;
    GameObject[] enemyGameObjects;
    // EnemyEntity[]
    int[] enemyHp;

    [Header("SETTINGS")]
    public GameObject[] spawnLocations; // 3
    public int minEnemies = 2;
    public int enemiesRange = 2;
    public Vector2 enemiesDirection;
    float angle;
    Quaternion lookDirection;

    [Header("ENEMY 01")]
    public GameObject enemy01;
    public int enemy01Hp = 10;
    public float enemy01Speed = 0.1f;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        versions = new int[maxEnemies];
        enemyGameObjects = new GameObject[maxEnemies];
        enemyHp = new int[maxEnemies];

        angle = Mathf.Atan2(enemiesDirection.y, enemiesDirection.x) * Mathf.Rad2Deg - 90;
        lookDirection = Quaternion.Euler(0, 0, angle);
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        InvokeRepeating(nameof(SpawnEnemies), 2.0f, 1f);
    }

    void FixedUpdate()
    {
        Enemy01Movements();
    }

    // Update is called once per frame
    void Update()
    {

    }

    void SpawnEnemies() // TODO : Spawn Enemy Type Function
    {
        int enemiesNbrRoll = Random.Range(minEnemies, minEnemies + enemiesRange);

        for (int i = 0; i < enemiesNbrRoll; i++)
        {
            GameObject newEnemy = SpawnEnemy();
            newEnemy.transform.rotation = lookDirection;
            int locationOffset = (int)Random.Range(spawnLocations[1].transform.position.x, spawnLocations[2].transform.position.x);
            newEnemy.transform.position = spawnLocations[0].transform.position + new Vector3(locationOffset, 0);
        }
    }

    GameObject SpawnEnemy()
    {
        GameObject newEnemy = SpawnEnemy01();

        enemyGameObjects[activeCount] = newEnemy;
        versions[activeCount] += 1;

        if (newEnemy.TryGetComponent(out EnemyEntity entity))
        {
            entity.index = activeCount;
            entity.version = versions[activeCount] + 1;

            StartCoroutine(Player.RunAfterDelay(5, () =>
            {
                RemoveEnemy(entity.index);
            }));
        }

        activeCount++;

        return newEnemy;
    }

    void RemoveEnemy(int indexToRemove)
    {
        Destroy(enemyGameObjects[indexToRemove]);

        int lastIndex = activeCount - 1;

        if (indexToRemove != lastIndex)
        {
            GameObject movedEnemy = enemyGameObjects[lastIndex];

            // Move Data
            enemyGameObjects[indexToRemove] = movedEnemy;
            enemyHp[indexToRemove] = enemyHp[lastIndex];

            // Update GameObject Index
            if (movedEnemy.TryGetComponent(out EnemyEntity entity))
            {
                entity.index = indexToRemove;
            }
        }

        // Update Version
        versions[indexToRemove]++;
        activeCount--;
    }

    public void ApplyDamage(int index, int version, int damage)
    {
        if (versions[index] != version)
        {
            return;
        }

        int hp = enemyHp[index] -= damage;

        if (hp <= 0)
        {
            RemoveEnemy(index);
        }
    }

    GameObject SpawnEnemy01()
    {
        GameObject newEnemy = Instantiate(enemy01);
        enemyHp[activeCount] = enemy01Hp;

        return newEnemy;
    }

    void Enemy01Movements()
    {
        for (int i = 0; i < activeCount; i++)
        {
            enemyGameObjects[i].transform.position += enemyGameObjects[i].transform.up * enemy01Speed;
        }
    }
}
