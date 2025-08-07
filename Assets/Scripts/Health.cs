using UnityEngine;
using UnityEngine.Events; // Required for UnityEvent

public class Health : MonoBehaviour
{
    public int maxHealth = 100;
    public int currentHealth;

    [Header("Events")]
    public UnityEvent OnHealthChanged;
    public UnityEvent OnDeath;
    public UnityEvent OnDamageTaken;

    public CharacterDamageFeedback damageFeedback;

    void Awake()
    {
        currentHealth = maxHealth;
        
        damageFeedback = GetComponent<CharacterDamageFeedback>();
        if (damageFeedback == null)
        {
            Debug.LogWarning($"[Health] CharacterDamageFeedback component not found on {gameObject.name}. Damage feedback and invincibility won't work.");
        }

        OnHealthChanged.Invoke(); 
    }

    public void TakeDamage(int amount, Vector3 attackerPosition)
    {
        if (damageFeedback != null && damageFeedback.IsInvincible())
        {
            Debug.Log($"{gameObject.name} is currently invincible. Damage blocked.");
            return;
        }

        currentHealth -= amount;
        currentHealth = Mathf.Max(0, currentHealth);

        Debug.Log($"{gameObject.name} took {amount} damage. Current health: {currentHealth}/{maxHealth}");
        
        OnDamageTaken.Invoke(); 
        OnHealthChanged.Invoke(); 

        if (damageFeedback != null)
        {
            Vector3 knockbackDirection = (transform.position - attackerPosition).normalized;
            damageFeedback.TakeDamageFeedback(knockbackDirection);
        }

        if (currentHealth <= 0)
        {
            OnDeath.Invoke();
        }
    }

    public void Heal(int amount)
    {
        currentHealth += amount;
        currentHealth = Mathf.Min(maxHealth, currentHealth);

        Debug.Log($"{gameObject.name} healed {amount}. Current health: {currentHealth}/{maxHealth}");
        
        // NEW: Trigger heal visual feedback
        if (damageFeedback != null)
        {
            damageFeedback.HealFeedback();
        }

        OnHealthChanged.Invoke();
    }

    /// <summary>
    /// Increases the maximum health and optionally heals the character to the new maximum.
    /// This method is intended for upgrades or permanent health boosts.
    /// </summary>
    /// <param name="amountToIncrease">The amount to increase max health by.</param>
    /// <param name="healToNewMax">If true, current health will be set to the new max health. If false, current health remains the same or scales proportionally.</param>
    public void IncreaseMaxHealth(int amountToIncrease, bool healToNewMax = true)
    {
        maxHealth += amountToIncrease;
        Debug.Log($"{gameObject.name} max health increased by {amountToIncrease}. New max health: {maxHealth}");

        if (healToNewMax)
        {
            currentHealth = maxHealth; // Fully heal to the new max health
            Debug.Log($"{gameObject.name} healed to new max health: {currentHealth}/{maxHealth}");
        }
        // No explicit else needed for proportional scaling for now, as it's commented out
        
        // NEW: Trigger heal visual feedback for Max Health increase
        if (damageFeedback != null)
        {
            damageFeedback.HealFeedback();
        }

        OnHealthChanged.Invoke(); // Notify UI or other systems of the health change
    }
}