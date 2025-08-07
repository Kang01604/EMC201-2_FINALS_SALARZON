using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class HealthBarUI : MonoBehaviour
{
    public Image fillImage; // Assign the 'Fill' Image component from your prefab
    public Vector3 offset = new Vector3(0, 1.5f, 0); // Adjust this to position the bar above the character

    private Health targetHealth;
    private Transform targetTransform;
    private Camera mainCamera;

    // Call this method from your character's script (PlayerMovement or EnemyAI)
    public void Initialize(Health healthComponent, Transform characterTransform, Camera cam)
    {
        targetHealth = healthComponent;
        targetTransform = characterTransform;
        mainCamera = cam;

        if (targetHealth == null || targetTransform == null || mainCamera == null)
        {
            Debug.LogError("[HealthBarUI] Initialization failed: Missing Health, Transform, or Camera reference.", this);
            Destroy(gameObject); // Destroy self if cannot initialize properly
            return;
        }

        // Subscribe to the OnHealthChanged event of the target Health component
        targetHealth.OnHealthChanged.AddListener(UpdateHealthBar);
        targetHealth.OnDeath.AddListener(OnTargetDeath);

        // Perform an initial update
        UpdateHealthBar();
    }

    void OnDestroy()
    {
        // Unsubscribe from events to prevent memory leaks if health bar is destroyed before character
        if (targetHealth != null)
        {
            targetHealth.OnHealthChanged.RemoveListener(UpdateHealthBar);
            targetHealth.OnDeath.RemoveListener(OnTargetDeath);
        }
    }

    void UpdateHealthBar()
    {
        if (targetHealth == null || fillImage == null) return;

        float healthPercentage = (float)targetHealth.currentHealth / targetHealth.maxHealth;
        fillImage.fillAmount = healthPercentage;

        // Change color based on health percentage
        if (healthPercentage > 0.5f)
        {
            fillImage.color = Color.green; // Full HP or above 50%
        }
        else if (healthPercentage > 0.25f)
        {
            fillImage.color = Color.yellow; // Half HP (50% to 25%)
        }
        else
        {
            fillImage.color = Color.red; // Below 25% HP
        }
    }

    void OnTargetDeath()
    {
        // When the character dies, destroy the health bar
        Destroy(gameObject);
    }

    void LateUpdate()
    {
        // Update the position of the health bar in LateUpdate to ensure smooth following
        // after all character movement is calculated for the frame.
        if (targetTransform != null && mainCamera != null)
        {
            Vector3 worldPosition = targetTransform.position + offset;
            transform.position = worldPosition;

            // Optional: Make the health bar always face the camera
            // (Only if your game is fully 3D and camera can rotate freely)
            // transform.rotation = mainCamera.transform.rotation;
        }
    }
}