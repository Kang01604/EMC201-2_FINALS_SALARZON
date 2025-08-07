using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using static AudioManager;

[RequireComponent(typeof(Health))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CharacterDamageFeedback))]
[RequireComponent(typeof(EnemyStats))]
[RequireComponent(typeof(Animator))]
public class BossAI : MonoBehaviour
{
    [Header("Boss Capabilities")]
    [SerializeField] private GameObject bossMissilePrefab;
    [SerializeField] private Transform firePoint;

    [Header("Target & Movement")]
    public Transform playerTransform;
    public float patrolStoppingDistance = 0.8f;
    public float waitTimeAtPoint = 2f;
    public float rotationSpeed = 5f;

    [Header("Contact Damage")]
    public int contactDamage = 5;

    [Header("Waddle Effect")]
    public float waddleMagnitude = 2f;
    public float waddleSpeed = 5f;

    [Header("Death Settings")]
    public GameObject deathParticlePrefab;

    [Header("Attack Settings")]
    public float attackCooldown = 10f;
    public int missilesPerVolley = 5;
    public float volleyFireInterval = 1f;

    [Header("Animator Settings")]
    public string runAnimParameter = "IsRun";
    public string idleAnimParameter = "IsIdle";

    private Health health;
    private Rigidbody rb;
    private CharacterDamageFeedback damageFeedback;
    private EnemyStats enemyStats;
    private Collider bossCollider;
    private Animator animator;
    private LevelManager levelManager; // Reference to the LevelManager

    private bool isDead = false;
    private bool isMoving = false;
    private bool isWaitingAtPoint = false;
    private float initialZRotation;

    private Vector3 patrolTargetA;
    private Vector3 patrolTargetB;
    private Vector3 currentPatrolTarget;

    private float nextAttackTime;
    private Coroutine currentPatrolCoroutine;
    private Coroutine currentAttackCoroutine;

    void Awake()
    {
        health = GetComponent<Health>();
        rb = GetComponent<Rigidbody>();
        damageFeedback = GetComponent<CharacterDamageFeedback>();
        enemyStats = GetComponent<EnemyStats>();
        bossCollider = GetComponent<Collider>();
        animator = GetComponent<Animator>();

        if (rb == null) Debug.LogError("[BossAI] Rigidbody component not found.", this);
        else
        {
            rb.useGravity = false;
            rb.isKinematic = false;
            rb.freezeRotation = true;
        }

        if (damageFeedback == null) Debug.LogError("[BossAI] CharacterDamageFeedback component not found.");
        if (enemyStats == null) Debug.LogError("[BossAI] EnemyStats component not found.", this);
        if (bossCollider == null) Debug.LogWarning("[BossAI] Collider component not found. Cannot disable on death!", this);
        else if (!bossCollider.isTrigger)
        {
            Debug.LogError("[BossAI] The boss's collider is NOT set to 'Is Trigger'. It will collide with objects. Please enable 'Is Trigger' on its collider component for correct movement.", this);
        }

        if (animator == null) Debug.LogError("[BossAI] Animator component not found on Boss GameObject! Animations will not play.", this);

        if (animator != null)
        {
            if (string.IsNullOrEmpty(runAnimParameter) || !CheckAnimatorParameter(runAnimParameter, AnimatorControllerParameterType.Bool))
            {
                Debug.LogWarning($"[BossAI] The animator parameter '{runAnimParameter}' (for running) is not set or does not exist.");
            }
            if (string.IsNullOrEmpty(idleAnimParameter) || !CheckAnimatorParameter(idleAnimParameter, AnimatorControllerParameterType.Bool))
            {
                Debug.LogWarning($"[BossAI] The animator parameter '{idleAnimParameter}' (for idling) is not set or does not exist.");
            }
        }

        if (bossMissilePrefab == null) Debug.LogWarning("[BossAI] Boss Missile Prefab not assigned!");
        if (firePoint == null) Debug.LogWarning("[BossAI] Fire Point not assigned for Boss Missile!");
        
        initialZRotation = transform.rotation.eulerAngles.z;
        health.OnDeath.AddListener(OnBossDeath);
    }

    private bool CheckAnimatorParameter(string paramName, AnimatorControllerParameterType paramType)
    {
        if (animator == null) return false;
        foreach (var param in animator.parameters)
        {
            if (param.name == paramName && param.type == paramType) return true;
        }
        return false;
    }

