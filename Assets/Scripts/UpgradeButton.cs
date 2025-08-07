// FULL CODE FOR UpgradeButton.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems; // Required for IPointerEnterHandler, IPointerExitHandler

public class UpgradeButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI Elements")]
    public Button button;
    public TextMeshProUGUI promptText;
    public TextMeshProUGUI statBoostText;
    public Image sourceImage; // The Image component whose color will change (e.g., background)

    [Header("Type Colors")]
    public Color damageColor = Color.red;
    public Color fireRateColor = Color.Lerp(Color.red, Color.yellow, 0.5f); // Orange
    public Color movementSpeedColor = Color.blue;
    public Color luckColor = Color.green;
    public Color maxHealthColor = Color.cyan; // ADDED: New color for Max Health
    public Color defaultColor = Color.white; // Fallback or initial color

    [Header("Highlight Settings")]
    public Color highlightColor = Color.yellow; // The color for selected/hovered state
    public float highlightIntensity = 1.2f; // Multiplier for highlight brightness
    private Color originalColor; // To store the color set by statType

    private UpgradeOption currentOption;
    private UpgradeManager upgradeManager;
    private bool isMouseOver = false; // Track mouse hover state
    private bool isKeyboardSelected = false; // Track keyboard selection state

    void Awake()
    {
        if (button == null)
        {
            button = GetComponent<Button>();
            if (button == null)
            {
                Debug.LogError("[UpgradeButton] Button component not found on this GameObject.", this);
            }
        }
        
        // Add a listener to the button click event
        if (button != null)
        {
            button.onClick.AddListener(OnUpgradeButtonClicked);
        }

        // Get Source Image if not assigned
        if (sourceImage == null)
        {
            sourceImage = GetComponent<Image>();
            if (sourceImage == null)
            {
                Debug.LogWarning("[UpgradeButton] No Source Image assigned and no Image component found on this GameObject. Button color won't change.", this);
            }
        }
    }

    // This method is called by UpgradeManager to set up the button's content
    public void SetUpgradeOption(UpgradeOption option, UpgradeManager manager)
    {
        currentOption = option;
        upgradeManager = manager;

        if (promptText != null)
        {
            promptText.text = option.promptDescription;
        }

        if (statBoostText != null)
        {
            string boostSymbol = "";
            // Special handling for MaxHealth if you want to show it differently (e.g., just the number)
            if (option.statType == PlayerStatType.MaxHealth)
            {
                boostSymbol = $"+{option.statBoostAmount}"; // Show actual number for health boost
            }
            else if (option.statBoostAmount == 0.5f) boostSymbol = "+";
            else if (option.statBoostAmount == 1f) boostSymbol = "++";
            else if (option.statBoostAmount == 2f) boostSymbol = "+++";
            else boostSymbol = option.statBoostAmount.ToString("F1"); // Fallback for other values

            // Use switch for statType to handle specific display names if needed
            string statTypeName;
            switch (option.statType)
            {
                case PlayerStatType.DamageMultiplier: statTypeName = "Damage"; break;
                case PlayerStatType.FireRate: statTypeName = "Fire Rate"; break;
                case PlayerStatType.MovementSpeed: statTypeName = "Movement Speed"; break;
                case PlayerStatType.Luck: statTypeName = "Luck"; break;
                case PlayerStatType.MaxHealth: statTypeName = "Max Health"; break; // Custom name for MaxHealth
                default: statTypeName = option.statType.ToString().Replace("Multiplier", ""); break; // Default removal
            }

            statBoostText.text = $"{boostSymbol} {statTypeName}";
        }

        // Set the source image color based on stat type
        if (sourceImage != null)
        {
            switch (option.statType)
            {
                case PlayerStatType.DamageMultiplier:
                    originalColor = damageColor;
                    break;
                case PlayerStatType.FireRate:
                    originalColor = fireRateColor;
                    break;
                case PlayerStatType.MovementSpeed:
                    originalColor = movementSpeedColor;
                    break;
                case PlayerStatType.Luck:
                    originalColor = luckColor;
                    break;
                case PlayerStatType.MaxHealth: // ADDED: Handle MaxHealth color
                    originalColor = maxHealthColor;
                    break;
                default:
                    originalColor = defaultColor; // Fallback color
                    break;
            }
            sourceImage.color = originalColor; // Apply the initial color
        }

        // Ensure button is in correct state after setup
        UpdateHighlight();
    }

    private void OnUpgradeButtonClicked()
    {
        if (currentOption != null && upgradeManager != null)
        {
            upgradeManager.ApplyUpgrade(currentOption);
        }
    }

    // NEW: Method for keyboard selection
    public void Select()
    {
        isKeyboardSelected = true;
        UpdateHighlight();
    }

    // NEW: Method for keyboard deselection
    public void Deselect()
    {
        isKeyboardSelected = false;
        UpdateHighlight();
    }

    // IPointerEnterHandler for mouse hover
    public void OnPointerEnter(PointerEventData eventData)
    {
        isMouseOver = true;
        // Inform UpgradeManager that this button is now hovered, so it can update keyboard selection
        if (upgradeManager != null)
        {
            upgradeManager.OnButtonHovered(this);
        }
        UpdateHighlight();
    }

    // IPointerExitHandler for mouse hover
    public void OnPointerExit(PointerEventData eventData)
    {
        isMouseOver = false;
        UpdateHighlight();
    }

    // Update the visual highlight based on current state (mouse or keyboard)
    private void UpdateHighlight()
    {
        if (sourceImage == null) return;

        if (isMouseOver || isKeyboardSelected)
        {
            // Apply yellow glow (blended with current type color)
            sourceImage.color = Color.Lerp(originalColor, highlightColor, 0.75f) * highlightIntensity;
        }
        else
        {
            // Revert to original type color
            sourceImage.color = originalColor;
        }
    }
}