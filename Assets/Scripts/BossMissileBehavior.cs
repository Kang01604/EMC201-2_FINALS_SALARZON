using UnityEngine;

public class BossMissileBehavior : MonoBehaviour
{
    public float speed = 8f;
    public float lifetime = 5f;
    public string targetTag = "Player";
    public float homingStrength = 1.0f; // Increased for more dynamic turning
    public float flightHeight = 10f;    // The height the missile will fly up to before homing
    public float upwardSpeedMultiplier = 1.2f; // How much faster it goes up initially
    public float rotationLerpSpeed = 5f; // Speed for general rotation (both Y and Z)
    public float dehomingHeight = 1.5f; // New variable: the Y position where homing starts to decrease
    public float dehomingFactor = 0.5f; // New variable: controls how quickly homing is lost

    // NEW: LayerMask to define layers the missile should ignore in collisions
    [Header("Collision Settings")]
    [SerializeField] private LayerMask ignoredLayers;

    // NEW: Reference to the empty GameObject that acts as the missile's head/nose for rotation
    [Header("Components")]
    public Transform missileHead; 

    // Visual adjustments for homing phase
    [Header("Homing Visuals")]
    public float homingDivePitch = 20f; // Angle in degrees for the missile to point its nose down while homing
    public float homingPhaseRollAngle = 270f; // Specific Z-rotation (roll) applied during homing

    // Prefab for the effect that plays on collision
    [Header("Effects")]
    public GameObject collisionEffectPrefab;

    private Transform playerTarget;
    private Rigidbody rb;
    private float missileDamage;
    private float initialYRotation; 
    private float currentHomingStrength; // New variable to dynamically control homing power

    private enum FlightPhase { UpwardFlight, Homing } 
    private FlightPhase currentPhase;

    // This method is called by BossAI to set the player target for homing.
    public void SetTarget(Transform target)
    {
        playerTarget = target;
    }

    // This method is called by BossAI to set the initial speed.
    public void SetInitialDirection(Vector3 ignoredDirection, float initialSpeed)
    {
        speed = initialSpeed;
        initialYRotation = transform.rotation.eulerAngles.y; 
    }

