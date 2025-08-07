using UnityEngine;
using System.Collections;
using UnityEngine.Events; // Required for UnityEvents (though not directly used in this script, good to keep if other parts of EnemyAI use it)
using static AudioManager; // ADDED: To allow direct access to SFXType enum

[RequireComponent(typeof(Health))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CharacterDamageFeedback))]
[RequireComponent(typeof(EnemyStats))]
public class EnemyAI : MonoBehaviour
{
    [Header("Enemy Capabilities")]
    [SerializeField] private bool isMeleeAttacker = false;
    [SerializeField] private bool isRangedAttacker = true;

    [Header("Target & Movement")]
    public Transform playerTransform;
    public float meleeStoppingDistance = 0.8f;
    public float rangedStoppingDistance = 5f;

    // NEW: Reference to the Health Bar UI Prefab
    [Header("UI References")]
    public GameObject healthBarUIPrefab; // Assign your HealthBarCanvas prefab here
    private HealthBarUI enemyHealthBar; // Reference to the instantiated health bar script

    [Header("Ranged Attack Settings (if Is Ranged Attacker)")]
    public GameObject projectilePrefab;
    public Transform firePoint;

    [Header("Melee Attack Settings (if Is Melee Attacker)")]
    public float meleeAttackCooldown = 1f;
    public float meleePauseDuration = 2f;
    private float nextMeleeAttackTime;

    // NEW: Contact Damage Setting
    [Header("Contact Damage")]
    public int contactDamage = 5; // Damage applied when player collides with enemy

    [Header("Waddle Effect")]
    public float waddleMagnitude = 2f;
    public float waddleSpeed = 5f;

    [Header("Death Settings")]
    public GameObject deathParticlePrefab;

    private float nextFireTime;
    private Health health;
    private Rigidbody rb;
    private CharacterDamageFeedback damageFeedback;
    private EnemyStats enemyStats;
    private Camera gameCamera; // Reference to the main camera for health bar UI
    private Collider enemyCollider; // ADDED: Reference to the enemy's collider

    private bool isDead = false;
    private bool isMoving = false;
    private bool isStunned = false;
    private float initialZRotation;

    void Awake()
    {
        health = GetComponent<Health>();
        rb = GetComponent<Rigidbody>();
        damageFeedback = GetComponent<CharacterDamageFeedback>();
        enemyStats = GetComponent<EnemyStats>();
        enemyCollider = GetComponent<Collider>(); // ADDED: Get the collider component

        if (rb == null)
        {
            Debug.LogError("[EnemyAI] Rigidbody component not found on enemy. Movement might not be consistent.", this);
        }
        else
        {
            rb.useGravity = false;
            rb.isKinematic = false;
            rb.freezeRotation = true;
        }

        if (damageFeedback == null)
        {
            Debug.LogError("[EnemyAI] CharacterDamageFeedback component not found on enemy. Damage feedback effects (like enemy stumble from melee) will not work.");
        }

        if (enemyStats == null)
        {
            Debug.LogError("[EnemyAI] EnemyStats component not found on enemy. Enemy stats (Damage, Movement Speed, Fire Rate) will not function correctly.", this);
        }
        
        // ADDED: Warn if collider is not found, although RequireComponent ensures one exists.
        if (enemyCollider == null)
        {
            Debug.LogWarning("[EnemyAI] Collider component not found on enemy. Cannot disable on death!", this);
        }

        initialZRotation = transform.rotation.eulerAngles.z;

        health.OnDeath.AddListener(OnEnemyDeath);
    }

    void Start()
    {
        // Find the player transform if not assigned in inspector
        if (playerTransform == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                playerTransform = playerObj.transform;
            }
            else
            {
                Debug.LogWarning("[EnemyAI] Player GameObject with 'Player' tag not found at Start for " + gameObject.name);
            }
        }

        // Get the main camera for the health bar UI
        gameCamera = Camera.main;
        if (gameCamera == null)
        {
            Debug.LogError("[EnemyAI] Main Camera not found! Health bar UI may not display correctly for " + gameObject.name);
        }

