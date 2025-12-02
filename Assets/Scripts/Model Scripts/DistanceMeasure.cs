using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Linq; // Required for sorting the hits

public class DistanceMeasure : MonoBehaviour
{
    // --- Model Management & Exclusion ---
    private Vector3 startPositionLocal;
    private Transform modelTransformReference;
    
    public ModelManager modelManager; 

    [Header("Exclusion Target")]
    [Tooltip("The Collider that the raycast should completely ignore (e.g., a rotation gizmo).")]
    public Collider ignoreCollider; 

    // --- Configuration Fields ---
    [Header("Measurement Target")]
    [Tooltip("The maximum distance the raycast can reach to find the model.")]
    public float maxRayDistance = 10f;
    
    [Header("Reticle Visual")]
    [Tooltip("The prefab or GameObject to use as a reticle/hit marker.")]
    public GameObject reticlePrefab;
    private GameObject currentReticleGO;

    [Header("Reticle Smoothing")]
    [Tooltip("Smoothing speed for reticle position (higher = faster snap).")]
    public float reticleMoveSpeed = 20f;
    [Tooltip("Smoothing speed for reticle rotation (higher = faster snap).")]
    public float reticleRotateSpeed = 20f;
    
    [Header("Ruler Activation")]
    [Tooltip("Toggled by the UI Button. Only measures when enabled.")]
    public bool isEnabled = false;

    // --- UI Button Visuals ---
    [Header("UI Button Visuals")]
    public Image toggleButtonImage;
    public Image toggleButtonImage2;
    public Sprite onSprite;
    public Sprite offSprite;

    [Range(0, 255)] public int onAlpha = 255;
    [Range(0, 255)] public int offAlpha = 150;
    
    // --- Input and Ruler Visuals ---
    [Header("Input Setup")]
    public InputActionProperty triggerAction;

    [Header("Ruler Visuals")]
    public LineRenderer lineRenderer;
    public GameObject textPrefab;

    // --- Internal State ---
    private bool isMeasuring = false;
    private Vector3 startPosition; 
    private GameObject currentTextGO;
    private TextMeshPro currentTextDisplay;
    private Transform measuringHandTransform;
    
    private RaycastHit latestHitInfo; 

    private const float COOLDOWN_DURATION = 0.25f;
    private float lastFinishTime = -COOLDOWN_DURATION; 


    private void Awake()
    {
        measuringHandTransform = this.transform; 

        if (lineRenderer != null)
        {
            lineRenderer.positionCount = 0;
            lineRenderer.enabled = false;
        }
    }
    
    private void Start()
    {
        UpdateVisuals(isEnabled);
        if (reticlePrefab != null)
        {
            currentReticleGO = Instantiate(reticlePrefab);
            currentReticleGO.SetActive(false);
        }
    }

