using UnityEngine;
using System.Collections.Generic; // For List

// This class defines a single entry in the enemy's drop table
[System.Serializable] // Make it visible in the Inspector
public class DropTableEntry
{
    [Tooltip("The prefab of the item to drop.")]
    public GameObject dropPrefab;
    [Range(0f, 1f)]
    [Tooltip("The chance (0.0 to 1.0) for this item to drop.")]
    public float dropChance = 0.5f; // 50% chance by default
}

[RequireComponent(typeof(Health))] // Enemy must have Health to drop items on death
public class EnemyDrops : MonoBehaviour
{
    [Header("Enemy Drop Table")]
    public List<DropTableEntry> dropTable = new List<DropTableEntry>();

    private Health enemyHealth;

    void Awake()
    {
        enemyHealth = GetComponent<Health>();
        if (enemyHealth == null)
        {
            Debug.LogError("[EnemyDrops] Health component not found on enemy. Drops cannot be managed on death.", this);
        }
    }

    void OnEnable()
    {
        if (enemyHealth != null)
        {
            enemyHealth.OnDeath.AddListener(HandleEnemyDeath);
        }
    }

    void OnDisable()
    {
        if (enemyHealth != null)
        {
            enemyHealth.OnDeath.RemoveListener(HandleEnemyDeath);
        }
    }

    void HandleEnemyDeath()
    {
        Debug.Log($"[EnemyDrops] {gameObject.name} died. Checking for drops...");
        foreach (DropTableEntry entry in dropTable)
        {
            float roll = Random.Range(0f, 1f); // Generate a random number between 0.0 and 1.0
            if (roll <= entry.dropChance)
            {
                if (entry.dropPrefab != null)
                {
                    // Instantiate the drop item at the enemy's position
                    Instantiate(entry.dropPrefab, transform.position, Quaternion.identity);
                    Debug.Log($"[EnemyDrops] Dropped {entry.dropPrefab.name} (Chance: {entry.dropChance * 100}%).");
                }
                else
                {
                    Debug.LogWarning($"[EnemyDrops] Drop prefab is null for an entry in {gameObject.name}'s drop table.", this);
                }
            }
            else
            {
                // Debug.Log($"[EnemyDrops] {entry.dropPrefab?.name ?? "Null Prefab"} did not drop (Chance: {entry.dropChance * 100}% vs Roll: {roll * 100}%).");
            }
        }
    }
}