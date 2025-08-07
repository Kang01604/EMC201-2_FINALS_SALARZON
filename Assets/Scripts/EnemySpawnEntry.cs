using UnityEngine;
using System; // Required for [Serializable]

// This class will hold information for each enemy type that can be spawned.
// [Serializable] makes it visible and editable in the Unity Inspector.
[Serializable]
public class EnemySpawnEntry
{
    [Tooltip("The enemy GameObject prefab to spawn.")]
    public GameObject enemyPrefab;

    [Tooltip("The higher this value, the more likely this enemy is to spawn.")]
    [Range(1, 100)] // Restrict weight from 1 to 100 for easier balancing
    public int spawnWeight = 10;
}