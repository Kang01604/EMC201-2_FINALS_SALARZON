using UnityEngine;
using System.Collections;

// Define the types of drops
public enum DropType
{
    COIN_BRONZE,
    COIN_SILVER,
    COIN_GOLD,
    HEALTH_PACK,
    AMMO_PACK
}

[RequireComponent(typeof(Collider))] // Ensure the drop item has a collider
[RequireComponent(typeof(Rigidbody))] // Ensure the drop item has a rigidbody for physics/triggers
public class DropItem : MonoBehaviour
{
    [Header("Drop Item Settings")]
    public DropType dropType; // Set this in the Inspector for each prefab
    public float homingSpeed = 5f; // Speed at which the item homes towards the player
    public float lifetime = 15f; // How long the item exists before disappearing if not collected

    private Transform playerTransform;
    private Rigidbody rb;
    private bool isCollected = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true; // Make it kinematic so it doesn't fall through the world
            rb.useGravity = false; // No gravity for floating items
        }

        // Ensure collider is a trigger for collection
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }
    }

    void Start()
    {
        // Find the player immediately. If player spawns late, this might need adjustment
        // or a more robust player finding mechanism (e.g., a GameManager reference).
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
        }
        else
        {
            Debug.LogWarning("[DropItem] Player GameObject with 'Player' tag not found. Homing will not work.", this);
        }

        // Destroy the item after its lifetime if not collected
        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        if (isCollected) return;

        // Homing behavior
        if (playerTransform != null)
        {
            Vector3 directionToPlayer = (playerTransform.position - transform.position).normalized;
            transform.position = Vector3.MoveTowards(transform.position, playerTransform.position, homingSpeed * Time.deltaTime);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (isCollected) return;

        // Check if the collider belongs to the player
        if (other.CompareTag("Player"))
        {
            PlayerStats playerStats = other.GetComponent<PlayerStats>();
            Health playerHealth = other.GetComponent<Health>();
            // NEW: Get CharacterDamageFeedback component from the player
            CharacterDamageFeedback playerDamageFeedback = other.GetComponent<CharacterDamageFeedback>(); 

            if (playerStats == null)
            {
                Debug.LogWarning("[DropItem] PlayerStats component not found on player. Cannot apply drop effects.", other.gameObject);
                return;
            }

            isCollected = true; // Mark as collected to prevent multiple applications

            switch (dropType)
            {
                case DropType.COIN_BRONZE:
                    playerStats.AddFunds(5);
                    Debug.Log("Collected Bronze Coin: +5 Funds");
                    break;
                case DropType.COIN_SILVER:
                    playerStats.AddFunds(10);
                    Debug.Log("Collected Silver Coin: +10 Funds");
                    break;
                case DropType.COIN_GOLD:
                    playerStats.AddFunds(20);
                    Debug.Log("Collected Gold Coin: +20 Funds");
                    break;
                case DropType.HEALTH_PACK:
                    if (playerHealth != null)
                    {
                        // Heal 25% of overall HP
                        int healAmount = Mathf.RoundToInt(playerHealth.maxHealth * 0.25f);
                        playerHealth.Heal(healAmount);
                        Debug.Log($"Collected Health Pack: Healed {healAmount} HP (25% of max).");
                    }
                    else
                    {
                        Debug.LogWarning("[DropItem] Health component not found on player. Cannot apply Health Pack effect.", other.gameObject);
                    }
                    break;
                case DropType.AMMO_PACK:
                    // Apply ammo buff (3x faster fire rate for 10 seconds)
                    // NEW: Pass the playerDamageFeedback component to ApplyAmmoBuff
                    playerStats.ApplyAmmoBuff(3f, 10f, playerDamageFeedback); 
                    Debug.Log("Collected Ammo Pack: Fire rate 3x faster for 10 seconds.");
                    break;
                default:
                    Debug.LogWarning("[DropItem] Unhandled DropType: " + dropType.ToString());
                    break;
            }

            // Destroy the item after its effect is applied
            Destroy(gameObject);
        }
    }
}