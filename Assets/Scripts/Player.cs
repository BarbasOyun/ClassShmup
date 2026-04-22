using System;
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
    public Vector3 moveInput;
    public GameObject[] mapLimits;

    float horizontalLimit;
    float verticalLimit;

    [Header("SHOOT")]
    public GameObject shootPos;
    public float shootDelay = 0.25f;
    private float lastShoot;
    //List<GameObject> lasers;

    void Awake()
    {
        UpdateMovementLimits();
    }

    void Start()
    {

    }

    // 50 Hz -> Movements
    void FixedUpdate()
    {
        Movements();
    }

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
            moveInput = new Vector3(moveInput.x, 1);
        }
        else if (Keyboard.current.sKey.isPressed)
        {
            // print("Backward");
            // transform.position -= gameObject.transform.up * speed;
            moveInput = new Vector3(moveInput.x, -1);
        }

        if (Keyboard.current.aKey.isPressed)
        {
            // print("Left");
            // transform.position -= gameObject.transform.right * speed;
            // transform.position += Vector3.Cross(gameObject.transform.forward, Vector3.up) * speed;
            moveInput = new Vector3(-1, moveInput.y);
        }
        else if (Keyboard.current.dKey.isPressed)
        {
            // print("Right");
            // transform.position += gameObject.transform.right * speed;
            // transform.position -= Vector3.Cross(gameObject.transform.forward, Vector3.up) * speed;
            moveInput = new Vector3(1, moveInput.y);
        }
    }

    void Movements()
    {
        // Normalize Velocity
        if (moveInput.magnitude > 1)
        {
            moveInput.Normalize();
        }

        // Apply Velocity
        transform.position += moveInput * speed; // * Time.deltaTime;
        moveInput = Vector3.zero;

        // Map Limits -> Camera
        float clampX = Math.Clamp(transform.position.x, -horizontalLimit, horizontalLimit);
        float clampY = Math.Clamp(transform.position.y, -verticalLimit, verticalLimit);
        transform.position = new Vector3(clampX, clampY, transform.position.z);

        // Map Limit -> Points
        // float clampX = Math.Clamp(transform.position.x, mapLimits[0].transform.position.x, mapLimits[1].transform.position.x);
        // float clampY = Math.Clamp(transform.position.y, mapLimits[2].transform.position.y, mapLimits[3].transform.position.y);
        // transform.position = new Vector3(clampX, clampY, transform.position.z);
    }

    // When Changing Camera orthographicSize or Player SpriteSize
    void UpdateMovementLimits()
    {
        verticalLimit = Camera.main.orthographicSize - spriteRenderer.bounds.size.y / 2;
        horizontalLimit = Camera.main.orthographicSize * Screen.width / Screen.height - spriteRenderer.bounds.size.x / 2;
    }

    void Shoot()
    {
        if ((Keyboard.current.spaceKey.isPressed || Mouse.current.leftButton.isPressed) && Time.fixedTime > lastShoot + shootDelay)
        {
            GameObject spawnedLaser = ProjectileManager.instance.SpawnProjectile();

            if (spawnedLaser)
            {
                spawnedLaser.transform.position = shootPos.transform.position;
                spawnedLaser.transform.rotation = transform.rotation;
            }

            lastShoot = Time.fixedTime;

            // Destroy(newLaser, 2);

            // lasers.Add(newLaser);

            // StartCoroutine(RunAfterDelay(2, () =>
            // {
            // RemoveLaser();
            // }));
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
