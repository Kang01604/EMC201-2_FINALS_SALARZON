using UnityEngine;
using System.Collections; // Required for Coroutines
using System.Collections.Generic; // Required for List
using System; // Required for [Serializable]

public class EnemySpawner : MonoBehaviour
{
    // Nested class definition for EnemySpawnEntry
    // [System.Serializable] makes it visible in the Unity Inspector
    [Serializable]
    public class EnemySpawnEntry
    {
        public GameObject enemyPrefab;
        [Tooltip("The base weight for this enemy type. Higher values mean more frequent spawns.")]
        [Range(1, 100)] // Restrict base weight from 1 to 100 for easier balancing
        public int spawnWeight = 10; // Default weight

        [HideInInspector] // Hide in inspector, as this will be dynamically adjusted
        public int currentSpawnWeight; // The actual weight used for spawning
    }

    [Header("Spawn Settings")]
    [Tooltip("List of enemy prefabs and their relative spawn chances.")]
    public List<EnemySpawnEntry> enemySpawnList = new List<EnemySpawnEntry>();

    [Tooltip("The time interval (in seconds) between enemy spawns. This value will be managed by the LevelManager.")]
    public float spawnInterval = 3f;

    [Tooltip("The transform where enemies will be spawned.")]
    public Transform spawnPoint;

    [Tooltip("Enemies will spawn randomly within this radius around the spawnPoint.")]
    [SerializeField] private float spawnRadius = 5f;

    [Header("Target Settings")]
    [Tooltip("The player's transform that spawned enemies will target.")]
    public Transform playerTransform;

    // Internal state
    private int totalCurrentSpawnWeight; // Sum of all CURRENT spawn weights for random selection
    private Coroutine spawnCoroutine; // Reference to the ongoing spawn coroutine

    // Constants for weight adjustment logic
    private const int MAX_WEIGHT_ADJUSTMENT_MINUTES = 5; // Weights stop increasing after this many minutes
    private const int BASE_WEIGHT_THRESHOLD = 10; // Only enemies with base weight less than this will increase
    private const float WEIGHT_INCREASE_PER_MINUTE = 1.5f; // How much weight increases per minute
    private const int MAX_ADJUSTED_WEIGHT = 20; // The hard cap for adjusted enemy spawn weights

    void Awake()
    {
        // If spawnPoint is not assigned, use the spawner's own transform
        if (spawnPoint == null)
        {
            spawnPoint = this.transform;
            Debug.LogWarning("[EnemySpawner] Spawn Point not assigned. Using spawner's own transform as spawn point.", this);
        }

        // Try to find the player if not assigned in the Inspector
        if (playerTransform == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                playerTransform = playerObj.transform;
            }
            else
            {
                Debug.LogWarning("[EnemySpawner] Player GameObject with 'Player' tag not found. Spawned enemies may not have a target.", this);
            }
        }

