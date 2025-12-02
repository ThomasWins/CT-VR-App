using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class FullscreenSliceController : MonoBehaviour
{
    public GameObject fullscreenPanel;
    public Image headerBackground;
    public RawImage sliceImage;
    public Slider sliceSlider;
    public Button closeButton;
    public MeasureTool measureTool;

    private List<Texture2D> currentSlices = new List<Texture2D>();
    private int currentSliceIndex;

    private void Awake()
    {
        if (fullscreenPanel != null)
        {
        
            fullscreenPanel.SetActive(true);    
            fullscreenPanel.SetActive(false);  
        }

        if (closeButton != null)
            closeButton.onClick.AddListener(CloseFullscreen);
    }
    public void ShowFullscreen(Sprite headerSprite, List<Texture2D> slices, int startingIndex)
    {

        if (fullscreenPanel == null)
        {
            Debug.LogError("fullscreen is NULL");
            return;
        }

        fullscreenPanel.SetActive(true);

        if (fullscreenPanel == null || slices == null || slices.Count == 0)
            return;

        currentSlices = slices;
        currentSliceIndex = Mathf.Clamp(startingIndex, 0, slices.Count - 1);

        if (headerBackground != null && headerSprite != null)
        {
            headerBackground.sprite = headerSprite;
            headerBackground.type = Image.Type.Sliced;
        }

        if (sliceSlider != null)
        {
            sliceSlider.minValue = 0;
            sliceSlider.maxValue = slices.Count - 1;
            sliceSlider.wholeNumbers = true;
            sliceSlider.value = currentSliceIndex;

            sliceSlider.onValueChanged.RemoveAllListeners();
            sliceSlider.onValueChanged.AddListener(OnSliderChanged);
        }

        UpdateImage(currentSliceIndex);

        if (measureTool != null && sliceImage != null)
        {
            measureTool.drawingArea = sliceImage.rectTransform;
        }

        fullscreenPanel.SetActive(true);
    }

    public void OnSliderChanged(float value)
    {
        int index = Mathf.Clamp(Mathf.RoundToInt(value), 0, currentSlices.Count - 1);
        UpdateImage(index);
    }

    private void UpdateImage(int index)
    {
        currentSliceIndex = index;
        if (sliceImage != null && currentSlices.Count > index)
            sliceImage.texture = currentSlices[index];
    }

    public void CloseFullscreen()
    {
        if (fullscreenPanel != null)
            fullscreenPanel.SetActive(false);
    }
}