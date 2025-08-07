using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class LevelManager : MonoBehaviour
{
    [Header("Level Settings")]
    [Tooltip("The total duration of the level before the boss spawns, in minutes.")]
    [SerializeField] private float levelDurationInMinutes = 5f;

    [Tooltip("The percentage by which enemy spawn speed increases every minute.")]
    [SerializeField] [Range(0.01f, 1.0f)] private float spawnSpeedIncreasePercentage = 0.05f;

    [Tooltip("The delay between activating each spawner at the start of the level.")]
    [SerializeField] private float spawnerActivationDelay = 1f;

    [Header("Boss Settings")]
    [Tooltip("The boss prefab to be spawned.")]
    [SerializeField] private GameObject bossPrefab;

    [Tooltip("A list of the 4 spawner objects where the boss can appear.")]
    [SerializeField] private List<EnemySpawner> bossSpawnPoints = new List<EnemySpawner>();

    [Header("UI Settings")]
    [Tooltip("The TextMeshPro UI element to display the timer and boss message.")]
    [SerializeField] private TextMeshProUGUI timerText;

    // Internal state variables
    private float timeLeft;
    private bool isBossSpawned = false;
    private int minutesPassed = 0;
    private bool stampedeEventTriggered = false;

    void Start()
    {
        timeLeft = 0f;
        StartCoroutine(LevelRoutine());
    }
    
    private IEnumerator LevelRoutine()
    {
        float totalLevelSeconds = levelDurationInMinutes * 60f;
        
        EnemySpawner[] allSpawners = FindObjectsByType<EnemySpawner>(FindObjectsSortMode.InstanceID);

        foreach (var spawner in allSpawners)
        {
            spawner.StartSpawning();
            Debug.Log($"[LevelManager] Activating spawner: {spawner.gameObject.name}");
            yield return new WaitForSeconds(spawnerActivationDelay);
        }

        while (timeLeft < totalLevelSeconds)
        {
            UpdateTimerUI();
            yield return new WaitForSeconds(1f);
            timeLeft++;

            if (!stampedeEventTriggered && timeLeft >= 180f)
            {
                stampedeEventTriggered = true;
                StartCoroutine(BrokerStampedeEvent());
            }

            int currentMinutesElapsed = (int)(timeLeft / 60f);
            if (currentMinutesElapsed > minutesPassed)
            {
                minutesPassed = currentMinutesElapsed;
                SpeedUpSpawners();
                AdjustEnemySpawnWeights(minutesPassed);
            }
        }
        
        StartCoroutine(SpawnBossSequence());
    }

    private void SpeedUpSpawners()
    {
        EnemySpawner[] allSpawners = FindObjectsByType<EnemySpawner>(FindObjectsSortMode.None);
        foreach (var spawner in allSpawners)
        {
            float newInterval = Mathf.Max(0.5f, spawner.spawnInterval * (1f - spawnSpeedIncreasePercentage));
            spawner.spawnInterval = newInterval;
            Debug.Log($"[LevelManager] Spawner interval for {spawner.gameObject.name} reduced to {spawner.spawnInterval:F2}s.");
        }
    }

    private void AdjustEnemySpawnWeights(int currentMinutes)
    {
        EnemySpawner[] allSpawners = FindObjectsByType<EnemySpawner>(FindObjectsSortMode.None);
        foreach (var spawner in allSpawners)
        {
            spawner.UpdateSpawnWeights(currentMinutes);
        }
    }
    
    private IEnumerator BrokerStampedeEvent()
    {
        Debug.Log("[LevelManager] 3-minute mark reached. Initiating BROKER STAMPEDE!");
        
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowEventMessage("BROKER STAMPEDE!");
        }

        EnemySpawner[] allSpawners = FindObjectsByType<EnemySpawner>(FindObjectsSortMode.None);
        Dictionary<EnemySpawner, float> originalIntervals = new Dictionary<EnemySpawner, float>();

        foreach (var spawner in allSpawners)
        {
            originalIntervals[spawner] = spawner.spawnInterval;
            // Set a fixed spawn interval of 0.5 seconds for the event
            spawner.spawnInterval = 0.5f;
            Debug.Log($"[LevelManager] STAMPEDE: {spawner.gameObject.name} interval set to {spawner.spawnInterval:F2}s.");
        }
        
        // Duration of the stampede event
        yield return new WaitForSeconds(90f);
        
        Debug.Log("[LevelManager] Broker Stampede event ending. Restoring spawn rates.");
        foreach (var spawner in allSpawners)
        {
            if (originalIntervals.ContainsKey(spawner))
            {
                spawner.spawnInterval = originalIntervals[spawner];
                Debug.Log($"[LevelManager] STAMPEDE END: {spawner.gameObject.name} interval restored to {spawner.spawnInterval:F2}s.");
            }
        }
    }
    
    private IEnumerator SpawnBossSequence()
    {
        if (isBossSpawned) yield break;
        isBossSpawned = true;
        
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowEventMessage("BOSS INCOMING!");
        }
        yield return new WaitForSeconds(3f);
        
        if (bossSpawnPoints.Count > 0)
        {
            int randomIndex = Random.Range(0, bossSpawnPoints.Count);
            EnemySpawner chosenSpawner = bossSpawnPoints[randomIndex];
            Transform spawnPointTransform = chosenSpawner.spawnPoint;

            if (bossPrefab != null)
            {
                GameObject instantiatedBoss = Instantiate(bossPrefab, spawnPointTransform.position, Quaternion.identity);
                Debug.Log($"[LevelManager] Boss spawned at {spawnPointTransform.position} from spawner '{chosenSpawner.gameObject.name}'.");

                BossAI bossAI = instantiatedBoss.GetComponent<BossAI>();
                if (bossAI != null)
                {
                    // Pass a reference of this LevelManager to the boss
                    bossAI.SetLevelManager(this); 
                    bossAI.SetInitialPatrol(spawnPointTransform, bossSpawnPoints);
                }
                else
                {
                    Debug.LogError("[LevelManager] Spawned Boss Prefab does not have a BossAI script attached!");
                }
            }
            else
            {
                Debug.LogError("[LevelManager] Boss Prefab is not assigned in the LevelManager! The boss will not spawn.");
            }
        }
        else
        {
            Debug.LogError("[LevelManager] No boss spawn points assigned in the LevelManager! The boss will not spawn.");
        }

        if (timerText != null)
        {
            timerText.text = "DEFEAT THE BOSS";
            Debug.Log("[LevelManager] Timer UI updated to 'DEFEAT THE BOSS'.");
        }
    }

    private void UpdateTimerUI()
    {
        if (timerText != null)
        {
            int minutes = Mathf.FloorToInt(timeLeft / 60f);
            int seconds = Mathf.FloorToInt(timeLeft % 60f);
            timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
        }
    }

    public void HandleBossDefeated()
    {
        Debug.Log("[LevelManager] Boss has been defeated! Initiating win sequence.");
        StartCoroutine(ShowWinPanelAfterDelay(1.5f));
    }

    private IEnumerator ShowWinPanelAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowGameWinPanel();
        }
        else
        {
            Debug.LogError("[LevelManager] UIManager instance is null. Cannot show Game Win panel!");
        }
    }
}
