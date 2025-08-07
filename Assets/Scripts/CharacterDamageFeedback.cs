using UnityEngine;
using System.Collections;

public class CharacterDamageFeedback : MonoBehaviour
{
    [Header("Damage Feedback Settings")]
    [SerializeField] private float knockbackForce = 3f;
    [SerializeField] private float knockbackDuration = 0.15f;
    
    // Make this editable in the Inspector for each GameObject
    [SerializeField] public float invincibilityDuration = 2f; // Now configurable per instance (e.g., 2f for player, 0.5f for enemy)
    
    [SerializeField] private Color initialFlashColor = Color.red; // Color for the immediate flash (damage)
    [SerializeField] private float initialFlashDuration = 0.1f;    // Duration of the immediate flash (damage)
    [SerializeField] [Range(0f, 1f)] private float invincibleBlinkAlpha = 0.95f; // Alpha value for blinking (damage)
    [SerializeField] private float blinkInterval = 0.15f; // Time for one blink cycle (on and off) (damage)

    [Header("Healing Feedback Settings")]
    [SerializeField] private Color healFlashColor = Color.green; // Color for healing/HP upgrade flash
    [SerializeField] private float healFlashDuration = 0.2f;     // Duration of healing flash

    [Header("Ammo Pack Feedback Settings")]
    [SerializeField] private Color ammoBuffColor = Color.yellow; // Color for ammo pack buff
    [SerializeField] private float ammoBuffBlinkInterval = 0.1f; // Blinking interval for ammo buff

    private bool isInvincible = false;
    private bool isKnockedBack = false;
    private CharacterController characterController;
    private Rigidbody rb;
    private Renderer[] characterRenderers;
    private Color[] originalColors;

    // Coroutine references to manage multiple concurrent effects
    private Coroutine invincibilityCoroutine;
    private Coroutine visualFeedbackCoroutine;
    private Coroutine knockbackCoroutine;
    private Coroutine ammoBuffVisualCoroutine; // New: To manage ammo buff visual

    void Awake()
    {
        characterController = GetComponent<CharacterController>();
        if (characterController == null)
        {
            rb = GetComponent<Rigidbody>();
        }

        characterRenderers = GetComponentsInChildren<Renderer>();
        if (characterRenderers.Length > 0)
        {
            originalColors = new Color[characterRenderers.Length];
            for (int i = 0; i < characterRenderers.Length; i++)
            {
                if (characterRenderers[i].material.HasProperty("_Color"))
                {
                    originalColors[i] = characterRenderers[i].material.color;
                }
                else
                {
                    originalColors[i] = Color.white;
                }
                // IMPORTANT: For alpha blending to work, your material's shader must
                // support transparency (e.g., Standard shader with Render Mode set to "Fade" or "Transparent").
            }
        }
        else
        {
            Debug.LogWarning($"[CharacterDamageFeedback] No Renderer components found on {gameObject.name} or its children. Visual feedback will not work.");
        }
    }

    /// <summary>
    /// Call this method when the character takes damage.
    /// It will apply invincibility frames, visual feedback (flash then blink), and knockback.
    /// </summary>
    /// <param name="knockbackDirection">The normalized direction to apply knockback (e.g., away from the attacker).</param>
    public void TakeDamageFeedback(Vector3 knockbackDirection)
    {
        // Prevent new feedback effects if already invincible or knocked back
        if (isInvincible || isKnockedBack)
        {
            return;
        }

        // Stop other non-damage visual coroutines to prevent conflicts
        if (ammoBuffVisualCoroutine != null) StopCoroutine(ammoBuffVisualCoroutine);
        ResetToOriginalColors(); // Ensure model is not orange before flashing red

        // Start damage feedback coroutines, storing their references
        invincibilityCoroutine = StartCoroutine(InvincibilityCoroutine());
        visualFeedbackCoroutine = StartCoroutine(HandleDamageVisualFeedback());
        knockbackCoroutine = StartCoroutine(ApplyKnockback(knockbackDirection));
        
        Debug.Log($"[CharacterDamageFeedback] {gameObject.name} initiated damage feedback.");
    }

    /// <summary>
    /// Call this method when the character receives healing or upgrades Max HP.
    /// </summary>
    public void HealFeedback()
    {
        // Stop any active damage visual feedback, but allow invincibility to continue if active
        if (visualFeedbackCoroutine != null) StopCoroutine(visualFeedbackCoroutine);
        if (ammoBuffVisualCoroutine != null) StopCoroutine(ammoBuffVisualCoroutine); // Stop ammo buff if active

        StartCoroutine(HandleHealVisualFeedback());
        Debug.Log($"[CharacterDamageFeedback] {gameObject.name} initiated heal feedback.");
    }

