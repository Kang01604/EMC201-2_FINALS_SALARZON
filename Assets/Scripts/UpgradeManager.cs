// FULL CODE FOR UpgradeManager.cs
using UnityEngine;
using System.Collections.Generic; // For List
using TMPro; // Required for TextMeshProUGUI
using UnityEngine.UI; // Required for UI elements like Button, Image
// Removed Unity.Events as it's not explicitly used in your provided code, keeping it minimal.

public class UpgradeManager : MonoBehaviour
{
    [Header("References")]
    public PlayerStats playerStats; // Drag your Player GameObject here
    public Health playerHealth; // Reference to the player's Health component
    [Tooltip("List of all possible UpgradeOption ScriptableObjects.")]
    public List<UpgradeOption> allUpgradeOptions; // Drag all your created UpgradeOption assets here

    [Header("UI References - Main Panel")]
    public GameObject mainUpgradePanel; // The main parent UI GameObject for the entire upgrade screen
    public CanvasGroup panelCanvasGroup; // Used for fading the panel in/out and controlling interactivity

    [Header("UI References - Titles")]
    public TextMeshProUGUI stonksTitleText; // For the "STONKS!" title
    public TextMeshProUGUI investmentTitleText; // For the "MAKE YOUR INVESTMENT!" title

    [Header("UI References - Player Stats Section")]
    public TextMeshProUGUI hpValueText;       // For "HP :" (now pulling from Health script)
    public TextMeshProUGUI damageValueText;   // For "DAMAGE :"
    public TextMeshProUGUI fireRateValueText; // For "FIRE RATE :"
    public TextMeshProUGUI speedValueText;    // For "SPEED :"
    public TextMeshProUGUI luckValueText;     // For "LUCK :"

    [Header("UI References - Upgrade Options Section")]
    public UpgradeButton[] upgradeButtons; // Array of the individual upgrade choice buttons

    [Header("Upgrade Selection Settings")]
    public int numOptionsToPresent = 4; // Set to 4 as per the design with 4 choices

    // NEW: Optional Controls toggle
    [Header("Control Settings")]
    [Tooltip("Enable keyboard navigation (W/S to move, Space to confirm).")]
    public bool enableKeyboardNavigation = true;

    private bool isUpgradePanelActive = false;
    private PlayerMovement playerMovement; // To pause/unpause player movement

    // NEW: Navigation & Selection variables
    private int selectedIndex = 0; // Index of the currently highlighted button for keyboard navigation
    private float verticalInputCooldown = 0.2f; // To prevent rapid selection changes
    private float lastVerticalInputTime = 0f;

    void Awake()
    {
        if (playerStats == null)
        {
            // Updated from FindObjectOfType to FindFirstObjectByType
            playerStats = FindFirstObjectByType<PlayerStats>();
            if (playerStats == null)
            {
                Debug.LogError("[UpgradeManager] PlayerStats component not found in scene!", this);
                return;
            }
        }
        
        // Find PlayerHealth if not assigned
        if (playerHealth == null)
        {
            playerHealth = playerStats.GetComponent<Health>(); // Assuming Health is on the same GameObject as PlayerStats
            if (playerHealth == null)
            {
                Debug.LogError("[UpgradeManager] Health component not found on PlayerStats GameObject!", this);
            }
        }

        playerMovement = playerStats.GetComponent<PlayerMovement>();
        if (playerMovement == null)
        {
            Debug.LogError("[UpgradeManager] PlayerMovement component not found on PlayerStats GameObject!", this);
        }

        // Ensure CanvasGroup is present or found
        if (mainUpgradePanel != null)
        {
            if (panelCanvasGroup == null) // Only try to get/add if not already assigned in Inspector
            {
                panelCanvasGroup = mainUpgradePanel.GetComponent<CanvasGroup>();
                if (panelCanvasGroup == null)
                {
                    Debug.LogWarning("[UpgradeManager] CanvasGroup component not found on MainUpgradePanel. Adding one.", mainUpgradePanel);
                    panelCanvasGroup = mainUpgradePanel.AddComponent<CanvasGroup>();
                }
            }
            
            // Initial state set here in Awake
            // IMPORTANT: Explicitly ensure the panel is inactive at the very start.
            // This line ensures it's off regardless of editor's initial state.
            panelCanvasGroup.alpha = 0f;
            panelCanvasGroup.interactable = false;
            panelCanvasGroup.blocksRaycasts = false;
            mainUpgradePanel.SetActive(false); // Ensure the panel is inactive at start
        }
        else
        {
            Debug.LogError("[UpgradeManager] MainUpgradePanel UI GameObject not assigned!", this);
        }

        if (upgradeButtons.Length < numOptionsToPresent)
        {
            Debug.LogError($"[UpgradeManager] Not enough Upgrade Buttons assigned! Expected at least {numOptionsToPresent}, but found {upgradeButtons.Length}. Please assign more buttons or reduce 'Num Options To Present'.", this);
        }
    }

