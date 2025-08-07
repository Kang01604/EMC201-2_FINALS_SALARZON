using UnityEngine;
using System.Collections.Generic; // Required for List and Dictionary
using UnityEngine.SceneManagement; // Required for scene loading events

// IMPORTANT: Ensure your SFXType enum is defined in a separate file named SFXType.cs,
// or if it must be in this file, it needs to be placed at the very top,
// BEFORE the 'public class AudioManager' declaration, but AFTER all 'using' statements.

public class AudioManager : MonoBehaviour
{
    // Singleton instance backing field
    private static AudioManager _instance;

    // Public static property to access the singleton instance.
    // This getter will ensure an AudioManager instance exists and is initialized.
    public static AudioManager Instance
    {
        get
        {
            // If the instance doesn't exist yet, try to find it in the scene
            if (_instance == null)
            {
                _instance = FindObjectOfType<AudioManager>();

                // If no instance exists after searching, try to load it from a Prefab in Resources
                if (_instance == null)
                {
                    // IMPORTANT: Ensure your AudioManager Prefab is in a folder named "Resources"
                    // e.g., Assets/Resources/AudioManager.prefab or Assets/MyPrefabs/Resources/AudioManager.prefab
                    GameObject audioManagerPrefab = Resources.Load<GameObject>("AudioManager"); // Assumes the Prefab name is "AudioManager"

                    if (audioManagerPrefab != null)
                    {
                        // Instantiate the Prefab to create the singleton instance
                        GameObject singletonObject = Instantiate(audioManagerPrefab);
                        _instance = singletonObject.GetComponent<AudioManager>();
                        singletonObject.name = "AudioManager (Singleton)"; // Rename for clarity in Hierarchy
                        Debug.Log("[AudioManager] Instance loaded from Prefab and created automatically.");
                    }
                    else
                    {
                        // Fallback: If no prefab found in Resources, create a new GameObject dynamically
                        GameObject singletonObject = new GameObject("AudioManager");
                        _instance = singletonObject.AddComponent<AudioManager>();
                        Debug.LogWarning("[AudioManager] No AudioManager Prefab found in Resources. Creating a new instance dynamically. Please create a Prefab named 'AudioManager' and place it in a 'Resources' folder for consistent settings.", singletonObject);
                    }
                }

                // Ensure it persists across scene loads
                DontDestroyOnLoad(_instance.gameObject);
            }
            return _instance;
        }
    }

    [Header("Audio Sources (Managed Automatically)")]
    [Tooltip("AudioSource for Background Music (BGM). Automatically created if not assigned.")]
    [SerializeField] private AudioSource bgmAudioSource;

    // Dictionary to hold dedicated AudioSources for each SFXType
    private Dictionary<SFXType, AudioSource> sfxTypeAudioSources = new Dictionary<SFXType, AudioSource>();

    [Header("SFX Clips")]
    [Tooltip("Assign all your SFX clips here, mapped to their SFXType.")]
    public List<SFXClipEntry> sfxClips = new List<SFXClipEntry>();
    private Dictionary<SFXType, AudioClip> sfxClipDictionary = new Dictionary<SFXType, AudioClip>();

    [Header("Scene Background Music")]
    [Tooltip("Assign BGM clips for specific scenes. AudioManager will switch BGM automatically.")]
    public List<SceneBGMEntry> sceneBGMs = new List<SceneBGMEntry>();

    // Private backing fields for volumes, with public properties for access
    private float _bgmVolume = 0.5f; // Default BGM volume
    private float _sfxVolume = 0.75f; // Default SFX volume

    // Public properties for BGM and SFX volume, which update the AudioSources and PlayerPrefs
    public float BgmVolume
    {
        get { return _bgmVolume; }
        set
        {
            _bgmVolume = Mathf.Clamp01(value); // Clamp between 0 and 1
            if (bgmAudioSource != null)
            {
                bgmAudioSource.volume = _bgmVolume;
            }
            PlayerPrefs.SetFloat("BGMVolume", _bgmVolume); // Save volume
            PlayerPrefs.Save(); // Ensure it's written to disk
            Debug.Log($"[AudioManager] BGM Volume set to: {_bgmVolume}");
        }
    }

    public float SfxVolume
    {
        get { return _sfxVolume; }
        set
        {
            _sfxVolume = Mathf.Clamp01(value); // Clamp between 0 and 1
            // Apply new volume to all SFX sources (dedicated ones)
            if (sfxTypeAudioSources != null)
            {
                foreach (var entry in sfxTypeAudioSources)
                {
                    if (entry.Value != null) // entry.Value is the AudioSource
                    {
                        entry.Value.volume = _sfxVolume;
                    }
                }
            }
            PlayerPrefs.SetFloat("SFXVolume", _sfxVolume); // Save volume
            PlayerPrefs.Save(); // Ensure it's written to disk
            Debug.Log($"[AudioManager] SFX Volume set to: {_sfxVolume}");
        }
    }

    // Flag to ensure initialization logic runs only once
    private bool _isInitialized = false;

