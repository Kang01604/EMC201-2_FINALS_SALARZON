using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement; // For scene loading

public class GameWinUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI headerText;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button mainMenuButton;

    // Variable for the Main Menu scene name
    [Header("Scene References")]
    [Tooltip("The name of the scene to load when 'Main Menu' is clicked.")]
    [SerializeField] private string mainMenuSceneName = "MainMenu"; // Default name, adjust as needed

    void Awake()
    {
        // Initially hide the panel when it's instantiated
        gameObject.SetActive(false);
    }

    void Start()
    {
        // Assign button listeners
        if (continueButton != null)
        {
            continueButton.onClick.AddListener(OnContinueClicked);
        }
        else
        {
            Debug.LogWarning("[GameWinUI] Continue button is not assigned!");
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.AddListener(OnMainMenuClicked);
        }
        else
        {
            Debug.LogWarning("[GameWinUI] Main Menu button is not assigned!");
        }

        // Set header text
        if (headerText != null)
        {
            headerText.text = "STONKS! You have defeated the BOSS!";
        }
        else
        {
            Debug.LogWarning("[GameWinUI] Header Text (TextMeshProUGUI) is not assigned!");
        }
    }

    /// <summary>
    /// Activates the Game Win panel and pauses the game.
    /// </summary>
    public void ShowGameWin()
    {
        gameObject.SetActive(true);
        Time.timeScale = 0f; // Pause the game

        Debug.Log("[GameWinUI] Game Win panel displayed. Game paused.");

        // Ensure cursor is visible and unlocked for UI interaction
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    private void OnContinueClicked()
    {
        Debug.Log("[GameWinUI] 'Continue' button clicked. Restarting current scene.");
        Time.timeScale = 1f; // Resume time before reloading
        gameObject.SetActive(false); // Hide the panel immediately

        // Explicitly stop the BGM before reloading.
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopBGM();
            Debug.Log("[GameWinUI] BGM explicitly stopped to ensure restart on scene load.");
        }

        // Load the current active scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void OnMainMenuClicked()
    {
        Debug.Log($"[GameWinUI] 'Main Menu' button clicked. Loading scene: {mainMenuSceneName}");
        Time.timeScale = 1f; // Resume time before loading a new scene

        // Destroy the GameWinUI GameObject since we're leaving the gameplay loop.
        Destroy(gameObject);
        Debug.Log("[GameWinUI] GameWinUI panel explicitly DESTROYED before loading Main Menu scene.");

        // Stop BGM when going back to the Main Menu scene.
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopBGM();
        }

        // Load the designated main menu scene
        if (!string.IsNullOrEmpty(mainMenuSceneName))
        {
            SceneManager.LoadScene(mainMenuSceneName);
        }
        else
        {
            Debug.LogError("[GameWinUI] 'Main Menu Scene Name' is not set in the Inspector. Cannot load scene!");
        }
    }

    void OnDestroy()
    {
        // Clean up event listeners to prevent memory leaks
        if (continueButton != null)
            continueButton.onClick.RemoveListener(OnContinueClicked);
        if (mainMenuButton != null)
            mainMenuButton.onClick.RemoveListener(OnMainMenuClicked);
    }
}