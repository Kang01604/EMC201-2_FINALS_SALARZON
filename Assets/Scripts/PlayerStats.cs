using UnityEngine;
using System.Collections;
using UnityEngine.Events; // Required for UnityEvents
using static AudioManager; // Required for AudioManager.Instance.PlaySFX

public class PlayerStats : MonoBehaviour
{
    [Header("Core Player Stats")]
    [Tooltip("Damage Multiplier: The base projectile damage (currently 5) will be multiplied by this value. Starts at 1.")]
    [SerializeField]
    private float _damageMultiplier = 1f;
    public float DamageMultiplier
    {
        get { return _damageMultiplier; }
        set { _damageMultiplier = Mathf.Max(0.1f, value); }
    }

    [Tooltip("Fire Rate: How many shots per second the player can fire. Higher value means faster shooting.")]
    [SerializeField]
    private float _fireRate = 5f;
    private float _currentFireRate; 
    public float FireRate
    {
        get { return _currentFireRate; }
        set { _currentFireRate = Mathf.Max(0.5f, value); }
    }

    [Tooltip("Movement Speed: The base walking speed of the player.")]
    [SerializeField]
    private float _movementSpeed = 5f;
    public float MovementSpeed
    {
        get { return _movementSpeed; }
        set { _movementSpeed = Mathf.Max(1f, value); }
    }

    [Tooltip("Luck: Affects the quality of investments/upgrades.")]
    [SerializeField]
    private float _luck = 1f;
    public float Luck
    {
        get { return _luck; }
        set { _luck = Mathf.Max(0f, value); }
    }

    // Reference to the Player's Health component
    private Health playerHealth; 
    // NEW: Reference to the Player's CharacterDamageFeedback component
    private CharacterDamageFeedback characterDamageFeedback;

    [Header("Projectile Base Values")]
    [Tooltip("The base damage of a single projectile before multipliers.")]
    public float baseProjectileDamage = 5f;

    // --- Player Economy & Leveling ---
    [Header("Player Economy & Leveling")]
    [SerializeField] private int _funds = 0;
    public int Funds
    {
        get { return _funds; }
        private set 
        {
            if (_funds != value) 
            {
                _funds = value;
                // Invoke event only if UIManager.Instance is available.
                // UIManager will subscribe to this event.
                if (UIManager.Instance != null) 
                {
                    OnFundsChanged.Invoke(_funds); 
                }
                CheckForLevelUp(); 
            }
        }
    }

    [SerializeField] private int _playerLevel = 1;
    public int PlayerLevel
    {
        get { return _playerLevel; }
        private set
        {
            if (_playerLevel != value)
            {
                _playerLevel = value;
                // Invoke event only if UIManager.Instance is available.
                // UIManager will subscribe to this event.
                if (UIManager.Instance != null)
                {
                    OnLevelUp.Invoke(_playerLevel); // This is only invoked when level actually changes
                }
            }
        }
    }

    [Tooltip("The amount of funds required to reach the next level.")]
    public int fundsToNextLevel = 100; 

    [Tooltip("Multiplier for fundsToNextLevel for subsequent levels.")]
    public float fundsRequiredMultiplier = 1.5f; 

    // --- Events ---
    [Header("Player Events")]
    public UnityEvent<int> OnFundsChanged; 
    public UnityEvent<int> OnLevelUp;     

    // Ammo Pack Buff Management
    private float fireRateBuffMultiplier = 1f;
    private Coroutine fireRateBuffCoroutine;

    public float GetCalculatedProjectileDamage()
    {
        return baseProjectileDamage * DamageMultiplier;
    }

    public void AddFunds(int amount)
    {
        Funds += amount; 
        Debug.Log($"Funds: {Funds}");
    }

    public void IncreaseDamageMultiplier(float amount)
    {
        DamageMultiplier += amount;
        Debug.Log($"Damage Multiplier increased to: {DamageMultiplier}");
    }

    public void IncreaseFireRate(float amount)
    {
        _fireRate += amount; 
        UpdateEffectiveFireRate();
        Debug.Log($"Base Fire Rate increased to: {_fireRate}");
    }

    public void IncreaseMovementSpeed(float amount)
    {
        MovementSpeed += amount;
        Debug.Log($"Movement Speed increased to: {MovementSpeed}");
    }