    /// <summary>
    /// Call this method when an ammo pack buff is applied.
    /// </summary>
    /// <param name="duration">The duration of the ammo buff.</param>
    public void ApplyAmmoBuffFeedback(float duration)
    {
        // Stop any active non-damage visual feedback
        if (visualFeedbackCoroutine != null) StopCoroutine(visualFeedbackCoroutine);
        if (ammoBuffVisualCoroutine != null) StopCoroutine(ammoBuffVisualCoroutine);

        ammoBuffVisualCoroutine = StartCoroutine(HandleAmmoBuffVisualFeedback(duration));
        Debug.Log($"[CharacterDamageFeedback] {gameObject.name} initiated ammo buff feedback for {duration} seconds.");
    }

    /// <summary>
    /// Call this method when an ammo pack buff wears off or is manually removed.
    /// </summary>
    public void RemoveAmmoBuffFeedback()
    {
        if (ammoBuffVisualCoroutine != null)
        {
            StopCoroutine(ammoBuffVisualCoroutine);
            ammoBuffVisualCoroutine = null;
            ResetToOriginalColors(); // Immediately revert to original colors
            Debug.Log($"[CharacterDamageFeedback] {gameObject.name} ammo buff feedback removed.");
        }
    }


    public bool IsInvincible()
    {
        return isInvincible;
    }

    public bool IsKnockedBack()
    {
        return isKnockedBack;
    }

    private IEnumerator ApplyKnockback(Vector3 direction)
    {
        isKnockedBack = true;
        float startTime = Time.time;
        Vector3 currentKnockbackVelocity = direction * knockbackForce;

        while (Time.time < startTime + knockbackDuration)
        {
            if (characterController != null)
            {
                // Move with character controller
                characterController.Move(currentKnockbackVelocity * Time.deltaTime);
            }
            else if (rb != null)
            {
                // Apply velocity for Rigidbody based knockback
                rb.linearVelocity = currentKnockbackVelocity;
            }
            yield return null; 
        }
        
        // After knockback, reset velocity if using Rigidbody to prevent sliding
        if (rb != null && !rb.isKinematic)
        {
            rb.linearVelocity = Vector3.zero;
        }

        isKnockedBack = false;
        Debug.Log($"[CharacterDamageFeedback] {gameObject.name} knockback ended.");
    }

    private IEnumerator InvincibilityCoroutine()
    {
        isInvincible = true;
        Debug.Log($"[CharacterDamageFeedback] {gameObject.name} is now invincible for {invincibilityDuration} seconds!");
        yield return new WaitForSeconds(invincibilityDuration);
        isInvincible = false;
        Debug.Log($"[CharacterDamageFeedback] {gameObject.name} is no longer invincible.");
    }

    private IEnumerator HandleDamageVisualFeedback()
    {
        if (characterRenderers == null || characterRenderers.Length == 0) yield break;

        // --- Stage 1: Initial Red Flash ---
        foreach (Renderer rend in characterRenderers)
        {
            if (rend != null && rend.material != null && rend.material.HasProperty("_Color"))
            {
                rend.material.color = initialFlashColor; // Set to red
            }
        }
        yield return new WaitForSeconds(initialFlashDuration);

        // Restore original colors (but keep alpha at 1.0 for now)
        for (int i = 0; i < characterRenderers.Length; i++)
        {
            if (characterRenderers[i] != null && characterRenderers[i].material != null && characterRenderers[i].material.HasProperty("_Color"))
            {
                Color tempColor = originalColors[i];
                tempColor.a = 1.0f; // Ensure full opacity after the red flash, before blinking starts
                characterRenderers[i].material.color = tempColor;
            }
        }
        yield return null; // Wait one frame to ensure color is set before blinking starts

        // --- Stage 2: Blinking Opacity during Invincibility ---
        float startTime = Time.time;
        // Remaining time for blinking is total invincibility duration minus initial flash duration
        float remainingInvincibilityTime = invincibilityDuration - initialFlashDuration;

        while (Time.time < startTime + remainingInvincibilityTime)
        {
            // Set to semi-transparent (targetAlpha)
            foreach (Renderer rend in characterRenderers)
            {
                if (rend != null && rend.material != null && rend.material.HasProperty("_Color"))
                {
                    Color currentColor = rend.material.color;
                    currentColor.a = invincibleBlinkAlpha; 
                    rend.material.color = currentColor;
                }
            }
            yield return new WaitForSeconds(blinkInterval / 2f); // Half interval for 'on' state

            // Set back to fully opaque (1.0f)
            for (int i = 0; i < characterRenderers.Length; i++)
            {
                if (characterRenderers[i] != null && characterRenderers[i].material != null && characterRenderers[i].material.HasProperty("_Color"))
                {
                    Color tempColor = characterRenderers[i].material.color; // Get current RGB
                    tempColor.a = 1.0f; // Set alpha to 100%
                    characterRenderers[i].material.color = tempColor;
                }
            }
            yield return new WaitForSeconds(blinkInterval / 2f); // Half interval for 'off' state
        }

        // --- Final State: Ensure full opacity at the end ---
        ResetToOriginalColors();
        Debug.Log($"[CharacterDamageFeedback] {gameObject.name} visual feedback ended.");
    }