    void OnEnable()
    {
        if (playerStats != null)
        {
            playerStats.OnLevelUp.AddListener(ShowUpgradePanel);
        }
    }

    void OnDisable()
    {
        if (playerStats != null)
        {
            playerStats.OnLevelUp.RemoveListener(ShowUpgradePanel);
        }
    }

    void Update()
    {
        if (!isUpgradePanelActive || !enableKeyboardNavigation) return; // Only process input if panel is active and keyboard nav is enabled

        // NEW: Navigation Input (W/S or Up/Down Arrow)
        float verticalInput = Input.GetAxisRaw("Vertical");

        if (Time.unscaledTime >= lastVerticalInputTime + verticalInputCooldown) // Use unscaledTime as game is paused
        {
            if (verticalInput > 0.1f) // Up
            {
                lastVerticalInputTime = Time.unscaledTime;
                NavigateButtons(-1); // Move selection up
            }
            else if (verticalInput < -0.1f) // Down
            {
                lastVerticalInputTime = Time.unscaledTime;
                NavigateButtons(1); // Move selection down
            }
        }

        // NEW: Confirm Selection Input (Spacebar)
        if (Input.GetKeyDown(KeyCode.Space) && upgradeButtons.Length > 0 && selectedIndex >= 0 && selectedIndex < numOptionsToPresent)
        {
            // Trigger the click event of the selected button
            if (upgradeButtons[selectedIndex].button != null)
            {
                upgradeButtons[selectedIndex].button.onClick.Invoke();
                Debug.Log($"[UpgradeManager] Spacebar confirmed selection: Button {selectedIndex}");
            }
        }
    }

    private void ShowUpgradePanel(int newLevel)
    {
        Debug.Log("[UpgradeManager] Showing Upgrade Panel. Time.timeScale set to 0. Player movement paused.");
        if (isUpgradePanelActive) return;

        isUpgradePanelActive = true;
        Time.timeScale = 0f; // Pause game

        if (playerMovement != null)
        {
            playerMovement.SetMovementAllowed(false); // Disable player movement
        }

        if (mainUpgradePanel != null)
        {
            mainUpgradePanel.SetActive(true); // Activate the GameObject first
            panelCanvasGroup.alpha = 1f;
            panelCanvasGroup.interactable = true;
            panelCanvasGroup.blocksRaycasts = true;
        }

        if (stonksTitleText != null) { stonksTitleText.text = "STONKS!"; }
        if (investmentTitleText != null) { investmentTitleText.text = "MAKE YOUR INVESTMENT!"; }
        
        UpdatePlayerStatsDisplay();
        PopulateUpgradeOptions();

        // NEW: Select the first button initially if keyboard navigation is enabled
        if (enableKeyboardNavigation && upgradeButtons.Length > 0)
        {
            selectedIndex = 0;
            SelectButton(selectedIndex);
        }
    }

    private void HideUpgradePanel()
    {
        Debug.Log("[UpgradeManager] Hiding Upgrade Panel. Time.timeScale set to 1. Player movement resumed.");
        if (!isUpgradePanelActive) return;

        isUpgradePanelActive = false;

        if (playerMovement != null) playerMovement.SetMovementAllowed(true);
        Time.timeScale = 1f;

        if (mainUpgradePanel != null)
        {
            panelCanvasGroup.alpha = 0f;
            panelCanvasGroup.interactable = false;
            panelCanvasGroup.blocksRaycasts = false;
            mainUpgradePanel.SetActive(false); // Deactivate the GameObject
        }

        // NEW: Deselect all buttons when hiding the panel
        foreach (UpgradeButton button in upgradeButtons)
        {
            if (button != null) // Check for null in case some slots are empty
            {
                button.Deselect();
            }
        }
    }

