using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class MeasureTool : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    [Header("References")]
    public RectTransform drawingArea;
    public LineRenderer lineRenderer;
    public TMP_Text distanceLabel;
    public Button toggleButton;
    public TMP_Text toggleButtonText;

    [Header("Extra Reference")]
    public RawImage sliceImage;  

    [Header("Colors")]
    public Color normalTextColor = Color.white;
    public Color activeTextColor = Color.green;

    [Header("Real World Size (mm)")]
    public float realWidthMM = 532f;
    public float realHeightMM = 252f;

    private bool measureMode = false;
    private bool isDragging = false;
    private Vector2 startScreenPos;

    void Start()
    {
        if (toggleButton != null)
        {
            toggleButton.onClick.AddListener(ToggleMeasureMode);

            if (toggleButtonText == null)
                toggleButtonText = toggleButton.GetComponentInChildren<TMP_Text>();

            UpdateToggleButtonColor();
        }

        if (lineRenderer != null)
        {
            lineRenderer.enabled = false;
            lineRenderer.positionCount = 0;
            lineRenderer.startWidth = 0.004f;
            lineRenderer.endWidth = 0.004f;

            if (lineRenderer.material == null)
            {
                Material mat = new Material(Shader.Find("Unlit/Color"));
                mat.color = Color.red;
                lineRenderer.material = mat;
            }

            lineRenderer.enabled = false;
        }

        if (distanceLabel != null)
        {
            distanceLabel.text = "";
        }

        if (sliceImage != null)
            sliceImage.raycastTarget = measureMode;
    }

    public void ToggleMeasureMode()
    {
        measureMode = !measureMode;
        UpdateToggleButtonColor();

        if (sliceImage != null)
        {
            sliceImage.raycastTarget = measureMode;
        }

        if (!measureMode)
        {
            if (lineRenderer != null)
                lineRenderer.enabled = false;

            if (distanceLabel != null)
                distanceLabel.text = "";
        }
    }

    void UpdateToggleButtonColor()
    {
        if (toggleButtonText != null)
        {
            toggleButtonText.color = measureMode ? activeTextColor : normalTextColor;
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!measureMode) return;

        isDragging = true;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(drawingArea, eventData.position, eventData.pressEventCamera, out startScreenPos);

        if (lineRenderer != null)
        {
            lineRenderer.positionCount = 2;
            lineRenderer.enabled = true;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!measureMode || !isDragging) return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(drawingArea, eventData.position, eventData.pressEventCamera, out Vector2 currentPos);

        Vector3 worldStart = drawingArea.TransformPoint(startScreenPos);
        Vector3 worldEnd = drawingArea.TransformPoint(currentPos);

        if (lineRenderer != null)
        {
            lineRenderer.SetPosition(0, worldStart);
            lineRenderer.SetPosition(1, worldEnd);
        }

        Vector2 size = drawingArea.rect.size;
        float scaleX = realWidthMM / size.x;
        float scaleY = realHeightMM / size.y;

        Vector2 delta = currentPos - startScreenPos;
        float distanceMM = Mathf.Sqrt(Mathf.Pow(delta.x * scaleX, 2) + Mathf.Pow(delta.y * scaleY, 2));

        if (distanceLabel != null)
        {
            distanceLabel.text = $"{distanceMM:0.0} mm";
            distanceLabel.transform.position = (worldStart + worldEnd) / 2f;
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isDragging = false;
    }

    public void ClearMeasurement()
    {
        isDragging = false;

        if (lineRenderer != null)
        {
            lineRenderer.enabled = false;
            lineRenderer.positionCount = 0;
        }

        if (distanceLabel != null)
            distanceLabel.text = "";
    }

    private void OnDisable()
    {
        ClearMeasurement();
    }
}
