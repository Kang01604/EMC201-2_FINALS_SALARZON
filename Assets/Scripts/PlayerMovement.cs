using UnityEngine;
using System;
using System.Collections; // Required for Coroutines
using UnityEngine.Events;
using static AudioManager; // To allow direct access to SFXType enum

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float sprintSpeedMultiplier = 1.5f;

    public SPUM_Prefabs spumScript;
    private CharacterController characterController;
    private bool wasMoving = false;

    // Camera follow variables
    public Transform cameraTarget;
    public float smoothSpeed = 0.125f;

    // *** CAMERA SETTINGS FOR 2.5D VIEW (Controlled by Script) ***
    [Header("Camera Settings (Controlled by Script)")]
    public Camera gameCamera;
    public bool useOrthographicProjection = false;
    public float orthographicCameraSize = 5f;
    public float perspectiveCameraFOV = 25f;
    public Vector3 cameraFixedOffset = new Vector3(0f, 15f, -15f);
    public Vector3 cameraFixedRotation = new Vector3(45f, 0f, 0f);
    private readonly Quaternion playerFixedRotation = Quaternion.Euler(45f, 0f, 0f);

    // *** Gungeon Style Additions ***
    [Header("Gungeon Style Settings")]
    public RectTransform crosshairUI;

    // NEW: Reference to the Health Bar UI Prefab
    [Header("UI References")]
    public GameObject healthBarUIPrefab; // Assign your HealthBarCanvas prefab here
    private HealthBarUI playerHealthBar; // Reference to the instantiated health bar script

    [Header("Shooting Settings")]
    public GameObject projectilePrefab;
    public Transform firePoint;
    public float projectileSpeed = 20f;
    private float nextFireTime = 0f;
    [Tooltip("Extra distance to spawn projectile in front of player's collider edge. Adjust this based on character size.")]
    [SerializeField] private float autoFirePointOffset = 0.3f;

    // *** DODGE SETTINGS ***
    [Header("Dodge Settings")]
    public float dodgeSpeed = 10f;
    public float dodgeDuration = 0.3f;
    public float dodgeCooldown = 1f;

    private bool isDodging = false;
    private float timeUntilNextDodge = 0f;
    private float dodgeStartTime = 0f;
    private Vector3 dodgeDirection = Vector3.zero;
    private Vector3 lastMoveInputDirection = Vector3.zero;

    // --- References to other components ---
    private Health playerHealth;
    private CharacterDamageFeedback damageFeedback; // Corrected type
    private PlayerStats playerStats; // Reference to PlayerStats script

    // --- Player State Flags ---
    private bool isDead = false;
    private bool isDamagedAnimationPlaying = false;

    // NEW: Control flag for player movement/actions (for upgrade system)
    private bool canMove = true; // Added for upgrade system to pause player

    // NEW: Particle effect for death
    [Header("Death Effects")]
    public GameObject deathParticlePrefab; // Assign your particle system prefab here

    void Awake()
    {
        characterController = GetComponent<CharacterController>();
        if (characterController == null)
        {
            Debug.LogError("[PlayerMovement] CharacterController component not found on the player GameObject. Please add one!");
            return;
        }

        characterController.radius = 0.35f;
        characterController.height = 1.738078f;
        characterController.center = new Vector3(0f, 0.6f, -0.35f);

        transform.rotation = playerFixedRotation;

        playerHealth = GetComponent<Health>();
        if (playerHealth == null)
        {
            Debug.LogError("[PlayerMovement] Health component not found on player. Damage feedback will not work.");
        }

        damageFeedback = GetComponent<CharacterDamageFeedback>();
        if (damageFeedback == null)
        {
            Debug.LogError("[PlayerMovement] CharacterDamageFeedback component not found on player. Damage feedback effects will not work.");
        }

        playerStats = GetComponent<PlayerStats>();
        if (playerStats == null)
        {
            Debug.LogError("[PlayerMovement] PlayerStats component not found on player. Player stats (Damage, Fire Rate, Movement Speed, Luck) will not function correctly.");
        }
    }

    void OnEnable()
    {
        if (playerHealth != null)
        {
            playerHealth.OnDamageTaken.AddListener(PlayerTakesDamage);
            playerHealth.OnDeath.AddListener(PlayerDies);
        }
    }

    void OnDisable()
    {
        if (playerHealth != null)
        {
            playerHealth.OnDamageTaken.RemoveListener(PlayerTakesDamage);
            playerHealth.OnDeath.RemoveListener(PlayerDies);
        }
    }

    void Start()
    {
        if (spumScript == null)
        {
            spumScript = GetComponentInChildren<SPUM_Prefabs>();
            if (spumScript != null)
            {
                Debug.Log("SPUM_Prefabs script found and assigned automatically.");
            }
        }

        if (spumScript == null)
        {
            Debug.LogWarning("SPUM script component (SPUM_Prefabs) NOT FOUND on " + gameObject.name + " or its children. Animations will not play correctly.");
        }

        if (spumScript != null)
        {
            if (!spumScript.allListsHaveItemsExist())
            {
                Debug.Log("SPUM_Prefabs lists are not populated. Attempting to call PopulateAnimationLists().");
                spumScript.PopulateAnimationLists();
            }

            if (spumScript.StateAnimationPairs == null || !spumScript.StateAnimationPairs.ContainsKey(PlayerState.IDLE.ToString()))
            {
                Debug.Log("SPUM_Prefabs StateAnimationPairs not initialized or missing IDLE key. Attempting to call OverrideControllerInit().");
                spumScript.OverrideControllerInit();
            }

            if (spumScript.StateAnimationPairs != null && spumScript.StateAnimationPairs.ContainsKey(PlayerState.IDLE.ToString()))
            {
                Debug.Log("SPUM_Prefabs StateAnimationPairs confirmed to have IDLE key after initialization attempts.");
            }
            else
            {
                Debug.LogError("SPUM_Prefabs StateAnimationPairs still not correctly initialized after attempts. Check SPUM_Prefabs script setup.");
            }
        }

        if (cameraTarget == null)
        {
            cameraTarget = this.transform;
        }

        if (gameCamera == null)
        {
            gameCamera = Camera.main;
            if (gameCamera != null)
            {
                Debug.Log("[PlayerMovement] Main Camera found via Camera.main. (Consider assigning it directly to 'Game Camera' slot for robustness).");
            }
        }

        if (gameCamera != null)
        {
            gameCamera.orthographic = useOrthographicProjection;
            if (useOrthographicProjection)
            {
                gameCamera.orthographicSize = orthographicCameraSize;
            }
            else
            {
                gameCamera.fieldOfView = perspectiveCameraFOV;
            }

            gameCamera.transform.rotation = Quaternion.Euler(cameraFixedRotation);

            Debug.Log("[PlayerMovement] Game Camera settings applied.");
            Debug.Log($"[PlayerMovement] Current Camera State: Orthographic={gameCamera.orthographic}, Size/FOV={(gameCamera.orthographic ? gameCamera.orthographicSize.ToString() : gameCamera.fieldOfView.ToString())}, Rotation X={gameCamera.transform.rotation.eulerAngles.x}");
        }
        else
        {
            Debug.LogError("[PlayerMovement] No Camera assigned to 'Game Camera' slot and no 'Main Camera' tagged in the scene. Camera cannot be controlled by script.");
        }

        // NEW: Instantiate and Initialize the Player's Health Bar
        if (healthBarUIPrefab != null && playerHealth != null && gameCamera != null)
        {
            GameObject healthBarGO = Instantiate(healthBarUIPrefab);
            playerHealthBar = healthBarGO.GetComponent<HealthBarUI>();
            if (playerHealthBar != null)
            {
                // Pass THIS player's Health component, Transform, and the active game camera
                playerHealthBar.Initialize(playerHealth, this.transform, gameCamera);
            }
            else
            {
                Debug.LogWarning("[PlayerMovement] HealthBarUIPrefab does not have a HealthBarUI component!", this);
            }
        }
        else
        {
            if (healthBarUIPrefab == null) Debug.LogWarning("[PlayerMovement] Health Bar UI Prefab not assigned for Player!");
            if (playerHealth == null) Debug.LogWarning("[PlayerMovement] Player's Health component not found, cannot initialize health bar.");
            if (gameCamera == null) Debug.LogWarning("[PlayerMovement] Game Camera not found, cannot initialize health bar position correctly.");
        }

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Confined;
    }

    void Update()
    {
        if (isDead)
        {
            return;
        }

        // NEW: If movement is not allowed (e.g., upgrade UI is open), stop all player control.
        if (!canMove)
        {
            // Reset animations to idle if they were moving when paused
            if (wasMoving && spumScript != null)
            {
                spumScript.PlayAnimation(PlayerState.IDLE, 0);
                wasMoving = false;
            }
            // Ensure no lingering dodge or damaged animation if paused during it
            if (isDodging)
            {
                isDodging = false; // Force stop dodge
                StopCoroutine("HandleDodge"); // Stop dodge coroutine if it was a coroutine
                transform.rotation = playerFixedRotation; // Reset rotation
            }
            if (isDamagedAnimationPlaying)
            {
                isDamagedAnimationPlaying = false; // Force stop damaged animation
                StopCoroutine("HandleDamagedAnimation"); // Stop coroutine
            }
            if (crosshairUI != null) crosshairUI.gameObject.SetActive(false); // Hide crosshair
            return; // Exit Update early if movement is disallowed
        }
        else
        {
            if (crosshairUI != null) crosshairUI.gameObject.SetActive(true);
        }


        // Existing logic to handle knockback and timeUntilNextDodge
        if (damageFeedback != null && damageFeedback.IsKnockedBack())
        {
            if (!isDodging && !isDamagedAnimationPlaying && spumScript != null)
            {
                // You might want to add animation handling for knockback here
            }
            return;
        }

        if (timeUntilNextDodge > 0)
        {
            timeUntilNextDodge -= Time.deltaTime;
        }

        if (isDodging || isDamagedAnimationPlaying)
        {
            if (isDodging)
            {
                HandleDodge();
            }
            return;
        }

        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");

        Vector3 moveDirection = new Vector3(horizontalInput, 0f, verticalInput).normalized;

        if (moveDirection.magnitude > 0.01f)
        {
            lastMoveInputDirection = moveDirection;
        }

        // Use movement speed from PlayerStats
        float currentSpeed = playerStats != null ? playerStats.MovementSpeed : 5f; 
        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
        {
            currentSpeed *= sprintSpeedMultiplier;
        }

        Vector3 finalMovement = moveDirection * currentSpeed;

        if (characterController != null)
        {
            characterController.Move(finalMovement * Time.deltaTime);
            transform.position = new Vector3(transform.position.x, 0f, transform.position.z);
        }

        if (Input.GetKeyDown(KeyCode.Space) && timeUntilNextDodge <= 0 && !isDodging)
        {
            InitiateDodge(moveDirection);
        }

        if (gameCamera != null)
        {
            if (crosshairUI != null)
            {
                crosshairUI.position = Input.mousePosition;
            }

            Vector3 mousePosScreen = Input.mousePosition;
            mousePosScreen.z = gameCamera.WorldToScreenPoint(transform.position + characterController.center).z;
            Vector3 mouseWorldPosition = gameCamera.ScreenToWorldPoint(mousePosScreen);

            if (mouseWorldPosition.x > transform.position.x)
            {
                if (transform.localScale.x > 0)
                {
                    transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
                }
            }
            else if (mouseWorldPosition.x < transform.position.x)
            {
                if (transform.localScale.x < 0)
                {
                    transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
                }
            }
        }
        else
        {
            Debug.LogWarning("[PlayerMovement] gameCamera not assigned, cannot perform aiming logic or position crosshair.");
            if (crosshairUI != null) crosshairUI.gameObject.SetActive(false);
        }

        // Use fire rate from PlayerStats
        float actualFireRate = playerStats != null ? playerStats.FireRate : 5f; // Fallback to a default if PlayerStats is null
        if (Input.GetMouseButton(0) && Time.time >= nextFireTime)
        {
            Shoot();
            nextFireTime = Time.time + 1f / actualFireRate;
        }

        if (spumScript != null && !isDodging && !isDamagedAnimationPlaying)
        {
            bool isMoving = moveDirection.magnitude > 0.01f;

            if (isMoving != wasMoving)
            {
                string targetStateKey = isMoving ? PlayerState.MOVE.ToString() : PlayerState.IDLE.ToString();

                if (spumScript.StateAnimationPairs != null && spumScript.StateAnimationPairs.ContainsKey(targetStateKey))
                {
                    var animationsList = spumScript.StateAnimationPairs[targetStateKey];
                    if (animationsList != null && animationsList.Count > 0)
                    {
                        if (isMoving)
                        {
                            spumScript.PlayAnimation(PlayerState.MOVE, 0);
                        }
                        else
                        {
                            spumScript.PlayAnimation(PlayerState.IDLE, 0);
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"Animation list for state '{targetStateKey}' is empty in SPUM_Prefabs. Cannot play animation.");
                    }
                }
                else
                {
                    Debug.LogWarning($"State '{targetStateKey}' not found in SPUM_Prefabs.StateAnimationPairs. Cannot play animation.");
                }
                wasMoving = isMoving;
            }
        }
    }

    void LateUpdate()
    {
        if (cameraTarget != null && gameCamera != null)
        {
            Vector3 desiredPosition = cameraTarget.position + cameraFixedOffset;
            Vector3 smoothedPosition = Vector3.Lerp(gameCamera.transform.position, desiredPosition, smoothSpeed);
            gameCamera.transform.position = smoothedPosition;
        }
    }

    void InitiateDodge(Vector3 currentMoveInput)
    {
        isDodging = true;
        dodgeStartTime = Time.time;
        timeUntilNextDodge = dodgeCooldown;

        if (currentMoveInput.magnitude > 0.01f)
        {
            dodgeDirection = currentMoveInput.normalized;
        }
        else if (lastMoveInputDirection.magnitude > 0.01f)
        {
            dodgeDirection = lastMoveInputDirection.normalized;
        }
        else
        {
            if (transform.localScale.x < 0)
            {
                dodgeDirection = Vector3.right;
            }
            else
            {
                dodgeDirection = Vector3.left;
            }
        }

        Debug.Log($"Initiating dodge in direction: {dodgeDirection}");
        if (spumScript != null) spumScript.PlayAnimation(PlayerState.IDLE, 0);
    }

    void HandleDodge()
    {
        float normalizedTime = (Time.time - dodgeStartTime) / dodgeDuration;

        if (normalizedTime < 1f)
        {
            Vector3 horizontalDodge = dodgeDirection * dodgeSpeed * Time.deltaTime;

            float currentYOffset = Mathf.Sin(normalizedTime * Mathf.PI) * 0.5f;

            float previousYOffset = 0f;
            if (Time.time - Time.deltaTime > dodgeStartTime)
            {
                float prevNormalizedTime = (Time.time - Time.deltaTime - dodgeStartTime) / dodgeDuration;
                if (prevNormalizedTime < 0) prevNormalizedTime = 0;
                previousYOffset = Mathf.Sin(prevNormalizedTime * Mathf.PI) * 0.5f;
            }

            float deltaY = currentYOffset - previousYOffset;

            Vector3 dodgeMovement = horizontalDodge + new Vector3(0, deltaY, 0);

            if (characterController != null)
            {
                characterController.Move(dodgeMovement);
            }

            float targetZRotation = normalizedTime * 360f;
            Quaternion currentFixedRotation = playerFixedRotation;

            transform.rotation = Quaternion.Euler(currentFixedRotation.eulerAngles.x, currentFixedRotation.eulerAngles.y, currentFixedRotation.eulerAngles.z + targetZRotation);

        }
        else
        {
            isDodging = false;
            transform.rotation = playerFixedRotation;

            if (spumScript != null)
            {
                float horizontalInput = Input.GetAxis("Horizontal");
                float verticalInput = Input.GetAxis("Vertical");
                bool isMovingAfterDodge = new Vector3(horizontalInput, 0f, verticalInput).magnitude > 0.01f;

                if (isMovingAfterDodge)
                {
                    spumScript.PlayAnimation(PlayerState.MOVE, 0);
                }
                else
                {
                    spumScript.PlayAnimation(PlayerState.IDLE, 0);
                }
            }
        }
    }

    void Shoot()
    {
        if (projectilePrefab == null)
        {
            Debug.LogWarning("[PlayerMovement] Projectile Prefab is not assigned! Cannot shoot.");
            return;
        }
        if (firePoint == null)
        {
            Debug.LogError("[PlayerMovement] Fire Point Transform is NOT assigned! Automatic placement will fail. Please assign the 'FirePoint' child GameObject in the Inspector.");
            return;
        }
        if (playerStats == null)
        {
            Debug.LogError("[PlayerMovement] PlayerStats component is missing! Cannot calculate projectile damage. Defaulting to 5 damage.");
        }

        Vector3 mousePosScreen = Input.mousePosition;
        mousePosScreen.z = gameCamera.WorldToScreenPoint(transform.position).z;
        Vector3 mouseWorldPosition = gameCamera.ScreenToWorldPoint(mousePosScreen);

        Vector3 playerFlatPosition = new Vector3(transform.position.x, 0f, transform.position.z);
        Vector3 targetFlatPosition = new Vector3(mouseWorldPosition.x, 0f, mouseWorldPosition.z);

        Vector3 shootDirection = (targetFlatPosition - playerFlatPosition).normalized;

        Vector3 desiredFirePointWorldPosition = new Vector3(playerFlatPosition.x, transform.position.y + characterController.center.y, playerFlatPosition.z) + shootDirection * (characterController.radius + autoFirePointOffset);

        firePoint.position = desiredFirePointWorldPosition;

        // Instantiate projectile with fixed X (90), Y tracking shootDirection, and fixed Z (0)
        GameObject projectileGO = Instantiate(projectilePrefab, firePoint.position, Quaternion.Euler(90f, Quaternion.LookRotation(shootDirection).eulerAngles.y, 0f));

        // ADDED: Play PlayerFire SFX
        AudioManager.Instance.PlaySFX(SFXType.PlayerFire);

        Collider projectileCollider = projectileGO.GetComponent<Collider>();
        if (projectileCollider != null && characterController != null)
        {
            // NEW: Ensure collision is ignored between player's own collider and their projectiles.
            // This is crucial to prevent self-collision upon firing.
            Physics.IgnoreCollision(characterController, projectileCollider, true);
        }
        else
        {
            if (projectileCollider == null) Debug.LogWarning("[PlayerMovement] Projectile prefab does not have a Collider component. Cannot ignore collisions.");
            if (characterController == null) Debug.LogError("[PlayerMovement] CharacterController is null during Shoot. This should not happen.");
        }

        ProjectileBehavior projectileBehavior = projectileGO.GetComponent<ProjectileBehavior>();
        if (projectileBehavior != null)
        {
            // Pass the calculated damage to the projectile
            float calculatedDamage = playerStats != null ? playerStats.GetCalculatedProjectileDamage() : 5f;
            projectileBehavior.SetDamage(calculatedDamage);

            projectileBehavior.SetDirection(shootDirection, projectileSpeed);
            projectileBehavior.targetTag = "Enemy";
        }
        else
        {
            Debug.LogWarning("Projectile prefab has no ProjectileBehavior. It may not move or destroy itself correctly.");
            Rigidbody rb = projectileGO.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = shootDirection * projectileSpeed;
            }
            else
            {
                Debug.LogError("Projectile prefab has no ProjectileBehavior or Rigidbody. Cannot set its initial velocity.");
            }
        }
    }

    private void PlayerTakesDamage()
    {
        if (!isDamagedAnimationPlaying && !isDead)
        {
            Debug.Log("[PlayerMovement] Player took damage (event triggered)! Starting DAMAGED animation sequence.");
            StartCoroutine(HandleDamagedAnimation());
        }
        else if (isDamagedAnimationPlaying)
        {
            Debug.Log("[PlayerMovement] Player took damage, but DAMAGED animation is already playing. Skipping new animation call.");
        }
    }

    private IEnumerator HandleDamagedAnimation()
    {
        isDamagedAnimationPlaying = true;

        if (spumScript != null && spumScript._anim != null)
        {
            spumScript._anim.Play("DAMAGED");
            Debug.Log("[PlayerMovement] Playing DAMAGED animation.");

            float animationLength = 0f; // Declare animationLength here
            float startTime = Time.time;
            const float maxWaitTime = 1.0f;

            while (!spumScript._anim.GetCurrentAnimatorStateInfo(0).IsName("DAMAGED") &&
                    Time.time < startTime + maxWaitTime)
            {
                yield return null;
            }

            AnimatorStateInfo stateInfo = spumScript._anim.GetCurrentAnimatorStateInfo(0);
            if (stateInfo.IsName("DAMAGED"))
            {
                animationLength = stateInfo.length;
                Debug.Log($"[PlayerMovement] Found DAMAGED animation length from state info: {animationLength} seconds.");
            }
            else
            {
                AnimatorClipInfo[] clipInfo = spumScript._anim.GetCurrentAnimatorClipInfo(0);
                foreach (var clip in clipInfo)
                {
                    if (clip.clip.name.ToUpper().Contains("DAMAGED"))
                    {
                        animationLength = clip.clip.length;
                        Debug.Log($"[PlayerMovement] Found DAMAGED animation length by iterating clips (fallback): {animationLength} seconds.");
                        break;
                    }
                }
            }


            if (animationLength <= 0)
            {
                Debug.LogWarning("[PlayerMovement] Could not determine DAMAGED animation length. Defaulting to 0.5 seconds for recovery.");
                animationLength = 0.5f;
            }

            yield return new WaitForSeconds(animationLength);

            isDamagedAnimationPlaying = false;

            float horizontalInput = Input.GetAxis("Horizontal");
            float verticalInput = Input.GetAxis("Vertical");
            bool isMoving = new Vector3(horizontalInput, 0f, verticalInput).magnitude > 0.01f;

            if (spumScript != null)
            {
                if (isMoving)
                {
                    spumScript.PlayAnimation(PlayerState.MOVE, 0);
                    wasMoving = true;
                }
                else
                {
                    spumScript.PlayAnimation(PlayerState.IDLE, 0);
                    wasMoving = false;
                }
                Debug.Log("[PlayerMovement] DAMAGED animation finished. Reverting to " + (isMoving ? "MOVE" : "IDLE") + " animation.");
            }
        }
        else
        {
            Debug.LogWarning("[PlayerMovement] SPUM Animator (_anim) is not assigned in SPUM_Prefabs, or spumScript is null. Cannot play DAMAGED animation. Resetting flag.");
            isDamagedAnimationPlaying = false;
        }
    }

    private void PlayerDies()
    {
        Debug.Log("[PlayerMovement] Player has died!");
        isDead = true;

        AudioManager.Instance.PlaySFX(SFXType.PlayerDeath);

        // NEW: Stop any ongoing damage feedback/knockback coroutines on CharacterDamageFeedback
        if (damageFeedback != null)
        {
            damageFeedback.StopAllCoroutines(); // This stops coroutines on the CharacterDamageFeedback script
            Debug.Log("[PlayerMovement] Stopped CharacterDamageFeedback coroutines to prevent 'inactive controller' error.");
        }

        StopAllCoroutines(); // Stops coroutines on THIS PlayerMovement script (e.g., HandleDodge, HandleDamagedAnimation)
        isDodging = false;
        isDamagedAnimationPlaying = false;

        // Now it's safe to disable the CharacterController
        if (characterController != null)
        {
            characterController.enabled = false;
        }
        this.enabled = false; // Disable this script (PlayerMovement)

        if (spumScript != null && spumScript._anim != null)
        {
            spumScript._anim.Play("DEATH");
            Debug.Log("[PlayerMovement] Attempting to play DEATH animation directly and exclusively.");
        }
        else if (spumScript != null)
        {
            Debug.LogWarning("[PlayerMovement] SPUM Animator (_anim) is not assigned in SPUM_Prefabs. Cannot play DEATH animation.");
        }

        if (crosshairUI != null)
        {
            crosshairUI.gameObject.SetActive(false);
        }
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // Trigger the Game Over UI via UIManager
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowGameOverPanel();
        }
        else
        {
            Debug.LogError("[PlayerMovement] UIManager.Instance is null. Cannot show Game Over panel!");
        }

        // The Player GameObject will be destroyed AFTER the death sequence
        // which now includes showing the Game Over panel.
        StartCoroutine(HandleDeathSequence());
    }

    private IEnumerator HandleDeathSequence()
    {
        float deathAnimationLength = 0f;

        if (spumScript != null && spumScript._anim != null)
        {
            float startTime = Time.time;
            const float maxAnimationWaitTime = 0.5f;

            while (!spumScript._anim.GetCurrentAnimatorStateInfo(0).IsName("DEATH") &&
                    Time.time < startTime + maxAnimationWaitTime)
            {
                yield return null;
            }

            AnimatorStateInfo stateInfo = spumScript._anim.GetCurrentAnimatorStateInfo(0);
            if (stateInfo.IsName("DEATH"))
            {
                deathAnimationLength = stateInfo.length;
                Debug.Log($"[PlayerMovement] Found DEATH animation length from state info: {deathAnimationLength} seconds.");
            }
            else
            {
                AnimatorClipInfo[] clipInfo = spumScript._anim.GetCurrentAnimatorClipInfo(0);
                foreach (var clip in clipInfo)
                {
                    if (clip.clip.name.ToUpper().Contains("DEATH"))
                    {
                        deathAnimationLength = clip.clip.length;
                        Debug.Log($"[PlayerMovement] Found DEATH animation length by iterating clips (fallback): {deathAnimationLength} seconds. Clip Name: {clip.clip.name}");
                        break;
                    }
                }
                Debug.LogWarning("[PlayerMovement] DEATH state not directly found by name after waiting. Ensure your Animator state for death is named 'DEATH'.");
            }
        }

        if (deathAnimationLength <= 0)
        {
            Debug.LogWarning("[PlayerMovement] Could not determine DEATH animation length. Defaulting to 0.5 second for death animation duration.");
            deathAnimationLength = 0.5f;
        }

        yield return new WaitForSeconds(deathAnimationLength);
        Debug.Log("[PlayerMovement] Death animation (or fallback duration) finished.");

        if (deathParticlePrefab != null)
        {
            GameObject particleInstance = Instantiate(deathParticlePrefab, transform.position, Quaternion.identity);
            Debug.Log("[PlayerMovement] Playing death particle effect upon destruction sequence.");
        }
        else
        {
            Debug.LogWarning("[PlayerMovement] Death Particle Prefab is not assigned. No particle effect will play.");
        }

        yield return new WaitForSeconds(0f); // Small delay to allow particle to appear before destroy

        Debug.Log("[PlayerMovement] Player GameObject being destroyed.");
        // The scene reload or application quit will handle the destruction of objects,
        // but destroying the player here explicitly is fine if it's the last step.
        Destroy(gameObject);
    }

    // NEW: Method to control player movement/actions via the UpgradeManager
    public void SetMovementAllowed(bool allowed)
    {
        canMove = allowed;
        if (!canMove)
        {
            // If movement is disallowed, immediately stop character controller's current movement
            if (characterController != null)
            {
                // CharacterController.Move with zero vector will effectively stop it.
                // We're already returning early from Update, so no explicit stop needed here.
            }
            // Also ensure crosshair is hidden when paused
            if (crosshairUI != null) crosshairUI.gameObject.SetActive(false);
            
            // If any continuous actions were happening, stop them
            if (isDodging) { StopCoroutine("HandleDodge"); isDodging = false; }
            if (isDamagedAnimationPlaying) { StopCoroutine("HandleDamagedAnimation"); isDamagedAnimationPlaying = false; }
            if (spumScript != null) spumScript.PlayAnimation(PlayerState.IDLE, 0); // Force idle animation
            wasMoving = false;
        }
        else
        {
            // When movement is re-allowed, ensure crosshair is visible again
            if (crosshairUI != null) crosshairUI.gameObject.SetActive(true);
        }
        Debug.Log($"[PlayerMovement] Player movement allowed: {canMove}");
    }
}