using System;
using System.Diagnostics;
using System.Collections;

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;
using UnityEngine.SceneManagement;

public class Player : MonoBehaviour
{
    public static Player instance;

    [Header("REFERENCES")]
    public SpriteRenderer spriteRenderer;
    public Slider hpSlider;
    public GameObject gameOver;
    public Button gameOverButton;

    [Header("BACKGROUND")]
    public GameObject[] backgrounds;
    int currentBg = 0;
    public Vector3 bgResetPos = new Vector3(0, 37.5f, 0);
    public float bgSpeed = 0.1f;
    float baseBgSpeed;

    [Header("STATS")]
    [SerializeField]
    private int maxHp = 100;
    [SerializeField]
    private int hp = 100;

    [Header("MOVEMENTS")]
    public float speed = 0.3f;
    public float baseSpeed;
    public Vector3 moveInput;

    float horizontalLimit;
    float verticalLimit;

    [Header("SHOOT")]
    public AudioSource laserSound;
    public GameObject shootPos;
    public float shootDelay;
    public float shootDelayMin = 0.05f;
    public float baseShootDelayMin;
    public float shootDelayMax = 0.15f;
    public float baseShootDelayMax;
    private float lastShoot;

    private Vector2 shootDirection1, shootDirection2, shootDirection3, shootDirection4;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            // DontDestroyOnLoad(gameObject);
        }

        UpdateMovementLimits();
        UpdateShootDirection();

        baseBgSpeed = bgSpeed;
        baseSpeed = speed;
        baseShootDelayMin = shootDelayMin;
        baseShootDelayMax = shootDelayMax;
    }

    void Start()
    {
        gameOverButton.onClick.AddListener(LoadMainMenu);
    }

    void FixedUpdate()
    {
        Movements();

        foreach (GameObject bg in backgrounds)
        {
            bg.transform.Translate(Vector2.down * bgSpeed);
        }

        if (backgrounds[currentBg].transform.position.y <= 7)
        {
            currentBg++;

            if (currentBg >= backgrounds.Length)
            {
                currentBg = 0;
            }

            backgrounds[currentBg].transform.position = bgResetPos;
        }
    }

    void Update()
    {
        MovementsInputs();
        Shoot();
    }

    void LoadMainMenu()
    {
        ProjectileManager.instance.RemoveAll();
        EnemyManager.instance.RemoveAll();
        EnemyManager.instance.ResetIncrement();
        SceneManager.LoadScene("GigaMenu");
    }

    void MovementsInputs()
    {
        if (Keyboard.current.wKey.isPressed)
        {
            // print("Forward");
            moveInput.y = 1;
        }
        else if (Keyboard.current.sKey.isPressed)
        {
            // print("Backward");
            moveInput.y = -1;
        }

        if (Keyboard.current.aKey.isPressed)
        {
            // print("Left");
            moveInput.x = -1;
        }
        else if (Keyboard.current.dKey.isPressed)
        {
            // print("Right");
            moveInput.x = 1;
        }
    }

    void Movements()
    {
        // Map Limits
        if (transform.position.x < -horizontalLimit) moveInput.x = Math.Max(moveInput.x, 0);

        if (transform.position.x > horizontalLimit) moveInput.x = Math.Min(moveInput.x, 0);

        if (transform.position.y < -verticalLimit) moveInput.y = Math.Max(moveInput.y, 0);

        if (transform.position.y > verticalLimit) moveInput.y = Math.Min(moveInput.y, 0);

        // Normalize Velocity
        if (moveInput.magnitude > 1)
        {
            moveInput.Normalize();
        }

        // Apply Velocity
        transform.position += moveInput * speed;
        moveInput = Vector3.zero;
    }

    // When Changing Camera orthographicSize or Player SpriteSize
    void UpdateMovementLimits()
    {
        verticalLimit = Camera.main.orthographicSize - spriteRenderer.bounds.size.y / 2;
        horizontalLimit = Camera.main.orthographicSize * Screen.width / Screen.height - spriteRenderer.bounds.size.x / 2;
    }

    void UpdateShootDirection()
    {
        // In update if player can rotate
        shootDirection1 = RotateVector(transform.up, (float)(0.2f * Math.PI));
        shootDirection2 = RotateVector(transform.up, (float)(0.325f * Math.PI));
        shootDirection3 = new Vector2(-shootDirection1.x, shootDirection1.y);
        shootDirection4 = new Vector2(-shootDirection2.x, shootDirection2.y);
    }

    void Shoot()
    {
        if ((Keyboard.current.spaceKey.isPressed || Mouse.current.leftButton.isPressed) && Time.time > lastShoot + shootDelay)
        {
            ShootLaser(transform.up); // Forward
            ShootLaser(shootDirection1); // Left 36°
            ShootLaser(shootDirection2); // Left 58.5°
            ShootLaser(shootDirection3); // Mirror
            ShootLaser(shootDirection4);

            lastShoot = Time.time;
            shootDelay = Random.Range(shootDelayMin, shootDelayMax);
            laserSound.Stop();
            laserSound.Play();
        }
    }

    void ShootLaser(Vector2 direction)
    {
        var laser = ProjectileManager.instance.SpawnProjectile(ProjectileManager.ProjectileType.Laser, direction);

        if (!laser) return;

        laser.transform.position = shootPos.transform.position;
        // Rotate Laser
        // laser.transform.rotation = transform.rotation;
        laser.transform.rotation = LookRotation2D(direction); // Engine Agnostic
    }

    public void TakeDamage(int damage)
    {
        hp -= damage;

        float percentHp = hp / (float)maxHp;
        float missingPercentHp = 1 + 1 - percentHp;
        hpSlider.value = percentHp;

        // Debug.Log($"Player Take Damage = {damage} -> HP = {hp} -> Slider Value = {percentHp}");

        if (hp <= 0)
        {
            Death();
            return;
        }

        // Increase Stats
        bgSpeed = baseBgSpeed * missingPercentHp;
        speed = baseSpeed * missingPercentHp;
        shootDelayMin = baseShootDelayMin * (percentHp + 0.15f);
        shootDelayMax = baseShootDelayMax * (percentHp + 0.15f);
    }

    void Death()
    {
        // Debug.Log("Game over");
        gameOver?.SetActive(true);
        gameObject.SetActive(false);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent<EnemyEntity>(out var entity))
        {
            EnemyManager.instance.ApplyDamage(entity.index, entity.version, 50);
        }
    }

    // TODO : Move to Gears
    #region UTILS

    public static IEnumerator RunAfterDelay(float delay, Action action)
    {
        yield return new WaitForSeconds(delay);
        action();
    }

    public Vector2 RotateVector(Vector2 vector2, float angleRadiant)
    {
        float cosValue = (float)Math.Cos(angleRadiant);
        float sinValue = (float)Math.Sin(angleRadiant);

        return new Vector2(
        vector2.x * cosValue - vector2.y * sinValue,
        vector2.x * sinValue + vector2.y * cosValue);
    }

    public static Quaternion LookRotation2D(Vector2 direction)
    {
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
        return Quaternion.Euler(0, 0, angle);
    }

    public static IEnumerator WaitFrames(int frames)
    {
        return WaitFrames(frames);
    }

    public static void SpeedTest(String testName, Action action1)
    {
        Stopwatch sw = new Stopwatch();

        sw.Start();
        {
            action1();
        }
        sw.Stop();

        Debug.LogWarning($"{testName} Speed MS = {sw.Elapsed.Milliseconds}");
    }

    #endregion
}
