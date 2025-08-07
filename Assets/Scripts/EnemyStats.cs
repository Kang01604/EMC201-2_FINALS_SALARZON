using UnityEngine;

public class EnemyStats : MonoBehaviour
{
    [Header("Core Enemy Stats")]
    [Tooltip("Enemy Health (read by Health script).")]
    [SerializeField]
    private float _maxHealth = 100f; // This will typically be set on the Health component, but can be a reference here.
    public float MaxHealth // Public property
    {
        get { return _maxHealth; }
        set { _maxHealth = Mathf.Max(1f, value); }
    }

    [Tooltip("Damage Multiplier: The base projectile/melee damage will be multiplied by this value. Starts at 1.")]
    [SerializeField]
    private float _damageMultiplier = 1f;
    public float DamageMultiplier
    {
        get { return _damageMultiplier; }
        set { _damageMultiplier = Mathf.Max(0.1f, value); }
    }

    [Tooltip("Movement Speed: How fast the enemy moves when chasing or patrolling.")]
    [SerializeField]
    private float _movementSpeed = 3f;
    public float MovementSpeed
    {
        get { return _movementSpeed; }
        set { _movementSpeed = Mathf.Max(0f, value); }
    }

    [Tooltip("Fire Rate: How many shots per second the enemy can fire (if ranged).")]
    [SerializeField]
    private float _fireRate = 1f; // e.g., 1 shot per second
    public float FireRate
    {
        get { return _fireRate; }
        set { _fireRate = Mathf.Max(0.1f, value); }
    }

    [Header("Projectile/Melee Base Values")]
    [Tooltip("The base damage of a single projectile before multipliers (if ranged).")]
    public float baseProjectileDamage = 10f;

    [Tooltip("The base damage of a single melee attack before multipliers (if melee).")]
    public float baseMeleeDamage = 15f;

    // Utility method to get calculated projectile damage
    public float GetCalculatedProjectileDamage()
    {
        return baseProjectileDamage * DamageMultiplier;
    }

    // Utility method to get calculated melee damage
    public float GetCalculatedMeleeDamage()
    {
        return baseMeleeDamage * DamageMultiplier;
    }

    // Example of an upgrade method for enemies (if you have an enemy upgrade system)
    public void IncreaseDamageMultiplier(float amount)
    {
        DamageMultiplier += amount;
    }

    // You would attach this script to your Enemy Prefabs or Enemy GameObjects.
    // The Health script on the enemy should read its max health from this script in its Awake/Start.
    void Awake()
    {
        // Optionally, if your Health script is designed to read from here:
        Health enemyHealth = GetComponent<Health>();
        if (enemyHealth != null)
        {
            // Explicitly cast to int to resolve CS0266 errors
            enemyHealth.maxHealth = (int)_maxHealth; 
            enemyHealth.currentHealth = (int)_maxHealth; 
        }
    }
}