    void Awake()
    {
        // This Awake() runs when the GameObject is loaded or created.
        // It's crucial for managing duplicates and ensuring persistence.
        if (_instance != null && _instance != this)
        {
            // If an instance already exists and it's not THIS one, destroy this duplicate.
            Debug.LogWarning("[AudioManager] Duplicate instance found, destroying self.", this);
            Destroy(gameObject);
            return; // Exit to prevent further execution for this duplicate
        }

        // If this is the designated instance (either found or newly created by the getter)
        _instance = this;

        // Ensure it persists across scene loads
        DontDestroyOnLoad(gameObject);

        // Perform initialization only if it hasn't been done yet for this instance.
        // This prevents re-initializing if it was created by the getter first.
        if (!_isInitialized)
        {
            InitializeAudioManager();
            _isInitialized = true;
        }
    }

    // NEW: Called once after Awake, when the script instance is being enabled.
    void Start()
    {
        // IMPORTANT FIX: Manually call OnSceneLoaded for the *initial* scene that the game starts in.
        // SceneManager.sceneLoaded event DOES NOT fire for the first scene loaded when the game begins.
        // We simulate that event here to ensure BGM plays correctly if you start directly in a scene.
        OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    void OnEnable()
    {
        // Subscribe to scene loaded event to automatically play BGM for new scenes
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        // Only unsubscribe if this is the actual singleton instance to prevent errors
        if (_instance == this)
        {
             SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    // New method to encapsulate all initialization logic that should run once
    private void InitializeAudioManager()
    {
        Debug.Log("[AudioManager] Initializing...");

        // --- Initialize BGM AudioSource ---
        // Automatically add if not assigned in Inspector
        if (bgmAudioSource == null)
        {
            Debug.LogWarning("[AudioManager] BGM AudioSource not assigned. Adding one automatically.", this);
            bgmAudioSource = gameObject.AddComponent<AudioSource>();
        }
        bgmAudioSource.loop = true; // BGM should loop by default
        bgmAudioSource.playOnAwake = false; // Don't play automatically
        bgmAudioSource.spatialBlend = 0; // Ensure 2D sound


        // --- Initialize SFX AudioSources ---
        // Clear existing dictionaries to avoid issues if Awake/Initialize runs unexpectedly multiple times (e.g. in editor)
        sfxClipDictionary.Clear();
        sfxTypeAudioSources.Clear(); 
        
        foreach (SFXClipEntry entry in sfxClips)
        {
            if (entry.clip == null)
            {
                Debug.LogWarning($"[AudioManager] SFXClipEntry for type {entry.type} has a null AudioClip. This SFX will not be playable.", this);
                continue; // Skip if clip is null
            }
            if (sfxClipDictionary.ContainsKey(entry.type))
            {
                Debug.LogWarning($"[AudioManager] Duplicate SFXClipEntry for type {entry.type}. Only the first one will be used.", this);
                continue; // Skip duplicates
            }

            sfxClipDictionary.Add(entry.type, entry.clip);

            // Create and configure a dedicated AudioSource for this SFXType
            AudioSource newSfxSource = gameObject.AddComponent<AudioSource>();
            newSfxSource.name = $"SFX_Source_{entry.type}"; // Name for clarity in Hierarchy
            newSfxSource.loop = false;
            newSfxSource.playOnAwake = false;
            newSfxSource.spatialBlend = 0; // Ensure 2D sound
            newSfxSource.clip = entry.clip; // Pre-assign the clip to the source for simplicity (PlayOneShot will override it if needed)

            sfxTypeAudioSources.Add(entry.type, newSfxSource);
        }

        // --- Load Volumes ---
        _bgmVolume = PlayerPrefs.GetFloat("BGMVolume", 0.5f); // 0.5f is the default if not found
        _sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 0.75f); // 0.75f is the default if not found

        // Apply loaded volumes to AudioSources
        bgmAudioSource.volume = _bgmVolume;
        foreach (var entry in sfxTypeAudioSources)
        {
            if (entry.Value != null)
            {
                entry.Value.volume = _sfxVolume;
            }
        }

        Debug.Log("[AudioManager] Initialized. BGM: " + _bgmVolume + ", SFX: " + _sfxVolume);
    }
    
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"[AudioManager] Scene loaded: {scene.name}. Checking for scene-specific BGM.");
        bool foundBGM = false;
        foreach (SceneBGMEntry entry in sceneBGMs)
        {
            // FIX (from previous): Added .Trim() to both scene name strings to handle potential hidden whitespace
            if (entry.sceneName.Trim() == scene.name.Trim()) 
            {
                if (entry.bgmClip != null)
                {
                    PlayBGM(entry.bgmClip, entry.loop);
                    foundBGM = true;
                }
                else
                {
                    Debug.LogWarning($"[AudioManager] Scene '{scene.name}' has a BGM entry but no AudioClip assigned. No BGM will play for this scene.", this);
                }
                break; // Found the entry for this scene
            }
        }

        if (!foundBGM)
        {
            // Optional: If no specific BGM is found for the scene, you might want to stop the current BGM
            // or play a default BGM. For now, we'll log.
            Debug.Log($"[AudioManager] No specific BGM found for scene: {scene.name}. Current BGM will continue or remain stopped.");
            // If you want to stop BGM if no specific one is set for a scene:
            // StopBGM();
        }
    }