        // Instantiate and Initialize the Enemy's Health Bar
        if (healthBarUIPrefab != null && health != null && gameCamera != null)
        {
            GameObject healthBarGO = Instantiate(healthBarUIPrefab);
            enemyHealthBar = healthBarGO.GetComponent<HealthBarUI>();
            if (enemyHealthBar != null)
            {
                // Pass THIS enemy's Health component, Transform, and the active game camera
                enemyHealthBar.Initialize(health, this.transform, gameCamera);
            }
            else
            {
                Debug.LogWarning("[EnemyAI] HealthBarUIPrefab does not have a HealthBarUI component!", this);
            }
        }
        else
        {
            if (healthBarUIPrefab == null) Debug.LogWarning("[EnemyAI] Health Bar UI Prefab not assigned for " + gameObject.name + "!");
            if (health == null) Debug.LogWarning("[EnemyAI] Enemy's Health component not found, cannot initialize health bar for " + gameObject.name + ".");
            if (gameCamera == null) Debug.LogWarning("[EnemyAI] Game Camera not found, cannot initialize health bar position correctly for " + gameObject.name + ".");
        }
    }

    void OnEnemyDeath()
    {
        Debug.Log(gameObject.name + " has been defeated!");
        isDead = true;
        StopAllCoroutines();

        // Play EnemyDeath SFX
        AudioManager.Instance.PlaySFX(SFXType.EnemyDeath);

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        // ADDED: Disable the collider immediately upon death
        if (enemyCollider != null)
        {
            enemyCollider.enabled = false;
        }

        this.enabled = false; // Disable this script

        // Disable the enemy's health bar when it dies
        if (enemyHealthBar != null)
        {
            Destroy(enemyHealthBar.gameObject); // Destroy the health bar UI
        }

        StartCoroutine(HandleDeathSequence());
    }

    private IEnumerator HandleDeathSequence()
    {
        if (deathParticlePrefab != null)
        {
            Instantiate(deathParticlePrefab, transform.position, Quaternion.identity);
        }

        float duration = 0.5f;
        Quaternion startRotation = transform.rotation;
        // Adjust end rotation to make enemy fall on its side (e.g., -90 degrees around Z-axis)
        Quaternion endRotation = Quaternion.Euler(startRotation.eulerAngles.x, startRotation.eulerAngles.y, -90f); 

        float startTime = Time.time;
        while (Time.time < startTime + duration)
        {
            float t = (Time.time - startTime) / duration;
            transform.rotation = Quaternion.Slerp(startRotation, endRotation, t);
            yield return null;
        }
        transform.rotation = endRotation; // Ensure final rotation is set

        Debug.Log($"[EnemyAI] {gameObject.name} GameObject being destroyed.");
        Destroy(gameObject);
    }

    void Update()
    {
        if (isDead || (damageFeedback != null && damageFeedback.IsKnockedBack()) || isStunned)
        {
            if (rb != null) rb.linearVelocity = Vector3.zero;
            isMoving = false;
            return;
        }

        // Continually try to find player if not assigned, in case player spawns later
        if (playerTransform == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                playerTransform = playerObj.transform;
            }
            else
            {
                if (rb != null) rb.linearVelocity = Vector3.zero;
                isMoving = false;
                return;
            }
        }

        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        float currentStoppingDistance;

        if (isMeleeAttacker)
        {
            currentStoppingDistance = meleeStoppingDistance;
        }
        else if (isRangedAttacker)
        {
            currentStoppingDistance = rangedStoppingDistance;
        }
        else
        {
            currentStoppingDistance = 0f; // If neither, always move
        }

        // Use movement speed from EnemyStats
        float currentChaseSpeed = enemyStats != null ? enemyStats.MovementSpeed : 3f; 

        // --- Movement Logic ---
        if (distanceToPlayer > currentStoppingDistance)
        {
            Vector3 direction = (playerTransform.position - transform.position).normalized;
            if (rb != null)
            {
                rb.linearVelocity = direction * currentChaseSpeed; // Use stat-driven speed
            }
            isMoving = true;
        }
        else
        {
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
            }
            isMoving = false;
        }

        // --- Rotation to Face Player ---
        Vector3 enemyPosFlat = new Vector3(transform.position.x, 0, transform.position.z);
        Vector3 playerPosFlat = new Vector3(playerTransform.position.x, 0, playerTransform.position.z);

        Vector3 lookDirection = (playerPosFlat - enemyPosFlat).normalized;

        if (lookDirection != Vector3.zero)
        {
            // Only update Y rotation to look at player, keep X and Z (for waddle)
            Quaternion targetYRotation = Quaternion.LookRotation(lookDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, 
                                                  Quaternion.Euler(transform.rotation.eulerAngles.x, targetYRotation.eulerAngles.y, transform.rotation.eulerAngles.z), 
                                                  Time.deltaTime * 10f);
        }

        // --- Waddle Effect ---
        if (isMoving)
        {
            float waddleZ = Mathf.Sin(Time.time * waddleSpeed) * waddleMagnitude;
            transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles.x, transform.rotation.eulerAngles.y, initialZRotation + waddleZ);
        }
        else
        {
            // Smoothly return to initial Z rotation when not moving
            Quaternion targetNoWaddleRotation = Quaternion.Euler(transform.rotation.eulerAngles.x, transform.rotation.eulerAngles.y, initialZRotation);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetNoWaddleRotation, Time.deltaTime * 5f);
        }

        // --- Attack Logic (based on Capabilities) ---
        if (isMeleeAttacker && distanceToPlayer <= meleeStoppingDistance) 
        {
            if (Time.time >= nextMeleeAttackTime)
            {
                MeleeAttack();
                nextMeleeAttackTime = Time.time + meleeAttackCooldown;
            }
        }
        else if (isRangedAttacker && distanceToPlayer <= rangedStoppingDistance) 
        {
            float actualFireRate = enemyStats != null ? enemyStats.FireRate : 1f; // Fallback
            if (Time.time >= nextFireTime)
            {
                Shoot();
                nextFireTime = Time.time + 1f / actualFireRate;
            }
        }
    }

    /// <summary>
    /// Handles collision with other GameObjects, specifically for applying contact damage to the player.
    /// </summary>
    /// <param name="collision">The Collision data.</param>
    void OnCollisionStay(Collision collision)
    {
        // Only apply contact damage if the enemy is not dead or stunned/knocked back
        if (isDead || (damageFeedback != null && damageFeedback.IsKnockedBack()) || isStunned)
        {
            return;
        }

        // Check if the collided object is the Player
        if (collision.gameObject.CompareTag("Player"))
        {
            Health playerHealth = collision.gameObject.GetComponent<Health>();
            if (playerHealth != null)
            {
                // Apply contact damage
                playerHealth.TakeDamage(contactDamage, transform.position);
                // Optionally add a small cooldown here to prevent damage every frame
                // For now, it will damage every frame it's in contact if no invincibility on player.
            }
        }
    }

    void Shoot()
    {
        if (projectilePrefab != null && firePoint != null)
        {
            // Calculate the direction from enemy to player (flat on XZ plane)
            Vector3 playerPosFlat = new Vector3(playerTransform.position.x, 0, playerTransform.position.z);
            Vector3 enemyPosFlat = new Vector3(transform.position.x, 0, transform.position.z);
            Vector3 shootDirection = (playerPosFlat - enemyPosFlat).normalized;

            // Calculate the Y rotation needed to look along the shootDirection
            float targetYRotation = Quaternion.LookRotation(shootDirection).eulerAngles.y;

            // Instantiate projectile with:
            // X: 90 (fixed for upright look)
            // Y: targetYRotation (to track player horizontally)
            // Z: 0 (no waddle or roll for projectile)
            GameObject bullet = Instantiate(projectilePrefab, firePoint.position, Quaternion.Euler(90f, targetYRotation, 0f)); 
            
            ProjectileBehavior bulletScript = bullet.GetComponent<ProjectileBehavior>();
            if (bulletScript != null)
            {
                // Pass the calculated damage from EnemyStats to the projectile
                float calculatedDamage = enemyStats != null ? enemyStats.GetCalculatedProjectileDamage() : 10f; // Fallback
                bulletScript.SetDamage(calculatedDamage);
                
                // Projectile speed can also come from EnemyStats if you want it to be variable
                // For now, using bulletScript.speed (default on prefab)
                bulletScript.SetDirection(shootDirection, bulletScript.speed); 
                bulletScript.targetTag = "Player"; // Enemy projectiles target the Player
            }
            else
            {
                Debug.LogWarning("[EnemyAI] Projectile prefab does not have a ProjectileBehavior script!");
            }

            // Play EnemyFire SFX
            AudioManager.Instance.PlaySFX(SFXType.EnemyFire);
        }
    }

    void MeleeAttack()
    {
        Debug.Log($"[EnemyAI] {gameObject.name} performing melee attack on player!");
        if (playerTransform != null)
        {
            Health playerHealth = playerTransform.GetComponent<Health>();
            if (playerHealth != null)
            {
                // Pass the calculated melee damage from EnemyStats
                float calculatedMeleeDamage = enemyStats != null ? enemyStats.GetCalculatedMeleeDamage() : 15f; // Fallback
                playerHealth.TakeDamage(Mathf.RoundToInt(calculatedMeleeDamage), transform.position); 
            }
        }
        
        StartCoroutine(StunEnemyForDuration(meleePauseDuration));
    }

    private IEnumerator StunEnemyForDuration(float duration)
    {
        isStunned = true;
        isMoving = false;
        if (rb != null) rb.linearVelocity = Vector3.zero;

        Debug.Log($"[EnemyAI] {gameObject.name} is stunned for {duration} seconds.");

        yield return new WaitForSeconds(duration);

        isStunned = false;
        Debug.Log($"[EnemyAI] {gameObject.name} is no longer stunned.");
    }
}