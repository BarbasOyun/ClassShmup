using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections;

public class Player : MonoBehaviour
{
    [Header("REFERENCES")]
    public SpriteRenderer spriteRenderer;
    public Slider hpSlider;

    [Header("STATS")]
    [SerializeField]
    private int maxHp = 100;
    [SerializeField]
    private int hp = 100;

    [Header("MOVEMENTS")]
    public float speed = 0.1f;
    public Vector3 velocityDirection; // Normalized
    public GameObject[] mapLimits;
    public Camera mainCamera;

    [Header("SHOOT")]
    public GameObject shootPos;
    public GameObject laser;
    public float shootDelay = 0.25f;
    private float lastShoot;
    public float laserSpeed = 10;
    //List<GameObject> lasers;

    // ECS -> ECS System
    // TODO : Object Pooling
    int maxLaser = 1000;
    int activeCount = 0;

    int[] versions;
    GameObject[] laserGameObjects;

    void Awake()
    {
        versions = new int[maxLaser];
        laserGameObjects = new GameObject[maxLaser];
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // 50 Hz -> Movements
    void FixedUpdate()
    {
        Movements();
        LaserMovements();
    }

    // Update is called once per frame -> Inputs
    void Update()
    {
        // Movements();
        // LaserMovements();

        MovementsInputs();
        Shoot();
    }

    void MovementsInputs()
    {
        if (Keyboard.current.wKey.isPressed) // Old Input system: Input.GetKeyDown(KeyCode.Z)
        {
            // print("Forward");
            // transform.position += gameObject.transform.up * speed;
            velocityDirection = new Vector3(velocityDirection.x, 1);
        }
        else if (Keyboard.current.sKey.isPressed)
        {
            // print("Backward");
            // transform.position -= gameObject.transform.up * speed;
            velocityDirection = new Vector3(velocityDirection.x, -1);
        }

        if (Keyboard.current.aKey.isPressed)
        {
            // print("Left");
            // transform.position -= gameObject.transform.right * speed;
            // transform.position += Vector3.Cross(gameObject.transform.forward, Vector3.up) * speed;
            velocityDirection = new Vector3(-1, velocityDirection.y);
        }
        else if (Keyboard.current.dKey.isPressed)
        {
            // print("Right");
            // transform.position += gameObject.transform.right * speed;
            // transform.position -= Vector3.Cross(gameObject.transform.forward, Vector3.up) * speed;
            velocityDirection = new Vector3(1, velocityDirection.y);
        }
    }

    void Movements()
    {
        // Apply Velocity
        transform.position += velocityDirection * speed; // * Time.deltaTime;
        velocityDirection = Vector3.zero;

        // Map Limits -> Camera
        float verticalLimit = mainCamera.orthographicSize - spriteRenderer.bounds.size.y / 2;
        float horizontalLimit = mainCamera.orthographicSize * Screen.width / Screen.height - spriteRenderer.bounds.size.x / 2;

        float clampX = Math.Clamp(transform.position.x, -horizontalLimit, horizontalLimit);
        float clampY = Math.Clamp(transform.position.y, -verticalLimit, verticalLimit);
        transform.position = new Vector3(clampX, clampY, transform.position.z);

        // Map Limit -> Points
        // float clampX = Math.Clamp(transform.position.x, mapLimits[0].transform.position.x, mapLimits[1].transform.position.x);
        // float clampY = Math.Clamp(transform.position.y, mapLimits[2].transform.position.y, mapLimits[3].transform.position.y);
        // transform.position = new Vector3(clampX, clampY, transform.position.z);
    }

    void Shoot()
    {
        if ((Keyboard.current.spaceKey.isPressed || Mouse.current.leftButton.isPressed) && Time.fixedTime > lastShoot + shootDelay)
        {
            GameObject newLaser = SpawnLaser();
            newLaser.transform.position = shootPos.transform.position;
            newLaser.transform.rotation = transform.rotation;

            lastShoot = Time.fixedTime;

            // Destroy(newLaser, 2);

            // lasers.Add(newLaser);

            // StartCoroutine(RunAfterDelay(2, () =>
            // {
            // RemoveLaser();
            // }));
        }
    }

    GameObject SpawnLaser()
    {
        GameObject newLaser = Instantiate(laser);

        laserGameObjects[activeCount] = newLaser;
        versions[activeCount] += 1;

        if (newLaser.TryGetComponent(out Bullet entity))
        {
            entity.index = activeCount;
            entity.version = versions[activeCount] + 1;

            StartCoroutine(RunAfterDelay(2, () =>
            {
                RemoveLaser(entity.index);
            }));
        }

        activeCount++;

        return newLaser;
    }

    public void RemoveLaser(int indexToRemove)
    {
        Destroy(laserGameObjects[indexToRemove]);

        int lastIndex = activeCount - 1;

        if (indexToRemove != lastIndex)
        {
            GameObject movedEnemy = laserGameObjects[lastIndex];

            // Move Data
            laserGameObjects[indexToRemove] = movedEnemy;

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

    void LaserMovements()
    {
        foreach (GameObject laser in laserGameObjects)
        {
            laser.transform.position += laser.transform.up * laserSpeed; // * Time.deltaTime;
        }
    }

    public void TakeDamage(int damage)
    {
        hp -= damage;
        hpSlider.value = hp / maxHp;
    }

    #region UTILS

    public static IEnumerator RunAfterDelay(float delay, Action action)
    {
        yield return new WaitForSeconds(delay);
        action();
    }

    #endregion
}