    private void OnEnable()
    {
        if (triggerAction.action != null)
        {
            triggerAction.action.started += StartMeasurement;
            triggerAction.action.canceled += EndMeasurement; 
            triggerAction.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (triggerAction.action != null)
        {
            triggerAction.action.started -= StartMeasurement;
            triggerAction.action.canceled -= EndMeasurement;
            triggerAction.action.Disable();
            ClearMeasurement(); 
        }
    }

    private void Update()
    {
        // 1. Update Reticle Position and Visibility
        if (isEnabled)
        {
            RaycastHit hit;
            bool modelDetected = FindModelHitPoint(out hit);

            if (currentReticleGO != null)
            {
                if (modelDetected)
                {
                    currentReticleGO.SetActive(true);

                    currentReticleGO.transform.position = Vector3.Lerp(
                        currentReticleGO.transform.position,
                        hit.point,
                        Time.deltaTime * reticleMoveSpeed
                    );

                    currentReticleGO.transform.rotation = Quaternion.Lerp(
                        currentReticleGO.transform.rotation,
                        Quaternion.LookRotation(hit.normal),
                        Time.deltaTime * reticleRotateSpeed
                    );
                }
                else
                {
                    currentReticleGO.SetActive(false);
                }
            }
        }
        else if (currentReticleGO != null)
        {
            currentReticleGO.SetActive(false);
        }

        if (isMeasuring && isEnabled && lineRenderer != null)
        {
            PerformMeasurementUpdate();
        }
    }


    private bool FindModelHitPoint(out RaycastHit hit)
    {
        GameObject targetModel = modelManager.CurrentModel; 

        if (targetModel == null || targetModel.GetComponent<Collider>() == null)
        {
            hit = default;
            return false;
        }

        // 1. Use RaycastAll to get all hits along the ray
        RaycastHit[] hits = Physics.RaycastAll(measuringHandTransform.position, 
                                               measuringHandTransform.forward, 
                                               maxRayDistance);
        
        // 2. Sort the hits by distance (nearest first)
        System.Array.Sort(hits, (h1, h2) => h1.distance.CompareTo(h2.distance));

        // 3. Iterate through the sorted hits to find the first valid target
        foreach (RaycastHit currentHit in hits)
        {
            // A. Check if this is the collider we are supposed to ignore
            if (ignoreCollider != null && currentHit.collider == ignoreCollider)
            {
                continue; // Skip this collider
            }

            // B. Check if this is the actual target model
            if (currentHit.collider.gameObject == targetModel)
            {
                hit = currentHit;
                latestHitInfo = hit;
                return true; 
            }
        }
        
        // If the loop finishes without finding a valid hit
        hit = default;
        return false;
    }


    private void StartMeasurement(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        if (Time.time < lastFinishTime + COOLDOWN_DURATION) return;
        if (!isEnabled) return;
        
        RaycastHit hit;
        // GATING: Must hit the model to start
        if (!FindModelHitPoint(out hit))
        {
            ClearMeasurement(); 
            return; 
        }

        ClearMeasurement(); 

        isMeasuring = true;

        startPosition = hit.point; 

        modelTransformReference = modelManager.CurrentModel.transform;
        startPositionLocal = modelTransformReference.InverseTransformPoint(startPosition); 

        if (lineRenderer != null)
        {
            lineRenderer.enabled = true;
            lineRenderer.positionCount = 2;
            lineRenderer.SetPosition(0, startPosition);
            lineRenderer.SetPosition(1, startPosition); 

            if (textPrefab != null)
            {
                currentTextGO = Instantiate(textPrefab, startPosition, Quaternion.identity);
                currentTextDisplay = currentTextGO.GetComponent<TextMeshPro>();

                currentTextGO.transform.localScale = new Vector3(0.005f, 0.005f, 0.005f); 
            }
        }
    }

    private void EndMeasurement(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        if (!isMeasuring) return;

        PerformMeasurementUpdate(); 

        isMeasuring = false;
        lastFinishTime = Time.time;
    }


    private void PerformMeasurementUpdate()
    {
        if (modelTransformReference == null)
        {
            lineRenderer.enabled = false;
            return;
        }

        RaycastHit hit;
        Vector3 currentEndPositionWorld;

        // Find the current end point
        if (FindModelHitPoint(out hit))
        {
            currentEndPositionWorld = hit.point;
        }
        else
        {
            currentEndPositionWorld = lineRenderer.GetPosition(1); 
        }

        // Recalculate the START position based on current model transform
        Vector3 currentStartPositionWorld = modelTransformReference.TransformPoint(startPositionLocal);
        
        float distance = Vector3.Distance(currentStartPositionWorld, currentEndPositionWorld);

        // Update LineRenderer
        lineRenderer.SetPosition(0, currentStartPositionWorld); 
        lineRenderer.SetPosition(1, currentEndPositionWorld); 

        // Update Text Display
        if (currentTextDisplay != null)
        {
            Vector3 midPoint = (currentStartPositionWorld + currentEndPositionWorld) / 2f;
            currentTextDisplay.transform.position = midPoint;

            // FIX: Text always faces the user
            Transform cameraTransform = Camera.main.transform;
            Vector3 toCamera = cameraTransform.position - midPoint;
            
            toCamera.y = 0;

            if (toCamera.sqrMagnitude > 0.0001f)
            {
                Quaternion lookAtCamera = Quaternion.LookRotation(toCamera, Vector3.up);

                Quaternion finalRotation = lookAtCamera * Quaternion.Euler(0, 180f, 0);

                currentTextDisplay.transform.rotation = finalRotation;
            }

            if (modelManager.currentModelIndex >= 0 && modelManager.currentModelIndex < 4) // Models 0-3
            {
                float distance_mm = (distance * 600f) / (modelManager.CurrentModel.transform.localScale.x * 500); // Transforms the local scale of 0.002 to 1, and divide the measurement by that to get real world size
                currentTextDisplay.text = $"{distance_mm:F0} mm";

            }

            else if (modelManager.currentModelIndex >= 4 && modelManager.currentModelIndex < 8) // Models 4-7
            {
                float distance_mm = (distance * 550f) / (modelManager.CurrentModel.transform.localScale.x * 500); // Adjust this if measurement seems off (not sure yet how to scale with model size)
                currentTextDisplay.text = $"{distance_mm:F0} mm";
            }

            else if (modelManager.currentModelIndex >= 8 && modelManager.currentModelIndex < 12) // Models 8-11
            {
                float distance_mm = (distance * 600f) / (modelManager.CurrentModel.transform.localScale.x * 500); // Adjust this if measurement seems off (not sure yet how to scale with model size)
                currentTextDisplay.text = $"{distance_mm:F0} mm";
            }

            else
            {
                float distance_mm = (distance * 550f) / (modelManager.CurrentModel.transform.localScale.x * 500); // Adjust this if measurement seems off (not sure yet how to scale with model size)
                currentTextDisplay.text = $"{distance_mm:F0} mm";
            }
        }
    }

    public void ToggleRuler()
    {
        isEnabled = !isEnabled;
        UpdateVisuals(isEnabled);

        if (!isEnabled)
        {
            ClearMeasurement();
        }
        else
        {
            Debug.Log("VR Ruler Enabled. Press and hold controller trigger to measure.");
        }
    }

    private void ClearMeasurement()
    {
        if (lineRenderer != null)
        {
            lineRenderer.positionCount = 0;
            lineRenderer.enabled = false;
        }

        if (currentTextGO != null)
        {
            Destroy(currentTextGO);
            currentTextGO = null;
            currentTextDisplay = null;
        }

        if (currentReticleGO != null)
        {
            currentReticleGO.SetActive(false); 
        }

        isMeasuring = false;
        modelTransformReference = null; 
    }

    private void UpdateVisuals(bool isActive)
    {
        // Logic for toggleButtonImage
        if (toggleButtonImage != null && onSprite != null && offSprite != null)
        {
            toggleButtonImage.sprite = isActive ? onSprite : offSprite;
            Color c = toggleButtonImage.color;
            float a = (isActive ? onAlpha : offAlpha) / 255f;
            c.a = a;
            toggleButtonImage.color = c;
        }
        else if (toggleButtonImage == null)
        {
            Debug.LogWarning("Toggle Button Image reference 1 is missing. Cannot update visuals.");
        }

        // Logic for toggleButtonImage2
        if (toggleButtonImage2 != null && onSprite != null && offSprite != null)
        {
            toggleButtonImage2.sprite = isActive ? onSprite : offSprite;
            Color c = toggleButtonImage2.color;
            float a = (isActive ? onAlpha : offAlpha) / 255f;
            c.a = a;
            toggleButtonImage2.color = c;
        }
        else if (toggleButtonImage2 == null)
        {
            Debug.LogWarning("Toggle Button Image reference 2 is missing. Cannot update visuals.");
        }
    }
}