    /// <summary>
    /// Plays a background music clip. Stops current BGM if one is playing.
    /// </summary>
    /// <param name="clip">The AudioClip to play as BGM.</param>
    /// <param name="loop">Whether the BGM should loop. Defaults to true.</param>
    public void PlayBGM(AudioClip clip, bool loop = true)
    {
        if (bgmAudioSource == null)
        {
            Debug.LogWarning("[AudioManager] BGM AudioSource is null. Cannot play BGM.", this);
            return;
        }

        // FIX (from previous): Added a check to ensure the AudioClip itself is not null
        if (clip == null)
        {
            Debug.LogWarning("[AudioManager] Attempted to play BGM with a null AudioClip. Cannot play sound.", this);
            return;
        }

        // Only play if the clip is different or not currently playing (to avoid re-starting the same song)
        if (bgmAudioSource.clip != clip || !bgmAudioSource.isPlaying)
        {
            bgmAudioSource.clip = clip;
            bgmAudioSource.loop = loop;
            bgmAudioSource.Play();
            Debug.Log($"[AudioManager] Playing BGM: {clip.name} (Loop: {loop})");
        }
        else
        {
            Debug.Log($"[AudioManager] BGM '{clip.name}' is already playing. Not restarting.");
        }
    }

    /// <summary>
    /// Stops the currently playing background music.
    /// </summary>
    public void StopBGM()
    {
        if (bgmAudioSource != null && bgmAudioSource.isPlaying)
        {
            bgmAudioSource.Stop();
            Debug.Log("[AudioManager] BGM stopped.");
        }
    }

    /// <summary>
    /// Plays a sound effect of a specific type on its dedicated AudioSource.
    /// </summary>
    /// <param name="type">The type of SFX to play.</param>
    public void PlaySFX(SFXType type)
    {
        if (!sfxTypeAudioSources.ContainsKey(type) || sfxTypeAudioSources[type] == null)
        {
            Debug.LogWarning($"[AudioManager] No dedicated AudioSource found or assigned for SFXType: {type}. Make sure it's set up in SFX Clips and has a valid clip.", this);
            return;
        }

        AudioSource dedicatedSource = sfxTypeAudioSources[type];
        AudioClip clipToPlay = sfxClipDictionary.ContainsKey(type) ? sfxClipDictionary[type] : null;

        if (clipToPlay == null)
        {
            Debug.LogWarning($"[AudioManager] No AudioClip found in dictionary for SFXType: {type}. Cannot play sound.", this);
            return;
        }
        
        // Play the clip. If the dedicated source is already playing, PlayOneShot will override it.
        dedicatedSource.PlayOneShot(clipToPlay); 
        Debug.Log($"[AudioManager] Playing SFX: {type} ({clipToPlay.name}) on dedicated source.");
    }

    // --- HELPER METHODS FOR UI BUTTONS (to be assigned in OnClick events) ---

    /// <summary>
    /// Plays the SFX for a general/default button click.
    /// Assign this to Unity UI Button's OnClick event.
    /// </summary>
    public void PlayDefaultButtonSFX()
    {
        PlaySFX(SFXType.DefaultButton);
    }

    /// <summary>
    /// Plays the SFX for a cancel button click.
    /// Assign this to Unity UI Button's OnClick event.
    /// </summary>
    public void PlayCancelButtonSFX()
    {
        PlaySFX(SFXType.CancelButton);
    }

    /// <summary>
    /// Plays the SFX for an upgrade button click.
    /// Assign this to Unity UI Button's OnClick event.
    /// </summary>
    public void PlayUpgradeButtonSFX()
    {
        PlaySFX(SFXType.UpgradeButton);
    }

    /// <summary>
    /// Plays the SFX for a cursor hovering over a UI element.
    /// This method is typically called by a separate ButtonHoverSound script.
    /// </summary>
    public void PlayCursorHoverSFX()
    {
        PlaySFX(SFXType.CursorHover);
    }

    // You can add more specific SFX helper methods here if needed for other SFXTypes, eg.:
    // public void PlayPlayerFireSFX() { PlaySFX(SFXType.PlayerFire); }
    // public void PlayGameOverSFX() { PlaySFX(SFXType.GameOver); }
}

// Helper class to make SFX clip assignment in Inspector easier
[System.Serializable]
public class SFXClipEntry
{
    public SFXType type;
    public AudioClip clip;
}

// NEW: Helper class for assigning BGM clips per scene
[System.Serializable]
public class SceneBGMEntry
{
    [Tooltip("The exact name of the scene (as in File > Build Settings).")]
    public string sceneName;
    [Tooltip("The BGM clip to play when this scene loads.")]
    public AudioClip bgmClip;
    [Tooltip("Should this BGM clip loop?")]
    public bool loop = true;
}

// NOTE: Your SFXType enum still needs to be defined in a SFXType.cs file
// or above the AudioManager class in the same file.