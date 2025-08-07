using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement; // For scene loading

public class GameOverUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI headerText;
    [SerializeField] private Button reinvestButton;
    [SerializeField] private Button fileBankruptcyButton;

    // Variable for the Start Game scene name
    [Header("Scene References")]
    [Tooltip("The name of the scene to load when 'File Bankruptcy' is clicked.")]
    [SerializeField] private string startLevelSceneName = "START GAME"; // Default name for convenience

    void Awake()
    {
        // Initially hide the panel when it's instantiated
        gameObject.SetActive(false);
    }

    void Start()
    {
        // Assign button listeners
        if (reinvestButton != null)
        {
            reinvestButton.onClick.AddListener(OnReinvestClicked);
        }
        else
        {
            Debug.LogWarning("[GameOverUI] Reinvest button is not assigned!");
        }

        if (fileBankruptcyButton != null)
        {
            fileBankruptcyButton.onClick.AddListener(OnFileBankruptcyClicked);
        }
        else
        {
            Debug.LogWarning("[GameOverUI] File Bankruptcy button is not assigned!");
        }

        // Set header text (redundant if set in Unity, but good for robustness)
        if (headerText != null)
        {
            headerText.text = "NOT STONKS! You have gone BROKE!";
        }
        else
        {
            Debug.LogWarning("[GameOverUI] Header Text (TextMeshProUGUI) is not assigned!");
        }
    }

    /// <summary>
    /// Activates the Game Over panel and pauses the game.
    /// </summary>
    public void ShowGameOver()
    {
        gameObject.SetActive(true);
        Time.timeScale = 0f; // Pause the game

        Debug.Log("[GameOverUI] Game Over panel displayed. Game paused.");

        // Ensure cursor is visible and unlocked for UI interaction
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    private void OnReinvestClicked()
    {
        Debug.Log("[GameOverUI] 'Reinvest' button clicked. Restarting scene.");
        Time.timeScale = 1f; // Resume time before reloading
        gameObject.SetActive(false); // Hide the panel immediately
        
        // Explicitly stop the BGM before reloading.
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopBGM();
            Debug.Log("[GameOverUI] BGM explicitly stopped to ensure restart on scene load.");
        }

        // Load the current active scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void OnFileBankruptcyClicked()
    {
        Debug.Log($"[GameOverUI] 'File Bankruptcy' button clicked. Loading scene: {startLevelSceneName}");
        Time.timeScale = 1f; // Resume time before loading a new scene
        
        // --- CRUCIAL CHANGE HERE ---
        // Instead of just hiding, destroy the GameOverUI GameObject
        // since we're leaving the gameplay loop entirely.
        Destroy(gameObject); 
        Debug.Log("[GameOverUI] GameOverUI panel explicitly DESTROYED before loading Start Game scene.");

        // Stop BGM when going back to the START GAME scene.
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopBGM();
        }
        
        // Load the designated start level scene
        if (!string.IsNullOrEmpty(startLevelSceneName))
        {
            SceneManager.LoadScene(startLevelSceneName);
        }
        else
        {
            Debug.LogError("[GameOverUI] 'Start Level Scene Name' is not set in the Inspector. Cannot load scene!");
        }
    }

    void OnDestroy()
    {
        // Clean up event listeners to prevent memory leaks
        if (reinvestButton != null)
            reinvestButton.onClick.RemoveListener(OnReinvestClicked);
        if (fileBankruptcyButton != null)
            fileBankruptcyButton.onClick.RemoveListener(OnFileBankruptcyClicked);
    }
}