    private IEnumerator HandleHealVisualFeedback()
    {
        if (characterRenderers == null || characterRenderers.Length == 0) yield break;

        // Store current colors before applying flash, in case we interrupt another effect
        Color[] currentColorsBeforeHeal = new Color[characterRenderers.Length];
        for (int i = 0; i < characterRenderers.Length; i++)
        {
            if (characterRenderers[i].material.HasProperty("_Color"))
            {
                currentColorsBeforeHeal[i] = characterRenderers[i].material.color;
            }
            else
            {
                currentColorsBeforeHeal[i] = Color.white;
            }
        }

        // Flash green
        foreach (Renderer rend in characterRenderers)
        {
            if (rend != null && rend.material != null && rend.material.HasProperty("_Color"))
            {
                rend.material.color = healFlashColor;
            }
        }
        yield return new WaitForSeconds(healFlashDuration);

        // Revert to original colors or the state before the heal flash
        for (int i = 0; i < characterRenderers.Length; i++)
        {
            if (characterRenderers[i] != null && characterRenderers[i].material != null && characterRenderers[i].material.HasProperty("_Color"))
            {
                characterRenderers[i].material.color = currentColorsBeforeHeal[i];
            }
        }
        Debug.Log($"[CharacterDamageFeedback] {gameObject.name} heal visual feedback ended.");
    }

    private IEnumerator HandleAmmoBuffVisualFeedback(float duration)
    {
        if (characterRenderers == null || characterRenderers.Length == 0) yield break;

        float startTime = Time.time;
        
        while (Time.time < startTime + duration)
        {
            // Set to orange
            foreach (Renderer rend in characterRenderers)
            {
                if (rend != null && rend.material != null && rend.material.HasProperty("_Color"))
                {
                    rend.material.color = ammoBuffColor;
                }
            }
            yield return new WaitForSeconds(ammoBuffBlinkInterval / 2f);

            // Set back to original (or slightly transparent if desired, but full opacity is cleaner for buff)
            for (int i = 0; i < characterRenderers.Length; i++)
            {
                if (characterRenderers[i] != null && characterRenderers[i].material != null && characterRenderers[i].material.HasProperty("_Color"))
                {
                    Color tempColor = originalColors[i];
                    tempColor.a = 1.0f; // Ensure full opacity
                    characterRenderers[i].material.color = tempColor;
                }
            }
            yield return new WaitForSeconds(ammoBuffBlinkInterval / 2f);
        }

        // Ensure original colors are restored after the buff expires
        ResetToOriginalColors();
        Debug.Log($"[CharacterDamageFeedback] {gameObject.name} ammo buff visual feedback expired.");
        ammoBuffVisualCoroutine = null; // Clear the coroutine reference
    }

    // Helper method to reset all renderers to their original colors and full opacity
    private void ResetToOriginalColors()
    {
        if (characterRenderers != null && originalColors != null)
        {
            for (int i = 0; i < characterRenderers.Length; i++)
            {
                if (characterRenderers[i] != null && characterRenderers[i].material != null && characterRenderers[i].material.HasProperty("_Color"))
                {
                    Color finalColor = originalColors[i];
                    finalColor.a = 1.0f; // Force to 100% opacity
                    characterRenderers[i].material.color = finalColor;
                }
            }
        }
    }
}