    private void UpdatePlayerStatsDisplay()
    {
        if (playerStats != null)
        {
            // Use currentHealth and maxHealth from the Health component
            // This line was already correct!
            if (hpValueText != null && playerHealth != null) hpValueText.text = $"HP  : {playerHealth.currentHealth} / {playerHealth.maxHealth}";
            
            if (damageValueText != null) damageValueText.text = $"DMG : {playerStats.DamageMultiplier:F1}";
            if (fireRateValueText != null) fireRateValueText.text = $"FR  : {playerStats.FireRate:F1}";
            if (speedValueText != null) speedValueText.text = $"SPD : {playerStats.MovementSpeed:F1}";
            if (luckValueText != null) luckValueText.text = $"LCK : {playerStats.Luck:F1}";
        }
    }

    private void PopulateUpgradeOptions()
    {
        List<UpgradeOption> availableUpgrades = new List<UpgradeOption>(allUpgradeOptions);
        // Using your ListExtensions.Shuffle()
        availableUpgrades.Shuffle(); 

        for (int i = 0; i < upgradeButtons.Length; i++)
        {
            if (i < numOptionsToPresent && i < availableUpgrades.Count)
            {
                upgradeButtons[i].gameObject.SetActive(true);
                upgradeButtons[i].SetUpgradeOption(availableUpgrades[i], this);
            }
            else
            {
                upgradeButtons[i].gameObject.SetActive(false);
            }
        }
    }

    public void ApplyUpgrade(UpgradeOption chosenOption)
    {
        if (playerStats == null) return;

        // Play UpgradeButton SFX
        AudioManager.Instance.PlaySFX(SFXType.UpgradeButton);

        switch (chosenOption.statType)
        {
            case PlayerStatType.DamageMultiplier: 
                playerStats.IncreaseDamageMultiplier(chosenOption.statBoostAmount);
                break;
            case PlayerStatType.FireRate:
                playerStats.IncreaseFireRate(chosenOption.statBoostAmount);
                break;
            case PlayerStatType.MovementSpeed: 
                playerStats.IncreaseMovementSpeed(chosenOption.statBoostAmount);
                break;
            case PlayerStatType.Luck:
                playerStats.IncreaseLuck(chosenOption.statBoostAmount);
                break;
            case PlayerStatType.MaxHealth: 
                // Cast statBoostAmount to int as IncreaseMaxHealth expects an int
                playerStats.IncreaseMaxHealth((int)chosenOption.statBoostAmount); 
                break; 
        }

        Debug.Log($"Applied Upgrade: {chosenOption.promptDescription} (+{chosenOption.statBoostAmount} to {chosenOption.statType})");
        
        UpdatePlayerStatsDisplay(); // Immediately update the display after applying the upgrade
        
        HideUpgradePanel(); // Hide the panel after an upgrade is chosen
    }

    // NEW: Selection Logic for keyboard navigation and mouse hover sync
    private void SelectButton(int newIndex)
    {
        // Deselect the previously selected button if it's valid
        if (selectedIndex >= 0 && selectedIndex < upgradeButtons.Length && upgradeButtons[selectedIndex].gameObject.activeSelf)
        {
            upgradeButtons[selectedIndex].Deselect();
        }

        // Set the new selected index
        selectedIndex = newIndex;

        // Select the new button if it's valid and active
        if (selectedIndex >= 0 && selectedIndex < upgradeButtons.Length && upgradeButtons[selectedIndex].gameObject.activeSelf)
        {
            upgradeButtons[selectedIndex].Select();
            // Play cursor hover SFX when navigating with keyboard
            AudioManager.Instance.PlaySFX(SFXType.CursorHover); 
        }
    }

    private void NavigateButtons(int direction) // direction: -1 for up, 1 for down
    {
        if (numOptionsToPresent == 0) return; // No upgrades to navigate

        int newIndex = selectedIndex + direction;

        // Wrap around logic based on the number of *currently displayed* upgrade options
        if (newIndex < 0)
        {
            newIndex = numOptionsToPresent - 1;
        }
        else if (newIndex >= numOptionsToPresent)
        {
            newIndex = 0;
        }

        SelectButton(newIndex);
    }

    // NEW: Called by UpgradeButton when mouse hovers over it
    public void OnButtonHovered(UpgradeButton hoveredButton)
    {
        if (!enableKeyboardNavigation) return; // Only sync if keyboard navigation is active

        for (int i = 0; i < upgradeButtons.Length; i++)
        {
            if (upgradeButtons[i] == hoveredButton)
            {
                if (selectedIndex != i) // If the hovered button is not the current keyboard-selected one
                {
                    SelectButton(i); // Set it as the keyboard-selected button
                }
                return;
            }
        }
    }
}