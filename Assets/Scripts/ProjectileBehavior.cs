using UnityEngine;

public class ProjectileBehavior : MonoBehaviour
{
    public float speed = 10f;
    public float lifetime = 3f; // How long the projectile exists before self-destructing
    public string targetTag;     // Tag of the target this projectile should hit (e.g., "Enemy" or "Player")

    // NEW: Prefab for the effect that plays on collision
    [Header("Effects")]
    public GameObject collisionEffectPrefab;

    private Vector3 direction;   // Direction the projectile will travel
    private Rigidbody rb;        // Reference to the Rigidbody component

    private float projectileDamage; // Private variable to store the actual damage value

    // This method is called by the shooting script to set the projectile's initial direction and speed
    public void SetDirection(Vector3 newDirection, float newSpeed)
    {
        direction = newDirection.normalized; // Ensure it's a unit vector
        speed = newSpeed;
    }

    // This method is called by the shooting script (PlayerMovement or EnemyAI) to set the damage
    public void SetDamage(float damageAmount) // CORRECTED: Removed the extra 'void' keyword here
    {
        projectileDamage = damageAmount;
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody>(); // Get the Rigidbody component
        if (rb == null)
        {
            Debug.LogWarning("[ProjectileBehavior] No Rigidbody found on projectile. Movement might not be consistent.", this);
        }

        // --- Layer checks are for informing the user, not for ignoring collisions here ---
        // These are useful to ensure your Unity layers are set up correctly.
        int projectileLayer = LayerMask.NameToLayer("Projectile");
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        int playerLayer = LayerMask.NameToLayer("Player");

        if (projectileLayer == -1)
        {
            Debug.LogError("Error: 'Projectile' layer not found in Project Settings -> Tags & Layers!", this);
        }
        if (enemyLayer == -1)
        {
            Debug.LogError("Error: 'Enemy' layer not found in Project Settings -> Tags & Layers!", this);
        }
        if (playerLayer == -1)
        {
            Debug.LogError("Error: 'Player' layer not found in Project Settings -> Tags & Layers!", this);
        }

        // Remember: Physics.IgnoreLayerCollision should be managed in Project Settings -> Physics (2D/3D)
        // for global interactions, not typically hardcoded in individual scripts to prevent hits.
    }

    void Start()
    {
        if (rb != null)
        {
            rb.useGravity = false; // Projectiles usually don't need gravity
            rb.isKinematic = false; // Must be non-kinematic for collisions/triggers to work correctly
            rb.linearVelocity = direction * speed; // Use Rigidbody velocity for movement
        }
        else
        {
            Debug.LogWarning("[ProjectileBehavior] Rigidbody not found. Projectile movement will rely on Transform directly.", this);
        }

        // Start a timer to destroy the projectile after its lifetime
        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        // Only use transform.position movement if there is no Rigidbody
        // (Rigidbody.velocity is preferred for physics objects)
        if (rb == null)
        {
            transform.position += direction * speed * Time.deltaTime;
        }
    }

    // NEW: Helper method to determine if the hit object is a valid target for this projectile
    private bool IsValidTarget(GameObject hitObject)
    {
        // Always hit if it matches the exact targetTag
        if (hitObject.CompareTag(targetTag))
        {
            return true;
        }

        // Special rule: If this projectile targets "Enemy", it can also hit objects tagged "Boss".
        // This allows player projectiles to hit both standard enemies and bosses.
        if (targetTag == "Enemy" && hitObject.CompareTag("Boss"))
        {
            return true;
        }

        return false;
    }


    // Handles logic for both OnTriggerEnter and OnCollisionEnter
    private void HandleHit(GameObject hitObject, Vector3 hitPosition)
    {
        if (IsValidTarget(hitObject))
        {
            Health targetHealth = hitObject.GetComponent<Health>();
            if (targetHealth != null)
            {
                // Call the TakeDamage method on the target's Health component using projectileDamage
                targetHealth.TakeDamage(Mathf.RoundToInt(projectileDamage), hitPosition);
                Debug.Log($"[ProjectileBehavior] Hit {hitObject.name} (Tag: {hitObject.tag}) for {projectileDamage} damage.");
            }

            // Apply damage feedback if the target has the component
            CharacterDamageFeedback damageFeedback = hitObject.GetComponent<CharacterDamageFeedback>();
            if (damageFeedback != null)
            {
                Vector3 knockbackDirection = (hitObject.transform.position - transform.position).normalized;
                damageFeedback.TakeDamageFeedback(knockbackDirection);
            }

            // NEW: Instantiate collision effect upon hitting a valid target
            if (collisionEffectPrefab != null)
            {
                Instantiate(collisionEffectPrefab, transform.position, Quaternion.identity);
            }

            // Destroy the projectile AFTER it hits its intended target
            Destroy(gameObject);
        }
        else
        {
            // Optional: If you want the projectile to be destroyed on hitting ANY non-target object (like walls, ground etc.),
            // you can add logic here. For now, it will only destroy if it hits a 'valid target'.
            // Example for destroying on non-target (excluding self and other projectiles):
            // if (!hitObject.CompareTag("Projectile") && !hitObject.CompareTag("IgnoreCollision")) {
            //     if (collisionEffectPrefab != null) { Instantiate(collisionEffectPrefab, transform.position, Quaternion.identity); }
            //     Destroy(gameObject);
            // }
        }
    }


    // This method is called when the projectile's TRIGGER COLLIDER overlaps with another object
    void OnTriggerEnter(Collider other)
    {
        HandleHit(other.gameObject, transform.position); // Use transform.position as approximate hit position for triggers
    }

    // This method is called when the projectile's NON-TRIGGER COLLIDER hits another object
    void OnCollisionEnter(Collision collision)
    {
        // This method will ONLY be called if 'Is Trigger' is UNCHECKED on the projectile's collider.
        // If your projectile is meant to be a solid object that bounces or is stopped by non-targets,
        // then 'Is Trigger' would be unchecked, and this method would be more relevant.

        // For OnCollisionEnter, it's generally better to use collision.contacts[0].point for the exact hit position.
        Vector3 hitPosition = collision.contacts.Length > 0 ? collision.contacts[0].point : transform.position;
        HandleHit(collision.gameObject, hitPosition);
    }
}