        // Initialize current spawn weights to their base values at Awake
        InitializeCurrentSpawnWeights();
    }

    void Start()
    {
        // Start the spawning process is now handled by LevelManager
        // This Start() method will only calculate initial weights and log a message.
        CalculateTotalCurrentSpawnWeight(); // Initial calculation
        if (enemySpawnList.Count > 0 && totalCurrentSpawnWeight > 0)
        {
            Debug.Log("[EnemySpawner] Initializing spawner: ready for LevelManager to call StartSpawning().");
        }
        else
        {
            Debug.LogWarning("[EnemySpawner] No valid enemy prefabs in the spawn list or total weight is zero. Spawning will not occur.", this);
        }
    }

    /// <summary>
    /// Initializes the 'currentSpawnWeight' for each entry based on its 'spawnWeight'.
    /// </summary>
    private void InitializeCurrentSpawnWeights()
    {
        foreach (EnemySpawnEntry entry in enemySpawnList)
        {
            entry.currentSpawnWeight = entry.spawnWeight;
        }
    }

    /// <summary>
    /// Starts the spawning coroutine. Called by the LevelManager.
    /// </summary>
    public void StartSpawning()
    {
        if (spawnCoroutine == null)
        {
            if (enemySpawnList.Count > 0 && totalCurrentSpawnWeight > 0)
            {
                spawnCoroutine = StartCoroutine(SpawnEnemyRoutine());
                Debug.Log($"[EnemySpawner] Starting enemy spawning routine for {gameObject.name}.");
            }
            else
            {
                Debug.LogWarning($"[EnemySpawner] Cannot start spawning for {gameObject.name}: No valid enemy prefabs or total weight is zero. Check configuration.", this);
            }
        }
    }

    /// <summary>
    /// Stops the spawning coroutine. This is called by the LevelManager when the boss spawns.
    /// </summary>
    public void StopSpawning()
    {
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null; // Clear the reference
            Debug.Log($"[EnemySpawner] Spawning routine for {gameObject.name} stopped.");
        }
    }

    /// <summary>
    /// Calculates the sum of all CURRENT spawn weights in the enemySpawnList.
    /// This is used for weighted random selection.
    /// </summary>
    private void CalculateTotalCurrentSpawnWeight()
    {
        totalCurrentSpawnWeight = 0;
        foreach (EnemySpawnEntry entry in enemySpawnList)
        {
            if (entry.enemyPrefab != null) // Only count entries with a valid prefab
            {
                totalCurrentSpawnWeight += entry.currentSpawnWeight; // Use currentSpawnWeight
            }
        }
        Debug.Log($"[EnemySpawner] Total calculated CURRENT spawn weight for {gameObject.name}: {totalCurrentSpawnWeight}");
    }

    /// <summary>
    /// Coroutine that handles the timed spawning of enemies.
    /// </summary>
    private IEnumerator SpawnEnemyRoutine()
    {
        while (true) // Loop indefinitely until stopped
        {
            yield return new WaitForSeconds(spawnInterval); // Wait for the specified interval

            if (playerTransform == null)
            {
                Debug.LogWarning("[EnemySpawner] Player transform is null, cannot spawn enemies without a target. Attempting to re-find player.", this);
                GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj != null)
                {
                    playerTransform = playerObj.transform;
                }
                // If player is still null, continue to next iteration after logging
                if (playerTransform == null) continue;
            }

            GameObject enemyToSpawn = ChooseRandomEnemy(); // Select an enemy based on weights

            if (enemyToSpawn != null)
            {
                SpawnEnemy(enemyToSpawn); // Instantiate the chosen enemy
            }
            else
            {
                Debug.LogWarning("[EnemySpawner] Failed to choose an enemy to spawn. Check enemySpawnList and weights.", this);
            }
        }
    }

    /// <summary>
    /// Selects an enemy prefab from the list based on their assigned CURRENT spawn weights.
    /// </summary>
    /// <returns>The GameObject prefab of the chosen enemy, or null if no enemy can be chosen.</returns>
    private GameObject ChooseRandomEnemy()
    {
        if (totalCurrentSpawnWeight == 0 || enemySpawnList.Count == 0)
        {
            Debug.LogWarning($"[EnemySpawner] Cannot choose random enemy for {gameObject.name}: totalCurrentSpawnWeight is 0 or enemySpawnList is empty.", this);
            return null;
        }

        int randomNumber = UnityEngine.Random.Range(0, totalCurrentSpawnWeight); // Generate a random number within the total current weight
        int currentAccumulatedWeight = 0;

        foreach (EnemySpawnEntry entry in enemySpawnList)
        {
            if (entry.enemyPrefab == null) continue; // Skip null prefabs

            currentAccumulatedWeight += entry.currentSpawnWeight; // Use currentSpawnWeight
            if (randomNumber < currentAccumulatedWeight)
            {
                return entry.enemyPrefab; // This enemy is chosen
            }
        }

        // Fallback in case something goes wrong (shouldn't happen if weights are positive and calculated correctly)
        Debug.LogError($"[EnemySpawner] Failed to choose an enemy from the list for {gameObject.name}. This indicates an issue with weight calculation or list setup.", this);
        return null;
    }

    /// <summary>
    /// Instantiates the given enemy prefab at a random position within the spawn radius.
    /// It also attempts to set the player as the enemy's target.
    /// </summary>
    /// <param name="enemyPrefabToSpawn">The enemy GameObject prefab to instantiate.</param>
    private void SpawnEnemy(GameObject enemyPrefabToSpawn)
    {
        // Calculate a random position within the spawn radius
        Vector3 randomOffset = UnityEngine.Random.insideUnitSphere * spawnRadius;
        randomOffset.y = 0; // Keep enemies on the ground plane (assuming 2.5D or top-down)
        Vector3 spawnPosition = spawnPoint.position + randomOffset;

        // Instantiate the enemy
        GameObject spawnedEnemy = Instantiate(enemyPrefabToSpawn, spawnPosition, Quaternion.identity);
        Debug.Log($"[EnemySpawner] Spawned {spawnedEnemy.name} at {spawnPosition} from spawner {gameObject.name}.");

        // Attempt to set the player as the target for the spawned enemy's AI
        // Ensure you have an EnemyAI script on your enemy prefabs that exposes a 'playerTransform' field
        EnemyAI enemyAI = spawnedEnemy.GetComponent<EnemyAI>();
        if (enemyAI != null)
        {
            if (playerTransform != null)
            {
                enemyAI.playerTransform = playerTransform;
            }
            else
            {
                Debug.LogWarning($"[EnemySpawner] Spawned enemy {spawnedEnemy.name} could not find playerTransform. It might not move or attack correctly.", spawnedEnemy);
            }
        }
        else
        {
            Debug.LogWarning($"[EnemySpawner] Spawned enemy {spawnedEnemy.name} does not have an EnemyAI script. It will not move or attack.", spawnedEnemy);
        }
    }

    /// <summary>
    /// Adjusts the spawn weights of enemies in this spawner based on the minutes passed in the level.
    /// Weights for enemies with a base 'spawnWeight' less than 10 will increase by 1.5 per minute
    /// until they reach a maximum of 20 or until 5 minutes have passed.
    /// This method is called by the LevelManager.
    /// </summary>
    /// <param name="minutesPassed">The number of minutes elapsed in the level.</param>
    public void UpdateSpawnWeights(int minutesPassed)
    {
        // Cap the minutes for adjustment to prevent endless increases
        int effectiveMinutes = Mathf.Min(minutesPassed, MAX_WEIGHT_ADJUSTMENT_MINUTES);

        foreach (var entry in enemySpawnList)
        {
            // Only adjust if the original spawnWeight is less than the threshold (10)
            // AND if the currentSpawnWeight is not already at the max adjusted weight (20)
            if (entry.spawnWeight < BASE_WEIGHT_THRESHOLD && entry.currentSpawnWeight < MAX_ADJUSTED_WEIGHT)
            {
                // Calculate the potential new weight
                float potentialNewWeight = entry.spawnWeight + (WEIGHT_INCREASE_PER_MINUTE * effectiveMinutes);

                // Ensure the new weight does not exceed the maximum allowed adjusted weight
                entry.currentSpawnWeight = Mathf.Min(Mathf.RoundToInt(potentialNewWeight), MAX_ADJUSTED_WEIGHT);

                Debug.Log($"[EnemySpawner] Adjusted spawn weight for {entry.enemyPrefab.name} in {gameObject.name} to {entry.currentSpawnWeight} (base: {entry.spawnWeight}).");
            }
        }
        // Recalculate total current weight after adjustments
        CalculateTotalCurrentSpawnWeight();
    }


    // Optional: Draw the spawn radius in the editor for visualization
    void OnDrawGizmosSelected()
    {
        if (spawnPoint != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(spawnPoint.position, spawnRadius);
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(spawnPoint.position, 0.5f); // Indicate the center
        }
    }
}