    // This method is called by BossAI to set the damage the missile will inflict.
    public void SetDamage(float damageAmount)
    {
        missileDamage = damageAmount;
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogWarning("[BossMissileBehavior] No Rigidbody found on missile. Homing movement might not be consistent.", this);
        }
        if (missileHead == null)
        {
            Debug.LogError("[BossMissileBehavior] 'Missile Head' Transform is not assigned! Homing visuals will not work correctly.", this);
        }
    }

    void Start()
    {
        if (rb != null)
        {
            rb.useGravity = false;
            rb.isKinematic = false;
        }

        currentPhase = FlightPhase.UpwardFlight;
        currentHomingStrength = homingStrength; // Initialize homing strength to full power
        
        // Initial velocity for upward flight is set here
        if (rb != null)
        {
            rb.linearVelocity = Vector3.up * speed * upwardSpeedMultiplier;
        }
        
        // Initial rotation of the root missile object for upward phase (e.g., flat on side)
        transform.rotation = Quaternion.Euler(0, initialYRotation, 90f);

        // Destroy the missile after its lifetime
        Destroy(gameObject, lifetime);
    }

    void FixedUpdate() 
    {
        if (playerTarget == null)
        {
            if (rb != null) rb.linearVelocity = Vector3.zero;
            return;
        }

        switch (currentPhase)
        {
            case FlightPhase.UpwardFlight:
                // We're already setting velocity in Start(), so we just need to handle rotation
                // and the phase transition here. The velocity remains constant.
                
                // Maintain the initial Y-rotation and Z=90 during upward flight on the root transform
                Quaternion targetUpwardRotation = Quaternion.Euler(0, initialYRotation, 90f);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetUpwardRotation, Time.fixedDeltaTime * rotationLerpSpeed);

                // Transition to homing state when it reaches desired height
                if (transform.position.y >= flightHeight)
                {
                    currentPhase = FlightPhase.Homing;
                }
                break;

            case FlightPhase.Homing:
                // Check if we are below the dehoming height
                if (transform.position.y <= dehomingHeight)
                {
                    // Gradually reduce homing strength as the missile gets lower
                    // The closer to the ground, the weaker the homing.
                    float lerpFactor = (dehomingHeight - transform.position.y) / dehomingHeight;
                    currentHomingStrength = homingStrength * Mathf.Lerp(1.0f, 0.0f, lerpFactor * dehomingFactor);
                }
                else
                {
                    // Maintain full homing strength above the dehoming height
                    currentHomingStrength = homingStrength;
                }

                // Calculate direction to player and apply homing force
                Vector3 directionToPlayer = (playerTarget.position - transform.position).normalized;

                if (rb != null)
                {
                    // Smoothly rotate the missile's current linear velocity towards the player direction using the current homing strength
                    Vector3 newVelocity = Vector3.RotateTowards(rb.linearVelocity.normalized, directionToPlayer, currentHomingStrength * Time.fixedDeltaTime, 0.0f);
                    rb.linearVelocity = newVelocity.normalized * speed;

                    if (rb.linearVelocity.magnitude > 0.01f)
                    {
                        // The root transform rotates to face the direction of the velocity. This is the main steering.
                        Quaternion targetLookRotation = Quaternion.LookRotation(rb.linearVelocity.normalized);
                        transform.rotation = Quaternion.Slerp(transform.rotation, targetLookRotation, Time.fixedDeltaTime * rotationLerpSpeed);

                        // The missileHead is the visual component that aims at the player.
                        if (missileHead != null)
                        {
                            // Calculate the direction to the player in world space
                            Vector3 worldDirectionToPlayer = (playerTarget.position - missileHead.position).normalized;
                            
                            // Create a world-space rotation that looks at the player
                            Quaternion headTargetRotation = Quaternion.LookRotation(worldDirectionToPlayer);

                            // Apply the visual offsets (pitch and roll) on top of the look-at rotation
                            Quaternion finalVisualRotation = headTargetRotation * Quaternion.Euler(homingDivePitch, 0, homingPhaseRollAngle);
                            
                            // Smoothly rotate the head towards this final rotation
                            missileHead.rotation = Quaternion.Slerp(missileHead.rotation, finalVisualRotation, Time.fixedDeltaTime * rotationLerpSpeed);
                        }
                    }
                }
                break;
        }
    }

    /// <summary>
    /// Handles collision when the missile's trigger collider enters another object.
    /// </summary>
    void OnTriggerEnter(Collider other)
    {
        HandleHit(other.gameObject);
    }

    /// <summary>
    /// Handles collision when the missile's non-trigger collider hits another object.
    /// </summary>
    void OnCollisionEnter(Collision collision)
    {
        HandleHit(collision.gameObject);
    }

    private void HandleHit(GameObject hitObject)
    {
        // Check if the hit object is on one of the ignored layers or has the "Boss" tag
        if (((1 << hitObject.layer) & ignoredLayers) != 0 || hitObject.CompareTag("Boss"))
        {
            // The missile does not collide with ignored objects or the boss.
            // It will simply continue on its path.
            return;
        }

        // If the hit object has the player tag, deal damage
        if (hitObject.CompareTag(targetTag))
        {
            Health targetHealth = hitObject.GetComponent<Health>();
            if (targetHealth != null)
            {
                targetHealth.TakeDamage(Mathf.RoundToInt(missileDamage), transform.position);
                Debug.Log($"[BossMissileBehavior] Hit {hitObject.name} (Tag: {hitObject.tag}) for {missileDamage} damage.");
            }

            CharacterDamageFeedback damageFeedback = hitObject.GetComponent<CharacterDamageFeedback>();
            if (damageFeedback != null)
            {
                Vector3 knockbackDirection = (hitObject.transform.position - transform.position).normalized;
                damageFeedback.TakeDamageFeedback(knockbackDirection);
            }

            // Play collision effect and destroy the missile
            if (collisionEffectPrefab != null)
            {
                Instantiate(collisionEffectPrefab, transform.position, Quaternion.identity);
            }
            Destroy(gameObject);
        }
        else // Collided with something that's not the player, boss, or an ignored layer.
        {
            // Play collision effect and destroy the missile
            if (collisionEffectPrefab != null)
            {
                Instantiate(collisionEffectPrefab, transform.position, Quaternion.identity);
            }
            Destroy(gameObject);
        }
    }
}