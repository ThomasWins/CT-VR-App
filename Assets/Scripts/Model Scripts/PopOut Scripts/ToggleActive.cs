using UnityEngine;
using UnityEngine.UI; 

public class ToggleActive : MonoBehaviour
{
    [Header("Target & State")]
    [Tooltip("The GameObject whose active state will be toggled.")]
    public GameObject targetObject;

    [Header("Button Graphics")]
    [Tooltip("The Sprite to show when the object IS active (Toggle ON).")]
    public Sprite onSprite;
    [Tooltip("The Sprite to show when the object IS NOT active (Toggle OFF).")]
    public Sprite offSprite;

    [Header("Alpha Settings")]
    [Tooltip("Alpha value (0-255) to apply to the button image when the toggle is ON")]
    [Range(0, 255)] public int onAlpha = 255; 
    [Tooltip("Alpha value (0-255) to apply to the button image when the toggle is OFF")]
    [Range(0, 255)] public int offAlpha = 150; 

    private Image buttonImage;
    private Button toggleButton;


    void Start()
    {
        buttonImage = GetComponent<Image>();
        if (buttonImage == null)
        {
            Debug.LogError("ToggleActive requires an Image component on the same GameObject to change sprites.");
            return;
        }

        toggleButton = GetComponent<Button>();
        if (toggleButton != null)
        {
            toggleButton.onClick.AddListener(ToggleObjectActiveState);
        }
        else
        {
            Debug.LogError("ToggleActive requires a Button component on the same GameObject.");
            return;
        }

        if (targetObject == null)
        {
            Debug.LogError("Target GameObject is not assigned in the Inspector.");
            return;
        }

        UpdateVisuals(targetObject.activeSelf);
    }


    public void ToggleObjectActiveState()
    {
        if (targetObject == null) return;

        bool isActive = !targetObject.activeSelf;

        targetObject.SetActive(isActive);

        UpdateVisuals(isActive);
    }

    // If we want to ensure the object is turned off specifically
    public void ToggleObjectOFF()
    {
        if (targetObject == null) return;

        targetObject.SetActive(false);
        UpdateVisuals(false);
    }

    private void UpdateVisuals(bool isActive)
    {
        if (buttonImage != null && onSprite != null && offSprite != null)
        {
            // Set the appropriate sprite
            buttonImage.sprite = isActive ? onSprite : offSprite;
            
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