using UnityEngine;
using TMPro;

public class WorldLabelProxy : MonoBehaviour
{
    public TextMeshPro tmp;

    private Camera _cam;

    [Header("Size (world space)")]
    [Tooltip("Target text height in meters (e.g., 0.09 = 9 cm).")]
    public float worldHeightMeters = 0.09f; 

    [Header("Font")]
    [Tooltip("Fixed TMP point size; not autosized.")]
    public float fontSizePoints = 36f; // 3× the prior 12f
    public TMP_FontAsset fontAsset;
    public Color color = Color.white;

    private bool _scaledOnce;

    public void Init(string text, Camera cam, TMP_FontAsset font, Color c,
                     float fontSizePointsOverride = -1f, float _unusedPixelHeight = 0f)
    {
        _cam = cam;
        fontAsset = font ? font : fontAsset;
        color = c;

        if (!tmp) tmp = gameObject.AddComponent<TextMeshPro>();

        tmp.text = text;
        if (fontAsset) tmp.font = fontAsset;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;

        // Lock font size (no autosize)
        tmp.enableWordWrapping = false;
        tmp.overflowMode = TextOverflowModes.Overflow;
        tmp.enableAutoSizing = false;
        tmp.fontSize = (fontSizePointsOverride > 0f) ? fontSizePointsOverride : fontSizePoints;

        // Render on top
        var mr = tmp.GetComponent<MeshRenderer>();
        if (mr)
        {
            mr.sortingOrder = 32000;
            mr.receiveShadows = false;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        // One-time world-space scale to get proper size
        ApplyWorldHeightScale();
        _scaledOnce = true;

        // Slight push forward to avoid z-fighting
        if (_cam)
            transform.position += _cam.transform.forward * 0.0015f;
    }

    private void LateUpdate()
    {
        if (_cam)
            transform.rotation = Quaternion.LookRotation(transform.position - _cam.transform.position);

        if (!_scaledOnce)
        {
            ApplyWorldHeightScale();
            _scaledOnce = true;
        }
    }

    private void ApplyWorldHeightScale()
    {
        if (!tmp) return;

        tmp.ForceMeshUpdate();
        var size = tmp.bounds.size.y;

        // Handle very small bounds safely
        if (size <= 0.0001f)
        {
            string original = tmp.text;
            tmp.text = original + " ";
            tmp.ForceMeshUpdate();
            size = tmp.bounds.size.y;
            tmp.text = original;
        }

        size = Mathf.Max(0.0001f, size);
        float scale = worldHeightMeters / size;
        scale = Mathf.Clamp(scale, 0.0001f, 10f);
        transform.localScale = Vector3.one * scale;
    }
}
