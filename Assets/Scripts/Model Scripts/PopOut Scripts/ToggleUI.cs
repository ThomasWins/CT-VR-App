using UnityEngine;
using UnityEngine.UI; 


public class ToggleUI : MonoBehaviour
{
    [Header("Target & State")]
    [Tooltip("The GameObject whose visibility will be toggled by moving it.")]
    public GameObject targetObject;

    [Header("Movement Settings")]
    [Tooltip("The temporary position (World Space) to move the object when 'OFF'.")]
    public Vector3 remotePosition = new Vector3(0, 10000f, 0);
    
    [Header("Button Graphics")]
    [Tooltip("The Sprite to show when the object IS visible (Toggle ON).")]
    public Sprite onSprite;
    [Tooltip("The Sprite to show when the object IS NOT visible (Toggle OFF).")]
    public Sprite offSprite;

    [Header("Alpha Settings")]
    [Tooltip("Alpha value (0-255) to apply to the button image when the toggle is ON")]
    [Range(0,255)] public int onAlpha = 150;
    [Tooltip("Alpha value (0-255) to apply to the button image when the toggle is OFF")]
    [Range(0, 255)] public int offAlpha = 255;
    
    private Vector3 originalPosition; 
    private bool isVisible = true;    
    private Image buttonImage;
    private Button toggleButton;


    void Start()
    {
        buttonImage = GetComponent<Image>();
        if (buttonImage == null)
        {
            Debug.LogError("ToggleRemote requires an Image component on the same GameObject to change sprites.");
            return;
        }

        toggleButton = GetComponent<Button>();
        if (toggleButton != null)
        {
            toggleButton.onClick.AddListener(ToggleObjectVisibility);
        }
        else
        {
            Debug.LogError("ToggleRemote requires a Button component on the same GameObject.");
            return;
        }

        if (targetObject != null)
        {
            // Assuming the object starts visible save its initial position
            originalPosition = targetObject.transform.position;
            isVisible = true; 
        }
        else
        {
            Debug.LogError("Target GameObject is not assigned in the Inspector.");
            return;
        }

        UpdateVisuals(isVisible);
    }

// Basically instead of making the UI Invisibile we move it far away to preserve any active states like the ruler or planes
    public void ToggleObjectVisibility()
    {
        if (targetObject == null) return;

        isVisible = !isVisible;

        if (isVisible)
        {
            targetObject.transform.position = originalPosition;
        }
        else
        {
            targetObject.transform.position = remotePosition;
        }

        UpdateVisuals(isVisible);
    }


    private void UpdateVisuals(bool isActive)
    {
        if (buttonImage != null && onSprite != null && offSprite != null)
        {
            buttonImage.sprite = isActive ? onSprite : offSprite;
            // Apply alpha (preserve RGB)
            Color c = buttonImage.color;
            float a = (isActive ? onAlpha : offAlpha) / 255f;
            c.a = a;
            buttonImage.color = c;
        } 
        else if (buttonImage == null)
        {
            Debug.LogError("Button Image component is missing.");
        }
        else
        {
            Debug.LogWarning("Please assign both the 'On Sprite' and 'Off Sprite' in the Inspector.");
        }
    }
}