using UnityEngine;
using UnityEngine.UI;
using System.Linq;
//using UnityEditor.PackageManager; // Used for System.Array.Sort

public class CTPlaneController : MonoBehaviour
{
    [Header("Slider Reference")]
    public Slider sliceSlider;

    [Header("Slice Plane or Object to Move")]
    public Transform sliceObject;

    [Header("Movement Settings")]
    public Vector3 moveDirection = Vector3.up; 
    [Tooltip("Total physical distance (in scene units/meters) the plane should travel from bottom to top of the model")]
    public float modelPhysicalSize = 1.0f;

    [Header("Plane Thickness")]
    [Tooltip("The thickness (scale) of the plane along its movement axis. E.g., 0.001")]
    public float planeThickness = 0.001f;

    [Header("Auto-detection")]
    [Tooltip("If assigned and enabled, the controller will compute the travel distance from this target's renderer bounds along moveDirection.")]
    public Transform modelTarget;

    [Tooltip("Optional reference to the scene's ModelManager. If left empty the script will try to find one at runtime.")]
    public ModelManager modelManager;

    [Tooltip("If true and a Model Target is assigned, automatically compute the modelPhysicalSize from the target bounds along the move direction.")]
    public bool autoDetectModelSize = true;

    [Header("Slicing Steps")]
    [Tooltip("If true, use the slider's range (max-min) for slices. Otherwise set numSlices manually.")]
    public bool useSliderRangeForSlices = true;

    [Tooltip("Number of slices (used if useSliderRangeForSlices is false). Typical value: 120.")]
    public int numSlices = 120;

    private Vector3 initialPosition;

    private void Start()
    {
        if (sliceObject == null || sliceSlider == null)
        {
            Debug.LogError("Missing references in CTPlaneController!");
            return;
        }

        // Set initial plane state based on the current model
        UpdatePlaneToCurrentModel();

        // Hook slider event
        sliceSlider.onValueChanged.AddListener(UpdateSlicePosition);

        // Sync plane to current slider value
        UpdateSlicePosition(sliceSlider.value);
    }

    private void OnDestroy()
    {
    // FIX: Always check before removing the listener!
    if (sliceSlider != null) 
    {
        sliceSlider.onValueChanged.RemoveListener(UpdateSlicePosition);
    }
    }
    
    /// Called once on Start, and again whenever a new model is loaded via ResetSlider().
    private void UpdatePlaneToCurrentModel()
    {
        if (modelManager != null)
        {
            GameObject currentModelGO = modelManager.CurrentModel;
            modelTarget = currentModelGO != null ? currentModelGO.transform : null;
        }
        else
        {
            if (modelTarget == null) return;
        }
        
        if (modelTarget == null) return;

        // Set the plane's rotation 
        SetPlaneRotation(); 

        if (autoDetectModelSize)
        {
            modelPhysicalSize = CalculateAxisSizeFromTarget(modelTarget, moveDirection.normalized);
            AdjustPlaneScaleToModelBounds(modelTarget);
        }

        //Recenter the initial position to the model's bounds center
        Bounds b = GetCombinedBounds(modelTarget);
        initialPosition = b.center;
    }


    /// This updates all model-dependent parameters (scale, position) and resets the slider position to the center.
    public void ResetSlider()
    {
        UpdatePlaneToCurrentModel();

        // Hook slider event after Michael script removes it
        // This should be uncommented in future, I cant figure out the fix rn
        // sliceSlider.onValueChanged.AddListener(UpdateSlicePosition); 
        float mid = (sliceSlider.minValue + sliceSlider.maxValue) / 2f;
        sliceSlider.value = mid;
        UpdateSlicePosition(mid);

    }

    private void SetPlaneRotation()
    {
        Vector3 direction = moveDirection.normalized;

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        

        if (Mathf.Abs(Vector3.Dot(direction, Vector3.right)) > 0.99f)
        {
            // Apply a 90-degree rotation around the plane's own Z-axis (which is now aligned with the world X-axis)
            targetRotation *= Quaternion.Euler(0, 0, 90f);
        }
        
        sliceObject.rotation = targetRotation;
    }

    private void UpdateSlicePosition(float value)
    {
        if (sliceObject == null) {
            Debug.Log("SliceObject is null in UpdateSlicePosition");
            return;
        }

        // Determine normalized progress (0 = bottom, 1 = top) based on slider range
        float min = sliceSlider.minValue;
        float max = sliceSlider.maxValue;
        float progress = 0f;
        
        if (useSliderRangeForSlices)
        {
            float range = Mathf.Max(1e-6f, max - min);
            progress = Mathf.Clamp01((value - min) / range);
        }
        else
        {
            if (numSlices <= 1) numSlices = 2;
            float steps = (float)(numSlices - 1);
            progress = Mathf.Clamp01((value - min) / (steps == 0 ? 1f : steps));
        }

        // Map progress to -0.5 -> +0.5 around the initial position, scaled by the model physical size
        float t = progress - 0.5f;
        sliceObject.position = initialPosition + moveDirection.normalized * modelPhysicalSize * t;

    }

    private Bounds GetCombinedBounds(Transform target)
    {
        Bounds bounds = new Bounds(target.position, Vector3.zero);
        Renderer[] rends = target.GetComponentsInChildren<Renderer>();
        if (rends != null && rends.Length > 0)
        {
            bounds = rends[0].bounds;
            for (int i = 1; i < rends.Length; i++)
                bounds.Encapsulate(rends[i].bounds);
        }
        return bounds;
    }

