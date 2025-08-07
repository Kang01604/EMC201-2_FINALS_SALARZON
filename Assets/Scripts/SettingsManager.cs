using UnityEngine;
using UnityEngine.UI; // Required for Slider
using TMPro; // Required for TextMeshProUGUI

public class SettingsManager : MonoBehaviour
{
    [Header("UI References")]
    public Slider bgmSlider;
    public TextMeshProUGUI bgmValueText;
    public Slider sfxSlider;
    public TextMeshProUGUI sfxValueText;

    private AudioManager audioManager;

    void Awake()
    {
        // Find the AudioManager instance in the scene
        audioManager = AudioManager.Instance;
        if (audioManager == null)
        {
            Debug.LogError("[SettingsManager] AudioManager instance not found! Audio settings will not function.", this);
        }
    }

    void OnEnable()
    {
        // When the settings panel becomes active, load current volumes and set up listeners
        if (audioManager != null)
        {
            if (bgmSlider != null)
            {
                bgmSlider.value = audioManager.BgmVolume;
                bgmSlider.onValueChanged.AddListener(OnBGMVolumeChanged);
            }
            if (sfxSlider != null)
            {
                sfxSlider.value = audioManager.SfxVolume;
                sfxSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
            }
            UpdateVolumeTexts(); // Update text displays immediately
        }
    }

    void OnDisable()
    {
        // When the settings panel becomes inactive, remove listeners to prevent memory leaks
        if (bgmSlider != null)
        {
            bgmSlider.onValueChanged.RemoveListener(OnBGMVolumeChanged);
        }
        if (sfxSlider != null)
        {
            sfxSlider.onValueChanged.RemoveListener(OnSFXVolumeChanged);
        }
    }

    private void OnBGMVolumeChanged(float value)
    {
        if (audioManager != null)
        {
            audioManager.BgmVolume = value; // Update AudioManager's volume
            UpdateVolumeTexts(); // Update the UI text
        }
    }

    private void OnSFXVolumeChanged(float value)
    {
        if (audioManager != null)
        {
            audioManager.SfxVolume = value; // Update AudioManager's volume
            UpdateVolumeTexts(); // Update the UI text
            // NEW: Play a small SFX when slider is dragged, using the new SFXType system
            // You might want to debounce this or only play on mouse up for performance
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX(SFXType.SliderDrag);
            }
        }
    }

    /// <summary>
    /// Updates the TextMeshProUGUI elements to show current volume percentages.
    /// </summary>
    private void UpdateVolumeTexts()
    {
        if (bgmValueText != null && audioManager != null)
        {
            bgmValueText.text = $"{Mathf.RoundToInt(audioManager.BgmVolume * 100)}%";
        }
        if (sfxValueText != null && audioManager != null)
        {
            sfxValueText.text = $"{Mathf.RoundToInt(audioManager.SfxVolume * 100)}%";
        }
    }
}