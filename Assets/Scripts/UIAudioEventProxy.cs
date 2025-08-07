using UnityEngine;

// This script acts as a proxy to trigger audio events via the AudioManager singleton.
// Attach this script to a GameObject in each scene where you need to assign
// UI Button OnClick() events in the Inspector (e.g., your Canvas).
public class UIAudioEventProxy : MonoBehaviour
{
    // --- Methods to be assigned in Unity UI OnClick() Events ---

    /// <summary>
    /// Triggers the default button click SFX via AudioManager.
    /// Assign this to a UI Button's OnClick event.
    /// </summary>
    public void PlayDefaultButtonSFX()
    {
        if (AudioManager.Instance != null)
        {
            // Call the method that already exists in your AudioManager instance
            AudioManager.Instance.PlayDefaultButtonSFX();
            Debug.Log("[UIAudioEventProxy] Triggered PlayDefaultButtonSFX.");
        }
        else
        {
            Debug.LogWarning("[UIAudioEventProxy] AudioManager.Instance is null! Cannot play Default Button SFX. Ensure AudioManager is in your initial scene and uses DontDestroyOnLoad.");
        }
    }

    /// <summary>
    /// Triggers the cancel button click SFX via AudioManager.
    /// Assign this to a UI Button's OnClick event.
    /// </summary>
    public void PlayCancelButtonSFX()
    {
        if (AudioManager.Instance != null)
        {
            // Call the method that already exists in your AudioManager instance
            AudioManager.Instance.PlayCancelButtonSFX();
            Debug.Log("[UIAudioEventProxy] Triggered PlayCancelButtonSFX.");
        }
        else
        {
            Debug.LogWarning("[UIAudioEventProxy] AudioManager.Instance is null! Cannot play Cancel Button SFX.");
        }
    }

    /// <summary>
    /// Triggers the upgrade button click SFX via AudioManager.
    /// Assign this to a UI Button's OnClick event.
    /// </summary>
    public void PlayUpgradeButtonSFX()
    {
        if (AudioManager.Instance != null)
        {
            // Call the method that already exists in your AudioManager instance
            AudioManager.Instance.PlayUpgradeButtonSFX();
            Debug.Log("[UIAudioEventProxy] Triggered PlayUpgradeButtonSFX.");
        }
        else
        {
            Debug.LogWarning("[UIAudioEventProxy] AudioManager.Instance is null! Cannot play Upgrade Button SFX.");
        }
    }

    // IMPORTANT: For the Cursor Hover SFX, the ButtonHoverSound script you have
    // should continue to call AudioManager.Instance.PlayCursorHoverSFX() directly.
    // There's no need for a proxy for the hover sound, as ButtonHoverSound is already
    // designed to talk to the AudioManager.
}