    /// Adjusts the scale of the sliceObject so it covers the model's bounds in the two non-movement axes,
    /// and forces the scale along the movement axis to a fixed 'planeThickness'.
    private void AdjustPlaneScaleToModelBounds(Transform target)
    {
        if (sliceObject == null || target == null) return;

        Bounds b = GetCombinedBounds(target);
        Vector3 size = b.size;
        Vector3 moveAxis = moveDirection.normalized;
        
        // Determine the World Space dimensions of the model perpendicular to the moveDirection.

        float xSize = Mathf.Abs(size.x);
        float ySize = Mathf.Abs(size.y);
        float zSize = Mathf.Abs(size.z);
        
        // Calculate alignment with the moveDirection
        float dotX = Mathf.Abs(Vector3.Dot(Vector3.right, moveAxis));
        float dotY = Mathf.Abs(Vector3.Dot(Vector3.up, moveAxis));
        float dotZ = Mathf.Abs(Vector3.Dot(Vector3.forward, moveAxis));

        // Create an array of (dot, dimension, local index) tuples
        (float dot, float dim, int index)[] axes = new (float, float, int)[]
        {
            (dotX, xSize, 0), (dotY, ySize, 1), (dotZ, zSize, 2)
        };
        
        // Sort by dot product to find the two axes most PERPENDICULAR to moveDirection (smallest dot)
        System.Array.Sort(axes, (a, b) => a.dot.CompareTo(b.dot));

        // The two smallest dot products define the plane's extent (width/height).
        float requiredScalePerp1 = axes[0].dim;
        float requiredScalePerp2 = axes[1].dim;

        Vector3 newLocalScale = sliceObject.localScale;

        // Determine which local axis is closest to the 'thickness' direction 
        Vector3 localX = sliceObject.localToWorldMatrix.MultiplyVector(Vector3.right).normalized;
        Vector3 localY = sliceObject.localToWorldMatrix.MultiplyVector(Vector3.up).normalized;
        Vector3 localZ = sliceObject.localToWorldMatrix.MultiplyVector(Vector3.forward).normalized;

        float dotLocalX = Mathf.Abs(Vector3.Dot(localX, moveAxis));
        float dotLocalY = Mathf.Abs(Vector3.Dot(localY, moveAxis));
        float dotLocalZ = Mathf.Abs(Vector3.Dot(localZ, moveAxis));

        // Find the local axis (index 0, 1, 2) closest to the move direction
        float maxDot = Mathf.Max(dotLocalX, dotLocalY, dotLocalZ);
        int thicknessLocalIndex = 0;
        if (maxDot == dotLocalY) thicknessLocalIndex = 1;
        else if (maxDot == dotLocalZ) thicknessLocalIndex = 2;

        // Assign dimensions to the perpendicular local axes
        float dim1 = requiredScalePerp1;
        float dim2 = requiredScalePerp2;
        
        if (thicknessLocalIndex == 0) // Thickness is on Local X
        {
            newLocalScale.x = Mathf.Max(0.0001f, planeThickness);
            newLocalScale.y = dim1;
            newLocalScale.z = dim2;
        }
        else if (thicknessLocalIndex == 1) // Thickness is on Local Y
        {
            newLocalScale.x = dim1;
            newLocalScale.y = Mathf.Max(0.0001f, planeThickness);
            newLocalScale.z = dim2;
        }
        else // Thickness is on Local Z
        {
            newLocalScale.x = dim1;
            newLocalScale.y = dim2;
            newLocalScale.z = Mathf.Max(0.0001f, planeThickness);
        }
        
        sliceObject.localScale = newLocalScale;
    }

    // Called when change in the model size so planes update correctly.
    public void SetModelPhysicalSize(float size)
    {
        modelPhysicalSize = Mathf.Max(0f, size);
        
        SetPlaneRotation();
        
        if (modelTarget != null)
        {
            AdjustPlaneScaleToModelBounds(modelTarget);
        }

        if (sliceSlider != null)
        {
            UpdateSlicePosition(sliceSlider.value);
        }
    }

    private float CalculateAxisSizeFromTarget(Transform target, Vector3 axis)
    {
        if (target == null) return modelPhysicalSize;

        // Aggregate bounds from all renderers under the target
        Renderer[] rends = target.GetComponentsInChildren<Renderer>();
        if (rends == null || rends.Length == 0) return modelPhysicalSize;

        Bounds combined = rends[0].bounds;
        for (int i = 1; i < rends.Length; i++)
            combined.Encapsulate(rends[i].bounds);

        float size = 0f;
        // Compute projection of corner offsets onto axis to find total span
        Vector3[] corners = new Vector3[8];
        Vector3 c = combined.center;
        Vector3 e = combined.extents;
        corners[0] = c + new Vector3(+e.x, +e.y, +e.z); // dreampt this up, no big deal
        corners[1] = c + new Vector3(+e.x, +e.y, -e.z);
        corners[2] = c + new Vector3(+e.x, -e.y, +e.z);
        corners[3] = c + new Vector3(+e.x, -e.y, -e.z);
        corners[4] = c + new Vector3(-e.x, +e.y, +e.z);
        corners[5] = c + new Vector3(-e.x, +e.y, -e.z);
        corners[6] = c + new Vector3(-e.x, -e.y, +e.z);
        corners[7] = c + new Vector3(-e.x, -e.y, -e.z);

        float minProj = float.MaxValue;
        float maxProj = float.MinValue;
        for (int i = 0; i < 8; i++)
        {
            float p = Vector3.Dot(corners[i], axis);
            if (p < minProj) minProj = p;
            if (p > maxProj) maxProj = p;
        }

        size = Mathf.Abs(maxProj - minProj);
        return size;
    }
}