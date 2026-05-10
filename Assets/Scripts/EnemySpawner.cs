using System;
using TMPro;
using UnityEngine;

using Random = UnityEngine.Random;

public class EnemySpawner : MonoBehaviour
{
    public static EnemySpawner instance;

    public GameObject[] mapLimits;
    public TextMeshProUGUI scoreText;
    public int score = 0;

    [Header("ENEMY SPAWNER")]
    public GameObject[] spawnLocations; // 3
    public float spawnDelta = 2f;
    private float lastSpawn;

    [Header("ENEMY - Scout")]
    public GameObject scoutPrefab;
    public int minScout = 2;
    public int scoutSpawnRange = 2;

    [Header("ENEMY - Frigate")]
    public GameObject frigatePrefab;

    void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);
    }

    void Start()
    {

    }

    void Update()
    {
        if (Time.time > lastSpawn + spawnDelta)
        {
            SpawnEnemies();
            lastSpawn = Time.time;
        }
    }

    public bool IsInBounds(Vector3 position)
    {
        return position.x >= mapLimits[0].transform.position.x && position.x <= mapLimits[1].transform.position.x
            && position.y >= mapLimits[2].transform.position.y && position.y <= mapLimits[3].transform.position.y;
    }

    public bool IsOutOfBounds(Vector3 position)
    {
        return position.x < mapLimits[0].transform.position.x - 3 || position.x > mapLimits[1].transform.position.x + 3
            || position.y < mapLimits[2].transform.position.y - 3 || position.y > mapLimits[3].transform.position.y + 3;
    }

    void SpawnEnemies()
    {
        // Spawn Scout
        int enemiesNbrRoll = Random.Range(minScout, minScout + scoutSpawnRange);

        for (int i = 0; i < enemiesNbrRoll; i++)
        {
            GameObject spawnedScout = Instantiate(scoutPrefab);
            SetupEnemy(spawnedScout);
        }

        // Spawn Frigate
        // GameObject spawnedFrigate = Instantiate(frigatePrefab);
        // SetupEnemy(spawnedFrigate);
    }

    void SetupEnemy(GameObject enemy)
    {
        if (!enemy) return;

        float xOffset = Random.Range(spawnLocations[1].transform.position.x, spawnLocations[2].transform.position.x);
        float yOffset = Random.Range(0, 2);
        enemy.transform.position = spawnLocations[0].transform.position + new Vector3(xOffset, yOffset);

        Destroy(enemy, 5);
    }

    public void IncrementScore(int amount)
    {
        score += amount;
        scoreText.SetText("Score : " + score.ToString());
    }

    public static void StraightMovement(GameObject entityBody, float speed)
    {
        entityBody.transform.position += entityBody.transform.up * speed * Time.deltaTime;
    }

    public static void OscilatingMovement(GameObject entityBody, float speed)
    {
        entityBody.transform.position += (entityBody.transform.right * (float)Math.Sin(Time.time) + entityBody.transform.up) * speed * Time.deltaTime;
    }
}
