using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class CPlayer : MonoBehaviour
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
    public Vector3 moveInput;
    public GameObject[] mapLimits;

    // float horizontalLimit;
    // float verticalLimit;

    [Header("SHOOT")]
    public GameObject laserPrefab;
    public GameObject shootPos;
    public float shootDelay = 0.25f;
    public int laserDamage = 10;
    public float laserSpeed = 0.3f;
    private float lastShoot;
    List<GameObject> lasers;

    // Called before Start()
    // void Awake()
    // {
    //     UpdateMovementLimits();
    // }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // 50 Hz -> Movements
    // void FixedUpdate()
    // {
    //     Movements();
    // }

    // Update is called once per frame
    void Update()
    {
        Movements();
        Shoot();
        LaserMovements();
    }

    // When Changing Camera orthographicSize or Player SpriteSize
    // void UpdateMovementLimits()
    // {
    //     verticalLimit = Camera.main.orthographicSize - spriteRenderer.bounds.size.y / 2;
    //     horizontalLimit = Camera.main.orthographicSize * Screen.width / Screen.height - spriteRenderer.bounds.size.x / 2;
    // }

    void Movements()
    {
        if (Keyboard.current.wKey.isPressed) // Old Unity Input system: Input.GetKeyDown(KeyCode.Z)
        {
            // print("Forward");
            // transform.position += gameObject.transform.up * speed;
            moveInput.y = 1;
        }
        else if (Keyboard.current.sKey.isPressed)
        {
            // print("Backward");
            // transform.position -= gameObject.transform.up * speed;
            moveInput.y = -1;
        }

        if (Keyboard.current.aKey.isPressed)
        {
            // print("Left");
            // transform.position -= gameObject.transform.right * speed; // Unity
            // transform.position += Vector3.Cross(gameObject.transform.forward, Vector3.up) * speed; // Engine Agnostic
            moveInput.x = -1;
        }
        else if (Keyboard.current.dKey.isPressed)
        {
            // print("Right");
            // transform.position += gameObject.transform.right * speed;
            // transform.position -= Vector3.Cross(gameObject.transform.forward, Vector3.up) * speed;
            moveInput.x = 1;
        }

        // Normalize Velocity -> Use only Direction
        if (moveInput.magnitude > 1)
        {
            moveInput.Normalize();
        }

        // Apply Velocity
        transform.position += moveInput * speed * Time.deltaTime;
        moveInput = Vector3.zero;

        // Map Limits -> Camera
        // float clampX = Math.Clamp(transform.position.x, -horizontalLimit, horizontalLimit);
        // float clampY = Math.Clamp(transform.position.y, -verticalLimit, verticalLimit);
        // transform.position = new Vector3(clampX, clampY, transform.position.z);

        // Map Limit -> Points
        float clampX = Math.Clamp(transform.position.x, mapLimits[0].transform.position.x, mapLimits[1].transform.position.x);
        float clampY = Math.Clamp(transform.position.y, mapLimits[2].transform.position.y, mapLimits[3].transform.position.y);
        transform.position = new Vector3(clampX, clampY, transform.position.z);
    }

    void Shoot()
    {
        if ((Keyboard.current.spaceKey.isPressed || Mouse.current.leftButton.isPressed) && Time.fixedTime > lastShoot + shootDelay)
        {
            GameObject spawnedLaser = Instantiate(laserPrefab);
            Destroy(spawnedLaser, 2);
            lasers.Add(spawnedLaser);

            lastShoot = Time.fixedTime;
        }
    }

    void LaserMovements()
    {
        foreach (GameObject laser in lasers)
        {
            laser.transform.position += laser.transform.up * laserSpeed * Time.deltaTime;
        }
    }

    public void TakeDamage(int damage)
    {
        hp -= damage;
        hpSlider.value = hp / maxHp;

        if (hp <= 0)
        {
            Debug.Log("Game Over");
        }
    }
}
