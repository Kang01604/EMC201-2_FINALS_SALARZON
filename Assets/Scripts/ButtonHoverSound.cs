using UnityEngine;
using UnityEngine.EventSystems; // Required for IPointerEnterHandler

// This script should be attached to any UI Button or Selectable element
// you want to have a hover sound.
public class ButtonHoverSound : MonoBehaviour, IPointerEnterHandler
{
    /// <summary>
    /// This function is called when the pointer enters the object's collider or UI element.
    /// It plays the CursorHover SFX using the AudioManager singleton.
    /// </summary>
    /// <param name="eventData">PointerEventData associated with the event.</param>
    public void OnPointerEnter(PointerEventData eventData)
    {
        // Check if the AudioManager instance exists before trying to play a sound
        if (AudioManager.Instance != null)
        {
            // Request the AudioManager to play the CursorHover SFX
            AudioManager.Instance.PlaySFX(SFXType.CursorHover);
            Debug.Log($"[ButtonHoverSound] Playing {SFXType.CursorHover} SFX.");
        }
        else
        {
            Debug.LogWarning("[ButtonHoverSound] AudioManager.Instance is null! Cannot play hover sound.", this);
        }
    }
}