    public void IncreaseLuck(float amount)
    {
        Luck += amount;
        Debug.Log($"Luck increased to: {Luck}");
    }

    public void IncreaseMaxHealth(int amount)
    {
        if (playerHealth != null)
        {
            playerHealth.IncreaseMaxHealth(amount, true); 
            Debug.Log($"Player Max Health increased by {amount}. Current Max Health: {playerHealth.maxHealth}");
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX(SFXType.PlayerHeal);
            }
        }
        else
        {
            Debug.LogWarning("[PlayerStats] Player Health component not found, cannot increase max health!");
        }
    }

    public void ApplyAmmoBuff(float multiplier, float duration, CharacterDamageFeedback feedback = null)
    {
        if (fireRateBuffCoroutine != null)
        {
            StopCoroutine(fireRateBuffCoroutine);
        }
        fireRateBuffMultiplier = multiplier; 
        characterDamageFeedback = feedback != null ? feedback : GetComponent<CharacterDamageFeedback>();
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(SFXType.PlayerBuff);
        }
        fireRateBuffCoroutine = StartCoroutine(FireRateBuffRoutine(multiplier, duration, characterDamageFeedback));
    }

    private IEnumerator FireRateBuffRoutine(float multiplier, float duration, CharacterDamageFeedback feedback)
    {
        fireRateBuffMultiplier = multiplier;
        UpdateEffectiveFireRate();
        Debug.Log($"Fire Rate Buff Applied: {multiplier}x for {duration} seconds. New effective Fire Rate: {FireRate}");

        if (feedback != null)
        {
            feedback.ApplyAmmoBuffFeedback(duration);
        }

        yield return new WaitForSeconds(duration);

        fireRateBuffMultiplier = 1f;
        UpdateEffectiveFireRate();
        Debug.Log("Fire Rate Buff Expired. Effective Fire Rate reverted.");

        if (feedback != null)
        {
            feedback.RemoveAmmoBuffFeedback();
        }
    }

    private void UpdateEffectiveFireRate()
    {
        _currentFireRate = _fireRate * fireRateBuffMultiplier;
    }

    void Awake()
    {
        _currentFireRate = _fireRate;
        playerHealth = GetComponent<Health>();
        if (playerHealth == null)
        {
            Debug.LogError("[PlayerStats] Health component not found on Player GameObject! Max Health upgrades will not work.");
        }
        characterDamageFeedback = GetComponent<CharacterDamageFeedback>();
        if (characterDamageFeedback == null)
        {
            Debug.LogWarning("[PlayerStats] CharacterDamageFeedback component not found on Player GameObject. Ammo buff visuals may not work.");
        }

        if (OnFundsChanged == null)
        {
            OnFundsChanged = new UnityEvent<int>();
        }
        if (OnLevelUp == null)
        {
            OnLevelUp = new UnityEvent<int>();
        }
    }

    void Start() 
    {
        if (UIManager.Instance != null)
        {
            // IMPORTANT: Add listeners BEFORE registering, so UIManager updates trigger these events.
            OnFundsChanged.AddListener(UIManager.Instance.UpdateFunds);
            OnLevelUp.AddListener(UIManager.Instance.UpdateLevel);

            // Register this PlayerStats instance with the UIManager.
            // This will also trigger the initial UI updates via UIManager.RegisterPlayerStats().
            UIManager.Instance.RegisterPlayerStats(this);
            
            // Removed redundant initial UI updates as UIManager.RegisterPlayerStats() handles them.
            // OnFundsChanged.Invoke(Funds); 
            // UIManager.Instance.UpdateLevel(PlayerLevel);
        }
        else
        {
            Debug.LogError("[PlayerStats] UIManager.Instance is NULL. UI events will not be wired up correctly.");
        }
    }

    private void CheckForLevelUp()
    {
        if (Funds >= fundsToNextLevel)
        {
            PlayerLevel++; 
            Funds -= fundsToNextLevel; 
            fundsToNextLevel = Mathf.RoundToInt(fundsToNextLevel * fundsRequiredMultiplier); 
            Debug.Log($"LEVEL UP! New Level: {PlayerLevel}. Next level requires {fundsToNextLevel} funds.");
            if (AudioManager.Instance != null) // Add null check for AudioManager
            {
                AudioManager.Instance.PlaySFX(SFXType.PlayerLevelUp);
            }
        }
    }
}