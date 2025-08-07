using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("UI Elements (Found by Tag)")]
    private TextMeshProUGUI levelText;
    private Slider fundBar;
    private TextMeshProUGUI fundText;
    private Image flashImage;
    private TextMeshProUGUI eventText;

    private PlayerStats playerStats;

    [Header("Panel Prefabs")]
    [SerializeField] private GameObject gameOverPanelPrefab;
    private GameOverUI gameOverUIInstance;
    [SerializeField] private GameObject gameWinPanelPrefab;
    private GameWinUI gameWinUIInstance;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"[UIManager] Scene '{scene.name}' loaded. Re-initializing UI.");
        if (scene.name != "START GAME")
        {
            FindCoreUIElements();
            FindEventUIElements(); 
            InstantiateUIPanels();
        }
    }

    private void FindCoreUIElements()
    {
        GameObject fundBarObj = GameObject.FindWithTag("UI_FundBar");
        if (fundBarObj != null)
        {
            fundBar = fundBarObj.GetComponent<Slider>();
            if (fundBar != null) fundBar.interactable = false;
        }
        else Debug.LogWarning("[UIManager] Could not find GameObject with tag 'UI_FundBar'.");

        GameObject fundTextObj = GameObject.FindWithTag("UI_FundText");
        if (fundTextObj != null) fundText = fundTextObj.GetComponent<TextMeshProUGUI>();
        else Debug.LogWarning("[UIManager] Could not find GameObject with tag 'UI_FundText'.");

        GameObject levelTextObj = GameObject.FindWithTag("UI_LevelText");
        if (levelTextObj != null) levelText = levelTextObj.GetComponent<TextMeshProUGUI>();
        else Debug.LogWarning("[UIManager] Could not find GameObject with tag 'UI_LevelText'.");

        if (playerStats != null)
        {
            UpdateLevel(playerStats.PlayerLevel);
            UpdateFunds(playerStats.Funds);
        }
    }

    // --- MODIFIED SECTION ---
    // This method now finds an active parent first, then its inactive children.
    private void FindEventUIElements()
    {
        // 1. Find the active parent container using its unique tag.
        GameObject eventUIContainer = GameObject.FindWithTag("UI_EventContainer");

        if (eventUIContainer != null)
        {
            // 2. Find the components in the container's children, including inactive ones.
            flashImage = eventUIContainer.GetComponentInChildren<Image>(true);
            eventText = eventUIContainer.GetComponentInChildren<TextMeshProUGUI>(true);

            // Optional: More specific checks to ensure you found the right components.
            if (flashImage == null)
            {
                Debug.LogWarning("[UIManager] Could not find 'Image' component in children of 'UI_EventContainer'.");
            }
            if (eventText == null)
            {
                Debug.LogWarning("[UIManager] Could not find 'TextMeshProUGUI' component in children of 'UI_EventContainer'.");
            }
        }
        else
        {
            Debug.LogWarning("[UIManager] Could not find GameObject with tag 'UI_EventContainer'. Make sure an active object in your scene has this tag.");
        }
    }
    // --- END MODIFIED SECTION ---

    private void InstantiateUIPanels()
    {
        if (gameOverUIInstance == null && gameOverPanelPrefab != null)
        {
            GameObject goPanel = Instantiate(gameOverPanelPrefab);
            goPanel.transform.SetParent(this.transform, false);
            gameOverUIInstance = goPanel.GetComponent<GameOverUI>();
            if (gameOverUIInstance == null)
            {
                Debug.LogError("[UIManager] GameOverPanelPrefab does not have a GameOverUI script! Destroying instance.", this);
                Destroy(goPanel);
            }
        }
        if (gameWinUIInstance == null && gameWinPanelPrefab != null)
        {
            GameObject gwPanel = Instantiate(gameWinPanelPrefab);
            gwPanel.transform.SetParent(this.transform, false);
            gameWinUIInstance = gwPanel.GetComponent<GameWinUI>();
            if (gameWinUIInstance == null)
            {
                Debug.LogError("[UIManager] GameWinPanelPrefab does not have a GameWinUI script! Destroying instance.", this);
                Destroy(gwPanel);
            }
        }
    }

    public void RegisterPlayerStats(PlayerStats stats)
    {
        playerStats = stats;
        if (playerStats != null)
        {
            UpdateLevel(playerStats.PlayerLevel);
            UpdateFunds(playerStats.Funds);
        }
    }

    public void UpdateLevel(int newLevel)
    {
        if (levelText != null)
        {
            levelText.text = $"Lv. {newLevel}";
        }
        if (playerStats != null)
        {
            UpdateFunds(playerStats.Funds);
        }
    }

    public void UpdateFunds(int currentFunds)
    {
        if (playerStats == null || fundBar == null || fundText == null) return;
        float maxFunds = playerStats.fundsToNextLevel;
        if (maxFunds <= 0) maxFunds = 1f;
        float percentage = (currentFunds / maxFunds) * 100f;
        fundText.text = $"{Mathf.RoundToInt(percentage)}%";
        fundBar.value = Mathf.Clamp01(currentFunds / maxFunds);
    }

    public void ShowGameOverPanel()
    {
        if (gameOverUIInstance != null) gameOverUIInstance.ShowGameOver();
    }

    public void ShowGameWinPanel()
    {
        if (gameWinUIInstance != null) gameWinUIInstance.ShowGameWin();
    }

    public void ShowEventMessage(string message, float duration = 3f)
    {
        if (eventText != null && flashImage != null)
        {
            StartCoroutine(EventMessageRoutine(message, duration));
        }
        else
        {
            Debug.LogWarning("[UIManager] Cannot show event message because Event Text or Flash Image is not found. Check tags 'UI_EventContainer', and its children.");
        }
    }

    private IEnumerator EventMessageRoutine(string message, float duration)
    {
        flashImage.canvasRenderer.SetAlpha(0f);
        eventText.canvasRenderer.SetAlpha(0f);
        flashImage.gameObject.SetActive(true);
        eventText.gameObject.SetActive(true);
        eventText.text = message;

        flashImage.CrossFadeAlpha(0.6f, 0.1f, true);
        eventText.CrossFadeAlpha(1f, 0.2f, true);
        yield return new WaitForSeconds(0.1f);

        flashImage.CrossFadeAlpha(0f, 0.5f, true);
        yield return new WaitForSeconds(duration - 0.8f);

        eventText.CrossFadeAlpha(0f, 0.5f, true);
        yield return new WaitForSeconds(0.5f);
        
        eventText.gameObject.SetActive(false);
        flashImage.gameObject.SetActive(false);
    }
}