using UnityEngine;
using UnityEngine.SceneManagement; // Required for scene loading

public class MainMenuManager : MonoBehaviour
{
    [Header("Scene Management")]
    [Tooltip("The build index of the scene to load when 'Play' is clicked.")]
    public int gameSceneIndex = 1; // IMPORTANT: Set this to the actual build index of your main game scene

    [Header("UI Panels")]
    [Tooltip("Reference to the About Panel GameObject to show/hide.")]
    public GameObject aboutPanel; // Drag your AboutPanel GameObject here in the Inspector

    [Tooltip("Reference to the Settings Panel GameObject to show/hide.")]
    public GameObject settingsPanel; // NEW: Drag your SettingsPanel GameObject here in the Inspector

    // This method will be called when the Play button is clicked
    public void PlayGame()
    {
        Debug.Log($"PLAY button clicked! Loading scene with index: {gameSceneIndex}...");
        SceneManager.LoadScene(gameSceneIndex); // Load the specified game scene by its index
    }

    // This method will be called when the Quit button is clicked
    public void QuitGame()
    {
        Debug.Log("QUIT button clicked! Quitting application...");

        // If running in a built game (exe), this will close the application
        Application.Quit();

        // If running in the Unity Editor, this will stop Play Mode
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }

    // This method will be called when the About button is clicked
    public void OpenAbout()
    {
        Debug.Log("ABOUT button clicked! Opening About Panel.");
        if (aboutPanel != null)
        {
            aboutPanel.SetActive(true); // Show the About Panel
        }
        else
        {
            Debug.LogWarning("About Panel reference is missing in MainMenuManager!");
        }
    }

    // This method will be called when the AboutCloseButton is clicked
    public void CloseAbout()
    {
        Debug.Log("CLOSE button clicked! Closing About About Panel.");
        if (aboutPanel != null)
        {
            aboutPanel.SetActive(false); // Hide the About Panel
        }
        else
        {
            Debug.LogWarning("About Panel reference is missing in MainMenuManager!");
        }
    }

    // This method will be called when the Settings button is clicked
    public void OpenSettings()
    {
        Debug.Log("SETTINGS button clicked! Opening Settings Panel.");
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true); // Show the Settings Panel
        }
        else
        {
            Debug.LogWarning("Settings Panel reference is missing in MainMenuManager!");
        }
    }

    // NEW: This method will be called when the SettingsCloseButton is clicked
    public void CloseSettings()
    {
        Debug.Log("CLOSE button clicked! Closing Settings Panel.");
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false); // Hide the Settings Panel
        }
        else
        {
            Debug.LogWarning("Settings Panel reference is missing in MainMenuManager!");
        }
    }
}