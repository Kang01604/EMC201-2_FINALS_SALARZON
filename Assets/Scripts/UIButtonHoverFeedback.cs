using UnityEngine;
using UnityEngine.EventSystems; // Required for IPointerEnterHandler and IPointerExitHandler
using UnityEngine.UI; // Required for Image component

// This script should be attached to any UI Button or Selectable element
// that you want to have hover visual feedback.
public class UIButtonHoverFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Hover Settings")]
    [Tooltip("The scale to apply when the button is hovered over (e.g., 1.05 for a 5% increase).")]
    [SerializeField] private Vector3 hoverScale = new Vector3(1.05f, 1.05f, 1.05f);

    [Tooltip("The color to apply when the button is hovered over.")]
    [SerializeField] private Color hoverColor = Color.yellow;

    private Vector3 originalScale;
    private Color originalColor;
    private Image buttonImage;
    private RectTransform rectTransform; // Used for scale

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        buttonImage = GetComponent<Image>(); // Get the Image component of the button

        if (rectTransform == null)
        {
            Debug.LogError("[UIButtonHoverFeedback] RectTransform not found on this GameObject. Cannot apply scale feedback.", this);
            enabled = false; // Disable script if essential component is missing
        }

        if (buttonImage == null)
        {
            Debug.LogWarning("[UIButtonHoverFeedback] Image component not found on this GameObject. Color feedback will not work.", this);
            // We won't disable the script, as scale might still work.
        }
        
        // Store the original properties
        originalScale = rectTransform.localScale;
        if (buttonImage != null)
        {
            originalColor = buttonImage.color;
        }
    }

    /// <summary>
    /// Called when the mouse pointer enters the UI element.
    /// Applies hover scale and color.
    /// </summary>
    /// <param name="eventData">Pointer event data.</param>
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (rectTransform != null)
        {
            rectTransform.localScale = hoverScale;
        }
        if (buttonImage != null)
        {
            buttonImage.color = hoverColor;
        }
        Debug.Log($"{gameObject.name} hovered! Scale: {hoverScale}, Color: {hoverColor}");
    }

    /// <summary>
    /// Called when the mouse pointer exits the UI element.
    /// Reverts to original scale and color.
    /// </summary>
    /// <param name="eventData">Pointer event data.</param>
    public void OnPointerExit(PointerEventData eventData)
    {
        if (rectTransform != null)
        {
            rectTransform.localScale = originalScale;
        }
        if (buttonImage != null)
        {
            buttonImage.color = originalColor;
        }
        Debug.Log($"{gameObject.name} unhovered! Reverted to original.");
    }
}