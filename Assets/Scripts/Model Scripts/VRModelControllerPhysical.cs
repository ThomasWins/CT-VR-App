using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.InputSystem;

[RequireComponent(typeof(UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable))]
public class VRModelControllerPhysical : MonoBehaviour
{
    // THIS SCRIPT IS THE SAME AS THE OTHER, PHYSICAL ROTATES Y DIFFERENTLY THAN THIS ONE
    [Header("VR Input")]
    [Tooltip("The joystick or thumbstick input action to use for rotation and scaling.")]
    public InputActionProperty joystickInput;

    [Header("Control Settings")]
    [Tooltip("The speed of rotation (left/right joystick).")]
    public float rotationSpeed = 100f;

    [Tooltip("The speed of scaling (up/down joystick).")]
    public float scaleSpeed = 0.0005f; 

    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable interactable;
    public ModelManager modelManager; 
    private bool isSelecting = false;

    private void Awake()
    {
        interactable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>();

        if (modelManager == null)
        {
            Debug.LogError("ModelManager not found or not assigned! Please add a GameObject with the ModelManager script and assign it.");
        }
    }

    private void OnEnable()
    {
        if (joystickInput.action != null)
            joystickInput.action.Enable();

        interactable.selectEntered.AddListener(OnSelectEnter);
        interactable.selectExited.AddListener(OnSelectExit);
    }

    private void OnDisable()
    {
        if (joystickInput.action != null)
            joystickInput.action.Disable();

        interactable.selectEntered.RemoveListener(OnSelectEnter);
        interactable.selectExited.RemoveListener(OnSelectExit);
    }

    private void OnSelectEnter(SelectEnterEventArgs args)
    {
        isSelecting = true;
    }

    private void OnSelectExit(SelectExitEventArgs args)
    {
        isSelecting = false;
    }

    private void Update()
    {
        if (!isSelecting || modelManager == null || modelManager.CurrentModel == null)
            return;

        Vector2 inputVector = joystickInput.action.ReadValue<Vector2>();

        if (inputVector.magnitude > 0.1f)
        {
            GameObject currentModel = modelManager.CurrentModel;

            // 1. ROTATION (Left/Right Joystick)
            // Rotate around world Y-axis for horizontal joystick movement (left/right)
            float yRotation = inputVector.x * rotationSpeed * Time.deltaTime;
            currentModel.transform.Rotate(Vector3.down, yRotation, Space.World);

            // 2. SCALING (Up/Down Joystick)
            // Scale uniformally based on vertical joystick movement (up/down)
            float scaleDelta = inputVector.y * scaleSpeed * Time.deltaTime;

            // Ensure we don't scale down to zero or negative
            Vector3 currentScale = currentModel.transform.localScale;
            Vector3 newScale = currentScale + new Vector3(scaleDelta, scaleDelta, scaleDelta);

            // Optional: Clamp the scale to prevent models from becoming too small or too big
            float minScale = 0.0015f;
            float maxScale = 0.0026f;
            newScale = Vector3.Max(newScale, Vector3.one * minScale);
            newScale = Vector3.Min(newScale, Vector3.one * maxScale);

            currentModel.transform.localScale = newScale;

            // Whenever the model is grabbed and moved, adjust the CTPlaneControllers if they exist
            foreach (CTPlaneController controller in FindObjectsByType<CTPlaneController>(FindObjectsSortMode.None))
            {
                controller.ResetSlider();
            }
        }
        
    }
}