    void Start()
    {
        if (playerTransform == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) playerTransform = playerObj.transform;
            else Debug.LogWarning("[BossAI] Player not found. Boss attacks requiring player will not function.");
        }
        nextAttackTime = Time.time + attackCooldown;
    }

    public void SetLevelManager(LevelManager manager)
    {
        levelManager = manager;
    }

    public void SetInitialPatrol(Transform spawnLocationTransform, List<EnemySpawner> allBossSpawners)
    {
        patrolTargetA = spawnLocationTransform.position;
        Debug.Log($"[BossAI] Patrol Point A set to spawn location: {patrolTargetA}");

        float maxDistance = -1f;
        Vector3 furthestSpawnerPosition = Vector3.zero;
        bool foundOtherSpawner = false;

        foreach (EnemySpawner spawner in allBossSpawners)
        {
            Vector3 currentSpawnerPosition = spawner.spawnPoint.position;
            if (currentSpawnerPosition == patrolTargetA) continue;

            float distance = Vector3.Distance(patrolTargetA, currentSpawnerPosition);
            if (distance > maxDistance)
            {
                maxDistance = distance;
                furthestSpawnerPosition = currentSpawnerPosition;
                foundOtherSpawner = true;
            }
        }

        if (foundOtherSpawner)
        {
            patrolTargetB = furthestSpawnerPosition;
            Debug.Log($"[BossAI] Patrol Point B set to furthest spawner at: {patrolTargetB} (Distance: {maxDistance:F2})");
        }
        else
        {
            Debug.LogError("[BossAI] Could not find a distinct spawn point for Patrol Point B. Boss will not patrol.", this);
            patrolTargetB = patrolTargetA;
        }

        currentPatrolTarget = patrolTargetB;

        if (patrolTargetA != patrolTargetB)
        {
            if (currentPatrolCoroutine != null) StopCoroutine(currentPatrolCoroutine);
            currentPatrolCoroutine = StartCoroutine(PatrolRoutine());
        }
        else
        {
            Debug.LogWarning("[BossAI] Patrol points are identical. Boss will idle.", this);
            isMoving = false;
            isWaitingAtPoint = true;
            UpdateAnimator(false, true);
        }
    }

    void Update()
    {
        if (isDead)
        {
            if (rb != null) rb.linearVelocity = Vector3.zero;
            UpdateAnimator(false, false);
            return;
        }

        if (isMoving)
        {
            float waddleZ = Mathf.Sin(Time.time * waddleSpeed) * waddleMagnitude;
            transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles.x, transform.rotation.eulerAngles.y, initialZRotation + waddleZ);
        }
        else
        {
            Quaternion targetNoWaddleRotation = Quaternion.Euler(transform.rotation.eulerAngles.x, transform.rotation.eulerAngles.y, initialZRotation);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetNoWaddleRotation, Time.deltaTime * 5f);
        }

        if (playerTransform != null && Time.time >= nextAttackTime && currentAttackCoroutine == null)
        {
            currentAttackCoroutine = StartCoroutine(HandleBossAttack());
            nextAttackTime = Time.time + attackCooldown;
        }
    }

    private IEnumerator PatrolRoutine()
    {
        while (!isDead)
        {
            Vector3 targetPosition = currentPatrolTarget;
            float distanceToTarget = Vector3.Distance(transform.position, targetPosition);
            float currentMoveSpeed = enemyStats != null ? enemyStats.MovementSpeed : 3f;

            while (distanceToTarget > patrolStoppingDistance && !isDead)
            {
                isMoving = true;
                isWaitingAtPoint = false;
                UpdateAnimator(isMoving, isWaitingAtPoint);

                Vector3 direction = (targetPosition - transform.position).normalized;
                if (rb != null) rb.linearVelocity = direction * currentMoveSpeed;

                if (direction.magnitude > 0.01f)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
                    transform.rotation = Quaternion.Slerp(transform.rotation,
                                                          Quaternion.Euler(transform.rotation.eulerAngles.x, targetRotation.eulerAngles.y, transform.rotation.eulerAngles.z),
                                                          Time.deltaTime * rotationSpeed);
                }
                distanceToTarget = Vector3.Distance(transform.position, targetPosition);
                yield return null;
            }

            if (rb != null) rb.linearVelocity = Vector3.zero;
            isMoving = false;
            isWaitingAtPoint = true;
            UpdateAnimator(isMoving, isWaitingAtPoint);

            yield return new WaitForSeconds(waitTimeAtPoint);
            currentPatrolTarget = (currentPatrolTarget == patrolTargetA) ? patrolTargetB : patrolTargetA;
        }
        if (rb != null) rb.linearVelocity = Vector3.zero;
        UpdateAnimator(false, false);
    }

    private IEnumerator HandleBossAttack()
    {
        Debug.Log($"[BossAI] {gameObject.name} initiating a volley of {missilesPerVolley} missiles!");
        for (int i = 0; i < missilesPerVolley; i++)
        {
            if (isDead || playerTransform == null) yield break;
            ShootBossMissile();
            yield return new WaitForSeconds(volleyFireInterval);
        }
        currentAttackCoroutine = null;
    }

    void ShootBossMissile()
    {
        if (bossMissilePrefab != null && firePoint != null && playerTransform != null)
        {
            GameObject missile = Instantiate(bossMissilePrefab, firePoint.position, Quaternion.Euler(0f, firePoint.rotation.eulerAngles.y, 0f));
            BossMissileBehavior missileScript = missile.GetComponent<BossMissileBehavior>();
            if (missileScript != null)
            {
                float calculatedDamage = enemyStats != null ? enemyStats.GetCalculatedProjectileDamage() : 10f;
                missileScript.SetDamage(calculatedDamage);
                missileScript.SetTarget(playerTransform);
                Vector3 initialDirection = firePoint.forward;
                missileScript.SetInitialDirection(initialDirection, missileScript.speed);
            }
            else Debug.LogWarning("[BossAI] Boss Missile Prefab does not have a BossMissileBehavior script!");

            AudioManager.Instance.PlaySFX(SFXType.EnemyFire);
        }
        else
        {
            if (bossMissilePrefab == null) Debug.LogError("[BossAI] Boss Missile Prefab not set!");
            if (firePoint == null) Debug.LogError("[BossAI] Fire Point not set!");
            if (playerTransform == null) Debug.LogError("[BossAI] Player transform not found, cannot shoot missile!");
        }
    }
    
    void OnTriggerStay(Collider other)
    {
        if (isDead) return;

        if (other.gameObject.CompareTag("Player"))
        {
            Health playerHealth = other.gameObject.GetComponent<Health>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(contactDamage, transform.position);
            }
        }
    }

    void UpdateAnimator(bool moving, bool waiting)
    {
        if (animator != null)
        {
            animator.SetBool(runAnimParameter, moving);
            animator.SetBool(idleAnimParameter, waiting);
        }
    }

    void OnBossDeath()
    {
        if (isDead) return;

        Debug.Log(gameObject.name + " has been defeated!");
        isDead = true;
        StopAllCoroutines();
        AudioManager.Instance.PlaySFX(SFXType.EnemyDeath);

        // Tell the LevelManager that the boss is defeated
        if (levelManager != null)
        {
            levelManager.HandleBossDefeated();
        }
        else
        {
            Debug.LogError("[BossAI] LevelManager reference is not set! Cannot trigger win condition.");
        }

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.isKinematic = true;
        }
        if (bossCollider != null)
        {
            bossCollider.enabled = false;
        }
        if (animator != null)
        {
            UpdateAnimator(false, false);
            animator.enabled = false; // Disable animator to allow manual rotation
        }
        
        // Start ONLY the death animation sequence
        StartCoroutine(HandleDeathSequence());
    }
    
    private IEnumerator HandleDeathSequence()
    {
        // Procedural "fall over" animation
        float fallDuration = 1.0f; 
        float elapsedTime = 0f;
        Quaternion startRotation = transform.rotation;
        Quaternion targetRotation = startRotation * Quaternion.Euler(90, 0, 0);

        while (elapsedTime < fallDuration)
        {
            transform.rotation = Quaternion.Slerp(startRotation, targetRotation, elapsedTime / fallDuration);
            elapsedTime += Time.deltaTime;
            yield return null; 
        }

        if (deathParticlePrefab != null)
        {
            Instantiate(deathParticlePrefab, transform.position, Quaternion.identity);
        }

        Debug.Log($"[BossAI] {gameObject.name} GameObject being destroyed.");
        Destroy(gameObject);
    }

    private IEnumerator StunEnemyForDuration(float duration)
    {
        Debug.Log($"[BossAI] {gameObject.name} is 'stunned' for {duration} seconds.");
        yield return new WaitForSeconds(duration);
        Debug.Log($"[BossAI] {gameObject.name} is no longer 'stunned'.");
    }
}